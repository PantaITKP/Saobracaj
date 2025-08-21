using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.MLProd
{
    public sealed class EtaService
    {
        private readonly string _conn;
        private readonly MLContext _ml;

        // 1) model vremena segmenta (radi na SegmentData)
        private readonly PredictionEngine<SegmentData, SegmentPrediction> _pe;

        // 2) stop modeli: obavezno koristimo input tipove sa DUMMY label kolonom
        private readonly PredictionEngine<StopFeatures, StopBinOut> _peStop;
        private readonly PredictionEngine<StopFeaturesReason, StopReasonOut> _peReason;
        private readonly PredictionEngine<StopFeaturesWait, StopWaitOut> _peWait;

        public EtaService(string connString, string modelPathTime)
        {
            _conn = connString;
            _ml = new MLContext();

            if (!File.Exists(modelPathTime))
                throw new FileNotFoundException("Model (vreme) nije pronađen", modelPathTime);

            // Učitaj model vremena
            var timeModel = _ml.Model.Load(modelPathTime, out _);
            _pe = _ml.Model.CreatePredictionEngine<SegmentData, SegmentPrediction>(timeModel);

            // Pokušaćemo da učitamo stop modele, ali nećemo rušiti ako neki ne postoji
            var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_bin.zip");
            var reasonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_reason.zip");
            var waitPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stop_wait.zip");

            if (File.Exists(binPath))
                _peStop = _ml.Model.CreatePredictionEngine<StopFeatures, StopBinOut>(_ml.Model.Load(binPath, out _));
            if (File.Exists(reasonPath))
                _peReason = _ml.Model.CreatePredictionEngine<StopFeaturesReason, StopReasonOut>(_ml.Model.Load(reasonPath, out _));
            if (File.Exists(waitPath))
                _peWait = _ml.Model.CreatePredictionEngine<StopFeaturesWait, StopWaitOut>(_ml.Model.Load(waitPath, out _));
        }

        public DateTime GetStartTimeForRN(int rn)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();

                // Najraniji realni događaj za RN & prvu trasu (RB=1)
                using (var cmd = new SqlCommand(@"
                    SELECT MIN(pl.Datum)
                    FROM PutniList pl
                    JOIN RadniNalogTrase r ON r.IDRadnogNaloga=pl.BrojRN AND r.IDTrase=pl.IDTrase
                    WHERE pl.BrojRN=@rn AND r.RB=1 AND pl.Datum > '19000101'", conn))
                {
                    cmd.Parameters.AddWithValue("@rn", rn);
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return Convert.ToDateTime(o);
                }

                // Fallback: planirano vreme RB=1
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 DatumPolaskaReal
                    FROM RadniNalogTrase
                    WHERE IDRadnogNaloga=@rn AND RB=1 AND DatumPolaskaReal > '19000101'
                    ORDER BY RB", conn))
                {
                    cmd.Parameters.AddWithValue("@rn", rn);
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) return Convert.ToDateTime(o);
                }
            }
            return DateTime.Now;
        }

        public List<(SegmentData seg, int stanicaToId, int rbTrase)> BuildSegmentsForRN(int rn, DateTime tripStart)
        {
            var list = new List<(SegmentData, int, int)>();

            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();
                using (var da = new SqlDataAdapter("SELECT * FROM dbo.fn_Segments_For_RN(@rn) ORDER BY RBTrase, RBFrom", conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@rn", rn);
                    var dt = new DataTable();
                    da.Fill(dt);

                    int isoDow = ((int)tripStart.DayOfWeek + 6) % 7 + 1; // Mon=1..Sun=7

                    foreach (DataRow r in dt.Rows)
                    {
                        var sd = new SegmentData
                        {
                            IDTrase = Convert.ToSingle(r["IDTrase"]),
                            StanicaFromID = Convert.ToSingle(r["StanicaFromID"]),
                            StanicaToID = Convert.ToSingle(r["StanicaToID"]),
                            PlaniranaMasa = Convert.ToSingle(r["PlaniranaMasa"]),
                            MasaLokomotive = Convert.ToSingle(r["MasaLokomotive"]),
                            MasaVoza = Convert.ToSingle(r["MasaVoza"]),
                            BrutoMasa = Convert.ToSingle(r["BrutoMasa"]),
                            Rezi = Convert.ToSingle(r["Rezi"]),
                            StartHour = tripStart.Hour,
                            DayOfWeek = isoDow
                        };
                        list.Add((sd, Convert.ToInt32(r["StanicaToID"]), Convert.ToInt32(r["RBTrase"])));
                    }
                }
            }
            return list;
        }

        private (double median, int samples, bool byTrasa) TryGetABStat(float idTrase, int a, int b)
        {
            using (var conn = new SqlConnection(_conn))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"
                    SELECT Samples, MedianMin FROM dbo.vw_SegmentStats_AB_Trasa
                    WHERE IDTrase=@t AND StanicaFromID=@a AND StanicaToID=@b", conn))
                {
                    cmd.Parameters.AddWithValue("@t", (int)idTrase);
                    cmd.Parameters.AddWithValue("@a", a);
                    cmd.Parameters.AddWithValue("@b", b);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read()) return (Convert.ToDouble(rd["MedianMin"]), Convert.ToInt32(rd["Samples"]), true);
                    }
                }

                using (var cmd = new SqlCommand(@"
                    SELECT Samples, MedianMin FROM dbo.vw_SegmentStats_AB
                    WHERE StanicaFromID=@a AND StanicaToID=@b", conn))
                {
                    cmd.Parameters.AddWithValue("@a", a);
                    cmd.Parameters.AddWithValue("@b", b);
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read()) return (Convert.ToDouble(rd["MedianMin"]), Convert.ToInt32(rd["Samples"]), false);
                    }
                }
            }
            return (double.NaN, 0, false);
        }

        public double PredictSegmentMinutes(SegmentData seg)
        {
            var (med, n, trasa) = TryGetABStat(seg.IDTrase, (int)seg.StanicaFromID, (int)seg.StanicaToID);

            double ml = _pe.Predict(seg).Score;
            if (ml < 1) ml = 1;

            if (!double.IsNaN(med))
            {
                double K = trasa ? 30.0 : 50.0;   // više verujemo medianu kad je specifično po trasi
                double w = Math.Min(1.0, n / K);  // 0..1
                return w * med + (1 - w) * ml;
            }
            return ml;
        }

        // Konverzije u inpute za stop modele
        private static StopFeatures ToStopFeatures(SegmentData s) => new StopFeatures
        {
            IDTrase = s.IDTrase,
            StanicaFromID = s.StanicaFromID,
            StanicaToID = s.StanicaToID,
            StartHour = s.StartHour,
            DayOfWeek = s.DayOfWeek,
            PlaniranaMasa = s.PlaniranaMasa,
            MasaLokomotive = s.MasaLokomotive,
            MasaVoza = s.MasaVoza,
            BrutoMasa = s.BrutoMasa,
            Rezi = s.Rezi
        };

        private static StopFeaturesReason ToStopFeaturesReason(SegmentData s)
        {
            var f = new StopFeaturesReason();
            // kopiraj zajedničke osobine:
            f.IDTrase = s.IDTrase; f.StanicaFromID = s.StanicaFromID; f.StanicaToID = s.StanicaToID;
            f.StartHour = s.StartHour; f.DayOfWeek = s.DayOfWeek;
            f.PlaniranaMasa = s.PlaniranaMasa; f.MasaLokomotive = s.MasaLokomotive;
            f.MasaVoza = s.MasaVoza; f.BrutoMasa = s.BrutoMasa; f.Rezi = s.Rezi;
            // label nije potreban na predikciji, ali polje mora postojati u šemi:
            f.ReasonLabel = string.Empty;
            return f;
        }

        private static StopFeaturesWait ToStopFeaturesWait(SegmentData s)
        {
            var f = new StopFeaturesWait();
            f.IDTrase = s.IDTrase; f.StanicaFromID = s.StanicaFromID; f.StanicaToID = s.StanicaToID;
            f.StartHour = s.StartHour; f.DayOfWeek = s.DayOfWeek;
            f.PlaniranaMasa = s.PlaniranaMasa; f.MasaLokomotive = s.MasaLokomotive;
            f.MasaVoza = s.MasaVoza; f.BrutoMasa = s.BrutoMasa; f.Rezi = s.Rezi;
            f.WaitLabel = 0f; // nije potreban za predikciju
            return f;
        }

        private string ResolveReasonDescription(int id)
        {
            if (id <= 0) return null;
            using (var cn = new SqlConnection(_conn))
            {
                cn.Open();
                using (var cmd = new SqlCommand("SELECT Opis FROM Razlozi WHERE ID=@id", cn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    var o = cmd.ExecuteScalar();
                    return o == null ? ("R" + id.ToString()) : o.ToString();
                }
            }
        }

        public (List<(int rbTrase, int stanicaTo, DateTime eta, float pStop, string reason, float waitMin)> chain,
        DateTime start, DateTime final)
    PredictChainForRN_WithStops(int rn, float stopThreshold = 0.5f)
        {
            var start = GetStartTimeForRN(rn);
            var segs = BuildSegmentsForRN(rn, start);

            var t = start;
            var res = new List<(int, int, DateTime, float, string, float)>();

            foreach (var (seg, stTo, rb) in segs)
            {
                float p = 0f; string reason = ""; float waitPred = 0f;

                // 1) Proračuni za stop/razlog/čekanje – uvek pokušaj
                if (_peStop != null)
                {
                    var pOut = _peStop.Predict(ToStopFeatures(seg));
                    p = pOut.Probability;
                }
                if (_peReason != null)
                {
                    var rOut = _peReason.Predict(ToStopFeaturesReason(seg));
                    if (!string.IsNullOrEmpty(rOut.Reason) && rOut.Reason.StartsWith("R") &&
                        int.TryParse(rOut.Reason.Substring(1), out int rid))
                        reason = ResolveReasonDescription(rid);
                }
                if (_peWait != null)
                {
                    var wOut = _peWait.Predict(ToStopFeaturesWait(seg));
                    waitPred = Math.Max(0f, wOut.Score);
                }

                // 2) U ETA dodaj čekanje samo ako je pStop iznad praga
                if (p >= stopThreshold && waitPred > 0)
                    t = t.AddMinutes(waitPred);

                // 3) A->B zavisno vreme
                var mins = PredictSegmentMinutes(seg);
                t = t.AddMinutes(Math.Max(1.0, mins));

                // 4) *** OVDE BIRAŠ ŠTA PRIKAZUJEŠ U GRIDU ***
                //    a) Ako želiš da uvek vidiš očekivano čekanje (i kada je p<thr):
                //res.Add((rb, stTo, t, p, reason, waitPred));
                //    b) Ako želiš čekanje samo kada je p>=thr (kao dosad):
                res.Add((rb, stTo, t, p, reason, (p >= stopThreshold ? waitPred : 0f)));
            }
            return (res, start, t);
        }


        // Stara varijanta bez stopova – koristi novu sa velikim pragom
        public (List<(int rbTrase, int stanicaTo, DateTime eta)> chain, DateTime start, DateTime final)
            PredictChainForRN(int rn)
        {
            var ext = PredictChainForRN_WithStops(rn, 1.1f);
            var basic = new List<(int, int, DateTime)>();
            foreach (var c in ext.chain) basic.Add((c.rbTrase, c.stanicaTo, c.eta));
            return (basic, ext.start, ext.final);
        }
    }
}

