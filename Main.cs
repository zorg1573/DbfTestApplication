using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestApp.FUNCTION;
using TestApp.MODEL;
using TestApp.ViewControl;

namespace TestApp
{
    public partial class Main : Form
    {
        private Dictionary<string, UserControl> _controlMap;
        private DeviceListControl deviceListControl = new DeviceListControl();
        private Dictionary<string, ScpiDevice> _scpiDeviceMap = new Dictionary<string, ScpiDevice>();

        public Main()
        {
            InitializeComponent();
            InitMenu();
            this.Load += MainForm_Load;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            //deviceListControl.DeviceConnected += AddConnectedDeviceToListBox;
            deviceListControl.DeviceConnected += async (device) =>
            {
                await AddConnectedDeviceToListBox(device);
            };
        }

        private void lan_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form lanSetForm = new LANSet_Form();
            lanSetForm.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //DeviceListControl deviceListControl = new DeviceListControl();
            deviceListControl.Dock = DockStyle.Fill;
            splitContainer2.Panel1.Controls.Clear();
            splitContainer2.Panel1.Controls.Add(deviceListControl);

        }
        private void InitMenu()
        {
            // 设置菜单项
            /*            device_listBox.Items.Add("DP2031");
                        device_listBox.Items.Add("E8257D");*/

            // 初始化控件映射
            /*            _controlMap = new Dictionary<string, UserControl>
                        {
                            { "DP2031", new ChargeControl() },
                            { "E8257D", new PsgControl() },
                        };*/
            _controlMap = new Dictionary<string, UserControl>
            {
            };

            // 设置默认显示
            /*            device_listBox.SelectedIndexChanged += device_listBox_SelectedIndexChanged;
                        device_listBox.SelectedIndex = 0;*/
        }
        private void device_listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = device_listBox.SelectedItem.ToString();

            if (_controlMap.ContainsKey(selected))
            {
                splitContainer2.Panel1.Controls.Clear(); // 移除旧控件
                var control = _controlMap[selected];
                control.Dock = DockStyle.Fill;
                // 注入ScpiDevice
                if (_scpiDeviceMap.ContainsKey(selected))
                {
                    if (control is ChargeControl chargeControl)
                    {
                        chargeControl.CurrentDevice = _scpiDeviceMap[selected];
                    }
                    else if (control is PsgControl psgControl)
                    {
                        psgControl.CurrentDevice = _scpiDeviceMap[selected];
                    }
                }
                splitContainer2.Panel1.Controls.Add(control);
            }
        }
        public void LogToConsole(string message)
        {
            if (console_textBox.InvokeRequired)
            {
                console_textBox.Invoke(new Action(() => {
                    console_textBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                }));
            }
            else
            {
                console_textBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
        }

        private void serial_toolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form sereialSetForm = new SerialSet_Form();
            sereialSetForm.ShowDialog();
        }
        /*        public void AddConnectedDeviceToListBox(DeviceInfo device)
                {
                    if (!device_listBox.Items.Contains(device.DeviceName))
                    {
                        device_listBox.Items.Add(device.DeviceName);
                        _scpiDeviceMap[device.DeviceName] = new ScpiDevice(device);
                    }
                }*/
        public static string BuildVisaAddress(string ipPort, bool useSocket = true)
        {
            if (string.IsNullOrWhiteSpace(ipPort))
                throw new ArgumentException("IP 地址不能为空");

            // ipPort 可能为 "192.168.1.2" 或 "192.168.1.2:5025"
            string ip;
            int? port = null;
            var parts = ipPort.Split(':');
            if (parts.Length == 1)
            {
                ip = parts[0];
            }
            else if (parts.Length == 2 && int.TryParse(parts[1], out int p))
            {
                ip = parts[0];
                port = p;
            }
            else
            {
                throw new ArgumentException("地址格式无效，应为 IP 或 IP:端口");
            }

            if (port.HasValue && useSocket)
            {
                return $"TCPIP0::{ip}::{port}::SOCKET";
            }
            else
            {
                return $"TCPIP0::{ip}::INSTR";
            }
        }
        public async Task AddConnectedDeviceToListBox(DeviceInfo device)
        {
            if (!device_listBox.Items.Contains(device.DeviceName))
            {
                device_listBox.Items.Add(device.DeviceName);
            }

            var scpiDevice = new ScpiDevice(device);
            string visaAddress = BuildVisaAddress(device.Address);
            bool connected = await scpiDevice.ConnectAsync(visaAddress);

            if (!connected)
            {
                LogToConsole($"设备 {device.DeviceName} 连接失败！");
                return;
            }

            _scpiDeviceMap[device.DeviceName] = scpiDevice;

            if (!_controlMap.ContainsKey(device.DeviceName))
            {
                if (device.DeviceName.Contains("DP"))
                {
                    _controlMap[device.DeviceName] = new ChargeControl();
                }
                else if (device.DeviceName.Contains("E82") || device.DeviceName.Contains("SP"))
                {
                    _controlMap[device.DeviceName] = new PsgControl();
                }
                else if (device.DeviceName.Contains("4052F"))
                {
                    _controlMap[device.DeviceName] = new SpectrumControl();
                }
            }

            if (_controlMap[device.DeviceName] is ChargeControl chargeControl)
            {
                chargeControl.CurrentDevice = scpiDevice;
            }
            else if (_controlMap[device.DeviceName] is PsgControl psgControl)
            {
                psgControl.CurrentDevice = scpiDevice;
            }
            else if (_controlMap[device.DeviceName] is SpectrumControl spectrumControl)
            {
                spectrumControl.CurrentDevice = scpiDevice;
            }

            device_listBox.SelectedItem = device.DeviceName;
        }

    }
}
