using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DbfTest.FUNCTION;
using DbfTest.MODEL;
using DbfTest.ViewControl;

namespace DbfTest
{
    public partial class DeviceListControl : UserControl
    {
        private List<DeviceInfo> deviceList = new List<DeviceInfo>();
        private readonly string saveFilePath = Path.Combine(Application.StartupPath, "devices.json");
        private LanScanFunc lanScanFunc = new LanScanFunc();
        private SerialScanFunc serialScanFunc = new SerialScanFunc();
        private DeviceConnectionManager deviceConnectionManager = new DeviceConnectionManager();
        public event Action<DeviceInfo> DeviceConnected;

        private Form Main => this.FindForm();

        public DeviceListControl()
        {
            InitializeComponent();
            this.Load += DeviceListControl_Load;
        }

        private void DeviceListControl_Load(object sender, EventArgs e)
        {
            LoadDevicesFromFile();
        }
        private void Log(string msg)
        {
            if (Main is Main parent)
            {
                parent.LogToConsole(msg);
            }
        }
        public void LoadDevicesFromFile()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    deviceList = JsonSerializer.Deserialize<List<DeviceInfo>>(json) ?? new List<DeviceInfo>();

                    foreach (var device in deviceList)
                    {
                        AddDeviceToGrid(device);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("加载设备信息失败：" + ex.Message);
                }
            }
        }

        public void AddDeviceToGrid(DeviceInfo device)
        {
            dataGridView1.Rows.Add(device.DeviceName, device.Address);
        }

        private void btnAddDevice_Click(object sender, EventArgs e)
        {
            using (var form = new DeviceList_Add_Form())
            {
                if (form.ShowDialog() == DialogResult.OK && form.Device != null)
                {
                    AddDeviceToGrid(form.Device); 
                }
            }
        }

        private LanScanConfig LoadLanScanConfig()
        {
            var path = Path.Combine(Application.StartupPath, "LanScanConfig.json");
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<LanScanConfig>(File.ReadAllText(path));
        }

        private SerialScanConfig LoadSerialScanConfig()
        {
            var path = Path.Combine(Application.StartupPath, "SerialScanConfig.json");
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<SerialScanConfig>(File.ReadAllText(path));
        }


        private void add_button_Click(object sender, EventArgs e)
        {
            using (var form = new DeviceList_Add_Form())
            {
                if (form.ShowDialog() == DialogResult.OK && form.Device != null)
                {
                    deviceList.Add(form.Device); 
                    AddDeviceToGrid(form.Device);
                    SaveDevicesToFile();
                }
            }
        }

        /*        private async void scan_button_Click(object sender, EventArgs e)
                {
                    Log("开始扫描...");
                    // var lanDevices = await lanScanFunc.ScanLanDevicesAsync();
                    var lanDevices = await lanScanFunc.ScanLanDevicesWithVisaAsync();
                    var serialDevices = serialScanFunc.ScanSerialPorts();

                    var allDevices = lanDevices.Concat(serialDevices).ToList();

                    foreach (var device in allDevices)
                    {
                        if (!deviceList.Any(d => d.Address == device.Address))
                        {
                            deviceList.Add(device);
                            AddDeviceToGrid(device);
                            Log($"扫描到设备:"+device.DeviceName);
                        }
                    }
                    if(allDevices.Count == 0)
                    {
                        Log("未扫描到设备");
                    }
                    SaveDevicesToFile();
                }*/
        private async void scan_button_Click(object sender, EventArgs e)
        {
            Log("开始扫描...");

            // 禁用按钮，避免重复点击
            scan_button.Enabled = false;

            try
            {
                var allDevices = await Task.Run(() =>
                {
                    var lanDevices = lanScanFunc.ScanLanDevicesWithVisaAsync().Result;
                    var serialDevices = serialScanFunc.ScanSerialPorts();
                    return lanDevices.Concat(serialDevices).ToList();
                });

                foreach (var device in allDevices)
                {
                    if (!deviceList.Any(d => d.Address == device.Address))
                    {
                        deviceList.Add(device);

                        // 必须在 UI 线程更新控件
                        Invoke(new Action(() =>
                        {
                            AddDeviceToGrid(device);
                            Log($"扫描到设备: {device.DeviceName}");
                        }));
                    }
                }

                if (allDevices.Count == 0)
                {
                    Log("未扫描到设备");
                }
                else
                {

                   Log("扫描结束");
                }

                SaveDevicesToFile();
            }
            catch (Exception ex)
            {
                Log("扫描过程中发生错误: " + ex.Message);
            }
            finally
            {
                scan_button.Enabled = true;
            }
        }



        private void SaveDevicesToFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(deviceList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存设备信息失败：" + ex.Message);
            }
        }

        private async void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // 跳过表头或无效点击
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var columnName = dataGridView1.Columns[e.ColumnIndex].Name;
            var device = deviceList[e.RowIndex];
            if (device == null) return;

            // 连接按钮
            if (columnName == "Column1")
            {
                var (success, message, scpiDevice) = await deviceConnectionManager.ConnectAsync(device);

                if (success)
                {
                    DeviceConnected?.Invoke(device); // 触发事件
                    Log(device.DeviceName + " 连接成功。" + message);
                }
                else
                {
                    Log(device.DeviceName + " 连接失败。" + message);
                }

                dataGridView1.Rows[e.RowIndex].Cells["StatusColumn"].Value = success ? "已连接" : "未连接";
            }

            // 编辑按钮
            else if (columnName == "Column2")
            {
                using (var form = new DeviceList_Add_Form(device)) // 传入原始设备信息以便编辑
                {
                    if (form.ShowDialog() == DialogResult.OK && form.Device != null)
                    {
                        // 更新设备列表中的设备信息
                        deviceList[e.RowIndex] = form.Device;

                        // 更新表格显示
                        var row = dataGridView1.Rows[e.RowIndex];
                        row.Cells[0].Value = form.Device.DeviceName;
                        row.Cells[1].Value = form.Device.Address;

                        Log($"已更新设备：{form.Device.DeviceName}");
                        SaveDevicesToFile();
                    }
                }
            }

            // 删除按钮
            else if (columnName == "Column3")
            {
                var result = MessageBox.Show("确定要删除该设备吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    deviceList.RemoveAt(e.RowIndex);
                    dataGridView1.Rows.RemoveAt(e.RowIndex);
                    SaveDevicesToFile();
                    Log($"已删除设备：{device.DeviceName}");
                }
            }
        }

    }
}
