using AxDSOFramer;
using DbfTest.DAL;
using DbfTest.FUNCTION;
using DbfTest.PAGE;
using DbfTest.PAGE.WaitForm;
using ExcelDataReader;
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

namespace DbfTest
{
    public partial class Main_Form : MetroForm
    {
        #region 全局变量
        //DeviceAddress_KU.json
        string chargeAddress = "";
        string vnaAddress = "";
        string gonglvAddress = "";
        string xinhaoAddress = "";
        string xinhaoBenzhenAddress = "";
        string pinpuAddress = "";

        //TestSet_KU.json
        double power = -1;
        double powerJie = -1;
        double startFreq = -1;
        double stopFreq = -1;
        int pointCount = -1;
        double ch1_vol = -1;
        double ch1_cur = -1;
        double ch2_vol = -1;
        double ch2_cur = -1;

        //DeviceFiles_KU.json
        string excelPath = "";
        string vnaFilePath = "";
        string excelMobanPath = "";
        string buchangFilePath = "";
        string shiwangChaSunPath = ""; //矢网差损文件路径
        string pinpuZhupuStatePath = ""; //频谱分析仪主谱状态文件
        string pinpuDaiwaiyizhiPath = ""; //频谱分析仪带外抑制状态文件
        string sanjieJiaotiaoPath = ""; //三阶交调文件路径
        #endregion

        public Main_Form()
        {
            InitializeComponent();
            this.Load += Main_Form_Load;
        }
        private void Main_Form_Load(object sender, EventArgs e)
        {
            testType_comboBox.SelectedIndex = 0;
            operator_textBox.Text = "操作员";
            componentName_textBox.Text = "KU";
            GetAddress();
            GetDeviceFilesJson();
            GetTestSetNewJson();
            InitializeDSO();

        }

        #region 发射测试
        private async void button8_Click(object sender, EventArgs e)
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(operator_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            WritePersonToAllSheets();
            LogToConsole("开始发射测试...");
            await ChargeSendPowerON(); // 发射加电

            await changeToFuZaiTai();
            await Task.Delay(500);
            I_DQ5 = await GetCurrent(2);
            await RecieveTestUDP();
            await Task.Delay(500);
            I_R5 = await GetCurrent(2);

            await LoadGonglvState(); // 调用功率计文件
            await SendTestUDP(); //FPGA发包
            await Task.Delay(500); // 延时保证设备稳定
                                   //await WriteFreqArray();

            string ch = "";
            string testType = testType_comboBox.Text;
            int chNum = 0;
            if (ch1_checkBox.Checked)
            {
                ch = $"通道1-{testType}";
                chNum = 1;
            }
            if (ch2_checkBox.Checked)
            {
                ch = $"通道2-{testType}";
                chNum = 2;
            }
            if (ch3_checkBox.Checked)
            {
                ch = $"通道3-{testType}";
                chNum = 3;
            }
            if (ch4_checkBox.Checked)
            {
                ch = $"通道4-{testType}";
                chNum = 4;
            }

            string sheetName = $"测试结果{chNum}";

            //GetTestSetNewJson();
            if (pointCount <= 0)
            {
                MessageBox.Show("请先设置点数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int num = 0;
            progressBar1.Maximum = pointCount;
            progressBar1.Value = 0;

            string[] freqArray = new string[pointCount];
            string[] pulsePowerString = new string[pointCount];
            string[] xiaolvString = new string[pointCount];
            string[] dingJiang = new string[pointCount]; //顶降
            string[] shangshengyan = new string[pointCount];
            string[] xiajiangyan = new string[pointCount];

            string[] compensatedPowerString = new string[pointCount];

            var xinhaoDevice = new ScpiDevice();
            var powerMeter = new ScpiDevice();

            bool deviceConnected = await xinhaoDevice.ConnectAsync(xinhaoAddress);
            bool pmConnected = await powerMeter.ConnectAsync(gonglvAddress);

            if (!deviceConnected || !pmConnected)
            {
                LogToConsole("设备连接失败");
                return;
            }

            try
            {
                await xinhaoDevice.SetPower(power);
                await xinhaoDevice.QueryOpc();
                await xinhaoDevice.EnableOutput();

                LogToConsole("获取功率计数据");
                freqArray = GetFilterFreqArray(pointCount);
                double step = (stopFreq - startFreq) / (pointCount - 1);
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    /*                    await xinhaoDevice.SetFrequency(freqHz);
                                        await xinhaoDevice.QueryOpc();
                                        await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                                        await xinhaoBenzhenDevice.QueryOpc();*/
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();
                    await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.SendCommandAsync($":SENS:FREQ {freqHz}");
                    await powerMeter.SendCommandAsync(":INIT:IMM");         // 开始测量
                    await powerMeter.SendCommandAsync("*WAI");              // 等待测量完成
                    await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.ReadPulsePowerArrayAsync(); // 预读取一次丢弃

                    // 读取功率计峰值功率（dBm）
                    double[] pulsePower = await powerMeter.ReadPulsePowerArrayAsync();
                    double positiveDur = await powerMeter.GetPositiveDuration() * 1e6 ?? -1;
                    double negativeDur = await powerMeter.GetNegativeDuration() * 1e6 ?? -1;
                    dingJiang[i] = pulsePower[6].ToString();
                    shangshengyan[i] = positiveDur.ToString();
                    xiajiangyan[i] = negativeDur.ToString();

                    double compensatedPower = pulsePower[0];
                    double PowerWatt = dBmToWatt(compensatedPower); // dBm 转 W
                    compensatedPowerString[i] = compensatedPower.ToString();

                    I_T85 = await GetCurrent(1);
                    I_T5 = await GetCurrent(2);

                    double fenmu1 = ch1_vol * I_T85;
                    double fenmu2 = ch2_vol * (I_T5 - 0.75 * I_DQ5);
                    double fenmu3 = 0.8 * ch2_vol * (I_R5 - 0.75 * I_DQ5);

                    double chargePower = fenmu1 + fenmu2 + fenmu3;
                    xiaolvString[i] = PowerWatt * 0.2 / chargePower * 10 + "%"; // 计算效率百分比

                    num++;
                    progressBar1.Text = ((double)num / pointCount * 100).ToString("f2") + "%";
                    progressBar1.Refresh();

                    //main_DAL.UpdateTestDataFreq_DT(ch, componentName, double.Parse(freqArray[i]), double.Parse(compensatedPowerString[i]));
                }
                await xinhaoDevice.DisableOutput();
                //WriteArrayToExcelColumn(freqArray, 7, ch);
                //WriteArrayToExcelColumn(compensatedPowerString, 8, ch);
                WritePeakPowerToMatchingFrequencyRows_New(freqArray, compensatedPowerString, xiaolvString, dingJiang, sheetName);

                LogToConsole("Excel写入完成");
            }
            catch (Exception ex)
            {
                LogToConsole($"测量异常：{ex.Message}");
            }
            finally
            {

                //await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                //mod_checkBox.Checked = false;
                xinhaoDevice.Disconnect();
                powerMeter.Disconnect();
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("发射测试已完成");
            }
        }
        /// <summary>
        /// 发射移相
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 

        private async void button9_Click(object sender, EventArgs e)
        {
            LogToConsole("开始移相精度测试...");
            WritePersonToAllSheets();
            //LoadVNAState(); // 调用矢网文件
            await ChargeSendPowerON(); // 发射加电

            string ch = "";
            string testType = testType_comboBox.Text;
            int chNum = 0;

            int num = 0;
            progressBar1.Maximum = 64;
            progressBar1.Value = 0;


            if (ch1_checkBox.Checked)
            {
                ch = $"通道1-{testType}";
                chNum = 1;
            }
            if (ch2_checkBox.Checked)
            {
                ch = $"通道2-{testType}";
                chNum = 2;
            }
            if (ch3_checkBox.Checked)
            {
                ch = $"通道3-{testType}";
                chNum = 3;
            }
            if (ch4_checkBox.Checked)
            {
                ch = $"通道4-{testType}";
                chNum = 4;
            }

            string sheetName = $"测试结果{chNum}";

            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            //await scpiDevice.LoadStateFile("C:\\Users\\IFET\\Desktop\\Kufasheyixiang20251118.csa");
            await scpiDevice.LoadStateFile("kuyixiang.csa");
            await scpiDevice.EnableOutput();
            //await SendTestUDP(0, "移相"); // FPGA发码
            //await Task.Delay(1000);           // 等待设备稳定
            //await scpiDevice.SetNormalize_Send();
            List<double[]> unwrappedPhases = new List<double[]>();
            double[] previousPhase = null;
            double[] phaseOffset = null;
            double[] zeroPhase = null;
            for (int idx = 0; idx < 64; idx++)
            {

                await SendTestUDP(idx, "移相"); // FPGA发码

                await Task.Delay(500);           // 等待设备稳定
                await scpiDevice.ScanOnce0();
                await Task.Delay(500);
                string[] initial = await scpiDevice.GetPhase_Send();    // 初相（°）

                // 转换为 double[]
                double[] currentPhase = initial.Select(s =>
                {
                    double.TryParse(s, out double v);
                    return v;
                }).ToArray();
                /*                if (idx == 0)
                                {
                                    zeroPhase = currentPhase;
                                }*/

                if (previousPhase == null)
                {
                    previousPhase = currentPhase;
                    phaseOffset = new double[currentPhase.Length];
                    unwrappedPhases.Add(currentPhase.ToArray());
                }
                else
                {
                    double[] unwrapped = new double[currentPhase.Length];

                    for (int j = 0; j < currentPhase.Length; j++)
                    {
                        double diff = currentPhase[j] - previousPhase[j];

                        if (diff > 180)
                            phaseOffset[j] -= 360;
                        else if (diff < -180)
                            phaseOffset[j] += 360;

                        unwrapped[j] = currentPhase[j] + phaseOffset[j];
                    }

                    unwrappedPhases.Add(unwrapped);
                    previousPhase = currentPhase;
                }
                //unwrappedPhases.Add(currentPhase.ToArray());

                num++;
                progressBar1.Value += 1;
                label6.Text = ((double)num / 64 * 100).ToString("f2") + "%";
                label6.Refresh();
            }

            await scpiDevice.DisableOutput();
            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电

            // 写入解包后的初相（第 i + 2 列）
            for (int i = 0; i < unwrappedPhases.Count; i++)
            {
                string[] phaseStrings = unwrappedPhases[i].Select(v => v.ToString()).ToArray();
                WriteArrayToExcelColumn_New(phaseStrings, i + 2, $"发射通道相移精度测试结果{chNum}");
            }
            SubtractStandardAndWriteResult($"发射通道相移精度测试结果{chNum}");
            CalculatePhaseAccuracyAndWriteToExcel($"发射通道相移精度测试结果{chNum}", chNum);
        }
        /// <summary>
        /// 杂散抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button10_Click(object sender, EventArgs e)
        {
            var signalGen = new ScpiDevice();
            var pinpuDevice = new ScpiDevice();
            try
            {
                string ch = "";
                string testType = testType_comboBox.Text;
                int chNum = 0;

                int num = 0;
                progressBar1.Maximum = pointCount*3;
                progressBar1.Value = 0;

                if (ch1_checkBox.Checked)
                {
                    ch = $"通道1-{testType}";
                    chNum = 1;
                }
                if (ch2_checkBox.Checked)
                {
                    ch = $"通道2-{testType}";
                    chNum = 2;
                }
                if (ch3_checkBox.Checked)
                {
                    ch = $"通道3-{testType}";
                    chNum = 3;
                }
                if (ch4_checkBox.Checked)
                {
                    ch = $"通道4-{testType}";
                    chNum = 4;
                }

                string sheetName = $"测试结果{chNum}";

                await ChargeSendPowerON(); // 发射加电
                await SendTestUDP(); //FPGA发包
                await Task.Delay(500); // 延时保证设备稳定
                //await WriteFreqArray();
                string[] freqArray = GetFilterFreqArray(pointCount);
                string[] fasheYizhi = new string[pointCount]; //发射抑制
                double[] zhupuP = new double[pointCount];
                double[] dwyzP = new double[pointCount];

                bool sgConnected = await signalGen.ConnectAsync(xinhaoAddress);
                bool pinpuConnected = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!sgConnected || !pinpuConnected)
                {
                    LogToConsole("连接失败：信号源或频谱无法连接");
                    return;
                }
                await signalGen.SetPower(power);
                await signalGen.QueryOpc();
                await signalGen.EnableOutput(); // 打开信号源输出

                // 1. 加载状态文件（仅一次）
                await pinpuDevice.LoadPinpuStateAsync(pinpuZhupuStatePath);
                await Task.Delay(2000); // 延时保证设备稳定
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqGHz = double.Parse(freqArray[i]);

                    await signalGen.SetFrequency(freqHz);
                    await signalGen.QueryOpc();

                    await Task.Delay(1000); // 延时保证设备稳定

                    // 2. 设置频率范围
                    double center = freqHz;
                    double start = center - (0.2 * 1e9);
                    double stop = center + (0.2 * 1e9);
                    await pinpuDevice.SetStartFrequencyAsync(start);
                    await pinpuDevice.SetStopFrequencyAsync(stop);
                    await pinpuDevice.SetCenterFrequencyAsync(center);

                    // 4. 启用 Marker 并设置频率位置
                    await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");

                    double power = double.NaN;

                    for (int j = 0; j < 10; j++)
                    {
                        // 将 marker 设置为最大点
                        await pinpuDevice.SendCommandAsync(":CALC:MARK1:MAX");
                        await Task.Delay(200); // 让设备处理
                                               // 读取 Marker 的功率值
                        power = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                        // 判断是否为有效功率
                        if (!double.IsNaN(power) && power > -10 && power < 10)
                            break;

                        await Task.Delay(200); // 等待波形稳定
                    }
                    zhupuP[i] = power;

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    label6.Refresh();
                }
                // 1. 加载状态文件
                await pinpuDevice.LoadPinpuStateAsync(pinpuDaiwaiyizhiPath);
                await Task.Delay(2000); // 延时保证设备稳定
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqGHz = double.Parse(freqArray[i]);

                    await signalGen.SetFrequency(freqHz);
                    await signalGen.QueryOpc();

                    await Task.Delay(1000); // 延时保证设备稳定

                    // 2. 开启 marker 并设置位置
                    await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                    await pinpuDevice.SendCommandAsync(":CALC:MARK2:STATE ON");
                    //double freq1 = freq - 0.3 * 1e9;
                    double rbw = 300 * 1e6;
                    double freq1 = freqHz - rbw;
                    double freq2 = freqHz + rbw;

                    double power1 = double.NaN;
                    double power2 = double.NaN;

                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {freq1}");
                    await pinpuDevice.SendCommandAsync($":CALC:MARK2:X {freq2}");
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    power1 = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                    power2 = await pinpuDevice.ReadMarkerPowerAsync(2) ?? double.NaN;
                    /*                  for (int j = 0; j < 10; j++)
                                      {
                                          await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {freq1}");
                                          await pinpuDevice.SendCommandAsync($":CALC:MARK2:X {freq2}");
                                          await Task.Delay(200); // 让设备处理
                                                                 // 读取 Marker 的功率值
                                          power1 = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                                          power2 = await pinpuDevice.ReadMarkerPowerAsync(2) ?? double.NaN;
                                          // 判断是否为有效功率
                                          if (!double.IsNaN(power1) && power1 > -100 && power1 < 100)
                                              break;

                                          await Task.Delay(200); // 等待波形稳定
                                      }*/
                    dwyzP[i] = Math.Max(power1, power2);

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    label6.Refresh();
                }
                for (int i = 0; i < pointCount; i++)
                {
                    double result = zhupuP[i] - dwyzP[i];
                    fasheYizhi[i] = result.ToString("F3");
                    LogToConsole("发射抑制测试:" + freqArray[i] + ": " + zhupuP[i] + " - " + dwyzP[i] + " = " + result);

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                //fasheYizhi = await GetFasheyizhiAsync(freqArray);
                WriteFasheyizhiToMatchingFrequencyRows(freqArray, fasheYizhi, sheetName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射抑制测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射抑制测试失败", ex.ToString(), operator_textBox.Text);
            }
            finally
            {
                await signalGen.DisableOutput(); // 安全关闭输出
                //await signalGen.ModOFF();
                //rf_checkBox.Checked = false;
                //mod_checkBox.Checked = false;
                signalGen.Disconnect();
                pinpuDevice.Disconnect();
                await CloseCharge(); // 电源关电
            }
        }
        #endregion

        #region 接收测试
        /// <summary>
        /// 接收增益
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button11_Click(object sender, EventArgs e)
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(operator_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始接收测试...");
            WritePersonToAllSheets();
            await ChargeRecievePowerON(); // 接收加电
            await RecieveTestUDP(); //FPGA发包
            await Task.Delay(500); // 延时保证设备稳定

            //var compensationTable = LoadCompensationTable(buchangFilePath, "增益");
            ScpiDevice vnaDevice = new ScpiDevice();

            try
            {
                string ch = "";

                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;
                int chNum = 0;
                if (ch1_checkBox.Checked)
                {
                    ch = $"通道1-{testType}";
                    chNum = 1;
                }
                if (ch2_checkBox.Checked)
                {
                    ch = $"通道2-{testType}";
                    chNum = 2;
                }
                if (ch3_checkBox.Checked)
                {
                    ch = $"通道3-{testType}";
                    chNum = 3;
                }
                if (ch4_checkBox.Checked)
                {
                    ch = $"通道4-{testType}";
                    chNum = 4;
                }

                string sheetName = $"测试结果{chNum}";

                bool connected = await vnaDevice.ConnectAsync(vnaAddress);
                if (!connected)
                {
                    LogToConsole("矢网连接失败");
                    return;
                }

                await vnaDevice.LoadStateFile("kuzengyixiangwei.csa");
                //await vnaDevice.EnableOutput();
                await Task.Delay(500);
                await vnaDevice.ScanOnce();
                await Task.Delay(500);
                string[] gain = await vnaDevice.GetGainStringAsync();               // 增益（dB）
                string[] initial = await vnaDevice.GetInitialPhaseStringAsync();    // 初相（°）
                string[] inputVswr = await vnaDevice.GetInputVSWRStringAsync();     // 输入驻波比
                string[] outputVswr = await vnaDevice.GetOutputVSWRStringAsync();   // 输出驻波比

                string[] gain21 = ExtractStep100MHz(gain);
                string[] initial21 = ExtractStep100MHz(initial);
                string[] inputVswr21 = ExtractStep100MHz(inputVswr);
                string[] outputVswr21 = ExtractStep100MHz(outputVswr);

                WriteArrayToExcelColumn(gain21, 2, sheetName);
                WriteArrayToExcelColumn(initial21, 3, sheetName);
                WriteArrayToExcelColumn(inputVswr21, 4, sheetName);
                WriteArrayToExcelColumn(outputVswr21, 5, sheetName);
                LogToConsole("写入Excel完成");
            }
            catch (Exception ex)
            {
                LogToConsole("接收测试出错: " + ex.Message);
            }
            finally
            {
                //await vnaDevice.DisableOutput();
                vnaDevice.Disconnect(); // 释放资源
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("接收测试已完成");
            }
        }
        private string[] ExtractStep100MHz(string[] fullArray)
        {
            List<string> result = new List<string>();

            double startGHz = 15.0;
            double endGHz = 17.0;
            double fullStepGHz = 0.01;   // 原始步进
            double targetStepGHz = 0.1;  // 目标步进（0.1GHz）

            int totalPoints = fullArray.Length; // 201

            for (double freq = startGHz; freq <= endGHz + 1e-9; freq += targetStepGHz)
            {
                double indexD = (freq - startGHz) / fullStepGHz;
                int index = (int)Math.Round(indexD);

                if (index >= 0 && index < totalPoints)
                    result.Add(fullArray[index]);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 噪声采集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
                {
                    MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string ch = "";
                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;

                //进度条
                int num = 0;
                progressBar1.Maximum = pointCount;
                progressBar1.Value = 0;

                if (ch1_checkBox.Checked)
                {
                    ch = $"通道1-{testType}";
                }
                if (ch2_checkBox.Checked)
                {
                    ch = $"通道2-{testType}";
                }
                if (ch3_checkBox.Checked)
                {
                    ch = $"通道3-{testType}";
                }
                if (ch4_checkBox.Checked)
                {
                    ch = $"通道4-{testType}";
                }
                if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
                {
                    MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                string[] freqArray = new string[pointCount];
                string[] NFData = new string[pointCount];
                double step = 0;
                step = (stopFreq - startFreq) / (pointCount - 1);

                await pinpuDevice.SendCommandAsync(":MMEM:LOAD:STAT 1,'C:/R_S/Instr/user/QuickSave/kuzaosheng.dfl'");
                await pinpuDevice.SendCommandAsync("*OPC");

                string[] data = await pinpuDevice.GetZaoshengPointAsync();

                WriteZaoshengToMatchingFrequencyRows(freqArray, data, "测试结果");
                LogToConsole("噪声采集");

                pinpuDevice.Disconnect();
                await CloseCharge();
                await CloseFPGA();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"噪声采集失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("噪声采集失败", ex.ToString(), operator_textBox.Text);
            }
        }
        /// <summary>
        /// 压缩点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
                {
                    MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string ch = "";
                string testType = testType_comboBox.Text;
                int chNum = 0;

                //进度条
                int num = 0;
                progressBar1.Maximum = pointCount*3;
                progressBar1.Value = 0;

                if (ch1_checkBox.Checked)
                {
                    ch = $"通道1-{testType}";
                    chNum = 1;
                }
                if (ch2_checkBox.Checked)
                {
                    ch = $"通道2-{testType}";
                    chNum = 2;
                }
                if (ch3_checkBox.Checked)
                {
                    ch = $"通道3-{testType}";
                    chNum = 3;
                }
                if (ch4_checkBox.Checked)
                {
                    ch = $"通道4-{testType}";
                    chNum = 4;
                }

                string sheetName = $"测试结果{chNum}";

                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP();       // FPGA发包
                await Task.Delay(500);     // 延时保证设备稳定
                //await WriteFreqArray();

                string[] yasuodian = new string[pointCount];
                string[] pset = new string[pointCount];
                string[] freqArray = GetFilterFreqArray(pointCount);
                LogToConsole("压缩点测试");
                var compensationTable = LoadCompensationTable(buchangFilePath, "压缩点");
                ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
                ScpiDevice xinhaoDevice = new ScpiDevice();
                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await xinhaoBenzhenDevice.ConnectAsync(vnaAddress);
                bool connected2 = await xinhaoDevice.ConnectAsync(xinhaoAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await pinpuDevice.LoadPinpuStateAsync("C:\\R_S\\Instr\\user\\QuickSave\\dbfzhupu.dfl");
                double center = 175 * 1e6;
                //double start = center - (100 * 1e6);
                //double stop = center + (100 * 1e6);
                //await pinpuDevice.SetStartFrequencyAsync(start);
                //await pinpuDevice.SetStopFrequencyAsync(stop);
                //await pinpuDevice.SetCenterFrequencyAsync(center);
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                double markPower = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");

                await xinhaoDevice.SetPower(powerJie);
                await xinhaoDevice.QueryOpc();
                await xinhaoDevice.EnableOutput();

                await xinhaoBenzhenDevice.LoadStateFile("xinhaoyuan.csa");
                await xinhaoBenzhenDevice.EnableOutput();

                double startPower = -60.5;
                double stopPower = -50;
                double stepPower = 0.5;

                double[] refGains = new double[pointCount];
                //double[] compressionPoints = new double[freqCount];
                bool[] found = new bool[pointCount];
                await Task.Delay(500);
                double step = (stopFreq - startFreq) / (pointCount - 1);
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();

                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    refGains[i] = markPower;
                    found[i] = false;

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    label6.Refresh();
                }
                // 2. 增加功率，查找压缩点
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();


                    for (double power = startPower + stepPower; power <= stopPower && !found[i]; power += stepPower)
                    {
                        await xinhaoDevice.SetPower(power);
                        await xinhaoDevice.QueryOpc();
                        await Task.Delay(800);
                        markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                        markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                        if ((power + 60) - (markPower - refGains[i]) > 1)
                        {
                            //yasuodian[i] = (markPower+ compensationTable[freqGHz]).ToString("F2");
                            yasuodian[i] = (markPower + 20).ToString("F2");
                            found[i] = true;
                        }
                    }

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();
                    await xinhaoDevice.SetPower(-50);
                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    //pset[i] = (markPower + compensationTable[freqGHz]).ToString("F2");
                    pset[i] = (markPower + 40).ToString("F2");

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseCharge();
                await CloseFPGA();
                //WriteYasuodianToMatchingFrequencyRows(freqArray, yasuodian, "测试结果");
                WriteArrayToExcelColumn(yasuodian, 5, sheetName);  // B列，从第9行开始
                WriteArrayToExcelColumn(pset, 6, sheetName);
                LogToConsole("压缩点测试完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"压缩点测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("压缩点测试失败", ex.ToString(), operator_textBox.Text);
            }
        }
        /// <summary>
        /// 相移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button1_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始相移精度测试...");
            WritePersonToAllSheets();
            //LoadVNAState(); // 调用矢网文件
            await ChargeRecievePowerON(); // 加电

            string ch = "";
            string testType = testType_comboBox.Text;
            int chNum = 0;
            if (ch1_checkBox.Checked)
            {
                ch = $"通道1-{testType}";
                chNum = 1;
            }
            if (ch2_checkBox.Checked)
            {
                ch = $"通道2-{testType}";
                chNum = 2;
            }
            if (ch3_checkBox.Checked)
            {
                ch = $"通道3-{testType}";
                chNum = 3;
            }
            if (ch4_checkBox.Checked)
            {
                ch = $"通道4-{testType}";
                chNum = 4;
            }

            string sheetName = $"测试结果{chNum}";

            ScpiDevice scpiDevice = new ScpiDevice();
            ScpiDevice kaiguanDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(vnaAddress);

            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            await scpiDevice.LoadStateFile("kuzengyixiangwei.csa");
            //await vnaDevice.EnableOutput();
            await Task.Delay(500);

            //await scpiDevice.SetNormalize();
            List<double[]> unwrappedPhases = new List<double[]>();
            double[] previousPhase = null;
            double[] phaseOffset = null;
            for (int i = 0; i < 64; i++)
            {
                await RecieveTestUDP(i, "移相"); // FPGA发码
                await Task.Delay(500);           // 等待设备稳定
                await scpiDevice.ScanOnce();

                string[] gain = await scpiDevice.GetGainStringAsync_New();             // 衰减
                string[] initial = await scpiDevice.GetInitialPhaseStringAsync_New();  // 初相

                // 转换为 double[]
                double[] currentPhase = initial.Select(s =>
                {
                    double.TryParse(s, out double v);
                    return v;
                }).ToArray();

                if (previousPhase == null)
                {
                    previousPhase = currentPhase;
                    phaseOffset = new double[currentPhase.Length];
                    unwrappedPhases.Add(currentPhase.ToArray());
                }
                else
                {
                    double[] unwrapped = new double[currentPhase.Length];

                    for (int j = 0; j < currentPhase.Length; j++)
                    {
                        double diff = currentPhase[j] - previousPhase[j];

                        if (diff > 180)
                            phaseOffset[j] -= 360;
                        else if (diff < -180)
                            phaseOffset[j] += 360;

                        unwrapped[j] = currentPhase[j] + phaseOffset[j];
                    }

                    unwrappedPhases.Add(unwrapped);
                    previousPhase = currentPhase;
                }

                // 写入增益
                WriteArrayToExcelColumn_New(gain, i + 2, $"接收寄生调幅{chNum}");
            }

            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电

            // 写入解包后的初相（第 i + 2 列）
            for (int i = 0; i < unwrappedPhases.Count; i++)
            {
                string[] phaseStrings = unwrappedPhases[i].Select(v => v.ToString()).ToArray();
                WriteArrayToExcelColumn_New(phaseStrings, i + 2, $"接收通道相移精度测试结果{chNum}");
            }

            SubtractStandardAndWriteResult($"接收通道相移精度测试结果{chNum}");

            CalculatePhaseAccuracyAndWriteToExcel($"接收通道相移精度测试结果{chNum}", chNum);

            CalculatePhaseAccuracyAndWriteToExcel_Jisheng($"接收寄生调幅{chNum}", chNum);
        }
        /// <summary>
        /// 带外抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
                {
                    MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string ch = "";
                string testType = testType_comboBox.Text;
                int chNum = 0;

                //进度条
                int num = 0;
                progressBar1.Maximum = pointCount;
                progressBar1.Value = 0;

                if (ch1_checkBox.Checked)
                {
                    ch = $"通道1-{testType}";
                    chNum = 1;
                }
                if (ch2_checkBox.Checked)
                {
                    ch = $"通道2-{testType}";
                    chNum = 2;
                }
                if (ch3_checkBox.Checked)
                {
                    ch = $"通道3-{testType}";
                    chNum = 3;
                }
                if (ch4_checkBox.Checked)
                {
                    ch = $"通道4-{testType}";
                    chNum = 4;
                }

                string sheetName = $"测试结果{chNum}";

                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP();       // FPGA发包
                await Task.Delay(500);     // 延时保证设备稳定
                //await WriteFreqArray();

                string[] m45 = new string[pointCount];
                string[] freqArray = GetFilterFreqArray(pointCount);
                LogToConsole("带外抑制测试");

                ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
                ScpiDevice xinhaoDevice = new ScpiDevice();
                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await xinhaoBenzhenDevice.ConnectAsync(vnaAddress);
                bool connected2 = await xinhaoDevice.ConnectAsync(xinhaoAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await pinpuDevice.LoadPinpuStateAsync("C:\\R_S\\Instr\\user\\QuickSave\\dbfzhupu.dfl");
                double center = 175 * 1e6;
                //double start = center - (100 * 1e6);
                //double stop = center + (100 * 1e6);
                //await pinpuDevice.SetStartFrequencyAsync(start);
                //await pinpuDevice.SetStopFrequencyAsync(stop);
                //await pinpuDevice.SetCenterFrequencyAsync(center);
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                double markPower = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK2:STATE ON");
                double markPower2 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK2:X {center - 45 * 1e6}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK3:STATE ON");
                double markPower3 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK3:X {center + 45 * 1e6}");

                await xinhaoDevice.SetPower(-60);
                await xinhaoDevice.QueryOpc();
                await xinhaoDevice.EnableOutput();

                await xinhaoBenzhenDevice.LoadStateFile("xinhaoyuan.csa");
                await xinhaoBenzhenDevice.EnableOutput();

                double[] refGains = new double[pointCount];

                await Task.Delay(500);
                double step = (stopFreq - startFreq) / (pointCount - 1);
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();

                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                    markPower2 = await pinpuDevice.ReadMarkerPowerAsync(2) ?? double.NaN;
                    markPower3 = await pinpuDevice.ReadMarkerPowerAsync(3) ?? double.NaN;
                    m45[i] = (Math.Min(markPower2, markPower3) - markPower).ToString("F2");

                    num++;
                    progressBar1.Value += 1;
                    label6.Text = ((double)num / pointCount * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseCharge();
                await CloseFPGA();
                //WriteYasuodianToMatchingFrequencyRows(freqArray, yasuodian, "测试结果");
                WriteArrayToExcelColumn(m45, 9, sheetName);
                LogToConsole("带外抑制测试完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MGC测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("MGC测试失败", ex.ToString(), operator_textBox.Text);
            }
        }
        /// <summary>
        /// 衰减误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button6_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始衰减精度测试...");
            WritePersonToAllSheets();
            //LoadVNAState(); // 调用矢网文件
            await ChargeRecievePowerON(); // 加电

            string ch = "";
            string testType = testType_comboBox.Text;
            int chNum = 0;
            if (ch1_checkBox.Checked)
            {
                ch = $"通道1-{testType}";
                chNum = 1;
            }
            if (ch2_checkBox.Checked)
            {
                ch = $"通道2-{testType}";
                chNum = 2;
            }
            if (ch3_checkBox.Checked)
            {
                ch = $"通道3-{testType}";
                chNum = 3;
            }
            if (ch4_checkBox.Checked)
            {
                ch = $"通道4-{testType}";
                chNum = 4;
            }

            string sheetName = $"测试结果{chNum}";

            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(vnaAddress);

            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }

            await scpiDevice.LoadStateFile("kuzengyixiangwei.csa");
            //await vnaDevice.EnableOutput();
            await Task.Delay(500);
            //await RecieveTestUDP(0, "移相"); // FPGA发码
            //await Task.Delay(1000);           // 等待设备稳定
            //await scpiDevice.SetNormalize();

            for (int i = 0; i < 64; i++)
            {
                await RecieveTestUDP(i, "衰减"); //FPGA发包
                await Task.Delay(1000); // 延时保证设备稳定
                await scpiDevice.ScanOnce();
                string[] gain = await scpiDevice.GetGainStringAsync_New();               // 增益（dB）
                string[] initial = await scpiDevice.GetInitialPhaseStringAsync_New();    // 初相（°）

                WriteArrayToExcelColumn_New(gain, i + 2, $"接收通道衰减精度测试结果{chNum}");
                //WriteArrayToExcelColumn_New(initial, i + 2, "接收寄生调相");
            }

            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电
            SubtractStandardAndWriteResult($"接收通道衰减精度测试结果{chNum}");
            CalculatePhaseAccuracyAndWriteToExcel($"接收通道衰减精度测试结果{chNum}",chNum);
            //CalculatePhaseAccuracyAndWriteToExcel_Jisheng("接收寄生调相");
        }
        /// <summary>
        /// 三阶交调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button3_Click(object sender, EventArgs e)
        {
            ScpiDevice signalGen = new ScpiDevice();
            string ch = "";

            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;
            int chNum = 0;
            if (ch1_checkBox.Checked)
            {
                ch = $"通道1-{testType}";
                chNum = 1;
            }
            if (ch2_checkBox.Checked)
            {
                ch = $"通道2-{testType}";
                chNum = 2;
            }
            if (ch3_checkBox.Checked)
            {
                ch = $"通道3-{testType}";
                chNum = 3;
            }
            if (ch4_checkBox.Checked)
            {
                ch = $"通道4-{testType}";
                chNum = 4;
            }

            string sheetName = $"测试结果{chNum}";
            try
            {
                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP(); //FPGA发包
                await Task.Delay(500); // 延时保证设备稳定
                string[] freqArray = GetFilterFreqArray(pointCount);
                string[] sanjieJiaotiao = new string[pointCount];

                ScpiDevice scpiDevice = new ScpiDevice();
                ScpiDevice vnaDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(pinpuAddress);
                bool sgConnected = await signalGen.ConnectAsync(xinhaoAddress);
                bool vnaConnected = await vnaDevice.ConnectAsync(vnaAddress);

                if (!connected)
                {
                    LogToConsole("连接失败：矢网或频谱分析仪无法连接");
                    return;
                }

                LogToConsole("三阶交调测试");
                await vnaDevice.LoadStateFile(vnaFilePath);

                await scpiDevice.LoadPinpuStateAsync(sanjieJiaotiaoPath);

                await vnaDevice.SendsjjtStart();

                await signalGen.SetPower(powerJie);
                await signalGen.QueryOpc();
                await signalGen.EnableOutput(); // 打开信号源输出
                //await signalGen.ModON(); // 打开调制输出
                rf_checkBox.Checked = true;
                mod_checkBox.Checked = true;
                int maxRetryCount = 10; // 测量次数
                double toiDefault = -90; // 默认最小值，保证后续比较时不会误判为最大

                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqHz2 = freqHz + 0.001 * 1e9;
                    double freqHz3 = freqHz + 0.0005 * 1e9;

                    await vnaDevice.SetVNACWFreq(freqHz2);
                    await signalGen.SetFrequency(freqHz);
                    await signalGen.QueryOpc();
                    await signalGen.SetPower(-34);
                    await signalGen.QueryOpc();


                    await scpiDevice.SendCommandAsync(":CALC:MARK1:FUNC:TOI:STAT ON");
                    await scpiDevice.SendCommandAsync(":CALC:MARK2:FUNC:TOI:STAT ON");
                    await scpiDevice.SendCommandAsync(":CALC:MARK3:FUNC:TOI:STAT ON");
                    await scpiDevice.SendCommandAsync(":CALC:MARK4:FUNC:TOI:STAT ON");
                    await scpiDevice.SendCommandAsync(":CALC1:MARK1:FUNC:TOI:SEAR ONCE");
                    await scpiDevice.SendCommandAsync(":CALC1:MARK2:FUNC:TOI:SEAR ONCE");
                    await scpiDevice.SendCommandAsync(":CALC1:MARK3:FUNC:TOI:SEAR ONCE");
                    await scpiDevice.SendCommandAsync(":CALC1:MARK4:FUNC:TOI:SEAR ONCE");
                    await scpiDevice.SendCommandAsync($":SENS:FREQ:CENT {freqHz3}");

                    double maxToi = toiDefault;
                    double result = 0;

                    for (int attempt = 0; attempt < maxRetryCount; attempt++)
                    {
                        await Task.Delay(200); // 等待设备稳定

                        //string ip3Str = await scpiDevice.QueryAsync(":CALC:MARK:FUNC:TOI:RES:MIN?");
                        await scpiDevice.SendCommandAsync(":CALC1:MARK1:FUNC:TOI:SEAR ONCE");
                        await scpiDevice.SendCommandAsync(":CALC1:MARK2:FUNC:TOI:SEAR ONCE");
                        await scpiDevice.SendCommandAsync(":CALC1:MARK3:FUNC:TOI:SEAR ONCE");
                        await scpiDevice.SendCommandAsync(":CALC1:MARK4:FUNC:TOI:SEAR ONCE");
                        string m1 = await scpiDevice.QueryAsync(":CALC:MARK1:Y?");
                        string m2 = await scpiDevice.QueryAsync(":CALC:MARK2:Y?");
                        string m3 = await scpiDevice.QueryAsync(":CALC:MARK3:Y?");
                        string m4 = await scpiDevice.QueryAsync(":CALC:MARK4:Y?");
                        double m1D = 0;
                        double m2D = 0;
                        double m3D = 0;
                        double m4D = 0;
                        if (m1 != null)
                        {
                            m1D = double.Parse(m1);
                        }
                        if (m2 != null)
                        {
                            m2D = double.Parse(m2);
                        }
                        if (m3 != null)
                        {
                            m3D = double.Parse(m3);
                        }
                        if (m4 != null)
                        {
                            m4D = double.Parse(m4);
                        }
                        double res1 = m1D - m3D;
                        double res2 = m2D - m4D;

                        if (res1 >= res2)
                        {
                            result = res2;
                        }
                        else
                        {
                            result = res1;
                        }
                        //if (double.TryParse(ip3Str, out double val))
                        //{
                        //    if (val > maxToi)
                        //    {
                        //        maxToi = val;
                        //    }
                        //}
                    }

                    // 保存最大值，若始终无效则为默认值
                    //sanjieJiaotiao[i] = maxToi == toiDefault ? toiDefault.ToString() : maxToi.ToString();
                    sanjieJiaotiao[i] = result.ToString();
                }

                WriteArrayToExcelColumn(sanjieJiaotiao, 9, sheetName);  // B列，从第9行开始
                scpiDevice.Disconnect();
                vnaDevice.Disconnect();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"三阶交调测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("三阶交调测试失败", ex.ToString(), operator_textBox.Text);
            }
            finally
            {
            await CloseCharge(); // 电源关电
            await signalGen.DisableOutput(); // 安全关闭输出
            //await signalGen.ModOFF();
            rf_checkBox.Checked = false;
            //mod_checkBox.Checked = false;
            }
        }

        /// <summary>
        /// 接收测试（增益、镜频、带外、mgc、平坦度）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void label1_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                string ch = "";
                string testType = testType_comboBox.Text;
                int chNum = 0;

                //进度条
                int num = 0;
                progressBar1.Maximum = pointCount;
                progressBar1.Value = 0;

                if (ch1_checkBox.Checked)
                {
                    ch = $"通道1-{testType}";
                    chNum = 1;
                }
                if (ch2_checkBox.Checked)
                {
                    ch = $"通道2-{testType}";
                    chNum = 2;
                }
                if (ch3_checkBox.Checked)
                {
                    ch = $"通道3-{testType}";
                    chNum = 3;
                }
                if (ch4_checkBox.Checked)
                {
                    ch = $"通道4-{testType}";
                    chNum = 4;
                }

                string sheetName = $"测试结果{chNum}";

                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP();       // FPGA发包
                await Task.Delay(500);     // 延时保证设备稳定
                //await WriteFreqArray();

                double[] refGains = new double[pointCount];
                double[] refGains2 = new double[pointCount];
                string[] gain = new string[pointCount];
                string[] m20 = new string[pointCount];
                string[] m40 = new string[pointCount];
                string[] m45 = new string[pointCount];
                string[] jpyz = new string[pointCount];
                string[] mgc = new string[pointCount];
                string[] freqArray = GetFilterFreqArray(pointCount);
                LogToConsole("接收测试");

                ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
                ScpiDevice xinhaoDevice = new ScpiDevice();
                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await xinhaoBenzhenDevice.ConnectAsync(vnaAddress);
                bool connected2 = await xinhaoDevice.ConnectAsync(xinhaoAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await pinpuDevice.LoadPinpuStateAsync("C:\\R_S\\Instr\\user\\QuickSave\\dbfzhupu.dfl");
                double center = 175 * 1e6;
                //double start = center - (100 * 1e6);
                //double stop = center + (100 * 1e6);
                //await pinpuDevice.SetStartFrequencyAsync(start);
                //await pinpuDevice.SetStopFrequencyAsync(stop);
                //await pinpuDevice.SetCenterFrequencyAsync(center);
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                double markPower = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK2:STATE ON");
                double markPower2 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK2:X {center - 10 * 1e6}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK3:STATE ON");
                double markPower3 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK3:X {center + 10 * 1e6}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK4:STATE ON");
                double markPower4 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK4:X {center - 20 * 1e6}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK5:STATE ON");
                double markPower5 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK5:X {center + 20 * 1e6}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK6:STATE ON");
                double markPower6 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK6:X {center - 45 * 1e6}");

                await pinpuDevice.SendCommandAsync(":CALC:MARK7:STATE ON");
                double markPower7 = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK7:X {center + 45 * 1e6}");

                await xinhaoDevice.SetPower(-60);
                await xinhaoDevice.QueryOpc();
                await xinhaoDevice.EnableOutput();

                await xinhaoBenzhenDevice.LoadStateFile("xinhaoyuan.csa");
                await xinhaoBenzhenDevice.EnableOutput();

                await Task.Delay(500);
                double step = (stopFreq - startFreq) / (pointCount - 1);
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqHzd20 = freqHz - 10 * 1e6;
                    double freqHzp20 = freqHz + 10 * 1e6;
                    double freqHzd40 = freqHz - 20 * 1e6;
                    double freqHzp40 = freqHz + 20 * 1e6;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();

                    await Task.Delay(1000); // 让设备处理
                    markPower = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                    markPower6 = await pinpuDevice.ReadMarkerPowerAsync(6) ?? double.NaN;
                    markPower7 = await pinpuDevice.ReadMarkerPowerAsync(7) ?? double.NaN;
                    gain[i] = (markPower + 40).ToString("F2");
                    refGains[i] = markPower;
                    refGains2[i] = markPower;

                    await xinhaoDevice.SetFrequency(freqHzd20);
                    await xinhaoDevice.QueryOpc();
                    await Task.Delay(1000); // 让设备处理
                    markPower2 = await pinpuDevice.ReadMarkerPowerAsync(2) ?? double.NaN;

                    await xinhaoDevice.SetFrequency(freqHzp20);
                    await xinhaoDevice.QueryOpc();
                    await Task.Delay(1000); // 让设备处理
                    markPower3 = await pinpuDevice.ReadMarkerPowerAsync(3) ?? double.NaN;

                    await xinhaoDevice.SetFrequency(freqHzd40);
                    await xinhaoDevice.QueryOpc();
                    await Task.Delay(1000); // 让设备处理
                    markPower4 = await pinpuDevice.ReadMarkerPowerAsync(4) ?? double.NaN;

                    await xinhaoDevice.SetFrequency(freqHzp40);
                    await xinhaoDevice.QueryOpc();
                    await Task.Delay(1000); // 让设备处理
                    markPower5 = await pinpuDevice.ReadMarkerPowerAsync(5) ?? double.NaN;



                    m45[i] = (Math.Min(markPower6, markPower7) - markPower).ToString("F2");
                    m20[i] = (markPower - Math.Min(markPower2, markPower3)).ToString("F2");
                    m40[i] = (markPower - Math.Min(markPower4, markPower5)).ToString("F2");

                    num++;
                    progressBar1.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    progressBar1.Refresh();
                }

                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz - 350 * 1e6);
                    await xinhaoDevice.QueryOpc();

                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    LogToConsole(markPower + " - " + refGains[i] + " = " + (markPower - refGains[i]));
                    refGains[i] = markPower - refGains[i];
                    jpyz[i] = refGains[i].ToString("F2");

                    num++;
                    progressBar1.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    progressBar1.Refresh();
                }

/*                string ch1send = ch1_checkBox.Checked ? "1" : "0";
                string ch2send = ch2_checkBox.Checked ? "1" : "0";
                string ch3send = ch3_checkBox.Checked ? "1" : "0";
                string ch4send = ch4_checkBox.Checked ? "1" : "0";
                string ch5send = ch5_checkBox.Checked ? "1" : "0";
                string ch6send = ch6_checkBox.Checked ? "1" : "0";
                string ch7send = ch7_checkBox.Checked ? "1" : "0";
                string ch8send = ch8_checkBox.Checked ? "1" : "0";
                string ch1 = ch1send + "000000";
                string ch2 = ch2send + "000000";
                string ch3 = ch3send + "000000";
                string ch4 = ch4send + "000000";
                string ch5 = ch5send + "000000";
                string ch6 = ch6send + "000000";
                string ch7 = ch7send + "000000";
                string ch8 = ch8send + "000000";
                string model = "10";
                string model_stc = model + "000000" + "1";
                string buling = new string('0', 55);
                modelValue = StringToByteArray("01 03 02 00");
                var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });

                SendCustomPacket(headValue, modelValue, emptyValue, codeValue);*/
                await Task.Delay(500); // 让设备处理

                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetCenterFrequencyAsync(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();

                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    refGains2[i] = refGains2[i] - markPower - 20;
                    mgc[i] = refGains2[i].ToString("F2");

                    num++;
                    progressBar1.Text = ((double)num / pointCount * 3 * 100).ToString("f2") + "%";
                    progressBar1.Refresh();
                }


                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseCharge();
                await CloseFPGA();
                //WriteYasuodianToMatchingFrequencyRows(freqArray, yasuodian, "测试结果");
                WriteArrayToExcelColumn(gain, 2, sheetName);
                WriteArrayToExcelColumn(jpyz, 6, sheetName);
                WriteArrayToExcelColumn(m20, 7, sheetName);
                WriteArrayToExcelColumn(m40, 8, sheetName);
                WriteArrayToExcelColumn(m45, 9, sheetName);
                WriteArrayToExcelColumn(mgc, 10, sheetName);
                LogToConsole("接收测试完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接收测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), operator_textBox.Text);
            }
        }
        #endregion

        #region 通用方法
        private AxFramerControl _axFramerControl;
        private void InitializeDSO()
        {
            try
            {
                _axFramerControl = new AxFramerControl();
                _axFramerControl.Dock = DockStyle.Fill;

                this.splitContainer1.Panel1.Controls.Add(_axFramerControl);
                _axFramerControl.CreateControl(); // 强制初始化
                _axFramerControl.Titlebar = false;

                // Excel 文件路径

                excelPath = Path.Combine(excelPath, "测试模板Ku.xls");

                if (File.Exists(excelPath))
                {
                    _axFramerControl.Open(excelPath, false, "Excel.Sheet", "", "");

                    Task.Delay(1500).ContinueWith(_ =>
                    {
                        try
                        {
                            dynamic document = _axFramerControl.ActiveDocument;
                            if (document == null)
                            {
                                MessageBox.Show("未能获取 Excel 文档对象");
                                return;
                            }

                            Excel.Workbook workbook = (Excel.Workbook)document;
                            Excel.Application excelApp = workbook.Application;

                            if (excelApp == null || excelApp.ActiveWindow == null)
                            {
                                MessageBox.Show("Excel 应用或窗口未就绪，跳过缩放设置");
                                return;
                            }

                            // 强制激活第一个工作表
                            Excel.Worksheet sheet = (Excel.Worksheet)workbook.Worksheets[1];
                            sheet.Activate();

                            // 稍微等待后再次尝试设置缩放（第一次可能失败）
                            Task.Delay(500).ContinueWith(__ =>
                            {
                                try
                                {
                                    Excel.Window window = excelApp.ActiveWindow;

                                    if (window != null)
                                    {
                                        window.Zoom = false; // 自动适应窗口
                                                             // 或：window.Zoom = 100; （如果你要固定缩放）
                                    }
                                }
                                catch (Exception zoomEx)
                                {
                                    Console.WriteLine("设置缩放失败：" + zoomEx.Message);
                                }
                            }, TaskScheduler.FromCurrentSynchronizationContext());

                            // 可选：清除数据
                            /*                            foreach (Excel.Worksheet ws in workbook.Worksheets)
                                                        {
                                                            ClearExcelContentBelowRow(ws, 8);
                                                        }*/

                            workbook.Save();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("初始化 Excel 失败：" + ex.Message);
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());



                }
                else
                {
                    MessageBox.Show($"找不到 Excel 文件：{excelPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化 DSOFramer 出错：" + ex.ToString());
            }
        }
        /// <summary>
        /// 控制台输出
        /// </summary>
        /// <param name="message"></param>
        public void LogToConsole(string message)
        {
            if (console_textBox.InvokeRequired)
            {
                console_textBox.Invoke(new System.Action(() => {
                    console_textBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                }));
            }
            else
            {
                console_textBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            }
        }
        public double dBmToWatt(double dBm)
        {
            return Math.Pow(10, (dBm / 10.0));
        }
        public string ToSixBitBinaryString(int number)
        {
            if (number < 0 || number > 63)
                throw new ArgumentOutOfRangeException(nameof(number), "输入必须在 0 到 63 之间。");

            //return Convert.ToString(number, 2).PadLeft(6, '0');
            string binary = Convert.ToString(number, 2).PadLeft(6, '0');
            char[] reversed = binary.ToCharArray();
            Array.Reverse(reversed);
            return new string(reversed);
            //return binary;
        }

        private string[] GetFilterFreqArray(int countNum)
        {
            string[] freqArray = new string[countNum];
            double step = 0;
            if (countNum > 1)
            {
                step = (stopFreq - startFreq) / (countNum - 1);
            }
            if (countNum == 1)
            {
                step = 0;
            }
            if (countNum < 0)
            {
                MessageBox.Show("测试设置采集点数错误，请检查设置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            for (int i = 0; i < countNum; i++)
            {
                double freqHz = startFreq + step * i;
                double freqGHz = freqHz / 1e9;
                freqArray[i] = freqGHz.ToString("F6");
            }
            return freqArray;
        }
        #endregion

        #region 加载文件
        private void GetTestSetNewJson()
        {
            try
            {
                string filePath = "TestSet_KU.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                string startFreqDanwei = "";
                string stopFreqDanwei = "";

                data.TryGetValue("comboBox1", out object start_freq_danwei);
                if (start_freq_danwei != null)
                {
                    startFreqDanwei = start_freq_danwei.ToString();
                }
                data.TryGetValue("comboBox2", out object stop_freq_danwei);
                if (stop_freq_danwei != null)
                {
                    stopFreqDanwei = stop_freq_danwei.ToString();
                }

                data.TryGetValue("start_freq_textBox", out object start_freq);
                if (start_freq != null)
                {
                    if (startFreqDanwei == "MHz")
                        startFreq = double.Parse(start_freq.ToString()) * 1e6; // 转换为Hz
                    else if (startFreqDanwei == "GHz")
                        startFreq = double.Parse(start_freq.ToString()) * 1e9; // 转换为Hz
                }

                data.TryGetValue("stop_freq_textBox", out object stop_freq);
                if (stop_freq != null)
                {
                    if (stopFreqDanwei == "MHz")
                        stopFreq = double.Parse(stop_freq.ToString()) * 1e6; // 转换为Hz
                    else if (stopFreqDanwei == "GHz")
                        stopFreq = double.Parse(stop_freq.ToString()) * 1e9; // 转换为Hz
                }

                data.TryGetValue("point_count_textBox", out object point_count);
                if (point_count != null)
                {
                    pointCount = int.Parse(point_count.ToString());
                }

                data.TryGetValue("power_textBox", out object _power);
                if (_power != null)
                {
                    power = double.Parse(_power.ToString());
                }

                data.TryGetValue("powerJie_textBox", out object _powerJ);
                if (_powerJ != null)
                {
                    powerJie = double.Parse(_powerJ.ToString());
                }

                data.TryGetValue("ch1_vol_textBox", out object ch1v);
                if (ch1v != null)
                {
                    ch1_vol = double.Parse(ch1v.ToString());
                }

                data.TryGetValue("ch1_cur_textBox", out object ch1c);
                if (ch1c != null)
                {
                    ch1_cur = double.Parse(ch1c.ToString());
                }

                data.TryGetValue("ch2_vol_textBox", out object ch2v);
                if (ch2v != null)
                {
                    ch2_vol = double.Parse(ch2v.ToString());
                }

                data.TryGetValue("ch2_cur_textBox", out object ch2c);
                if (ch2c != null)
                {
                    ch2_cur = double.Parse(ch2c.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载TestSet_KU.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void GetAddress()
        {
            try
            {
                string filePath = "DeviceAddress_KU.json";
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

                data.TryGetValue("shiwang_textBox", out object shiwang);
                if (shiwang != null)
                {
                    vnaAddress = shiwang.ToString();
                }

                data.TryGetValue("charge_textBox", out object charge);
                if (charge != null)
                {
                    chargeAddress = charge.ToString();
                }

                data.TryGetValue("gonglv_textBox", out object gonglv);
                if (gonglv != null)
                {
                    gonglvAddress = gonglv.ToString();
                }

                data.TryGetValue("xinhao_textBox", out object xinhao);
                if (xinhao != null)
                {
                    xinhaoAddress = xinhao.ToString();
                }

                data.TryGetValue("xinhaoBenzhen_textBox", out object xinhaoBenzhen);
                if (xinhaoBenzhen != null)
                {
                    xinhaoBenzhenAddress = xinhaoBenzhen.ToString();
                }

                data.TryGetValue("pinpu_textBox", out object pinpu);
                if (pinpu != null)
                {
                    pinpuAddress = pinpu.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceAddress.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void GetDeviceFilesJson()
        {
            try
            {
                string filePath = "DeviceFiles_KU.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                data.TryGetValue("textBox6", out object value);
                if (value != null)
                {
                    excelPath = value.ToString();
                }

                data.TryGetValue("textBox1", out object value2);
                if (value2 != null)
                {
                    vnaFilePath = value2.ToString();
                }

                data.TryGetValue("textBox7", out object value3);
                if (value3 != null)
                {
                    excelMobanPath = value3.ToString();
                }

                data.TryGetValue("textBox8", out object value4);
                if (value4 != null)
                {
                    buchangFilePath = value4.ToString();
                }

                data.TryGetValue("textBox9", out object value5);
                if (value5 != null)
                {
                    shiwangChaSunPath = value5.ToString();
                }

                data.TryGetValue("textBox3", out object value6);
                if (value6 != null)
                {
                    pinpuZhupuStatePath = value6.ToString();
                }

                data.TryGetValue("textBox4", out object value7);
                if (value7 != null)
                {
                    pinpuDaiwaiyizhiPath = value7.ToString();
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceFiles_KU.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Excel操作
        private void WriteArrayToExcelColumn(string[] data, int columnIndex, string sheetName)
        {
            try
            {
                // 去除每个字符串的空格
                string[] cleanedData = data.Select(s => s.Replace("\n", "")).ToArray();

                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                Excel.Workbook workbook = excelApp.ActiveWorkbook;

                // 根据名称获取指定的工作表
                Excel.Worksheet worksheet = null;
                foreach (Excel.Worksheet sheet in workbook.Sheets)
                {
                    if (sheet.Name == sheetName)
                    {
                        worksheet = sheet;
                        break;
                    }
                }

                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                // 清空第8行以下的数据
                ClearExcelColumnBelowRow(worksheet, columnIndex, 8);

                // 写入数据，从第8行开始
                for (int i = 0; i < cleanedData.Length; i++)
                {
                    worksheet.Cells[8 + i, columnIndex] = cleanedData[i];
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入 Excel 失败：" + ex.Message);
            }
        }
        private void WriteArrayToExcelColumn(string[] data, int columnIndex, int targetColumnIndex, string sheetName)
        {
            try
            {
                // 去除换行符
                string[] cleanedData = data.Select(s => s.Replace("\n", "")).ToArray();

                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                Excel.Workbook workbook = excelApp.ActiveWorkbook;

                // 根据名称获取指定的工作表
                Excel.Worksheet worksheet = null;
                foreach (Excel.Worksheet sheet in workbook.Sheets)
                {
                    if (sheet.Name == sheetName)
                    {
                        worksheet = sheet;
                        break;
                    }
                }

                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                // 清空 columnIndex 的 8行以下数据
                ClearExcelColumnBelowRow(worksheet, columnIndex, 8);

                // 写入数据 (仅 8–20 行)
                for (int i = 0; i < cleanedData.Length && i < 13; i++) // 8~20 共 13 行
                {
                    int row = 8 + i;

                    // 从目标列取值
                    var targetCell = worksheet.Cells[row, targetColumnIndex];
                    double targetVal = 0;
                    if (targetCell != null)
                    {
                        targetVal = double.Parse(targetCell.Value.ToString());
                    }
                    else
                    {
                        targetVal = 0;
                    }

                    // 当前数据转 double
                    if (double.TryParse(cleanedData[i], out double currentVal))
                    {
                        worksheet.Cells[row, columnIndex] = currentVal - targetVal;
                    }
                    else
                    {
                        worksheet.Cells[row, columnIndex] = cleanedData[i]; // 如果不是数值，就原样写入
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入 Excel 失败：" + ex.Message);
            }
        }
        private void ClearExcelColumnBelowRow(Excel.Worksheet worksheet, int columnIndex, int startRow)
        {
            try
            {
                // 找到当前列中最后有数据的行号
                int lastRow = worksheet.Cells[worksheet.Rows.Count, columnIndex].End(Excel.XlDirection.xlUp).Row;

                // 如果最后行在第 startRow 行或之后，清除从 startRow 到最后行之间的单元格
                if (lastRow >= startRow)
                {
                    Excel.Range clearRange = worksheet.Range[worksheet.Cells[startRow, columnIndex], worksheet.Cells[lastRow, columnIndex]];
                    clearRange.ClearContents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("清除 Excel 列数据失败：" + ex.Message);
            }
        }
        private void WritePeakPowerToMatchingFrequencyRows_New(string[] freqArray, string[] powerArray, string[] xiaolvArray, string[] dingJiang, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int powerColumn = 14;
                int xiaolvColumn = 15;
                int dingJiangColumn = 16;

                int usedRowCount = worksheet.UsedRange.Rows.Count;

                for (int i = 0; i < freqArray.Length; i++)
                {
                    // 保留三位小数进行对比
                    string targetFreq = double.Parse(freqArray[i]).ToString("F3");

                    for (int row = startRow; row <= usedRowCount; row++)
                    {
                        var cellValue = worksheet.Cells[row, freqColumn].Text.ToString().Trim();

                        // Excel单元格内容保留三位小数进行对比
                        if (double.TryParse(cellValue, out double cellFreq))
                        {
                            string formattedCellFreq = cellFreq.ToString("F3");

                            if (formattedCellFreq == targetFreq)
                            {
                                worksheet.Cells[row, powerColumn] = powerArray[i];
                                worksheet.Cells[row, xiaolvColumn] = xiaolvArray[i];
                                worksheet.Cells[row, dingJiangColumn] = dingJiang[i];
                                break;
                            }
                        }
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入峰值功率失败：" + ex.Message);
            }
        }
        private void WriteArrayToExcelColumn_New(string[] data, int columnIndex, string sheetName)
        {
            try
            {
                // 清理字符串
                string[] cleanedData = data.Select(s => s.Trim().Replace("\n", "").Replace("\r", "")).ToArray();

                // 获取当前 Excel 实例
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                Excel.Workbook workbook = excelApp.ActiveWorkbook;

                // 查找指定工作表
                Excel.Worksheet worksheet = workbook.Sheets.Cast<Excel.Worksheet>()
                    .FirstOrDefault(s => s.Name == sheetName);

                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                for (int i = 0; i < cleanedData.Length; i++)
                {
                    int row = 4 + i; // 从第4行开始写

                    // 尝试将字符串转换为 double
                    if (double.TryParse(cleanedData[i], out double currentValue))
                    {
                        if (columnIndex > 2)
                        {
                            // 读取基态列（第2列）的单元格值
                            object baseObj = (worksheet.Cells[row, 2] as Excel.Range).Value;
                            double baseValue = 0.0;

                            if (baseObj != null && double.TryParse(baseObj.ToString(), out double parsedBase))
                            {
                                baseValue = parsedBase;
                            }

                            // 写入差值 = 当前值 - 基态值
                            worksheet.Cells[row, columnIndex] = currentValue - baseValue;
                        }
                        else
                        {
                            // 前两列直接写入原始值
                            worksheet.Cells[row, columnIndex] = currentValue;
                        }
                        //worksheet.Cells[row, columnIndex] = currentValue;
                    }
                    else
                    {
                        // 如果不是数字，直接写原文本
                        worksheet.Cells[row, columnIndex] = cleanedData[i];
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入 Excel 失败：" + ex.Message);
            }
        }
        private void WriteFasheyizhiToMatchingFrequencyRows(string[] freqArray, string[] fasheYizhi, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int fasheYizhiColumn = 14;  // I列

                int usedRowCount = worksheet.UsedRange.Rows.Count;

                for (int i = 0; i < freqArray.Length; i++)
                {
                    // 保留三位小数进行对比
                    string targetFreq = double.Parse(freqArray[i]).ToString("F3");

                    for (int row = startRow; row <= usedRowCount; row++)
                    {
                        var cellValue = worksheet.Cells[row, freqColumn].Text.ToString().Trim();

                        // Excel单元格内容保留三位小数进行对比
                        if (double.TryParse(cellValue, out double cellFreq))
                        {
                            string formattedCellFreq = cellFreq.ToString("F3");

                            if (formattedCellFreq == targetFreq)
                            {
                                worksheet.Cells[row, fasheYizhiColumn] = fasheYizhi[i]; ;
                                break;
                            }
                        }
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入发射抑制失败：" + ex.Message);
            }
        }
        /// <summary>
        ///  写入测试员
        /// </summary>
        private void WritePersonToAllSheets()
        {
            try
            {
                // 获取当前运行的 Excel 实例
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;

                string personText = operator_textBox.Text.Trim();

                // 遍历所有工作表
                foreach (Excel.Worksheet sheet in workbook.Sheets)
                {
                    sheet.Cells[1, 2] = personText; // B1 单元格
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入测试员失败：" + ex.Message);
            }
        }
        private void WriteZaoshengToMatchingFrequencyRows(string[] freqArray, string[] data, string sheetName)
        {
            try
            {
                // 去除每个字符串的空格
                string[] cleanedData = data.Select(s => s.Replace("\n", "")).ToArray();
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int zaoshengColumn = 4;  // G列

                int usedRowCount = worksheet.UsedRange.Rows.Count;

                for (int i = 0; i < freqArray.Length; i++)
                {
                    // 保留三位小数进行对比
                    string targetFreq = double.Parse(freqArray[i]).ToString("F3");

                    for (int row = startRow; row <= usedRowCount; row++)
                    {
                        var cellValue = worksheet.Cells[row, freqColumn].Text.ToString().Trim();

                        // Excel单元格内容保留三位小数进行对比
                        if (double.TryParse(cellValue, out double cellFreq))
                        {
                            string formattedCellFreq = cellFreq.ToString("F3");

                            if (formattedCellFreq == targetFreq)
                            {
                                worksheet.Cells[row, zaoshengColumn] = cleanedData[i];
                                break;
                            }
                        }
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入噪声系数失败：" + ex.Message);
            }
        }
        public void SubtractStandardAndWriteResult(string sheetName)
        {
            LogToConsole("开始写入数据差值（按目标频率点过滤）");

            var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
            var workbook = excelApp.ActiveWorkbook;
            Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];

            int startCol = 2;
            int endCol = 65;

            // 允许 0.01 GHz 的匹配误差（Excel 精度避免错误）
            const double tolerance = 0.0001;

            double step = (stopFreq - startFreq) / (pointCount - 1);
            double[] selectedFreqGHz = Enumerable.Range(0, pointCount).Select(i => (startFreq + i * step) * 1e-9).ToArray();

            for (int col = startCol; col <= endCol; col++)
            {
                object standardObj = phaseSheet.Cells[3, col].Value;
                if (standardObj == null || !double.TryParse(standardObj.ToString(), out double standardValue))
                    continue;

                // 遍历所有频率行（第 4 到 204 行）
                for (int row = 4; row <= 204; row++)
                {
                    // A 列（第 1 列）存频率
                    object freqObj = phaseSheet.Cells[row, 1].Value;

                    if (freqObj == null || !double.TryParse(freqObj.ToString(), out double freqGHz))
                        continue;

                    // ⭐ 判断该行频率是否在目标频率列表中
                    bool isTargetFreq =
                        selectedFreqGHz.Any(f => Math.Abs(f - freqGHz) < tolerance);

                    if (!isTargetFreq)
                        continue; // 跳过非目标频率

                    // 处理有效相位
                    object cellValObj = phaseSheet.Cells[row, col].Value;

                    if (cellValObj != null &&
                        double.TryParse(cellValObj.ToString(), out double measuredValue))
                    {
                        double result = measuredValue - standardValue;

                        // 对应写入 209+(row-4)
                        int targetRow = 209 + (row - 4);
                        phaseSheet.Cells[targetRow, col].Value = result;
                    }
                }
            }

            LogToConsole("差值写入完成（已按目标频率点过滤）");
        }
        public void SubtractStandardAndWriteResult_Jieshou(string sheetName)
        {
            LogToConsole("开始写入数据差值");
            var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
            var workbook = excelApp.ActiveWorkbook;
            Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];

            int startCol = 2;  // 从第2列开始
            int endCol = 65;

            for (int col = startCol; col <= endCol; col++)
            {
                // 获取标准值（第3行）
                object standardObj = phaseSheet.Cells[3, col].Value;
                if (standardObj == null || !double.TryParse(standardObj.ToString(), out double standardValue))
                    continue; // 跳过该列

                // 遍历第4~154行
                for (int row = 4; row <= 16; row++)
                {
                    object cellValueObj = phaseSheet.Cells[row, col].Value;
                    if (cellValueObj != null && double.TryParse(cellValueObj.ToString(), out double measuredValue))
                    {
                        double result = measuredValue - standardValue;
                        int targetRow = 23 + (row - 4);
                        phaseSheet.Cells[targetRow, col].Value = result;
                    }
                }
            }

            LogToConsole("数据差值写入完成");
        }
        private void CalculatePhaseAccuracyAndWriteToExcel(string sheetName, int chNum)
        {
            try
            {
                LogToConsole("开始计算精度...");

                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];
                Excel.Worksheet resultSheet = workbook.Sheets[$"测试结果{chNum}"];

                // =============================
                // ⭐ 只处理这些频率（GHz）
                // =============================
                double step = (stopFreq - startFreq) / (pointCount - 1);
                double[] selectedFreqGHz = Enumerable.Range(0, pointCount).Select(i => (startFreq + i * step)*1e-9).ToArray();


                int startRow = 209;
                int currentRow = startRow;

                while (true)
                {
                    Excel.Range freqCell = phaseSheet.Cells[currentRow, 1]; // A列
                    if (freqCell == null || freqCell.Value == null)
                        break;

                    string freqStr = freqCell.Value.ToString();
                    if (!double.TryParse(freqStr, out double freqGHz))
                        break;

                    // =============================
                    // ⭐ 不在目标频率数组中 → 跳过
                    // =============================
                    bool isSelected = selectedFreqGHz.Any(f => Math.Abs(f - freqGHz) < 1e-6);

                    if (!isSelected)
                    {
                        currentRow++;
                        continue;
                    }

                    // 读取 C〜BM（共64列）
                    List<double> phaseValues = new List<double>();
                    for (int col = 3; col <= 65; col++)
                    {
                        var cell = phaseSheet.Cells[currentRow, col];
                        double val = 0; // 先初始化
                        if (cell != null && double.TryParse(cell.Value?.ToString(), out val))
                        {
                            phaseValues.Add(val);
                        }
                    }

                    // 计算 RMS
                    double rms = Math.Sqrt(phaseValues.Average(v => v * v));

                    // 写入结果表
                    int resultRow = FindRowByFrequency(resultSheet, freqGHz);
                    if (resultRow > 0)
                    {
                        if (sheetName.Equals($"接收通道相移精度测试结果{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 9].Value = rms.ToString();
                        }
                        if (sheetName.Equals($"接收通道衰减精度测试结果{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 10].Value = rms.ToString();
                        }
                        if (sheetName.Equals($"发射通道相移精度测试结果{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 13].Value = rms.ToString();
                        }
                    }

                    currentRow++;
                }

                workbook.Save();
                LogToConsole("精度计算完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理移相精度时出错：" + ex.Message);
            }
        }

        /*        private void CalculatePhaseAccuracyAndWriteToExcel(string sheetName, int chNum)
                {
                    try
                    {
                        LogToConsole("开始计算精度...");
                        var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                        var workbook = excelApp.ActiveWorkbook; ;
                        Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];
                        Excel.Worksheet resultSheet = workbook.Sheets[$"测试结果{chNum}"];

                        // 获取频率点行数
                        int startRow = 131;
                        int currentRow = startRow;

                        while (true)
                        {
                            Excel.Range freqCell = phaseSheet.Cells[currentRow, 1]; // A列
                            if (freqCell == null || freqCell.Value == null)
                                break;

                            string freqStr = freqCell.Value.ToString();
                            if (!double.TryParse(freqStr, out double freqGHz))
                                break;

                            // 读取 C 到 BM 列（64 个值）
                            List<double> phaseValues = new List<double>();
                            for (int col = 3; col <= 65; col++) // C = 3, BM = 65
                            {
                                var cell = phaseSheet.Cells[currentRow, col];
                                double val = 0; // 先初始化
                                if (cell != null && double.TryParse(cell.Value?.ToString(), out val))
                                {
                                    phaseValues.Add(val);
                                }
                            }

                            // 计算均方根（RMS）误差
                            double rms = Math.Sqrt(phaseValues.Average(v => v * v));

                            // 在“测试结果”中查找对应频率行并写入 RMS 到 I 列（第9列）
                            int resultRow = FindRowByFrequency(resultSheet, freqGHz);
                            if (resultRow > 0)
                            {
                                if (sheetName.Equals($"接收通道相移精度测试结果{chNum}"))
                                {
                                    resultSheet.Cells[resultRow, 6].Value = rms.ToString();
                                }
                                if (sheetName.Equals($"接收通道衰减精度测试结果{chNum}"))
                                {
                                    resultSheet.Cells[resultRow, 7].Value = rms.ToString();
                                }
                                if (sheetName.Equals($"发射通道相移精度测试结果"))
                                {
                                    resultSheet.Cells[resultRow, 9].Value = rms.ToString();
                                }
                            }

                            currentRow++;
                        }

                        workbook.Save();
                        LogToConsole("精度计算完成");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("处理移相精度时出错：" + ex.Message);
                    }
                }*/
        private void CalculatePhaseAccuracyAndWriteToExcel_Jisheng(string sheetName, int chNum)
        {
            try
            {
                LogToConsole("开始计算寄生精度...");
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook; ;
                Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];
                Excel.Worksheet resultSheet = workbook.Sheets[$"测试结果{chNum}"];

                // 获取频率点行数
                int startRow = 4;
                int currentRow = startRow;

                while (true)
                {
                    Excel.Range freqCell = phaseSheet.Cells[currentRow, 1]; // A列
                    if (freqCell == null || freqCell.Value == null)
                        break;

                    string freqStr = freqCell.Value.ToString();
                    if (!double.TryParse(freqStr, out double freqGHz))
                        break;

                    // 读取 B 到 BM 列（64 个值）
                    List<double> phaseValues = new List<double>();
                    for (int col = 3; col <= 65; col++) // C = 3, BM = 65
                    {
                        var cell = phaseSheet.Cells[currentRow, col];
                        double val = 0; // 先初始化
                        if (cell != null && double.TryParse(cell.Value?.ToString(), out val))
                        {
                            phaseValues.Add(val);
                        }
                    }

                    // 计算均方根（RMS）误差
                    //double avg = phaseValues.Average();
                    //double rms = Math.Sqrt(phaseValues.Average(v => Math.Pow(v - avg, 2)));
                    double rms = Math.Sqrt(phaseValues.Average(v => v * v));

                    // 在“测试结果”中查找对应频率行并写入 RMS 到 I 列（第9列）
                    int resultRow = FindRowByFrequency(resultSheet, freqGHz);
                    if (resultRow > 0)
                    {
                        if (sheetName.Equals($"接收寄生调幅{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 11].Value = rms.ToString();
                        }
                        if (sheetName.Equals($"接收寄生调相{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 12].Value = rms.ToString();
                        }
                        if (sheetName.Equals($"发射寄生调幅{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 24].Value = rms.ToString();
                        }
                    }


                    currentRow++;
                }

                workbook.Save();
                LogToConsole("精度计算完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理寄生精度时出错：" + ex.Message);
            }
        }
        private int FindRowByFrequency(Excel.Worksheet sheet, double freqGHz)
        {
            int row = 8; // 从第8行开始查找
            while (true)
            {
                var cell = sheet.Cells[row, 1]; // A列
                if (cell == null || cell.Value == null)
                    break;

                if (double.TryParse(cell.Value.ToString(), out double f) &&
                    Math.Abs(f - freqGHz) < 0.0001)
                {
                    return row;
                }

                row++;
            }

            return -1; // 未找到
        }

        /// <summary>
        /// 加载补偿数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private Dictionary<double, double> LoadCompensationTable(string filePath, string type)
        {
            try
            {
                var compensationTable = new Dictionary<double, double>();
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    var table = result.Tables[0];

                    // 从第3行（索引2）开始，假设频率在第1列（索引0），补偿值在第10列（索引9）

                    for (int i = 2; i < 15; i++)
                    {
                        if (double.TryParse(table.Rows[i][0]?.ToString(), out double freq) &&
                            double.TryParse(table.Rows[i][1]?.ToString(), out double comp))
                        {
                            compensationTable[freq] = comp;
                        }
                    }

                    if (type == "压缩点")
                    {
                        for (int i = 19; i < 32; i++)
                        {
                            if (double.TryParse(table.Rows[i][0]?.ToString(), out double freq) &&
                                double.TryParse(table.Rows[i][1]?.ToString(), out double comp))
                            {
                                compensationTable[freq] = comp + compensationTable[freq];
                            }
                        }
                    }

                }
                return compensationTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载补偿表失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new Dictionary<double, double>();
            }
        }
        #endregion

        #region 电源操作
        private double I_T85 = 0;
        private double I_T5 = 0;
        private double I_DQ5 = 0;
        private double I_R5 = 0;
        private async Task CloseCharge()
        {
            try
            {
                await changeToFuZaiTai();
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await scpiDevice.SelectChannel(1);
                await scpiDevice.DisableOutput();
                await scpiDevice.SelectChannel(2);
                await scpiDevice.DisableOutput();
                await scpiDevice.SelectChannel(3);
                await scpiDevice.DisableOutput();
                LogToConsole("电源关电");
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"电源关电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("电源关电失败", ex.ToString(), operator_textBox.Text);
            }
        }
        private async Task ChargeRecievePowerON()
        {
            try
            {
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                if (ch2_vol <= 0 || ch2_cur <= 0)
                {
                    MessageBox.Show("电压或电流值设置有误，请检查测试设置。");
                    return;
                }
                await scpiDevice.SelectChannel(2);
                await scpiDevice.EnableOutput();
                await scpiDevice.SetVoltage(ch2_vol);
                await scpiDevice.SetCurrent(ch2_cur);
                LogToConsole("接收加电...");
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接收加电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("接收加电失败", ex.ToString(), operator_textBox.Text);
            }
        }
        private async Task ChargeSendPowerON()
        {
            try
            {
                //string visaAddress = "TCPIP0::192.168.0.8::INSTR";
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                if (ch1_vol <= 0 || ch1_cur <= 0 || ch2_vol <= 0 || ch2_cur <= 0)
                {
                    MessageBox.Show("电压或电流值设置有误，请检查测试设置。");
                    return;
                }
                await scpiDevice.SelectChannel(1);
                await scpiDevice.EnableOutput();
                await scpiDevice.SetVoltage(ch1_vol);
                await scpiDevice.SetCurrent(ch1_cur);

                await scpiDevice.SelectChannel(2);
                await scpiDevice.EnableOutput();
                await scpiDevice.SetVoltage(ch2_vol);
                await scpiDevice.SetCurrent(ch2_cur);


                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射加电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射加电失败", ex.ToString(), operator_textBox.Text);
            }
        }

        private async Task<double> GetCurrent(int channel)
        {
            try
            {
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("电源连接失败");
                    return -1;
                }
                await scpiDevice.SelectChannel(channel);
                double cur = await scpiDevice.ReadCurrent() ?? 0;

                scpiDevice.Disconnect();
                return cur;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取电源数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("读取电源数据失败", ex.ToString(), operator_textBox.Text);
                return -1;
            }
        }
        private async void 接收加电ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                if (ch2_vol <= 0 || ch2_cur <= 0)
                {
                    MessageBox.Show("电压或电流值设置有误，请检查测试设置。");
                }
                await scpiDevice.SelectChannel(2);
                await scpiDevice.EnableOutput();
                await scpiDevice.SetVoltage(ch2_vol);
                await scpiDevice.SetCurrent(ch2_cur);
                LogToConsole("接收加电");
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接收加电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("接收加电失败", ex.ToString(), operator_textBox.Text);
            }
        }

        private async void 发射加电ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //string visaAddress = "TCPIP0::192.168.0.8::INSTR";
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                if (ch1_vol <= 0 || ch1_cur <= 0 || ch2_vol <= 0 || ch2_cur <= 0)
                {
                    MessageBox.Show("电压或电流值设置有误，请检查测试设置。");
                    return;
                }
                await scpiDevice.SelectChannel(1);
                await scpiDevice.EnableOutput();
                await scpiDevice.SetVoltage(ch1_vol);
                await scpiDevice.SetCurrent(ch1_cur);

                await scpiDevice.SelectChannel(2);
                await scpiDevice.EnableOutput();
                await scpiDevice.SetVoltage(ch2_vol);
                await scpiDevice.SetCurrent(ch2_cur);

                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射加电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射加电失败", ex.ToString(), operator_textBox.Text);
            }
        }

        private async void 关电ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //string visaAddress = "TCPIP0::192.168.0.8::INSTR";
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await scpiDevice.SelectChannel(1);
                await scpiDevice.DisableOutput();
                await scpiDevice.SelectChannel(2);
                await scpiDevice.DisableOutput();
                await scpiDevice.SelectChannel(3);
                await scpiDevice.DisableOutput();

                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"电源关电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("电源关电失败", ex.ToString(), operator_textBox.Text);
            }
        }
        #endregion

        #region FPGA操作
        private OperateLog_DAL operateLog_DAL = new OperateLog_DAL();
        static ushort srcPort = 8080;
        static ushort dstPort = 8080;
        string srcIpStr = "";
        string dstIpStr = "";
        string dstMacStr = "";
        string srcMacStr = ""; //上位机MAC地址
        string srcMacAddress = "";
        string ifaceName = "";

        byte[] headValue;
        byte[] modelValue;
        byte[] emptyValue = StringToByteArray("00 00 00 00 00 00 00 00");
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
                    LogToConsole("找不到接口：" + ifaceName);
                    return;
                }

                device.Open();
                device.SendPacket(ethernetPacket);
                device.Close();

                LogToConsole($"发送数据包：Payload长度={payload.Length}字节");
                LogToConsole($"Payload (Hex): {BitConverter.ToString(payload).Replace("-", " ")}");
                LogToConsole("数据包已发送。\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"UDP发送失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("UDP发送失败", ex.ToString(), operator_textBox.Text);
            }

        }

        static byte[] GenerateCodeValueFromBits(string[] bitStrings)
        {
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
        /// <summary>
        /// 切换至负载态
        /// </summary>

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

        private async Task changeToFuZaiTai()
        {
            await Task.Run(() =>
            {
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
            });
        }
        private async Task RecieveTestUDP()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1recieve = ch1_checkBox.Checked ? "1" : "0";
                    string ch2recieve = ch2_checkBox.Checked ? "1" : "0";
                    string ch3recieve = ch3_checkBox.Checked ? "1" : "0";
                    string ch4recieve = ch4_checkBox.Checked ? "1" : "0";
                    string tr = ch4recieve + "0" + ch3recieve + "0" + ch2recieve + "0" + ch1recieve + "0";
                    string ta = new string('0', 24);
                    string tp = new string('0', 24);
                    string ra = new string('0', 24);
                    string rp = new string('0', 24);
                    string model = "00000001";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });

                    string chSum = "";
                    if (ch1_checkBox.Checked)
                    {
                        chSum += "通道1 ";
                    }
                    if (ch2_checkBox.Checked)
                    {
                        chSum += " 通道2 ";
                    }
                    if (ch3_checkBox.Checked)
                    {
                        chSum += " 通道3 ";
                    }
                    if (ch4_checkBox.Checked)
                    {
                        chSum += " 通道4 ";
                    }

                    LogToConsole("FPGA发包:" + chSum);

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);

                    operateLog_DAL.InsertOperateLog_DT("接收测试", $"{ch1recieve},{ch2recieve},{ch3recieve},{ch4recieve}", operator_textBox.Text);
                }
                catch (Exception ex)
                {
                    LogToConsole("接收测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        private async Task SendTestUDP()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1send = ch1_checkBox.Checked ? "1" : "0";
                    string ch2send = ch2_checkBox.Checked ? "1" : "0";
                    string ch3send = ch3_checkBox.Checked ? "1" : "0";
                    string ch4send = ch4_checkBox.Checked ? "1" : "0";
                    string tr = "0" + ch4send + "0" + ch3send + "0" + ch2send + "0" + ch1send;
                    string ta = new string('0', 24);
                    string tp = new string('0', 24);
                    string ra = new string('0', 24);
                    string rp = new string('0', 24);
                    string model = "00000000";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });


                    string chSum = "";
                    if (ch1_checkBox.Checked)
                    {
                        chSum += "通道1 ";
                    }
                    if (ch2_checkBox.Checked)
                    {
                        chSum += " 通道2 ";
                    }
                    if (ch3_checkBox.Checked)
                    {
                        chSum += " 通道3 ";
                    }
                    if (ch4_checkBox.Checked)
                    {
                        chSum += " 通道4 ";
                    }

                    if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
                    {
                        MessageBox.Show("请至少选择一个通道进行发射测试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    LogToConsole("FPGA发包:" + chSum);
                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);

                    operateLog_DAL.InsertOperateLog_DT("发射测试", $"{ch1send},{ch2send},{ch3send},{ch4send}", operator_textBox.Text);

                }
                catch (Exception ex)
                {
                    LogToConsole("发射测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("发射测试失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        private async Task CloseFPGA()
        {
            await Task.Run(() =>
            {
                try
                {
                    LogToConsole("切换至负载态");
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
                }
                catch (Exception ex)
                {
                    LogToConsole("切换至负载态失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("切换至负载态失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        private async Task RecieveTestUDP(int num, string yixiangOrshuaijian)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 获取勾选的通道数量
                    int selectedCount = 0;
                    if (ch1_checkBox.Checked) selectedCount++;
                    if (ch2_checkBox.Checked) selectedCount++;
                    if (ch3_checkBox.Checked) selectedCount++;
                    if (ch4_checkBox.Checked) selectedCount++;

                    // 判断是否仅选择一个
                    if (selectedCount != 1)
                    {
                        MessageBox.Show("请只选择一个接收通道！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return; // 终止方法
                    }
                    string numToString = ToSixBitBinaryString(num);
                    // 分别设置通道值
                    string ch1recieve = ch1_checkBox.Checked ? "1" : "0";
                    string ch2recieve = ch2_checkBox.Checked ? "1" : "0";
                    string ch3recieve = ch3_checkBox.Checked ? "1" : "0";
                    string ch4recieve = ch4_checkBox.Checked ? "1" : "0";

                    string tr = ch4recieve + "0" + ch3recieve + "0" + ch2recieve + "0" + ch1recieve + "0";
                    string ta = new string('0', 24);
                    string tp = new string('0', 24);
                    string ra = new string('0', 24);
                    string rp = new string('0', 24);
                    if(yixiangOrshuaijian == "移相")
                    {
                        rp = numToString + numToString + numToString + numToString;
                    }
                    else
                    {
                        ra = numToString + numToString + numToString + numToString;
                    }
                    string model = "00000001";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });
                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    LogToConsole(numToString);
                }
                catch (Exception ex)
                {
                    LogToConsole("接收测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        private async Task SendTestUDP(int num, string yixiangOrshuaijian)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 获取勾选的通道数量
                    int selectedCount = 0;
                    if (ch1_checkBox.Checked) selectedCount++;
                    if (ch2_checkBox.Checked) selectedCount++;
                    if (ch3_checkBox.Checked) selectedCount++;
                    if (ch4_checkBox.Checked) selectedCount++;

                    // 判断是否仅选择一个
                    if (selectedCount != 1)
                    {
                        MessageBox.Show("请只选择一个接收通道！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return; // 终止方法
                    }
                    string numToString = ToSixBitBinaryString(num);
                    // 分别设置通道值
                    string ch1send = ch1_checkBox.Checked ? "1" : "0";
                    string ch2send = ch2_checkBox.Checked ? "1" : "0";
                    string ch3send = ch3_checkBox.Checked ? "1" : "0";
                    string ch4send = ch4_checkBox.Checked ? "1" : "0";

                    string tr = "0" + ch4send + "0" + ch3send + "0" + ch2send + "0" + ch1send;
                    string ta = new string('0', 24);
                    string tp = new string('0', 24);
                    string ra = new string('0', 24);
                    string rp = new string('0', 24);
                    if (yixiangOrshuaijian == "移相")
                    {
                        tp = numToString + numToString + numToString + numToString;
                    }
                    else
                    {
                        ta = numToString + numToString + numToString + numToString;
                    }
                    string model = "00000000";
                    string buling = new string('0', 8);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { tr, ta, ra, tp, rp, model, buling });
                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    LogToConsole(numToString);
                }
                catch (Exception ex)
                {
                    LogToConsole("发射测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("发射测试失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        #endregion

        #region 功率计操作
        /// <summary>
        /// 调用功率计文件
        /// </summary>
        /// <returns></returns>
        private async Task LoadGonglvState()
        {
            try
            {
                string visaAddress = gonglvAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("功率计连接失败");
                    return;
                }
                bool state = await scpiDevice.LoadGonglvState();
                if (state)
                {
                    LogToConsole("调用功率计文件");
                }

                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用功率计文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("调用功率计文件失败", ex.ToString(), operator_textBox.Text);
            }
        }



        #endregion

        #region 信号发生器操作
        /// <summary>
        /// 开关射频输出功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void rf_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (rf_checkBox.Checked)
            {
                try
                {
                    string visaAddress = xinhaoAddress;

                    ScpiDevice scpiDevice = new ScpiDevice();

                    bool connected = await scpiDevice.ConnectAsync(visaAddress);
                    if (!connected)
                    {
                        LogToConsole("连接失败");
                        return;
                    }
                    await scpiDevice.EnableOutput();
                    LogToConsole("打开射频输出");

                    scpiDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开射频输出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    operateLog_DAL.InsertOperateLog_DT("打开射频输出失败", ex.ToString(), operator_textBox.Text);
                }
            }
            else
            {
                try
                {
                    string visaAddress = xinhaoAddress;

                    ScpiDevice scpiDevice = new ScpiDevice();

                    bool connected = await scpiDevice.ConnectAsync(visaAddress);
                    if (!connected)
                    {
                        LogToConsole("连接失败");
                        return;
                    }
                    await scpiDevice.DisableOutput();
                    LogToConsole("关闭射频输出");

                    scpiDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"关闭射频输出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    operateLog_DAL.InsertOperateLog_DT("关闭射频输出失败", ex.ToString(), operator_textBox.Text);
                }
            }
        }
        /// <summary>
        /// 开关调制功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mod_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (mod_checkBox.Checked)
            {
                try
                {
                    string visaAddress = xinhaoAddress;

                    ScpiDevice scpiDevice = new ScpiDevice();

                    bool connected = await scpiDevice.ConnectAsync(visaAddress);
                    if (!connected)
                    {
                        LogToConsole("连接失败");
                        return;
                    }
                    await scpiDevice.ModON();
                    LogToConsole("启用调制功能");

                    scpiDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启用调制功能失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    operateLog_DAL.InsertOperateLog_DT("启用调制功能失败", ex.ToString(), operator_textBox.Text);
                }
            }
            else
            {
                try
                {
                    string visaAddress = xinhaoAddress;

                    ScpiDevice scpiDevice = new ScpiDevice();

                    bool connected = await scpiDevice.ConnectAsync(visaAddress);
                    if (!connected)
                    {
                        LogToConsole("连接失败");
                        return;
                    }
                    await scpiDevice.ModOFF();
                    LogToConsole("关闭调制功能");

                    scpiDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"关闭调制功能失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    operateLog_DAL.InsertOperateLog_DT("关闭调制功能失败", ex.ToString(), operator_textBox.Text);
                }
            }
        }
        #endregion

        #region 工具栏按钮
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前 Excel 应用程序实例
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                Excel.Workbook workbook = excelApp?.ActiveWorkbook;

                if (workbook == null)
                {
                    MessageBox.Show("未检测到活动的 Excel 工作簿。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 构建 testChannel
                string testChannel = "";
                if (ch1_checkBox.Checked) testChannel = "ch1";
                if (ch2_checkBox.Checked) testChannel = "ch2";
                if (ch3_checkBox.Checked) testChannel = "ch3";
                if (ch4_checkBox.Checked) testChannel = "ch4";
                if (ch1_checkBox.Checked) testChannel = "ch5";
                if (ch2_checkBox.Checked) testChannel = "ch6";
                if (ch3_checkBox.Checked) testChannel = "ch7";
                if (ch4_checkBox.Checked) testChannel = "ch8";

                string testComponent = componentName_textBox.Text ?? "component";
                string testType = testType_comboBox.Text ?? "";
                string timestamp = DateTime.Now.ToString("yyyy.MM.dd.HHmmss");

                // 先尝试用你现有的 excelPath（假设这是类级字段），否则尝试 workbook.Path（已保存工作簿的目录），否则使用用户文档目录
                string excelPathTemp = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(excelPath))
                    {
                        excelPathTemp = Path.GetDirectoryName(excelPath);
                    }
                }
                catch { /* ignore */ }

                if (string.IsNullOrWhiteSpace(excelPathTemp))
                {
                    // workbook.Path 在工作簿未保存时通常为空字符串
                    if (!string.IsNullOrWhiteSpace(workbook.Path))
                        excelPathTemp = workbook.Path;
                    else
                        excelPathTemp = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                // 获取扩展名：优先用 workbook.FullName 的扩展名；如果没有（新文档），默认使用 .xlsx
                string ext = ".xlsx";
                try
                {
                    string fullName = workbook.FullName; // 可能会在未保存时为 "" 或抛异常（通常不会）
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        string e1 = Path.GetExtension(fullName);
                        if (!string.IsNullOrWhiteSpace(e1))
                            ext = e1;
                    }
                    else
                    {
                        // 如果 FullName 为空，可以尝试根据 FileFormat 推测（可选）
                        // ext = workbook.FileFormat == (int)Excel.XlFileFormat.xlExcel8 ? ".xls" : ".xlsx";
                    }
                }
                catch
                {
                    // 忽略，使用默认扩展名
                }

                // 构造安全的文件名（移除文件名中非法字符）
                string rawFileName = $"{testComponent}{testChannel}_高低温{testType}_{timestamp}";
                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (var c in invalidChars)
                {
                    rawFileName = rawFileName.Replace(c.ToString(), "_");
                }

                string savePath = Path.Combine(excelPathTemp, rawFileName + ext);

                // 保存副本
                workbook.SaveCopyAs(savePath);

                MessageBox.Show($"已成功另存为：{savePath}", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("另存为失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            dynamic document = _axFramerControl.ActiveDocument;
            if (document == null)
            {
                MessageBox.Show("未能获取 Excel 文档对象");
                return;
            }

            Excel.Workbook workbook = (Excel.Workbook)document;
            Excel.Application excelApp = workbook.Application;

            if (excelApp == null || excelApp.ActiveWindow == null)
            {
                MessageBox.Show("Excel 应用或窗口未就绪，跳过操作");
                return;
            }

            // 新增：用户确认弹窗
            DialogResult confirmResult = MessageBox.Show(
                "⚠️ 确定要清空当前 Excel 文件中的数据吗？\n\n此操作不可恢复！",
                "确认清空数据",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmResult != DialogResult.Yes)
            {
                MessageBox.Show("操作已取消。", "已取消", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }


            try
            {
                // ---------- 1️⃣ 清空第一个sheet ----------
                Excel.Worksheet sheet1 = (Excel.Worksheet)workbook.Worksheets[1];
                Excel.Range range1 = sheet1.Range["B8", "Q" + sheet1.Rows.Count]; // 从第9行到最后一行
                range1.ClearContents(); // 清空文本内容

                // ---------- 2️⃣ 第二个sheet ----------
                Excel.Worksheet sheet2 = (Excel.Worksheet)workbook.Worksheets[2];
                Excel.Range range2a = sheet2.Range["B4", "BM16"];
                Excel.Range range2b = sheet2.Range["B23", "BM35"];
                range2a.Value2 = 0;
                range2b.Value2 = 0;

                // ---------- 3️⃣ 第三个sheet ----------
                Excel.Worksheet sheet3 = (Excel.Worksheet)workbook.Worksheets[3];
                Excel.Range range3a = sheet3.Range["B4", "BM124"];
                Excel.Range range3b = sheet3.Range["B131", "BM251"];
                range3a.Value2 = 0;
                range3b.Value2 = 0;
                for (int i = 4; i <= 17; i++)
                {
                    if (i % 2 == 0)
                    {
                        Excel.Worksheet sheet = (Excel.Worksheet)workbook.Worksheets[i];
                        Excel.Range range = sheet.Range["B8", "Q" + sheet.Rows.Count]; // 从第9行到最后一行
                        range.ClearContents(); // 清空文本内容
                    }
                    else
                    {
                        Excel.Worksheet sheet = (Excel.Worksheet)workbook.Worksheets[i];
                        Excel.Range rangea = sheet.Range["B4", "BM16"];
                        Excel.Range rangeb = sheet.Range["B23", "BM35"];
                        rangea.Value2 = 0;
                        rangeb.Value2 = 0;
                    }
                }
                // ---------- 保存 ----------
                workbook.Save();

                MessageBox.Show("数据已清空并重置成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("操作 Excel 失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void 手动发码ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new ManualSend_Form(this);
            form.ShowDialog();
        }
        private void 参数设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new ChargeControl_Form(this);
            form.ShowDialog();
        }

        private async void 调用矢网信号源文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var vnaDevice = new ScpiDevice();

                bool deviceConnected = await vnaDevice.ConnectAsync(vnaAddress);

                if (!deviceConnected)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await vnaDevice.LoadStateFile("xinhaoyuan.csa");
                LogToConsole("调用矢网文件完成");
                vnaDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show("调用失败:" + ex.ToString());
            }
        }

        private async void 调用矢网移相状态文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var vnaDevice = new ScpiDevice();

                bool deviceConnected = await vnaDevice.ConnectAsync(vnaAddress);

                if (!deviceConnected)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await vnaDevice.LoadStateFile("yixiang.csa");
                LogToConsole("调用矢网文件完成");
                vnaDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show("调用失败:" + ex.ToString());
            }
        }

        private void 打开矢网差损文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(shiwangChaSunPath) || !File.Exists(shiwangChaSunPath))
                {
                    MessageBox.Show("找不到指定的 Excel 文件路径：" + shiwangChaSunPath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建 Excel 应用程序实例
                var excelApp = new Excel.Application();
                excelApp.Visible = true; // 显示 Excel 窗口

                // 打开指定工作簿
                excelApp.Workbooks.Open(shiwangChaSunPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开 Excel 文件失败：" + ex.Message);
            }
        }

        private void 参数设置ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form form = new PinpuControl_Form(this);
            form.ShowDialog();
        }

        private async void 调用主谱状态文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var scpiDevice = new ScpiDevice();

                bool deviceConnected = await scpiDevice.ConnectAsync(pinpuAddress);

                if (!deviceConnected)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await scpiDevice.LoadPinpuStateAsync("C:\\R_S\\Instr\\user\\QuickSave\\dbfzhupu.dfl");
                LogToConsole("调用频谱文件完成");
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show("调用失败:" + ex.ToString());
            }
        }

        private async void 调用功率计状态文件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await LoadGonglvState();
        }

        private void 仪表地址设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new DeviceAddressNew_Form(this);
            form.ShowDialog();
        }

        private void 调用文件设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new DeviceFiles_Form();
            form.ShowDialog();
        }

        private void 测试设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new TestSet_New_Form();
            form.ShowDialog();
        }

        private void 数据库设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new SqlSet_Form();
            form.ShowDialog();
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

        #endregion

        private async void button13_Click(object sender, EventArgs e)
        {
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(vnaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            string ans = await scpiDevice.QueryAsync(":CALC1:PAR:CAT?");
            LogToConsole(ans);

            //await scpiDevice.SendCommandAsync($"CALC:PAR:SEL 'CH1_S22_4'");

            scpiDevice.Disconnect();
            //CalculatePhaseAccuracyAndWriteToExcel($"接收通道衰减精度测试结果1", 1);
        }


    }
}
