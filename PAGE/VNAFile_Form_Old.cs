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
    public partial class VNAFile_Form_Old : Form
    {
        string vnaAddress = "";
        string vnaFilePath = "";
        public VNAFile_Form_Old()
        {
            InitializeComponent();
            this.Load += VNAFile_Form_Old_Load;
        }
        private void VNAFile_Form_Old_Load(object sender, EventArgs e)
        {
            LoadFromJson();
            GetAddress();
            GetDeviceFilesJson();
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

                data.TryGetValue("shiwang_textBox", out object shiwang);
                if (shiwang != null)
                {
                    vnaAddress = shiwang.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceAddress.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void GetDeviceFilesJson()
        {
            try
            {
                string filePath = "DeviceFiles.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                data.TryGetValue("textBox1", out object value2);
                if (value2 != null)
                {
                    vnaFilePath = value2.ToString();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceFiles.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                else if (ctrl is CheckBox checkBox)
                {
                    data[checkBox.Name] = checkBox.Checked;
                }
                else if (ctrl is RadioButton radioButton)
                {
                    data[radioButton.Name] = radioButton.Checked;
                }
                else if (ctrl is NumericUpDown numericUpDown)
                {
                    data[numericUpDown.Name] = numericUpDown.Value;
                }
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("VNAFile.json", json);
        }
        private void LoadFromJson()
        {
            string filePath = "VNAFile.json";
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
                    else if (ctrl is CheckBox checkBox)
                    {
                        if (bool.TryParse(value.ToString(), out bool isChecked))
                        {
                            checkBox.Checked = isChecked;
                        }
                    }
                    else if (ctrl is RadioButton radioButton)
                    {
                        if (bool.TryParse(value.ToString(), out bool isChecked))
                        {
                            radioButton.Checked = isChecked;
                        }
                    }
                    else if (ctrl is NumericUpDown numericUpDown)
                    {
                        numericUpDown.Value = decimal.Parse(value.ToString());
                    }
                }
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(vnaAddress);
            if (!connected)
            {
                MessageBox.Show("连接失败！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string startFreqDanwei = comboBox1.Text;
            string stopFreqDanwei = comboBox2.Text;
            double startFreq = 0.0;
            double stopFreq = 0.0;
            if (startFreqDanwei == "MHz")
            {
                startFreq = double.Parse(start_freq_textBox.Text) * 1e6; // 转换为Hz
            }
            else if (startFreqDanwei == "GHz")
            {
                startFreq = double.Parse(start_freq_textBox.Text) * 1e9; // 转换为Hz
            }
            if (stopFreqDanwei == "MHz")
            {
                stopFreq = double.Parse(stop_freq_textBox.Text) * 1e6; // 转换为Hz
            }
            else if (stopFreqDanwei == "GHz")
            {
                stopFreq = double.Parse(stop_freq_textBox.Text) * 1e9; // 转换为Hz
            }

            await scpiDevice.SetVNAStartFreq(startFreq);
            await scpiDevice.SetVNAStopFreq(stopFreq);
            await scpiDevice.SetPointCount(int.Parse(pointCount_textBox.Text));
            await scpiDevice.SaveStateFile(vnaFilePath);

            scpiDevice.Disconnect(); // 释放资源

            SaveToJson();
            MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
