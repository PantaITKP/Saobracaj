using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.MLProd
{
    /// <summary>
    /// Ulazne osobine za stop modele (binarni/razlog/čekanje).
    /// Imena polja moraju odgovarati onima koja su korišćena u treningu.
    /// </summary>
    public class StopFeatures
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
    }

    /// <summary> Izlaz za binarni model (stop/no-stop). </summary>
    public sealed class StopBinOut
    {
        [ColumnName("PredictedLabel")]
        public bool Predicted { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }

    /// <summary> Izlaz za multiclass model (razlog). </summary>
    public sealed class StopReasonOut
    {
        // U treningu smo MapKeyToValue stavili u "PredictedReason"
        [ColumnName("PredictedReason")]
        public string Reason { get; set; }
    }

    /// <summary> Izlaz za regresioni model (čekanje u minutima). </summary>
    public sealed class StopWaitOut
    {
        public float Score { get; set; }
    }

    public sealed class StopFeaturesReason : StopFeatures
    {
        public string ReasonLabel { get; set; }  // može biti prazan pri predikciji
    }

    // Ulaz za "wait" model – isti feature-i + WaitLabel (nije potreban pri predikciji)
    public sealed class StopFeaturesWait : StopFeatures
    {
        public float WaitLabel { get; set; }     // može biti 0 pri predikciji
    }
}