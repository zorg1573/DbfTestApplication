using DbfTest.DAL;
using DbfTest.FUNCTION;
using DbfTest.PAGE;
using Excel;
using ExcelDataReader;
using MetroFramework.Forms;
using NPOI.POIFS.Crypt.Dsig;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace DbfTest
{
    public partial class Main_Form : MetroForm
    {
        #region 全局变量
        //JsonSet_DBF.json
        string _deviceAddressPath = "";
        string _deviceFilesPath = "";
        string _testSetPath = "";

        //DeviceAddress_DBF.json
        string _chargeAddress = "";
        //string _charge2Address = "";
        string _vnaAddress = "";
        /// <summary>矢网作信号源时的射频端口号（SOUR:POWn）。</summary>
        const int _vnaRfPortNum = 2;
        string _gonglvAddress = "";
        string _xinhaoAddress = "";
        string _xinhaoBenzhenAddress = "";
        string _pinpuAddress = "";

        //TestSet_DBF.json
        double _Power = -1;
        double _PowerBenzhen = -1;
        double _startFreq = -1;
        double _stopFreq = -1;
        int _pointCount = -1;
        double _ch1_vol = -1;
        double _ch1_cur = -1;
        double _ch2_vol = -1;
        double _ch2_cur = -1;

        //DeviceFiles_DBF.json
        string _excelPath = "";
        string _vnaFilePath = "";
        string _jsonPath = "";
        string _buchangFilePath = "";
        string _shiwangChaSunPath = ""; //矢网差损文件路径
        string _pinpuZhupuStatePath = ""; //频谱分析仪主谱状态文件
        string _pinpuDaiwaiyizhiPath = ""; //频谱分析仪带外抑制状态文件
        string _sanjieJiaotiaoPath = ""; //三阶交调文件路径

        /// <summary>发射移相仅测 7 个基态（经 ToSixBitBinaryString 反转后对应发码索引）。</summary>
        static readonly int[] TxPhaseBaseStateIndices = { 0, 1, 2, 4, 8, 16, 32 };

        // XDBF：9.0~10.2GHz；矢网约 10MHz 步进 → 121 点；测试频点 0.1GHz 步进 → 13 点
        const double RfBandStartGHz = 9.0;
        const double RfBandStopGHz = 10.2;

        const int TxPhaseMeasureStartRow = 4;
        const int TxPhaseMeasureEndRow = 124; // 与模板 B4:BM124 对齐（121 点）
        const int TxPhaseDifferenceStartRow = 131; // 与模板 B131:BM251 对齐

        const int RxAttenMeasureStartRow = 4;
        const int RxAttenMeasureEndRow = 16; // 与模板 B4:BM16 对齐（13 点）
        const int RxAttenDifferenceStartRow = 23; // 与模板 B23:BM35 对齐
        const int RxAttenDifferenceEndRow = 35;
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
            componentName_textBox.Text = "XDBF20W";
            jsbc_textBox.Text = "60";
            textBox_rf_power.Text = "-60";
            textBox_benzhen_power.Text = "2.5";
            textBox_benzhen_power_fs.Text = "1";
            GetAddress();
            GetDeviceFilesJson();
            GetTestSetNewJson();
            InitializeDSO();
            StartExcelWorker();
            InitTemperaturePanel();
            StartTemperatureReceiver();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                try { _tempReceiver?.Dispose(); } catch { /* 关闭阶段忽略 */ }

                // 1. 先恢复 Excel 窗口的父窗口关系（如果已嵌入）
                if (_excelHwnd != IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 恢复 Excel 窗口父关系，句柄: {_excelHwnd}");
                    try
                    {
                        // 将窗口恢复到桌面
                        SetParent(_excelHwnd, IntPtr.Zero);
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 窗口父关系已恢复");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] 恢复 Excel 窗口父关系失败: {ex.Message}");
                    }
                    _excelHwnd = IntPtr.Zero;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 窗口句柄为空，跳过恢复");
                }

                // 2. 关闭工作簿（按照成熟方案，先关闭工作簿）
                if (_workbook != null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 开始关闭工作簿...");
                    try
                    {
                        _workbook.Close(false);
                        System.Diagnostics.Debug.WriteLine("[DEBUG] 工作簿已关闭");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] 关闭工作簿失败: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            // 使用 FinalReleaseComObject 确保完全释放
                            Marshal.FinalReleaseComObject(_workbook);
                            System.Diagnostics.Debug.WriteLine("[DEBUG] 工作簿 COM 对象已释放");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 释放工作簿 COM 对象失败: {ex.Message}");
                        }
                        _workbook = null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 工作簿为空，跳过关闭");
                }

                // 3. 退出 Excel 应用
                if (_excelApp != null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] 开始退出 Excel 应用...");
                    try
                    {
                        _excelApp.DisplayAlerts = false;
                        _excelApp.Quit();
                        System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 应用已退出");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] 退出 Excel 应用失败: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            // 使用 FinalReleaseComObject 确保完全释放
                            Marshal.FinalReleaseComObject(_excelApp);
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 应用 COM 对象已释放");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 释放 Excel 应用 COM 对象失败: {ex.Message}");
                        }
                        _excelApp = null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 应用为空，跳过退出");
                }

                // 4. 如果 Excel 进程仍然存在，快速强制终止（不等待，避免阻塞）
                if (_excelProcess != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] 检查 Excel 进程，ID: {_excelProcess.Id}, HasExited: {_excelProcess.HasExited}");
                    try
                    {
                        if (!_excelProcess.HasExited)
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] 强制终止 Excel 进程...");
                            // 不等待，直接强制终止
                            _excelProcess.Kill();
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 进程已终止");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 进程已退出");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] 终止 Excel 进程失败: {ex.Message}");
                        // 如果无法终止，尝试通过进程名查找并终止
                        try
                        {
                            var excelProcesses = System.Diagnostics.Process.GetProcessesByName("EXCEL");
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 找到 {excelProcesses.Length} 个 Excel 进程");
                            foreach (var proc in excelProcesses)
                            {
                                try
                                {
                                    if (_excelProcess != null && proc.Id == _excelProcess.Id)
                                    {
                                        proc.Kill();
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] 已终止 Excel 进程 ID: {proc.Id}");
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 通过进程名终止失败: {ex2.Message}");
                        }
                    }
                    finally
                    {
                        try
                        {
                            _excelProcess?.Dispose();
                            System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 进程对象已释放");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] 释放 Excel 进程对象失败: {ex.Message}");
                        }
                        _excelProcess = null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Excel 进程为空，跳过终止");
                }

                // 5. 强制垃圾回收，确保 COM 对象被完全释放（按照成熟方案多次调用）
                System.Diagnostics.Debug.WriteLine("[DEBUG] 开始垃圾回收...");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                System.Diagnostics.Debug.WriteLine("[DEBUG] 垃圾回收完成");
            }
            catch (Exception ex)
            {
                // 记录错误但不阻止窗口关闭
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 关闭 Excel 时出错: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // 确保不阻止窗口关闭
                e.Cancel = false;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] finally 块 - 设置 Cancel = false, 准备调用 base.OnFormClosing");

                // 检查窗口状态
                System.Diagnostics.Debug.WriteLine($"[DEBUG] 窗口状态 - WindowState: {this.WindowState}, Visible: {this.Visible}, IsDisposed: {this.IsDisposed}");

                // 调用基类方法，让窗口正常关闭
                System.Diagnostics.Debug.WriteLine("[DEBUG] 调用 base.OnFormClosing...");
                base.OnFormClosing(e);
            }
        }
        private void StartExcelWorker()
        {
            Task.Run(async () =>
            {
                foreach (var task in _excelTaskQueue.GetConsumingEnumerable())
                {
                    // 回 UI 线程执行 Excel 操作
                    await this.InvokeAsync(async () =>
                    {
                        await task();
                    });
                }
            });
        }

        #region 发射测试
        private async void button8_Click(object sender, EventArgs e)
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
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
            await Task.Delay(500); // 延时保证设备稳定
/*            await CloseFPGA();
            await Task.Delay(500);
            I_DQ5 = await GetCurrent(2);
            await RecieveTestUDP();
            await Task.Delay(500);
            I_R5 = await GetCurrent(2);*/

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
            if (ch5_checkBox.Checked)
            {
                ch = $"通道5-{testType}";
                chNum = 5;
            }
            if (ch6_checkBox.Checked)
            {
                ch = $"通道6-{testType}";
                chNum = 6;
            }
            if (ch7_checkBox.Checked)
            {
                ch = $"通道7-{testType}";
                chNum = 7;
            }
            if (ch8_checkBox.Checked)
            {
                ch = $"通道8-{testType}";
                chNum = 8;
            }
            string sheetName = $"测试结果{chNum}";

            //GetTestSetNewJson();
            if (_pointCount <= 0)
            {
                MessageBox.Show("请先设置点数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int num = 0;
            SafeSetProgressBarMaximum(_pointCount, 0);

            string[] freqArray = new string[_pointCount];
            string[] pulsePowerString = new string[_pointCount];
            string[] xiaolvString = new string[_pointCount];
            string[] dingJiang = new string[_pointCount]; //顶降
            string[] shangshengyan = new string[_pointCount];
            string[] xiajiangyan = new string[_pointCount];

            string[] compensatedPowerString = new string[_pointCount];

            var xinhaoBenzhenDevice = new ScpiDevice();
            var powerMeter = new ScpiDevice();

            bool deviceConnected = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
            bool pmConnected = await powerMeter.ConnectAsync(_gonglvAddress);

            if (!deviceConnected || !pmConnected)
            {
                LogToConsole("设备连接失败");
                return;
            }

            try
            {
                await powerMeter.LoadGonglvState();
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb_FS());
                await xinhaoBenzhenDevice.EnableRfOutput();
                LogToConsole("获取功率计数据");
                freqArray = GetFilterFreqArray(_pointCount);
                // textBox157~textBox169 对应 9.0GHz~10.2GHz（步进0.1GHz）共13个点的发射功率补偿
                System.Windows.Forms.TextBox[] txPowerCompBoxes = {
                    textBox157, textBox158, textBox159, textBox160,
                    textBox161, textBox162, textBox163, textBox164,
                    textBox165, textBox166, textBox167, textBox168,
                    textBox169
                };
                double step = (_stopFreq - _startFreq) / (_pointCount - 1);
                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetFrequency(freqHz);
                    //await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.SendCommandAsync($":SENS:FREQ {freqHz}");
                    await powerMeter.SendCommandAsync(":INIT:IMM");         // 开始测量
                    await powerMeter.SendCommandAsync("*WAI");              // 等待测量完成
                    await Task.Delay(800); // 延时保证设备稳定
                    await powerMeter.ReadPulsePowerArrayAsync(); // 预读取一次丢弃
                    await powerMeter.ReadPulsePowerArrayAsync(); // 预读取一次丢弃
                    // 读取功率计峰值功率（dBm）
                    double[] pulsePower = await powerMeter.ReadPulsePowerArrayAsync();
                    double positiveDur = await powerMeter.GetPositiveDuration() * 1e6 ?? -1;
                    double negativeDur = await powerMeter.GetNegativeDuration() * 1e6 ?? -1;
                    dingJiang[i] = pulsePower[6].ToString();
                    shangshengyan[i] = positiveDur.ToString();
                    xiajiangyan[i] = negativeDur.ToString();

                    int compIndex = (int)Math.Round((freqGHz - RfBandStartGHz) / 0.1);
                    double compensation = 0;
                    if (compIndex >= 0 && compIndex < txPowerCompBoxes.Length)
                        compensation = double.Parse(txPowerCompBoxes[compIndex].Text);
                    double compensatedPower = pulsePower[0] + compensation;
                    //double PowerWatt = dBmToWatt(compensatedPower); // dBm 转 W
                    compensatedPowerString[i] = compensatedPower.ToString();

                    //I_T28 = await GetCurrent(1);
                    //I_T5 = await GetCurrent(2);

                    //double fenmu1 = 28 * I_T28;
                    //double fenmu2 = 5 * (I_T5 - 0.75 * I_DQ5);
                    //double fenmu3 = 0.8 * 5 * (I_R5 - 0.75 * I_DQ5);

                    //double chargePower = fenmu1 + fenmu2 + fenmu3;
                    //xiaolvString[i] = PowerWatt * 0.2 / chargePower * 10 + "%"; // 计算效率百分比

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label6.Refresh();

                    //main_DAL.UpdateTestDataFreq_DT(ch, componentName, double.Parse(freqArray[i]), double.Parse(compensatedPowerString[i]));
                }
                //await vnaDevice.DisableOutput();
                //WriteArrayToExcelColumn(freqArray, 7, ch);
                //WriteArrayToExcelColumn(compensatedPowerString, 8, ch);
                WritePeakPowerToMatchingFrequencyRows_New(freqArray, compensatedPowerString, xiaolvString, dingJiang, sheetName);
                WriteArrayToExcelColumn(shangshengyan, 19, sheetName);
                WriteArrayToExcelColumn(xiajiangyan, 20, sheetName);

                LogToConsole("Excel写入完成");
            }
            catch (Exception ex)
            {
                LogToConsole($"测量异常：{ex.Message}");
            }
            finally
            {
                await xinhaoBenzhenDevice.DisableRfOutput();
                //await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                //mod_checkBox.Checked = false;
                xinhaoBenzhenDevice.Disconnect();
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
        Dictionary<double, string> phaseToBits = new Dictionary<double, string>
{
    {0,   "000000"},
    {-5.625,   "001000"},
    {-11.250,  "000100"},
    {-16.875,  "001100"},
    {-22.500,  "000010"},
    {-28.125,  "001010"},
    {-33.750,  "000110"},
    {-39.375,  "001110"},
    {-45.000,  "010000"},
    {-50.625,  "011000"},
    {-56.250,  "010100"},
    {-61.875,  "011100"},
    {-67.500,  "010010"},
    {-73.125,  "011010"},
    {-78.750,  "010110"},
    {-84.375,  "011110"},
    {-90.000,  "000001"},
    {-95.625,  "001001"},
    {-101.250, "000101"},
    {-106.875, "001101"},
    {-112.500, "000011"},
    {-118.125, "001011"},
    {-123.750, "000111"},
    {-129.375, "001111"},
    {-135.000, "010001"},
    {-140.625, "011001"},
    {-146.250, "010101"},
    {-151.875, "011101"},
    {-157.500, "010011"},
    {-163.125, "011011"},
    {-168.750, "010111"},
    {-174.375, "011111"},
    {-180.000, "100000"},
    {-185.625, "101000"},
    {-191.250, "100100"},
    {-196.875, "101100"},
    {-202.500, "100010"},
    {-208.125, "101010"},
    {-213.750, "100110"},
    {-219.375, "101110"},
    {-225.000, "110000"},
    {-230.625, "111000"},
    {-236.250, "110100"},
    {-241.875, "111100"},
    {-247.500, "110010"},
    {-253.125, "111010"},
    {-258.750, "110110"},
    {-264.375, "111110"},
    {-270.000, "100001"},
    {-275.625, "101001"},
    {-281.250, "100101"},
    {-286.875, "101101"},
    {-292.500, "100011"},
    {-298.125, "101011"},
    {-303.750, "100111"},
    {-309.375, "101111"},
    {-315.000, "110001"},
    {-320.625, "111001"},
    {-326.250, "110101"},
    {-331.875, "111101"},
    {-337.500, "110011"},
    {-343.125, "111011"},
    {-348.750, "110111"},
    {-354.375, "111111"}
};
        private readonly BlockingCollection<Func<Task>> _excelTaskQueue = new BlockingCollection<Func<Task>>();
        private async void button15_Click(object sender, EventArgs e)
        {
            LogToConsole("开始发射初相测试...");
            WritePersonToAllSheets();
            await ChargeSendPowerON(); // 发射加电
            await Task.Delay(1000);
            int chNum = 0;

            if (ch1_checkBox.Checked) chNum = 1;
            if (ch2_checkBox.Checked) chNum = 2;
            if (ch3_checkBox.Checked) chNum = 3;
            if (ch4_checkBox.Checked) chNum = 4;
            if (ch5_checkBox.Checked) chNum = 5;
            if (ch6_checkBox.Checked) chNum = 6;
            if (ch7_checkBox.Checked) chNum = 7;
            if (ch8_checkBox.Checked) chNum = 8;

            if (chNum == 0)
            {
                MessageBox.Show("请先选择通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (_pointCount <= 0)
            {
                MessageBox.Show("请先设置点数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string sheetName = $"测试结果{chNum}";
            string[] chuxiang = new string[_pointCount];
            ScpiDevice scpiDevice = new ScpiDevice();
            bool connected = await scpiDevice.ConnectAsync(_vnaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }

            try
            {
                await scpiDevice.LoadStateFile("501.csa");
                await scpiDevice.EnableOutput();
                await SendTestUDP(0, "移相"); // FPGA发码
                await Task.Delay(1000);

                await scpiDevice.TriggerSingleSweepAfterHoldAsync();
                string[] initial = await scpiDevice.GetPhase_Send();    // 121点初相（°），对应9.0~10.2GHz
                if (initial == null || initial.Length == 0)
                {
                    LogToConsole("未获取到初相数据");
                    return;
                }

                // 矢网扫频：9.0GHz~10.2GHz，共121点，步进约10MHz
                const double vnaStartHz = RfBandStartGHz * 1e9;
                const double vnaStopHz = RfBandStopGHz * 1e9;
                int vnaPoints = initial.Length;
                double vnaStepHz = (vnaPoints > 1) ? (vnaStopHz - vnaStartHz) / (vnaPoints - 1) : 0;

                double testStepHz = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + testStepHz * i;
                    int idx = (vnaStepHz > 0)
                        ? (int)Math.Round((freqHz - vnaStartHz) / vnaStepHz)
                        : 0;
                    if (idx < 0) idx = 0;
                    if (idx >= vnaPoints) idx = vnaPoints - 1;

                    if (double.TryParse(initial[idx], out double phaseDeg))
                        chuxiang[i] = phaseDeg.ToString();
                    else
                        chuxiang[i] = initial[idx]?.Trim() ?? "";
                }

                WriteArrayToExcelColumn(chuxiang, 13, sheetName);
                LogToConsole($"发射初相测试完成，已写入{chuxiang.Length}个频点到{sheetName}");
            }
            catch (Exception ex)
            {
                LogToConsole($"发射初相测试异常：{ex.Message}");
            }
            finally
            {
                await scpiDevice.DisableOutput();
                scpiDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge();
            }
        }
        /// <summary>
        /// 杂散抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button10_Click(object sender, EventArgs e)
        {
            var xinhaoBenzhenDevice = new ScpiDevice();
            var pinpuDevice = new ScpiDevice();
            try
            {
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
                if (ch5_checkBox.Checked)
                {
                    ch = $"通道5-{testType}";
                    chNum = 5;
                }
                if (ch6_checkBox.Checked)
                {
                    ch = $"通道6-{testType}";
                    chNum = 6;
                }
                if (ch7_checkBox.Checked)
                {
                    ch = $"通道7-{testType}";
                    chNum = 7;
                }
                if (ch8_checkBox.Checked)
                {
                    ch = $"通道8-{testType}";
                    chNum = 8;
                }
                string sheetName = $"测试结果{chNum}";

                await ChargeSendPowerON(); // 发射加电
                await Task.Delay(500);
                await SendTestUDP(); // FPGA发包
                await Task.Delay(500);

                string[] freqArray = GetFilterFreqArray(_pointCount);
                string[] fasheYizhi = new string[_pointCount];

                bool sgConnected = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                bool pinpuConnected = await pinpuDevice.ConnectAsync(_pinpuAddress);
                if (!sgConnected || !pinpuConnected)
                {
                    LogToConsole("连接失败：信号源或频谱无法连接");
                    return;
                }
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb_FS());
                await xinhaoBenzhenDevice.EnableRfOutput();

                const double markerStepHz = 0.1e9;   // 0.1 GHz
                const double innerOffsetHz = 0.3e9;  // 距中心 0.3 GHz
                const double outerOffsetHz = 1.5e9;  // 距中心 1.5 GHz

                await pinpuDevice.SendCommandAsync(":INST:SEL SA");
                await pinpuDevice.LoadPinpuStateAsync("/usrdata/Data/xdbfzasan.state");
                await Task.Delay(500);
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");

                int num = 0;
                SafeSetProgressBarMaximum(_pointCount, 0);

                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqGHz = double.Parse(freqArray[i]);

                    await xinhaoBenzhenDevice.SetFrequency(freqHz);
                    await Task.Delay(500);

                    // 频谱扫宽覆盖两侧扫描区间 [fc-1.5G, fc+1.5G]
                    await pinpuDevice.SetStartFrequencyAsync(freqHz - outerOffsetHz);
                    await pinpuDevice.SetStopFrequencyAsync(freqHz + outerOffsetHz);
                    await pinpuDevice.SetCenterFrequencyAsync(freqHz);
                    await Task.Delay(500);

                    // 主谱功率：Marker 寻峰
                    double centerPower = double.NaN;
                    for (int j = 0; j < 10; j++)
                    {
                        await pinpuDevice.SendCommandAsync(":CALC:MARK1:MAX");
                        await Task.Delay(200);
                        centerPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                        if (!double.IsNaN(centerPower) && centerPower > -10 && centerPower < 10)
                            break;
                        await Task.Delay(300);
                    }

                    if (double.IsNaN(centerPower))
                    {
                        fasheYizhi[i] = "";
                        LogToConsole($"{freqGHz} GHz: 主谱功率无效，跳过本点");
                    }
                    else
                    {
                        // 与接收带外抑制相同：两侧区间扫描 |P0-P| 最大值
                        double lowDiff = await MeasureMaxPowerDifferenceInMarkerSpanAsync(
                            pinpuDevice, freqHz, centerPower, markerStepHz,
                            freqHz - outerOffsetHz, freqHz - innerOffsetHz);
                        double highDiff = await MeasureMaxPowerDifferenceInMarkerSpanAsync(
                            pinpuDevice, freqHz, centerPower, markerStepHz,
                            freqHz + innerOffsetHz, freqHz + outerOffsetHz);

                        double result = Math.Max(lowDiff, highDiff);
                        fasheYizhi[i] = result.ToString("F3");
                        LogToConsole($"发射抑制测试:{freqGHz} GHz: 主谱={centerPower:F3}, " +
                            $"低侧={lowDiff:F3}, 高侧={highDiff:F3}, 杂散抑制={result:F3}");
                    }

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                WriteFasheyizhiToMatchingFrequencyRows(freqArray, fasheYizhi, sheetName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射抑制测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射抑制测试失败", ex.ToString(), operator_textBox.Text);
            }
            finally
            {
                await xinhaoBenzhenDevice.DisableRfOutput();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseCharge();
            }
        }
        #endregion

        #region 接收测试
        /// <summary>
        /// 接收测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <summary>
        /// 接收测试（一次扫频完成：接收增益、镜频抑制、带外抑制、-1dB带宽、平坦度）
        /// </summary>
        private async void button11_Click(object sender, EventArgs e)
        {
            await RunCombinedReceiveTestsAsync();
        }

        /// <summary>
        /// 合并接收指标测试：弹窗勾选后，每个射频频点共用一次 RF/本振设置，
        /// 按勾选依次完成 增益 → 平坦度 → 带外抑制 → -1dB带宽 → 镜频抑制；
        /// 最后（需切换矢网状态文件 502.csa）完成驻波。
        /// </summary>
        private async Task RunCombinedReceiveTestsAsync()
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(operator_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool testGain, testImage, testOutOfBand, testBandwidth, testFlatness, testVswr;
            using (var selectForm = new ReceiveTestSelect_Form())
            {
                if (selectForm.ShowDialog(this) != DialogResult.OK)
                    return;
                testGain = selectForm.TestGain;
                testImage = selectForm.TestImage;
                testOutOfBand = selectForm.TestOutOfBand;
                testBandwidth = selectForm.TestBandwidth;
                testFlatness = selectForm.TestFlatness;
                testVswr = selectForm.TestVswr;
            }

            bool needSpectrumTests = testGain || testImage || testOutOfBand || testBandwidth || testFlatness;

            double rfPower;
            try
            {
                rfPower = GetRfPowerDb();
                GetBenzhenPowerDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var selectedNames = new List<string>();
            if (testGain) selectedNames.Add("增益");
            if (testImage) selectedNames.Add("镜频");
            if (testOutOfBand) selectedNames.Add("带外");
            if (testBandwidth) selectedNames.Add("-1dB带宽");
            if (testFlatness) selectedNames.Add("平坦度");
            if (testVswr) selectedNames.Add("驻波");
            LogToConsole($"开始接收综合测试（{string.Join("/", selectedNames)}）...");
            WritePersonToAllSheets();
            await ChargeRecievePowerON();
            await Task.Delay(500);
            await RecieveTestUDP();
            await Task.Delay(500);

            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();

            try
            {
                int chNum = 0;
                if (ch1_checkBox.Checked) chNum = 1;
                if (ch2_checkBox.Checked) chNum = 2;
                if (ch3_checkBox.Checked) chNum = 3;
                if (ch4_checkBox.Checked) chNum = 4;
                if (ch5_checkBox.Checked) chNum = 5;
                if (ch6_checkBox.Checked) chNum = 6;
                if (ch7_checkBox.Checked) chNum = 7;
                if (ch8_checkBox.Checked) chNum = 8;
                string sheetName = $"测试结果{chNum}";

                bool connected = await xinhaoDevice.ConnectAsync(_vnaAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                bool connected3 = !needSpectrumTests || await pinpuDevice.ConnectAsync(_pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                const double centerIfHz = 175e6;
                const double markerStepHz = 0.1e6;
                const double flatSpanHalfHz1 = 10e6;
                const double flatSpanHalfHz2 = 20e6;
                const double lowBandMinHz = 130e6;
                const double lowBandMaxHz = 160e6;
                const double highBandMinHz = 200e6;
                const double highBandMaxHz = 210e6;
                double saMinHz = centerIfHz - 100e6;
                double saMaxHz = centerIfHz + 100e6;

                double jsbc = needSpectrumTests ? double.Parse(jsbc_textBox.Text) : 0;
                string[] freqArray = new string[_pointCount];
                string[] gain = new string[_pointCount];
                string[] bandwidthMHz = new string[_pointCount];
                string[] jpyz = new string[_pointCount];
                string[] outOfBand = new string[_pointCount];
                string[] flatness1 = new string[_pointCount];
                string[] flatness2 = new string[_pointCount];
                string[] inputVswr = new string[_pointCount];

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                int progressTotal = (needSpectrumTests ? _pointCount : 0) + (testVswr ? _pointCount : 0);
                int num = 0;
                SafeSetProgressBarMaximum(Math.Max(progressTotal, 1), 0);

                if (needSpectrumTests)
                {
                    await pinpuDevice.SendCommandAsync(":INST:SEL SA");
                    await pinpuDevice.LoadPinpuStateAsync("/usrdata/Data/xdbf20w.state");
                    //await pinpuDevice.SetStartFrequencyAsync(saMinHz);
                    //await pinpuDevice.SetStopFrequencyAsync(saMaxHz);
                    //await pinpuDevice.SetCenterFrequencyAsync(centerIfHz);
                    await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");

                    await xinhaoDevice.LoadStateFile("50.csa");
                    await xinhaoDevice.SetPower(rfPower, _vnaRfPortNum);
                    await xinhaoDevice.EnableOutput();
                    await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                    await xinhaoBenzhenDevice.EnableRfOutput();

                    for (int i = 0; i < _pointCount; i++)
                    {
                        double freqHz = _startFreq + step * i;
                        double freqGHz = Math.Round(freqHz / 1e9, 3);
                        freqArray[i] = freqGHz.ToString();
                        gain[i] = bandwidthMHz[i] = jpyz[i] = outOfBand[i] = flatness1[i] = "";

                        await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                        await Task.Delay(200);
                        await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                        await Task.Delay(200);

                        await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                        await Task.Delay(100);
                        double centerPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                        if (double.IsNaN(centerPower))
                        {
                            LogToConsole($"{freqGHz} GHz: 中心功率无效，跳过本点");
                        }
                        else
                        {
                            var pointLog = new List<string>();

                            if (testGain)
                            {
                                gain[i] = (centerPower + jsbc).ToString("F2");
                                pointLog.Add($"增益={gain[i]}");
                            }

                            if (testFlatness)
                            {
                                // 平坦度：Marker 与本振同步 0.1 MHz 步进（±10 MHz），含杂峰过滤
                                double flatDiff1 = await MeasureFlatnessWithLoStepAsync(
                                    pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, centerPower,
                                    markerStepHz, centerIfHz - flatSpanHalfHz1, centerIfHz + flatSpanHalfHz1);
                                flatness1[i] = flatDiff1.ToString("F2");
                                pointLog.Add($"平坦度={flatness1[i]}");
                            }

                            if (testFlatness)
                            {
                                // 平坦度：Marker 与本振同步 0.1 MHz 步进（±20 MHz），含杂峰过滤
                                double flatDiff2 = await MeasureFlatnessWithLoStepAsync(
                                    pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, centerPower,
                                    markerStepHz, centerIfHz - flatSpanHalfHz2, centerIfHz + flatSpanHalfHz2);
                                flatness2[i] = flatDiff2.ToString("F2");
                                pointLog.Add($"平坦度={flatness2[i]}");
                            }

                            if (testOutOfBand)
                            {
                                // 带外：仅移 Marker（本振保持中心）
                                double lowDiff = await MeasureMaxPowerDifferenceInMarkerSpanAsync(
                                    pinpuDevice, centerIfHz, centerPower, markerStepHz, lowBandMinHz, lowBandMaxHz);
                                double highDiff = await MeasureMaxPowerDifferenceInMarkerSpanAsync(
                                    pinpuDevice, centerIfHz, centerPower, markerStepHz, highBandMinHz, highBandMaxHz);
                                outOfBand[i] = Math.Max(lowDiff, highDiff).ToString("F2");
                                pointLog.Add($"带外={outOfBand[i]}");
                            }

                            if (testBandwidth)
                            {
                                // -1dB带宽前先回到中心
                                await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                                await Task.Delay(50);

                                double targetPower = centerPower - 1.0;
                                double? leftFreq = await FindMinus1dBMarkerFrequencyWithLoStepAsync(
                                    pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, targetPower,
                                    markerStepHz, -1, saMinHz, saMaxHz);

                                await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                                await Task.Delay(50);

                                double? rightFreq = await FindMinus1dBMarkerFrequencyWithLoStepAsync(
                                    pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, targetPower,
                                    markerStepHz, 1, saMinHz, saMaxHz);

                                await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");

                                if (leftFreq.HasValue && rightFreq.HasValue)
                                    bandwidthMHz[i] = ((rightFreq.Value - leftFreq.Value) / 1e6).ToString("F2");

                                pointLog.Add($"-1dB带宽={bandwidthMHz[i]} MHz");
                            }

                            if (testImage)
                            {
                                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                                await xinhaoDevice.SetCenterFrequencyAsync(freqHz - 350e6);
                                await Task.Delay(300);
                                double imagePower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                                if (!double.IsNaN(imagePower))
                                    jpyz[i] = (imagePower - centerPower).ToString("F2");

                                pointLog.Add($"镜频={jpyz[i]} dB");
                            }

                            if (pointLog.Count > 0)
                                LogToConsole($"{freqGHz} GHz: {string.Join(", ", pointLog)}");
                        }

                        num++;
                        SafeIncrementProgressBar();
                        label6.Text = ((double)num / progressTotal * 100).ToString("f2") + "%";
                        label6.Refresh();
                    }
                }

                // 驻波需切换矢网状态文件，放在最后单独扫频
                if (testVswr)
                {
                    LogToConsole("开始驻波测试（加载 502.csa）...");
                    await xinhaoDevice.LoadStateFile("502.csa");
                    await xinhaoDevice.SetPower(rfPower, _vnaRfPortNum);
                    await xinhaoDevice.EnableOutput();
                    await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                    await xinhaoBenzhenDevice.EnableRfOutput();

                    for (int i = 0; i < _pointCount; i++)
                    {
                        double freqHz = _startFreq + step * i;
                        double freqGHz = Math.Round(freqHz / 1e9, 3);
                        if (string.IsNullOrEmpty(freqArray[i]))
                            freqArray[i] = freqGHz.ToString();
                        inputVswr[i] = "";

                        await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                        await Task.Delay(200);
                        await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                        await Task.Delay(200);

                        await xinhaoDevice.ScanOnce(1);
                        await Task.Delay(100);
                        inputVswr[i] = await xinhaoDevice.GetInputVSWRStringAsync();
                        LogToConsole($"{freqGHz} GHz: 驻波={inputVswr[i]}");

                        num++;
                        SafeIncrementProgressBar();
                        label6.Text = ((double)num / progressTotal * 100).ToString("f2") + "%";
                        label6.Refresh();
                    }
                }

                // 仅写入勾选的列，避免覆盖 Excel 中未测项的旧数据
                WriteArrayToExcelColumn(freqArray, 1, sheetName);
                if (testGain) WriteArrayToExcelColumn(gain, 2, sheetName);
                if (testBandwidth) WriteArrayToExcelColumn(bandwidthMHz, 10, sheetName);
                if (testImage) WriteArrayToExcelColumn(jpyz, 11, sheetName);
                if (testOutOfBand) WriteArrayToExcelColumn(outOfBand, 12, sheetName);
                if (testFlatness) WriteArrayToExcelColumn(flatness1, 13, sheetName);
                if (testFlatness) WriteArrayToExcelColumn(flatness2, 14, sheetName);
                if (testVswr) WriteArrayToExcelColumn(inputVswr, 15, sheetName);
                LogToConsole("接收综合测试完成，已写入Excel");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接收综合测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("接收综合测试失败", ex.ToString(), operator_textBox.Text);
                LogToConsole("接收综合测试出错: " + ex.Message);
            }
            finally
            {
                await xinhaoBenzhenDevice.DisableRfOutput();
                await xinhaoDevice.DisableOutput();
                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge();
                LogToConsole("接收综合测试已结束");
            }
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
                if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
                {
                    MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string ch = "";
                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;

                //进度条
                int num = 0;
                SafeSetProgressBarMaximum(_pointCount, 0);

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
                if (ch5_checkBox.Checked)
                {
                    ch = $"通道5-{testType}";
                }
                if (ch6_checkBox.Checked)
                {
                    ch = $"通道6-{testType}";
                }
                if (ch7_checkBox.Checked)
                {
                    ch = $"通道7-{testType}";
                }
                if (ch8_checkBox.Checked)
                {
                    ch = $"通道8-{testType}";
                }

                await ChargeRecievePowerON(); // 接收加电
                await Task.Delay(500);     // 延时保证设备稳定
                RecieveTestUDP();
                await Task.Delay(500);     // 延时保证设备稳定

                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await pinpuDevice.ConnectAsync(_pinpuAddress);

                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }

                await pinpuDevice.SendCommandAsync(":INST:SEL NFIGURE");
                await pinpuDevice.SendCommandAsync(":MMEM:LOAD:STATe '/usrdata/Data/xdbfzs.sta'");

                LogToConsole("噪声采集");
                pinpuDevice.Disconnect();
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
                if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
                {
                    MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string ch = "";
                string testType = testType_comboBox.Text;
                int chNum = 0;

                //进度条：三衰减态 × 频点数
                int num = 0;
                SafeSetProgressBarMaximum(_pointCount * 3, 0);

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
                if (ch5_checkBox.Checked)
                {
                    ch = $"通道5-{testType}";
                    chNum = 5;
                }
                if (ch6_checkBox.Checked)
                {
                    ch = $"通道6-{testType}";
                    chNum = 6;
                }
                if (ch7_checkBox.Checked)
                {
                    ch = $"通道7-{testType}";
                    chNum = 7;
                }
                if (ch8_checkBox.Checked)
                {
                    ch = $"通道8-{testType}";
                    chNum = 8;
                }
                string sheetName = $"测试结果{chNum}";

                await ChargeRecievePowerON(); // 接收加电
                await Task.Delay(500);     // 延时保证设备稳定

                string[] p_1input = new string[_pointCount];
                string[] p_1output = new string[_pointCount];
                string[] p_1input2 = new string[_pointCount];
                string[] p_1output2 = new string[_pointCount];
                string[] p_1input3 = new string[_pointCount];
                string[] p_1output3 = new string[_pointCount];
                string[] freqArray = GetFilterFreqArray(_pointCount);
                double p1bc = double.Parse(p1bc_textBox.Text);
                double jsbc = double.Parse(jsbc_textBox.Text);
                LogToConsole("压缩点测试（三衰减态）");
                ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
                ScpiDevice xinhaoDevice = new ScpiDevice();
                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                bool connected2 = await xinhaoDevice.ConnectAsync(_vnaAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(_pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await pinpuDevice.LoadPinpuStateAsync("/usrdata/Data/xdbf20w.state");
                double center = 175 * 1e6;
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                double markPower = double.NaN;
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");

                // 三衰减态对应不同发码与功率扫描区间
                double[] startPowers = { -75, -55, -35 };
                double[] stopPowers = { -55, -35, -15 };
                string[][] inputResults = { p_1input, p_1input2, p_1input3 };
                string[][] outputResults = { p_1output, p_1output2, p_1output3 };
                string[] stateNames = { "衰减态1(RecieveTestUDP)", "衰减态2(RecieveTestUDPFullAtt2)", "衰减态3(RecieveTestUDPFullAtt3)" };
                double stepPower = 0.5;

                await xinhaoDevice.LoadStateFile("50.csa");
                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                await xinhaoBenzhenDevice.EnableRfOutput();

                double step = (_stopFreq - _startFreq) / (_pointCount - 1);

                for (int state = 0; state < 3; state++)
                {
                    LogToConsole($"开始{stateNames[state]}，功率扫描 {startPowers[state]}~{stopPowers[state]} dBm");
                    if (state == 0)
                        await RecieveTestUDP();
                    else if (state == 1)
                        await RecieveTestUDPFullAtt2();
                    else
                        await RecieveTestUDPFullAtt3();
                    await Task.Delay(500);

                    double startPower = startPowers[state];
                    double stopPower = stopPowers[state];
                    string[] inputArr = inputResults[state];
                    string[] outputArr = outputResults[state];

                    await xinhaoDevice.SetPower(startPower, _vnaRfPortNum);
                    await Task.Delay(500);

                    double[] refGains = new double[_pointCount];
                    bool[] found = new bool[_pointCount];

                    // 1. 各频点在起始功率下测参考功率
                    for (int i = 0; i < _pointCount; i++)
                    {
                        double freqHz = _startFreq + step * i;
                        double freqGHz = Math.Round(freqHz / 1e9, 3);
                        if (state == 0)
                            freqArray[i] = freqGHz.ToString();
                        await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                        await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                        markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                        refGains[i] = markPower;
                        found[i] = false;
                    }

                    // 2. 升功率查找 1dB 压缩点
                    for (int i = 0; i < _pointCount; i++)
                    {
                        double freqHz = _startFreq + step * i;
                        double freqGHz = Math.Round(freqHz / 1e9, 3);
                        await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                        await xinhaoDevice.SetCenterFrequencyAsync(freqHz);

                        for (double power = startPower + stepPower; power <= stopPower && !found[i]; power += stepPower)
                        {
                            await xinhaoDevice.SetPower(power, _vnaRfPortNum);
                            await Task.Delay(800);
                            markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                            markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                            double delta = (power + jsbc - 31.5) - (markPower - refGains[i]);
                            LogToConsole($"{stateNames[state]} {freqGHz}GHz @ {power:F1}dBm: Δ={delta:F3}");
                            if (delta > 1)
                            {
                                inputArr[i] = power.ToString("F2");
                                outputArr[i] = (p1bc + markPower).ToString("F2");
                                found[i] = true;
                                LogToConsole($"压缩点: Pin={inputArr[i]}, Pout={outputArr[i]}");
                            }
                        }

                        if (!found[i])
                        {
                            inputArr[i] = "";
                            outputArr[i] = "";
                            LogToConsole($"{stateNames[state]} 频点 {freqGHz} GHz 未找到压缩点");
                        }

                        num++;
                        SafeIncrementProgressBar();
                        label6.Text = ((double)num / (_pointCount * 3) * 100).ToString("f2") + "%";
                        label6.Refresh();
                    }
                }

                await xinhaoBenzhenDevice.DisableRfOutput();
                await xinhaoDevice.DisableOutput();
                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseCharge();
                await CloseFPGA();
                WriteArrayToExcelColumn(p_1input, 4, sheetName);
                WriteArrayToExcelColumn(p_1output, 5, sheetName);
                WriteArrayToExcelColumn(p_1input2, 6, sheetName);
                WriteArrayToExcelColumn(p_1output2, 7, sheetName);
                WriteArrayToExcelColumn(p_1input3, 8, sheetName);
                WriteArrayToExcelColumn(p_1output3, 9, sheetName);
                LogToConsole("压缩点测试完成（三衰减态）");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"压缩点测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("压缩点测试失败", ex.ToString(), operator_textBox.Text);
            }
        }
        /// <summary>
        /// 已整合到「接收测试」(button11)，请点击接收测试一次性完成。
        /// </summary>
        private async void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("该项已整合到「接收测试」中，请点击「接收测试」一次性完成增益/镜频/带外/-1dB带宽/平坦度。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 已整合到「接收测试」(button11)，请点击接收测试一次性完成。
        /// </summary>
        private async void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("该项已整合到「接收测试」中，请点击「接收测试」一次性完成增益/镜频/带外/-1dB带宽/平坦度。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 衰减误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button6_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始衰减精度测试...");
            WritePersonToAllSheets();
            //LoadVNAState(); // 调用矢网文件
            await ChargeRecievePowerON(); // 加电
            await Task.Delay(500);
            await RecieveTestUDP();
            await Task.Delay(500);
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
            if (ch5_checkBox.Checked)
            {
                ch = $"通道5-{testType}";
                chNum = 5;
            }
            if (ch6_checkBox.Checked)
            {
                ch = $"通道6-{testType}";
                chNum = 6;
            }
            if (ch7_checkBox.Checked)
            {
                ch = $"通道7-{testType}";
                chNum = 7;
            }
            if (ch8_checkBox.Checked)
            {
                ch = $"通道8-{testType}";
                chNum = 8;
            }
            string sheetName = $"测试结果{chNum}";

            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();

            bool connected = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
            bool connected2 = await xinhaoDevice.ConnectAsync(_vnaAddress);
            bool connected3 = await pinpuDevice.ConnectAsync(_pinpuAddress);
            if (!connected || !connected2 || !connected3)
            {
                LogToConsole("设备连接失败");
                return;
            }

            List<double[]> unwrappedPhases = new List<double[]>();
            double[] previousPhase = null;
            double[] phaseOffset = null;
            double[] zeroPhase = null;

            await pinpuDevice.LoadPinpuStateAsync("/usrdata/Data/xdbf20w.state");
            double center = 175 * 1e6;
            double start = center - (100 * 1e6);
            double stop = center + (100 * 1e6);
            await pinpuDevice.SetStartFrequencyAsync(start);
            await pinpuDevice.SetStopFrequencyAsync(stop);
            await pinpuDevice.SetCenterFrequencyAsync(center);
            await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
            double markPower = double.NaN;
            await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");

            await xinhaoDevice.LoadStateFile("50.csa");
            await xinhaoDevice.SetPower(GetRfPowerDb(), _vnaRfPortNum);
            await xinhaoDevice.EnableOutput();
            await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
            await xinhaoBenzhenDevice.EnableRfOutput();

            //进度条
            int num = 0;
            SafeSetProgressBarMaximum(_pointCount * 64, 0);

            double step = (_stopFreq - _startFreq) / (_pointCount - 1);
            //string[] freqArray = new string[_pointCount];

            for (int idx = 0; idx < 64; idx++)
            {
                if(idx == 0)
                {
                    await RecieveTestUDP();
                }
                else
                {
                    string numToString = ToSixBitBinaryString(idx);

                    LogToConsole("idx:" + idx + ",bitString:" + numToString);
                    string ch1recive = ch1_checkBox.Checked ? "1" : "0";
                    string ch2recive = ch2_checkBox.Checked ? "1" : "0";
                    string ch3recive = ch3_checkBox.Checked ? "1" : "0";
                    string ch4recive = ch4_checkBox.Checked ? "1" : "0";
                    string ch5recive = ch5_checkBox.Checked ? "1" : "0";
                    string ch6recive = ch6_checkBox.Checked ? "1" : "0";
                    string ch7recive = ch7_checkBox.Checked ? "1" : "0";
                    string ch8recive = ch8_checkBox.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + ch1recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + ch2recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + ch3recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + ch4recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + ch5recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + ch6recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + ch7recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + ch8recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string model = "10";
                    string model_stc = model + numToString + "0";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                }

                await Task.Delay(500); // 让设备处理
                double[] gain = new double[_pointCount];
                double lastPower = double.NaN;
                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);

                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await Task.Delay(100);
                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    for (int j = 0; j < 10; j++)
                    {
                        // 将 marker 设置为最大点
                        await pinpuDevice.SendCommandAsync(":CALC:MARK1:MAX");
                        await Task.Delay(100); // 让设备处理
                                               // 读取 Marker 的功率值
                        lastPower = markPower;
                        markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                        // 判断是否为有效功率
                        if (markPower > -80 && markPower > lastPower)
                            break;
                    }
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    gain[i] = markPower;
                    LogToConsole(freqGHz + ": " + gain[i].ToString());
                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / (_pointCount * 64) * 100).ToString("f2") + "%";
                    label6.Refresh();
                }
                unwrappedPhases.Add(gain);
            }


            await xinhaoBenzhenDevice.DisableRfOutput();
            await xinhaoDevice.DisableOutput();

            xinhaoBenzhenDevice.Disconnect(); // 释放资源
            xinhaoDevice.Disconnect(); // 释放资源
            pinpuDevice.Disconnect();
            await CloseFPGA();
            await CloseCharge(); // 电源关电
            LogToConsole("等待数据写入");
            // 写入解包后的初相（第 i + 2 列）
            _excelTaskQueue.Add(async () =>
            {
                ProcessRxAttenuationAccuracyExcelFast(unwrappedPhases, chNum);
                await Task.CompletedTask;
            });
        }
        /// <summary>
        /// 已整合到「接收测试」(button11)，请点击接收测试一次性完成。
        /// </summary>
        private async void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("该项已整合到「接收测试」中，请点击「接收测试」一次性完成增益/镜频/带外/-1dB带宽/平坦度。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 从中心频点向一侧步进 Marker，同时本振按相同步进跟随（LO = RF - IF），
        /// 找到功率首次不高于目标功率（中心功率-1dB）的频率。
        /// </summary>
        /// <param name="direction">-1 向低频，+1 向高频</param>
        private async Task<double?> FindMinus1dBMarkerFrequencyWithLoStepAsync(
            ScpiDevice pinpuDevice,
            ScpiDevice loDevice,
            double rfFreqHz,
            double centerIfHz,
            double targetPowerDb,
            double stepHz,
            int direction,
            double minFreqHz,
            double maxFreqHz)
        {
            double freq = centerIfHz;
            int maxSteps = (int)Math.Ceiling((maxFreqHz - minFreqHz) / stepHz) + 1;

            for (int s = 0; s < maxSteps; s++)
            {
                freq += direction * stepHz;
                if (freq < minFreqHz || freq > maxFreqHz)
                    return null;

                await loDevice.SetFrequency(rfFreqHz - freq);
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {freq}");
                await Task.Delay(50);
                double power = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                if (!double.IsNaN(power) && power <= targetPowerDb)
                    return freq;
            }

            return null;
        }

        /// <summary>
        /// 从中心频点向一侧步进 Marker，找到功率首次不高于目标功率（中心功率-1dB）的频率。
        /// （仅移 Marker，本振不变）
        /// </summary>
        /// <param name="direction">-1 向低频，+1 向高频</param>
        private async Task<double?> FindMinus1dBMarkerFrequencyAsync(
            ScpiDevice pinpuDevice,
            double centerFreqHz,
            double targetPowerDb,
            double stepHz,
            int direction,
            double minFreqHz,
            double maxFreqHz)
        {
            double freq = centerFreqHz;
            int maxSteps = (int)Math.Ceiling((maxFreqHz - minFreqHz) / stepHz) + 1;

            for (int s = 0; s < maxSteps; s++)
            {
                freq += direction * stepHz;
                if (freq < minFreqHz || freq > maxFreqHz)
                    return null;

                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {freq}");
                await Task.Delay(50);
                double power = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                if (!double.IsNaN(power) && power <= targetPowerDb)
                    return freq;
            }

            return null;
        }

        /// <summary>
        /// 已整合到「接收测试」(button11)，请点击接收测试一次性完成。
        /// </summary>
        private async void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("该项已整合到「接收测试」中，请点击「接收测试」一次性完成增益/镜频/带外/-1dB带宽/平坦度。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 平坦度：在 [scanMin, scanMax] 内以 step 步进移动 Marker，同时本振按相同步进跟随
        /// （LO = RF - IF），返回 |P(f)-P0| 的最大值。结束后恢复中心 IF / LO。
        /// 对杂峰/掉锁点做过滤：功率相对 P0 跌落过大的点丢弃，再用中位数剔除残余离群值。
        /// </summary>
        /// <param name="spurDropRejectDb">相对 P0 功率跌落超过该值视为杂峰/无效，不参与统计（默认 15 dB）</param>
        /// <param name="outlierMarginDb">相对差值中位数的离群裕量（默认 5 dB）</param>
        private async Task<double> MeasureFlatnessWithLoStepAsync(
            ScpiDevice pinpuDevice,
            ScpiDevice loDevice,
            double rfFreqHz,
            double centerIfHz,
            double centerPowerDb,
            double stepHz,
            double scanMinIfHz,
            double scanMaxIfHz,
            double spurDropRejectDb = 15.0,
            double outlierMarginDb = 5.0)
        {
            var diffs = new List<double>();
            int spurRejected = 0;
            int steps = (int)Math.Round((scanMaxIfHz - scanMinIfHz) / stepHz);
            for (int s = 0; s <= steps; s++)
            {
                double ifFreq = scanMinIfHz + s * stepHz;
                if (Math.Abs(ifFreq - centerIfHz) < stepHz * 0.1)
                    continue; // 跳过中心点本身

                // Marker 与本振同步步进：LO = RF - IF
                await loDevice.SetFrequency(rfFreqHz - ifFreq);
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {ifFreq}");
                await Task.Delay(50);
                double power = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                if (double.IsNaN(power))
                    continue;

                // 功率相对中心跌落过大：多为杂峰/噪声底，不计入平坦度
                if (centerPowerDb - power > spurDropRejectDb)
                {
                    spurRejected++;
                    continue;
                }

                diffs.Add(Math.Abs(centerPowerDb - power));
            }

            // 恢复中心本振与 Marker
            await loDevice.SetFrequency(rfFreqHz - centerIfHz);
            await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
            await Task.Delay(50);

            if (diffs.Count == 0)
            {
                LogToConsole($"平坦度扫描无有效点（杂峰剔除 {spurRejected}）");
                return 0;
            }

            double maxDiff = FilterSpuriousDiffMax(diffs, outlierMarginDb, out int outlierRejected);
            if (spurRejected > 0 || outlierRejected > 0)
            {
                LogToConsole($"平坦度滤波: 有效点={diffs.Count}, 功率跌落剔除={spurRejected}, " +
                             $"离群剔除={outlierRejected}, 结果={maxDiff:F2} dB");
            }

            return maxDiff;
        }

        /// <summary>
        /// 用中位数剔除离群差值后取 max。阈值：diff &gt; median + marginDb。
        /// </summary>
        private static double FilterSpuriousDiffMax(List<double> diffs, double marginDb, out int rejected)
        {
            rejected = 0;
            if (diffs == null || diffs.Count == 0)
                return 0;
            if (diffs.Count < 5)
                return diffs.Max();

            var sorted = diffs.OrderBy(d => d).ToList();
            double median = sorted[sorted.Count / 2];
            double threshold = median + marginDb;
            var kept = diffs.Where(d => d <= threshold).ToList();
            rejected = diffs.Count - kept.Count;
            if (kept.Count == 0)
                return median;
            return kept.Max();
        }

        /// <summary>
        /// 在 [scanMin, scanMax] 内以 step 步进移动 Marker，返回 |P(f)-P0| 的最大值。
        /// （仅移 Marker，本振不变；用于带外抑制等）
        /// </summary>
        private async Task<double> MeasureMaxPowerDifferenceInMarkerSpanAsync(
            ScpiDevice pinpuDevice,
            double centerFreqHz,
            double centerPowerDb,
            double stepHz,
            double scanMinHz,
            double scanMaxHz)
        {
            double maxDiff = 0;
            int steps = (int)Math.Round((scanMaxHz - scanMinHz) / stepHz);
            for (int s = 0; s <= steps; s++)
            {
                double freq = scanMinHz + s * stepHz;
                if (Math.Abs(freq - centerFreqHz) < stepHz * 0.1)
                    continue; // 跳过中心点本身

                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {freq}");
                await Task.Delay(50);
                double power = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                if (double.IsNaN(power))
                    continue;

                double diff = Math.Abs(centerPowerDb - power);
                if (diff > maxDiff)
                    maxDiff = diff;
            }

            return maxDiff;
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
                SafeSetProgressBarMaximum(_pointCount * 3, 0);

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
                if (ch5_checkBox.Checked)
                {
                    ch = $"通道5-{testType}";
                    chNum = 5;
                }
                if (ch6_checkBox.Checked)
                {
                    ch = $"通道6-{testType}";
                    chNum = 6;
                }
                if (ch7_checkBox.Checked)
                {
                    ch = $"通道7-{testType}";
                    chNum = 7;
                }
                if (ch8_checkBox.Checked)
                {
                    ch = $"通道8-{testType}";
                    chNum = 8;
                }
                string sheetName = $"测试结果{chNum}";

                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP();       // FPGA发包
                await Task.Delay(500);     // 延时保证设备稳定
                //await WriteFreqArray();

                double[] refGains = new double[_pointCount];
                double[] refGains2 = new double[_pointCount];
                string[] gain = new string[_pointCount];
                string[] m20 = new string[_pointCount];
                string[] m40 = new string[_pointCount];
                string[] m45 = new string[_pointCount];
                string[] jpyz = new string[_pointCount];
                string[] mgc = new string[_pointCount];
                string[] freqArray = GetFilterFreqArray(_pointCount);
                LogToConsole("接收测试");

                ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
                ScpiDevice xinhaoDevice = new ScpiDevice();
                ScpiDevice pinpuDevice = new ScpiDevice();

                bool connected = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                bool connected2 = await xinhaoDevice.ConnectAsync(_vnaAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(_pinpuAddress);
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

                await xinhaoDevice.LoadStateFile("xinhaoyuan.csa");
                await xinhaoDevice.SetPower(GetRfPowerDb(), _vnaRfPortNum);
                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                await xinhaoBenzhenDevice.EnableRfOutput();

                await Task.Delay(500);
                double step = (_stopFreq - _startFreq) / (_pointCount - 1);
                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqHzd20 = freqHz - 10 * 1e6;
                    double freqHzp20 = freqHz + 10 * 1e6;
                    double freqHzd40 = freqHz - 20 * 1e6;
                    double freqHzp40 = freqHz + 20 * 1e6;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz);

                    await Task.Delay(1000); // 让设备处理
                    markPower = await pinpuDevice.ReadMarkerPowerAsync(1) ?? double.NaN;
                    markPower6 = await pinpuDevice.ReadMarkerPowerAsync(6) ?? double.NaN;
                    markPower7 = await pinpuDevice.ReadMarkerPowerAsync(7) ?? double.NaN;
                    gain[i] = (markPower + double.Parse(jsbc_textBox.Text)).ToString("F2");
                    refGains[i] = markPower;
                    refGains2[i] = markPower;

                    await xinhaoDevice.SetCenterFrequencyAsync(freqHzd20);
                    await Task.Delay(1000); // 让设备处理
                    markPower2 = await pinpuDevice.ReadMarkerPowerAsync(2) ?? double.NaN;

                    await xinhaoDevice.SetCenterFrequencyAsync(freqHzp20);
                    await Task.Delay(1000); // 让设备处理
                    markPower3 = await pinpuDevice.ReadMarkerPowerAsync(3) ?? double.NaN;

                    await xinhaoDevice.SetCenterFrequencyAsync(freqHzd40);
                    await Task.Delay(1000); // 让设备处理
                    markPower4 = await pinpuDevice.ReadMarkerPowerAsync(4) ?? double.NaN;

                    await xinhaoDevice.SetCenterFrequencyAsync(freqHzp40);
                    await Task.Delay(1000); // 让设备处理
                    markPower5 = await pinpuDevice.ReadMarkerPowerAsync(5) ?? double.NaN;



                    m45[i] = (Math.Min(markPower6, markPower7) - markPower).ToString("F2");
                    m20[i] = (markPower - Math.Min(markPower2, markPower3)).ToString("F2");
                    m40[i] = (markPower - Math.Min(markPower4, markPower5)).ToString("F2");

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / (_pointCount * 3) * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz - 350 * 1e6);

                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    LogToConsole(markPower + " - " + refGains[i] + " = " + (markPower - refGains[i]));
                    refGains[i] = markPower - refGains[i];
                    jpyz[i] = refGains[i].ToString("F2");

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / (_pointCount * 3) * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                await RecieveTestUDP();

                await Task.Delay(500); // 让设备处理

                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz);

                    await Task.Delay(1000); // 让设备处理
                                            // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    refGains2[i] = refGains2[i] - markPower - 20;
                    mgc[i] = refGains2[i].ToString("F2");

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / (_pointCount * 3) * 100).ToString("f2") + "%";
                    label6.Refresh();
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
        /// <summary>
        /// 安全地设置进度条的值，确保值在有效范围内
        /// </summary>
        /// <param name="value">要设置的值</param>
        private void SafeSetProgressBarValue(int value)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => SafeSetProgressBarValue(value)));
                return;
            }

            try
            {
                if (value < progressBar1.Minimum)
                    value = progressBar1.Minimum;
                else if (value > progressBar1.Maximum)
                    value = progressBar1.Maximum;

                progressBar1.Value = value;
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                LogToConsole($"进度条更新错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全地增加进度条的值
        /// </summary>
        private void SafeIncrementProgressBar()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(SafeIncrementProgressBar));
                return;
            }

            try
            {
                int newValue = progressBar1.Value + 1;
                if (newValue > progressBar1.Maximum)
                    newValue = progressBar1.Maximum;

                progressBar1.Value = newValue;
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                LogToConsole($"进度条递增错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全地设置进度条的最大值和当前值
        /// </summary>
        /// <param name="maximum">最大值</param>
        /// <param name="value">当前值，默认为0</param>
        private void SafeSetProgressBarMaximum(int maximum, int value = 0)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => SafeSetProgressBarMaximum(maximum, value)));
                return;
            }

            try
            {
                if (maximum < progressBar1.Minimum)
                    maximum = progressBar1.Minimum;

                progressBar1.Maximum = maximum;

                if (value < progressBar1.Minimum)
                    value = progressBar1.Minimum;
                else if (value > progressBar1.Maximum)
                    value = progressBar1.Maximum;

                progressBar1.Value = value;
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                LogToConsole($"进度条设置最大值错误: {ex.Message}");
            }
        }

        private dynamic _excelApp;
        private dynamic _workbook;
        private IntPtr _excelHwnd = IntPtr.Zero;
        private Panel _excelHostPanel;
        private System.Diagnostics.Process _excelProcess;
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(
            IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private async void InitializeDSO()
        {
            try
            {
                Panel excelHostPanel = new Panel();
                excelHostPanel.Dock = DockStyle.Fill;
                this.splitContainer1.Panel1.Controls.Add(excelHostPanel);
                _excelHostPanel = excelHostPanel;

                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    MessageBox.Show("未检测到 Excel");
                    return;
                }

                _excelApp = Activator.CreateInstance(excelType);
                _excelApp.Visible = true;
                _excelApp.DisplayAlerts = false;

                //string filePath = Path.Combine(excelPath, "测试模板.xls");
                string filePath = "";

                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "测试模板.xls");

                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"Excel 文件不存在：{filePath}");
                    return;
                }

                _workbook = _excelApp.Workbooks.Open(filePath);

                await WaitForExcelWindowAsync(_excelApp);

                // 获取 Excel 窗口句柄并保存进程引用
                IntPtr hwnd = IntPtr.Zero;
                try
                {
                    hwnd = (IntPtr)_excelApp.Hwnd;
                }
                catch
                {
                    // 如果 Hwnd 属性不可用，尝试通过进程查找窗口
                    System.Diagnostics.Process excelProcess = System.Diagnostics.Process.GetProcessesByName("EXCEL")
                        .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                    if (excelProcess != null)
                    {
                        hwnd = excelProcess.MainWindowHandle;
                        _excelProcess = excelProcess;
                    }
                }

                // 通过窗口句柄查找对应的进程并保存
                if (_excelProcess == null && hwnd != IntPtr.Zero)
                {
                    try
                    {
                        uint processId;
                        GetWindowThreadProcessId(hwnd, out processId);
                        if (processId > 0)
                        {
                            _excelProcess = System.Diagnostics.Process.GetProcessById((int)processId);
                        }
                    }
                    catch { }
                }

                // 如果仍然没有找到，尝试通过进程名查找最新的 Excel 进程
                if (_excelProcess == null)
                {
                    try
                    {
                        var excelProcesses = System.Diagnostics.Process.GetProcessesByName("EXCEL");
                        if (excelProcesses.Length > 0)
                        {
                            // 选择启动时间最近的进程
                            _excelProcess = excelProcesses
                                .OrderByDescending(p => p.StartTime)
                                .FirstOrDefault();
                        }
                    }
                    catch { }
                }

                if (hwnd == IntPtr.Zero)
                {
                    MessageBox.Show("无法获取 Excel 窗口句柄");
                    return;
                }

                _excelHwnd = hwnd;

                SetParent(hwnd, excelHostPanel.Handle);
                MoveWindow(hwnd, 0, 0, excelHostPanel.Width, excelHostPanel.Height, true);

                excelHostPanel.Resize += (s, e) =>
                {
                    if (hwnd != IntPtr.Zero)
                    {
                        MoveWindow(hwnd, 0, 0, excelHostPanel.Width, excelHostPanel.Height, true);
                    }
                };

                // 设置 Zoom 为 100（正常大小），而不是 false
                // Zoom 属性的有效范围通常是 10-400
                try
                {
                    if (_excelApp.ActiveWindow != null)
                    {
                        _excelApp.ActiveWindow.Zoom = 100;
                    }
                }
                catch
                {
                    // 如果设置 Zoom 失败，忽略错误（不是关键操作）
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("初始化 Excel 失败：" + ex.Message);
            }
        }
        private async Task WaitForExcelWindowAsync(dynamic excelApp)
        {
            for (int i = 0; i < 50; i++) // 最多等 5 秒
            {
                try
                {
                    if (excelApp.ActiveWindow != null)
                        return;
                }
                catch
                {
                    // ActiveWindow 尚未就绪
                }
                await Task.Delay(100);
            }

            throw new Exception("Excel 窗口未能初始化完成");
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
            //char[] reversed = binary.ToCharArray();
            //Array.Reverse(reversed);
            //return new string(reversed);
            return binary;
        }
        public string ToSixBitBinaryString3(int number)
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
        /*        public string ToSixBitBinaryString2(int number)
                {
                    if (number < 0 || number > 63)
                        throw new ArgumentOutOfRangeException(nameof(number), "输入必须在 0 到 63 之间。");

                    //return Convert.ToString(number, 2).PadLeft(6, '0');
                    string binary = Convert.ToString(number, 2).PadLeft(6, '0');
                    //char[] reversed = binary.ToCharArray();
                    //Array.Reverse(reversed);
                    //return new string(reversed);
                    return binary;
                }*/

        public string ToSixBitBinaryString2(int number)
        {
            /*            if (number < 0 || number > 63)
                            throw new ArgumentOutOfRangeException(nameof(number), "输入必须在 0 到 63 之间。");

                        // 1️⃣ 转6位二进制
                        string binary = Convert.ToString(number, 2).PadLeft(6, '0');

                        // 2️⃣ 反转
                        char[] reversed = binary.ToCharArray();
                        Array.Reverse(reversed);

            *//*            // 3️⃣ 取反（0->1, 1->0）
                        for (int i = 0; i < reversed.Length; i++)
                        {
                            reversed[i] = reversed[i] == '0' ? '1' : '0';
                        }*//*

                        return new string(reversed);*/
            if (number < 0 || number > 63)
                throw new ArgumentOutOfRangeException(nameof(number), "输入必须在 0 到 63 之间。");

            // 1️⃣ 转6位二进制
            string binary = Convert.ToString(number, 2).PadLeft(6, '0');

            // 2️⃣ 反转
            char[] reversed = binary.ToCharArray();
            Array.Reverse(reversed);

            // 3️⃣ 取反（0->1, 1->0）
            for (int i = 0; i < reversed.Length; i++)
            {
                reversed[i] = reversed[i] == '0' ? '1' : '0';
            }

            return new string(reversed);
        }

        private string[] GetFilterFreqArray(int countNum)
        {
            string[] freqArray = new string[countNum];
            double step = 0;
            if (countNum > 1)
            {
                step = (_stopFreq - _startFreq) / (countNum - 1);
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
                double freqHz = _startFreq + step * i;
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
                string filePath = "TestSet_DBF.json";
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
                        _startFreq = double.Parse(start_freq.ToString()) * 1e6; // 转换为Hz
                    else if (startFreqDanwei == "GHz")
                        _startFreq = double.Parse(start_freq.ToString()) * 1e9; // 转换为Hz
                }

                data.TryGetValue("stop_freq_textBox", out object stop_freq);
                if (stop_freq != null)
                {
                    if (stopFreqDanwei == "MHz")
                        _stopFreq = double.Parse(stop_freq.ToString()) * 1e6; // 转换为Hz
                    else if (stopFreqDanwei == "GHz")
                        _stopFreq = double.Parse(stop_freq.ToString()) * 1e9; // 转换为Hz
                }

                data.TryGetValue("point_count_textBox", out object point_count);
                if (point_count != null)
                {
                    _pointCount = int.Parse(point_count.ToString());
                }

                data.TryGetValue("power_textBox", out object _power);
                if (_power != null)
                {
                    _Power = double.Parse(_power.ToString());
                }

                data.TryGetValue("power2_textBox", out object _power2);
                if (_power2 != null)
                {
                    _PowerBenzhen = double.Parse(_power2.ToString());
                }

                data.TryGetValue("ch1_vol_textBox", out object ch1v);
                if (ch1v != null)
                {
                    _ch1_vol = double.Parse(ch1v.ToString());
                }

                data.TryGetValue("ch1_cur_textBox", out object ch1c);
                if (ch1c != null)
                {
                    _ch1_cur = double.Parse(ch1c.ToString());
                }

                data.TryGetValue("ch2_vol_textBox", out object ch2v);
                if (ch2v != null)
                {
                    _ch2_vol = double.Parse(ch2v.ToString());
                }

                data.TryGetValue("ch2_cur_textBox", out object ch2c);
                if (ch2c != null)
                {
                    _ch2_cur = double.Parse(ch2c.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载TestSet_DBF.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                data.TryGetValue("shiwang_textBox", out object shiwang);
                if (shiwang != null)
                {
                    _vnaAddress = shiwang.ToString();
                }

                data.TryGetValue("charge_textBox", out object charge);
                if (charge != null)
                {
                    _chargeAddress = charge.ToString();
                }

                data.TryGetValue("gonglv_textBox", out object gonglv);
                if (gonglv != null)
                {
                    _gonglvAddress = gonglv.ToString();
                }

                data.TryGetValue("xinhao_textBox", out object xinhao);
                if (xinhao != null)
                {
                    _xinhaoAddress = xinhao.ToString();
                }

                data.TryGetValue("xinhaoBenzhen_textBox", out object xinhaoBenzhen);
                if (xinhaoBenzhen != null)
                {
                    _xinhaoBenzhenAddress = xinhaoBenzhen.ToString();
                }

                data.TryGetValue("pinpu_textBox", out object pinpu);
                if (pinpu != null)
                {
                    _pinpuAddress = pinpu.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceAddress_DBF.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void GetDeviceFilesJson()
        {
            try
            {
                string filePath = "DeviceFiles_DBF.json";
                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                data.TryGetValue("textBox6", out object value);
                if (value != null)
                {
                    _excelPath = value.ToString();
                }

                data.TryGetValue("textBox1", out object value2);
                if (value2 != null)
                {
                    _vnaFilePath = value2.ToString();
                }

                data.TryGetValue("textBox7", out object value3);
                if (value3 != null)
                {
                    _jsonPath = value3.ToString();
                }

                data.TryGetValue("textBox8", out object value4);
                if (value4 != null)
                {
                    _buchangFilePath = value4.ToString();
                }

                data.TryGetValue("textBox9", out object value5);
                if (value5 != null)
                {
                    _shiwangChaSunPath = value5.ToString();
                }

                data.TryGetValue("textBox3", out object value6);
                if (value6 != null)
                {
                    _pinpuZhupuStatePath = value6.ToString();
                }

                data.TryGetValue("textBox4", out object value7);
                if (value7 != null)
                {
                    _pinpuDaiwaiyizhiPath = value7.ToString();
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("加载DeviceFiles_DBF.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Excel操作
        /// <summary>矢网作信号源时的射频功率（dBm）。</summary>
        private double GetRfPowerDb()
        {
            if (string.IsNullOrWhiteSpace(textBox_rf_power.Text) ||
                !double.TryParse(textBox_rf_power.Text.Trim(), out double power))
                throw new InvalidOperationException("请输入有效的矢网功率（textBox_rf_power）");
            return power;
        }

        /// <summary>信号发生器作本振时的功率（dBm）。</summary>
        private double GetBenzhenPowerDb()
        {
            if (string.IsNullOrWhiteSpace(textBox_benzhen_power.Text) ||
                !double.TryParse(textBox_benzhen_power.Text.Trim(), out double power))
                throw new InvalidOperationException("请输入有效的本振功率（textBox_benzhen_power）");
            return power;
        }
        private double GetBenzhenPowerDb_FS()
        {
            if (string.IsNullOrWhiteSpace(textBox_benzhen_power_fs.Text) ||
                !double.TryParse(textBox_benzhen_power_fs.Text.Trim(), out double power))
                throw new InvalidOperationException("请输入有效的本振功率（textBox_benzhen_power）");
            return power;
        }

        /// <summary>优先使用 InitializeDSO 打开的工作簿，避免 GetActiveObject 拿到其它 Excel 实例。</summary>
        private dynamic GetExcelWorkbook()
        {
            if (_workbook != null)
                return _workbook;

            dynamic excelApp = System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
            return excelApp?.ActiveWorkbook;
        }

        private dynamic FindWorksheet(dynamic workbook, string sheetName)
        {
            if (workbook == null || string.IsNullOrEmpty(sheetName))
                return null;

            foreach (dynamic sheet in workbook.Sheets)
            {
                if ((string)sheet.Name == sheetName)
                    return sheet;
            }
            return null;
        }

        private void WriteArrayToExcelColumn(string[] data, int columnIndex, string sheetName)
        {
            try
            {
                // 去除每个字符串的换行；null 视为空串，避免未赋值频点空引用
                string[] cleanedData = data.Select(s => (s ?? "").Replace("\n", "")).ToArray();

                dynamic workbook = GetExcelWorkbook();
                if (workbook == null)
                {
                    MessageBox.Show("未检测到 Excel 工作簿。");
                    return;
                }

                dynamic worksheet = FindWorksheet(workbook, sheetName);
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
                    worksheet.Cells[8 + i, columnIndex].Value2 = cleanedData[i];
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
                // 去除换行符；null 视为空串
                string[] cleanedData = data.Select(s => (s ?? "").Replace("\n", "")).ToArray();

                dynamic workbook = GetExcelWorkbook();
                if (workbook == null)
                {
                    MessageBox.Show("未检测到 Excel 工作簿。");
                    return;
                }

                dynamic worksheet = FindWorksheet(workbook, sheetName);
                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                // 清空 columnIndex 的 8行以下数据
                ClearExcelColumnBelowRow(worksheet, columnIndex, 8);

                // 写入数据 (仅 8–20 行)
                for (int i = 0; i < cleanedData.Length && i < _pointCount; i++) // 8~20 共 13 行
                {
                    int row = 8 + i;

                    // 从目标列取值
                    object targetObj = worksheet.Cells[row, targetColumnIndex].Value2;
                    double targetVal = 0;
                    if (targetObj != null)
                        double.TryParse(targetObj.ToString(), out targetVal);

                    // 当前数据转 double
                    if (double.TryParse(cleanedData[i], out double currentVal))
                    {
                        worksheet.Cells[row, columnIndex].Value2 = currentVal - targetVal;
                    }
                    else
                    {
                        worksheet.Cells[row, columnIndex].Value2 = cleanedData[i]; // 如果不是数值，就原样写入
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入 Excel 失败：" + ex.Message);
            }
        }
        private void ClearExcelColumnBelowRow(dynamic worksheet, int columnIndex, int startRow)
        {
            try
            {
                // xlUp = -4162
                int lastRow = (int)worksheet.Cells[worksheet.Rows.Count, columnIndex].End(-4162).Row;

                // 如果最后行在第 startRow 行或之后，清除从 startRow 到最后行之间的单元格
                if (lastRow >= startRow)
                {
                    dynamic clearRange = worksheet.Range[worksheet.Cells[startRow, columnIndex], worksheet.Cells[lastRow, columnIndex]];
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
                dynamic workbook = GetExcelWorkbook();
                dynamic worksheet = FindWorksheet(workbook, sheetName);
                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                int startRow = 8;
                int freqColumn = 1;   // A列
                int powerColumn = 18;
                //int xiaolvColumn = 13;
                int dingJiangColumn = 21;

                int usedRowCount = (int)worksheet.UsedRange.Rows.Count;

                for (int i = 0; i < freqArray.Length; i++)
                {
                    // 保留三位小数进行对比
                    string targetFreq = double.Parse(freqArray[i]).ToString("F3");

                    for (int row = startRow; row <= usedRowCount; row++)
                    {
                        object cellObj = worksheet.Cells[row, freqColumn].Text;
                        string cellValue = cellObj?.ToString()?.Trim() ?? "";

                        // Excel单元格内容保留三位小数进行对比
                        if (double.TryParse(cellValue, out double cellFreq))
                        {
                            string formattedCellFreq = cellFreq.ToString("F3");

                            if (formattedCellFreq == targetFreq)
                            {
                                worksheet.Cells[row, powerColumn].Value2 = powerArray[i];
                                //worksheet.Cells[row, xiaolvColumn].Value2 = xiaolvArray[i];
                                worksheet.Cells[row, dingJiangColumn].Value2 = dingJiang[i];
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
                // 清理字符串；null 视为空串
                string[] cleanedData = data.Select(s => (s ?? "").Trim().Replace("\n", "").Replace("\r", "")).ToArray();

                dynamic workbook = GetExcelWorkbook();
                dynamic worksheet = FindWorksheet(workbook, sheetName);

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
                            object baseObj = worksheet.Cells[row, 2].Value2;
                            double baseValue = 0.0;

                            if (baseObj != null && double.TryParse(baseObj.ToString(), out double parsedBase))
                            {
                                baseValue = parsedBase;
                            }

                            // 写入差值 = 当前值 - 基态值
                            worksheet.Cells[row, columnIndex].Value2 = currentValue - baseValue;
                        }
                        else
                        {
                            // 前两列直接写入原始值
                            worksheet.Cells[row, columnIndex].Value2 = currentValue;
                        }
                    }
                    else
                    {
                        // 如果不是数字，直接写原文本
                        worksheet.Cells[row, columnIndex].Value2 = cleanedData[i];
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
                dynamic workbook = GetExcelWorkbook();
                dynamic worksheet = FindWorksheet(workbook, sheetName);
                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                int startRow = 8;
                int freqColumn = 1;   // A列
                int fasheYizhiColumn = 22;

                int usedRowCount = (int)worksheet.UsedRange.Rows.Count;

                for (int i = 0; i < freqArray.Length; i++)
                {
                    // 保留三位小数进行对比
                    string targetFreq = double.Parse(freqArray[i]).ToString("F3");

                    for (int row = startRow; row <= usedRowCount; row++)
                    {
                        object cellObj = worksheet.Cells[row, freqColumn].Text;
                        string cellValue = cellObj?.ToString()?.Trim() ?? "";

                        // Excel单元格内容保留三位小数进行对比
                        if (double.TryParse(cellValue, out double cellFreq))
                        {
                            string formattedCellFreq = cellFreq.ToString("F3");

                            if (formattedCellFreq == targetFreq)
                            {
                                worksheet.Cells[row, fasheYizhiColumn].Value2 = fasheYizhi[i];
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
                dynamic workbook = GetExcelWorkbook();
                if (workbook == null)
                {
                    MessageBox.Show("未检测到 Excel 工作簿。");
                    return;
                }

                string personText = operator_textBox.Text.Trim();

                // 遍历所有工作表
                foreach (dynamic sheet in workbook.Sheets)
                {
                    sheet.Cells[1, 2].Value2 = personText; // B1 单元格
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
                // 去除换行；null 视为空串
                string[] cleanedData = data.Select(s => (s ?? "").Replace("\n", "")).ToArray();
                dynamic workbook = GetExcelWorkbook();
                dynamic worksheet = FindWorksheet(workbook, sheetName);
                if (worksheet == null)
                {
                    MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                    return;
                }

                int startRow = 8;
                int freqColumn = 1;   // A列
                int zaoshengColumn = 3;  // G列

                int usedRowCount = (int)worksheet.UsedRange.Rows.Count;

                for (int i = 0; i < freqArray.Length; i++)
                {
                    // 保留三位小数进行对比
                    string targetFreq = double.Parse(freqArray[i]).ToString("F3");

                    for (int row = startRow; row <= usedRowCount; row++)
                    {
                        object cellObj = worksheet.Cells[row, freqColumn].Text;
                        string cellValue = cellObj?.ToString()?.Trim() ?? "";

                        // Excel单元格内容保留三位小数进行对比
                        if (double.TryParse(cellValue, out double cellFreq))
                        {
                            string formattedCellFreq = cellFreq.ToString("F3");

                            if (formattedCellFreq == targetFreq)
                            {
                                worksheet.Cells[row, zaoshengColumn].Value2 = cleanedData[i];
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
        private static int[] GetTxPhaseBaseStateColumns()
        {
            return TxPhaseBaseStateIndices.Select(idx => idx + 2).ToArray();
        }

        /// <summary>参与 RMS 计算的基态列（排除参考态 000000 所在列）。</summary>
        private static int[] GetTxPhaseRmsColumns()
        {
            return TxPhaseBaseStateIndices.Where(idx => idx != 0).Select(idx => idx + 2).ToArray();
        }

        private static object[,] CreateColumnArray(int rowCount)
        {
            return new object[rowCount, 1];
        }

        private static void WriteColumnRange(dynamic sheet, int startRow, int col, object[,] data, int rowCount)
        {
            if (rowCount <= 0)
                return;
            dynamic range = sheet.Range[sheet.Cells[startRow, col], sheet.Cells[startRow + rowCount - 1, col]];
            range.Value2 = data;
        }

        private static object[,] ReadColumnRange(dynamic sheet, int startRow, int endRow, int col)
        {
            dynamic range = sheet.Range[sheet.Cells[startRow, col], sheet.Cells[endRow, col]];
            object raw = range.Value2;
            if (raw == null)
                return null;
            if (raw is object[,] arr)
                return arr;
            // 单单元格
            var single = CreateColumnArray(1);
            single[0, 0] = raw;
            return single;
        }

        private static bool TryGetArrayDouble(object[,] arr, int zeroBasedRow, out double value)
        {
            value = 0;
            if (arr == null)
                return false;
            // Excel COM 读回通常是 1-based；本机写入的 0-based 也可能原样返回
            int r0 = arr.GetLowerBound(0);
            int c0 = arr.GetLowerBound(1);
            object cell = arr[r0 + zeroBasedRow, c0];
            return cell != null && double.TryParse(cell.ToString(), out value);
        }

        /// <summary>
        /// 发射移相精度：批量 Range 读写 + 仅 Save 一次，避免逐格 COM 调用过慢。
        /// </summary>
        private void ProcessTxPhaseAccuracyExcelFast(
            List<double[]> unwrappedPhases,
            List<int> testedIndices,
            int chNum)
        {
            const string sheetName = "发射通道相移精度测试结果";
            LogToConsole("开始批量写入移相数据...");

            dynamic workbook = GetExcelWorkbook();
            dynamic phaseSheet = FindWorksheet(workbook, sheetName);
            dynamic resultSheet = FindWorksheet(workbook, $"测试结果{chNum}");
            if (phaseSheet == null || resultSheet == null)
            {
                MessageBox.Show($"未找到工作表“{sheetName}”或“测试结果{chNum}”。");
                return;
            }
            if (unwrappedPhases == null || unwrappedPhases.Count == 0)
            {
                LogToConsole("无移相数据可写入");
                return;
            }

            dynamic excelApp = null;
            object oldScreenUpdating = null;
            object oldCalculation = null;
            try
            {
                excelApp = workbook.Application;
                oldScreenUpdating = excelApp.ScreenUpdating;
                oldCalculation = excelApp.Calculation;
                excelApp.ScreenUpdating = false;
                excelApp.Calculation = -4135; // xlCalculationManual
            }
            catch { /* 忽略 Excel 属性设置失败 */ }

            try
            {
                int dataLen = unwrappedPhases[0].Length;
                int measureEnd = Math.Min(TxPhaseMeasureStartRow + dataLen - 1, TxPhaseMeasureEndRow);
                int rowCount = measureEnd - TxPhaseMeasureStartRow + 1;
                int diffEnd = TxPhaseDifferenceStartRow + (TxPhaseMeasureEndRow - TxPhaseMeasureStartRow);

                // 1) 整块清空测量区/差值区数值（保留 A 列频率），两次 COM 调用
                phaseSheet.Range[
                    phaseSheet.Cells[TxPhaseMeasureStartRow, 2],
                    phaseSheet.Cells[TxPhaseMeasureEndRow, 65]].ClearContents();
                phaseSheet.Range[
                    phaseSheet.Cells[TxPhaseDifferenceStartRow, 2],
                    phaseSheet.Cells[diffEnd, 65]].ClearContents();

                // 2) 先找基态列数据（索引 0 → 第 2 列），再写各基态列
                double[] basePhase = null;
                for (int i = 0; i < testedIndices.Count; i++)
                {
                    if (testedIndices[i] == 0)
                    {
                        basePhase = unwrappedPhases[i];
                        break;
                    }
                }
                if (basePhase == null)
                    basePhase = unwrappedPhases[0];

                for (int i = 0; i < unwrappedPhases.Count; i++)
                {
                    int col = testedIndices[i] + 2;
                    var colData = CreateColumnArray(rowCount);
                    double[] phase = unwrappedPhases[i];
                    int n = Math.Min(rowCount, phase.Length);
                    for (int r = 0; r < n; r++)
                    {
                        if (col == 2)
                            colData[r, 0] = phase[r];
                        else
                            colData[r, 0] = phase[r] - basePhase[Math.Min(r, basePhase.Length - 1)];
                    }
                    WriteColumnRange(phaseSheet, TxPhaseMeasureStartRow, col, colData, rowCount);
                }

                // 3) 批量读频率与标准值，内存中算差值后整列写回
                object[,] freqArr = ReadColumnRange(phaseSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, 1);
                int measureRowCount = TxPhaseMeasureEndRow - TxPhaseMeasureStartRow + 1;

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                var selectedFreqSet = new HashSet<string>(
                    Enumerable.Range(0, Math.Max(_pointCount, 0))
                        .Select(i => ((_startFreq + i * step) * 1e-9).ToString("F3")));

                int[] baseCols = GetTxPhaseBaseStateColumns();
                int[] rmsCols = GetTxPhaseRmsColumns();
                var rmsColSet = new HashSet<int>(rmsCols);

                // 差值区按列缓存，最后一次性写入；同时累计各行 RMS
                var diffByCol = new Dictionary<int, object[,]>();
                var rowSqSum = new double[measureRowCount];
                var rowSqCount = new int[measureRowCount];
                var rowIsSelected = new bool[measureRowCount];

                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;
                    rowIsSelected[r] = selectedFreqSet.Contains(freqGHz.ToString("F3"));
                }

                foreach (int col in baseCols)
                {
                    object standardObj = phaseSheet.Cells[3, col].Value2;
                    if (standardObj == null || !double.TryParse(standardObj.ToString(), out double standardValue))
                        continue;

                    object[,] measureCol = ReadColumnRange(phaseSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, col);
                    var diffCol = CreateColumnArray(measureRowCount);

                    for (int r = 0; r < measureRowCount; r++)
                    {
                        if (!rowIsSelected[r])
                            continue;
                        if (!TryGetArrayDouble(measureCol, r, out double measuredValue))
                            continue;

                        double result = measuredValue - standardValue;
                        diffCol[r, 0] = result;

                        if (rmsColSet.Contains(col))
                        {
                            rowSqSum[r] += result * result;
                            rowSqCount[r]++;
                        }
                    }

                    diffByCol[col] = diffCol;
                }

                foreach (var kv in diffByCol)
                    WriteColumnRange(phaseSheet, TxPhaseDifferenceStartRow, kv.Key, kv.Value, measureRowCount);

                // 4) 结果表频率一次读入建索引，再写 RMS
                object[,] resultFreqArr = ReadColumnRange(resultSheet, 8, 200, 1);
                var freqToResultRow = new Dictionary<string, int>();
                if (resultFreqArr != null)
                {
                    int rf0 = resultFreqArr.GetLowerBound(0);
                    int len = resultFreqArr.GetLength(0);
                    for (int i = 0; i < len; i++)
                    {
                        if (!TryGetArrayDouble(resultFreqArr, i, out double f))
                            continue;
                        string key = f.ToString("F3");
                        if (!freqToResultRow.ContainsKey(key))
                            freqToResultRow[key] = 8 + i;
                    }
                }

                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!rowIsSelected[r] || rowSqCount[r] <= 0)
                        continue;
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;

                    double rms = Math.Sqrt(rowSqSum[r] / rowSqCount[r]);
                    if (freqToResultRow.TryGetValue(freqGHz.ToString("F3"), out int resultRow))
                        resultSheet.Cells[resultRow, 17].Value2 = rms.ToString();
                }

                workbook.Save();
                LogToConsole("移相精度批量写入完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理移相精度时出错：" + ex.Message);
            }
            finally
            {
                try
                {
                    if (excelApp != null)
                    {
                        if (oldScreenUpdating != null)
                            excelApp.ScreenUpdating = oldScreenUpdating;
                        if (oldCalculation != null)
                            excelApp.Calculation = oldCalculation;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 发射寄生调幅精度：流程同 ProcessTxPhaseAccuracyExcelFast，
        /// 写入「发射通道寄生调幅测试结果」，RMS 写到「测试结果{chNum}」第 19 列；
        /// 差值/精度不减第 3 行标准值，直接对测量值（相对基态）做 RMS。
        /// </summary>
        private void ProcessTxParasiticAmplitudeExcelFast(
            List<double[]> gains,
            List<int> testedIndices,
            int chNum)
        {
            const string sheetName = "发射通道寄生调幅测试结果";
            LogToConsole("开始批量写入寄生调幅数据...");

            dynamic workbook = GetExcelWorkbook();
            dynamic ampSheet = FindWorksheet(workbook, sheetName);
            dynamic resultSheet = FindWorksheet(workbook, $"测试结果{chNum}");
            if (ampSheet == null || resultSheet == null)
            {
                MessageBox.Show($"未找到工作表“{sheetName}”或“测试结果{chNum}”。");
                return;
            }
            if (gains == null || gains.Count == 0)
            {
                LogToConsole("无寄生调幅数据可写入");
                return;
            }

            dynamic excelApp = null;
            object oldScreenUpdating = null;
            object oldCalculation = null;
            try
            {
                excelApp = workbook.Application;
                oldScreenUpdating = excelApp.ScreenUpdating;
                oldCalculation = excelApp.Calculation;
                excelApp.ScreenUpdating = false;
                excelApp.Calculation = -4135; // xlCalculationManual
            }
            catch { /* 忽略 Excel 属性设置失败 */ }

            try
            {
                int dataLen = gains[0].Length;
                int measureEnd = Math.Min(TxPhaseMeasureStartRow + dataLen - 1, TxPhaseMeasureEndRow);
                int rowCount = measureEnd - TxPhaseMeasureStartRow + 1;
                int diffEnd = TxPhaseDifferenceStartRow + (TxPhaseMeasureEndRow - TxPhaseMeasureStartRow);

                // 1) 整块清空测量区/差值区数值（保留 A 列频率）
                ampSheet.Range[
                    ampSheet.Cells[TxPhaseMeasureStartRow, 2],
                    ampSheet.Cells[TxPhaseMeasureEndRow, 65]].ClearContents();
                ampSheet.Range[
                    ampSheet.Cells[TxPhaseDifferenceStartRow, 2],
                    ampSheet.Cells[diffEnd, 65]].ClearContents();

                // 2) 基态列写绝对值，其余列写相对基态差值
                double[] baseGain = null;
                for (int i = 0; i < testedIndices.Count; i++)
                {
                    if (testedIndices[i] == 0)
                    {
                        baseGain = gains[i];
                        break;
                    }
                }
                if (baseGain == null)
                    baseGain = gains[0];

                for (int i = 0; i < gains.Count; i++)
                {
                    int col = testedIndices[i] + 2;
                    var colData = CreateColumnArray(rowCount);
                    double[] gain = gains[i];
                    int n = Math.Min(rowCount, gain.Length);
                    for (int r = 0; r < n; r++)
                    {
                        if (col == 2)
                            colData[r, 0] = gain[r];
                        else
                            colData[r, 0] = gain[r] - baseGain[Math.Min(r, baseGain.Length - 1)];
                    }
                    WriteColumnRange(ampSheet, TxPhaseMeasureStartRow, col, colData, rowCount);
                }

                // 3) 批量读频率；寄生调幅不减第 3 行标准值，直接用测量值写入差值区并累计 RMS
                object[,] freqArr = ReadColumnRange(ampSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, 1);
                int measureRowCount = TxPhaseMeasureEndRow - TxPhaseMeasureStartRow + 1;

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                var selectedFreqSet = new HashSet<string>(
                    Enumerable.Range(0, Math.Max(_pointCount, 0))
                        .Select(i => ((_startFreq + i * step) * 1e-9).ToString("F3")));

                int[] baseCols = GetTxPhaseBaseStateColumns();
                int[] rmsCols = GetTxPhaseRmsColumns();
                var rmsColSet = new HashSet<int>(rmsCols);

                var diffByCol = new Dictionary<int, object[,]>();
                var rowSqSum = new double[measureRowCount];
                var rowSqCount = new int[measureRowCount];
                var rowIsSelected = new bool[measureRowCount];

                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;
                    rowIsSelected[r] = selectedFreqSet.Contains(freqGHz.ToString("F3"));
                }

                foreach (int col in baseCols)
                {
                    object[,] measureCol = ReadColumnRange(ampSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, col);
                    var diffCol = CreateColumnArray(measureRowCount);

                    for (int r = 0; r < measureRowCount; r++)
                    {
                        if (!rowIsSelected[r])
                            continue;
                        if (!TryGetArrayDouble(measureCol, r, out double measuredValue))
                            continue;

                        // 不减第 3 行标准值
                        diffCol[r, 0] = measuredValue;

                        if (rmsColSet.Contains(col))
                        {
                            rowSqSum[r] += measuredValue * measuredValue;
                            rowSqCount[r]++;
                        }
                    }

                    diffByCol[col] = diffCol;
                }

                foreach (var kv in diffByCol)
                    WriteColumnRange(ampSheet, TxPhaseDifferenceStartRow, kv.Key, kv.Value, measureRowCount);

                // 4) 结果表按频率写入 RMS 到第 19 列
                object[,] resultFreqArr = ReadColumnRange(resultSheet, 8, 200, 1);
                var freqToResultRow = new Dictionary<string, int>();
                if (resultFreqArr != null)
                {
                    int len = resultFreqArr.GetLength(0);
                    for (int i = 0; i < len; i++)
                    {
                        if (!TryGetArrayDouble(resultFreqArr, i, out double f))
                            continue;
                        string key = f.ToString("F3");
                        if (!freqToResultRow.ContainsKey(key))
                            freqToResultRow[key] = 8 + i;
                    }
                }

                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!rowIsSelected[r] || rowSqCount[r] <= 0)
                        continue;
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;

                    double rms = Math.Sqrt(rowSqSum[r] / rowSqCount[r]);
                    if (freqToResultRow.TryGetValue(freqGHz.ToString("F3"), out int resultRow))
                        resultSheet.Cells[resultRow, 23].Value2 = rms.ToString();
                }

                workbook.Save();
                LogToConsole("寄生调幅精度批量写入完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理寄生调幅精度时出错：" + ex.Message);
            }
            finally
            {
                try
                {
                    if (excelApp != null)
                    {
                        if (oldScreenUpdating != null)
                            excelApp.ScreenUpdating = oldScreenUpdating;
                        if (oldCalculation != null)
                            excelApp.Calculation = oldCalculation;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 基于 Excel 中「发射通道寄生调幅测试结果」已有测量区数据重算：
        /// 不减第 3 行标准值，写差值区并对基态（除参考态）做 RMS，写入测试结果第 19 列。
        /// </summary>
        private void CalculateTxParasiticAmplitudeAccuracyFromExcel(int chNum)
        {
            const string sheetName = "发射通道寄生调幅测试结果";
            const int resultRmsColumn = 19;

            dynamic workbook = GetExcelWorkbook();
            dynamic ampSheet = FindWorksheet(workbook, sheetName);
            dynamic resultSheet = FindWorksheet(workbook, $"测试结果{chNum}");
            if (ampSheet == null)
            {
                MessageBox.Show($"未找到工作表“{sheetName}”。");
                return;
            }
            if (resultSheet == null)
            {
                MessageBox.Show($"未找到工作表“测试结果{chNum}”。");
                return;
            }

            LogToConsole($"开始重算寄生调幅精度（通道{chNum}）...");

            dynamic excelApp = null;
            object oldScreenUpdating = null;
            object oldCalculation = null;
            try
            {
                excelApp = workbook.Application;
                oldScreenUpdating = excelApp.ScreenUpdating;
                oldCalculation = excelApp.Calculation;
                excelApp.ScreenUpdating = false;
                excelApp.Calculation = -4135;
            }
            catch { }

            try
            {
                int measureRowCount = TxPhaseMeasureEndRow - TxPhaseMeasureStartRow + 1;
                int diffEnd = TxPhaseDifferenceStartRow + measureRowCount - 1;

                ampSheet.Range[
                    ampSheet.Cells[TxPhaseDifferenceStartRow, 2],
                    ampSheet.Cells[diffEnd, 65]].ClearContents();

                object[,] freqArr = ReadColumnRange(ampSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, 1);

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                var selectedFreqSet = new HashSet<string>(
                    Enumerable.Range(0, Math.Max(_pointCount, 0))
                        .Select(i => ((_startFreq + i * step) * 1e-9).ToString("F3")));

                int[] baseCols = GetTxPhaseBaseStateColumns();
                int[] rmsCols = GetTxPhaseRmsColumns();
                var rmsColSet = new HashSet<int>(rmsCols);

                var rowSqSum = new double[measureRowCount];
                var rowSqCount = new int[measureRowCount];
                var rowIsSelected = new bool[measureRowCount];

                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;
                    rowIsSelected[r] = selectedFreqSet.Contains(freqGHz.ToString("F3"));
                }

                foreach (int col in baseCols)
                {
                    object[,] measureCol = ReadColumnRange(ampSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, col);
                    var diffCol = CreateColumnArray(measureRowCount);

                    for (int r = 0; r < measureRowCount; r++)
                    {
                        if (!rowIsSelected[r])
                            continue;
                        if (!TryGetArrayDouble(measureCol, r, out double measuredValue))
                            continue;

                        // 不减第 3 行标准值
                        diffCol[r, 0] = measuredValue;

                        if (rmsColSet.Contains(col))
                        {
                            rowSqSum[r] += measuredValue * measuredValue;
                            rowSqCount[r]++;
                        }
                    }

                    WriteColumnRange(ampSheet, TxPhaseDifferenceStartRow, col, diffCol, measureRowCount);
                }

                object[,] resultFreqArr = ReadColumnRange(resultSheet, 8, 200, 1);
                var freqToResultRow = new Dictionary<string, int>();
                if (resultFreqArr != null)
                {
                    int len = resultFreqArr.GetLength(0);
                    for (int i = 0; i < len; i++)
                    {
                        if (!TryGetArrayDouble(resultFreqArr, i, out double f))
                            continue;
                        string key = f.ToString("F3");
                        if (!freqToResultRow.ContainsKey(key))
                            freqToResultRow[key] = 8 + i;
                    }
                }

                int written = 0;
                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!rowIsSelected[r] || rowSqCount[r] <= 0)
                        continue;
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;

                    double rms = Math.Sqrt(rowSqSum[r] / rowSqCount[r]);
                    if (freqToResultRow.TryGetValue(freqGHz.ToString("F3"), out int resultRow))
                    {
                        resultSheet.Cells[resultRow, resultRmsColumn].Value2 = rms.ToString();
                        written++;
                    }
                }

                workbook.Save();
                LogToConsole($"寄生调幅精度重算完成：写入 {written} 个频点 → 测试结果{chNum} 第 {resultRmsColumn} 列");
            }
            catch (Exception ex)
            {
                MessageBox.Show("重算寄生调幅精度失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (excelApp != null)
                    {
                        if (oldScreenUpdating != null)
                            excelApp.ScreenUpdating = oldScreenUpdating;
                        if (oldCalculation != null)
                            excelApp.Calculation = oldCalculation;
                    }
                }
                catch { }
            }
        }

        private void ClearTxPhaseNonBaseStateColumns(string sheetName)
        {
            dynamic workbook = GetExcelWorkbook();
            dynamic phaseSheet = FindWorksheet(workbook, sheetName);
            if (phaseSheet == null)
                return;

            int diffEndRow = TxPhaseDifferenceStartRow + (TxPhaseMeasureEndRow - TxPhaseMeasureStartRow);
            // 整块清空后由后续写入覆盖基态列，避免逐格 COM
            phaseSheet.Range[
                phaseSheet.Cells[TxPhaseMeasureStartRow, 2],
                phaseSheet.Cells[TxPhaseMeasureEndRow, 65]].ClearContents();
            phaseSheet.Range[
                phaseSheet.Cells[TxPhaseDifferenceStartRow, 2],
                phaseSheet.Cells[diffEndRow, 65]].ClearContents();
            workbook.Save();
        }

        public void SubtractStandardAndWriteResult(string sheetName)
        {
            LogToConsole("开始写入数据差值（按目标频率点过滤）");

            dynamic workbook = GetExcelWorkbook();
            dynamic phaseSheet = FindWorksheet(workbook, sheetName);
            if (phaseSheet == null)
            {
                MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                return;
            }

            int[] columns = GetTxPhaseBaseStateColumns();
            int measureRowCount = TxPhaseMeasureEndRow - TxPhaseMeasureStartRow + 1;

            double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
            var selectedFreqSet = new HashSet<string>(
                Enumerable.Range(0, Math.Max(_pointCount, 0))
                    .Select(i => ((_startFreq + i * step) * 1e-9).ToString("F3")));

            object[,] freqArr = ReadColumnRange(phaseSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, 1);
            var rowIsSelected = new bool[measureRowCount];
            for (int r = 0; r < measureRowCount; r++)
            {
                if (TryGetArrayDouble(freqArr, r, out double freqGHz))
                    rowIsSelected[r] = selectedFreqSet.Contains(freqGHz.ToString("F3"));
            }

            foreach (int col in columns)
            {
                object standardObj = phaseSheet.Cells[3, col].Value2;
                if (standardObj == null || !double.TryParse(standardObj.ToString(), out double standardValue))
                    continue;

                object[,] measureCol = ReadColumnRange(phaseSheet, TxPhaseMeasureStartRow, TxPhaseMeasureEndRow, col);
                var diffCol = CreateColumnArray(measureRowCount);
                for (int r = 0; r < measureRowCount; r++)
                {
                    if (!rowIsSelected[r])
                        continue;
                    if (!TryGetArrayDouble(measureCol, r, out double measuredValue))
                        continue;
                    diffCol[r, 0] = measuredValue - standardValue;
                }
                WriteColumnRange(phaseSheet, TxPhaseDifferenceStartRow, col, diffCol, measureRowCount);
            }

            workbook.Save();
            LogToConsole("差值写入完成（已按目标频率点过滤）");
        }
        public void SubtractStandardAndWriteResult_Jieshou(string sheetName)
        {
            LogToConsole("开始写入数据差值");
            dynamic workbook = GetExcelWorkbook();
            dynamic phaseSheet = FindWorksheet(workbook, sheetName);
            if (phaseSheet == null)
            {
                MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                return;
            }

            const int measureStartRow = RxAttenMeasureStartRow;
            const int measureEndRow = RxAttenMeasureEndRow;
            const int diffStartRow = RxAttenDifferenceStartRow;
            int rowCount = measureEndRow - measureStartRow + 1;

            for (int col = 2; col <= 65; col++)
            {
                object standardObj = phaseSheet.Cells[3, col].Value2;
                if (standardObj == null || !double.TryParse(standardObj.ToString(), out double standardValue))
                    continue;

                object[,] measureCol = ReadColumnRange(phaseSheet, measureStartRow, measureEndRow, col);
                var diffCol = CreateColumnArray(rowCount);
                for (int r = 0; r < rowCount; r++)
                {
                    if (!TryGetArrayDouble(measureCol, r, out double measuredValue))
                        continue;
                    diffCol[r, 0] = measuredValue - standardValue;
                }
                WriteColumnRange(phaseSheet, diffStartRow, col, diffCol, rowCount);
            }

            workbook.Save();
            LogToConsole("数据差值写入完成");
        }

        /// <summary>
        /// 接收衰减精度：批量 Range 读写 + 仅 Save 一次。
        /// 测量区 B4:BM16，差值区从第 23 行起；对 1～63 态算 RMS，写入测试结果第 11 列。
        /// </summary>
        private void ProcessRxAttenuationAccuracyExcelFast(List<double[]> unwrappedPhases, int chNum)
        {
            string sheetName = $"接收通道衰减精度测试结果{chNum}";
            LogToConsole("开始批量写入衰减精度数据...");

            dynamic workbook = GetExcelWorkbook();
            dynamic phaseSheet = FindWorksheet(workbook, sheetName);
            dynamic resultSheet = FindWorksheet(workbook, $"测试结果{chNum}");
            if (phaseSheet == null)
            {
                MessageBox.Show($"未找到名为“{sheetName}”的工作表。");
                return;
            }
            if (resultSheet == null)
            {
                MessageBox.Show($"未找到工作表“测试结果{chNum}”。");
                return;
            }
            if (unwrappedPhases == null || unwrappedPhases.Count == 0)
            {
                LogToConsole("无衰减精度数据可写入");
                return;
            }

            const int measureStartRow = RxAttenMeasureStartRow;
            const int measureEndRow = RxAttenMeasureEndRow;
            const int diffStartRow = RxAttenDifferenceStartRow;
            const int diffEndRow = RxAttenDifferenceEndRow;
            int maxRows = measureEndRow - measureStartRow + 1;

            dynamic excelApp = null;
            object oldScreenUpdating = null;
            object oldCalculation = null;
            try
            {
                excelApp = workbook.Application;
                oldScreenUpdating = excelApp.ScreenUpdating;
                oldCalculation = excelApp.Calculation;
                excelApp.ScreenUpdating = false;
                excelApp.Calculation = -4135; // xlCalculationManual
            }
            catch { }

            try
            {
                int dataLen = unwrappedPhases[0].Length;
                int rowCount = Math.Min(dataLen, maxRows);

                // 整块清空测量区/差值区
                phaseSheet.Range[
                    phaseSheet.Cells[measureStartRow, 2],
                    phaseSheet.Cells[measureEndRow, 65]].ClearContents();
                phaseSheet.Range[
                    phaseSheet.Cells[diffStartRow, 2],
                    phaseSheet.Cells[diffEndRow, 65]].ClearContents();

                double[] baseGain = unwrappedPhases[0];

                // 写 64 档：第 2 列（0 态）绝对值，其余列相对基态
                int colCount = Math.Min(unwrappedPhases.Count, 64);
                for (int i = 0; i < colCount; i++)
                {
                    int col = i + 2;
                    var colData = CreateColumnArray(rowCount);
                    double[] gain = unwrappedPhases[i];
                    int n = Math.Min(rowCount, gain.Length);
                    for (int r = 0; r < n; r++)
                    {
                        if (col == 2)
                            colData[r, 0] = gain[r];
                        else
                            colData[r, 0] = gain[r] - baseGain[Math.Min(r, baseGain.Length - 1)];
                    }
                    WriteColumnRange(phaseSheet, measureStartRow, col, colData, rowCount);
                }

                // 减标准值 + 1～63 态 RMS → 测试结果第 11 列（可单独重算）
                CalculateRxAttenuationAccuracyFromExcel(chNum, rowCount, true, phaseSheet, resultSheet, workbook);

                LogToConsole("衰减精度批量写入完成（含 1～63 态 RMS → 测试结果第 11 列）");
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理衰减精度时出错：" + ex.Message);
            }
            finally
            {
                try
                {
                    if (excelApp != null)
                    {
                        if (oldScreenUpdating != null)
                            excelApp.ScreenUpdating = oldScreenUpdating;
                        if (oldCalculation != null)
                            excelApp.Calculation = oldCalculation;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 基于已写入的测量区数据重算衰减精度：减标准值写差值区，对 1～63 态算 RMS，写入测试结果第 11 列。
        /// 不重新采集，可直接用于验证计算。
        /// </summary>
        /// <param name="rowCount">测量行数；≤0 时自动按第 4～16 行有频率的行数检测</param>
        private void CalculateRxAttenuationAccuracyFromExcel(
            int chNum,
            int rowCount = 0,
            bool skipSheetLookup = false,
            dynamic phaseSheet = null,
            dynamic resultSheet = null,
            dynamic workbook = null)
        {
            const int measureStartRow = RxAttenMeasureStartRow;
            const int measureEndRow = RxAttenMeasureEndRow;
            const int diffStartRow = RxAttenDifferenceStartRow;
            const int resultRmsColumn = 16;

            if (!skipSheetLookup)
            {
                workbook = GetExcelWorkbook();
                phaseSheet = FindWorksheet(workbook, $"接收通道衰减精度测试结果{chNum}");
                resultSheet = FindWorksheet(workbook, $"测试结果{chNum}");
                if (phaseSheet == null)
                {
                    MessageBox.Show($"未找到名为“接收通道衰减精度测试结果{chNum}”的工作表。");
                    return;
                }
                if (resultSheet == null)
                {
                    MessageBox.Show($"未找到工作表“测试结果{chNum}”。");
                    return;
                }
            }

            LogToConsole($"开始重算衰减精度（通道{chNum}，1～63 态 RMS）...");

            dynamic excelApp = null;
            object oldScreenUpdating = null;
            object oldCalculation = null;
            bool ownExcelUi = !skipSheetLookup;
            try
            {
                if (ownExcelUi)
                {
                    excelApp = workbook.Application;
                    oldScreenUpdating = excelApp.ScreenUpdating;
                    oldCalculation = excelApp.Calculation;
                    excelApp.ScreenUpdating = false;
                    excelApp.Calculation = -4135;
                }
            }
            catch { }

            try
            {
                int maxRows = measureEndRow - measureStartRow + 1;
                if (rowCount <= 0)
                {
                    object[,] freqProbe = ReadColumnRange(phaseSheet, measureStartRow, measureEndRow, 1);
                    rowCount = 0;
                    for (int r = 0; r < maxRows; r++)
                    {
                        if (TryGetArrayDouble(freqProbe, r, out _))
                            rowCount = r + 1;
                        else
                            break;
                    }
                    if (rowCount <= 0)
                        rowCount = maxRows;
                }
                else
                {
                    rowCount = Math.Min(rowCount, maxRows);
                }

                var rowSqSum = new double[rowCount];
                var rowSqCount = new int[rowCount];

                for (int col = 2; col <= 65; col++)
                {
                    object standardObj = phaseSheet.Cells[3, col].Value2;
                    if (standardObj == null || !double.TryParse(standardObj.ToString(), out double standardValue))
                        continue;

                    object[,] measureCol = ReadColumnRange(phaseSheet, measureStartRow, measureStartRow + rowCount - 1, col);
                    var diffCol = CreateColumnArray(rowCount);
                    for (int r = 0; r < rowCount; r++)
                    {
                        if (!TryGetArrayDouble(measureCol, r, out double measuredValue))
                            continue;
                        double result = measuredValue - standardValue;
                        diffCol[r, 0] = result;

                        // 态 1～63 → 列 3～65
                        if (col >= 3)
                        {
                            rowSqSum[r] += result * result;
                            rowSqCount[r]++;
                        }
                    }
                    WriteColumnRange(phaseSheet, diffStartRow, col, diffCol, rowCount);
                }

                object[,] freqArr = ReadColumnRange(phaseSheet, measureStartRow, measureStartRow + rowCount - 1, 1);
                object[,] resultFreqArr = ReadColumnRange(resultSheet, 8, 200, 1);
                var freqToResultRow = new Dictionary<string, int>();
                if (resultFreqArr != null)
                {
                    int len = resultFreqArr.GetLength(0);
                    for (int i = 0; i < len; i++)
                    {
                        if (!TryGetArrayDouble(resultFreqArr, i, out double f))
                            continue;
                        string key = f.ToString("F3");
                        if (!freqToResultRow.ContainsKey(key))
                            freqToResultRow[key] = 8 + i;
                    }
                }

                int written = 0;
                for (int r = 0; r < rowCount; r++)
                {
                    if (rowSqCount[r] <= 0)
                        continue;
                    if (!TryGetArrayDouble(freqArr, r, out double freqGHz))
                        continue;

                    double rms = Math.Sqrt(rowSqSum[r] / rowSqCount[r]);
                    if (freqToResultRow.TryGetValue(freqGHz.ToString("F3"), out int resultRow))
                    {
                        resultSheet.Cells[resultRow, resultRmsColumn].Value2 = rms.ToString();
                        written++;
                    }
                }

                workbook.Save();
                LogToConsole($"衰减精度重算完成：写入 {written} 个频点 → 测试结果{chNum} 第 11 列");
            }
            catch (Exception ex)
            {
                MessageBox.Show("重算衰减精度失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (ownExcelUi)
                {
                    try
                    {
                        if (excelApp != null)
                        {
                            if (oldScreenUpdating != null)
                                excelApp.ScreenUpdating = oldScreenUpdating;
                            if (oldCalculation != null)
                                excelApp.Calculation = oldCalculation;
                        }
                    }
                    catch { }
                }
            }
        }

        private int GetSelectedChannelNumber()
        {
            if (ch1_checkBox.Checked) return 1;
            if (ch2_checkBox.Checked) return 2;
            if (ch3_checkBox.Checked) return 3;
            if (ch4_checkBox.Checked) return 4;
            if (ch5_checkBox.Checked) return 5;
            if (ch6_checkBox.Checked) return 6;
            if (ch7_checkBox.Checked) return 7;
            if (ch8_checkBox.Checked) return 8;
            return 0;
        }

        private void CalculatePhaseAccuracyAndWriteToExcel(string sheetName, int chNum)
        {
            try
            {
                LogToConsole("开始计算精度...");
                dynamic workbook = GetExcelWorkbook();
                dynamic phaseSheet = FindWorksheet(workbook, sheetName);
                dynamic resultSheet = FindWorksheet(workbook, $"测试结果{chNum}");
                if (phaseSheet == null || resultSheet == null)
                {
                    MessageBox.Show($"未找到工作表“{sheetName}”或“测试结果{chNum}”。");
                    return;
                }

                // 差值区起始行：发射移相 131，接收衰减 23
                int startRow = sheetName.StartsWith("接收通道衰减精度测试结果")
                    ? RxAttenDifferenceStartRow
                    : TxPhaseDifferenceStartRow;
                int currentRow = startRow;
                // 发射移相：仅基态列；接收衰减等：列 3～65（态 1～63）
                int[] rmsColumns = sheetName.Equals("发射通道相移精度测试结果")
                    ? GetTxPhaseRmsColumns()
                    : Enumerable.Range(3, 63).ToArray();

                while (true)
                {
                    dynamic freqCell = phaseSheet.Cells[currentRow, 1]; // A列
                    object freqVal = freqCell?.Value2;
                    if (freqVal == null)
                        break;

                    string freqStr = freqVal.ToString();
                    if (!double.TryParse(freqStr, out double freqGHz))
                        break;

                    List<double> phaseValues = new List<double>();
                    foreach (int col in rmsColumns)
                    {
                        object cellVal = phaseSheet.Cells[currentRow, col].Value2;
                        if (cellVal != null && double.TryParse(cellVal.ToString(), out double val))
                        {
                            phaseValues.Add(val);
                        }
                    }

                    // 计算均方根（RMS）误差
                    double rms = phaseValues.Count > 0 ? Math.Sqrt(phaseValues.Average(v => v * v)) : 0;

                    // 在“测试结果”中查找对应频率行并写入 RMS 到 I 列（第9列）
                    int resultRow = FindRowByFrequency(resultSheet, freqGHz);
                    if (resultRow > 0)
                    {
                        if (sheetName.Equals($"接收通道相移精度测试结果{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 7].Value2 = rms.ToString();
                        }
                        if (sheetName.Equals($"接收通道衰减精度测试结果{chNum}"))
                        {
                            resultSheet.Cells[resultRow, 11].Value2 = rms.ToString();
                        }
                        if (sheetName.Equals($"发射通道相移精度测试结果"))
                        {
                            resultSheet.Cells[resultRow, 11].Value2 = rms.ToString();
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
        private int FindRowByFrequency(dynamic sheet, double freqGHz)
        {
            int row = 8; // 从第8行开始查找
            while (true)
            {
                object cellVal = sheet.Cells[row, 1].Value2; // A列
                if (cellVal == null)
                    break;

                if (double.TryParse(cellVal.ToString(), out double f) &&
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
        private double I_T28 = 0;
        private double I_T5 = 0;
        private double I_DQ5 = 0;
        private double I_R5 = 0;
        private async Task CloseCharge()
        {
            try
            {
                string chargeAddr = _chargeAddress;

                ScpiDevice charge = new ScpiDevice();

                bool connected = await charge.ConnectAsync(chargeAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await charge.SelectChannel(1);
                await charge.DisableOutput();

                await charge.SelectChannel(2);
                await charge.DisableOutput();

                LogToConsole("关电");

                charge.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("关电失败", ex.ToString(), operator_textBox.Text);
            }
        }
        private async Task ChargeRecievePowerON()
        {
            try
            {
                string chargeAddr = _chargeAddress;

                ScpiDevice charge = new ScpiDevice();

                bool connected = await charge.ConnectAsync(chargeAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await charge.SelectChannel(2);
                await charge.SetVoltage(5.5);
                await charge.SetCurrent(2);
                await charge.EnableOutput();

                LogToConsole("接收加电");
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
                string chargeAddr = _chargeAddress;

                ScpiDevice charge = new ScpiDevice();

                bool connected = await charge.ConnectAsync(chargeAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await charge.SelectChannel(1);
                await charge.SetVoltage(28);
                await charge.SetCurrent(2);
                await charge.EnableOutput();

                await charge.SelectChannel(2);
                await charge.SetVoltage(5.5);
                await charge.SetCurrent(2);
                await charge.EnableOutput();
                LogToConsole("发射加电");
                charge.Disconnect();
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
                string visaAddress = _chargeAddress;

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
                string chargeAddr = _chargeAddress;

                ScpiDevice charge = new ScpiDevice();

                bool connected = await charge.ConnectAsync(chargeAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await charge.SelectChannel(2);
                await charge.SetVoltage(5.5);
                await charge.SetCurrent(2);
                await charge.EnableOutput();

                LogToConsole("接收加电");
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
                string chargeAddr = _chargeAddress;

                ScpiDevice charge = new ScpiDevice();

                bool connected = await charge.ConnectAsync(chargeAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await charge.SelectChannel(1);
                await charge.SetVoltage(28);
                await charge.SetCurrent(2);
                await charge.EnableOutput();

                await charge.SelectChannel(2);
                await charge.SetVoltage(5.5);
                await charge.SetCurrent(2);
                await charge.EnableOutput();
                LogToConsole("发射加电");
                charge.Disconnect();
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
                string chargeAddr = _chargeAddress;

                ScpiDevice charge = new ScpiDevice();

                bool connected = await charge.ConnectAsync(chargeAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await charge.SelectChannel(1);
                await charge.DisableOutput();

                await charge.SelectChannel(2);
                await charge.DisableOutput();

                LogToConsole("关电");

                charge.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("关电失败", ex.ToString(), operator_textBox.Text);
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
        const int ApplicationHeaderLength = 40;
        const int ModelValueLength = 4;
        const int ReservedValueLength = 8;
        const int CodeValueLength = 40;
        const int ControlCodeByteLength = 28;
        const int TableHeaderBitCount = 6;           // 表头[5:0]
        const int IfAttenuationBitCount = 10;        // ATT[9:0]
        const int ChannelFieldBitCount = 6;
        const int ChannelControlBitCount = 26;       // RxEn + 4×6bit + TxEn
        const int ChannelCount = 8;
        const int ControlBitCount = TableHeaderBitCount + IfAttenuationBitCount + ChannelControlBitCount * ChannelCount;
        string DefaultTableHeaderBits = "010101"; // 表头[5:0] 默认值
        const int ControlCodeStartIndex = 13;

        public void SendCustomPacket(byte[] headValue, byte[] modelValue, byte[] emptyValue, byte[] codeValue)
        {
            try
            {
                if (string.IsNullOrEmpty(srcIpStr) || string.IsNullOrEmpty(dstIpStr) || string.IsNullOrEmpty(srcMacAddress) || string.IsNullOrEmpty(dstMacStr))
                {
                    MessageBox.Show("IP或MAC地址为空，请设置后再操作。");
                    return;
                }

                // 参数名与字段同名会遮蔽字段；EnsureApplicationHeader 更新的是字段，发送必须用字段。
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
                MessageBox.Show("UDP发送失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                throw new ArgumentException($"codeValue 应为 {CodeValueLength} 字节。" + codeValue.Length);
        }

        private bool[] GetChannelCheckedStates()
        {
            return new[]
            {
                !ch1_checkBox.Checked,
                !ch2_checkBox.Checked,
                !ch3_checkBox.Checked,
                !ch4_checkBox.Checked,
                !ch5_checkBox.Checked,
                !ch6_checkBox.Checked,
                !ch7_checkBox.Checked,
                !ch8_checkBox.Checked
            };
        }

        static string PadIfAttenuationBits(string bits)
        {
            string normalized = (bits ?? string.Empty).Replace(" ", "");
            if (normalized.Length > IfAttenuationBitCount)
                throw new ArgumentException($"中频衰减应为 {IfAttenuationBitCount} 位，但提供了 {normalized.Length} 位");

            return normalized.PadRight(IfAttenuationBitCount, '0');
        }

        static byte[] GenerateCodeValueFromBits(string[] bitStrings)
        {
            /*            if (bitStrings.Length != 10)
                            throw new ArgumentException("应包含10个通道的比特串");*/

            int[] expectedLengths = { 56, 28, 28, 28, 28, 28, 28, 28, 9, 59 };
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

        private async Task RecieveTestUDP()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1recive = ch1_checkBox.Checked ? "1" : "0";
                    string ch2recive = ch2_checkBox.Checked ? "1" : "0";
                    string ch3recive = ch3_checkBox.Checked ? "1" : "0";
                    string ch4recive = ch4_checkBox.Checked ? "1" : "0";
                    string ch5recive = ch5_checkBox.Checked ? "1" : "0";
                    string ch6recive = ch6_checkBox.Checked ? "1" : "0";
                    string ch7recive = ch7_checkBox.Checked ? "1" : "0";
                    string ch8recive = ch8_checkBox.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + ch1recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + ch2recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + ch3recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + ch4recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + ch5recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + ch6recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + ch7recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + ch8recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string model = "10";
                    string model_stc = model + "000000" + "0";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });
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
                    if (ch5_checkBox.Checked)
                    {
                        chSum += "通道5 ";
                    }
                    if (ch6_checkBox.Checked)
                    {
                        chSum += " 通道6 ";
                    }
                    if (ch7_checkBox.Checked)
                    {
                        chSum += " 通道7 ";
                    }
                    if (ch8_checkBox.Checked)
                    {
                        chSum += " 通道8 ";
                    }

                    LogToConsole("FPGA发包:" + chSum);

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);

                    operateLog_DAL.InsertOperateLog_DT("接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}", operator_textBox.Text);
                }
                catch (Exception ex)
                {
                    LogToConsole("接收测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        private async Task RecieveTestUDPFullAtt2()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1recive = ch1_checkBox.Checked ? "1" : "0";
                    string ch2recive = ch2_checkBox.Checked ? "1" : "0";
                    string ch3recive = ch3_checkBox.Checked ? "1" : "0";
                    string ch4recive = ch4_checkBox.Checked ? "1" : "0";
                    string ch5recive = ch5_checkBox.Checked ? "1" : "0";
                    string ch6recive = ch6_checkBox.Checked ? "1" : "0";
                    string ch7recive = ch7_checkBox.Checked ? "1" : "0";
                    string ch8recive = ch8_checkBox.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + ch1recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + ch2recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + ch3recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + ch4recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + ch5recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + ch6recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + ch7recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + ch8recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string model = "10";
                    string model_stc = model + "000000" + "1";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });
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
                    if (ch5_checkBox.Checked)
                    {
                        chSum += "通道5 ";
                    }
                    if (ch6_checkBox.Checked)
                    {
                        chSum += " 通道6 ";
                    }
                    if (ch7_checkBox.Checked)
                    {
                        chSum += " 通道7 ";
                    }
                    if (ch8_checkBox.Checked)
                    {
                        chSum += " 通道8 ";
                    }

                    LogToConsole("FPGA发包:" + chSum);

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);

                    operateLog_DAL.InsertOperateLog_DT("接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}", operator_textBox.Text);
                }
                catch (Exception ex)
                {
                    LogToConsole("接收测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }
        private async Task RecieveTestUDPFullAtt3()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1recive = ch1_checkBox.Checked ? "1" : "0";
                    string ch2recive = ch2_checkBox.Checked ? "1" : "0";
                    string ch3recive = ch3_checkBox.Checked ? "1" : "0";
                    string ch4recive = ch4_checkBox.Checked ? "1" : "0";
                    string ch5recive = ch5_checkBox.Checked ? "1" : "0";
                    string ch6recive = ch6_checkBox.Checked ? "1" : "0";
                    string ch7recive = ch7_checkBox.Checked ? "1" : "0";
                    string ch8recive = ch8_checkBox.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + ch1recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + ch2recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + ch3recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + ch4recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + ch5recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + ch6recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + ch7recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + ch8recive + "000000" + "000000" + "000000" + "000000" + "0";
                    string model = "10";
                    string model_stc = model + "111111" + "1";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });
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
                    if (ch5_checkBox.Checked)
                    {
                        chSum += "通道5 ";
                    }
                    if (ch6_checkBox.Checked)
                    {
                        chSum += " 通道6 ";
                    }
                    if (ch7_checkBox.Checked)
                    {
                        chSum += " 通道7 ";
                    }
                    if (ch8_checkBox.Checked)
                    {
                        chSum += " 通道8 ";
                    }

                    LogToConsole("FPGA发包:" + chSum);

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);

                    operateLog_DAL.InsertOperateLog_DT("接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}", operator_textBox.Text);
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
                    string ch5send = ch5_checkBox.Checked ? "1" : "0";
                    string ch6send = ch6_checkBox.Checked ? "1" : "0";
                    string ch7send = ch7_checkBox.Checked ? "1" : "0";
                    string ch8send = ch8_checkBox.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch1send;
                    string ch2 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch2send;
                    string ch3 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch3send;
                    string ch4 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch4send;
                    string ch5 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch5send;
                    string ch6 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch6send;
                    string ch7 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch7send;
                    string ch8 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + ch8send;
                    string model = "00";
                    string model_stc = model + "000000" + "0";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });
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
                    if (ch5_checkBox.Checked)
                    {
                        chSum += "通道5 ";
                    }
                    if (ch6_checkBox.Checked)
                    {
                        chSum += " 通道6 ";
                    }
                    if (ch7_checkBox.Checked)
                    {
                        chSum += " 通道7 ";
                    }
                    if (ch8_checkBox.Checked)
                    {
                        chSum += " 通道8 ";
                    }
                    if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
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
                    if (ch5_checkBox.Checked) selectedCount++;
                    if (ch6_checkBox.Checked) selectedCount++;
                    if (ch7_checkBox.Checked) selectedCount++;
                    if (ch8_checkBox.Checked) selectedCount++;

                    // 判断是否仅选择一个
                    if (selectedCount != 1)
                    {
                        MessageBox.Show("请只选择一个接收通道！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return; // 终止方法
                    }
                    string numToString = ToSixBitBinaryString3(num);
                    // 分别设置通道值
                    string ch1send = ch1_checkBox.Checked ? "1" : "0";
                    string ch2send = ch2_checkBox.Checked ? "1" : "0";
                    string ch3send = ch3_checkBox.Checked ? "1" : "0";
                    string ch4send = ch4_checkBox.Checked ? "1" : "0";
                    string ch5send = ch5_checkBox.Checked ? "1" : "0";
                    string ch6send = ch6_checkBox.Checked ? "1" : "0";
                    string ch7send = ch7_checkBox.Checked ? "1" : "0";
                    string ch8send = ch8_checkBox.Checked ? "1" : "0";
                    string ch1 = new string('0', 28) + "00" + "0" + numToString + "000000" + "000000" + "000000" + ch1send;
                    string ch2 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch2send;
                    string ch3 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch3send;
                    string ch4 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch4send;
                    string ch5 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch5send;
                    string ch6 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch6send;
                    string ch7 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch7send;
                    string ch8 = "00" + "0" + numToString + "000000" + "000000" + "000000" + ch8send;
                    string model = "00";
                    string model_stc = model + "000000" + "0";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });
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
        private async Task CloseFPGA()
        {
            await Task.Run(() =>
            {
                try
                {
                    LogToConsole("切换至负载态");
                    string ch1 = new string('0', 28) + "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch2 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch3 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch4 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch5 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch6 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch7 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string ch8 = "00" + "0" + "000000" + "000000" + "000000" + "000000" + "0";
                    string model_stc = "01" + "000000" + "0";
                    string buling = new string('0', 59);
                    modelValue = StringToByteArray("01 03 03 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8, model_stc, buling });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                }
                catch (Exception ex)
                {
                    LogToConsole("切换至负载态失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("切换至负载态失败", ex.ToString(), operator_textBox.Text);
                }
            });
        }

        private string BuildSelectedChannelSummary()
        {
            string chSum = "";
            if (ch1_checkBox.Checked) chSum += "通道1 ";
            if (ch2_checkBox.Checked) chSum += " 通道2 ";
            if (ch3_checkBox.Checked) chSum += " 通道3 ";
            if (ch4_checkBox.Checked) chSum += " 通道4 ";
            if (ch5_checkBox.Checked) chSum += "通道5 ";
            if (ch6_checkBox.Checked) chSum += " 通道6 ";
            if (ch7_checkBox.Checked) chSum += " 通道7 ";
            if (ch8_checkBox.Checked) chSum += " 通道8 ";
            return chSum;
        }
        #endregion

        #region 温度回传
        private TemperatureReturnReceiver _tempReceiver;
        private System.Windows.Forms.Label _temp2ValueLabel;
        private System.Windows.Forms.Label _temp3ValueLabel;
        private System.Windows.Forms.Label _temp4ValueLabel;

        /// <summary>
        /// 在 tableLayoutPanel1 左下格（第 0 列、第 2 行）放置温度回传显示区，
        /// 实时显示温度2/3/4（℃）。
        /// </summary>
        private void InitTemperaturePanel()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Margin = new Padding(2),
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            var title = new System.Windows.Forms.Label
            {
                Text = "温度回传",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new System.Drawing.Font("微软雅黑", 9F, FontStyle.Bold),
                AutoSize = false,
                Margin = new Padding(0)
            };
            _temp2ValueLabel = CreateTemperatureLabel();
            _temp3ValueLabel = CreateTemperatureLabel();
            _temp4ValueLabel = CreateTemperatureLabel();
            ResetTemperatureLabels();

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(_temp2ValueLabel, 0, 1);
            layout.Controls.Add(_temp3ValueLabel, 0, 2);
            layout.Controls.Add(_temp4ValueLabel, 0, 3);

            tableLayoutPanel1.Controls.Add(layout, 0, 2);
        }

        private static System.Windows.Forms.Label CreateTemperatureLabel()
        {
            return new System.Windows.Forms.Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new System.Drawing.Font("微软雅黑", 9F),
                AutoSize = false,
                Margin = new Padding(4, 0, 0, 0)
            };
        }

        private void ResetTemperatureLabels()
        {
            _temp2ValueLabel.Text = "温度2: --- ℃";
            _temp3ValueLabel.Text = "温度3: --- ℃";
            _temp4ValueLabel.Text = "温度4: --- ℃";
        }

        /// <summary>开机自动后台监听温度回传（下位机经 UDP 持续回传）。</summary>
        private void StartTemperatureReceiver()
        {
            try
            {
                _tempReceiver = new TemperatureReturnReceiver();
                _tempReceiver.Log += LogToConsole;
                _tempReceiver.FrameReceived += OnTemperatureFrameReceived;
                _tempReceiver.Start(ifaceName, dstIpStr, dstPort);
            }
            catch (Exception ex)
            {
                LogToConsole("温度回传监听启动失败：" + ex.Message);
            }
        }

        private void OnTemperatureFrameReceived(DbfTest.MODEL.TemperatureReturnFrame frame)
        {
            if (_temp2ValueLabel == null || _temp2ValueLabel.IsDisposed || !_temp2ValueLabel.IsHandleCreated)
                return;

            void Apply()
            {
                _temp2ValueLabel.Text = $"温度2: {frame.Temperature2C:F1} ℃";
                _temp3ValueLabel.Text = $"温度3: {frame.Temperature3C:F1} ℃";
                _temp4ValueLabel.Text = $"温度4: {frame.Temperature4C:F1} ℃";
            }

            try
            {
                if (_temp2ValueLabel.InvokeRequired)
                    _temp2ValueLabel.BeginInvoke((System.Action)Apply);
                else
                    Apply();
            }
            catch (ObjectDisposedException)
            {
                // 窗体正在关闭，忽略
            }
            catch (InvalidOperationException)
            {
                // 句柄已销毁，忽略
            }
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
                string visaAddress = _gonglvAddress;

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
                    string visaAddress = _vnaAddress;

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
                    string visaAddress = _vnaAddress;

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
        /// 开关本振输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mod_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (mod_checkBox.Checked)
            {
                try
                {
                    string visaAddress = _xinhaoAddress;

                    ScpiDevice scpiDevice = new ScpiDevice();

                    bool connected = await scpiDevice.ConnectAsync(visaAddress);
                    if (!connected)
                    {
                        LogToConsole("连接失败");
                        return;
                    }
                    await scpiDevice.EnableRfOutput();
                    LogToConsole("打开本振输出");

                    scpiDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开本振输出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    operateLog_DAL.InsertOperateLog_DT("打开本振输出失败", ex.ToString(), operator_textBox.Text);
                }
            }
            else
            {
                try
                {
                    string visaAddress = _xinhaoAddress;

                    ScpiDevice scpiDevice = new ScpiDevice();

                    bool connected = await scpiDevice.ConnectAsync(visaAddress);
                    if (!connected)
                    {
                        LogToConsole("连接失败");
                        return;
                    }
                    await scpiDevice.DisableRfOutput();
                    LogToConsole("关闭本振输出");

                    scpiDevice.Disconnect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"关闭本振输出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    operateLog_DAL.InsertOperateLog_DT("关闭本振输出失败", ex.ToString(), operator_textBox.Text);
                }
            }
        }
        #endregion

        #region 工具栏按钮
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                dynamic workbook = GetExcelWorkbook();
                if (workbook == null)
                {
                    MessageBox.Show("Excel 尚未初始化（InitializeDSO 未打开工作簿）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string testComponent = componentName_textBox.Text ?? "component";
                string testType = testType_comboBox.Text ?? "";
                string timestamp = DateTime.Now.ToString("yyyy.MM.dd.HHmmss");

                // 先尝试用你现有的 _excelPath（假设这是类级字段），否则尝试 workbook.Path（已保存工作簿的目录），否则使用用户文档目录
                string excelPathTemp = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(_excelPath))
                    {
                        excelPathTemp = Path.GetDirectoryName(_excelPath);
                    }
                }
                catch { /* ignore */ }

                if (string.IsNullOrWhiteSpace(excelPathTemp))
                {
                    // workbook.Path 在工作簿未保存时通常为空字符串
                    if (!string.IsNullOrWhiteSpace((string)workbook.Path))
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
                }
                catch
                {
                    // 忽略，使用默认扩展名
                }

                // 构造安全的文件名（移除文件名中非法字符）
                string rawFileName = $"{testComponent}_高低温{testType}_{timestamp}";
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
            dynamic workbook = GetExcelWorkbook();
            if (workbook == null)
            {
                MessageBox.Show("Excel 尚未初始化（InitializeDSO 未打开工作簿）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
                // ---------- 1️⃣ 清空测试结果sheet ----------
                for (int i = 2; i <= 9; i++)
                {
                    dynamic sheet = workbook.Worksheets[i];
                    dynamic range = sheet.Range["B8", "Q" + sheet.Rows.Count];
                    range.ClearContents();
                }
                // ---------- 2️⃣ 发射移相sheet ----------
                dynamic sheet3 = workbook.Worksheets[10];
                dynamic range3a = sheet3.Range["B4", "BM124"];
                dynamic range3b = sheet3.Range["B131", "BM251"];
                range3a.Value2 = 0;
                range3b.Value2 = 0;
                // ---------- 3️⃣ 接收衰减sheet ----------
                for (int i = 11; i <= 18; i++)
                {
                    dynamic sheet = workbook.Worksheets[i];
                    dynamic rangea = sheet.Range["B4", "BM16"];
                    dynamic rangeb = sheet.Range["B23", "BM35"];
                    rangea.Value2 = 0;
                    rangeb.Value2 = 0;
                }
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

                bool deviceConnected = await vnaDevice.ConnectAsync(_vnaAddress);

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

                bool deviceConnected = await vnaDevice.ConnectAsync(_vnaAddress);

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
                if (string.IsNullOrEmpty(_shiwangChaSunPath) || !File.Exists(_shiwangChaSunPath))
                {
                    MessageBox.Show("找不到指定的 Excel 文件路径：" + _shiwangChaSunPath, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建 Excel 应用程序实例
                var excelApp = new Excel.Application();
                excelApp.Visible = true; // 显示 Excel 窗口

                // 打开指定工作簿
                excelApp.Workbooks.Open(_shiwangChaSunPath);
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

                bool deviceConnected = await scpiDevice.ConnectAsync(_pinpuAddress);

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

        private void 配置刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetAddress();
            GetDeviceFilesJson();
            GetTestSetNewJson();
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

        #region XDBF20W

        private async void LoadPinpuState()
        {
            try
            {

                ScpiDevice xinhaoDevice = new ScpiDevice();

                bool connected = await xinhaoDevice.ConnectAsync(_vnaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await xinhaoDevice.SetPower(GetRfPowerDb(), _vnaRfPortNum);
                await xinhaoDevice.EnableOutput();

                LogToConsole("配置完成");
                xinhaoDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载频谱状态失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("加载频谱状态失败", ex.ToString(), operator_textBox.Text);
            }
        }
        private async void LoadVNAState()
        {
            try
            {
                string vnaAddr = _vnaAddress;

                ScpiDevice vna = new ScpiDevice();

                bool connected = await vna.ConnectAsync(vnaAddr);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                await vna.LoadStateFile("50.csa");
                LogToConsole("加载矢网状态");
                vna.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载矢网状态失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("加载矢网状态失败", ex.ToString(), operator_textBox.Text);
            }
        }
        #endregion

        private async void toolStripButton3_Click(object sender, EventArgs e)
        {
            LoadPinpuState();
        }

        private async void toolStripButton4_Click(object sender, EventArgs e)
        {
            CloseFPGA();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                if (_workbook == null)
                {
                    MessageBox.Show("Excel 尚未初始化（InitializeDSO 未打开工作簿）。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dynamic workbook = _workbook;
                dynamic recordSheet = workbook.Sheets["测试记录表"];

                // 源行：8/10/13/15/18/20 → 对应 9.0/9.2/9.5/9.7/10.0/10.2，目标列：F~K（第6~11列）
                int[] sourceRows = { 8, 10, 13, 15, 18, 20 };
                // 源列 L/N/O/P/Q/K，对应测试记录表通道1起始行（通道n = 起始行 + n - 1）
                int[] sourceCols = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17 };
                int[] targetBaseRows = { 74, 66, 90, 204, 180, 99, 188, 196, 265, 223, 13, 29, 37, 45, 53 };

                for (int ch = 1; ch <= 8; ch++)
                {
                    dynamic resultSheet = workbook.Sheets[$"测试结果{ch}"];
                    for (int m = 0; m < sourceCols.Length; m++)
                    {
                        int targetRow = targetBaseRows[m] + (ch - 1);
                        for (int i = 0; i < sourceRows.Length; i++)
                        {
                            object value = resultSheet.Cells[sourceRows[i], sourceCols[m]].Value2;
                            recordSheet.Cells[targetRow, 6 + i].Value2 = value;
                        }
                    }
                }
                #region 通道间同频点幅度一致性
                // B列各频点行：测试结果1~8 的最大值减最小值 → 测试记录表第61行 F~K
                for (int i = 0; i < sourceRows.Length; i++)
                {
                    double maxVal = double.MinValue;
                    double minVal = double.MaxValue;
                    bool hasValue = false;

                    for (int ch = 1; ch <= 8; ch++)
                    {
                        dynamic resultSheet = workbook.Sheets[$"测试结果{ch}"];
                        object cellVal = resultSheet.Cells[sourceRows[i], 2].Value2;
                        if (cellVal == null)
                            continue;

                        if (double.TryParse(cellVal.ToString(), out double num))
                        {
                            if (num > maxVal) maxVal = num;
                            if (num < minVal) minVal = num;
                            hasValue = true;
                        }
                    }

                    recordSheet.Cells[61, 6 + i].Value2 = hasValue ? (object)(maxVal - minVal) : null;
                }
                #endregion
                workbook.Save();
                MessageBox.Show("测试记录表更新成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新测试记录表失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripButton5_Click(object sender, EventArgs e)
        {
            try
            {
                ScpiDevice scpiDevice = new ScpiDevice();
                bool connected = await scpiDevice.ConnectAsync(_pinpuAddress);
                if (!connected)
                {
                    LogToConsole("设备连接失败: " + _pinpuAddress);
                    MessageBox.Show("设备连接失败，请检查地址。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                await scpiDevice.LoadPinpuStateAsync("/usrdata/Data/xdbf20w.state");
                await Task.Delay(500); // 延时保证设备稳定
                await scpiDevice.SetStartFrequencyAsync(RfBandStartGHz * 1e9 - 500 * 1e6);
                await scpiDevice.SetStopFrequencyAsync(RfBandStartGHz * 1e9 + 500 * 1e6);
                await scpiDevice.SetCenterFrequencyAsync(RfBandStartGHz * 1e9);
                //LogToConsole(result);
                scpiDevice.Disconnect();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(operator_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double rfPower;
            try
            {
                rfPower = GetRfPowerDb();
                GetBenzhenPowerDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LogToConsole("开始接收驻波测试");
            WritePersonToAllSheets();
            await ChargeRecievePowerON();
            await Task.Delay(500);
            await RecieveTestUDP();
            await Task.Delay(500);

            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();

            try
            {
                string testType = testType_comboBox.Text;
                int chNum = 0;
                if (ch1_checkBox.Checked) chNum = 1;
                if (ch2_checkBox.Checked) chNum = 2;
                if (ch3_checkBox.Checked) chNum = 3;
                if (ch4_checkBox.Checked) chNum = 4;
                if (ch5_checkBox.Checked) chNum = 5;
                if (ch6_checkBox.Checked) chNum = 6;
                if (ch7_checkBox.Checked) chNum = 7;
                if (ch8_checkBox.Checked) chNum = 8;
                string sheetName = $"测试结果{chNum}";

                bool connected = await xinhaoDevice.ConnectAsync(_vnaAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                if (!connected || !connected2)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                const double centerIfHz = 175e6;

                await xinhaoDevice.LoadStateFile("502.csa");
                await xinhaoDevice.SetPower(rfPower, _vnaRfPortNum);
                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                await xinhaoBenzhenDevice.EnableRfOutput();

                string[] freqArray = new string[_pointCount];
                string[] inputVswr = new string[_pointCount];
                string[] outputVswr = new string[_pointCount];
                string[] swrRate = new string[_pointCount];

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                int num = 0;
                SafeSetProgressBarMaximum(_pointCount, 0);

                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString();

                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                    await Task.Delay(200);
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                    await Task.Delay(200);

                    await xinhaoDevice.ScanOnce(1);
                    await Task.Delay(100);
                    inputVswr[i] = await xinhaoDevice.GetInputVSWRStringAsync();     // 输入驻波比

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                WriteArrayToExcelColumn(freqArray, 1, sheetName);
                WriteArrayToExcelColumn(inputVswr, 15, sheetName);
                LogToConsole("接收驻波测试完成，已写入Excel");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"接收驻波测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("接收驻波测试失败", ex.ToString(), operator_textBox.Text);
                LogToConsole("接收驻波测试出错: " + ex.Message);
            }
            finally
            {
                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge();
                LogToConsole("接收驻波测试已结束");
            }
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void button2_Click_1(object sender, EventArgs e)
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(operator_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double rfPower;
            try
            {
                rfPower = GetRfPowerDb();
                GetBenzhenPowerDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LogToConsole("开始接收平坦度测试");
            WritePersonToAllSheets();
            await ChargeRecievePowerON();
            await Task.Delay(500);
            await RecieveTestUDP();
            await Task.Delay(500);

            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();

            try
            {
                string testType = testType_comboBox.Text;
                int chNum = 0;
                if (ch1_checkBox.Checked) chNum = 1;
                if (ch2_checkBox.Checked) chNum = 2;
                if (ch3_checkBox.Checked) chNum = 3;
                if (ch4_checkBox.Checked) chNum = 4;
                if (ch5_checkBox.Checked) chNum = 5;
                if (ch6_checkBox.Checked) chNum = 6;
                if (ch7_checkBox.Checked) chNum = 7;
                if (ch8_checkBox.Checked) chNum = 8;
                string sheetName = $"测试结果{chNum}";

                bool connected = await xinhaoDevice.ConnectAsync(_vnaAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(_pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                const double centerIfHz = 175e6;
                const double markerStepHz = 0.1e6;
                const double flatSpanHalfHz = 10e6;
                double flatMinHz = centerIfHz - flatSpanHalfHz; // 165 MHz
                double flatMaxHz = centerIfHz + flatSpanHalfHz; // 185 MHz

                await pinpuDevice.SendCommandAsync(":INST:SEL SA");
                await pinpuDevice.LoadPinpuStateAsync("/usrdata/Data/xdbf20w.state");
                await pinpuDevice.SetCenterFrequencyAsync(centerIfHz);
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");

                await xinhaoDevice.LoadStateFile("50.csa");
                await xinhaoDevice.SetPower(rfPower, _vnaRfPortNum);
                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                await xinhaoBenzhenDevice.EnableRfOutput();

                string[] freqArray = new string[_pointCount];
                string[] flatness = new string[_pointCount];

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                int num = 0;
                SafeSetProgressBarMaximum(_pointCount, 0);

                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString();

                    // RF 固定；本振先置中心：LO = RF - 175 MHz
                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                    await Task.Delay(200);
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                    await Task.Delay(200);

                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                    await Task.Delay(100);
                    double centerPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    if (double.IsNaN(centerPower))
                    {
                        flatness[i] = "";
                        LogToConsole($"{freqGHz} GHz: 中心功率无效，跳过");
                    }
                    else
                    {
                        // Marker 与本振同步 0.1 MHz 步进扫描 ±10 MHz
                        double flatDiff = await MeasureFlatnessWithLoStepAsync(
                            pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, centerPower,
                            markerStepHz, flatMinHz, flatMaxHz);
                        flatness[i] = flatDiff.ToString("F2");
                        LogToConsole($"{freqGHz} GHz: 平坦度={flatness[i]} dB (P0={centerPower:F2} dBm)");
                    }

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label6.Refresh();
                }
                WriteArrayToExcelColumn(freqArray, 1, sheetName);
                WriteArrayToExcelColumn(flatness, 9, sheetName);
                LogToConsole("平坦度测试完成，已写入Excel");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"平坦度测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("平坦度测试失败", ex.ToString(), operator_textBox.Text);
                LogToConsole("平坦度测试出错: " + ex.Message);
            }
            finally
            {
                await xinhaoBenzhenDevice.DisableRfOutput();
                await xinhaoDevice.DisableOutput();
                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge();
                LogToConsole("平坦度测试已结束");
            }
        }

        /// <summary>
        /// -1dB带宽：Marker 与本振同步 0.1 MHz 步进，找两侧首次 ≤ P0-1dB 的频点。
        /// </summary>
        private async void button3_Click_1(object sender, EventArgs e)
        {
            if (testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked && !ch5_checkBox.Checked && !ch6_checkBox.Checked && !ch7_checkBox.Checked && !ch8_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(operator_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double rfPower;
            try
            {
                rfPower = GetRfPowerDb();
                GetBenzhenPowerDb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LogToConsole("开始-1dB带宽测试");
            WritePersonToAllSheets();
            await ChargeRecievePowerON();
            await Task.Delay(500);
            await RecieveTestUDP();
            await Task.Delay(500);

            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();

            try
            {
                int chNum = 0;
                if (ch1_checkBox.Checked) chNum = 1;
                if (ch2_checkBox.Checked) chNum = 2;
                if (ch3_checkBox.Checked) chNum = 3;
                if (ch4_checkBox.Checked) chNum = 4;
                if (ch5_checkBox.Checked) chNum = 5;
                if (ch6_checkBox.Checked) chNum = 6;
                if (ch7_checkBox.Checked) chNum = 7;
                if (ch8_checkBox.Checked) chNum = 8;
                string sheetName = $"测试结果{chNum}";

                bool connected = await xinhaoDevice.ConnectAsync(_vnaAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(_xinhaoAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(_pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                const double centerIfHz = 175e6;
                const double markerStepHz = 0.1e6;
                double saMinHz = centerIfHz - 100e6; // 75 MHz
                double saMaxHz = centerIfHz + 100e6; // 275 MHz

                await pinpuDevice.SendCommandAsync(":INST:SEL SA");
                await pinpuDevice.LoadPinpuStateAsync("/usrdata/Data/xdbf20w.state");
                await pinpuDevice.SetStartFrequencyAsync(saMinHz);
                await pinpuDevice.SetStopFrequencyAsync(saMaxHz);
                await pinpuDevice.SetCenterFrequencyAsync(centerIfHz);
                await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");

                await xinhaoDevice.LoadStateFile("50.csa");
                await xinhaoDevice.SetPower(rfPower, _vnaRfPortNum);
                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.SetAmplitude(GetBenzhenPowerDb());
                await xinhaoBenzhenDevice.EnableRfOutput();

                string[] freqArray = new string[_pointCount];
                string[] bandwidthMHz = new string[_pointCount];

                double step = (_pointCount > 1) ? (_stopFreq - _startFreq) / (_pointCount - 1) : 0;
                int num = 0;
                SafeSetProgressBarMaximum(_pointCount, 0);

                for (int i = 0; i < _pointCount; i++)
                {
                    double freqHz = _startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString();

                    await xinhaoDevice.SetCenterFrequencyAsync(freqHz);
                    await Task.Delay(200);
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                    await Task.Delay(200);

                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                    await Task.Delay(100);
                    double centerPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    if (double.IsNaN(centerPower))
                    {
                        bandwidthMHz[i] = "";
                        LogToConsole($"{freqGHz} GHz: 中心功率无效，跳过");
                    }
                    else
                    {
                        double targetPower = centerPower - 1.0;

                        double? leftFreq = await FindMinus1dBMarkerFrequencyWithLoStepAsync(
                            pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, targetPower,
                            markerStepHz, -1, saMinHz, saMaxHz);

                        // 一侧扫完后回到中心，再扫另一侧
                        await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                        await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");
                        await Task.Delay(50);

                        double? rightFreq = await FindMinus1dBMarkerFrequencyWithLoStepAsync(
                            pinpuDevice, xinhaoBenzhenDevice, freqHz, centerIfHz, targetPower,
                            markerStepHz, 1, saMinHz, saMaxHz);

                        await xinhaoBenzhenDevice.SetFrequency(freqHz - centerIfHz);
                        await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {centerIfHz}");

                        if (leftFreq.HasValue && rightFreq.HasValue)
                        {
                            bandwidthMHz[i] = ((rightFreq.Value - leftFreq.Value) / 1e6).ToString("F2");
                            LogToConsole($"{freqGHz} GHz: -1dB带宽={bandwidthMHz[i]} MHz " +
                                         $"(fL={leftFreq.Value / 1e6:F1}, fH={rightFreq.Value / 1e6:F1}, P0={centerPower:F2})");
                        }
                        else
                        {
                            bandwidthMHz[i] = "";
                            LogToConsole($"{freqGHz} GHz: -1dB带宽未找到两侧边沿");
                        }
                    }

                    num++;
                    SafeIncrementProgressBar();
                    label6.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label6.Refresh();
                }

                WriteArrayToExcelColumn(freqArray, 1, sheetName);
                WriteArrayToExcelColumn(bandwidthMHz, 6, sheetName);
                LogToConsole("-1dB带宽测试完成，已写入Excel");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"-1dB带宽测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("-1dB带宽测试失败", ex.ToString(), operator_textBox.Text);
                LogToConsole("-1dB带宽测试出错: " + ex.Message);
            }
            finally
            {
                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge();
                LogToConsole("-1dB带宽测试已结束");
            }
        }

        private async void button4_Click_1(object sender, EventArgs e)
        {
            const double stepHz = 100e6;
            ScpiDevice rfDevice = new ScpiDevice();
            ScpiDevice loDevice = new ScpiDevice();
            try
            {
                bool connected = await rfDevice.ConnectAsync(_vnaAddress);
                bool connected2 = await loDevice.ConnectAsync(_xinhaoAddress);
                if (!connected || !connected2)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                double? rfFreq = await rfDevice.GetCenterFrequencyAsync();
                double? loFreq = await loDevice.ReadFrequency();
                if (rfFreq == null || loFreq == null)
                {
                    LogToConsole("读取射频/本振频率失败");
                    return;
                }

                double newRf = rfFreq.Value + stepHz;
                double newLo = loFreq.Value + stepHz;

                await rfDevice.SetCenterFrequencyAsync(newRf);
                await Task.Delay(200);
                await loDevice.SetFrequency(newLo);
                await Task.Delay(200);

                LogToConsole($"射频/本振已各加 100 MHz：RF {rfFreq.Value / 1e9:F3}→{newRf / 1e9:F3} GHz，LO {loFreq.Value / 1e9:F3}→{newLo / 1e9:F3} GHz");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加频失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogToConsole("出错: " + ex.Message);
            }
            finally
            {
                rfDevice.Disconnect();
                loDevice.Disconnect();
            }
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            const double stepHz = 100e6;
            ScpiDevice rfDevice = new ScpiDevice();
            ScpiDevice loDevice = new ScpiDevice();
            try
            {
                bool connected = await rfDevice.ConnectAsync(_vnaAddress);
                bool connected2 = await loDevice.ConnectAsync(_xinhaoAddress);
                if (!connected || !connected2)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                double? rfFreq = await rfDevice.GetCenterFrequencyAsync();
                double? loFreq = await loDevice.ReadFrequency();
                if (rfFreq == null || loFreq == null)
                {
                    LogToConsole("读取射频/本振频率失败");
                    return;
                }

                double newRf = rfFreq.Value - stepHz;
                double newLo = loFreq.Value - stepHz;

                await rfDevice.SetCenterFrequencyAsync(newRf);
                await Task.Delay(200);
                await loDevice.SetFrequency(newLo);
                await Task.Delay(200);

                LogToConsole($"射频/本振已各减 100 MHz：RF {rfFreq.Value / 1e9:F3}→{newRf / 1e9:F3} GHz，LO {loFreq.Value / 1e9:F3}→{newLo / 1e9:F3} GHz");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加频失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogToConsole("出错: " + ex.Message);
            }
            finally
            {
                rfDevice.Disconnect();
                loDevice.Disconnect();
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            int chNum = GetSelectedChannelNumber();
            if (chNum <= 0)
            {
                MessageBox.Show("请先勾选一个通道（通道1～8）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 不重新采集：基于 Excel 中已有测量区数据重算寄生调幅差值与 RMS
            CalculateTxParasiticAmplitudeAccuracyFromExcel(chNum);
        }

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
            SafeSetProgressBarMaximum(TxPhaseBaseStateIndices.Length, 0);


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
            if (ch5_checkBox.Checked)
            {
                ch = $"通道5-{testType}";
                chNum = 5;
            }
            if (ch6_checkBox.Checked)
            {
                ch = $"通道6-{testType}";
                chNum = 6;
            }
            if (ch7_checkBox.Checked)
            {
                ch = $"通道7-{testType}";
                chNum = 7;
            }
            if (ch8_checkBox.Checked)
            {
                ch = $"通道8-{testType}";
                chNum = 8;
            }
            string sheetName = $"测试结果{chNum}";

            string visaAddress = _vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            await scpiDevice.LoadStateFile("501.csa");
            await scpiDevice.EnableOutput();
            await SendTestUDP(0, "移相"); // FPGA发码
            await Task.Delay(1000);           // 等待设备稳定
            //await scpiDevice.SetNormalize_Send();
            List<double[]> unwrappedPhases = new List<double[]>();
            List<double[]> gains = new List<double[]>();
            List<int> testedIndices = new List<int>();
            double[] previousPhase = null;
            double[] phaseOffset = null;
            double[] zeroPhase = null;
            foreach (int idx in TxPhaseBaseStateIndices)
            {
                await SendTestUDP(idx, "移相");
                await Task.Delay(600);           // 等待设备稳定
                /*                await scpiDevice.ScanOnce0();
                                await Task.Delay(500);*/
                await scpiDevice.TriggerSingleSweepAfterHoldAsync();
                string[] gain = await scpiDevice.GetGain_Send();    // 幅度（dB）
                string[] initial = await scpiDevice.GetPhase_Send();    // 初相（°）

                // 转换为 double[]
                double[] currentGain = (gain ?? Array.Empty<string>()).Select(s =>
                {
                    double.TryParse(s, out double v);
                    return v;
                }).ToArray();
                gains.Add(currentGain);

                double[] currentPhase = (initial ?? Array.Empty<string>()).Select(s =>
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
                testedIndices.Add(idx);

                num++;
                SafeIncrementProgressBar();
                label6.Text = ((double)num / TxPhaseBaseStateIndices.Length * 100).ToString("f2") + "%";
                label6.Refresh();
            }

            await scpiDevice.DisableOutput();
            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电
            LogToConsole("等待数据写入");
            // 写入解包后的初相与寄生调幅（按发码态索引写到对应列：idx+2）
            _excelTaskQueue.Add(async () =>
            {
                ProcessTxPhaseAccuracyExcelFast(unwrappedPhases, testedIndices, chNum);
                ProcessTxParasiticAmplitudeExcelFast(gains, testedIndices, chNum);
                await Task.CompletedTask;
            });
        }

        private async void toolStripButton7_Click(object sender, EventArgs e)
        {
            VNASetting form = new VNASetting(_vnaAddress);
            form.ShowDialog();
        }
    }
}
