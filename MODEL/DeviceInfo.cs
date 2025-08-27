using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbfTest.MODEL
{
    public class DeviceInfo
    {
        public string DeviceName { get; set; }
        public string Address { get; set; } // 如 IP、VISA 地址、COM3 等
        public ConnectionType Type { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>(); // 如端口号、波特率等
    }

    public enum ConnectionType
    {
        LAN,
        USB,
        Serial,
        GPIB,
        Bluetooth,
        CAN,
        PXI
    }

}
