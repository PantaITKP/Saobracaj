using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace Saobracaj.ML_Training
{
    public class TrainArrivalPrediction
    {
        [ColumnName("Score")]
        public float PredictedSeconds { get; set; }
    }
}
