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
        string ifaceName = "";
        string dstMacStr = "";
        string srcMacStr = ""; //上位机MAC地址
        string srcMacAddress = "";
        string srcIpStr = "";
        string dstIpStr = "";

        static ushort srcPort = 8080;
        static ushort dstPort = 8080;
        byte[] headValue;
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

        public ManualSend_Form(Main_Form mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            this.Load += ManualSend_Form_Load;
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
        static byte[] GenerateCodeValueFromBits(string[] bitStrings)
        {
            /*            if (bitStrings.Length != 10)
                            throw new ArgumentException("应包含10个通道的比特串");*/

            int[] expectedLengths = { 8, 24, 24, 24, 24, 8, 8 };
            string allBits = "";

            for (int i = 0; i < 7; i++)
            {
                string bits = bitStrings[i].Replace(" ", "");
                if (bits.Length != expectedLengths[i])
                    throw new ArgumentException($"通道 {i + 1} 应为 {expectedLengths[i]} 位，但提供了 {bits.Length} 位");

                allBits += bits;
            }

            if (allBits.Length != 120)
                throw new ArgumentException($"总位数应为120，但现在是 {allBits.Length}");

            // 输出 15 字节（120 位）
            byte[] codeBytes = new byte[15];
            for (int i = 0; i < 15; i++)
            {
                string byteStr = allBits.Substring(i * 8, 8);
                codeBytes[i] = Convert.ToByte(byteStr, 2);
            }

            return codeBytes;
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
                if (ch1_yixiang_textBox.Text != "")
                {
                    ch1Shuaijian = ch1_shuaijian_textBox.Text;
                }
                if (ch2_yixiang_textBox.Text != "")
                {
                    ch2Shuaijian = ch2_shuaijian_textBox.Text;
                }
                if (ch3_yixiang_textBox.Text != "")
                {
                    ch3Shuaijian = ch3_shuaijian_textBox.Text;
                }
                if (ch4_yixiang_textBox.Text != "")
                {
                    ch4Shuaijian = ch4_shuaijian_textBox.Text;
                }

                if (radioButton2.Checked)
                {
                    mainForm.LogToConsole("开始发射测试"); //接收开关 发射移相 接收移相 发射衰减 接收衰减 发射开关
                    string ch1send = checkBox_ch1.Checked ? "1" : "0";
                    string ch2send = checkBox_ch2.Checked ? "1" : "0";
                    string ch3send = checkBox_ch3.Checked ? "1" : "0";
                    string ch4send = checkBox_ch4.Checked ? "1" : "0";
                    string tr = "0" + ch4send + "0" + ch3send + "0" + ch2send + "0" + ch1send;
                    string ta = ch4Shuaijian + ch3Shuaijian + ch2Shuaijian + ch1Shuaijian;
                    string ra = new string('0', 24);
                    string tp = ch4Yixiang + ch3Yixiang + ch2Yixiang + ch1Yixiang;
                    string rp = new string('0', 24);
                    string model = "00000000";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("手动发码|发射测试", $"{ch1send},{ch2send},{ch3send},{ch4send}");
                }
                else if (radioButton1.Checked)
                {
                    mainForm.LogToConsole("开始接收测试");
                    string ch1recieve = checkBox_ch1.Checked ? "1" : "0";
                    string ch2recieve = checkBox_ch2.Checked ? "1" : "0";
                    string ch3recieve = checkBox_ch3.Checked ? "1" : "0";
                    string ch4recieve = checkBox_ch4.Checked ? "1" : "0";
                    string tr = ch4recieve + "0" + ch3recieve + "0" + ch2recieve + "0" + ch1recieve + "0";
                    string ta = new string('0', 24);
                    string ra = ch4Shuaijian + ch3Shuaijian + ch2Shuaijian + ch1Shuaijian;
                    string tp = new string('0', 24);
                    string rp = ch4Yixiang + ch3Yixiang + ch2Yixiang + ch1Yixiang;
                    string model = "00000001";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });
                    mainForm.LogToConsole(ch1Yixiang);
                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("手动发码|接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}");
                }
                else if (radioButton3.Checked)
                {
                    mainForm.LogToConsole("负载模式");
                    string tr = new string('0', 8);
                    string ta = new string('0', 24);
                    string tp = new string('0', 24);
                    string ra = new string('0', 24);
                    string rp = new string('0', 24);
                    string model = "00000010";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 03 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });

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
            catch (Exception ex)
            {
                MessageBox.Show("UDP发送失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //operateLog_DAL.InsertOperateLog_DT("UDP发送失败", ex.ToString());
            }

        }
    }
}
