using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.MLProd
{
    public sealed class SegmentData
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
        public float SegmentTimeMinutes { get; set; }
    }

    public sealed class SegmentPrediction { public float Score { get; set; } }
}
