using DbfTest.FUNCTION;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DbfTest.PAGE
{
    public partial class VNASetting : Form
    {
        private readonly string _vnaAddr;

        public VNASetting(string vnaAddress)
        {
            InitializeComponent();
            _vnaAddr = vnaAddress;
        }
        public void PrintTextBox(string message)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new System.Action(() =>
                {
                    textBox1.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                }));
            }
            else
            {
                textBox1.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
        }

        private async void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(_vnaAddr);
                if (!connected)
                {
                    PrintTextBox("矢网连接失败");
                    return;
                }
                string resp = await scpiDevice.QueryAsync(":CALC1:PAR:CAT?");
                PrintTextBox(resp);
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(_vnaAddr);
                if (!connected)
                {
                    PrintTextBox("矢网连接失败");
                    return;
                }
                string nameString = textBox2.Text;
                bool res = await scpiDevice.SendCommandAsync($":CALC1:PAR:DEL '{nameString}'");
                if (res)
                {
                    PrintTextBox("success.");
                }
                else
                {
                    PrintTextBox("fail.");
                }
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(_vnaAddr);
                if (!connected)
                {
                    PrintTextBox("矢网连接失败");
                    return;
                }
                string newName = textBox3.Text;
                string type = textBox6.Text;
                bool res = await scpiDevice.SendCommandAsync($":CALC1:PAR:DEF '{newName}','{type}'");
                if (res)
                {
                    PrintTextBox("1.success.");
                    bool res2 = await scpiDevice.SendCommandAsync($":DISP:WIND1:TRAC:FEED '{newName}'");
                    if (res)
                    {
                        PrintTextBox("2.success.");
                    }
                    else
                    {
                        PrintTextBox("2.fail.");
                    }
                }
                else
                {
                    PrintTextBox("1.fail.");
                }
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(_vnaAddr);
                if (!connected)
                {
                    PrintTextBox("矢网连接失败");
                    return;
                }
                string nameString = textBox4.Text;
                bool res = await scpiDevice.SendCommandAsync(nameString);
                if (res)
                {
                    PrintTextBox("success.");
                }
                else
                {
                    PrintTextBox("fail.");
                }
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(_vnaAddr);
                if (!connected)
                {
                    PrintTextBox("矢网连接失败");
                    return;
                }
                string nameString = textBox5.Text;
                string resp = await scpiDevice.QueryAsync(nameString);
                PrintTextBox(resp);
                scpiDevice.Disconnect(); // 释放资源
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

    }
}
