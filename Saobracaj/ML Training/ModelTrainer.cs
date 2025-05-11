using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace Saobracaj.ML_Training
{
    public static class ModelTrainer
    {
        private static readonly string ModelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model", "arrival_model.zip");
        private static PredictionEngine<TrainArrivalData, TrainArrivalPrediction> _engine;

        public static void Train(List<TrainArrivalData> data)
        {
            var context = new MLContext();
            var trainingData = context.Data.LoadFromEnumerable(data);

            var pipeline = context.Transforms.Concatenate("Features", new[] { "Trasa", "Stanica", "StartHour", "Redosled" })
                .AppendCacheCheckpoint(context)
                .Append(context.Regression.Trainers.FastTree(
                    labelColumnName: "ArrivalTimeSeconds",
                    numberOfLeaves: 5,
                    numberOfTrees: 10,
                    minimumExampleCountPerLeaf: 2));

            var model = pipeline.Fit(trainingData);

            var folder = Path.GetDirectoryName(ModelPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            context.Model.Save(model, trainingData.Schema, ModelPath);

            // Rebuild cached engine
            _engine = context.Model.CreatePredictionEngine<TrainArrivalData, TrainArrivalPrediction>(model);
        }

        private static void EnsureEngine()
        {
            if (_engine == null)
            {
                var context = new MLContext();
                var model = context.Model.Load(ModelPath, out _);
                _engine = context.Model.CreatePredictionEngine<TrainArrivalData, TrainArrivalPrediction>(model);
            }
        }

        public static float Predict(float trasa, float stanica, float startHour, float redosled)
        {
            EnsureEngine();

            return _engine.Predict(new TrainArrivalData
            {
                Trasa = trasa,
                Stanica = stanica,
                StartHour = startHour,
                Redosled = redosled
            }).PredictedSeconds;
        }
    }
}
