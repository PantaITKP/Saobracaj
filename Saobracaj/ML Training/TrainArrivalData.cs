using Syncfusion.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Saobracaj.ML_Training
{
    public class TrainArrivalData
    {
        public float Trasa { get; set; }
        public float Stanica { get; set; }
        public float StartHour { get; set; }
        public float Redosled { get; set; } // ➕ station order
        public float ArrivalTimeSeconds { get; set; } // Label
    }

}
