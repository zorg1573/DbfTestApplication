using System;
using Ivi.Driver;
using Keysight.KtNA; // 假设已引用 Keysight.KtNA.Interop

/// <summary>
/// 封装 Keysight KtNA 驱动常用测量功能的助手类。
/// </summary>
public class KtNAController : IDisposable
{
    private KtNA driver;
    private string channelName = "Channel1";

    /// <summary>
    /// 初始化并连接到仪器。
    /// </summary>
    /// <param name="resourceName">VISA 地址或别名</param>
    /// <param name="simulate">是否启用仿真模式</param>
    public KtNAController(string resourceName, bool simulate = true)
    {
        var options = $"QueryInstrStatus=true, Simulate={(simulate ? "true" : "false")}, DriverSetup= ";
        driver = new KtNA(resourceName, idQuery: true, reset: true, options: options);

        Console.WriteLine("Driver Initialized:");
        Console.WriteLine($" Model: {driver.Identity.InstrumentModel}");
        Console.WriteLine($" Firmware: {driver.Identity.InstrumentFirmwareRevision}");
        Console.WriteLine($" Simulate: {driver.DriverOperation.Simulate}");
    }

    /// <summary>
    /// 添加一个测量，并将其显示在指定窗口中。
    /// </summary>
    /// <param name="measName">测量名称（如 "Measurement1"）</param>
    /// <param name="measNumber">测量编号</param>
    /// <param name="window">显示窗口编号，默认为 1</param>
    public void AddMeasurement(string measName, int measNumber, int window = 1)
    {
        driver.Channels.AddMeasurement(measName, measNumber, 1);
        driver.Display.Windows.CreateOrDelete(true, window);
        driver.Display.Windows[$"Window{window}"].Traces.FeedMeasurementNumber(measNumber, measNumber);
    }

    /// <summary>
    /// 设置频率范围。
    /// </summary>
    /// <param name="startHz">起始频率（Hz）</param>
    /// <param name="stopHz">终止频率（Hz）</param>
    public void SetFrequency(double startHz, double stopHz)
    {
        var stim = driver.Channels[channelName].StandardStimulus.Sweep.Frequency;
        stim.Start = startHz;
        stim.Stop = stopHz;
    }

    /// <summary>
    /// 触发一次扫描并等待完成。
    /// </summary>
    public void TriggerAndWait()
    {
        driver.Channels[channelName].StandardStimulus.Sweep.TriggerMode = SweepTriggerMode.Single;
        driver.System.WaitForOperationComplete(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// 获取指定测量的格式化测量数据。
    /// </summary>
    /// <param name="measName">测量名称</param>
    /// <returns>格式化数据数组</returns>
    public double[] GetMeasurementData(string measName)
    {
        return driver.Channels[channelName].Measurements[measName].QueryData(MeasurementDataType.FormattedMeasData);
    }

    /// <summary>
    /// 设置并读取指定 X 位置的标记值。
    /// </summary>
    /// <param name="measName">测量名称</param>
    /// <param name="xHz">频率位置（Hz）</param>
    /// <returns>标记位置 (X, Y)</returns>
    public (double X, double Y) GetMarkerValue(string measName, double xHz)
    {
        var marker = driver.Channels[channelName].Measurements[measName].Marker;
        marker.SetEnabled(true, 1);
        marker.ActiveMarker = 1;
        marker.XPosition = xHz;
        double y = marker.GetYPosition();
        return (marker.XPosition, y);
    }

    /// <summary>
    /// 检查并输出仪器错误信息。
    /// </summary>
    public void CheckErrors()
    {
        Console.WriteLine("\nChecking for instrument errors...");
        ErrorQueryResult result;
        do
        {
            result = driver.Utility.ErrorQuery();
            Console.WriteLine($"Error: {result.Code}, {result.Message}");
        } while (result.Code != 0);
    }

    /// <summary>
    /// 释放资源并关闭驱动连接。
    /// </summary>
    public void Dispose()
    {
        driver?.Close();
    }
}
