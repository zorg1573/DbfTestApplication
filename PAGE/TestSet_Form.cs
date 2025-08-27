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
    public partial class TestSet_Form : Form
    {
        public TestSet_Form()
        {
            InitializeComponent();
            this.Load += TestSet_Form_Load;
        }
        private void TestSet_Form_Load(object sender, EventArgs e)
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
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("TestSet.json", json);
        }
        private void LoadFromJson()
        {
            string filePath = "TestSet.json";
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
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
