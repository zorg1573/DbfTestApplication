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
    public partial class TestSet_New_Form : MetroForm
    {
        public TestSet_New_Form()
        {
            InitializeComponent();
            this.Load += TestSet_New_Form_Load;
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
        }
        private void TestSet_New_Form_Load(object sender, EventArgs e)
        {
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
        private void LoadFromJson()
        {
            string filePath = "TestSetNew.json";
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
            File.WriteAllText("TestSetNew.json", json);
        }
        private void manual_button_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
