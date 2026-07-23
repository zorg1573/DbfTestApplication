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
using System.Runtime.CompilerServices;

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
        const int ApplicationHeaderLength = 40;
        const int ModelValueLength = 4;
        const int ReservedValueLength = 8;
        const int CodeValueLength = 41;
        const int ControlCodeByteLength = 28;
        const int TableHeaderBitCount = 6;           // 表头[5:0]
        const int IfAttenuationBitCount = 10;        // ATT[9:0]
        const int ChannelFieldBitCount = 6;
        const int ChannelControlBitCount = 26;       // RxEn + 4×6bit + TxEn
        const int ChannelCount = 8;
        const int ControlBitCount = TableHeaderBitCount + IfAttenuationBitCount + ChannelControlBitCount * ChannelCount;
        string DefaultTableHeaderBits = "010101"; // 表头[5:0] 默认值
        const int ControlCodeStartIndex = 13;
        private Main_Form mainForm;
        string ch1Yixiang = "000000";
        string ch2Yixiang = "000000";
        string ch3Yixiang = "000000";
        string ch4Yixiang = "000000";
        string ch5Yixiang = "000000";
        string ch6Yixiang = "000000";
        string ch7Yixiang = "000000";
        string ch8Yixiang = "000000";
        string zhongpinShuaijian = "0000000000";
        string ch1Shuaijian = "000000";
        string ch2Shuaijian = "000000";
        string ch3Shuaijian = "000000";
        string ch4Shuaijian = "000000";
        string ch5Shuaijian = "000000";
        string ch6Shuaijian = "000000";
        string ch7Shuaijian = "000000";
        string ch8Shuaijian = "000000";
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

            // 限制相关文本框长度为固定 6 位
            ch1_yixiang_textBox.MaxLength = 6;
            ch2_yixiang_textBox.MaxLength = 6;
            ch3_yixiang_textBox.MaxLength = 6;
            ch4_yixiang_textBox.MaxLength = 6;
            ch5_yixiang_textBox.MaxLength = 6;
            ch6_yixiang_textBox.MaxLength = 6;
            ch7_yixiang_textBox.MaxLength = 6;
            ch8_yixiang_textBox.MaxLength = 6;
            shuaijian_textBox.MaxLength = IfAttenuationBitCount;
            ch1_shuaijian_textBox.MaxLength = 6;
            ch2_shuaijian_textBox.MaxLength = 6;
            ch3_shuaijian_textBox.MaxLength = 6;
            ch4_shuaijian_textBox.MaxLength = 6;
            ch5_shuaijian_textBox.MaxLength = 6;
            ch6_shuaijian_textBox.MaxLength = 6;
            ch7_shuaijian_textBox.MaxLength = 6;
            ch8_shuaijian_textBox.MaxLength = 6;

            comboBox_mode.SelectedIndex = 0;
        }
        private void ManualSend_Form_Load(object sender, EventArgs e)
        {
            GetAddress();
        }
        private void GetAddress()
        {
            try
            {
                string filePath = "DeviceAddress_DBF.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                data.TryGetValue("pc_mac_textBox", out object pcMac);
                if (pcMac != null)
                {
                    srcMacStr = MacToHexString(pcMac.ToString());
                    srcMacAddress = pcMac.ToString();
                }

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

                headValue = BuildApplicationHeader();
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

                // 校验文本框长度必须为 6 位（如果不为空）
                if (!string.IsNullOrEmpty(ch1_yixiang_textBox.Text) && ch1_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道1移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch2_yixiang_textBox.Text) && ch2_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道2移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch3_yixiang_textBox.Text) && ch3_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道3移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch4_yixiang_textBox.Text) && ch4_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道4移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch5_yixiang_textBox.Text) && ch5_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道5移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch6_yixiang_textBox.Text) && ch6_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道6移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch7_yixiang_textBox.Text) && ch7_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道7移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch8_yixiang_textBox.Text) && ch8_yixiang_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道8移相输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(shuaijian_textBox.Text) && shuaijian_textBox.Text.Length != IfAttenuationBitCount)
                {
                    MessageBox.Show($"中频衰减输入必须为 {IfAttenuationBitCount} 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch1_shuaijian_textBox.Text) && ch1_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道1衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch2_shuaijian_textBox.Text) && ch2_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道2衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch3_shuaijian_textBox.Text) && ch3_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道3衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch4_shuaijian_textBox.Text) && ch4_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道4衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch5_shuaijian_textBox.Text) && ch5_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道5衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch6_shuaijian_textBox.Text) && ch6_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道6衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch7_shuaijian_textBox.Text) && ch7_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道7衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!string.IsNullOrEmpty(ch8_shuaijian_textBox.Text) && ch8_shuaijian_textBox.Text.Length != 6)
                {
                    MessageBox.Show("通道8衰减输入必须为 6 位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    zhongpinShuaijian = shuaijian_textBox.Text;
                }
                if (ch1_shuaijian_textBox.Text != "")
                {
                    ch1Shuaijian = ch1_shuaijian_textBox.Text;
                }
                if (ch2_shuaijian_textBox.Text != "")
                {
                    ch2Shuaijian = ch2_shuaijian_textBox.Text;
                }
                if (ch3_shuaijian_textBox.Text != "")
                {
                    ch3Shuaijian = ch3_shuaijian_textBox.Text;
                }
                if (ch4_shuaijian_textBox.Text != "")
                {
                    ch4Shuaijian = ch4_shuaijian_textBox.Text;
                }
                if (ch5_shuaijian_textBox.Text != "")
                {
                    ch5Shuaijian = ch5_shuaijian_textBox.Text;
                }
                if (ch6_shuaijian_textBox.Text != "")
                {
                    ch6Shuaijian = ch6_shuaijian_textBox.Text;
                }
                if (ch7_shuaijian_textBox.Text != "")
                {
                    ch7Shuaijian = ch7_shuaijian_textBox.Text;
                }
                if (ch8_shuaijian_textBox.Text != "")
                {
                    ch8Shuaijian = ch8_shuaijian_textBox.Text;
                }

                string[] phaseBits = { ch1Yixiang, ch2Yixiang, ch3Yixiang, ch4Yixiang, ch5Yixiang, ch6Yixiang, ch7Yixiang, ch8Yixiang };
                string[] attenuationBits = { ch1Shuaijian, ch2Shuaijian, ch3Shuaijian, ch4Shuaijian, ch5Shuaijian, ch6Shuaijian, ch7Shuaijian, ch8Shuaijian };
                string[] zeroBits = Enumerable.Repeat("000000", 8).ToArray();

                if (comboBox_mode.SelectedIndex == 0)
                {
                    DefaultTableHeaderBits = "010101";
                }
                else
                {
                    DefaultTableHeaderBits = "101010";
                }

                if (radioButton2.Checked)
                {
                    mainForm.LogToConsole("开始发射测试"); //接收开关 发射移相 接收移相 发射衰减 接收衰减 发射开关
                    bool[] rxDisable = { true, true, true, true, true, true, true, true };
                    bool[] txDisable = { true, true, true, true, true, true, true, true };
                    bool[] txEnable = GetChannelCheckedStates();
                    modelValue = StringToByteArray("01 03 01 00");

                    var codeValue = GenerateCodeValueFromBits(zhongpinShuaijian, phaseBits, zeroBits, attenuationBits, zeroBits, rxDisable, txEnable);

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("手动发码|发射测试", $"{ch1send},{ch2send},{ch3send},{ch4send}");
                }
                else if (radioButton1.Checked)
                {
                    mainForm.LogToConsole("开始接收测试");
                    bool[] rxEnable = GetChannelCheckedStates();
                    bool[] rxDisable = { true, true, true, true, true, true, true, true };
                    bool[] txDisable = { true, true, true, true, true, true, true, true };
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(zhongpinShuaijian, zeroBits, phaseBits, zeroBits, attenuationBits, rxEnable, txDisable);
                    mainForm.LogToConsole(ch1Yixiang);
                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    //operateLog_DAL.InsertOperateLog_DT("手动发码|接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}");
                }
                else if (radioButton3.Checked)
                {
                    mainForm.LogToConsole("负载模式");
                    bool[] rxDisable = { true, true, true, true, true, true, true, true };
                    bool[] txDisable = { true, true, true, true, true, true, true, true };
                    modelValue = StringToByteArray("01 03 03 00");
                    var codeValue = GenerateCodeValueFromBits(zhongpinShuaijian, zeroBits, zeroBits, zeroBits, zeroBits, rxDisable, txDisable);

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

                EnsureApplicationHeader();
                ValidatePayloadParts(this.headValue, modelValue, emptyValue, codeValue);
                var payload = this.headValue.Concat(modelValue).Concat(emptyValue).Concat(codeValue).ToArray();

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

        private byte[] BuildApplicationHeader()
        {
            if (string.IsNullOrEmpty(srcMacStr))
                return null;

            string dstMacHex = string.IsNullOrWhiteSpace(dstMacStr)
                ? "00 0a 35 01 fe c0"
                : MacToHexString(dstMacStr);
            string srcIpHex = IpToHexString(string.IsNullOrWhiteSpace(srcIpStr) ? "192.168.0.3" : srcIpStr);
            string dstIpHex = IpToHexString(string.IsNullOrWhiteSpace(dstIpStr) ? "192.168.0.2" : dstIpStr);

            // data_decode 只依赖这个内嵌头尾部的 81 70 同步字，内嵌头固定 40 字节。
            return StringToByteArray($"{dstMacHex} {srcMacStr} 08 00 45 00 b4 68 00 00 80 11 00 00 {srcIpHex} {dstIpHex} 1f 90 1f 90 00 23 81 70");
        }

        private void EnsureApplicationHeader()
        {
            if (headValue != null && headValue.Length == ApplicationHeaderLength)
                return;

            GetAddress();
            headValue = BuildApplicationHeader();
            if (headValue == null || headValue.Length != ApplicationHeaderLength)
                throw new InvalidOperationException("无法构建内嵌头，请先在设备地址设置中配置 PC/FPGA 的 MAC 和 IP。");
        }

        private static string IpToHexString(string ip)
        {
            return string.Join(" ", IPAddress.Parse(ip).GetAddressBytes().Select(b => b.ToString("x2")));
        }

        private static string MacToHexString(string mac)
        {
            return mac.Trim().Replace(":", " ").Replace("-", " ").ToLowerInvariant();
        }

        private static void ValidatePayloadParts(byte[] headValue, byte[] modelValue, byte[] emptyValue, byte[] codeValue)
        {
            if (headValue == null || headValue.Length != ApplicationHeaderLength)
                throw new ArgumentException($"内嵌头应为 {ApplicationHeaderLength} 字节，并以 81 70 结束。");
            if (headValue[ApplicationHeaderLength - 2] != 0x81 || headValue[ApplicationHeaderLength - 1] != 0x70)
                throw new ArgumentException("内嵌头末尾必须是同步字 81 70。");
            if (modelValue == null || modelValue.Length != ModelValueLength)
                throw new ArgumentException($"modelValue 应为 {ModelValueLength} 字节。");
            if (emptyValue == null || emptyValue.Length != ReservedValueLength)
                throw new ArgumentException($"保留字段应为 {ReservedValueLength} 字节。");
            if (codeValue == null || codeValue.Length != CodeValueLength)
                throw new ArgumentException($"codeValue 应为 {CodeValueLength} 字节。");
        }

        private bool[] GetChannelCheckedStates()
        {
            return new[]
            {
                !checkBox_ch1.Checked,
                !checkBox_ch2.Checked,
                !checkBox_ch3.Checked,
                !checkBox_ch4.Checked,
                !checkBox_ch5.Checked,
                !checkBox_ch6.Checked,
                !checkBox_ch7.Checked,
                !checkBox_ch8.Checked
            };
        }

        byte[] GenerateCodeValueFromBits(string ifAttenuationBits, string[] txPhaseBits, string[] rxPhaseBits, string[] txAttenuationBits, string[] rxAttenuationBits, bool[] rxEnable, bool[] txEnable)
        {
            ValidateChannelArrays(txPhaseBits, rxPhaseBits, txAttenuationBits, rxAttenuationBits, rxEnable, txEnable);

            List<int> controlBits = new List<int>(ControlBitCount);
            // 28字节控制码顺序: 表头[5:0] + ATT[9:0] + 26×8
            AppendBitString(controlBits, ToWireBitOrder(DefaultTableHeaderBits), TableHeaderBitCount, "表头");
            AppendBitString(controlBits, PadIfAttenuationBits(ifAttenuationBits), IfAttenuationBitCount, "ATT[9:0]");

            for (int i = 0; i < ChannelCount; i++)
            {
                AppendEnableBit(controlBits, rxEnable[i]);
                AppendBitString(controlBits, txPhaseBits[i], ChannelFieldBitCount, $"通道 {i + 1} 发射移相");
                AppendBitString(controlBits, rxPhaseBits[i], ChannelFieldBitCount, $"通道 {i + 1} 接收移相");
                AppendBitString(controlBits, txAttenuationBits[i], ChannelFieldBitCount, $"通道 {i + 1} 发射衰减");
                AppendBitString(controlBits, rxAttenuationBits[i], ChannelFieldBitCount, $"通道 {i + 1} 接收衰减");
                AppendEnableBit(controlBits, txEnable[i]);
            }

            if (controlBits.Count != ControlBitCount)
                throw new ArgumentException($"控制字应为{ControlBitCount}位（{ControlCodeByteLength}字节），但现在是 {controlBits.Count} 位");

            byte[] codeBytes = new byte[CodeValueLength];
            for (int i = 0; i < controlBits.Count; i++)
            {
                if (controlBits[i] != 1)
                    continue;

                int byteIndex = ControlCodeStartIndex + i / 8;
                if (byteIndex >= CodeValueLength)
                    throw new ArgumentException($"控制字超出 codeValue 范围：需要字节索引 {byteIndex}，但 codeValue 长度为 {CodeValueLength}。");

                codeBytes[byteIndex] |= (byte)(1 << (i % 8));
            }

            return codeBytes;
        }

        static string PadIfAttenuationBits(string bits)
        {
            string normalized = (bits ?? string.Empty).Replace(" ", "");
            if (normalized.Length > IfAttenuationBitCount)
                throw new ArgumentException($"中频衰减应为 {IfAttenuationBitCount} 位，但提供了 {normalized.Length} 位");

            return normalized.PadRight(IfAttenuationBitCount, '0');
        }

        static void ValidateChannelArrays(string[] txPhaseBits, string[] rxPhaseBits, string[] txAttenuationBits, string[] rxAttenuationBits, bool[] rxEnable, bool[] txEnable)
        {
            if (txPhaseBits.Length != 8 || rxPhaseBits.Length != 8 || txAttenuationBits.Length != 8 || rxAttenuationBits.Length != 8 || rxEnable.Length != 8 || txEnable.Length != 8)
                throw new ArgumentException("通道控制参数必须包含8个通道。");
        }

        static string ToWireBitOrder(string msbToLsbBits)
        {
            char[] reversed = msbToLsbBits.Replace(" ", "").ToCharArray();
            Array.Reverse(reversed);
            return new string(reversed);
        }

        static void AppendEnableBit(List<int> bits, bool enabled)
        {
            bits.Add(enabled ? 1 : 0);
        }

        static void AppendBitString(List<int> bits, string bitString, int expectedLength, string fieldName)
        {
            string normalized = bitString.Replace(" ", "");
            if (normalized.Length != expectedLength)
                throw new ArgumentException($"{fieldName} 应为 {expectedLength} 位，但提供了 {normalized.Length} 位");

            foreach (char bit in normalized)
            {
                if (bit != '0' && bit != '1')
                    throw new ArgumentException($"{fieldName} 只能包含0或1。");

                // UI字符串从左到右按协议低位到高位填写，协议低位映射到字节LSB并先串行发送。
                bits.Add(bit == '1' ? 1 : 0);
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
