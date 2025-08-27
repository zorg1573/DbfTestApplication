using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbfTest.MODEL
{
    public class SerialScanConfig
    {
        public string COMPort { get; set; }
        public string BaudRate { get; set; }
        public string DataBits { get; set; }
        public string StopBits { get; set; }
        public string Parity { get; set; }
        public string Handshake { get; set; }
    }
}
