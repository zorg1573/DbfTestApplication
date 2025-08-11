using System;
using System.IO;
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
using System.Text.Json;

namespace TestApp.PAGE
{
    public partial class DeviceAddres_Form : Form
    {
        public DeviceAddres_Form()
        {
            InitializeComponent();
            this.Load += DeviceAddres_Form_Load;
        }

        private void DeviceAddres_Form_Load(object sender, EventArgs e)
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
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("DeviceAddress.json", json);
        }

        private void LoadFromJson()
        {
            string filePath = "DeviceAddress.json";
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
                }
            }
        }


        private void save_button_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("设备地址已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
