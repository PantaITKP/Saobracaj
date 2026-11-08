using System;
using System.Collections.Generic;

namespace Saobracaj.Planiranje.Modeli
{
    // Roba jednog pravca posla.
    internal sealed class RobaPravca
    {
        internal int? NHM;
        internal decimal? Bruto;
        internal int? BrojKola;
        // Serija kola, kao Najava.SerijaVagona (dbo.VagoniSerije.Serija); u planu je „Tip kola“.
        internal string SerijaKola;
        internal decimal? Duzina;
        internal decimal? Neto;
        internal string RIDBroj;
        internal string OMBroj;
    }

    // JOBS (OP_030): posao koji zamenjuje najavu. Stanice su iz dbo.stanice, partneri iz dbo.Partnerji.
    // Bez stanice 2 posao ima jedan pravac (1 → 3), a sa njom dva (1 → 2 i 2 → 3).
    internal sealed class JobOP
    {
        internal int ID;
        internal int? PlatilacID;
        internal int? OrganizatorID;
        internal int? TipPrevozaID;
        internal int? OtpravnaID;
        internal int? UputnaID;
        internal int Status = 1;
        internal string Napomena;
        internal int? PreuzimaOdID;
        internal int? PredajeOperateruID;
        internal int Stanica1ID;
        internal int? Stanica2ID;
        internal int Stanica3ID;
        internal DateTime? ETD;
        internal RobaPravca Roba1 = new RobaPravca();
        internal RobaPravca Roba2 = new RobaPravca();
        internal byte[] RowVer;

        internal int BrojPravaca { get { return Stanica2ID.HasValue ? 2 : 1; } }

        // Pravac: od i do (dbo.stanice.ID).
        internal int OdPravca(int pravac) { return pravac == 1 ? Stanica1ID : Stanica2ID.Value; }
        internal int DoPravca(int pravac) { return pravac == 1 ? (Stanica2ID ?? Stanica3ID) : Stanica3ID; }

        // Polja za LOG: naziv i vrednost kao tekst.
        internal Dictionary<string, string> ZaLog()
        {
            Func<object, string> t = v => v == null ? "" : Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
            return new Dictionary<string, string>
            {
                { "Platilac", t(PlatilacID) }, { "Organizator", t(OrganizatorID) }, { "Tip prevoza", t(TipPrevozaID) },
                { "Otpravna", t(OtpravnaID) }, { "Uputna", t(UputnaID) }, { "Status", t(Status) }, { "Napomena", Napomena ?? "" },
                { "Preuzima od", t(PreuzimaOdID) }, { "Predaje operateru", t(PredajeOperateruID) },
                { "Stanica 1", t(Stanica1ID) }, { "Stanica 2", t(Stanica2ID) }, { "Stanica 3", t(Stanica3ID) },
                { "ETD", ETD.HasValue ? ETD.Value.ToString("dd.MM.yyyy HH:mm") : "" },
                { "NHM 1", t(Roba1.NHM) }, { "Bruto 1", t(Roba1.Bruto) }, { "Broj kola 1", t(Roba1.BrojKola) }, { "Serija kola 1", Roba1.SerijaKola ?? "" }, { "Dužina 1", t(Roba1.Duzina) },
                { "Neto 1", t(Roba1.Neto) }, { "RID broj 1", Roba1.RIDBroj ?? "" }, { "OM broj 1", Roba1.OMBroj ?? "" },
                { "NHM 2", t(Roba2.NHM) }, { "Bruto 2", t(Roba2.Bruto) }, { "Broj kola 2", t(Roba2.BrojKola) }, { "Serija kola 2", Roba2.SerijaKola ?? "" }, { "Dužina 2", t(Roba2.Duzina) },
                { "Neto 2", t(Roba2.Neto) }, { "RID broj 2", Roba2.RIDBroj ?? "" }, { "OM broj 2", Roba2.OMBroj ?? "" }
            };
        }
    }
}
