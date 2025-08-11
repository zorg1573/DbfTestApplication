using System;
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

namespace TestApp
{
    public partial class SerialSet_Form : Form
    {
        SerialScanFunc serialScanFunc = new SerialScanFunc();

        public SerialSet_Form()
        {
            InitializeComponent();
            this.Load += SerialSet_Form_Load;
        }

        private void SerialSet_Form_Load(object sender, EventArgs e)
        {
            SerialScanConfig config = serialScanFunc.LoadConfig();
            if (config != null)
            {
                comboBox1.Text = config.COMPort;
                comboBox2.Text = config.BaudRate;
                comboBox3.Text = config.DataBits;
                comboBox4.Text = config.StopBits;
                comboBox5.Text = config.Parity;
                comboBox6.Text = config.Handshake;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SerialScanConfig config = new SerialScanConfig()
            {
                COMPort = comboBox1.Text,
                BaudRate = comboBox2.Text,
                DataBits = comboBox3.Text,
                StopBits = comboBox4.Text,
                Parity = comboBox5.Text,
                Handshake = comboBox6.Text
            };

            serialScanFunc.SaveConfig(config);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
