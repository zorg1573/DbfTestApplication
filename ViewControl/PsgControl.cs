using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TestApp.FUNCTION;

namespace TestApp.ViewControl
{
    public partial class PsgControl : UserControl
    {
        private Timer _timer;
        private Random _rand = new Random();
        private int _timeCounter = 0;
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
                    InitChart();
                    InitTimer();
                }
                else
                {
                    Log("设备未连接，图表未初始化");
                }
            }
        }


        private Form Main => this.FindForm();

        public PsgControl()
        {
            InitializeComponent();
        }
        private void Log(string msg)
        {
            if (Main is Main parent)
            {
                parent.LogToConsole(msg);
            }
        }
        private void InitChart()
        {
            chart1.Series.Clear();

            var frequencySeries = new Series("频率")
            {
                ChartType = SeriesChartType.Line,
                Color = System.Drawing.Color.Red,
                YAxisType = AxisType.Primary  // 左侧主轴
            };

            var powerSeries = new Series("功率")
            {
                ChartType = SeriesChartType.Line,
                Color = System.Drawing.Color.Blue,
                YAxisType = AxisType.Secondary  // 右侧副轴
            };

            chart1.Series.Add(frequencySeries);
            chart1.Series.Add(powerSeries);

            var chartArea = chart1.ChartAreas[0];
            chartArea.AxisX.Title = "时间 (秒)";

            // 左侧Y轴显示频率
            chartArea.AxisY.Title = "频率 (Hz)";
            chartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            // 右侧Y轴显示功率
            chartArea.AxisY2.Title = "功率 (dBm)";
            chartArea.AxisY2.MajorGrid.LineColor = System.Drawing.Color.Transparent; // 避免网格重叠
            chartArea.AxisY2.Enabled = AxisEnabled.True; // 启用右侧Y轴
        }


        private void InitTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer.Dispose();
                _timer = null;
            }

            _timer = new Timer();
            _timer.Interval = 1000; // 1秒更新一次
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }


        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (CurrentDevice == null || !CurrentDevice.IsConnected)
            {
                Log("设备未连接，停止计时器");
                _timer?.Stop();
                return;
            }
            _timeCounter++;

            double freq = await CurrentDevice.ReadFrequency() ?? 0;
            double power = await CurrentDevice.ReadPower() ?? 0;

            var frequencySeries = chart1.Series["频率"];
            var powerSeries = chart1.Series["功率"];

            frequencySeries.Points.AddXY(_timeCounter, freq);
            powerSeries.Points.AddXY(_timeCounter, power);

            if (frequencySeries.Points.Count > 30)
            {
                frequencySeries.Points.RemoveAt(0);
                powerSeries.Points.RemoveAt(0);
            }

            chart1.ChartAreas[0].RecalculateAxesScale();
        }


        private async void start_output_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.EnableOutput();
                Log("输出已开启");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private async void stop_output_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                await CurrentDevice.DisableOutput();
                Log("输出已关闭");
            }
            else
            {
                Log("设备未连接");
            }
        }

        private async void manual_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                double freq = double.Parse(freq_textBox.Text);
                double power = double.Parse(power_textBox.Text);
                await CurrentDevice.SetFrequency(freq);
                await CurrentDevice.SetPower(power);
                Log("执行修改: 频率：" + freq + ", 功率：" + power);
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

                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Tick -= Timer_Tick; // 重要！移除事件处理
                    _timer.Dispose();
                    _timer = null;
                }
                label5.Text = "未连接";
                Log("设备已断开连接");
            }
            else
            {
                Log("设备未连接");
            }
        }


        public void RefreshDevice()
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                if (_timer == null)
                {
                    InitChart();
                    InitTimer();
                }

                Log($"刷新成功：当前设备地址为 {CurrentDevice.Device.Address}");
            }
            else
            {
                _timer?.Stop();
                _timer = null;
                chart1.Series.Clear();
                Log("刷新失败：设备未连接");
            }
        }


    }
}
