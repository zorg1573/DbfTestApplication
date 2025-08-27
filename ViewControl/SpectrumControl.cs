using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using DbfTest.FUNCTION;
using DbfTest.MODEL;

namespace DbfTest.ViewControl
{
    public partial class SpectrumControl : UserControl
    {
        private Form Main => this.FindForm();
        private ScpiDevice scpiDevice;
        public ScpiDevice CurrentDevice
        {
            get => scpiDevice; // 不抛异常，返回 null
            set
            {
                scpiDevice = value;

                if (scpiDevice != null)
                {
                    Log("已注入设备：" + scpiDevice?.Device.Address + "，连接状态：" + scpiDevice?.IsConnected);
                }

                if (scpiDevice != null && scpiDevice.IsConnected)
                {
                    label4.Text = scpiDevice.Device.DeviceName;
                    label5.Text = "已连接";
                }
                else
                {
                    Log("设备未连接，图表未初始化");
                }
            }
        }

        public SpectrumControl()
        {
            InitializeComponent();
            LoadSettingsIfExists();
            LoadDataGridViewFromJson();
            //this.Load += SpectrumControl_Load;
        }
/*        private async void SpectrumControl_Load(object sender, EventArgs e)
        {
            await EnsureExcelHeaderAsync();
            if (System.IO.File.Exists(excelPath))
            {
                webBrowser1.Navigate(excelPath);
            }
            else
            {
                MessageBox.Show("文件不存在: " + excelPath);
            }
            
        }*/
#region excel
/*        private async Task EnsureExcelHeaderAsync()
        {
            if (!System.IO.File.Exists(excelPath))
            {
                await Task.Run(() =>
                {
                    Excel.Application excelApp = new Excel.Application();
                    Excel.Workbook workbook = excelApp.Workbooks.Add();
                    Excel.Worksheet worksheet = workbook.Sheets[1];

                    string[] headers = { "序号", "时间戳", "中心频率(GHz)", "起始频率(MHz)", "终止频率(GHz)", "SPAN(GHz)", "RBW", "VBW", "扫描时间(ms)", "峰值频率(Hz)", "峰值功率(dBm)" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1] = headers[i];
                    }

                    workbook.SaveAs(excelPath);
                    workbook.Close(false);
                    excelApp.Quit();

                    Marshal.ReleaseComObject(worksheet);
                    Marshal.ReleaseComObject(workbook);
                    Marshal.ReleaseComObject(excelApp);
                });
            }
        }

        private void WriteToExcel(double centerFreq, double startFreq, double stopFreq, double span, double rbw, double vbw, double sweepTime, double peakFreq, double peakPower)
        {
            
            Excel.Application excelApp = null;
            Excel.Workbook workbook = null;
            Excel.Worksheet worksheet = null;

            try
            {
                excelApp = new Excel.Application();
                workbook = excelApp.Workbooks.Open(excelPath);
                worksheet = workbook.Sheets[1];

                // 查找下一个空行
                int row = worksheet.UsedRange.Rows.Count + 1;

                worksheet.Cells[row, 1] = row - 1; // 序号（减去表头）
                worksheet.Cells[row, 2] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 3] = centerFreq / 1e9;
                worksheet.Cells[row, 4] = startFreq / 1e6;
                worksheet.Cells[row, 5] = stopFreq / 1e9;
                worksheet.Cells[row, 6] = span / 1e9;
                worksheet.Cells[row, 7] = rbw;
                worksheet.Cells[row, 8] = vbw;
                worksheet.Cells[row, 9] = sweepTime;
                worksheet.Cells[row, 10] = peakFreq;
                worksheet.Cells[row, 11] = peakPower;

                workbook.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入Excel失败: " + ex.Message);
            }
            finally
            {
                if (workbook != null)
                {
                    workbook.Close(false);
                    Marshal.ReleaseComObject(workbook);
                }

                if (excelApp != null)
                {
                    excelApp.Quit();
                    Marshal.ReleaseComObject(excelApp);
                }
            }
        }*/
#endregion

        private void Log(string msg)
        {
            if (Main is Main parent)
            {
                parent.LogToConsole(msg);
            }
        }

        private async void set_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.SetCenterFrequencyAsync(double.Parse(centerFreq_textBox.Text) * 1000000000);
                await CurrentDevice.SetSpanAsync(double.Parse(span_textBox.Text) * 1000000000);
                await CurrentDevice.SetStartFrequencyAsync(double.Parse(startFreq_textBox.Text) * 1000000);
                await CurrentDevice.SetStopFrequencyAsync(double.Parse(stopFreq_textBox.Text) * 1000000000);
                await CurrentDevice.SetRBWAsync(double.Parse(rbw_textBox.Text));
                await CurrentDevice.SetVBWAsync(double.Parse(vbw_textBox.Text));
                await CurrentDevice.SetSweepTimeAsync(double.Parse(scanTime_textBox.Text));
                //await CurrentDevice.SetDetectorAsync(jianbo_comboBox.Text);
                await CurrentDevice.SetTriggerSourceAsync(trigger_comboBox.Text);
                Log("配置已修改");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private async void scanStart_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.StartSweepAsync();
                Log("扫描开始...");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private async void getData_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                double freq = await CurrentDevice.ReadMarkerFrequencyAsync() ?? 0;
                double power = await CurrentDevice?.ReadMarkerPowerAsync() ?? 0;
                Log("标记位频率：" + freq + ", 标记位功率：" + power);

                // 从界面读取其他设置参数
                double center = double.Parse(centerFreq_textBox.Text) * 1e9;
                double start = double.Parse(startFreq_textBox.Text) * 1e6;
                double stop = double.Parse(stopFreq_textBox.Text) * 1e9;
                double span = double.Parse(span_textBox.Text) * 1e9;
                double rbw = double.Parse(rbw_textBox.Text);
                double vbw = double.Parse(vbw_textBox.Text);
                double sweep = double.Parse(scanTime_textBox.Text);

                // 新增一行数据到dataGridView1
                int newIndex = dataGridView1.Rows.Count + 1;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                dataGridView1.Rows.Add(newIndex, timestamp, center, start, stop, span, rbw, vbw, sweep, freq, power);

                // 保存到本地JSON文件，路径自己定义
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum_data.json");
                SaveDataGridViewToJson(jsonPath);

                /*                // 先清空嵌入Excel（释放文件占用）
                                webBrowser1.Navigate("about:blank");
                                await Task.Delay(1000); // 等1秒确保释放锁

                                // 写入
                                WriteToExcel(center, start, stop, span, rbw, vbw, sweep, freq, power);

                                // 重新打开以查看最新数据
                                await Task.Delay(1000);
                                webBrowser1.Navigate(excelPath);*/
            }
            else
            {
                Log("设备未连接");
            }
        }


        private async void setMark_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.SetMarkerToMaxAsync();
                Log("标记最大点位");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private void disconnect_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                CurrentDevice.Disconnect();
                label5.Text = "未连接";
                Log("设备已断开连接");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private async void scanOn_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.StartSweepAsync();
                Log("开始扫描...");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private void SaveDataGridViewToJson(string filePath)
        {
            var list = new List<SpectrumData>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue; // 跳过新行

                list.Add(new SpectrumData
                {
                    Index = Convert.ToInt32(row.Cells[0].Value),
                    Timestamp = row.Cells[1].Value.ToString(),
                    CenterFrequencyGHz = Convert.ToDouble(row.Cells[2].Value),
                    StartFrequencyMHz = Convert.ToDouble(row.Cells[3].Value),
                    StopFrequencyGHz = Convert.ToDouble(row.Cells[4].Value),
                    SpanGHz = Convert.ToDouble(row.Cells[5].Value),
                    RBW = Convert.ToDouble(row.Cells[6].Value),
                    VBW = Convert.ToDouble(row.Cells[7].Value),
                    SweepTime = Convert.ToDouble(row.Cells[8].Value),
                    PeakFrequencyHz = Convert.ToDouble(row.Cells[9].Value),
                    PeakPowerdBm = Convert.ToDouble(row.Cells[10].Value),
                });
            }

            string jsonString = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonString);
            Log("参数已保存到 spectrum_settings.json");
        }

        private void save_button_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = new SpectrumData
                {
                    CenterFrequencyGHz = double.Parse(centerFreq_textBox.Text),
                    StartFrequencyMHz = double.Parse(startFreq_textBox.Text),
                    StopFrequencyGHz = double.Parse(stopFreq_textBox.Text),
                    SpanGHz = double.Parse(span_textBox.Text),
                    RBW = double.Parse(rbw_textBox.Text),
                    VBW = double.Parse(vbw_textBox.Text),
                    SweepTime = double.Parse(scanTime_textBox.Text),
                    TriggerSource = trigger_comboBox.Text
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum_settings.json");
                File.WriteAllText(path, json);
                Log("参数已保存到 spectrum_settings.json");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存参数失败: " + ex.Message);
            }
        }

        private void LoadSettingsIfExists()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum_settings.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<SpectrumData>(json);

                    if (settings != null)
                    {
                        centerFreq_textBox.Text = settings.CenterFrequencyGHz.ToString();
                        startFreq_textBox.Text = settings.StartFrequencyMHz.ToString();
                        stopFreq_textBox.Text = settings.StopFrequencyGHz.ToString();
                        span_textBox.Text = settings.SpanGHz.ToString();
                        rbw_textBox.Text = settings.RBW.ToString();
                        vbw_textBox.Text = settings.VBW.ToString();
                        scanTime_textBox.Text = settings.SweepTime.ToString();
                        trigger_comboBox.Text = settings.TriggerSource;
                    }

                    Log("已加载上次保存的参数");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("读取参数失败: " + ex.Message);
                }
            }
        }

        private void LoadDataGridViewFromJson()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum_data.json");
            if (!File.Exists(filePath))
            {
                Log($"文件不存在: {filePath}");
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<SpectrumData>>(jsonString);

                dataGridView1.Rows.Clear();

                if (list != null)
                {
                    foreach (var item in list)
                    {
                        dataGridView1.Rows.Add(
                            item.Index,
                            item.Timestamp,
                            item.CenterFrequencyGHz,
                            item.StartFrequencyMHz,
                            item.StopFrequencyGHz,
                            item.SpanGHz,
                            item.RBW,
                            item.VBW,
                            item.SweepTime,
                            item.PeakFrequencyHz,
                            item.PeakPowerdBm
                        );
                    }
                    Log($"成功加载 {list.Count} 条数据");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取 JSON 失败: " + ex.Message);
            }
        }

        private async void scanOnce_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.StartSingleSweepAsync();
                Log("单次扫描开始...");
            }
            else
            {
                Log("设备未连接");
            }
        }
    }
}
