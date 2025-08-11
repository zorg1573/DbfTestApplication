using MetroFramework.Forms;
using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using TestApp.DAL;
using TestApp.MODEL;

namespace TestApp.PAGE
{
    public partial class SqlSet_Form : MetroForm
    {
        public SqlSet_Form()
        {
            InitializeComponent();
        }
        private void SqlSet_Form_Load(object sender, EventArgs e)
        {
            var config = DbConfigHelper.LoadConfig();
            if (config != null)
            {
                server_textBox.Text = config.Server;
                database_textBox.Text = config.Database;
                userid_textBox.Text = config.UserId;
                password_textBox.Text = config.Password;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var connectionString = new DbConfig
            {
                Server = server_textBox.Text.Trim(),
                Database = database_textBox.Text.Trim(),
                UserId = userid_textBox.Text.Trim(),
                Password = password_textBox.Text.Trim()
            }.GetConnection();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    MessageBox.Show("连接成功！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接失败：" + ex.Message);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            var config = new DbConfig
            {
                Server = server_textBox.Text.Trim(),
                Database = database_textBox.Text.Trim(),
                UserId = userid_textBox.Text.Trim(),
                Password = password_textBox.Text.Trim()
            };

            try
            {
                DbConfigHelper.SaveConfig(config);
                MessageBox.Show("保存成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败：" + ex.Message);
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
