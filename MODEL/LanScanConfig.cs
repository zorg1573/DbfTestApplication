using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbfTest.MODEL
{
    public class LanScanConfig
    {
        public string StartIP { get; set; }
        public string EndIP { get; set; }
        public int Port { get; set; }
        public int Timeout { get; set; }
        public int MaxConcurrent { get; set; }
        public bool QueryIdn { get; set; }
        public bool AutoSave { get; set; }
    }

}
