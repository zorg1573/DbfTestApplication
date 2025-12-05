using MetroFramework.Forms;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DbfTest.DAL;

namespace DbfTest.PAGE
{
    public partial class ManualSend_Form : MetroForm
    {
        //string ifaceName = @"\Device\NPF_{3A0CA248-4796-4CBA-B275-E9C8E0A766CF}"; // 注意：需要和系统中接口名称完全匹配
        string ifaceName = "";
        //static string dstMacStr = "00:0a:35:01:fe:c0";
        string dstMacStr = "";
        string srcMacStr = ""; //上位机MAC地址
        string srcMacAddress = "";
        //static string srcIpStr = "192.168.0.3";
        //static string dstIpStr = "192.168.0.2";
        string srcIpStr = "";
        string dstIpStr = "";

        static ushort srcPort = 8080;
        static ushort dstPort = 8080;
        // byte[] headValue = StringToByteArray("00 0a 35 01 fe c0 00 2b 67 f1 71 51 08 00 45 00 b4 68 00 00 80 11 00 00 c0 a8 00 03 c0 a8 00 02 1f 90 1f 90 00 23 81 70");
        byte[] headValue;
        //byte[] modelValue = StringToByteArray("01 03 01 00");
        byte[] modelValue;
        byte[] emptyValue = StringToByteArray("00 00 00 00 00 00 00 00");
        private Main_Form mainForm;
        string ch1Yixiang = "000000";
        string ch2Yixiang = "000000";
        string ch3Yixiang = "000000";
        string ch4Yixiang = "000000";
        string ch5Yixiang = "000000";
        string ch6Yixiang = "000000";
        string ch7Yixiang = "000000";
        string ch8Yixiang = "000000";
        string ch1Shuaijian = "000000";
        string ch2Shuaijian = "000000";
        string ch3Shuaijian = "000000";
        string ch4Shuaijian = "000000";
        private OperateLog_DAL operateLog_DAL = new OperateLog_DAL();
        /*        public ManualSend_Form()
                {
                    InitializeComponent();
                    this.Load += ManualSend_Form_Load;
                    radioButton4.Checked = true;
                    radioButton6.Checked = true;
                }*/
        public ManualSend_Form(Main_Form mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            this.Load += ManualSend_Form_Load;
            mgc_comboBox.SelectedIndex = 0;
        }
        private void ManualSend_Form_Load(object sender, EventArgs e)
        {
            GetAddress();
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
                data.TryGetValue("pc_mac_textBox", out object pcMac);
                if (pcMac != null)
                {
                    srcMacStr = pcMac.ToString().Replace(":", " ");
                    srcMacAddress = pcMac.ToString();
                }
                headValue = StringToByteArray("00 0a 35 01 fe c0 " + srcMacStr + " 08 00 45 00 b4 68 00 00 80 11 00 00 c0 a8 00 03 c0 a8 00 02 1f 90 1f 90 00 23 81 70");

                data.TryGetValue("pc_jiekou_textBox", out object pcJiekou);
                if (pcJiekou != null)
                {
                    ifaceName = pcJiekou.ToString();
                }

                data.TryGetValue("pc_ip_textBox", out object pcIp);
                if (pcIp != null)
                {
                    srcIpStr = pcIp.ToString();
                }

                data.TryGetValue("fpga_ip_textBox", out object fpgaIp);
                if (fpgaIp != null)
                {
                    dstIpStr = fpgaIp.ToString();
                }

                data.TryGetValue("fpga_mac_textBox", out object fpgaMac);
                if (fpgaMac != null)
                {
                    dstMacStr = fpgaMac.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceAddress.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void send_button_Click(object sender, EventArgs e)
        {
            try
            {
                if (!radioButton1.Checked && !radioButton2.Checked && !radioButton3.Checked)
                {
                    MessageBox.Show("请选择发送或接收模式！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (ch1_yixiang_textBox.Text != "")
                {
                    ch1Yixiang = ch1_yixiang_textBox.Text;
                }
                if (ch2_yixiang_textBox.Text != "")
                {
                    ch2Yixiang = ch2_yixiang_textBox.Text;
                }
                if (ch3_yixiang_textBox.Text != "")
                {
                    ch3Yixiang = ch3_yixiang_textBox.Text;
                }
                if (ch4_yixiang_textBox.Text != "")
                {
                    ch4Yixiang = ch4_yixiang_textBox.Text;
                }
                if (ch5_yixiang_textBox.Text != "")
                {
                    ch5Yixiang = ch5_yixiang_textBox.Text;
                }
                if (ch6_yixiang_textBox.Text != "")
                {
                    ch6Yixiang = ch6_yixiang_textBox.Text;
                }
                if (ch7_yixiang_textBox.Text != "")
                {
                    ch7Yixiang = ch7_yixiang_textBox.Text;
                }
                if (ch8_yixiang_textBox.Text != "")
                {
                    ch8Yixiang = ch8_yixiang_textBox.Text;
                }
                if (shuaijian_textBox.Text != "")
                {
                    ch1Shuaijian = shuaijian_textBox.Text;
                }

                if (radioButton2.Checked)
                {
                    mainForm.LogToConsole("开始发射测试"); //接收开关 发射移相 接收移相 发射衰减 接收衰减 发射开关
                    string ch1send = checkBox_ch1.Checked ? "1" : "0";
                    string ch2send = checkBox_ch2.Checked ? "1" : "0";
                    string ch3send = checkBox_ch3.Checked ? "1" : "0";
                    string ch4send = checkBox_ch4.Checked ? "1" : "0";
                    string ch5send = checkBox_ch5.Checked ? "1" : "0";
                    string ch6send = checkBox_ch6.Checked ? "1" : "0";
                    string ch7send = checkBox_ch7.Checked ? "1" : "0";
                    string ch8send = checkBox_ch8.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + "0" + ch1Yixiang + "000000" + "000000" + "000000" + ch1send;
                    string ch2 = "00" + "0" + ch2Yixiang + "000000" + "000000" + "000000" + ch2send;
                    string ch3 = "00" + "0" + ch3Yixiang + "000000" + "000000" + "000000" + ch3send;
                    string ch4 = "00" + "0" + ch4Yixiang + "000000" + "000000" + "000000" + ch4send;
                    string ch5 = "00" + "0" + ch5Yixiang + "000000" + "000000" + "000000" + ch5send;
                    string ch6 = "00" + "0" + ch6Yixiang + "000000" + "000000" + "000000" + ch6send;
                    string ch7 = "00" + "0" + ch7Yixiang + "000000" + "000000" + "000000" + ch7send;
                    string ch8 = "00" + "0" + ch8Yixiang + "000000" + "000000" + "000000" + ch8send;
                    string model = "00";
                    string model_stc = model + ch1Shuaijian + mgc_comboBox.SelectedIndex;
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("手动发码|发射测试", $"{ch1send},{ch2send},{ch3send},{ch4send}");
                }
                else if (radioButton1.Checked)
                {
                    mainForm.LogToConsole("开始接收测试");
                    string ch1recive = checkBox_ch1.Checked ? "1" : "0";
                    string ch2recive = checkBox_ch2.Checked ? "1" : "0";
                    string ch3recive = checkBox_ch3.Checked ? "1" : "0";
                    string ch4recive = checkBox_ch4.Checked ? "1" : "0";
                    string ch5recive = checkBox_ch5.Checked ? "1" : "0";
                    string ch6recive = checkBox_ch6.Checked ? "1" : "0";
                    string ch7recive = checkBox_ch7.Checked ? "1" : "0";
                    string ch8recive = checkBox_ch8.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + ch1recive + ch1Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + ch2recive + ch2Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + ch3recive + ch3Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + ch4recive + ch4Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + ch5recive + ch5Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + ch6recive + ch6Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + ch7recive + ch7Yixiang + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + ch8recive + ch8Yixiang + "000000" + "000000" + "000000" + "0";
                    string model = "10";
                    string model_stc = model + ch1Shuaijian + mgc_comboBox.SelectedIndex;
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });
                    mainForm.LogToConsole(ch1Yixiang);
                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("手动发码|接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}");
                }
                else if (radioButton3.Checked)
                {
                    mainForm.LogToConsole("负载模式");
                    string ch1 = new string('0', 28) + "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string model_stc = "01" + "000000" + mgc_comboBox.SelectedIndex;
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 03 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("负载模式","");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("手动发码失败: " + ex.ToString(), "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //operateLog_DAL.InsertOperateLog_DT("手动发码失败", ex.ToString());
            }
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #region UDP发送
        public void SendCustomPacket(byte[] headValue, byte[] modelValue, byte[] emptyValue, byte[] codeValue)
        {
            try
            {
                if (string.IsNullOrEmpty(srcIpStr) || string.IsNullOrEmpty(dstIpStr) || string.IsNullOrEmpty(srcMacAddress) || string.IsNullOrEmpty(dstMacStr))
                {
                    MessageBox.Show("IP或MAC地址为空，请设置后再操作。");
                    return;
                }

                var payload = headValue.Concat(modelValue).Concat(emptyValue).Concat(codeValue).ToArray();

                PhysicalAddress srcMac = PhysicalAddress.Parse(srcMacAddress.Trim().Replace(":", "-").ToUpperInvariant());
                PhysicalAddress dstMac = PhysicalAddress.Parse(dstMacStr.Trim().Replace(":", "-").ToUpperInvariant());
                IPAddress srcIp = IPAddress.Parse(srcIpStr);
                IPAddress dstIp = IPAddress.Parse(dstIpStr);

                // 创建 UDP 数据包
                var udpPacket = new UdpPacket(srcPort, dstPort)
                {
                    PayloadData = payload
                };

                // 创建 IP 数据包
                var ipPacket = new IPv4Packet(srcIp, dstIp)
                {
                    Protocol = ProtocolType.Udp,
                    TimeToLive = 128
                };
                ipPacket.PayloadPacket = udpPacket;

                // 创建以太网帧
                var ethernetPacket = new EthernetPacket(srcMac, dstMac, EthernetType.IPv4)
                {
                    PayloadPacket = ipPacket
                };

                // 选择接口
                var devices = CaptureDeviceList.Instance;
                var device = CaptureDeviceList.Instance.FirstOrDefault(d => d.Name == ifaceName);
                if (device == null)
                {
                    mainForm.LogToConsole("找不到接口：" + ifaceName);
                    return;
                }

                device.Open();
                device.SendPacket(ethernetPacket);
                device.Close();

                mainForm.LogToConsole($"发送数据包：Payload长度={payload.Length}字节");
                mainForm.LogToConsole($"Payload (Hex): {BitConverter.ToString(payload).Replace("-", " ")}");
                mainForm.LogToConsole("数据包已发送。\n");
            }
            catch(Exception ex)
            {
                MessageBox.Show("UDP发送失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //operateLog_DAL.InsertOperateLog_DT("UDP发送失败", ex.ToString());
            }

        }

        static byte[] GenerateCodeValueFromBits(string[] bitStrings)
        {
/*            if (bitStrings.Length != 10)
                throw new ArgumentException("应包含10个通道的比特串");*/

            int[] expectedLengths = { 56, 28, 28, 28, 28, 28, 28, 28, 9, 59};
            string allBits = "";

            for (int i = 0; i < 10; i++)
            {
                string bits = bitStrings[i].Replace(" ", "");
                if (bits.Length != expectedLengths[i])
                    throw new ArgumentException($"通道 {i + 1} 应为 {expectedLengths[i]} 位，但提供了 {bits.Length} 位");

                allBits += bits;
            }

            if (allBits.Length != 320)
                throw new ArgumentException($"总位数应为320，但现在是 {allBits.Length}");

            // 输出 15 字节（120 位）
            byte[] codeBytes = new byte[40];
            for (int i = 0; i < 40; i++)
            {
                string byteStr = allBits.Substring(i * 8, 8);
                codeBytes[i] = Convert.ToByte(byteStr, 2);
            }

            return codeBytes;
        }


        static byte[] StringToByteArray(string hex)
        {
            try
            {
                return hex.Split(' ')
                  .Select(s => Convert.ToByte(s, 16))
                  .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine("转换十六进制字符串到字节数组失败: " + ex.Message);
                return new byte[0];
            }

        }
        private string GetBinaryFromTextBox(int number)
        {
            if (number < 0 || number > 63)
                throw new ArgumentOutOfRangeException(nameof(number), "输入必须在 0 到 63 之间。");

            //return Convert.ToString(number, 2).PadLeft(6, '0');
            string binary = Convert.ToString(number, 2).PadLeft(6, '0');
            char[] reversed = binary.ToCharArray();
            Array.Reverse(reversed);
            return new string(reversed);
        }

        public string DecimalToBinary(int number, int totalBits)
        {
            return Convert.ToString(number, 2).PadLeft(totalBits, '0');
        }
        #endregion
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
