using AxDSOFramer;
using ExcelDataReader;
using MetroFramework.Forms;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DbfTest.DAL;
using DbfTest.FUNCTION;
using DbfTest.MODEL;
using DbfTest.PAGE;
using DbfTest.PAGE.WaitForm;


namespace DbfTest
{
    public partial class Main_New : MetroForm
    {
        private Timer measureTimer;
        private ScpiDevice powerMeterPublic;
        private bool isMeasuring = false;
        #region 变量
    //DeviceFiles.json
        string excelPath = "";
        string vnaFilePath = "";
        string excelMobanPath = "";
        string buchangFilePath = "";
        string shiwangChaSunPath = ""; //矢网差损文件路径
        string pinpuZhupuStatePath = ""; //频谱分析仪主谱状态文件
        string pinpuDaiwaiyizhiPath = ""; //频谱分析仪带外抑制状态文件
        string sanjieJiaotiaoPath = ""; //三阶交调文件路径
        //string vnaSjjtPath = "";


    //DeviceAddressNew.json
        string chargeAddress = "";
        string vnaAddress = "";
        string gonglvAddress = "";
        string xinhaoAddress = "";
        string xinhaoBenzhenAddress = "";
        string pinpuAddress = "";
        string ifaceName = "";
        //string ifaceName = @"\Device\NPF_{3A0CA248-4796-4CBA-B275-E9C8E0A766CF}"; // 注意：需要和系统中接口名称完全匹配
        //static string dstMacStr = "00:0a:35:01:fe:c0";
        string dstMacStr = "";
        string srcMacStr = ""; //上位机MAC地址
        string srcMacAddress = "";
        string srcIpStr = "";
        string dstIpStr = "";
        //static string srcIpStr = "192.168.0.3";
        //static string dstIpStr = "192.168.0.2";

    //TestSetNew.json
        double power = -1;
        double powerBenzhen = -1;
        double startFreq = -1;
        double stopFreq = -1;
        int pointCount = -1;
        double ch1_vol = -1;
        double ch1_cur = -1;
        double ch2_vol = -1;
        double ch2_cur = -1;


        static ushort srcPort = 8080;
        static ushort dstPort = 8080;
        // byte[] headValue = StringToByteArray("00 0a 35 01 fe c0 00 2b 67 f1 71 51 08 00 45 00 b4 68 00 00 80 11 00 00 c0 a8 00 03 c0 a8 00 02 1f 90 1f 90 00 23 81 70");
        byte[] headValue;
        //byte[] modelValue = StringToByteArray("01 03 02 00");
        byte[] modelValue;
        byte[] emptyValue = StringToByteArray("00 00 00 00 00 00 00 00");
        private AxFramerControl _axFramerControl;
        private Main_DAL main_DAL = new Main_DAL();
        private OperateLog_DAL operateLog_DAL = new OperateLog_DAL();
        RecieveTestWait_Form recieveWaitForm = new RecieveTestWait_Form();
        SendTestWait_Form sendWaitForm = new SendTestWait_Form();
        FashejingduWait_Form fashejingduWaitForm = new FashejingduWait_Form();
        JieshoujingduWait_Form jieshoujingduWaitForm = new JieshoujingduWait_Form();
        //string[] jieshouGonglv = new string[200];
        double jieshouChargePower = 0; // 接收电源功率

        //效率
        double I_T85 = 0;
        double I_T5 = 0;
        double I_DQ5 = 0;
        double I_R5 = 0;

        int vnaFlag = 0; // 矢网标志位，0表示未调用矢网文件，1表示已调用矢网文件
        //int fpgaFlag = 0;
        //int testFlag = 0;
        #endregion

        public Main_New()
        {
            InitializeComponent();
            this.Load += Main_New_Load;
        }
        private void Main_New_Load(object sender, EventArgs e)
        {
            testType_comboBox.SelectedIndex = 0;
            person_textBox.Text = "操作员";
            componentName_textBox.Text = "DBF";
            GetAddress();
            GetDeviceFilesJson();
            GetTestSetNewJson();
            InitializeDSO();
        }
        #region 通用方法
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
            catch(Exception ex)
            {
                MessageBox.Show("加载DeviceAddress.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private void GetDeviceFilesJson()
        {
            try
            {
                string filePath = "DeviceFiles.json";
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

                data.TryGetValue("textBox5", out object value8);
                if (value8 != null)
                {
                    sanjieJiaotiaoPath = value8.ToString();
                }

                //data.TryGetValue("textBox10", out object value9);
                //if (value9 != null)
                //{
                //    vnaSjjtPath = value9.ToString();
                //}

            }
            catch(Exception ex)
            {
                MessageBox.Show("加载DeviceFiles.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void GetTestSetNewJson()
        {
            try
            {
                string filePath = "TestSetNew.json";
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
                    if(startFreqDanwei == "MHz")
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

                data.TryGetValue("power2_textBox", out object _power2);
                if (_power2 != null)
                {
                    powerBenzhen = double.Parse(_power2.ToString());
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
                MessageBox.Show("加载TestSetNew.json失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
}
        private void ClearExcelContentBelowRow(Excel.Worksheet sheet, int startRow)
        {
            try
            {
                foreach (Excel.Range column in sheet.UsedRange.Columns)
                {
                    int columnIndex = column.Column;
                    int lastRow = sheet.Cells[sheet.Rows.Count, columnIndex].End(Excel.XlDirection.xlUp).Row;

                    if (lastRow >= startRow)
                    {
                        Excel.Range clearRange = sheet.Range[sheet.Cells[startRow, columnIndex], sheet.Cells[lastRow, columnIndex]];
                        clearRange.ClearContents();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("清除数据失败：" + ex.Message);
            }
        }

        private void InitializeDSO()
        {
            try
            {
                _axFramerControl = new AxFramerControl();
                _axFramerControl.Dock = DockStyle.Fill;

                this.splitContainer2.Panel2.Controls.Add(_axFramerControl);
                _axFramerControl.CreateControl(); // 强制初始化
                _axFramerControl.Titlebar = false;

                // Excel 文件路径
                excelPath = Path.Combine(excelPath, "测试模板.xls");
                if (File.Exists(excelPath))
                {
                    _axFramerControl.Open(excelPath, false, "Excel.Sheet", "", "");

                    // 等待文档加载完成后清空数据（需稍作延迟）
                    /*                    Task.Delay(1000).ContinueWith(_ =>
                                        {
                                            try
                                            {
                                                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                                                Excel.Workbook workbook = excelApp.ActiveWorkbook;

                                                foreach (Excel.Worksheet sheet in workbook.Worksheets)
                                                {
                                                    ClearExcelContentBelowRow(sheet, 8);
                                                }

                                                workbook.Save();
                                            }
                                            catch (Exception ex)
                                            {
                                                MessageBox.Show("清空 Excel 内容失败：" + ex.Message);
                                            }
                                        }, TaskScheduler.FromCurrentSynchronizationContext());*/
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
        private async Task WriteFreqArray()
        {
            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("连接失败");
                return;
            }
            GetDeviceFilesJson();
            await scpiDevice.LoadStateFile(vnaFilePath);
            await Task.Delay(500);
            // 写入频率（从 A8 开始）
            double startFreq = await scpiDevice.GetFreqStart() ?? -1;  // 单位 Hz
            double stopFreq = await scpiDevice.GetFreqStop() ?? -1;    // 单位 Hz
            int _pointCount = await scpiDevice.GetPointCount() ?? -1;

            if (startFreq < 0 || stopFreq < 0 || _pointCount <= 0)
            {
                LogToConsole("获取频率或点数失败，请检查设备连接或设置。");
                scpiDevice.Disconnect();
                return;
            }

            double step = (stopFreq - startFreq) / (_pointCount - 1);
            string[] freqArray = new string[_pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                double freqGHz = (startFreq + step * i) / 1e9;
                freqArray[i] = freqGHz.ToString("F6"); // 保留6位小数（GHz）
            }

            WriteArrayToExcelColumn(freqArray, 1, "测试结果");  // A列，从第8行开始
            //WriteArrayToExcelColumn(freqArray, 1, "常温");  // A列，从第8行开始
            scpiDevice.Disconnect(); // 释放资源
        }
        #endregion

        #region Excel操作
        private void WriteShiWangChaSunToExcel(string[] data, int columnIndex)
        {
            try
            {
                if (!File.Exists(shiwangChaSunPath))
                {
                    MessageBox.Show("指定的 Excel 文件不存在：" + shiwangChaSunPath);
                    return;
                }
                // 去除每个字符串的空格
                string[] cleanedData = data.Select(s => s.Replace("\n", "")).ToArray();
                Excel.Application excelApp = new Excel.Application();
                Excel.Workbook workbook = excelApp.Workbooks.Open(shiwangChaSunPath);
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Sheets[1]; // 默认写入第一个Sheet
                //ClearExcelContentBelowRow(worksheet, 2);
                ClearExcelColumnBelowRow(worksheet, columnIndex, 3);

                for (int i = 0; i < cleanedData.Length; i++)
                {
                    worksheet.Cells[3 + i, columnIndex] = cleanedData[i];
                }

                workbook.Save();
                workbook.Close(false);
                excelApp.Quit();

                System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

            }
            catch (Exception ex)
            {
                MessageBox.Show("写入 Excel 失败：" + ex.Message);
            }
        }

        /*        private void ClearExcelColumnBelowRow(int columnIndex, int startRow = 8)
                {
                    try
                    {
                        var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                        Excel.Workbook workbook = excelApp.ActiveWorkbook;
                        Excel.Worksheet worksheet = (Excel.Worksheet)workbook.ActiveSheet;

                        // 找到当前列中最后有数据的行号
                        int lastRow = worksheet.Cells[worksheet.Rows.Count, columnIndex].End(Excel.XlDirection.xlUp).Row;

                        // 如果最后行在第8行或之后，清除从第8行到最后行之间的单元格
                        if (lastRow >= startRow)
                        {
                            Excel.Range clearRange = worksheet.Range[worksheet.Cells[startRow, columnIndex], worksheet.Cells[lastRow, columnIndex]];
                            clearRange.ClearContents();
                        }

                        workbook.Save();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("清除 Excel 列数据失败：" + ex.Message);
                    }
                }*/
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
        private double Parse(string input)
        {
            double.TryParse(input, out double val);
            return val;
        }
        private string[] ReadChaSunData(string sheetName, int columnIndex)
        {
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Open(shiwangChaSunPath, ReadOnly: true);
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 3;
                int lastRow = worksheet.Cells[worksheet.Rows.Count, columnIndex].End(Excel.XlDirection.xlUp).Row;

                int length = lastRow - startRow + 1;
                string[] result = new string[length];

                for (int i = 0; i < length; i++)
                {
                    var cellVal = worksheet.Cells[startRow + i, columnIndex].Text.ToString().Trim();
                    result[i] = cellVal;
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取矢网差损数据失败：" + ex.Message);
                return new string[0];
            }
            finally
            {
                if (workbook != null)
                {
                    workbook.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                }

                if (excelApp != null)
                {
                    excelApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private string[] ReadExcelColumnData(string sheetName, int columnIndex)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int lastRow = worksheet.Cells[worksheet.Rows.Count, columnIndex].End(Excel.XlDirection.xlUp).Row;

                int length = lastRow - startRow + 1;
                string[] result = new string[length];

                for (int i = 0; i < length; i++)
                {
                    var cellVal = worksheet.Cells[startRow + i, columnIndex].Text.ToString().Trim();
                    result[i] = cellVal;
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取常温数据失败：" + ex.Message);
                return new string[0];
            }
        }

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

        private void WriteArrayToExcelColumn_New(string[] data, int columnIndex, string sheetName)
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


                // 写入数据，从第8行开始
                for (int i = 0; i < cleanedData.Length; i++)
                {
                    worksheet.Cells[4 + i, columnIndex] = cleanedData[i];
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入 Excel 失败：" + ex.Message);
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

                string personText = person_textBox.Text.Trim();

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

        private void WritePeakPowerToMatchingFrequencyRows(string[] freqArray, string[] powerArray, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int powerColumn = 7;  // G列

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
        private void WriteYasuodianToMatchingFrequencyRows(string[] freqArray, string[] yasuodian, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int yasuodianColumn = 4;  // D列

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
                                worksheet.Cells[row, yasuodianColumn] = yasuodian[i]; ;
                                break;
                            }
                        }
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入压缩点失败：" + ex.Message);
            }
        }
        private void WriteDingjiangToMatchingFrequencyRows(string[] freqArray, string[] dingJiang, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int dingJiangColumn = 27;

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
                                worksheet.Cells[row, dingJiangColumn] = dingJiang[i]; ;
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
        private void WriteSanjieJiaotiaoToMatchingFrequencyRows(string[] freqArray, string[] sanjieJiaotiao, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];
                string[] cleanedData = sanjieJiaotiao.Select(s => s.Replace("\n", "")).ToArray();
                int startRow = 8;
                int freqColumn = 1;   // A列
                int sanjieJiaotiaoColumn = 14;  // K列

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
                                worksheet.Cells[row, sanjieJiaotiaoColumn] = cleanedData[i]; ;
                                break;
                            }
                        }
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入三阶交调失败：" + ex.Message);
            }
        }
        private void WriteToMatchingFrequencyRows(string[] freqArray, string[] dataArray, string sheetName, int writeColumn)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列

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
                                worksheet.Cells[row, writeColumn] = dataArray[i]; ;
                                break;
                            }
                        }
                    }
                }

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"函数WriteToMatchingFrequencyRows写入失败：" + ex.Message);
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
                int fasheYizhiColumn = 25;  // I列

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
                                worksheet.Cells[row, fasheYizhiColumn] = fasheYizhi[i];;
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
        private void WritePeakPowerToMatchingFrequencyRows_New(string[] freqArray, string[] powerArray, string[] xiaolvArray, string[] dingJiang, string sheetName)
        {
            try
            {
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook;
                Excel.Worksheet worksheet = workbook.Sheets[sheetName];

                int startRow = 8;
                int freqColumn = 1;   // A列
                int powerColumn = 26;
                int xiaolvColumn = 28;
                int dingJiangColumn = 27;

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
                int zaoshengColumn = 3;  // G列

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
        /// <summary>
        /// 加载补偿数据
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private Dictionary<double, double> LoadCompensationTable(string filePath)
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
                    for (int i = 2; i < table.Rows.Count; i++)
                    {
                        if (double.TryParse(table.Rows[i][0]?.ToString(), out double freq) &&
                            double.TryParse(table.Rows[i][9]?.ToString(), out double comp))
                        {
                            compensationTable[freq] = comp;
                        }
                    }
                }
                return compensationTable;
            }catch(Exception ex)
            {
                MessageBox.Show("加载补偿表失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new Dictionary<double, double>();
            }
        }


        // 简单线性插值
        private double InterpolateCompensation(double freqGHz, Dictionary<double, double> table)
        {
            var keys = table.Keys.OrderBy(f => f).ToList();

            if (freqGHz <= keys.First()) return table[keys.First()];
            if (freqGHz >= keys.Last()) return table[keys.Last()];

            for (int i = 0; i < keys.Count - 1; i++)
            {
                double f1 = keys[i], f2 = keys[i + 1];
                if (freqGHz >= f1 && freqGHz <= f2)
                {
                    double c1 = table[f1], c2 = table[f2];
                    double ratio = (freqGHz - f1) / (f2 - f1);
                    return c1 + ratio * (c2 - c1);
                }
            }
            return 0.0;
        }
        #endregion

        #region 弹出界面
        /// <summary>
        /// 设备地址设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void device_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new DeviceAddressNew_Form(this);
            form.ShowDialog();
        }
        /// <summary>
        /// 设备文件路径设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deviceFile_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new DeviceFiles_Form();
            form.ShowDialog();
        }
        /// <summary>
        /// 测试设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void testSet_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new TestSet_New_Form();
            form.ShowDialog();
        }
        /// <summary>
        /// 手动发码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton7_Click(object sender, EventArgs e)
        {
            Form form = new ManualSend_Form(this);
            form.ShowDialog();
        }

        /// <summary>
        /// 信号发生器设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Form form = new XinhaoControl_Form(this);
            form.ShowDialog();
        }
        /// <summary>
        /// 矢网文件设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vnaFile_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new VNAFile_Form();
            form.ShowDialog();
        }
        /// <summary>
        /// 设备管理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Form form = new DeviceManage_Form(this);
            form.ShowDialog();
        }
        /// <summary>
        /// 电源参数设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChargeSet_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new ChargeControl_Form(this);
            form.ShowDialog();
        }
        /// <summary>
        /// 测试记录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            Form form = new TestData_Form();
            form.ShowDialog();
        }
        /// <summary>
        /// 数据库设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SqlSet_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new SqlSet_Form();
            form.ShowDialog();
        }
        /// <summary>
        /// 频谱分析仪控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pinpuSet_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form form = new PinpuControl_Form(this);
            form.ShowDialog();
        }
        /// <summary>
        /// 功率计控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gonglvSet_ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region 按钮
        private void ch1_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (ch1_checkBox.Checked)
            {
                ch2_checkBox.Checked = false;
                ch3_checkBox.Checked = false;
                ch4_checkBox.Checked = false;
            }
        }

        private void ch2_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (ch2_checkBox.Checked)
            {
                ch1_checkBox.Checked = false;
                ch3_checkBox.Checked = false;
                ch4_checkBox.Checked = false;
            }
        }

        private void ch3_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (ch3_checkBox.Checked)
            {
                ch1_checkBox.Checked = false;
                ch2_checkBox.Checked = false;
                ch4_checkBox.Checked = false;
            }
        }

        private void ch4_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (ch4_checkBox.Checked)
            {
                ch1_checkBox.Checked = false;
                ch2_checkBox.Checked = false;
                ch3_checkBox.Checked = false;
            }
        }
        /// <summary>
        /// 发射测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task SendTestUDP()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1send = ch1_checkBox.Checked ? "0" : "1";
                    string ch2send = ch2_checkBox.Checked ? "0" : "1";
                    string ch3send = ch3_checkBox.Checked ? "0" : "1";
                    string ch4send = ch4_checkBox.Checked ? "0" : "1";
                    string ch1 = "1" + new string('0', 24) + ch1send;
                    string ch2 = "1" + new string('0', 24) + ch2send;
                    string ch3 = "1" + new string('0', 24) + ch3send;
                    string ch4 = "1" + new string('0', 24) + ch4send;
                    string ch5 = new string('0', 16);
                    byte[] modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5 });
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
                    
                    operateLog_DAL.InsertOperateLog_DT("发射测试", $"{ch1send},{ch2send},{ch3send},{ch4send}", person_textBox.Text);

                }
                catch (Exception ex)
                {
                    LogToConsole("发射测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("发射测试失败", ex.ToString(), person_textBox.Text);
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
                    string ch1send = ch1_checkBox.Checked ? "0" : "1";
                    string ch2send = ch2_checkBox.Checked ? "0" : "1";
                    string ch3send = ch3_checkBox.Checked ? "0" : "1";
                    string ch4send = ch4_checkBox.Checked ? "0" : "1";

                    string ch1 = "";
                    string ch2 = "";
                    string ch3 = "";
                    string ch4 = "";
                    if (yixiangOrshuaijian.Equals("移相"))
                    {
                        ch1 = "1" + numToString + "000000" + "000000" + "000000" + ch1send;
                        ch2 = "1" + numToString + "000000" + "000000" + "000000" + ch2send;
                        ch3 = "1" + numToString + "000000" + "000000" + "000000" + ch3send;
                        ch4 = "1" + numToString + "000000" + "000000" + "000000" + ch4send;
                    }
                    if (yixiangOrshuaijian.Equals("衰减"))
                    {
                        ch1 = "1" + "000000" + "000000" + numToString + "000000" + ch1send;
                        ch2 = "1" + "000000" + "000000" + numToString + "000000" + ch2send;
                        ch3 = "1" + "000000" + "000000" + numToString + "000000" + ch3send;
                        ch4 = "1" + "000000" + "000000" + numToString + "000000" + ch4send;
                    }
                    string ch5 = new string('0', 16);
                    modelValue = StringToByteArray("01 03 01 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5 });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    LogToConsole(numToString);
                }
                catch (Exception ex)
                {
                    LogToConsole("发射测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("发射测试失败", ex.ToString(), person_textBox.Text);
                }
            });
        }
        /// <summary>
        /// 发射测试|功率
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button4_Click(object sender, EventArgs e)
        {
/*            if(testFlag == 0)
            {
                MessageBox.Show("请先进行接收测试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }*/
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
            if (string.IsNullOrEmpty(person_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            WritePersonToAllSheets();
            sendWaitForm.Show(); // 显示等待界面
            LogToConsole("开始发射测试...");
            await ChargeSendPowerON(); // 发射加电
            await LoadGonglvState(); // 调用功率计文件
            await SendTestUDP(); //FPGA发包
            await Task.Delay(500); // 延时保证设备稳定
            await WriteFreqArray();
            await GetSendData(); // 获取功率计数据
        }
        /// <summary>
        /// 接收测试|增益
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void receiveTest_button_Click(object sender, EventArgs e)
        {
            if(testType_comboBox.SelectedIndex == -1)
            {
                MessageBox.Show("请选择测试类型", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(person_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始接收测试...");

            recieveWaitForm.Show(); // 显示等待界面
            if (vnaFlag == 0)
            {
                LoadVNAState(); // 调用矢网文件
            }
            WritePersonToAllSheets();
            recieveWaitForm.ChangeLabelText("step1_label","已完成");
            await ChargeRecievePowerON(); // 接收加电
            await RecieveTestUDP(); //FPGA发包
            recieveWaitForm.ChangeLabelText("step3_label", "已完成");
            await Task.Delay(500); // 延时保证设备稳定
            await LoadVNAData(); // 获取矢网数据
            //testFlag = 1;
        }
        private async Task RecieveTestUDP()
        {
            await Task.Run(() =>
            {
                try
                {
                    string ch1recive = ch1_checkBox.Checked ? "0" : "1";
                    string ch2recive = ch2_checkBox.Checked ? "0" : "1";
                    string ch3recive = ch3_checkBox.Checked ? "0" : "1";
                    string ch4recive = ch4_checkBox.Checked ? "0" : "1";
                    string ch1 = ch1recive + new string('0', 24) + "1";
                    string ch2 = ch2recive + new string('0', 24) + "1";
                    string ch3 = ch3recive + new string('0', 24) + "1";
                    string ch4 = ch4recive + new string('0', 24) + "1";
                    string ch5 = new string('0', 16);
                    byte[] modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5 });
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

                    operateLog_DAL.InsertOperateLog_DT("接收测试", $"{ch1recive},{ch2recive},{ch3recive},{ch4recive}", person_textBox.Text);
                }
                catch (Exception ex)
                {
                    LogToConsole("接收测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), person_textBox.Text);
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
                    string ch1 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                    string ch2 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                    string ch3 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                    string ch4 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                    string ch5 = new string('0', 16);
                    modelValue = StringToByteArray("01 03 03 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5 });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                }
                catch (Exception ex)
                {
                    LogToConsole("切换至负载态失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("切换至负载态失败", ex.ToString(), person_textBox.Text);
                }
            });
        }
        /// <summary>
        /// 调用矢网文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void LoadVNAState()
        {
            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("连接失败");
                return;
            }
            GetDeviceFilesJson();
            await scpiDevice.LoadStateFile(vnaFilePath);
            LogToConsole("调用矢网文件");
            vnaFlag = 1;
            scpiDevice.Disconnect(); // 释放资源
        }
        private async Task LoadVNAData()
        {
            recieveWaitForm.ChangeLabelText("step4_label", "进行中...");
            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;
            string sheetName = "测试结果";
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

            string visaAddress = vnaAddress;
            string pmAddress = gonglvAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }

            await scpiDevice.ScanOnce();
            await scpiDevice.SetNormalize();
            string[] gain = await scpiDevice.GetGainStringAsync();               // 增益（dB）
            string[] initial = await scpiDevice.GetInitialPhaseStringAsync();    // 初相（°）
            string[] inputVswr = await scpiDevice.GetInputVSWRStringAsync();     // 输入驻波比
            string[] outputVswr = await scpiDevice.GetOutputVSWRStringAsync();   // 输出驻波比

            recieveWaitForm.ChangeLabelText("step4_label", "已完成");

            recieveWaitForm.ChangeLabelText("step5_label", "进行中...");

            if (chasun_checkBox.Checked)
            {
                string[] gainPlusChasun = null;
                string[] gainChasun = null;
                if (ch.Contains("通道1"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 2);
                }
                if (ch.Contains("通道2"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 3);
                }
                if (ch.Contains("通道3"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 4);
                }
                if (ch.Contains("通道4"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 5);
                }

                int len = gain.Length;

                // 初始化差值数组
                gainPlusChasun = new string[len];

                for (int i = 0; i < len; i++)
                {
                    double g = Parse(gain[i]);
                    double gN = Parse(gainChasun[i]);
                    gainPlusChasun[i] = (g - gN).ToString();

                }
                gain = gainPlusChasun; // 替换原有增益数据
            }

            WriteArrayToExcelColumn(gain, 2, sheetName);
            WriteArrayToExcelColumn(initial, 3, sheetName);
            WriteArrayToExcelColumn(inputVswr, 4, sheetName);
            WriteArrayToExcelColumn(outputVswr, 5, sheetName);

            if(testType == "常温")
            {
                WriteArrayToExcelColumn(gain, 2, "常温");
                WriteArrayToExcelColumn(initial, 3, "常温");
                WriteArrayToExcelColumn(inputVswr, 4, "常温");
                WriteArrayToExcelColumn(outputVswr, 5, "常温");
            }

            LogToConsole("数据读取完成");

            // 写入测量数据
            // 判断是否是差值计算模式
            bool isNormal = testType == "常温";
            bool isLow = testType == "低温";
            bool isHigh = testType == "高温";

            // 需要写入的数据（可能被替换）
            string[] gainFinal = gain;
            string[] initialFinal = initial;
            string[] inputVswrFinal = inputVswr;
            string[] outputVswrFinal = outputVswr;

            LogToConsole("开始写入数据...");
            if (isLow || isHigh)
            {
                string[] gainNormal = ReadExcelColumnData("常温", 2);      // B列
                string[] initialNormal = ReadExcelColumnData("常温", 3);   // C列
                string[] inputVswrNormal = ReadExcelColumnData("常温", 4); // D列
                string[] outputVswrNormal = ReadExcelColumnData("常温", 5);// E列

                int len = gain.Length;

                // 初始化差值数组
                gainFinal = new string[len];
                initialFinal = new string[len];
                inputVswrFinal = new string[len];
                outputVswrFinal = new string[len];

                for (int i = 0; i < len; i++)
                {
                    double g = Parse(gain[i]);
                    double gN = Parse(gainNormal[i]);
                    double p = Parse(initial[i]);
                    double pN = Parse(initialNormal[i]);
                    double vin = Parse(inputVswr[i]);
                    double vinN = Parse(inputVswrNormal[i]);
                    double vout = Parse(outputVswr[i]);
                    double voutN = Parse(outputVswrNormal[i]);

                    if (isLow)
                    {
                        sheetName = "低温-常温";
                        gainFinal[i] = (g - gN).ToString();
                        initialFinal[i] = (p - pN).ToString();
                        inputVswrFinal[i] = (vin - vinN).ToString();
                        outputVswrFinal[i] = (vout - voutN).ToString();
                    }
                    else if (isHigh)
                    {
                        sheetName = "常温-高温";
                        gainFinal[i] = (gN - g).ToString();
                        initialFinal[i] = (pN - p).ToString();
                        inputVswrFinal[i] = (vinN - vin).ToString();
                        outputVswrFinal[i] = (voutN - vout).ToString();
                    }
                }
            }
            WriteArrayToExcelColumn(gainFinal, 2, sheetName);
            WriteArrayToExcelColumn(initialFinal, 3, sheetName);
            WriteArrayToExcelColumn(inputVswrFinal, 4, sheetName);
            WriteArrayToExcelColumn(outputVswrFinal, 5, sheetName);

            /*            WriteArrayToExcelColumn(gain, 2, ch);       // B列
                        WriteArrayToExcelColumn(initial, 3, ch);    // C列
                        WriteArrayToExcelColumn(inputVswr, 4, ch);  // D列
                        WriteArrayToExcelColumn(outputVswr, 5, ch);  // E列*/

            // 写入频率（从 A8 开始）
            double startFreq = await scpiDevice.GetFreqStart() ?? -1;  // 单位 Hz
            double stopFreq = await scpiDevice.GetFreqStop() ?? -1;    // 单位 Hz
            int _pointCount = await scpiDevice.GetPointCount() ?? -1;


            if (startFreq < 0 || stopFreq < 0 || _pointCount <= 0)
            {
                LogToConsole("获取频率或点数失败，请检查设备连接或设置。");
                scpiDevice.Disconnect();
                return;
            }

            //进度条
            int num = 0;
            progressBar1.Maximum = _pointCount;
            progressBar1.Value = 0;

            double step = (stopFreq - startFreq) / (_pointCount - 1);
            string[] freqArray = new string[_pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                double freqGHz = (startFreq + step * i) / 1e9;
                freqArray[i] = freqGHz.ToString("F6"); // 保留6位小数（GHz）
            }

            WriteArrayToExcelColumn(freqArray, 1, sheetName);  // A列，从第8行开始
            WriteArrayToExcelColumn(freqArray, 1, "常温");  // A列，从第8行开始
            LogToConsole("写入Excel完成");
            recieveWaitForm.ChangeLabelText("step5_label", "已完成");
            recieveWaitForm.ChangeLabelText("step6_label", "进行中...");
            // 写入数据库
            try
            {
                int batchId = main_DAL.GetBatchId(ch, componentName);
                string nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var batch = new MeasurementBatch
                {
                    TestType = ch,
                    ComponentName = componentName,
                    Operator = person_textBox.Text,
                    Description = "自动测试批次",
                    UpdateTime = nowTime
                };
                main_DAL.InsertTestBatch_DT(batch);

                for (int i = 0; i < gain.Length; i++)
                {
                    var result = new MeasurementResult
                    {
                        TestType = ch,
                        ComponentName = componentName,
                        BatchId = batchId + 1,
                        PointIndex = i,
                        PointFreq = double.Parse(freqArray[i]),
                        Gain = double.Parse(gainFinal[i]),
                        InitialPhase = double.Parse(initialFinal[i]),
                        InputSWR = double.Parse(inputVswrFinal[i]),
                        OutputSWR = double.Parse(outputVswrFinal[i]),
                        Person = person_textBox.Text,
                        UpdateTime = nowTime
                    };

                    main_DAL.InsertTestData_DT(result);

                    num++;
                    progressBar1.Value += 1;
                    label1.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label1.Refresh();
                }
                LogToConsole("写入数据库完成");
                recieveWaitForm.ChangeLabelText("step6_label", "已完成");
            }
            catch (Exception ex)
            {
                LogToConsole("写入数据库出错: " + ex.Message);
            }
            finally
            {
                scpiDevice.Disconnect(); // 释放资源
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("接收测试已完成");
                recieveWaitForm.Close(); // 关闭等待界面
            }

        }
        private async Task GetGainData_New()
        {
            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();
            try
            {
                string ch = "";
                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;
                string sheetName = "测试结果";
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

                string[] gain = new string[pointCount];

                bool connected = await xinhaoDevice.ConnectAsync(xinhaoAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(xinhaoBenzhenAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await xinhaoBenzhenDevice.SetPower(powerBenzhen);
                await xinhaoBenzhenDevice.QueryOpc();

                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.EnableOutput();

                //进度条
                int num = 0;
                progressBar1.Maximum = pointCount;
                progressBar1.Value = 0;

                double step = (stopFreq - startFreq) / (pointCount - 1);
                string[] freqArray = new string[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoBenzhenDevice.QueryOpc();

                    double center = 175 * 1e6;
                    double start = center - (100 * 1e6);
                    double stop = center + (100 * 1e9);

                    await pinpuDevice.SetStartFrequencyAsync(start);
                    await pinpuDevice.SetStopFrequencyAsync(stop);
                    await pinpuDevice.SetCenterFrequencyAsync(center);

                    await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");

                    double markPower = double.NaN;

                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    gain[i] = markPower.ToString();
                }
                WriteArrayToExcelColumn(freqArray, 1, sheetName);  // A列，从第8行开始
                WriteArrayToExcelColumn(gain, 2, sheetName);  // B列，从第9行开始
                LogToConsole("写入Excel完成");
            }
            catch (Exception ex)
            {
                LogToConsole("接收测试出错: " + ex.Message);
            }
            finally
            {
                await xinhaoDevice.DisableOutput();
                await xinhaoBenzhenDevice.DisableOutput();
                xinhaoDevice.Disconnect(); // 释放资源
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("接收测试已完成");
            }

        }
        private async Task GetDWYZData()
        {


        }
        private async Task GetSendData()
        {
            sendWaitForm.ChangeLabelText("step4_label", "进行中...");
            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;

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

            GetTestSetNewJson();
            string sgAddress = xinhaoAddress;   // 信号源地址
            string pmAddress = gonglvAddress; // 功率计地址
            if(pointCount <= 0)
            {
                MessageBox.Show("请先设置点数", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            int num = 0;
            progressBar1.Maximum = pointCount;
            progressBar1.Value = 0;

            string[] freqArray = new string[pointCount];
            string[] pulsePowerString = new string[pointCount];

            string[] compensatedPowerString = new string[pointCount];
            var compensationTable = LoadCompensationTable(buchangFilePath); //获取补偿数据

            sendWaitForm.ChangeLabelText("step4_label", "已完成");

            sendWaitForm.ChangeLabelText("step5_label", "进行中...");

            var signalGen = new ScpiDevice();
            var powerMeter = new ScpiDevice();

            bool sgConnected = await signalGen.ConnectAsync(sgAddress);
            bool pmConnected = await powerMeter.ConnectAsync(pmAddress);

            if (!sgConnected || !pmConnected)
            {
                LogToConsole("连接失败：信号源或功率计无法连接");
                return;
            }

            try
            {
                await signalGen.EnableOutput(); // 打开信号源输出
                //await signalGen.ModON(); // 打开调制输出
                rf_checkBox.Checked = true;
                //mod_checkBox.Checked = true;

                LogToConsole("获取功率计数据");
                double step = 0;
                if (pointCount > 1)
                {
                    step = (stopFreq - startFreq) / (pointCount - 1);
                }
                if (pointCount == 1)
                {
                    step = 0;
                }
                if (pointCount < 0)
                {
                    MessageBox.Show("信号源点数设置错误，请检查设置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                LogToConsole("开始写入数据...");
                //var results = new List<(double freqGHz, double power)>();
                sendWaitForm.ChangeLabelText("step6_label", "进行中...");
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double benzhenFreq = freqHz - 175 * 1e6;
                    double freqGHz = freqHz / 1e9;
                    freqArray[i] = freqGHz.ToString("F6");

                    await signalGen.SetFrequency(benzhenFreq);
                    await signalGen.QueryOpc();
                    await signalGen.SetPower(power);
                    await signalGen.QueryOpc();

                    await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.ReadPulsePowerArrayAsync(); // 预读取一次丢弃

                    // 读取功率计峰值功率（dBm）
                    double[] pulsePower = await powerMeter.ReadPulsePowerArrayAsync();

                    double compensation = InterpolateCompensation(freqGHz, compensationTable);
                    double compensatedPower = pulsePower[0] - compensation;
                    double PowerWatt = dBmToWatt(compensatedPower); // dBm 转 W

                    compensatedPowerString[i] = compensatedPower.ToString();
                    //compensatedPowerString[i] = compensatedPower.ToString("F3");

                    //pulsePowerString[i] = pulsePower[0].ToString("F3"); // 保留两位小数（dBm）


                    num++;
                    progressBar1.Value += 1;
                    label1.Text = ((double)num / pointCount * 100).ToString("f2") + "%";
                    label1.Refresh();

                    main_DAL.UpdateTestDataFreq_DT(ch, componentName, double.Parse(freqArray[i]), double.Parse(compensatedPowerString[i]));
                }
                //WriteArrayToExcelColumn(freqArray, 7, ch);
                //WriteArrayToExcelColumn(compensatedPowerString, 8, ch);
                WritePeakPowerToMatchingFrequencyRows(freqArray, compensatedPowerString, "测试结果");

                LogToConsole("Excel写入完成");
                sendWaitForm.ChangeLabelText("step5_label", "已完成");
                sendWaitForm.ChangeLabelText("step6_label", "已完成");
            }
            catch (Exception ex)
            {
                LogToConsole($"测量异常：{ex.Message}");
            }
            finally
            {
                await signalGen.DisableOutput(); // 安全关闭输出
                //await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                //mod_checkBox.Checked = false;
                signalGen.Disconnect();
                powerMeter.Disconnect();
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("发射测试已完成");
                sendWaitForm.Close(); // 关闭等待界面
            }
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
        private async Task GetSendData_New()
        {
            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;

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

            GetTestSetNewJson();
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
            var compensationTable = LoadCompensationTable(buchangFilePath);


            var xinhaoDevice = new ScpiDevice();
            var powerMeter = new ScpiDevice();
            var xinhaoBenzhenDevice = new ScpiDevice();

            bool deviceConnected = await xinhaoDevice.ConnectAsync(xinhaoAddress);
            bool deviceConnected2 = await xinhaoBenzhenDevice.ConnectAsync(xinhaoBenzhenAddress);
            bool pmConnected = await powerMeter.ConnectAsync(gonglvAddress);

            if (!deviceConnected || !deviceConnected2 || !pmConnected)
            {
                LogToConsole("设备连接失败");
                return;
            }

            try
            {
                await xinhaoDevice.EnableOutput();
                await xinhaoBenzhenDevice.EnableOutput();

                LogToConsole("获取功率计数据");
                freqArray = GetFilterFreqArray(pointCount);
                double step = (stopFreq - startFreq) / (pointCount - 1);
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoBenzhenDevice.QueryOpc();

                    await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.SendCommandAsync($":SENS:FREQ {freqHz}");
                    await powerMeter.SendCommandAsync(":INIT:IMM");         // 开始测量
                    await powerMeter.SendCommandAsync("*WAI");              // 等待测量完成
                    await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.ReadPulsePowerArrayAsync(); // 预读取一次丢弃

                    // 读取功率计峰值功率（dBm）
                    double[] pulsePower = await powerMeter.ReadPulsePowerArrayAsync();
                    double positiveDur = await powerMeter.GetPositiveDuration() ?? -1;
                    double negativeDur = await powerMeter.GetNegativeDuration() ?? -1;
                    dingJiang[i] = pulsePower[6].ToString();
                    shangshengyan[i] = positiveDur.ToString();
                    xiajiangyan[i] = negativeDur.ToString();

                    double compensation = InterpolateCompensation(freqGHz, compensationTable);
                    double compensatedPower = pulsePower[0] - compensation;
                    double PowerWatt = dBmToWatt(compensatedPower); // dBm 转 W
                    compensatedPowerString[i] = compensatedPower.ToString();

                    I_T85 = await GetCurrent(1);
                    I_T5 = await GetCurrent(2);

                    double fenmu1 = ch1_vol * I_T85;
                    double fenmu2 = ch2_vol * (I_T5 - 0.75 * I_DQ5);
                    double fenmu3 = 0.8 * ch2_vol * (I_R5 - 0.75 * I_DQ5);

                    double chargePower = fenmu1 + fenmu2 + fenmu3;
                    xiaolvString[i] = PowerWatt * 0.2 / chargePower * 10 + "%"; // 计算效率百分比

                    await Task.Delay(500);
                    num++;
                    progressBar1.Value += 1;
                    label1.Text = ((double)num / pointCount * 100).ToString("f2") + "%";
                    label1.Refresh();

                    //main_DAL.UpdateTestDataFreq_DT(ch, componentName, double.Parse(freqArray[i]), double.Parse(compensatedPowerString[i]));
                }

                //WriteArrayToExcelColumn(freqArray, 7, ch);
                //WriteArrayToExcelColumn(compensatedPowerString, 8, ch);
                WritePeakPowerToMatchingFrequencyRows_New(freqArray, compensatedPowerString, xiaolvString, dingJiang, "测试结果");
                WriteArrayToExcelColumn(shangshengyan, 11, "测试结果");
                WriteArrayToExcelColumn(xiajiangyan, 12, "测试结果");

                LogToConsole("Excel写入完成");
            }
            catch (Exception ex)
            {
                LogToConsole($"测量异常：{ex.Message}");
            }
            finally
            {
                await xinhaoDevice.DisableOutput(); // 安全关闭输出
                await xinhaoBenzhenDevice.DisableOutput();
                //await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                //mod_checkBox.Checked = false;
                xinhaoDevice.Disconnect();
                xinhaoBenzhenDevice.Disconnect();
                powerMeter.Disconnect();
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("发射测试已完成");
            }
        }
        /// <summary>
        /// 接收加电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RecievePower_ToolStripMenuItem_Click(object sender, EventArgs e)
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
            catch(Exception ex)
            {
                MessageBox.Show($"接收加电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("接收加电失败", ex.ToString(), person_textBox.Text);
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
                operateLog_DAL.InsertOperateLog_DT("接收加电失败", ex.ToString(), person_textBox.Text);
            }
        }
        /// <summary>
        /// 发射加电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SendPower_ToolStripMenuItem_Click(object sender, EventArgs e)
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
            catch(Exception ex)
            {
                MessageBox.Show($"发射加电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射加电失败", ex.ToString(), person_textBox.Text);
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
                if(ch1_vol <= 0 || ch1_cur <=0 || ch2_vol <= 0 || ch2_cur <= 0)
                {
                    MessageBox.Show("电压或电流值设置有误，请检查测试设置。");
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
                operateLog_DAL.InsertOperateLog_DT("发射加电失败", ex.ToString(), person_textBox.Text);
            }
        }
        private async Task<double> GetRecieveChargePower()
        {
            try
            {
                string visaAddress = chargeAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return -1;
                }
                await scpiDevice.SelectChannel(2);
                double vol2 = await scpiDevice.ReadVoltage() ?? 0;
                double cur2 = await scpiDevice.ReadCurrent() ?? 0;
                double chargePower2 = vol2 * cur2; // 计算电源功率

                scpiDevice.Disconnect();
                return chargePower2;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取电源数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("读取电源数据失败", ex.ToString(), person_textBox.Text);
                return -1;
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
                    LogToConsole("连接失败");
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
                operateLog_DAL.InsertOperateLog_DT("读取电源数据失败", ex.ToString(), person_textBox.Text);
                return -1;
            }
        } 
        /// <summary>
        /// 电源关电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ClosePower_ToolStripMenuItem_Click(object sender, EventArgs e)
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
            catch(Exception ex)
            {
                MessageBox.Show($"电源关电失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("电源关电失败", ex.ToString(), person_textBox.Text);
            }
        }
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
                operateLog_DAL.InsertOperateLog_DT("电源关电失败", ex.ToString(), person_textBox.Text);
            }
        }
        /// <summary>
        /// 新建Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            GetDeviceFilesJson();
            if (!File.Exists(excelMobanPath))
            {
                MessageBox.Show("模板文件不存在，请检查路径。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                string now = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                dialog.Title = "保存新建的 Excel 文件";
                dialog.Filter = "Excel 文件 (*.xls)|*.xls";
                dialog.FileName = "测试结果" + now + ".xls";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(excelMobanPath, dialog.FileName, overwrite: true);
                        MessageBox.Show("Excel 文件已成功创建。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"创建 Excel 文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        operateLog_DAL.InsertOperateLog_DT("创建 Excel 文件失败", ex.ToString(), person_textBox.Text);
                    }
                }
            }
        }
        /// <summary>
        /// 打开Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                _axFramerControl.Titlebar = false;
                var openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = System.Environment.CurrentDirectory;
                openFileDialog.Filter = "Excel 文件 (*.xls)|*.xls";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.FilterIndex = 1;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    this._axFramerControl.Open(openFileDialog.FileName);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"打开Excel失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("打开Excel失败", ex.ToString(), person_textBox.Text);
            }

        }
        /// <summary>
        /// 更新矢网差损
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void update_shiwangchasun_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LoadVNAState();
            string ch = "";
            if (ch1_checkBox.Checked)
            {
                ch = "CH1";
            }
            if (ch2_checkBox.Checked)
            {
                ch = "CH2";
            }
            if (ch3_checkBox.Checked)
            {
                ch = "CH3";
            }
            if (ch4_checkBox.Checked)
            {
                ch = "CH4";
            }
            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }

            await scpiDevice.ScanOnce();
            string[] gain = await scpiDevice.GetGainStringAsync();


            LogToConsole($"数据读取完成");

            // 写入测量数据
            if (ch == "CH1")
            {
                WriteShiWangChaSunToExcel(gain, 2);
            }
            if (ch == "CH2")
            {
                WriteShiWangChaSunToExcel(gain, 3);
            }
            if (ch == "CH3")
            {
                WriteShiWangChaSunToExcel(gain, 4);
            }
            if (ch == "CH4")
            {
                WriteShiWangChaSunToExcel(gain, 5);
            }


            // 写入频率
            double startFreq = await scpiDevice.GetFreqStart() ?? -1;  // 单位 Hz
            double stopFreq = await scpiDevice.GetFreqStop() ?? -1;    // 单位 Hz
            int _pointCount = await scpiDevice.GetPointCount() ?? -1;


            if (startFreq < 0 || stopFreq < 0 || _pointCount <= 0)
            {
                LogToConsole("获取频率或点数失败，请检查设备连接或设置。");
                scpiDevice.Disconnect();
                return;
            }

            int num = 0;
            progressBar1.Maximum = _pointCount;
            progressBar1.Value = 0;

            double step = (stopFreq - startFreq) / (_pointCount - 1);
            string[] freqArray = new string[_pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                double freqGHz = (startFreq + step * i) / 1e9;
                freqArray[i] = freqGHz.ToString("F6"); // 保留6位小数（GHz）

                num++;
                progressBar1.Value += 1;
                label1.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                label1.Refresh();
            }

            WriteShiWangChaSunToExcel(freqArray, 1);
            LogToConsole("写入Excel完成");
            MessageBox.Show("数据已写入：" + shiwangChaSunPath, "写入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            scpiDevice.Disconnect(); // 释放资源
            LogToConsole("矢网差损已完成");
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            LoadVNAState();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取当前 Excel 应用程序实例
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                Excel.Workbook workbook = excelApp.ActiveWorkbook;

                // 构建保存路径
                string testChannel = "";
                if (ch1_checkBox.Checked)
                {
                    testChannel = "ch1";
                }
                if (ch2_checkBox.Checked)
                {
                    testChannel = "ch2";
                }
                if (ch3_checkBox.Checked)
                {
                    testChannel = "ch3";
                }
                if (ch4_checkBox.Checked)
                {
                    testChannel = "ch4";
                }
                string testComponent = componentName_textBox.Text;
                string testType = testType_comboBox.Text;
                string timestamp = DateTime.Now.ToString("yyyy.MM.dd.HHmmss");
                string excelPathTemp = Path.GetDirectoryName(excelPath);
                string savePath = Path.Combine(excelPathTemp, $"{testComponent}{testChannel}_高低温{testType}_{timestamp}{Path.GetExtension(workbook.FullName)}");

                // 保存副本
                workbook.SaveCopyAs(savePath);

                MessageBox.Show($"已成功另存为：{savePath}", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("另存为失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
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
        /// <summary>
        /// 噪声采集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_zaosheng_test_Click(object sender, EventArgs e)
        {
            try
            {
                string visaAddress = pinpuAddress;
                string ch = "";
                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;

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
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                int testPoint = 11; // 采集点数
                string[] freqArray = new string[testPoint];
                string[] NFData = new string[testPoint];
                double step = 0;
                step = (stopFreq - startFreq) / (testPoint - 1);

                for (int i = 0; i < testPoint; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = freqHz / 1e9;
                    freqArray[i] = freqGHz.ToString("F6");
                    string[] data = await scpiDevice.GetZaoshengData(freqHz);
                    NFData[i] = data[5];
                }
                WriteZaoshengToMatchingFrequencyRows(freqArray, NFData, "测试结果");
                LogToConsole("噪声采集");

                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"噪声采集失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("噪声采集失败", ex.ToString(), person_textBox.Text);
            }

        }

        private async void loadState_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string visaAddress = pinpuAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                LogToConsole("调用频谱分析仪状态文件");
                await scpiDevice.LoadPinpuStateAsync(pinpuZhupuStatePath);
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用频谱分析仪状态文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("调用频谱分析仪状态文件失败", ex.ToString(), person_textBox.Text);
            }
        }

        private async void loadGonglvState_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string visaAddress = gonglvAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                { 
                    LogToConsole("连接失败");
                    return;
                }
                LogToConsole("调用功率计状态文件");
                await scpiDevice.LoadGonglvState();
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用功率计状态文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("调用功率计状态文件失败", ex.ToString(), person_textBox.Text);
            }
        }
        /// <summary>
        /// 接收测试|增益
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_recieve_test_Click(object sender, EventArgs e)
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
            if (string.IsNullOrEmpty(person_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始接收测试...");
            WritePersonToAllSheets();
            if (vnaFlag == 0)
            {
                LoadVNAState(); // 调用矢网文件
            }
            await ChargeRecievePowerON(); // 接收加电
            await RecieveTestUDP(); //FPGA发包
            await Task.Delay(500); // 延时保证设备稳定
            await GetGainData_New(); // 获取数据
            //testFlag = 1;
        }

        /// <summary>
        /// 发射测试（多指标）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_fasheceshi_gonglv_Click(object sender, EventArgs e)
        {
            /*            if (testFlag == 0)
                        {
                            MessageBox.Show("请先进行接收测试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }*/
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
            if (string.IsNullOrEmpty(person_textBox.Text))
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
            await GetSendData_New(); // 获取发射数据
        }
        /// <summary>
        /// 效率测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button5_Click(object sender, EventArgs e)
        {
            await WriteFreqArray();
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

            string[] freqArray = new string[pointCount];
            string[] xiaolvString = new string[pointCount];
            string[] compensatedPowerString = new string[pointCount];
            var compensationTable = LoadCompensationTable(buchangFilePath);
            var powerMeter = new ScpiDevice();
            var signalGen = new ScpiDevice();
            bool pmConnected = await powerMeter.ConnectAsync(gonglvAddress);
            bool sgConnected = await signalGen.ConnectAsync(xinhaoAddress);
            if (!pmConnected||!sgConnected)
            {
                LogToConsole("连接失败：功率计或信号源无法连接");
                return;
            }
            try
            {
                freqArray = GetFilterFreqArray(pointCount);
                await signalGen.EnableOutput(); // 打开信号源输出
                rf_checkBox.Checked = true;
                await signalGen.ModON(); // 打开调制输出
                mod_checkBox.Checked = true;
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqGHz = double.Parse(freqArray[i]);

                    await signalGen.SetFrequency(freqHz);
                    await signalGen.QueryOpc();
                    await signalGen.SetPower(power);
                    await signalGen.QueryOpc();
                    await Task.Delay(2000); // 延时保证设备稳定

                    await powerMeter.SendCommandAsync(":INIT:IMM");         // 开始测量
                    await powerMeter.SendCommandAsync("*WAI");              // 等待测量完成
                    await Task.Delay(500); // 延时保证设备稳定
                    await powerMeter.ReadPulsePowerArrayAsync(); // 预读取一次丢弃

                    I_T85 = await GetCurrent(1);
                    I_T5 = await GetCurrent(1);

                    // 读取功率计峰值功率（dBm）
                    double[] pulsePower = await powerMeter.ReadPulsePowerArrayAsync();
                    double postiveDur = await powerMeter.GetPositiveDuration() ?? -1;
                    double negativeDur = await powerMeter.GetNegativeDuration() ?? -1;


                    double compensation = InterpolateCompensation(freqGHz, compensationTable);
                    double compensatedPower = pulsePower[0] - compensation;
                    double PowerWatt = dBmToWatt(compensatedPower); // dBm 转 W

                    double fenmu1 = ch1_vol * I_T85;
                    double fenmu2 = ch2_vol * (I_T5 - 0.75 * I_DQ5);
                    double fenmu3 = 0.8 * ch2_vol * (I_R5 - 0.75 * I_DQ5);

                    double chargePower = fenmu1 + fenmu2 + fenmu3;
                    xiaolvString[i] = PowerWatt * 0.2 / chargePower * 100.0 + "%"; // 计算效率百分比

                    //main_DAL.UpdateTestDataFreq_DT(ch, componentName, double.Parse(freqArray[i]), double.Parse(compensatedPowerString[i]));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"效率测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await signalGen.DisableOutput(); // 安全关闭输出
                await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                mod_checkBox.Checked = false;
                signalGen.Disconnect();
                powerMeter.Disconnect();
                await CloseCharge(); // 电源关电
                LogToConsole("效率测试已完成");
            }

        }
        /// <summary>
        /// 发射抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_fasheyizhi_Click(object sender, EventArgs e)
        {
            var signalGen = new ScpiDevice();
            try
            {
                await ChargeSendPowerON(); // 发射加电
                await SendTestUDP(); //FPGA发包
                await Task.Delay(500); // 延时保证设备稳定
                await WriteFreqArray();
                string[] freqArray = GetFilterFreqArray(pointCount);
                string[] fasheYizhi = new string[pointCount]; //发射抑制

                bool sgConnected = await signalGen.ConnectAsync(xinhaoAddress);
                if (!sgConnected)
                {
                    LogToConsole("连接失败：信号源无法连接");
                    return;
                }
                await signalGen.EnableOutput(); // 打开信号源输出
                rf_checkBox.Checked = true;
                await signalGen.ModON(); // 打开调制输出
                mod_checkBox.Checked = true;
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqGHz = double.Parse(freqArray[i]);

                    await signalGen.SetFrequency(freqHz);
                    await signalGen.QueryOpc();
                    await signalGen.SetPower(power);
                    await signalGen.QueryOpc();
                    await Task.Delay(2000); // 延时保证设备稳定
                    fasheYizhi[i] = (await GetFasheyizhi(freqHz)).ToString();
                }

                //fasheYizhi = await GetFasheyizhiAsync(freqArray);
                WriteFasheyizhiToMatchingFrequencyRows(freqArray, fasheYizhi, "测试结果");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射抑制测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射抑制测试失败", ex.ToString(), person_textBox.Text);
            }
            finally
            {
                await signalGen.DisableOutput(); // 安全关闭输出
                await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                mod_checkBox.Checked = false;
                signalGen.Disconnect();
                await CloseCharge(); // 电源关电
            }
        }
        /// <summary>
        /// 顶降测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button7_Click(object sender, EventArgs e)
        {
            await ChargeSendPowerON(); // 发射加电
            await SendTestUDP(); //FPGA发包
            await Task.Delay(500); // 延时保证设备稳定
            await WriteFreqArray();
            var signalGen = new ScpiDevice();
            var powerMeter = new ScpiDevice();
            string sgAddress = xinhaoAddress;   // 信号源地址
            string pmAddress = gonglvAddress; // 功率计地址

            bool sgConnected = await signalGen.ConnectAsync(sgAddress);
            bool pmConnected = await powerMeter.ConnectAsync(pmAddress);
            if (!sgConnected || !pmConnected)
            {
                LogToConsole("连接失败：信号源或功率计无法连接");
                return;
            }
            try
            {
                await WriteFreqArray();
                await powerMeter.LoadGonglvState();
                string[] dingJiang = new string[pointCount]; //顶降
                string[] freqArray = GetFilterFreqArray(pointCount);
                await signalGen.EnableOutput(); // 打开信号源输出
                rf_checkBox.Checked = true;
                await signalGen.ModON(); // 打开调制输出
                mod_checkBox.Checked = true;
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = double.Parse(freqArray[i]) * 1e9;
                    double freqGHz = double.Parse(freqArray[i]);

                    await signalGen.SetFrequency(freqHz);
                    await signalGen.QueryOpc();
                    await signalGen.SetPower(power);
                    await signalGen.QueryOpc();

                    // 读取功率计顶降
                    double dingjiangDouble = await GetDingjiangDouble(powerMeter);
                    dingJiang[i] = dingjiangDouble.ToString();
                }
                WriteDingjiangToMatchingFrequencyRows(freqArray, dingJiang, "测试结果");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"顶降测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("顶降测试失败", ex.ToString(), person_textBox.Text);
            }
            finally
            {
                await signalGen.DisableOutput(); // 安全关闭输出
                await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                mod_checkBox.Checked = false;
                signalGen.Disconnect();
                powerMeter.Disconnect();
                await CloseCharge(); // 电源关电
            }
        }

        /// <summary>
        /// 加载频谱分析仪带外抑制状态文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void loadPinpudwyz_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string visaAddress = pinpuAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return;
                }
                LogToConsole("调用频谱分析仪状态文件");
                await scpiDevice.LoadPinpuStateAsync(pinpuDaiwaiyizhiPath);
                scpiDevice.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"调用频谱分析仪状态文件失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("调用频谱分析仪状态文件失败", ex.ToString(), person_textBox.Text);
            }
        }

        /// <summary>
        /// 三阶交调测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_sjjt_test_Click(object sender, EventArgs e)
        {
            ScpiDevice signalGen = new ScpiDevice();
            //try
            //{
                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP(); //FPGA发包
                await Task.Delay(500); // 延时保证设备稳定
                await WriteFreqArray();
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

                await signalGen.EnableOutput(); // 打开信号源输出
                await signalGen.ModON(); // 打开调制输出
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



                WriteSanjieJiaotiaoToMatchingFrequencyRows(freqArray, sanjieJiaotiao, "测试结果");
                scpiDevice.Disconnect();
                vnaDevice.Disconnect();

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"三阶交调测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    operateLog_DAL.InsertOperateLog_DT("三阶交调测试失败", ex.ToString(), person_textBox.Text);
            //}
            //finally
            //{
                await CloseCharge(); // 电源关电
                await signalGen.DisableOutput(); // 安全关闭输出
                await signalGen.ModOFF();
                rf_checkBox.Checked = false;
                mod_checkBox.Checked = false;
            //}
        }
        #endregion

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
            catch(Exception ex)
            {
                MessageBox.Show($"UDP发送失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("UDP发送失败", ex.ToString(), person_textBox.Text);
            }

        }

        static byte[] GenerateCodeValueFromBits(string[] bitStrings)
        {
            if (bitStrings.Length != 5)
                throw new ArgumentException("应包含5个通道的比特串");

            int[] expectedLengths = { 26, 26, 26, 26, 16 };
            string allBits = "";

            for (int i = 0; i < 5; i++)
            {
                var bits = bitStrings[i].Replace(" ", "");
                if (bits.Length != expectedLengths[i])
                    throw new ArgumentException($"通道{i + 1} 应为 {expectedLengths[i]} 位，但提供了 {bits.Length} 位");

                allBits += bits;
            }

            if (allBits.Length != 120)
                throw new ArgumentException("总位数应为120");

            byte[] codeBytes = new byte[15];
            for (int i = 0; i < 15; i++)
            {
                codeBytes[i] = Convert.ToByte(allBits.Substring(i * 8, 8), 2);
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

        #endregion

        #region 设备控制
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
                    LogToConsole("连接失败");
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
                operateLog_DAL.InsertOperateLog_DT("调用功率计文件失败", ex.ToString(), person_textBox.Text);
            }
        }
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
                    operateLog_DAL.InsertOperateLog_DT("打开射频输出失败", ex.ToString(), person_textBox.Text);
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
                    operateLog_DAL.InsertOperateLog_DT("关闭射频输出失败", ex.ToString(), person_textBox.Text);
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
                    operateLog_DAL.InsertOperateLog_DT("启用调制功能失败", ex.ToString(), person_textBox.Text);
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
                    operateLog_DAL.InsertOperateLog_DT("关闭调制功能失败", ex.ToString(), person_textBox.Text);
                }
            }
        }
        /// <summary>
        /// 关闭射频输出功能
        /// </summary>
        private async void CloseRFOutPut()
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
                operateLog_DAL.InsertOperateLog_DT("关闭射频输出失败", ex.ToString(), person_textBox.Text);
            }
        }
        /// <summary>
        /// 关闭调制功能
        /// </summary>
        private async void CloseModOutPut()
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
                operateLog_DAL.InsertOperateLog_DT("关闭调制功能失败", ex.ToString(), person_textBox.Text);
            }
        }
        private async Task<double> GetFasheyizhi(double freq)
        {
            try
            {
                string visaAddress = pinpuAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return 0;
                }

                /*                // 1. 加载状态文件
                                await scpiDevice.LoadPinpuStateAsync(pinpuZhupuStatePath);
                                //await scpiDevice.LoadPinpuStateAsync("/usrdata/Data/teststa.state");
                                await Task.Delay(1000); // 延时保证设备稳定
                                double center = freq;
                                double start = freq - (0.2 * 1e9);
                                double stop = freq + (0.2 *1e9);
                                await scpiDevice.SetCenterFrequencyAsync(center);
                                await scpiDevice.SetStartFrequencyAsync(start);
                                await scpiDevice.SetStopFrequencyAsync(stop);
                                await Task.Delay(1000); // 延时保证设备稳定
                                await scpiDevice.StartSingleSweepAsync();
                                await Task.Delay(1000); // 延时保证设备稳定

                                // 2. 开启 marker 并设置位置
                                await scpiDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                                await scpiDevice.SendCommandAsync($":CALC:MARK1:X {freq}");
                                //await scpiDevice.SetMarkerToMaxAsync();
                                // 3. 读取功率
                                double power = await scpiDevice.ReadMarkerPowerAsync() ?? 0;
                                await Task.Delay(1000); // 延时保证设备稳定*/
                // 1. 加载状态文件（仅一次）
                await scpiDevice.LoadPinpuStateAsync(pinpuZhupuStatePath);
                await Task.Delay(1000); // 延时保证设备稳定

                // 2. 设置频率范围
                double center = freq;
                double start = center - (0.2 * 1e9);
                double stop = center + (0.2 * 1e9);

                await scpiDevice.SetStartFrequencyAsync(start);
                await scpiDevice.SetStopFrequencyAsync(stop);
                await scpiDevice.SetCenterFrequencyAsync(center);

                // 4. 启用 Marker 并设置频率位置
                await scpiDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                int maxRetries = 10;
                double power = double.NaN;

                for (int i = 0; i < maxRetries; i++)
                {
                    // 将 marker 设置为最大点
                    await scpiDevice.SendCommandAsync(":CALC:MARK1:MAX");
                    await Task.Delay(200); // 让设备处理
                    // 读取 Marker 的功率值
                    power = await scpiDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    // 判断是否为有效功率
                    if (!double.IsNaN(power) && power > -10 && power < 10)
                        break;

                    await Task.Delay(200); // 等待波形稳定
                }

                // 1. 加载状态文件
                await scpiDevice.LoadPinpuStateAsync(pinpuDaiwaiyizhiPath);
                await Task.Delay(500); // 延时保证设备稳定

                // 2. 开启 marker 并设置位置
                await scpiDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                //double freq1 = freq - 0.3 * 1e9;
                double rbw = 300 * 1e6;
                double freq1 = freq - rbw;

                double power1 = double.NaN;

                for (int j = 0; j < maxRetries; j++)
                {
                    await scpiDevice.SendCommandAsync($":CALC:MARK1:X {freq1}");
                    await Task.Delay(200); // 让设备处理
                    // 读取 Marker 的功率值
                    power1 = await scpiDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    // 判断是否为有效功率
                    if (!double.IsNaN(power1) && power1 > -100 && power1 < 100)
                        break;

                    await Task.Delay(200); // 等待波形稳定
                }
                double result = power - power1;
                LogToConsole("发射抑制测试:" + freq / 1e9 + ": " + power + " - " + power1 + " = " + result);
                scpiDevice.Disconnect();
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射抑制测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射抑制测试失败", ex.ToString(), person_textBox.Text);
                return 0;
            }
        }
        private async Task<string[]> GetFasheyizhiAsync(string[] freqArray)
        {
            List<string> results = new List<string>();

            try
            {
                string visaAddress = pinpuAddress;
                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return results.ToArray();
                }

                // 解析频率数组为 double
                List<double> freqs = new List<double>();
                foreach (var f in freqArray)
                {
                    if (double.TryParse(f, out double parsed))
                    {
                        freqs.Add(parsed * 1e9);
                    }
                    else
                    {
                        LogToConsole($"频率格式错误: {f}");
                        results.Add(null);
                    }
                }

                // Step 1: 加载主频状态文件，一次读取所有主频点功率
                await scpiDevice.LoadPinpuStateAsync("/usrdata/Data/9105s.state");
                await Task.Delay(500);

                List<double> mainPowers = new List<double>();
                foreach (var freq in freqs)
                {
                    // 1. 加载状态文件（仅一次）
                    await scpiDevice.LoadPinpuStateAsync(pinpuZhupuStatePath);
                    await Task.Delay(1000); // 延时保证设备稳定

                    // 2. 设置频率范围
                    double center = freq;
                    double start = center - (0.2 * 1e9);
                    double stop = center + (0.2 * 1e9);

                    await scpiDevice.SetStartFrequencyAsync(start);
                    await scpiDevice.SetStopFrequencyAsync(stop);
                    await scpiDevice.SetCenterFrequencyAsync(center);

                    // 4. 启用 Marker 并设置频率位置
                    await scpiDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                    int maxRetries = 5;
                    double power = double.NaN;

                    for (int i = 0; i < maxRetries; i++)
                    {
                        // 将 marker 设置为最大点
                        await scpiDevice.SendCommandAsync(":CALC:MARK1:MAX");

                        // 读取 Marker 的功率值
                        power = await scpiDevice.ReadMarkerPowerAsync() ?? double.NaN;

                        // 判断是否为有效功率
                        if (!double.IsNaN(power) && power > -20 && power < 10)
                            break;

                        await Task.Delay(200); // 等待波形稳定
                    }

                    // 5. 日志记录
                    if (double.IsNaN(power) || power < -20)
                    {
                        LogToConsole($"频点 {freq / 1e9}GHz 无有效信号");
                    }
                    else
                    {
                        LogToConsole($"频点 {freq / 1e9}GHz 最大功率 = {power} dBm");
                    }
                    mainPowers.Add(power);
                }

                // Step 2: 加载偏移频率状态文件，一次读取所有偏移频率点功率
                await scpiDevice.LoadPinpuStateAsync("/usrdata/Data/9105dwyz.state");
                await Task.Delay(500);

                List<double> offsetPowers = new List<double>();
                foreach (var freq in freqs)
                {
                    // 1. 加载状态文件
                    await scpiDevice.LoadPinpuStateAsync(pinpuDaiwaiyizhiPath);
                    await Task.Delay(500); // 延时保证设备稳定

                    // 2. 开启 marker 并设置位置
                    await scpiDevice.SendCommandAsync(":CALC:MARK1:STATE ON");
                    //double freq1 = freq - 0.3 * 1e9;
                    double rbw = 200000;
                    double freq1 = freq - rbw;
                    await scpiDevice.SendCommandAsync($":CALC:MARK1:X {freq1}");
                    // 3. 读取功率
                    double power1 = await scpiDevice.ReadMarkerPowerAsync() ?? 0;
                    offsetPowers.Add(power1);
                }

                // Step 3: 计算差值
                for (int i = 0; i < freqs.Count; i++)
                {
                    string result = (mainPowers[i] - offsetPowers[i]).ToString();
                    results.Add(result);
                    LogToConsole($"发射抑制: {freqs[i] / 1e9:F3} GHz -> {mainPowers[i]:F2} - {offsetPowers[i]:F2} = {result:F2} dB");
                }

                scpiDevice.Disconnect();
                return results.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发射抑制测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("发射抑制测试失败", ex.ToString(), person_textBox.Text);
                return results.ToArray();
            }
        }
        private async Task<double> GetGonglvDingjiang()
        {
            try
            {
                string visaAddress = gonglvAddress;

                ScpiDevice scpiDevice = new ScpiDevice();

                bool connected = await scpiDevice.ConnectAsync(visaAddress);
                if (!connected)
                {
                    LogToConsole("连接失败");
                    return 0;
                }
                await Task.Delay(500); // 延时保证设备稳定
                await scpiDevice.GetDingjiang();//预读取一次丢弃
                double power = await scpiDevice.GetDingjiang() ?? 0;
                scpiDevice.Disconnect();
                return power;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"顶降测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("顶降测试失败", ex.ToString(), person_textBox.Text);
                return 0;
            }
        }
        /// <summary>
        /// 切换至负载态
        /// </summary>
        private async Task changeToFuZaiTai()
        {
            await Task.Run(() =>
            {
                string ch1 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                string ch2 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                string ch3 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                string ch4 = "1" + "000000" + "000000" + "000000" + "000000" + "1";
                string ch5 = new string('0', 16);
                modelValue = StringToByteArray("01 03 03 00");
                var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5 });

                SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
            });
        }
        /// <summary>
        /// 测量顶降
        /// </summary>
        /// <param name="powerMeter"></param>
        /// <returns></returns>
        private async Task<double> GetDingjiangDouble(ScpiDevice powerMeter)
        {
            try
            {
                double dingjiangDouble = double.NaN;
                int maxAttempts = 15;
                int attempt = 0;

                while (attempt < maxAttempts)
                {
                    attempt++;

                    await powerMeter.SendCommandAsync(":INIT:IMM");
                    await powerMeter.SendCommandAsync("*WAI");
                    await Task.Delay(300);

                    dingjiangDouble = await powerMeter.GetDingjiang() ?? double.NaN;

                    // 判断是否为有效值
                    if (Math.Abs(dingjiangDouble - 9.91e37) > 1e30)
                    {
                        LogToConsole($"顶降：{dingjiangDouble} dB");
                        break;
                    }
                }

                if (Math.Abs(dingjiangDouble - 9.91e37) <= 1e30)
                {
                    LogToConsole("多次测量未获得有效顶降值。");
                    return double.NaN;
                }
                return dingjiangDouble;
            }
            catch (Exception ex)
            {
                LogToConsole($"测量异常：{ex.Message}");
                return double.NaN;
            }
        }
        #endregion
        /// <summary>
        /// 压缩点测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_yasuodian_test_Click(object sender, EventArgs e)
        {
            try
            {
                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP();       // FPGA发包
                await Task.Delay(500);     // 延时保证设备稳定
                await WriteFreqArray();

                string[] yasuodian = new string[pointCount];
                string[] freqArray = GetFilterFreqArray(pointCount);
                LogToConsole("压缩点测试");

                var scpiDevice = new ScpiDevice();

                bool deviceConnected = await scpiDevice.ConnectAsync(vnaAddress);

                if (!deviceConnected)
                {
                    LogToConsole("连接失败：矢网无法连接");
                    return;
                }

                await scpiDevice.LoadStateFile(vnaFilePath);

                double startPower = -65;
                double stopPower = -40;
                double stepPower = 1;

                double[] refGains = new double[pointCount];
                //double[] compressionPoints = new double[freqCount];
                bool[] found = new bool[pointCount];

                // 1. 获取参考小信号增益
                await scpiDevice.SendCommandAsync($":SOUR:POW2:LEV:IMM:AMPL {startPower}");
                await Task.Delay(200);
                await scpiDevice.ScanOnce();
                await Task.Delay(500);
                string[] refGainStrs = await scpiDevice.GetGain_Yasuodian();
                refGains = refGainStrs.Select(s => double.TryParse(s, out var d) ? d : double.NaN).ToArray();

                // 2. 增加功率，查找压缩点
                for (double power = startPower + stepPower; power <= stopPower; power += stepPower)
                {
                    await scpiDevice.SendCommandAsync($":SOUR:POW2:LEV:IMM:AMPL {power}");
                    await Task.Delay(200);
                    await scpiDevice.ScanOnce();
                    await Task.Delay(500);
                    string[] gainStrs = await scpiDevice.GetGain_Yasuodian();
                    double[] gains = gainStrs.Select(s => double.TryParse(s, out var d) ? d : double.NaN).ToArray();

                    for (int i = 0; i < pointCount; i++)
                    {
                        if (!found[i] && !double.IsNaN(gains[i]) && !double.IsNaN(refGains[i]))
                        {
                            double delta = refGains[i] - gains[i];
                            if (delta >= 1.0)
                            {
                                //compressionPoints[i] = power; //当前功率即为压缩点
                                yasuodian[i] = power.ToString();
                                found[i] = true;
                            }
                        }
                    }
                }

                scpiDevice.Disconnect();
                await CloseCharge();

                //WriteYasuodianToMatchingFrequencyRows(freqArray, yasuodian, "测试结果");
                WriteArrayToExcelColumn(yasuodian, 4, "测试结果");  // B列，从第9行开始
                LogToConsole("压缩点测试完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"压缩点测试失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                operateLog_DAL.InsertOperateLog_DT("压缩点测试失败", ex.ToString(), person_textBox.Text);
            }
        }

        public async Task<double?> MeasureP1dBAsync(double testFreqHz)
        {
            double startPower_dBm = -30;
            double stopPower_dBm = 10;
            double step_dB = 1;
            List<double> inputList = new List<double>();
            List<double> outputList = new List<double>();
            List<double> gainList = new List<double>();
            var signalGen = new ScpiDevice();
            var powerMeter = new ScpiDevice();
            string sgAddress = xinhaoAddress;   // 信号源地址
            string pmAddress = gonglvAddress; // 功率计地址

            bool sgConnected = await signalGen.ConnectAsync(sgAddress);
            bool pmConnected = await powerMeter.ConnectAsync(pmAddress);
            if (!sgConnected || !pmConnected)
            {
                LogToConsole("连接失败：信号源或功率计无法连接");
                return null;
            }
            await signalGen.EnableOutput(); // 打开信号源输出
            rf_checkBox.Checked = true;
            await signalGen.ModON(); // 打开调制输出
            mod_checkBox.Checked = true;
            // 设置频率
            await signalGen.SetFrequency(testFreqHz);
            await signalGen.QueryOpc();

            for (double inputPower = startPower_dBm; inputPower <= stopPower_dBm; inputPower += step_dB)
            {
                await signalGen.SetPower(inputPower);
                await signalGen.QueryOpc();

                await powerMeter.SendCommandAsync(":INIT:IMM"); // 开始测量
                await powerMeter.SendCommandAsync("*WAI");      // 等待测量完成
                await Task.Delay(300); // 稳定等待

                double[] outputPower = await powerMeter.ReadPulsePowerArrayAsync(); // 自定义方法读取功率（dBm）

                double gain = outputPower[0] - inputPower;

                inputList.Add(inputPower);
                outputList.Add(outputPower[0]);
                gainList.Add(gain);

                Console.WriteLine($"In: {inputPower:F2} dBm -> Out: {outputPower[0]:F2} dBm, Gain: {gain:F2} dB");

                if (gainList.Count >= 2)
                {
                    double refGain = gainList[0]; // 第一次增益为参考
                    double gainDrop = refGain - gain;

                    if (gainDrop >= 1.0)
                    {
                        Console.WriteLine($"Detected 1dB compression at input = {inputPower:F2} dBm");
                        return inputPower;
                    }
                }
            }

            Console.WriteLine("No compression point detected in specified range.");
            await signalGen.DisableOutput(); // 安全关闭输出
            await signalGen.ModOFF();
            rf_checkBox.Checked = false;
            mod_checkBox.Checked = false;
            signalGen.Disconnect();
            powerMeter.Disconnect();
            await CloseCharge(); // 电源关电

            return null;
        }
        #region 定时测量顶降
        private async void button10_Click(object sender, EventArgs e)
        {
            if (isMeasuring)
            {
                LogToConsole("测量已在进行中...");
                return;
            }

            powerMeterPublic = new ScpiDevice();
            string pmAddress = gonglvAddress;

            bool pmConnected = await powerMeterPublic.ConnectAsync(pmAddress);
            if (!pmConnected)
            {
                LogToConsole("连接失败：功率计无法连接");
                return;
            }

            await powerMeterPublic.LoadGonglvState();
            await Task.Delay(500);

            measureTimer = new Timer();
            measureTimer.Interval = 2000; // 每秒执行一次
            measureTimer.Tick += async (s, args) => await MeasureDingJiang();
            measureTimer.Start();

            isMeasuring = true;
            LogToConsole("已开始每秒自动测量顶降...");
        }


        private async Task MeasureDingJiang()
        {
            try
            {
                double dingjiangDouble = double.NaN;
                int maxAttempts = 10;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    await powerMeterPublic.SendCommandAsync(":INIT:IMM");
                    await powerMeterPublic.SendCommandAsync("*WAI");
                    await Task.Delay(300);

                    dingjiangDouble = await powerMeterPublic.GetDingjiang() ?? double.NaN;

                    if (Math.Abs(dingjiangDouble - 9.91e37) > 1e30)
                    {
                        LogToConsole($"顶降：{dingjiangDouble} dB");
                        return;
                    }
                }

                LogToConsole("多次测量未获得有效顶降值。");
            }
            catch (Exception ex)
            {
                LogToConsole($"测量异常：{ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// 移相精度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_jieshouyixiang_test_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            jieshoujingduWaitForm.Show();
            LogToConsole("开始移相精度测试...");
            WritePersonToAllSheets();
            LoadVNAState(); // 调用矢网文件
            await ChargeRecievePowerON(); // 接收加电

            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;
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

            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(vnaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            await RecieveTestUDP(0, "移相"); // FPGA发码
            await Task.Delay(1000);           // 等待设备稳定
            await scpiDevice.SetNormalize();
            List<double[]> unwrappedPhases = new List<double[]>();
            double[] previousPhase = null;
            double[] phaseOffset = null;
            for (int i = 1; i <= 63; i++)
            {
                await RecieveTestUDP(i, "移相"); // FPGA发码
                await Task.Delay(500);           // 等待设备稳定
                await scpiDevice.ScanOnce(2);

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
                WriteArrayToExcelColumn_New(gain, i + 3, "接收寄生调幅");
            }

            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电

            // 写入解包后的初相（第 i + 2 列）
            for (int i = 0; i < unwrappedPhases.Count; i++)
            {
                string[] phaseStrings = unwrappedPhases[i].Select(v => v.ToString()).ToArray();
                WriteArrayToExcelColumn_New(phaseStrings, i + 3, "接收通道相移精度测试结果");
            }
            SubtractStandardAndWriteResult("接收通道相移精度测试结果");

            CalculatePhaseAccuracyAndWriteToExcel("接收通道相移精度测试结果");

            CalculatePhaseAccuracyAndWriteToExcel_Jisheng("接收寄生调幅");
        }
        public void SubtractStandardAndWriteResult(string sheetName)
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
                for (int row = 4; row <= 154; row++)
                {
                    object cellValueObj = phaseSheet.Cells[row, col].Value;
                    if (cellValueObj != null && double.TryParse(cellValueObj.ToString(), out double measuredValue))
                    {
                        double result = measuredValue - standardValue;
                        int targetRow = 161 + (row - 4);
                        phaseSheet.Cells[targetRow, col].Value = result;
                    }
                }
            }

            LogToConsole("数据差值写入完成");
        }


        private void CalculatePhaseAccuracyAndWriteToExcel(string sheetName)
        {
            try
            {
                LogToConsole("开始计算精度...");
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook; ;
                Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];
                Excel.Worksheet resultSheet = workbook.Sheets["测试结果"];

                // 获取频率点行数
                int startRow = 161;
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
                    double rms = Math.Sqrt(phaseValues.Average(v => v * v));

                    // 在“测试结果”中查找对应频率行并写入 RMS 到 I 列（第9列）
                    int resultRow = FindRowByFrequency(resultSheet, freqGHz);
                    if (resultRow > 0)
                    {
                        if (sheetName.Equals("接收通道相移精度测试结果"))
                        {
                            resultSheet.Cells[resultRow, 7].Value = rms.ToString();
                        }
                        if (sheetName.Equals("接收通道衰减精度测试结果"))
                        {
                            resultSheet.Cells[resultRow, 9].Value = rms.ToString();
                        }
                        if (sheetName.Equals("发射通道相移精度测试结果"))
                        {
                            resultSheet.Cells[resultRow, 19].Value = rms.ToString();
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
        private void CalculatePhaseAccuracyAndWriteToExcel_Jisheng(string sheetName)
        {
            try
            {
                LogToConsole("开始计算寄生精度...");
                var excelApp = (Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                var workbook = excelApp.ActiveWorkbook; ;
                Excel.Worksheet phaseSheet = workbook.Sheets[sheetName];
                Excel.Worksheet resultSheet = workbook.Sheets["测试结果"];

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
                    double rms = Math.Sqrt(phaseValues.Average(v => v * v));

                    // 在“测试结果”中查找对应频率行并写入 RMS 到 I 列（第9列）
                    int resultRow = FindRowByFrequency(resultSheet, freqGHz);
                    if (resultRow > 0)
                    {
                        if (sheetName.Equals("接收寄生调幅"))
                        {
                            resultSheet.Cells[resultRow, 8].Value = rms.ToString();
                        }
                        if (sheetName.Equals("接收寄生调相"))
                        {
                            resultSheet.Cells[resultRow, 10].Value = rms.ToString();
                        }
                        if (sheetName.Equals("发射寄生调幅"))
                        {
                            resultSheet.Cells[resultRow, 20].Value = rms.ToString();
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

        private async Task RecieveTestUDP(int num,string yixiangOrshuaijian)
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
                    string ch1recive = ch1_checkBox.Checked ? "0" : "1";
                    string ch2recive = ch2_checkBox.Checked ? "0" : "1";
                    string ch3recive = ch3_checkBox.Checked ? "0" : "1";
                    string ch4recive = ch4_checkBox.Checked ? "0" : "1";

                    string ch1 = "";
                    string ch2 = "";
                    string ch3 = "";
                    string ch4 = "";
                    if (yixiangOrshuaijian.Equals("移相"))
                    {
                        ch1 = ch1recive + "000000" + numToString + "000000" + "000000" + "1";
                        ch2 = ch2recive + "000000" + numToString + "000000" + "000000" + "1";
                        ch3 = ch3recive + "000000" + numToString + "000000" + "000000" + "1";
                        ch4 = ch4recive + "000000" + numToString + "000000" + "000000" + "1";
                    }
                    if(yixiangOrshuaijian.Equals("衰减"))
                    {
                        ch1 = ch1recive + "000000" + "000000" + "000000" + numToString + "1";
                        ch2 = ch2recive + "000000" + "000000" + "000000" + numToString + "1";
                        ch3 = ch3recive + "000000" + "000000" + "000000" + numToString + "1";
                        ch4 = ch4recive + "000000" + "000000" + "000000" + numToString + "1";
                    }
                    string ch5 = new string('0', 16);
                    modelValue = StringToByteArray("01 03 02 00");
                    var codeValue = GenerateCodeValueFromBits(new[] { ch1, ch2, ch3, ch4, ch5 });

                    SendCustomPacket(headValue, modelValue, emptyValue, codeValue);
                    LogToConsole(numToString);
                }
                catch (Exception ex)
                {
                    LogToConsole("接收测试失败: " + ex);
                    operateLog_DAL.InsertOperateLog_DT("接收测试失败", ex.ToString(), person_textBox.Text);
                }
            });
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
        }
        /// <summary>
        /// 镜频抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_jingpin_test_Click(object sender, EventArgs e)
        {
            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();
            try
            {
                string ch = "";
                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;
                string sheetName = "测试结果";
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

                string[] gain = new string[pointCount];

                bool connected = await xinhaoDevice.ConnectAsync(xinhaoAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(xinhaoBenzhenAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }

                await xinhaoBenzhenDevice.SetPower(powerBenzhen);
                await xinhaoBenzhenDevice.QueryOpc();


                //进度条
/*                int num = 0;
                progressBar1.Maximum = pointCount;
                progressBar1.Value = 0;*/

                double step = (stopFreq - startFreq) / (pointCount - 1);
                string[] freqArray = new string[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoDevice.SetFrequency(freqHz - 350 * 1e6);
                    await xinhaoDevice.QueryOpc();
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoBenzhenDevice.QueryOpc();

                    double center = 175 * 1e6;
                    double start = 0;
                    double stop = 350 * 1e6;

                    await pinpuDevice.SetStartFrequencyAsync(start);
                    await pinpuDevice.SetStopFrequencyAsync(stop);
                    await pinpuDevice.SetCenterFrequencyAsync(center);

                    await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");

                    double markPower = double.NaN;

                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;
                    gain[i] = markPower.ToString();
                }
                //WriteArrayToExcelColumn(freqArray, 1, sheetName);  // A列，从第8行开始
                WriteArrayToExcelColumn(gain, 6, 2, sheetName);  // B列，从第9行开始
                LogToConsole("写入Excel完成");
            }
            catch (Exception ex)
            {
                LogToConsole("镜频测试出错: " + ex.Message);
            }
            finally
            {
                xinhaoDevice.Disconnect(); // 释放资源
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("镜频测试已完成");
            }
        }

        /// <summary>
        /// 发射测试|增益
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void start_fasheceshiVNA_Click(object sender, EventArgs e)
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
            if (string.IsNullOrEmpty(person_textBox.Text))
            {
                MessageBox.Show("请填写测试人员", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始发射测试|增益...");
            WritePersonToAllSheets();
            LoadVNAState(); // 调用矢网文件
            await ChargeSendPowerON(); // 发射加电
            //await SendTestUDP(); //FPGA发包
            //await Task.Delay(500); // 延时保证设备稳定
            await LoadSendTestVNAData(); // 获取矢网数据
        }

        private async Task LoadSendTestVNAData()
        {
            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;
            string sheetName = "测试结果";
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

            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();;
            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            await scpiDevice.SendGainStart();

            await SendTestUDP(); //FPGA发包
            await Task.Delay(500); // 延时保证设备稳定

            string[] gain = await scpiDevice.GetGain_Send();               // 增益（dB）

            if (chasun_checkBox.Checked)
            {
                string[] gainPlusChasun = null;
                string[] gainChasun = null;
                if (ch.Contains("通道1"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 2);
                }
                if (ch.Contains("通道2"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 3);
                }
                if (ch.Contains("通道3"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 4);
                }
                if (ch.Contains("通道4"))
                {
                    gainChasun = ReadChaSunData("Sheet1", 5);
                }

                //int len = gain.Length;

                //// 初始化差值数组
                //gainPlusChasun = new string[len];

                //for (int i = 0; i < len; i++)
                //{
                //    double g = Parse(gain[i]);
                //    double gN = Parse(gainChasun[i]);
                //    gainPlusChasun[i] = (g - gN).ToString();
                //}
                //gain = gainPlusChasun; // 替换原有增益数据
            }

            WriteArrayToExcelColumn(gain, 8, sheetName);

            LogToConsole("数据读取完成");

            LogToConsole("开始写入数据...");

            // 写入频率（从 A8 开始）
            double startFreq = await scpiDevice.GetFreqStart() ?? -1;  // 单位 Hz
            double stopFreq = await scpiDevice.GetFreqStop() ?? -1;    // 单位 Hz
            int _pointCount = await scpiDevice.GetPointCount() ?? -1;


            if (startFreq < 0 || stopFreq < 0 || _pointCount <= 0)
            {
                LogToConsole("获取频率或点数失败，请检查设备连接或设置。");
                scpiDevice.Disconnect();
                return;
            }

            //进度条
            int num = 0;
            progressBar1.Maximum = _pointCount;
            progressBar1.Value = 0;

            double step = (stopFreq - startFreq) / (_pointCount - 1);
            string[] freqArray = new string[_pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                double freqGHz = (startFreq + step * i) / 1e9;
                freqArray[i] = freqGHz.ToString("F6"); // 保留6位小数（GHz）
            }

            WriteArrayToExcelColumn(freqArray, 1, sheetName);  // A列，从第8行开始
            LogToConsole("写入Excel完成");

            // 写入数据库
            try
            {
                for (int i = 0; i < gain.Length; i++)
                {

                    num++;
                    progressBar1.Value += 1;
                    label1.Text = ((double)num / _pointCount * 100).ToString("f2") + "%";
                    label1.Refresh();
                }
            }
            catch (Exception ex)
            {
                LogToConsole("写入数据库出错: " + ex.Message);
            }
            finally
            {
                await scpiDevice.SendCommandAsync(":SENS4:SWE:MODE HOLD");
                scpiDevice.Disconnect(); // 释放资源
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("发射测试|增益已完成");
            }

        }

        private async void start_fasheyixiang_test_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始移相精度测试...");
            WritePersonToAllSheets();
            LoadVNAState(); // 调用矢网文件
            await ChargeSendPowerON(); // 发射加电

            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;
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

            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            await SendTestUDP(0, "移相"); // FPGA发码
            await Task.Delay(1000);           // 等待设备稳定
            await scpiDevice.SetNormalize_Send();
            List<double[]> unwrappedPhases = new List<double[]>();
            double[] previousPhase = null;
            double[] phaseOffset = null;

            for (int i = 1; i <= 63; i++)
            {
                await SendTestUDP(i, "移相"); // FPGA发码
                await Task.Delay(500);           // 等待设备稳定
                await scpiDevice.ScanOnce(4);

                string[] initial = await scpiDevice.GetPhase_Send();    // 初相（°）

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
            }

            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电

            // 写入解包后的初相（第 i + 2 列）
            for (int i = 0; i < unwrappedPhases.Count; i++)
            {
                string[] phaseStrings = unwrappedPhases[i].Select(v => v.ToString()).ToArray();
                WriteArrayToExcelColumn_New(phaseStrings, i + 3, "发射通道相移精度测试结果");
            }
            SubtractStandardAndWriteResult("发射通道相移精度测试结果");
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

        /// <summary>
        /// 衰减误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
            if (!ch1_checkBox.Checked && !ch2_checkBox.Checked && !ch3_checkBox.Checked && !ch4_checkBox.Checked)
            {
                MessageBox.Show("请选择一个通道", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LogToConsole("开始衰减精度测试...");
            WritePersonToAllSheets();
            LoadVNAState(); // 调用矢网文件
            await ChargeRecievePowerON(); // 接收加电

            string ch = "";
            string testType = testType_comboBox.Text;
            string componentName = componentName_textBox.Text;
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

            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("矢网连接失败");
                return;
            }
            await RecieveTestUDP(0, "衰减"); // FPGA发码
            await Task.Delay(1000);           // 等待设备稳定
            await scpiDevice.SetNormalize();

            for (int i = 0; i <= 63; i++)
            {
                await RecieveTestUDP(i, "衰减"); //FPGA发包
                await Task.Delay(1000); // 延时保证设备稳定
                await scpiDevice.ScanOnce(2);
                string[] gain = await scpiDevice.GetGainStringAsync_New();               // 增益（dB）
                string[] initial = await scpiDevice.GetInitialPhaseStringAsync_New();    // 初相（°）

                WriteArrayToExcelColumn_New(gain, i + 2, "接收通道衰减精度测试结果");
                //WriteArrayToExcelColumn_New(initial, i + 2, "接收寄生调相");
            }

            scpiDevice.Disconnect(); // 释放资源
            await CloseFPGA();
            await CloseCharge(); // 电源关电
            SubtractStandardAndWriteResult("接收通道衰减精度测试结果");
            //CalculatePhaseAccuracyAndWriteToExcel("接收通道衰减精度测试结果");
            //CalculatePhaseAccuracyAndWriteToExcel_Jisheng("接收寄生调相");
        }
        /// <summary>
        /// 带外抑制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button3_Click(object sender, EventArgs e)
        {
            ScpiDevice xinhaoDevice = new ScpiDevice();
            ScpiDevice xinhaoBenzhenDevice = new ScpiDevice();
            ScpiDevice pinpuDevice = new ScpiDevice();
            try
            {
                string ch = "";
                string testType = testType_comboBox.Text;
                string componentName = componentName_textBox.Text;
                string sheetName = "测试结果";
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

                string[] dwyz = new string[pointCount];

                bool connected = await xinhaoDevice.ConnectAsync(xinhaoAddress);
                bool connected2 = await xinhaoBenzhenDevice.ConnectAsync(xinhaoBenzhenAddress);
                bool connected3 = await pinpuDevice.ConnectAsync(pinpuAddress);
                if (!connected || !connected2 || !connected3)
                {
                    LogToConsole("设备连接失败");
                    return;
                }
                await ChargeRecievePowerON(); // 接收加电
                await RecieveTestUDP(); //FPGA发包
                await Task.Delay(500); // 延时保证设备稳定

                await xinhaoBenzhenDevice.SetPower(powerBenzhen);
                await xinhaoBenzhenDevice.QueryOpc();


                //进度条
                int num = 0;
                progressBar1.Maximum = pointCount;
                progressBar1.Value = 0;

                double step = (stopFreq - startFreq) / (pointCount - 1);
                string[] freqArray = new string[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    double freqHz = startFreq + step * i;
                    double freqGHz = Math.Round(freqHz / 1e9, 3);
                    freqArray[i] = freqGHz.ToString(); // 保留6位小数（GHz）
                    await xinhaoDevice.SetFrequency(freqHz);
                    await xinhaoDevice.QueryOpc();
                    await xinhaoBenzhenDevice.SetFrequency(freqHz - 175 * 1e6);
                    await xinhaoBenzhenDevice.QueryOpc();

                    double center = 175 * 1e6;
                    double start = 130 * 1e6;
                    double stop = 220 * 1e6;

                    await pinpuDevice.SetStartFrequencyAsync(start);
                    await pinpuDevice.SetStopFrequencyAsync(stop);
                    await pinpuDevice.SetCenterFrequencyAsync(center);

                    await pinpuDevice.SendCommandAsync(":CALC:MARK1:STATE ON");

                    double markPower = double.NaN;

                    await pinpuDevice.SendCommandAsync($":CALC:MARK1:X {center}");
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    markPower = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    await pinpuDevice.SendCommandAsync(":CALC:MARK2:STATE ON");

                    double markPower1 = double.NaN;

                    await pinpuDevice.SendCommandAsync($":CALC:MARK2:X {start}");
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    markPower1 = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    await pinpuDevice.SendCommandAsync(":CALC:MARK3:STATE ON");

                    double markPower3 = double.NaN;

                    await pinpuDevice.SendCommandAsync($":CALC:MARK3:X {stop}");
                    await Task.Delay(200); // 让设备处理
                                           // 读取 Marker 的功率值
                    markPower3 = await pinpuDevice.ReadMarkerPowerAsync() ?? double.NaN;

                    double gainValue = markPower - Math.Max(markPower1, markPower3);

                    dwyz[i] = markPower.ToString();
                }
                WriteArrayToExcelColumn(freqArray, 1, sheetName);  // A列，从第8行开始
                WriteArrayToExcelColumn(dwyz, 7, sheetName);  // B列，从第9行开始
                LogToConsole("写入Excel完成");
            }
            catch (Exception ex)
            {
                LogToConsole("带外抑制测试出错: " + ex.Message);
            }
            finally
            {
                xinhaoDevice.Disconnect(); // 释放资源
                xinhaoBenzhenDevice.Disconnect();
                pinpuDevice.Disconnect();
                await CloseFPGA();
                await CloseCharge(); // 电源关电
                LogToConsole("带外抑制测试已完成");
            }
        }

        private async void button4_Click_1(object sender, EventArgs e)
        {
            string visaAddress = vnaAddress;
            ScpiDevice scpiDevice = new ScpiDevice();

            bool connected = await scpiDevice.ConnectAsync(visaAddress);
            if (!connected)
            {
                LogToConsole("连接失败");
                return;
            }
            GetDeviceFilesJson();
            await scpiDevice.LoadStateFile("0728.csa");
            LogToConsole("调用矢网文件");
            vnaFlag = 1;
            scpiDevice.Disconnect(); // 释放资源
        }
    }
}
