using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    internal static class RadniNalogDopuna
    {
        internal static DataTable Stanice()
        {
            using (var c = Db.Open()) return Db.Table(c, null, "SELECT ID,RTRIM(Opis) AS Opis FROM dbo.stanice ORDER BY Opis");
        }
        internal static DataRow Najava(int id)
        {
            using (var c = Db.Open())
            {
                var t = Db.Table(c, null, "SELECT ID,Tezina,BrojKola,Tovareno,SerijaVagona FROM dbo.Najava WHERE ID=@id", "@id", id);
                if (t.Rows.Count != 1) throw new InvalidOperationException("Najava ne postoji ili njen ID nije jedinstven: " + id);
                return t.Rows[0];
            }
        }
        internal static DataTable Nalozi(int najava)
        {
            using (var c = Db.Open()) return Db.Table(c, null, @"SELECT r.ID,r.StatusRN,r.Komentar,r.Planer,
(SELECT COUNT(*) FROM dbo.RadniNalogTrase t WHERE t.IDRadnogNaloga=r.ID) AS BrojStavki
FROM dbo.RadniNalog r WHERE EXISTS(SELECT 1 FROM dbo.RadniNalogVezaNajave v WHERE v.IDRadnogNaloga=r.ID AND v.IDNajave=@najava)
ORDER BY r.ID DESC", "@najava", najava);
        }
        internal static DataTable Stavke(int rn, string broj)
        {
            using (var c = Db.Open()) return Db.Table(c, null, @"SELECT r.*,RTRIM(s1.Opis) AS Otpravna,RTRIM(s2.Opis) AS Uputna,t.Godina
FROM dbo.RadniNalogTrase r JOIN dbo.Trase t ON t.ID=r.IDTrase
LEFT JOIN dbo.stanice s1 ON s1.ID=r.StanicaOd LEFT JOIN dbo.stanice s2 ON s2.ID=r.StanicaDo
WHERE r.IDRadnogNaloga=@rn AND LTRIM(RTRIM(t.Voz))=@broj ORDER BY r.RB", "@rn", rn, "@broj", broj);
        }
        internal static DataTable Mesta()
        {
            using (var c = Db.Open()) return Db.Table(c, null, "SELECT RTRIM(SmSifra) AS SmSifra,RTRIM(SmNaziv) AS Opis FROM dbo.Mesta WHERE Lokomotiva=1 ORDER BY SmSifra");
        }
        internal static List<RNRaspodela> PripremiAutomatski(DataRow voz)
        {
            ProveriNajavuZaZavrsetak(voz);
            int id = int.Parse(Db.Text(voz, "AnnouncementFullId"), CultureInfo.InvariantCulture);
            var n = Najava(id);
            var nalozi = Nalozi(id);
            if (nalozi.Rows.Count == 0) throw new InvalidOperationException("Najava " + id + " nema povezan postojeci radni nalog. Najpre je povezite u postojecoj aplikaciji.");
            if (nalozi.Rows.Count != 1) return null;
            int rn = Convert.ToInt32(nalozi.Rows[0]["ID"]);
            bool rezi = Convert.ToBoolean(voz["Rezi"]);
            int count = 0;
            if (!rezi)
            {
                using (var c = Db.Open())
                {
                    var links = Db.Table(c, null, "SELECT DISTINCT BrojKola FROM dbo.RadniNalogVezaNajave WHERE IDRadnogNaloga=@rn AND IDNajave=@naj", "@rn", rn, "@naj", id);
                    int? automatic = RNAutomatskiIzbor.AutomatskaKolicina(n.IsNull("BrojKola") ? (int?)null : Convert.ToInt32(n["BrojKola"]),
                        links.Rows.Cast<DataRow>().Select(x => x.IsNull("BrojKola") ? (int?)null : Convert.ToInt32(x["BrojKola"])), false);
                    if (!automatic.HasValue) return null;
                    count = automatic.Value;
                }
                if (n.IsNull("BrojKola") || count <= 0 || count != Convert.ToInt32(n["BrojKola"])) return null;
            }
            return new List<RNRaspodela> { new RNRaspodela {
                IDNajave=id, RadniNalog=rn, RB=0, BrojKola=count,
                Tovareno=!rezi && !n.IsNull("Tovareno") && Convert.ToInt32(n["Tovareno"])==1,
                PlaniranaMasa=n.IsNull("Tezina") ? 0 : Convert.ToDecimal(n["Tezina"]),
                NajavaSnapshot=n, StatusSnapshot=Db.Text(nalozi.Rows[0],"StatusRN") } };
        }
        internal static bool Nezavrsena(DataRow r)
        {
            return r.IsNull("DatumDolaskaReal") || Convert.ToDateTime(r["DatumDolaskaReal"]) < new DateTime(1900, 1, 2);
        }
        internal static void ProveriNajavuZaZavrsetak(DataRow row)
        {
            int id;
            if (!int.TryParse(Db.Text(row, "AnnouncementFullId"), out id) || id <= 0)
                throw new InvalidOperationException("Pre zavrsetka povezite voz sa najavom (i za rezi voznju). Potreban je puni ID najave.");
        }
        internal static bool IstiPodaci(DataRow a, DataRow b, IEnumerable<string> columns)
        {
            return columns.All(k => a.Table.Columns.Contains(k) && b.Table.Columns.Contains(k) && Equals(a[k], b[k]));
        }
        internal static void Upisi(SqlConnection c, SqlTransaction tx, SifarnikPrevoz p, int legacy)
        {
            if (p.RNStavke == null) throw new InvalidOperationException("Nije pripremljena raspodela radnih naloga.");
            var v = Db.Table(c, tx, "SELECT * FROM dbo.OperativniPlanVozovi WITH(UPDLOCK,HOLDLOCK) WHERE ID=@id", "@id", p.ID).Rows[0];
            ProveriNajavuZaZavrsetak(v);
            int primary = int.Parse(Db.Text(v, "AnnouncementFullId"), CultureInfo.InvariantCulture);
            bool rezi = Convert.ToBoolean(v["Rezi"]);
            RadniNalogPravila.ProveriRaspodelu(p.RNStavke, rezi, primary);
            if (!Convert.ToBoolean(v["Started"]) || v.IsNull("StartedAtLocal") || !p.KrajDeonice.HasValue)
                throw new InvalidOperationException("Voz mora biti pokrenut i zavrsen sa upisanim lokalnim datumima.");
            DateTime od = Convert.ToDateTime(v["StartedAtLocal"]), kraj = p.KrajDeonice.Value;
            int minutes = RadniNalogPravila.Trajanje(od, kraj);
            decimal weight = Convert.ToDecimal(v["Weight"]);
            string note = p.NapomenaDeonice ?? Db.Text(v, "Note");
            if (note.Length > 500) throw new InvalidOperationException("Napomena za radni nalog moze imati najvise 500 znakova.");
            if (p.RNStanicaOd <= 0 || p.RNStanicaDo <= 0) throw new InvalidOperationException("Nedostaju stanice realizacije.");
            var stations = Db.Table(c, tx, "SELECT ID,Opis FROM dbo.stanice WITH(HOLDLOCK) WHERE ID IN (@od,@do)", "@od", p.RNStanicaOd, "@do", p.RNStanicaDo);
            if (stations.Rows.Count != (p.RNStanicaOd == p.RNStanicaDo ? 1 : 2)) throw new DBConcurrencyException("Stanica realizacije vise ne postoji.");
            // Zakljucavaju se i podaci koje menjaju stare forme bez sp_getapplock.
            Db.Table(c, tx, "SELECT COUNT(*) FROM dbo.RadniNalogTrase WITH(TABLOCKX,HOLDLOCK)");
            Db.Table(c, tx, "SELECT COUNT(*) FROM dbo.RadniNalogVezaNajave WITH(TABLOCKX,HOLDLOCK)");
            foreach (var a in p.RNStavke)
            {
                var naj = Db.Table(c, tx, "SELECT ID,Tezina,BrojKola,Tovareno,SerijaVagona FROM dbo.Najava WITH(HOLDLOCK) WHERE ID=@id", "@id", a.IDNajave);
                if (naj.Rows.Count != 1 || a.NajavaSnapshot == null || !IstiPodaci(a.NajavaSnapshot, naj.Rows[0], new[] { "ID", "Tezina", "BrojKola", "Tovareno", "SerijaVagona" }))
                    throw new DBConcurrencyException("Najava je promenjena. Ponovo pripremite raspodelu.");
                var rn = Db.Table(c, tx, "SELECT ID,StatusRN FROM dbo.RadniNalog WITH(HOLDLOCK) WHERE ID=@rn", "@rn", a.RadniNalog);
                if (rn.Rows.Count != 1 || Db.Text(rn.Rows[0], "StatusRN") != a.StatusSnapshot)
                    throw new DBConcurrencyException("Nalog ne postoji, nije jedinstven ili mu je promenjen status. Ponovite izbor.");
                var link = Db.Table(c, tx, "SELECT ID FROM dbo.RadniNalogVezaNajave WITH(HOLDLOCK) WHERE IDRadnogNaloga=@rn AND IDNajave=@naj", "@rn", a.RadniNalog, "@naj", a.IDNajave);
                if (link.Rows.Count == 0) throw new DBConcurrencyException("Najava vise nije povezana sa izabranim nalogom. Novi nalog se ne kreira.");
                if (p.RNStavke.Where(x => x.IDNajave == a.IDNajave).Sum(x => x.BrojKola) > (naj.Rows[0].IsNull("BrojKola") ? 0 : Convert.ToInt32(naj.Rows[0]["BrojKola"])))
                    throw new InvalidOperationException("Raspodeljeni broj kola prelazi broj kola najave " + a.IDNajave);
            }
            foreach (var group in p.RNStavke.GroupBy(a => new { a.RadniNalog, a.RB }))
            {
                var a = group.First(); int rb = a.RB;
                var previous = Db.Table(c, tx, "SELECT * FROM dbo.OperativniPlanRNRealizacija WITH(UPDLOCK,HOLDLOCK) WHERE PrevozID=@id AND IDRadnogNaloga=@rn", "@id", p.ID, "@rn", a.RadniNalog);
                if (previous.Rows.Count > 0) throw new InvalidOperationException("Ova voznja je vec evidentirana na nalogu " + a.RadniNalog + ". Ponovo ucitajte spisak.");
                decimal masa = group.Sum(x => x.PlaniranaMasa);
                if (rb > 0)
                {
                    var old = Db.Table(c, tx, "SELECT * FROM dbo.RadniNalogTrase WHERE IDRadnogNaloga=@rn AND RB=@rb", "@rn", a.RadniNalog, "@rb", rb);
                    if (old.Rows.Count != 1 || a.StavkaSnapshot == null || !Nezavrsena(old.Rows[0]) ||
                        !IstiPodaci(a.StavkaSnapshot, old.Rows[0], old.Columns.Cast<DataColumn>().Select(k => k.ColumnName)))
                        throw new DBConcurrencyException("Izabrana stavka je promenjena ili vec zavrsena. Ponovite izbor.");
                    // Postojece veze tog RB ne smeju biti izgubljene niti prepisane drugom voznjom.
                    var links = Db.Table(c, tx, "SELECT * FROM dbo.RadniNalogVezaNajave WHERE IDRadnogNaloga=@rn AND RB=@rb", "@rn", a.RadniNalog, "@rb", rb);
                    if (links.Rows.Cast<DataRow>().Any(r => r.IsNull("IDNajave") || !group.Any(x => x.IDNajave == Convert.ToInt32(r["IDNajave"]))))
                        throw new InvalidOperationException("Izabrani RB sadrzi jos najava. Dodajte ih u raspodelu ili izaberite novu stavku.");
                    using (var cmd = Db.Command(c, tx, @"UPDATE dbo.RadniNalogTrase SET IDTrase=@trasa,DatumPolaskaReal=@od,DatumDolaskaReal=@do,
VremeReal=@min,PlaniranaMasa=@masa,MasaLokomotive=0,MasaVoza=0,BrutoMasa=@bruto,Napomena=@note,Rezi=@rezi,
StanicaOd=@sod,StanicaDo=@sdo,StatusTrase=1,Poslato=0 WHERE IDRadnogNaloga=@rn AND RB=@rb",
                        "@trasa", legacy, "@od", od, "@do", kraj, "@min", minutes, "@masa", masa, "@bruto", weight, "@note", note,
                        "@rezi", rezi ? 1 : 0, "@sod", p.RNStanicaOd, "@sdo", p.RNStanicaDo, "@rn", a.RadniNalog, "@rb", rb))
                        if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException("Nije azurirana tacno jedna stavka naloga.");
                }
                else
                {
                    using (var cmd = Db.Command(c, tx, "SELECT ISNULL(MAX(RB),0)+1 FROM dbo.RadniNalogTrase WHERE IDRadnogNaloga=@rn", "@rn", a.RadniNalog)) rb = Convert.ToInt32(cmd.ExecuteScalar());
                    using (var cmd = Db.Command(c, tx, @"INSERT dbo.RadniNalogTrase
(IDRadnogNaloga,RB,IDTrase,DatumPolaskaReal,DatumDolaskaReal,DatumPolaska,DatumDolaska,Vreme,VremeReal,
PlaniranaMasa,MasaLokomotive,MasaVoza,BrutoMasa,Napomena,Rezi,StanicaOd,StanicaDo,StatusTrase,Poslato)
VALUES(@rn,@rb,@trasa,@od,@do,@prazno,@prazno,@min,@min,@masa,0,0,@bruto,@note,@rezi,@sod,@sdo,1,0)",
                        "@rn", a.RadniNalog, "@rb", rb, "@trasa", legacy, "@od", od, "@do", kraj, "@prazno", new DateTime(1900, 1, 1),
                        "@min", minutes, "@masa", masa, "@bruto", weight, "@note", note, "@rezi", rezi ? 1 : 0, "@sod", p.RNStanicaOd, "@sdo", p.RNStanicaDo)) cmd.ExecuteNonQuery();
                }
                long realization;
                using (var cmd = Db.Command(c, tx, @"INSERT dbo.OperativniPlanRNRealizacija
(PrevozID,IDRadnogNaloga,RB,IDTrase,BrojVoznje,BrojIzvorneTrase,StanicaOd,StanicaDo,Polazak,Dolazak,Rezi,BrutoMasa,Kreirao,KreiranoLokalno,GlavniPrevozID)
VALUES(@id,@rn,@rb,@trasa,@voz,@izvor,@sod,@sdo,@od,@do,@rezi,@bruto,@user,@now,@root); SELECT CAST(SCOPE_IDENTITY() AS bigint)",
                    "@id", p.ID, "@rn", a.RadniNalog, "@rb", rb, "@trasa", legacy, "@voz", p.BrojVoznje, "@izvor", p.Broj,
                    "@sod", p.RNStanicaOd, "@sdo", p.RNStanicaDo, "@od", od, "@do", kraj, "@rezi", rezi,
                    "@bruto", weight, "@user", OperationalSession.UserName, "@now", DateTime.Now, "@root", p.GlavniPrevozID)) realization = Db.ScalarId(cmd);
                if (p.RNLokomotive == null) throw new InvalidOperationException("Nisu proverene lokomotive.");
                foreach (string lok in p.RNLokomotive)
                {
                    var mesta = Db.Table(c, tx, "SELECT SmSifra FROM dbo.Mesta WITH(HOLDLOCK) WHERE SmSifra=@lok AND Lokomotiva=1", "@lok", lok);
                    if (mesta.Rows.Count != 1) throw new DBConcurrencyException("Lokomotiva vise ne postoji u sifarniku: " + lok);
                    Db.Table(c, tx, "SELECT COUNT(*) FROM dbo.RadniNalogLokNaTrasi WITH(TABLOCKX,HOLDLOCK)");
                    using (var cmd = Db.Command(c, tx, @"INSERT dbo.RadniNalogLokNaTrasi
(IDRadnogNaloga,RB,IDTrase,SMSifra,Komentar,Vucna,StanicaOd,StanicaDo,vreme,ID2,RBTrase)
SELECT @rn,ISNULL(MAX(RB),0)+1,@trasa,@lok,N'',@vucna,@sod,@sdo,@min,@rn,@rb
FROM dbo.RadniNalogLokNaTrasi WHERE IDRadnogNaloga=@rn AND IDTrase=@trasa",
                        "@rn", a.RadniNalog, "@trasa", legacy, "@lok", lok,
                        "@vucna", string.Equals(lok, p.RNVucnaLokomotiva, StringComparison.Ordinal) ? 1 : 0,
                        "@sod", p.RNStanicaOd, "@sdo", p.RNStanicaDo, "@min", minutes, "@rb", rb)) cmd.ExecuteNonQuery();
                }
                foreach (var part in group)
                {
                    var links = Db.Table(c, tx, "SELECT ID FROM dbo.RadniNalogVezaNajave WHERE IDRadnogNaloga=@rn AND RB=@rb AND IDNajave=@naj", "@rn", part.RadniNalog, "@rb", rb, "@naj", part.IDNajave);
                    if (links.Rows.Count > 1) throw new InvalidOperationException("Postoje duple veze iste najave i RB. Ispravite ih pre evidentiranja.");
                    if (links.Rows.Count == 1)
                    {
                        using (var cmd = Db.Command(c, tx, "UPDATE dbo.RadniNalogVezaNajave SET BrojKola=@kola WHERE IDRadnogNaloga=@rn AND RB=@rb AND IDNajave=@naj", "@kola", part.BrojKola, "@rn", part.RadniNalog, "@rb", rb, "@naj", part.IDNajave)) cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using (var cmd = Db.Command(c, tx, @"INSERT dbo.RadniNalogVezaNajave(ID,IDRadnogNaloga,IDNajave,BrojKola,RB)
SELECT ISNULL(MAX(ID),0)+1,@rn,@naj,@kola,@rb FROM dbo.RadniNalogVezaNajave", "@rn", part.RadniNalog, "@naj", part.IDNajave, "@kola", part.BrojKola, "@rb", rb)) cmd.ExecuteNonQuery();
                    }
                    using (var cmd = Db.Command(c, tx, @"INSERT dbo.OperativniPlanRNNajava(RealizacijaID,IDNajave,BrojKola,Tovareno,PlaniranaMasa)
VALUES(@r,@naj,@kola,@tov,@masa)", "@r", realization, "@naj", part.IDNajave, "@kola", part.BrojKola, "@tov", part.Tovareno, "@masa", part.PlaniranaMasa)) cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
