using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Saobracaj.MLProd
{
    internal interface ICommonFeat
    {
        float IDTrase { get; set; }
        float StanicaFromID { get; set; }
        float StanicaToID { get; set; }
        float StartHour { get; set; }
        float DayOfWeek { get; set; }
        float PlaniranaMasa { get; set; }
        float MasaLokomotive { get; set; }
        float MasaVoza { get; set; }
        float BrutoMasa { get; set; }
        float Rezi { get; set; }
    }

    internal sealed class StopBinRow : ICommonFeat
    {
        public float IDTrase { get; set; }
        public float StanicaFromID { get; set; }
        public float StanicaToID { get; set; }
        public float StartHour { get; set; }
        public float DayOfWeek { get; set; }
        public float PlaniranaMasa { get; set; }
        public float MasaLokomotive { get; set; }
        public float MasaVoza { get; set; }
        public float BrutoMasa { get; set; }
        public float Rezi { get; set; }
        public bool Label { get; set; } // stop/no-stop
    }

    internal sealed class StopReasonRow : ICommonFeat
    {
        public float IDTrase { get; set; }
        public float StanicaFromID { get; set; }
        public float StanicaToID { get; set; }
        public float StartHour { get; set; }
        public float DayOfWeek { get; set; }
        public float PlaniranaMasa { get; set; }
        public float MasaLokomotive { get; set; }
        public float MasaVoza { get; set; }
        public float BrutoMasa { get; set; }
        public float Rezi { get; set; }
        public string ReasonLabel { get; set; } // “R{id}”
    }

    internal sealed class StopWaitRow : ICommonFeat
    {
        public float IDTrase { get; set; }
        public float StanicaFromID { get; set; }
        public float StanicaToID { get; set; }
        public float StartHour { get; set; }
        public float DayOfWeek { get; set; }
        public float PlaniranaMasa { get; set; }
        public float MasaLokomotive { get; set; }
        public float MasaVoza { get; set; }
        public float BrutoMasa { get; set; }
        public float Rezi { get; set; }
        public float WaitLabel { get; set; } // minute
    }

    public static class StopTrainer
    {
        private static string BaseSelect(string labelProjection) => @"
SELECT
  CAST(s.IDTrase AS REAL)                       AS IDTrase,
  CAST(s.StanicaFromID AS REAL)                 AS StanicaFromID,
  CAST(s.StanicaToID AS REAL)                   AS StanicaToID,
  CAST(DATEPART(HOUR, s.StartTimeAtA) AS REAL)  AS StartHour,
  CAST(((DATEPART(WEEKDAY, s.StartTimeAtA)+5)%7)+1 AS REAL) AS DayOfWeek,
  CAST(ISNULL(r.PlaniranaMasa,0) AS REAL)   AS PlaniranaMasa,
  CAST(ISNULL(r.MasaLokomotive,0) AS REAL)  AS MasaLokomotive,
  CAST(ISNULL(r.MasaVoza,0) AS REAL)        AS MasaVoza,
  CAST(ISNULL(r.BrutoMasa,0) AS REAL)       AS BrutoMasa,
  CAST(ISNULL(r.Rezi,0) AS REAL)            AS Rezi,
" + labelProjection + @"
FROM dbo.vw_SegmentStops_Labels s
LEFT JOIN RadniNalogTrase r
  ON r.IDRadnogNaloga = s.BrojRN AND r.IDTrase = s.IDTrase";

        private static (long total, long pos, long neg) CountBinLabels(MLContext ml, IDataView data)
        {
            long pos = 0, neg = 0, total = 0;
            foreach (var row in ml.Data.CreateEnumerable<StopBinRow>(data, reuseRowObject: true))
            {
                total++;
                if (row.Label) pos++; else neg++;
            }
            return (total, pos, neg);
        }

        private static (IDataView train, IDataView test) StratifiedSplitManual<T>(
            MLContext ml, IDataView data, double testFraction, int seed = 13) where T : class, new()
        {
            var all = ml.Data.CreateEnumerable<T>(data, reuseRowObject: false).ToList();
            var rnd = new Random(seed);

            if (typeof(T) == typeof(StopBinRow))
            {
                var pos = all.Cast<StopBinRow>().Where(z => z.Label).OrderBy(_ => rnd.Next()).ToList();
                var neg = all.Cast<StopBinRow>().Where(z => !z.Label).OrderBy(_ => rnd.Next()).ToList();

                int testPos = (int)Math.Round(testFraction * pos.Count);
                int testNeg = (int)Math.Round(testFraction * neg.Count);
                testPos = Math.Max(0, Math.Min(testPos, pos.Count));
                testNeg = Math.Max(0, Math.Min(testNeg, neg.Count));

                var test = new List<StopBinRow>();
                var train = new List<StopBinRow>();
                test.AddRange(pos.Take(testPos)); test.AddRange(neg.Take(testNeg));
                train.AddRange(pos.Skip(testPos)); train.AddRange(neg.Skip(testNeg));

                return (ml.Data.LoadFromEnumerable(train as IEnumerable<T>),
                        ml.Data.LoadFromEnumerable(test as IEnumerable<T>));
            }
            else
            {
                var shuffled = all.OrderBy(_ => rnd.Next()).ToList();
                int nTest = (int)Math.Round(testFraction * shuffled.Count);
                nTest = Math.Max(0, Math.Min(nTest, shuffled.Count));
                var test = shuffled.Take(nTest).ToList();
                var train = shuffled.Skip(nTest).ToList();
                return (ml.Data.LoadFromEnumerable(train),
                        ml.Data.LoadFromEnumerable(test));
            }
        }

        // HELPER: čitanje “razloga” u memoriju
        private static List<StopReasonRow> FetchReasonRows(string conn)
        {
            var list = new List<StopReasonRow>();
            string sql = BaseSelect(
                "CASE WHEN s.StopOccurred=1 AND s.StopReasonID IS NOT NULL THEN 'R'+CAST(s.StopReasonID AS NVARCHAR(8)) ELSE NULL END AS ReasonLabel")
                + " WHERE s.StopOccurred=1 AND s.StopReasonID IS NOT NULL OPTION (FAST 1, RECOMPILE)";

            using (var cn = new SqlConnection(conn))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandTimeout = 60;
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var row = new StopReasonRow
                            {
                                IDTrase = Convert.ToSingle(rd["IDTrase"]),
                                StanicaFromID = Convert.ToSingle(rd["StanicaFromID"]),
                                StanicaToID = Convert.ToSingle(rd["StanicaToID"]),
                                StartHour = Convert.ToSingle(rd["StartHour"]),
                                DayOfWeek = Convert.ToSingle(rd["DayOfWeek"]),
                                PlaniranaMasa = Convert.ToSingle(rd["PlaniranaMasa"]),
                                MasaLokomotive = Convert.ToSingle(rd["MasaLokomotive"]),
                                MasaVoza = Convert.ToSingle(rd["MasaVoza"]),
                                BrutoMasa = Convert.ToSingle(rd["BrutoMasa"]),
                                Rezi = Convert.ToSingle(rd["Rezi"]),
                                ReasonLabel = rd["ReasonLabel"] as string
                            };
                            if (!string.IsNullOrEmpty(row.ReasonLabel))
                                list.Add(row);
                        }
                    }
                }
            }
            return list;
        }

        // HELPER: čitanje “čekanja” u memoriju
        private static List<StopWaitRow> FetchWaitRows(string conn)
        {
            var list = new List<StopWaitRow>();
            string sql = BaseSelect("CAST(ISNULL(s.StopWaitMinutes,0) AS REAL) AS WaitLabel")
                + " WHERE s.StopOccurred=1 AND s.StopWaitMinutes IS NOT NULL OPTION (FAST 1, RECOMPILE)";

            using (var cn = new SqlConnection(conn))
            {
                cn.Open();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.CommandTimeout = 60;
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var row = new StopWaitRow
                            {
                                IDTrase = Convert.ToSingle(rd["IDTrase"]),
                                StanicaFromID = Convert.ToSingle(rd["StanicaFromID"]),
                                StanicaToID = Convert.ToSingle(rd["StanicaToID"]),
                                StartHour = Convert.ToSingle(rd["StartHour"]),
                                DayOfWeek = Convert.ToSingle(rd["DayOfWeek"]),
                                PlaniranaMasa = Convert.ToSingle(rd["PlaniranaMasa"]),
                                MasaLokomotive = Convert.ToSingle(rd["MasaLokomotive"]),
                                MasaVoza = Convert.ToSingle(rd["MasaVoza"]),
                                BrutoMasa = Convert.ToSingle(rd["BrutoMasa"]),
                                Rezi = Convert.ToSingle(rd["Rezi"]),
                                WaitLabel = Convert.ToSingle(rd["WaitLabel"])
                            };
                            list.Add(row);
                        }
                    }
                }
            }
            return list;
        }

        public static async Task<(string bin, string reason, string wait)> TrainAllAsync(
            string conn, string outBin, string outReason, string outWait,
            IProgress<TrainProgress> prog = null, CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                var ml = new MLContext(seed: 13);
                void Report(string s, int p, string m = null)
                    => prog?.Report(new TrainProgress { Stage = s, Percent = p, Message = m });

                // JEDAN niz imena feature-a za sva tri modela
                var featCols = new[]
                {
                    nameof(StopBinRow.IDTrase),
                    nameof(StopBinRow.StanicaFromID),
                    nameof(StopBinRow.StanicaToID),
                    nameof(StopBinRow.StartHour),
                    nameof(StopBinRow.DayOfWeek),
                    nameof(StopBinRow.PlaniranaMasa),
                    nameof(StopBinRow.MasaLokomotive),
                    nameof(StopBinRow.MasaVoza),
                    nameof(StopBinRow.BrutoMasa),
                    nameof(StopBinRow.Rezi)
                };

                // 1) BINARNO
                Report("Učitavanje (binarno)...", 5);
                var loadB = ml.Data.CreateDatabaseLoader<StopBinRow>();
                var sqlB = BaseSelect("CAST(CASE WHEN s.StopOccurred=1 THEN 1 ELSE 0 END AS BIT) AS Label");
                var dataB = loadB.Load(new DatabaseSource(System.Data.SqlClient.SqlClientFactory.Instance, conn, sqlB));

                var (totB, posB, negB) = CountBinLabels(ml, dataB);
                if (totB == 0 || posB == 0 || negB == 0)
                {
                    TrainLogger.Warn($"Stop bin: nedovoljno podataka (total={totB}, pos={posB}, neg={negB}) — preskačem.");
                    try { System.IO.File.Delete(outBin); } catch { }
                }
                else
                {
                    var splitB = StratifiedSplitManual<StopBinRow>(ml, dataB, 0.2, 13);

                    var binPipe = ml.Transforms.Concatenate("Features", featCols)
                        .Append(ml.Transforms.NormalizeMinMax("Features"))
                        .Append(ml.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
                        {
                            NumberOfTrees = 300,
                            NumberOfLeaves = 64,
                            MinimumExampleCountPerLeaf = 20,
                            FeatureFraction = 1.0f
                        }));

                    Report("Trening (binarno)...", 15);
                    var binModel = binPipe.Fit(splitB.train);

                    // eval samo ako test ima obe klase
                    var (tTot, tPos, tNeg) = CountBinLabels(ml, splitB.test);
                    string binTxt;
                    if (tPos == 0 || tNeg == 0)
                    {
                        binTxt = $"AUC=N/A (test bez obe klase) | Train pos/neg=({posB}/{negB})";
                    }
                    else
                    {
                        var binEval = ml.BinaryClassification.Evaluate(binModel.Transform(splitB.test));
                        binTxt = $"AUC={binEval.AreaUnderRocCurve:F3}, ACC={binEval.Accuracy:P1}";
                    }

                    ml.Model.Save(binModel, splitB.train.Schema, outBin);
                    Report("Binarno OK", 25, binTxt);
                    TrainLogger.Info("Stop bin metrics: " + binTxt);
                }

                // 2) RAZLOG
                Report("Učitavanje (razlog)...", 30);
                var rowsR = FetchReasonRows(conn);
                if (rowsR.Count == 0)
                {
                    TrainLogger.Warn("Stop reason: nema podataka — preskačem.");
                    try { System.IO.File.Delete(outReason); } catch { }
                }
                else
                {
                    var dataR = ml.Data.LoadFromEnumerable(rowsR);
                    var splitR = ml.Data.TrainTestSplit(dataR, 0.2);

                    var reasonPipe = ml.Transforms.Concatenate("Features", featCols)
                        .Append(ml.Transforms.NormalizeMinMax("Features"))
                        .Append(ml.Transforms.Conversion.MapValueToKey("Label", "ReasonLabel"))
                        .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                        .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedReason", "PredictedLabel"));

                    var reasonModel = reasonPipe.Fit(splitR.TrainSet);
                    ml.Model.Save(reasonModel, splitR.TrainSet.Schema, outReason);
                    Report("Razlog OK", 45);
                }

                // 3) ČEKANJE
                Report("Učitavanje (čekanje)...", 50);
                var rowsW = FetchWaitRows(conn);
                if (rowsW.Count == 0)
                {
                    TrainLogger.Warn("Stop wait: nema podataka — preskačem.");
                    try { System.IO.File.Delete(outWait); } catch { }
                }
                else
                {
                    var dataW = ml.Data.LoadFromEnumerable(rowsW);
                    var splitW = ml.Data.TrainTestSplit(dataW, 0.2);

                    var waitPipe = ml.Transforms.CopyColumns("Label", "WaitLabel")
                        .Append(ml.Transforms.Concatenate("Features", featCols))
                        .Append(ml.Transforms.NormalizeMinMax("Features"))
                        .Append(ml.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                        {
                            NumberOfTrees = 400,
                            NumberOfLeaves = 64,
                            MinimumExampleCountPerLeaf = 20,
                            LearningRate = 0.2f,
                            FeatureFraction = 1.0f
                        }));

                    var waitModel = waitPipe.Fit(splitW.TrainSet);

                    string wTxt;
                    var testHasRows = ml.Data.CreateEnumerable<StopWaitRow>(splitW.TestSet, reuseRowObject: true).Any();
                    if (testHasRows)
                    {
                        var wEval = ml.Regression.Evaluate(waitModel.Transform(splitW.TestSet), labelColumnName: "Label", scoreColumnName: "Score");
                        wTxt = $"RMSE_wait={wEval.RootMeanSquaredError:F2}, R²={wEval.RSquared:F3}";
                    }
                    else
                    {
                        wTxt = "RMSE_wait=N/A (prazan test skup)";
                    }

                    ml.Model.Save(waitModel, splitW.TrainSet.Schema, outWait);
                    Report("Čekanje OK", 70, wTxt);
                    TrainLogger.Info("Stop wait metrics: " + wTxt);
                }

                Report("Gotovo", 100);
                return ("OK", "OK", "OK");
            }, ct);
        }
    }
}

