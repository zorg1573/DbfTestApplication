using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp.MODEL
{
    public class SpectrumData
    {
        public int Index { get; set; }
        public string Timestamp { get; set; }
        public double CenterFrequencyGHz { get; set; }
        public double StartFrequencyMHz { get; set; }
        public double StopFrequencyGHz { get; set; }
        public double SpanGHz { get; set; }
        public double RBW { get; set; }
        public double VBW { get; set; }
        public double SweepTime { get; set; }
        public string TriggerSource { get; set; }
        public double PeakFrequencyHz { get; set; }
        public double PeakPowerdBm { get; set; }
    }

}
