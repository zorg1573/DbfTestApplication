using MetroFramework.Forms;
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
namespace DbfTest.PAGE
{
    public partial class XinhaoControl_Form : MetroForm
    {
        private Main_Form mainForm;
        string deviceAddress = "";

        public XinhaoControl_Form(Main_Form mainForm)
        {
            InitializeComponent();
            this.Load += XinhaoControl_Form_Load;
            this.mainForm = mainForm;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
        }
        private void XinhaoControl_Form_Load(object sender, EventArgs e)
        {
            GetAddress();
            LoadFromJson();
        }
        private IEnumerable<Control> GetAllControls(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                yield return ctrl;

                foreach (var child in GetAllControls(ctrl))
                    yield return child;
            }
        }
        private void GetAddress()
        {
            try
            {
                string filePath = "DeviceAddressNew.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                data.TryGetValue("xinhao_textBox", out object xinhao);
                if (xinhao != null)
                {
                    deviceAddress = xinhao.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceAddress.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void SaveToJson()
        {
            var data = new Dictionary<string, object>();

            foreach (Control ctrl in GetAllControls(this))
            {
                if (ctrl is TextBox textBox)
                {
                    data[textBox.Name] = textBox.Text;
                }
                else if (ctrl is ComboBox comboBox)
                {
                    data[comboBox.Name] = comboBox.SelectedItem?.ToString() ?? "";
                }
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("XinhaoSet.json", json);
        }

        private void LoadFromJson()
        {
            string filePath = "XinhaoSet.json";
            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (data == null)
                return;

            foreach (Control ctrl in GetAllControls(this))
            {
                if (data.TryGetValue(ctrl.Name, out object value))
                {
                    if (ctrl is TextBox textBox)
                    {
                        textBox.Text = value.ToString();
                    }
                    else if (ctrl is ComboBox comboBox)
                    {
                        string strVal = value.ToString();
                        if (comboBox.Items.Contains(strVal))
                            comboBox.SelectedItem = strVal;
                        else if (comboBox.Items.Count > 0)
                            comboBox.SelectedIndex = 0; // fallback
                    }
                }
            }
        }
        private async void start_output_button_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(deviceAddress);
            if (!connected)
            {
                mainForm.LogToConsole("连接失败");
                return;
            }
            await scpiDevice.EnableOutput();
            mainForm.LogToConsole("输出已开启");
            scpiDevice.Disconnect(); // 释放资源
        }

        private async void stop_output_button_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(deviceAddress);
            if (!connected)
            {
                mainForm.LogToConsole("连接失败");
                return;
            }
            await scpiDevice.DisableOutput();
            mainForm.LogToConsole("输出已关闭");
            scpiDevice.Disconnect(); // 释放资源
        }

        private void manual_button_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(deviceAddress);
            if (!connected)
            {
                mainForm.LogToConsole("连接失败");
                return;
            }
            await scpiDevice.ModON();
            mainForm.LogToConsole("MOD ON");
            scpiDevice.Disconnect(); // 释放资源
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(deviceAddress);
            if (!connected)
            {
                mainForm.LogToConsole("连接失败");
                return;
            }
            await scpiDevice.ModOFF();
            mainForm.LogToConsole("MOD OFF");
            scpiDevice.Disconnect(); // 释放资源
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(deviceAddress);
            if (!connected)
            {
                mainForm.LogToConsole("连接失败");
                return;
            }
            double freq = double.Parse(pinlv_textBox.Text);
            double power = double.Parse(gonglv_textBox.Text);
            string danwei = comboBox1.Text;
            if (danwei == "GHz")
            {
                freq = freq * 1e9;
            }
            if (danwei == "MHz")
            {
                freq = freq * 1e6;
            }
            await scpiDevice.SetFrequency(freq);
            await scpiDevice.SetPower(power);
            mainForm.LogToConsole("执行修改：" + ", 频率：" + freq + comboBox1.Text + ", 功率：" + power);
            scpiDevice.Disconnect(); // 释放资源
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
