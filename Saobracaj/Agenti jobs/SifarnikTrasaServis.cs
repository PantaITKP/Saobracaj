using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    internal static class SifarnikTrasaServis
    {
        internal static List<SifarnikPrevoz> Ucitaj(DataTable source)
        {
            var result = new List<SifarnikPrevoz>();
            var documents = new Dictionary<long, SifarnikDokument>();
            var excelRows = new Dictionary<long, RouteInfo>();
            using (var c = Db.Open())
            {
                foreach (DataRow row in source.Rows)
                {
                    if (row.RowState == DataRowState.Deleted || !Convert.ToBoolean(row["Completed"])) continue;
                    // Stari arhivirani prevozi se ovde ucitavaju i radi dopune nultih vrednosti cenovnika.
                    // Nova RN pravila ne primenjuju se retroaktivno na tu posebnu popravku.
                    if (!Convert.ToBoolean(row["CompletionRecorded"]))
                    {
                        RadniNalogDopuna.ProveriNajavuZaZavrsetak(row);
                        if (!Convert.ToBoolean(row["Started"]) || row.IsNull("StartedAtLocal") || row.IsNull("CompletedAtLocal"))
                            throw new InvalidOperationException("Voz " + Db.Text(row, "Train") + " mora imati stvarni polazak i dolazak.");
                        RadniNalogPravila.Trajanje(Convert.ToDateTime(row["StartedAtLocal"]), Convert.ToDateTime(row["CompletedAtLocal"]));
                    }
                    SifarnikDokument doc = null;
                    RouteInfo excel = null;
                    if (!row.IsNull("RouteID"))
                    {
                        long route = Convert.ToInt64(row["RouteID"]);
                        var idRows = Db.Table(c, null, "SELECT DocumentID FROM dbo.OperativniPlanTrasa WHERE ID=@id", "@id", route);
                        if (idRows.Rows.Count != 1) throw new InvalidOperationException("Nedostaje izvorna Excel trasa.");
                        long document = Convert.ToInt64(idRows.Rows[0]["DocumentID"]);
                        UcitajDokument(c, document, documents, excelRows);
                        doc = documents[document];
                        excel = excelRows[route];
                    }
                    else if (!row.IsNull("CenovnikDokumentID"))
                    {
                        long document = Convert.ToInt64(row["CenovnikDokumentID"]);
                        UcitajDokument(c, document, documents, excelRows);
                        doc = documents[document];
                    }
                    if (excel == null && !Convert.ToBoolean(row["IsAdHoc"]))
                        throw new InvalidOperationException("Nedostaje izvorna Excel trasa za voz " + Db.Text(row, "Train") +
                            ". Dopunski broj se ne može upisati kao nova trasa u šifarnik.");
                    if (excel == null)
                        excel = new RouteInfo
                        {
                            Train = string.IsNullOrWhiteSpace(Db.Text(row, "OriginalTrain")) ? Db.Text(row, "Train") : Db.Text(row, "OriginalTrain"),
                            From = Db.Text(row, "From"),
                            To = Db.Text(row, "To"),
                            Relation = Db.Text(row, "From") + " - " + Db.Text(row, "To"),
                            MaxWeight = Convert.ToDecimal(row["Weight"]),
                            Price = row.IsNull("Price") ? (decimal?)null : Convert.ToDecimal(row["Price"])
                        };
                    var p = new SifarnikPrevoz
                    {
                        ID = Convert.ToInt64(row["ID"]),
                        Verzija = ((byte[])row["VerzijaReda"]).ToArray(),
                        Broj = excel.Train.Trim(),
                        BrojVoznje = Db.Text(row, "Train").Trim(),
                        Excel = excel,
                        Dokument = doc,
                        AdHoc = Convert.ToBoolean(row["IsAdHoc"]),
                        Od = row.IsNull("CenovnikVaziOd") ? (doc == null ? null : doc.Od) : (DateTime?)Convert.ToDateTime(row["CenovnikVaziOd"]),
                        Do = row.IsNull("CenovnikVaziDo") ? (doc == null ? null : doc.Do) : (DateTime?)Convert.ToDateTime(row["CenovnikVaziDo"])
                    };
                    if (p.Od.HasValue && p.Do.HasValue) p.Godina = CenovnikPravila.Godina(p.Od.Value, p.Do.Value);
                    result.Add(p);
                }
            }
            return result;
        }

        private static void UcitajDokument(SqlConnection c, long id, Dictionary<long, SifarnikDokument> docs, Dictionary<long, RouteInfo> map)
        {
            if (docs.ContainsKey(id)) return;
            var data = Db.Table(c, null, "SELECT ID,OriginalniNaziv,Sadrzaj,VaziOd,VaziDo FROM dbo.OperativniPlanTraseDokument WHERE ID=@id", "@id", id);
            if (data.Rows.Count != 1) throw new InvalidOperationException("Nedostaje dokument cenovnika " + id);
            var d = data.Rows[0];
            var doc = new SifarnikDokument
            {
                ID = id,
                Naziv = Db.Text(d, "OriginalniNaziv"),
                Od = d.IsNull("VaziOd") ? (DateTime?)null : Convert.ToDateTime(d["VaziOd"]),
                Do = d.IsNull("VaziDo") ? (DateTime?)null : Convert.ToDateTime(d["VaziDo"])
            };
            doc.NedostajaoPeriod = !doc.Od.HasValue || !doc.Do.HasValue;
            List<RouteInfo> rows;
            using (var stream = new MemoryStream((byte[])d["Sadrzaj"])) rows = ExcelRoutes.Read(stream);
            var stored = Db.Table(c, null, "SELECT ID,[From],[To],Train FROM dbo.OperativniPlanTrasa WHERE DocumentID=@id ORDER BY ID", "@id", id);
            if (stored.Rows.Count != rows.Count) throw new InvalidOperationException("Excel i sačuvani redovi nisu usklađeni: " + doc.Naziv + ". Potrebno je ponovo uvesti cenovnik.");
            for (int i = 0; i < rows.Count; i++)
            {
                var original = stored.Rows[i];
                if (Db.Text(original, "Train").Trim() != rows[i].Train.Trim() ||
                    Db.Text(original, "From").Trim() != rows[i].From.Trim() ||
                    Db.Text(original, "To").Trim() != rows[i].To.Trim())
                    throw new InvalidOperationException("Nije moguće pouzdano povezati stare Excel redove: " + doc.Naziv);
                map[Convert.ToInt64(original["ID"])] = rows[i];
            }
            docs[id] = doc;
        }

        internal static DataTable PripremiStanice(List<SifarnikPrevoz> plans)
        {
            using (var c = Db.Open())
            {
                var existing = Db.Table(c, null, "SELECT Voz,Godina FROM dbo.Trase");
                foreach (var p in plans)
                    p.PotrebneStanice = !existing.Rows.Cast<DataRow>().Any(r =>
                    {
                        int year;
                        string yearText = Convert.ToString(r["Godina"]).Trim();
                        return Convert.ToString(r["Voz"]).Trim() == p.Broj && yearText.Length == 4 &&
                            yearText.All(char.IsDigit) && int.TryParse(yearText, out year) && year <= p.Godina;
                    });
                return plans.Any(p => p.PotrebneStanice)
                    ? Db.Table(c, null, "SELECT ID,RTRIM(Opis) AS Opis FROM dbo.stanice ORDER BY Opis")
                    : new DataTable();
            }
        }

        internal static int Evidentiraj(List<SifarnikPrevoz> plans)
        {
            if (plans.Count == 0) return 0;
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "OperativniPlanTrainNumbers");
                    Db.Lock(c, tx, "OperativniPlanRouteCatalog");
                    Db.Lock(c, tx, "SifarnikTrasaGodina");
                    Db.Lock(c, tx, "OperativniPlanDopunaRN");
                    using (var check = Db.Command(c, tx, "SELECT CASE WHEN OBJECT_ID('dbo.OperativniPlanRNRealizacija','U') IS NOT NULL AND OBJECT_ID('dbo.OperativniPlanRNNajava','U') IS NOT NULL AND COL_LENGTH('dbo.RadniNalogLokNaTrasi','RBTrase') IS NOT NULL THEN 1 ELSE 0 END"))
                        if (Convert.ToInt32(check.ExecuteScalar()) != 1)
                            throw new InvalidOperationException("Najpre pokrenite SQL skripte za dopunu RN i OperativniPlan-RNAuto.sql.");
                    foreach (var p in plans)
                    {
                        if (!p.Od.HasValue || !p.Do.HasValue) throw new InvalidOperationException("Nije unet period cenovnika.");
                        if (CenovnikPravila.Godina(p.Od.Value, p.Do.Value) != p.Godina)
                            throw new InvalidOperationException("Godina ne odgovara krajnjem datumu cenovnika.");
                        var current = Db.Table(c, tx, @"SELECT ID FROM dbo.OperativniPlanVozovi WITH(UPDLOCK,HOLDLOCK)
WHERE ID=@id AND VerzijaReda=@rv AND Completed=1 AND CompletedAtLocal IS NOT NULL AND CompletionRecorded=0",
                            "@id", p.ID, "@rv", p.Verzija);
                        if (current.Rows.Count != 1)
                            throw new DBConcurrencyException("Neki prevoz je promenjen na drugom računaru. Nijedan prevoz nije evidentiran.");
                    }
                    foreach (var doc in plans.Where(p => p.Dokument != null && p.Dokument.NedostajaoPeriod)
                        .Select(p => p.Dokument).Distinct())
                    {
                        var rows = Db.Table(c, tx, "SELECT VaziOd,VaziDo FROM dbo.OperativniPlanTraseDokument WITH(UPDLOCK,HOLDLOCK) WHERE ID=@id", "@id", doc.ID);
                        if (!doc.Od.HasValue || !doc.Do.HasValue || rows.Rows.Count != 1)
                            throw new InvalidOperationException("Nedostaje period dokumenta cenovnika.");
                        var row = rows.Rows[0];
                        if (row.IsNull("VaziOd") && row.IsNull("VaziDo"))
                            using (var cmd = Db.Command(c, tx, "UPDATE dbo.OperativniPlanTraseDokument SET VaziOd=@od,VaziDo=@do,Godina=@year WHERE ID=@id",
                                "@od", doc.Od.Value, "@do", doc.Do.Value, "@year", doc.Do.Value.Year, "@id", doc.ID)) cmd.ExecuteNonQuery();
                        else if (row.IsNull("VaziOd") || row.IsNull("VaziDo") ||
                            Convert.ToDateTime(row["VaziOd"]).Date != doc.Od.Value.Date || Convert.ToDateTime(row["VaziDo"]).Date != doc.Do.Value.Date)
                            throw new DBConcurrencyException("Drugi korisnik je uneo drugačiji period cenovnika. Ponovo učitajte prevoze.");
                    }
                    foreach (var p in plans)
                    {
                        int legacy = ObezbediTrasu(c, tx, p);
                        RadniNalogDopuna.Upisi(c, tx, p, legacy);
                        using (var cmd = Db.Command(c, tx, @"UPDATE dbo.OperativniPlanVozovi SET
CompletionRecorded=1,CompletedRecordedAtLocal=@now,IzmenjenoUtc=GETUTCDATE(),
CenovnikDokumentID=@document,CenovnikVaziOd=@od,CenovnikVaziDo=@do,CenovnikGodina=@year,SifarnikTrasaID=@legacy
WHERE ID=@id AND VerzijaReda=@rv AND CompletionRecorded=0",
                            "@now", DateTime.Now, "@document", p.Dokument == null ? (object)null : p.Dokument.ID,
                            "@od", p.Od.Value, "@do", p.Do.Value, "@year", p.Godina, "@legacy", legacy, "@id", p.ID, "@rv", p.Verzija))
                            if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException("Prevoz je promenjen; ceo upis je poništen.");
                    }
                    tx.Commit();
                    return plans.Count;
                }
                catch { try { tx.Rollback(); } catch { } throw; }
            }
        }

        private static int ObezbediTrasu(SqlConnection c, SqlTransaction tx, SifarnikPrevoz p)
        {
            string year = p.Godina.ToString("0000", System.Globalization.CultureInfo.InvariantCulture);
            var existingRows = Db.Table(c, tx, @"SELECT TOP 1 * FROM dbo.Trase WITH(UPDLOCK,HOLDLOCK)
WHERE LTRIM(RTRIM(Voz))=@voz AND LTRIM(RTRIM(Godina))=@year ORDER BY ID", "@voz", p.Broj, "@year", year);
            if (existingRows.Rows.Count > 0)
            {
                var row = existingRows.Rows[0];
                int existing = Convert.ToInt32(row["ID"]);
                DopuniPostojecu(c, tx, row, p.Excel);
                if (p.AdHoc)
                    using (var tag = Db.Command(c, tx, "UPDATE dbo.Trase SET OpisRelacije=N'AdHoc' WHERE ID=@id AND RTRIM(ISNULL(OpisRelacije,N'')) COLLATE Latin1_General_BIN2<>N'AdHoc'", "@id", existing))
                        tag.ExecuteNonQuery();
                return existing;
            }
            var old = Db.Table(c, tx, @"SELECT TOP 1 * FROM dbo.Trase WITH(UPDLOCK,HOLDLOCK)
WHERE LTRIM(RTRIM(Voz))=@voz AND LEN(LTRIM(RTRIM(Godina)))=4
AND LTRIM(RTRIM(Godina)) NOT LIKE '%[^0-9]%' AND LTRIM(RTRIM(Godina))<@year
ORDER BY LTRIM(RTRIM(Godina)) DESC,ID DESC", "@voz", p.Broj, "@year", year);
            DataRow template = old.Rows.Count == 0 ? null : old.Rows[0];
            if (template == null)
            {
                if (p.StanicaOd <= 0 || p.StanicaDo <= 0)
                    throw new DBConcurrencyException("Nedostaju stanice za novu trasu " + p.Broj + ". Ponovite evidentiranje.");
                using (var check = Db.Command(c, tx, "SELECT COUNT(*) FROM dbo.stanice WITH(HOLDLOCK) WHERE ID IN (@od,@do)",
                    "@od", p.StanicaOd, "@do", p.StanicaDo))
                    if (Convert.ToInt32(check.ExecuteScalar()) != (p.StanicaOd == p.StanicaDo ? 1 : 2))
                        throw new InvalidOperationException("Stanica je izbrisana iz šifarnika.");
            }
            int id;
            using (var cmd = Db.Command(c, tx, "SELECT ISNULL(MAX(ID),0) FROM dbo.Trase WITH(TABLOCKX,HOLDLOCK)"))
                id = checked(Convert.ToInt32(cmd.ExecuteScalar()) + 1);
            var values = CenovnikPravila.Vrednosti(template, p.Excel, p.Broj, p.Godina, p.StanicaOd, p.StanicaDo, p.AdHoc);
            var columns = new[] { "ID" }.Concat(CenovnikPravila.SifarnikKolone).ToArray();
            var args = new List<object>();
            for (int i = 0; i < columns.Length; i++) { args.Add("@p" + i); args.Add(i == 0 ? (object)id : values[columns[i]]); }
            using (var cmd = Db.Command(c, tx, "INSERT dbo.Trase (" +
                string.Join(",", columns.Select(n => "[" + n + "]")) + ") VALUES (" +
                string.Join(",", Enumerable.Range(0, columns.Length).Select(i => "@p" + i)) + ")", args.ToArray()))
                cmd.ExecuteNonQuery();
            return id;
        }

        private static bool DopuniPostojecu(SqlConnection c, SqlTransaction tx, DataRow row, RouteInfo source)
        {
            var values = CenovnikPravila.Dopune(row, source);
            if (values.Count == 0) return false;
            var fields = values.Keys.ToArray();
            var args = new List<object> { "@id", row["ID"] };
            for (int i = 0; i < fields.Length; i++)
            {
                args.Add("@v" + i); args.Add(values[fields[i]]);
            }
            using (var cmd = Db.Command(c, tx, "UPDATE dbo.Trase SET " +
                string.Join(",", fields.Select((name, index) => "[" + name + "]=@v" + index)) +
                " WHERE ID=@id", args.ToArray()))
                if (cmd.ExecuteNonQuery() != 1) throw new DBConcurrencyException("Trasa je izbrisana pre dopune.");
            return true;
        }

        // Correct the earlier skipped zeroes only where the exact catalog/source link was retained.
        // It does not create another trip, another start record or a new catalog row.
        internal static int DopuniVecEvidentirane()
        {
            DataTable data;
            using (var c = Db.Open())
                data = Db.Table(c, null, @";WITH izbor AS
(
 SELECT v.ID,ROW_NUMBER() OVER(PARTITION BY v.SifarnikTrasaID,v.RouteID
 ORDER BY v.CompletedRecordedAtLocal,v.ID) AS rn
 FROM dbo.OperativniPlanVozovi v
 JOIN dbo.Trase t ON t.ID=v.SifarnikTrasaID
 WHERE v.CompletionRecorded=1 AND v.Completed=1
 AND (ISNULL(t.TezinaVoza,0)=0 OR ISNULL(t.DuzinaVoza,0)=0
      OR ISNULL(t.ProcenatKocenja,0)=0 OR ISNULL(t.Cena,0)=0 OR ISNULL(t.ProcenatTrase,0)=0)
)
SELECT v.* FROM dbo.OperativniPlanVozovi v JOIN izbor i ON i.ID=v.ID
WHERE i.rn=1 ORDER BY v.CompletedRecordedAtLocal,v.ID");
            if (data.Rows.Count == 0) return 0;
            var plans = Ucitaj(data);
            var links = data.Rows.Cast<DataRow>().ToDictionary(r => Convert.ToInt64(r["ID"]), r => Convert.ToInt32(r["SifarnikTrasaID"]));
            var changed = new HashSet<int>();
            using (var c = Db.Open())
            using (var tx = c.BeginTransaction())
            {
                try
                {
                    Db.Lock(c, tx, "SifarnikTrasaGodina");
                    foreach (var p in plans)
                    {
                        if (p.Godina <= 0) continue;
                        int id = links[p.ID];
                        var rows = Db.Table(c, tx, @"SELECT t.* FROM dbo.Trase t WITH(UPDLOCK,HOLDLOCK)
WHERE t.ID=@id AND LTRIM(RTRIM(t.Voz))=@voz AND LTRIM(RTRIM(t.Godina))=@year
AND EXISTS(SELECT 1 FROM dbo.OperativniPlanVozovi v
 WHERE v.ID=@prevoz AND v.CompletionRecorded=1 AND v.SifarnikTrasaID=t.ID AND v.VerzijaReda=@rv)",
                            "@id", id, "@voz", p.Broj, "@year", p.Godina.ToString("0000", System.Globalization.CultureInfo.InvariantCulture),
                            "@prevoz", p.ID, "@rv", p.Verzija);
                        if (rows.Rows.Count == 1 && DopuniPostojecu(c, tx, rows.Rows[0], p.Excel))
                            changed.Add(id);
                    }
                    tx.Commit();
                    return changed.Count;
                }
                catch { try { tx.Rollback(); } catch { } throw; }
            }
        }
    }
}
