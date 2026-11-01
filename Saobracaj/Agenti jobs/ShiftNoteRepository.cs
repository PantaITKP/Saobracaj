using System;
using System.Data;

namespace Saobracaj.Agenti_jobs
{
    internal sealed class ShiftNoteSnapshot
    {
        public string Text;
        public byte[] Version;
    }

    internal static class ShiftNoteRepository
    {
        internal static ShiftNoteSnapshot Load()
        {
            using (var c = Db.Open())
            {
                var rows = Db.Table(c, null, "SELECT Tekst,VerzijaReda FROM dbo.OperativniPlanNapomenaSmene WHERE ID=1");
                if (rows.Rows.Count == 0)
                    throw new InvalidOperationException("Nedostaje početna napomena smene. Pokrenite SQL skriptu.");
                return new ShiftNoteSnapshot
                {
                    Text = Db.Text(rows.Rows[0], "Tekst"),
                    Version = (byte[])rows.Rows[0]["VerzijaReda"]
                };
            }
        }

        internal static ShiftNoteSnapshot Save(string text, byte[] version)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Command(c, null, "UPDATE dbo.OperativniPlanNapomenaSmene SET Tekst=@text,Izmenio=@user,IzmenjenoUtc=GETUTCDATE() OUTPUT inserted.VerzijaReda WHERE ID=1 AND VerzijaReda=@rv", "@text", text, "@user", OperationalSession.UserName, "@rv", version))
            {
                var rv = cmd.ExecuteScalar() as byte[];
                if (rv == null)
                    throw new DBConcurrencyException("Napomenu smene je promenio drugi korisnik. Kopirajte svoje izmene pre ponovnog učitavanja.");
                return new ShiftNoteSnapshot
                {
                    Text = text,
                    Version = rv
                };
            }
        }
    }
}
