using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Saobracaj.Agenti_jobs;
using Saobracaj.Planiranje.Modeli;
using Saobracaj.Planiranje.Pravila;

namespace Saobracaj.Planiranje.Podaci
{
    // Čita sve što je potrebno računu plana (samo čitanje).
    internal static class PlanRepo
    {
        private const int DanaUnazadKalendar = 30;
        private static readonly string[] Kljucevi =
        {
            "OBRADA_PRIJEM_MIN", "OBRADA_PREDAJA_MIN", "VOZNJA_MIN", "BRZINA_PRAZNE_VOZNJE_KMH", "GPS_STAROST_MAX_MIN",
            "REZERVA_PRE_POLASKA_MIN", "TEZINA_RAVNOMERNOST", "TEZINA_SERVIS", "MASA_LOK_U_VUCI"
        };

        internal static int? Ceo(DataRow r, string k)
        {
            return r.IsNull(k) ? (int?)null : Convert.ToInt32(r[k]);
        }

        internal static decimal? Dec(DataRow r, string k)
        {
            if (r.IsNull(k)) return null;
            object v = r[k];
            if (v is string)
            {
                decimal d;
                return decimal.TryParse(((string)v).Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out d) ? d : (decimal?)null;
            }
            return Convert.ToDecimal(v);
        }

        internal static DateTime? Dat(DataRow r, string k)
        {
            return r.IsNull(k) ? (DateTime?)null : Convert.ToDateTime(r[k]);
        }

        private static string Txt(DataRow r, string k)
        {
            return r.IsNull(k) ? null : Convert.ToString(r[k]).Trim();
        }

        internal static PlanKontekst Ucitaj(DateTime od, DateTime doDatuma)
        {
            var k = new PlanKontekst();
            foreach (string kljuc in Kljucevi) k.Parametri[kljuc] = ParametriRepo.ZaKljuc(kljuc);
            using (var c = Db.Open())
            {
                var veze = new List<VezaMreze>();
                foreach (DataRow r in Db.Table(c, null, "SELECT v.ID, v.StanicaAID, v.StanicaBID, v.DuzinaKm, v.PrugaID, COALESCE(v.Elektrificirana, p.Elektrificirana) AS Elektrificirana, " +
                    "p.Kategorija, v.OsovinskoT, v.Vmax, v.Dvokolosecna, v.MerodavniUspon, v.MerodavniPad, v.KiM " +
                    "FROM planiranje.OP_MrezaVeza v LEFT JOIN planiranje.OP_Pruga p ON p.ID = v.PrugaID WHERE v.Aktivna = 1").Rows)
                    veze.Add(new VezaMreze
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        A = Convert.ToInt32(r["StanicaAID"]),
                        B = Convert.ToInt32(r["StanicaBID"]),
                        Km = Dec(r, "DuzinaKm") ?? 0,
                        PrugaID = Ceo(r, "PrugaID"),
                        Elektrificirana = r.IsNull("Elektrificirana") ? (bool?)null : Convert.ToBoolean(r["Elektrificirana"]),
                        KategorijaPruge = Txt(r, "Kategorija"),
                        OsovinskoT = Dec(r, "OsovinskoT"),
                        Vmax = Ceo(r, "Vmax"),
                        Dvokolosecna = r.IsNull("Dvokolosecna") ? (bool?)null : Convert.ToBoolean(r["Dvokolosecna"]),
                        MerodavniUspon = Dec(r, "MerodavniUspon"),
                        MerodavniPad = Dec(r, "MerodavniPad"),
                        KiM = Convert.ToBoolean(r["KiM"])
                    });
                k.Veze = veze;
                // Pruge na KiM su pod nadzorom UNMIK-a, pa ih planiranje zaobilazi.
                k.Graf = new Graf(veze.Where(v => !v.KiM));

                foreach (DataRow r in Db.Table(c, null, "SELECT ID, Naziv FROM planiranje.OP_Stanice").Rows)
                    k.NaziviStanica[Convert.ToInt32(r["ID"])] = Txt(r, "Naziv");

                foreach (DataRow r in Db.Table(c, null,
                    "SELECT m.PrugaID, m.StanicaID, m.RB, m.MaxDuzinaVozaAB, m.MaxDuzinaVozaBA, m.Klasa FROM planiranje.OP_PrugaMesto m " +
                    "JOIN planiranje.OP_Pruga p ON p.ID = m.PrugaID JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID " +
                    "WHERE v.Status = N'VAZECA' AND m.StanicaID IS NOT NULL").Rows)
                    k.MestaPruga.Add(new MestoPruge
                    {
                        PrugaID = Convert.ToInt32(r["PrugaID"]),
                        StanicaID = Convert.ToInt32(r["StanicaID"]),
                        RB = Convert.ToInt32(r["RB"]),
                        MaxDuzinaAB = Ceo(r, "MaxDuzinaVozaAB"),
                        MaxDuzinaBA = Ceo(r, "MaxDuzinaVozaBA"),
                        Klasa = Txt(r, "Klasa")
                    });

                foreach (DataRow r in Db.Table(c, null,
                    "SELECT t.ID, t.BrojVoza, t.PocetnaStanicaID, t.KrajnjaStanicaID, t.VremePolaska, t.VremeDolaska, t.DanPlus, t.MaxMasaT, t.MaxDuzinaM, t.AdHoc, t.AdHocStatus, t.AdHocNajavaID, " +
                    "CASE WHEN t.AdHoc = 1 THEN t.AdHocVaziOd ELSE v.VaziOd END AS VaziOd, CASE WHEN t.AdHoc = 1 THEN t.AdHocVaziDo ELSE v.VaziDo END AS VaziDo " +
                    "FROM planiranje.OP_Trase t LEFT JOIN planiranje.OP_TraseVerzija v ON v.ID = t.VerzijaID " +
                    "WHERE t.PocetnaStanicaID IS NOT NULL AND t.KrajnjaStanicaID IS NOT NULL AND ((t.AdHoc = 0 AND v.Status = N'VAZECA') OR (t.AdHoc = 1 AND t.AdHocStatus IN (N'ZAHTEV', N'ODOBRENA')))").Rows)
                    k.Trase.Add(new TrasaOP
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        BrojVoza = Txt(r, "BrojVoza"),
                        PocetnaID = Ceo(r, "PocetnaStanicaID"),
                        KrajnjaID = Ceo(r, "KrajnjaStanicaID"),
                        Polazak = r.IsNull("VremePolaska") ? (TimeSpan?)null : (TimeSpan)r["VremePolaska"],
                        Dolazak = r.IsNull("VremeDolaska") ? (TimeSpan?)null : (TimeSpan)r["VremeDolaska"],
                        DanPlus = Ceo(r, "DanPlus") ?? 0,
                        MaxMasaT = Dec(r, "MaxMasaT"),
                        MaxDuzinaM = Dec(r, "MaxDuzinaM"),
                        AdHoc = Convert.ToBoolean(r["AdHoc"]),
                        AdHocStatus = Txt(r, "AdHocStatus"),
                        AdHocNajavaID = Ceo(r, "AdHocNajavaID"),
                        VaziOd = Dat(r, "VaziOd"),
                        VaziDo = Dat(r, "VaziDo")
                    });

                foreach (DataRow r in Db.Table(c, null,
                    "SELECT t.TelegramID, t.BrojTelegrama, t.Vrsta, t.Kolosek, t.VaziOd, t.VaziDo, t.TrajeOd, t.TrajeDo, t.Napomena, " +
                    "(SELECT TOP 1 p.ID FROM planiranje.OP_Pruga p JOIN planiranje.OP_RegistarVerzija v ON v.ID = p.RegistarVerzijaID WHERE p.IzvorniID = t.IzvornaPrugaID AND v.Status = N'VAZECA' ORDER BY p.ID) AS PrugaID, " +
                    "(SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = t.IzvornaStanicaOdID ORDER BY s.ID) AS OdID, " +
                    "(SELECT TOP 1 COALESCE(s.DuplikatOd, s.ID) FROM planiranje.OP_Stanice s WHERE s.IzvorniID = t.IzvornaStanicaDoID ORDER BY s.ID) AS DoID " +
                    "FROM planiranje.OP_vTelegram t WHERE t.Aktivan = 1 AND t.VaziOd <= @do AND (t.VaziDo IS NULL OR t.VaziDo >= @od)",
                    "@od", od, "@do", doDatuma.AddDays(2)).Rows)
                    k.Telegrami.Add(new TelegramOP
                    {
                        ID = Convert.ToInt32(r["TelegramID"]),
                        Broj = Txt(r, "BrojTelegrama"),
                        PrugaID = Ceo(r, "PrugaID"),
                        OdID = Ceo(r, "OdID"),
                        DoID = Ceo(r, "DoID"),
                        Vrsta = Txt(r, "Vrsta"),
                        Kolosek = Txt(r, "Kolosek"),
                        VaziOd = Convert.ToDateTime(r["VaziOd"]),
                        VaziDo = Dat(r, "VaziDo"),
                        TrajeOd = r.IsNull("TrajeOd") ? (TimeSpan?)null : (TimeSpan)r["TrajeOd"],
                        TrajeDo = r.IsNull("TrajeDo") ? (TimeSpan?)null : (TimeSpan)r["TrajeDo"],
                        Napomena = Txt(r, "Napomena")
                    });

                foreach (DataRow r in Db.Table(c, null, "SELECT SerijaID, PrugaID, StanicaOdID, StanicaDoID, Smer, BrojLokomotiva, Sloj, MaxMasaT, VaziOd, VaziDo FROM planiranje.OP_TablicaMasa").Rows)
                    k.Tablica.Add(new RedMase
                    {
                        SerijaID = Convert.ToInt32(r["SerijaID"]),
                        PrugaID = Ceo(r, "PrugaID"),
                        StanicaOdID = Ceo(r, "StanicaOdID"),
                        StanicaDoID = Ceo(r, "StanicaDoID"),
                        Smer = Txt(r, "Smer"),
                        BrojLokomotiva = Convert.ToInt32(r["BrojLokomotiva"]),
                        Sloj = Txt(r, "Sloj"),
                        MaxMasaT = Convert.ToDecimal(r["MaxMasaT"]),
                        VaziOd = Dat(r, "VaziOd"),
                        VaziDo = Dat(r, "VaziDo")
                    });

                foreach (DataRow r in Db.Table(c, null,
                    "SELECT l.ID, l.Broj, l.SerijaID, s.Oznaka, s.VrstaVuce, COALESCE(l.MasaT, s.MasaT) AS MasaT, l.ImaGPS, l.Vlasnistvo, l.MaticnaStanicaID " +
                    "FROM planiranje.OP_Lokomotive l LEFT JOIN planiranje.OP_SerijeLokomotiva s ON s.ID = l.SerijaID WHERE l.Aktivna = 1 ORDER BY l.Broj").Rows)
                    k.Lokomotive.Add(new LokomotivaOP
                    {
                        ID = Convert.ToInt32(r["ID"]),
                        Broj = Txt(r, "Broj"),
                        SerijaID = Ceo(r, "SerijaID"),
                        Serija = Txt(r, "Oznaka"),
                        Vuca = Txt(r, "VrstaVuce"),
                        MasaT = Dec(r, "MasaT"),
                        ImaGPS = Convert.ToBoolean(r["ImaGPS"]),
                        Zakup = Txt(r, "Vlasnistvo") == "ZAKUP",
                        MaticnaStanicaID = Ceo(r, "MaticnaStanicaID")
                    });

                k.Kalendar = Kalendar(c, null, od.AddDays(-DanaUnazadKalendar), doDatuma.AddDays(14), null);
            }
            return k;
        }

        internal static List<ZauzetostLok> Kalendar(System.Data.SqlClient.SqlConnection c, System.Data.SqlClient.SqlTransaction tx, DateTime od, DateTime doVreme, int? lokomotiva)
        {
            var lista = new List<ZauzetostLok>();
            foreach (DataRow r in Db.Table(c, tx,
                "SELECT k.LokomotivaID, k.Vrsta, k.PosaoID, k.RB, k.Od, k.Do, k.Projekat, k.Napomena, p.PredajaStanicaID " +
                "FROM planiranje.OP_vLokomotivaKalendar k LEFT JOIN planiranje.OP_Posao p ON p.ID = k.PosaoID " +
                "WHERE k.Od < @do AND k.Do > @od AND (@l IS NULL OR k.LokomotivaID = @l)",
                "@od", od, "@do", doVreme, "@l", (object)lokomotiva).Rows)
                lista.Add(new ZauzetostLok
                {
                    LokomotivaID = Convert.ToInt32(r["LokomotivaID"]),
                    Vrsta = Txt(r, "Vrsta"),
                    PosaoID = Ceo(r, "PosaoID"),
                    RB = Ceo(r, "RB") ?? 1,
                    Od = Convert.ToDateTime(r["Od"]),
                    Do = Convert.ToDateTime(r["Do"]),
                    Projekat = Txt(r, "Projekat"),
                    Napomena = Txt(r, "Napomena"),
                    PredajaStanicaID = Ceo(r, "PredajaStanicaID")
                });
            return lista;
        }

        private const string UpitPosla =
            "SELECT p.ID, p.NajavaID, p.BrojNajave, p.PlanOtpravna, p.PlanUputna, p.NajavaSerijaVagona, p.StatusNajave, p.StatusOpis, p.PrijemStanicaID, p.PredajaStanicaID, p.PlaniraniPrijem, p.PredvidjenaPredaja, " +
            "p.PlaniraniPolazak, p.PlaniraniDolazak, p.PlaniranaPredaja, p.NajavaBrutoT, p.NajavaDuzinaM, p.NajavaBrojKola, p.Projekat, p.Faza, p.RowVer, t.BrutoZbir " +
            "FROM planiranje.OP_vPosao p LEFT JOIN planiranje.OP_vTeretnicaNajava t ON t.NajavaID = p.NajavaID ";

        private static PosaoPlan UPosao(DataRow r)
        {
            return new PosaoPlan
            {
                ID = Convert.ToInt32(r["ID"]),
                NajavaID = Convert.ToInt32(r["NajavaID"]),
                // Najave iz novijeg unosa nemaju broj, pa je oznaka ID iz tabele Najava.
                BrojNajave = string.IsNullOrWhiteSpace(Txt(r, "BrojNajave")) ? Convert.ToString(r["NajavaID"]) : Txt(r, "BrojNajave"),
                StatusNajave = Txt(r, "StatusOpis") ?? Txt(r, "StatusNajave"),
                Otpravna = Txt(r, "PlanOtpravna"),
                Uputna = Txt(r, "PlanUputna"),
                TipKola = Txt(r, "NajavaSerijaVagona"),
                PrijemID = Ceo(r, "PrijemStanicaID"),
                PredajaID = Ceo(r, "PredajaStanicaID"),
                Prijem = Convert.ToDateTime(r["PlaniraniPrijem"]),
                PredvidjenaPredaja = Dat(r, "PredvidjenaPredaja"),
                PlaniraniPolazak = Dat(r, "PlaniraniPolazak"),
                PlaniraniDolazak = Dat(r, "PlaniraniDolazak"),
                PlaniranaPredaja = Dat(r, "PlaniranaPredaja"),
                BrutoNajava = Dec(r, "NajavaBrutoT"),
                BrutoTeretnica = Dec(r, "BrutoZbir"),
                DuzinaM = Dec(r, "NajavaDuzinaM"),
                BrojKola = Dec(r, "NajavaBrojKola").HasValue ? (int?)Dec(r, "NajavaBrojKola").Value : null,
                Projekat = Txt(r, "Projekat"),
                Faza = Convert.ToInt32(r["Faza"]),
                RowVer = (byte[])r["RowVer"]
            };
        }

        internal static List<PosaoPlan> Poslovi(DateTime od, DateTime doDatuma)
        {
            using (var c = Db.Open())
                return Db.Table(c, null, UpitPosla + "WHERE p.PlaniraniPrijem >= @od AND p.PlaniraniPrijem < @do AND p.Status <> N'OTKAZAN' " +
                    // Predat voz se više ne planira; ako je imao lokomotivu, ostaje u kalendaru kao istorija.
                    "AND ((ISNULL(p.StatusNajave, 0) <> 7 AND p.StvarnaPredaja IS NULL) OR EXISTS (SELECT 1 FROM planiranje.OP_PosaoLokomotiva l WHERE l.PosaoID = p.ID)) " +
                    "ORDER BY p.PlaniraniPrijem", "@od", od, "@do", doDatuma)
                    .Rows.Cast<DataRow>().Select(UPosao).ToList();
        }

        internal static PosaoPlan Posao(int id)
        {
            using (var c = Db.Open())
            {
                DataTable t = Db.Table(c, null, UpitPosla + "WHERE p.ID = @id AND p.PlaniraniPrijem IS NOT NULL", "@id", id);
                return t.Rows.Count == 0 ? null : UPosao(t.Rows[0]);
            }
        }
    }
}
