using System;
using System.Linq;
using DbfTest.MODEL;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace DbfTest.FUNCTION
{
    /// <summary>
    /// 温度回传 UDP 监听器。
    ///
    /// 下位机（FPGA）会把温度回传帧以 UDP 持续发回上位机；本类用 SharpPcap 在指定网卡上
    /// 抓包、提取 UDP 负载并交给 <see cref="TemperatureReturnParser"/> 解析，解析成功后通过
    /// <see cref="FrameReceived"/> 事件抛出。
    ///
    /// 注意：发送路径（Main_Form/ManualSend_Form）会在 CaptureDeviceList.Instance 的单例设备上
    /// 反复 Open/Close。为避免与发送相互干扰，这里用 LibPcapLiveDeviceList.New() 取一个独立句柄
    /// 长期打开用于抓包（Npcap 允许同一网卡多个句柄）。
    /// </summary>
    public class TemperatureReturnReceiver : IDisposable
    {
        private readonly object _sync = new object();
        private LibPcapLiveDeviceList _ownedList;
        private LibPcapLiveDevice _device;

        /// <summary>解析到一帧有效温度回传数据时触发（在抓包线程上调用）。</summary>
        public event Action<TemperatureReturnFrame> FrameReceived;

        /// <summary>状态/错误日志输出。</summary>
        public event Action<string> Log;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// 启动监听。
        /// </summary>
        /// <param name="ifaceName">网卡接口名（与发送使用的 \Device\NPF_{GUID} 一致）。</param>
        /// <param name="fpgaIp">下位机（回传来源）IP，用于 BPF 过滤；为空时退化为按端口过滤。</param>
        /// <param name="udpPort">UDP 端口，默认 8080（0x1F90）。</param>
        public void Start(string ifaceName, string fpgaIp, ushort udpPort = 8080)
        {
            lock (_sync)
            {
                if (IsRunning)
                    return;

                if (string.IsNullOrWhiteSpace(ifaceName))
                {
                    Log?.Invoke("温度回传监听未启动：未配置网卡接口（请在设备地址设置中配置 PC 接口）。");
                    return;
                }

                try
                {
                    _ownedList = LibPcapLiveDeviceList.New();
                    _device = _ownedList.FirstOrDefault(d => d.Name == ifaceName);
                    if (_device == null)
                    {
                        Log?.Invoke("温度回传监听未启动：找不到网卡接口 " + ifaceName);
                        Cleanup();
                        return;
                    }

                    _device.OnPacketArrival += OnPacketArrival;
                    _device.Open(DeviceModes.Promiscuous, 1000);
                    _device.Filter = BuildFilter(fpgaIp, udpPort);
                    _device.StartCapture();

                    IsRunning = true;
                    Log?.Invoke($"温度回传监听已启动（接口 {ifaceName}，过滤 {_device.Filter}）。");
                }
                catch (Exception ex)
                {
                    Log?.Invoke("温度回传监听启动失败：" + ex.Message);
                    Cleanup();
                }
            }
        }

        private static string BuildFilter(string fpgaIp, ushort udpPort)
        {
            return string.IsNullOrWhiteSpace(fpgaIp)
                ? $"udp and port {udpPort}"
                : $"udp and src host {fpgaIp}";
        }

        private void OnPacketArrival(object sender, PacketCapture e)
        {
            try
            {
                RawCapture raw = e.GetPacket();
                Packet packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);
                UdpPacket udp = packet.Extract<UdpPacket>();
                byte[] payload = udp?.PayloadData;
                if (payload == null)
                    return;

                if (TemperatureReturnParser.TryParse(payload, out TemperatureReturnFrame frame))
                {
                    FrameReceived?.Invoke(frame);
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke("温度回传解析异常：" + ex.Message);
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (!IsRunning && _device == null)
                    return;

                Cleanup();
                Log?.Invoke("温度回传监听已停止。");
            }
        }

        private void Cleanup()
        {
            try
            {
                if (_device != null)
                {
                    _device.OnPacketArrival -= OnPacketArrival;
                    if (_device.Started)
                        _device.StopCapture();
                    if (_device.Opened)
                        _device.Close();
                }
            }
            catch
            {
                // 释放阶段忽略异常
            }
            finally
            {
                _device = null;
                _ownedList = null;
                IsRunning = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
