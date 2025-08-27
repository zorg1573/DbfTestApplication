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
using DbfTest.FUNCTION;

namespace DbfTest.ViewControl
{
    public partial class ChargeControl : UserControl
    {
        private Form Main => this.FindForm();
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

        public ChargeControl()
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

            var voltageSeries = new Series("电压")
            {
                ChartType = SeriesChartType.Line,
                Color = System.Drawing.Color.Red,
                YAxisType = AxisType.Primary  // 左侧主轴显示电压
            };

            var currentSeries = new Series("电流")
            {
                ChartType = SeriesChartType.Line,
                Color = System.Drawing.Color.Blue,
                YAxisType = AxisType.Secondary  // 右侧副轴显示电流
            };

            chart1.Series.Add(voltageSeries);
            chart1.Series.Add(currentSeries);

            var chartArea = chart1.ChartAreas[0];
            chartArea.AxisX.Title = "时间 (秒)";

            // 左侧Y轴显示电压(V)
            chartArea.AxisY.Title = "电压 (V)";
            chartArea.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            // 右侧Y轴显示电流(A)
            chartArea.AxisY2.Title = "电流 (A)";
            chartArea.AxisY2.MajorGrid.LineColor = System.Drawing.Color.Transparent; // 避免和左轴网格线重叠
            chartArea.AxisY2.Enabled = AxisEnabled.True;
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

            // 模拟数据
            //double voltage = 5 + _rand.NextDouble();   // 5 ~ 6 V
            //double current = 1 + _rand.NextDouble();   // 1 ~ 2 A
            double voltage = await CurrentDevice.ReadVoltage() ?? 0;
            double current = await CurrentDevice.ReadCurrent() ?? 0;


            var voltageSeries = chart1.Series["电压"];
            var currentSeries = chart1.Series["电流"];

            voltageSeries.Points.AddXY(_timeCounter, voltage);
            currentSeries.Points.AddXY(_timeCounter, current);

            // 最多保留最近30个点
            if (voltageSeries.Points.Count > 30)
            {
                voltageSeries.Points.RemoveAt(0);
                currentSeries.Points.RemoveAt(0);
            }

            chart1.ChartAreas[0].RecalculateAxesScale();
        }

        private async void start_output_button_Click(object sender, EventArgs e)
        {
            if (CurrentDevice != null && CurrentDevice.IsConnected)
            {
                string chString = ch_comboBox.Text.Replace("CH", "");
                int ch = int.Parse(chString);
                await CurrentDevice.SelectChannel(ch);
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
                string chString = ch_comboBox.Text.Replace("CH", "");
                int ch = int.Parse(chString);
                await CurrentDevice.SelectChannel(ch);
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
                string chString = ch_comboBox.Text.Replace("CH", "");
                int ch = int.Parse(chString);
                double voltage = double.Parse(voltage_textBox.Text);
                double current = double.Parse(current_textBox.Text);
                await CurrentDevice.SelectChannel(ch);
                await CurrentDevice.SetVoltage(voltage);
                await CurrentDevice.SetCurrent(current);
                Log("执行修改:通道："+ ch_comboBox.Text+", 输出电压："+voltage+", 输出电流："+current);
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
