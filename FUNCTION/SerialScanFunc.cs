using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json; // .NET Core / .NET 5+ 推荐用System.Text.Json
using System.Windows.Forms;
using TestApp.MODEL;
using System.IO.Ports;

namespace TestApp.FUNCTION
{
    public class SerialScanFunc
    {
        public void SaveConfig(SerialScanConfig config)
        {
            try
            {
                string configPath = Path.Combine(Application.StartupPath, "SerialScanConfig.json");

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                MessageBox.Show("保存成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存配置失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public SerialScanConfig LoadConfig()
        {
            try
            {
                if (!File.Exists("SerialScanConfig.json"))
                    return null;

                string json = File.ReadAllText("SerialScanConfig.json");
                return JsonSerializer.Deserialize<SerialScanConfig>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载配置失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public List<DeviceInfo> ScanSerialPorts()
        {
            var config = LoadConfig();
            var found = new List<DeviceInfo>();
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
            {
                found.Add(new DeviceInfo
                {
                    DeviceName = $"串口设备（{port}）",
                    Address = port // 或 format like "COM3"
                });
            }

            return found;
        }


    }
}
