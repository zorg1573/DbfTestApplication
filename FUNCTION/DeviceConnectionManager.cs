using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading.Tasks;
using TestApp.MODEL;
using NationalInstruments.Visa;
using TestApp.FUNCTION;

public class DeviceConnectionManager
{
    private readonly Dictionary<string, object> _connections = new Dictionary<string, object>();

    // 异步连接接口，返回连接成功标志，消息和ScpiDevice实例（如果适用）
    public async Task<(bool success, string message, ScpiDevice device)> ConnectAsync(DeviceInfo device)
    {
        string message = "";
        ScpiDevice scpiDevice = null;

        try
        {
            switch (device.Type)
            {
                case ConnectionType.LAN:
                    // 构造VISA地址
                    string visaAddr = BuildVisaAddress(device.Address);
                    scpiDevice = new ScpiDevice(device);
                    bool connected = await scpiDevice.ConnectAsync(visaAddr);
                    if (!connected)
                    {
                        return (false, "VISA连接失败", null);
                    }
                    string idn = await scpiDevice.GetInfo();
                    if (string.IsNullOrEmpty(idn))
                    {
                        return (false, "连接成功但未能获取设备信息", scpiDevice);
                    }
                    message = "连接成功，设备信息：" + idn;

                    // 保存连接
                    _connections[device.Address] = scpiDevice;
                    break;
                case ConnectionType.GPIB:
                case ConnectionType.USB:
                    // 构造VISA地址
                    string visaAddr2 = BuildVisaAddress(device.Address);
                    scpiDevice = new ScpiDevice(device);
                    bool connected2 = await scpiDevice.ConnectAsync(visaAddr2);
                    if (!connected2)
                    {
                        return (false, "VISA连接失败", null);
                    }
                    string idn2 = await scpiDevice.GetInfo();
                    if (string.IsNullOrEmpty(idn2))
                    {
                        return (false, "连接成功但未能获取设备信息", scpiDevice);
                    }
                    message = "连接成功，设备信息：" + idn2;

                    // 保存连接
                    _connections[device.Address] = scpiDevice;
                    break;

                case ConnectionType.Serial:
                case ConnectionType.Bluetooth:
                    if (ConnectSerial(device, out message))
                    {
                        // 串口连接对象存储在_connections中，设备实例为null
                        return (true, message, null);
                    }
                    else
                    {
                        return (false, message, null);
                    }

                case ConnectionType.CAN:
                    message = "CAN 接口请使用厂商 SDK 实现。";
                    return (false, message, null);

                default:
                    message = "不支持的连接类型";
                    return (false, message, null);
            }

            return (true, message, scpiDevice);
        }
        catch (Exception ex)
        {
            return (false, "连接失败: " + ex.Message, null);
        }
    }

    public void Disconnect(DeviceInfo device)
    {
        if (_connections.TryGetValue(device.Address, out var conn))
        {
            switch (device.Type)
            {
                case ConnectionType.LAN:
                    if (conn is ScpiDevice scpi)
                    {
                        scpi.Disconnect();
                    }
                    break;
                case ConnectionType.GPIB:
                case ConnectionType.USB:
                    if (conn is ScpiDevice scpi2)
                    {
                        scpi2.Disconnect();
                    }
                    break;

                case ConnectionType.Serial:
                case ConnectionType.Bluetooth:
                    if (conn is SerialPort sp)
                    {
                        sp.Close();
                    }
                    break;

                    // TODO: CAN等其他类型断开逻辑
            }

            _connections.Remove(device.Address);
        }
    }

    private bool ConnectSerial(DeviceInfo device, out string message)
    {
        message = "";
        try
        {
            SerialPort port = new SerialPort(device.Address);

            if (device.Parameters.TryGetValue("BaudRate", out string baudStr) && int.TryParse(baudStr, out int baud))
                port.BaudRate = baud;
            else
                port.BaudRate = 9600;

            port.Open();
            _connections[device.Address] = port;
            message = "串口连接成功";
            return true;
        }
        catch (Exception ex)
        {
            message = "串口连接失败: " + ex.Message;
            return false;
        }
    }

    public static string BuildVisaAddress(string ipPort, bool useSocket = true)
    {
        if (string.IsNullOrWhiteSpace(ipPort))
            throw new ArgumentException("IP 地址不能为空");

        // ipPort 可能为 "192.168.1.2" 或 "192.168.1.2:5025"
        string ip;
        int? port = null;
        var parts = ipPort.Split(':');
        if (parts.Length == 1)
        {
            ip = parts[0];
        }
        else if (parts.Length == 2 && int.TryParse(parts[1], out int p))
        {
            ip = parts[0];
            port = p;
        }
        else
        {
            throw new ArgumentException("地址格式无效，应为 IP 或 IP:端口");
        }

        if (port.HasValue && useSocket)
        {
            return $"TCPIP0::{ip}::{port}::SOCKET";
        }
        else
        {
            return $"TCPIP0::{ip}::INSTR";
        }
    }
}
