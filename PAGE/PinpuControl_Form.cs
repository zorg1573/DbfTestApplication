using MetroFramework.Forms;
using Org.BouncyCastle.Ocsp;
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

namespace DbfTest.PAGE
{
    public partial class PinpuControl_Form : MetroForm
    {
        private Main_Form mainForm;
        string deviceAddress = "";
        public PinpuControl_Form(Main_Form mainForm)
        {
            InitializeComponent();
            this.Load += PinpuControl_Form_Load;
            this.mainForm = mainForm;
        }
        private void PinpuControl_Form_Load(object sender, EventArgs e)
        {
            GetAddress();
        }
        private void GetAddress()
        {
            try
            {
                string filePath = "DeviceAddress_KU.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                data.TryGetValue("pinpu_textBox", out object charge);
                if (charge != null)
                {
                    deviceAddress = charge.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceAddress.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private async void set_button_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(deviceAddress);
                if (!connected)
                {
                    mainForm.LogToConsole("连接失败");
                    return;
                }

                // 逐项检查是否为空，非空才设置
                if (!string.IsNullOrWhiteSpace(centerFreq_textBox.Text))
                    await scpiDevice.SetCenterFrequencyAsync(double.Parse(centerFreq_textBox.Text) * 1e9);

                if (!string.IsNullOrWhiteSpace(span_textBox.Text))
                    await scpiDevice.SetSpanAsync(double.Parse(span_textBox.Text) * 1e9);

                if (!string.IsNullOrWhiteSpace(startFreq_textBox.Text))
                    await scpiDevice.SetStartFrequencyAsync(double.Parse(startFreq_textBox.Text) * 1e9);

                if (!string.IsNullOrWhiteSpace(stopFreq_textBox.Text))
                    await scpiDevice.SetStopFrequencyAsync(double.Parse(stopFreq_textBox.Text) * 1e9);

                if (!string.IsNullOrWhiteSpace(rbw_textBox.Text))
                    await scpiDevice.SetRBWAsync(double.Parse(rbw_textBox.Text));

                if (!string.IsNullOrWhiteSpace(vbw_textBox.Text))
                    await scpiDevice.SetVBWAsync(double.Parse(vbw_textBox.Text));

                if (!string.IsNullOrWhiteSpace(scanTime_textBox.Text))
                    await scpiDevice.SetSweepTimeAsync(double.Parse(scanTime_textBox.Text));

                if (!string.IsNullOrWhiteSpace(trigger_comboBox.Text))
                    await scpiDevice.SetTriggerSourceAsync(trigger_comboBox.Text);

                mainForm.LogToConsole("参数已应用");
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置参数失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void save_button_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = new SpectrumData
                {
                    CenterFrequencyGHz = double.Parse(centerFreq_textBox.Text),
                    StartFrequencyMHz = double.Parse(startFreq_textBox.Text),
                    StopFrequencyGHz = double.Parse(stopFreq_textBox.Text),
                    SpanGHz = double.Parse(span_textBox.Text),
                    RBW = double.Parse(rbw_textBox.Text),
                    VBW = double.Parse(vbw_textBox.Text),
                    SweepTime = double.Parse(scanTime_textBox.Text),
                    TriggerSource = trigger_comboBox.Text
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum_settings.json");
                File.WriteAllText(path, json);
                mainForm.LogToConsole("参数已保存到 spectrum_settings.json");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存参数失败: " + ex.Message);
            }
        }

        private async void setMark_button_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(deviceAddress);
                if (!connected)
                {
                    mainForm.LogToConsole("连接失败");
                    return;
                }

                //await scpiDevice.SetMarkerToMaxAsync();
                //mainForm.LogToConsole("标记最大点位");
                double freq = double.Parse(mark_textBox.Text)*1e9;
                await scpiDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                await scpiDevice.SendCommandAsync($":CALC:MARK1:X {freq}");
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show("标记最大点位失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void scanStart_button_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(deviceAddress);
                if (!connected)
                {
                    mainForm.LogToConsole("连接失败");
                    return;
                }

                await scpiDevice.StartSweepAsync();
                mainForm.LogToConsole("连续扫描");
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show("连续扫描失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void getData_button_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(deviceAddress);
                if (!connected)
                {
                    mainForm.LogToConsole("连接失败");
                    return;
                }

                double freq = await scpiDevice.ReadMarkerFrequencyAsync() ?? 0;
                double power = await scpiDevice?.ReadMarkerPowerAsync() ?? 0;
                mainForm.LogToConsole("标记位频率：" + freq + ", 标记位功率：" + power);

                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取数据失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void scanOnce_button_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(deviceAddress);
                if (!connected)
                {
                    mainForm.LogToConsole("连接失败");
                    return;
                }

                await scpiDevice.StartSingleSweepAsync();
                mainForm.LogToConsole("单次扫描");
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show("单次扫描失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
