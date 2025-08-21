using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Syncfusion.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Saobracaj.MLProd
{
    public sealed class TrainProgress
    {
        public string Stage { get; set; } = "";
        public int Percent { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
    }

    public static class GlobalTrainer
    {
        // USKLAĐENO sa CAST(... AS REAL) u SQL-u -> float (Single) u .NET
        private sealed class SegmentRow
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
            public float SegmentTimeMinutes { get; set; } // <- label
        }

        public static async Task<string> TrainAndSaveAsync(
            string conn, string modelPath,
            IProgress<TrainProgress> progress = null,
            CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                var ml = new MLContext(seed: 7);
                void Report(string stage, int pct, string msg = null)
                    => progress?.Report(new TrainProgress { Stage = stage, Percent = pct, Message = msg });

                Report("Priprema upita...", 5);

                var loader = ml.Data.CreateDatabaseLoader<SegmentRow>();
                string query = @"
SELECT
    CAST(g.IDTrase AS REAL)              AS IDTrase,
    CAST(g.StanicaFromID AS REAL)        AS StanicaFromID,
    CAST(g.StanicaToID AS REAL)          AS StanicaToID,
    CAST(g.StartHour AS REAL)            AS StartHour,
    CAST(g.DayOfWeek AS REAL)            AS DayOfWeek,
    CAST(ISNULL(r.PlaniranaMasa,0) AS REAL)   AS PlaniranaMasa,
    CAST(ISNULL(r.MasaLokomotive,0) AS REAL)  AS MasaLokomotive,
    CAST(ISNULL(r.MasaVoza,0) AS REAL)        AS MasaVoza,
    CAST(ISNULL(r.BrutoMasa,0) AS REAL)       AS BrutoMasa,
    CAST(ISNULL(r.Rezi,0) AS REAL)            AS Rezi,
    CAST(g.SegmentTimeMinutes AS REAL)        AS SegmentTimeMinutes
FROM dbo.vw_SegmentPairs_Global g
LEFT JOIN RadniNalogTrase r
  ON r.IDRadnogNaloga = g.BrojRN AND r.IDTrase = g.IDTrase
WHERE g.SegmentTimeMinutes BETWEEN 1 AND 6000;";

                var source = new DatabaseSource(System.Data.SqlClient.SqlClientFactory.Instance, conn, query);

                Report("Učitavanje podataka...", 10);
                var data = loader.Load(source);

                Report("Podela train/test...", 15);
                var split = ml.Data.TrainTestSplit(data, 0.2);

                var featureCols = new[]
                {
                    nameof(SegmentRow.IDTrase),
                    nameof(SegmentRow.StanicaFromID),
                    nameof(SegmentRow.StanicaToID),
                    nameof(SegmentRow.StartHour),
                    nameof(SegmentRow.DayOfWeek),
                    nameof(SegmentRow.PlaniranaMasa),
                    nameof(SegmentRow.MasaLokomotive),
                    nameof(SegmentRow.MasaVoza),
                    nameof(SegmentRow.BrutoMasa),
                    nameof(SegmentRow.Rezi)
                };

                var ftOptions = new FastTreeRegressionTrainer.Options
                {
                    NumberOfTrees = 600,
                    NumberOfLeaves = 64,
                    MinimumExampleCountPerLeaf = 20,
                    LearningRate = 0.2f,
                    FeatureFraction = 1.0f,
                    // VAŽNO: reci treneru koja je label kolona (ili koristi CopyColumns ispod)
                    LabelColumnName = "Label",
                    FeatureColumnName = "Features"
                };

                // VAŽNO: kopiramo SegmentTimeMinutes u standardnu "Label"
                var pipeline = ml.Transforms.CopyColumns("Label", nameof(SegmentRow.SegmentTimeMinutes))
                              .Append(ml.Transforms.Concatenate("Features", featureCols))
                              .Append(ml.Transforms.NormalizeMinMax("Features"))
                              .Append(ml.Regression.Trainers.FastTree(ftOptions));

                Report("Treniranje (FastTree)...", 35);
                var model = pipeline.Fit(split.TrainSet);

                Report("Evaluacija...", 85);
                var scored = model.Transform(split.TestSet);

                // Evaluiramo vs "Label" (jer smo je napravili gore)
                var metrics = ml.Regression.Evaluate(scored, labelColumnName: "Label", scoreColumnName: "Score");
                string metricsText = $"RMSE={metrics.RootMeanSquaredError:F2}, R2={metrics.RSquared:F3}";

                Report("Snimanje modela...", 95);
                ml.Model.Save(model, split.TrainSet.Schema, modelPath);

                Report("Gotovo", 100, metricsText);
                return metricsText;
            }, ct);
        }
    }
}
