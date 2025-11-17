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

namespace DbfTest.PAGE
{
    public partial class DeviceFiles_Form : MetroForm
    {
        public DeviceFiles_Form()
        {
            InitializeComponent();
            this.Load += DeviceFiles_Form_Load;
        }
        private void DeviceFiles_Form_Load(object sender, EventArgs e)
        {
            LoadFromJson();
        }

        private void SaveToJson()
        {
            var data = new Dictionary<string, object>();

            foreach (Control ctrl in splitContainer1.Panel1.Controls)
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
            File.WriteAllText("DeviceFiles_KU.json", json);
        }

        private void LoadFromJson()
        {
            string filePath = "DeviceFiles_KU.json";
            if (!File.Exists(filePath))
                return;

            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (data == null)
                return;

            foreach (Control ctrl in splitContainer1.Panel1.Controls)
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
        private void button1_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择一个文件夹";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    textBox6.Text = dialog.SelectedPath;
                }
            }
        }
        /*        private void button3_Click(object sender, EventArgs e)
                {
                    using (OpenFileDialog dialog = new OpenFileDialog())
                    {
                        dialog.Title = "请选择一个 Excel 文件";
                        dialog.Filter = "Excel 文件 (*.xlsx;*.xls)|*.xlsx;*.xls|所有文件 (*.*)|*.*";
                        dialog.Multiselect = false;

                        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                        {
                            textBox6.Text = dialog.FileName;
                        }
                    }
                }*/


        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "请选择一个 Excel 模板";
                dialog.Filter = "Excel 模板 (*.xlt)|*.xlt|所有文件 (*.*)|*.*";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    textBox7.Text = dialog.FileName;
                }
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
