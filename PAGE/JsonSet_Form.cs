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
    public partial class JsonSet_Form : MetroForm
    {
        public JsonSet_Form()
        {
            InitializeComponent();
            this.Load += JsonSet_Form_Load;
        }
        private void JsonSet_Form_Load(object sender, EventArgs e)
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
            }

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("JsonSet_DBF.json", json);
        }

        private void LoadFromJson()
        {
            string filePath = "JsonSet_DBF.json";
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
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            SaveToJson();
            MessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
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

        private void button5_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "请选择一个 Json 文件";
                dialog.Filter = "Json 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    deviceAddress_textBox.Text = dialog.FileName;
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "请选择一个 Json 文件";
                dialog.Filter = "Json 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    deviceFiles_textBox.Text = dialog.FileName;
                }
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "请选择一个 Json 文件";
                dialog.Filter = "Json 文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    testSet_textBox.Text = dialog.FileName;
                }
            }
        }
    }
}
