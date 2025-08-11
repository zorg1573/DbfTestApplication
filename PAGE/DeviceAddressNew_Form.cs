using Ivi.Visa;
using MetroFramework.Forms;
using NationalInstruments.Visa;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestApp.PAGE
{
    public partial class DeviceAddressNew_Form : MetroForm
    {
        private Main_New mainForm;
        public DeviceAddressNew_Form(Main_New mainForm)
        {
            InitializeComponent();
            this.Load += DeviceAddressNew_Form_Load;
            this.mainForm = mainForm;
        }
        private void DeviceAddressNew_Form_Load(object sender, EventArgs e)
        {
            LoadFromJson();
        }
        private void SaveToJson()
        {
            var data = new Dictionary<string, object>();

            foreach (Control ctrl in tableLayoutPanel1.Controls)
            {
                if (ctrl is TextBox textBox)
                {
                    data[textBox.Name] = textBox.Text;
                }
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("DeviceAddressNew.json", json);
        }

        private void LoadFromJson()
        {
            string filePath = "DeviceAddressNew.json";
            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (data == null)
                return;

            foreach (Control ctrl in tableLayoutPanel1.Controls)
            {
                if (data.TryGetValue(ctrl.Name, out object value))
                {
                    if (ctrl is TextBox textBox)
                    {
                        textBox.Text = value.ToString();
                    }
                }
            }
        }
        private Task<string> TryConnectAndGetIdnAsync(string visaAddress)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var resourceManager = new ResourceManager())
                    using (var session = resourceManager.Open(visaAddress) as IMessageBasedSession)
                    {
                        if (session == null)
                            return null;

                        session.TimeoutMilliseconds = 2000;
                        session.FormattedIO.WriteLine("*IDN?");
                        string response = session.FormattedIO.ReadLine();

                        Console.WriteLine($"{visaAddress} → {response}");
                        return response; // 返回 *IDN 的结果
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to {visaAddress}: {ex.Message}");
                    return null; // 表示失败
                }
            });
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(shiwang_textBox.Text))
            {
                shiwangName_textBox.Text = await TryConnectAndGetIdnAsync(shiwang_textBox.Text);
            }
            if (!string.IsNullOrEmpty(charge_textBox.Text))
            {
                chargeName_textBox.Text = await TryConnectAndGetIdnAsync(charge_textBox.Text);
            }
            if (!string.IsNullOrEmpty(gonglv_textBox.Text))
            {
                gonglvName_textBox.Text = await TryConnectAndGetIdnAsync(gonglv_textBox.Text);
            }
            if (!string.IsNullOrEmpty(xinhao_textBox.Text))
            {
                xinhaoName_textBox.Text = await TryConnectAndGetIdnAsync(xinhao_textBox.Text);
            }
            if (!string.IsNullOrEmpty(pinpu_textBox.Text))
            {
                pinpuName_textBox.Text = await TryConnectAndGetIdnAsync(pinpu_textBox.Text);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            mainForm.LogToConsole("正在获取网络接口信息...\n");

            // 获取所有网络接口
            var adapters = NetworkInterface.GetAllNetworkInterfaces();
            var npfDevices = CaptureDeviceList.Instance;

            foreach (var adapter in adapters)
            {
                var ipProps = adapter.GetIPProperties();
                var unicastAddresses = ipProps.UnicastAddresses;

                mainForm.LogToConsole($"接口名称: {adapter.Name}");
                mainForm.LogToConsole($"描述: {adapter.Description}");

                // 打印 IPv4 地址
                foreach (var addr in unicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        mainForm.LogToConsole($"IP 地址: {addr.Address}");
                    }
                }

                // 打印 MAC 地址
                var mac = adapter.GetPhysicalAddress();
                mainForm.LogToConsole($"MAC 地址: {string.Join(":", mac.GetAddressBytes().Select(b => b.ToString("X2")))}");

                // 查找对应的 NPF 设备（通过 MAC 地址比对）
                var matchedDevice = npfDevices.FirstOrDefault(dev =>
                {
                    var devMac = dev.MacAddress;
                    return devMac != null && devMac.Equals(mac);
                });

                if (matchedDevice != null)
                {
                    mainForm.LogToConsole($"NPF 接口: {matchedDevice.Name}");
                }
                else
                {
                    mainForm.LogToConsole("NPF 接口: 未找到匹配");
                }

                mainForm.LogToConsole(new string('-', 50));
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
