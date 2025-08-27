using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json; // .NET Core / .NET 5+ 推荐用System.Text.Json
using System.Windows.Forms;
using DbfTest.MODEL;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using NationalInstruments.Visa;
using System.Collections.Concurrent;

namespace DbfTest.FUNCTION
{
    public class LanScanFunc
    {
        public void SaveConfig(LanScanConfig config)
        {
            try
            {
                string configPath = Path.Combine(Application.StartupPath, "LanScanConfig.json");

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                MessageBox.Show("保存成功");
                //MessageBox.Show($"配置已保存到：\n{configPath}", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存配置失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        public LanScanConfig LoadConfig()
        {
            try
            {
                if (!File.Exists("LanScanConfig.json"))
                    return null;

                string json = File.ReadAllText("LanScanConfig.json");
                return JsonSerializer.Deserialize<LanScanConfig>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载配置失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private List<string> GetIpRange(string startIP, string endIP)
        {
            var start = IPAddress.Parse(startIP).GetAddressBytes();
            var end = IPAddress.Parse(endIP).GetAddressBytes();
            var list = new List<string>();

            for (var a = start[0]; a <= end[0]; a++)
                for (var b = start[1]; b <= end[1]; b++)
                    for (var c = start[2]; c <= end[2]; c++)
                        for (var d = start[3]; d <= end[3]; d++)
                            list.Add($"{a}.{b}.{c}.{d}");

            return list;
        }


        public async Task<List<DeviceInfo>> ScanLanDevicesWithVisaAsync()
        {
            var config = LoadConfig();
            var ipList = GetIpRange(config.StartIP, config.EndIP);

            var found = new ConcurrentBag<DeviceInfo>(); // ✅ 线程安全集合
            var semaphore = new SemaphoreSlim(config.MaxConcurrent);

            var tasks = ipList.Select(async ip =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string resourceString = $"TCPIP0::{ip}::INSTR";

                    using (var rm = new ResourceManager())
                    using (var session = rm.Open(resourceString) as MessageBasedSession)
                    {
                        session.TimeoutMilliseconds = config.Timeout;

                        if (config.QueryIdn)
                        {
                            session.RawIO.Write("*IDN?\n");
                            string response = session.RawIO.ReadString();
                            found.Add(new DeviceInfo { DeviceName = response.Trim(), Address = ip });
                        }
                        else
                        {
                            found.Add(new DeviceInfo { DeviceName = ip, Address = ip });
                        }
                    }
                }
                catch
                {
                    // 忽略连接错误
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return found.ToList(); // ✅ 最后统一转为 List
        }

        /*        public async Task<List<DeviceInfo>> ScanLanDevicesWithVisaAsync()
                {
                    var config = LoadConfig();
                    var found = new List<DeviceInfo>();
                    var ipList = GetIpRange(config.StartIP, config.EndIP);
                    var semaphore = new SemaphoreSlim(config.MaxConcurrent);

                    var tasks = ipList.Select(async ip =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            string resourceString = $"TCPIP0::{ip}::INSTR";

                            using (var rm = new ResourceManager())
                            using (var session = rm.Open(resourceString) as MessageBasedSession)
                            {
                                // 设置超时时间，单位毫秒
                                session.TimeoutMilliseconds = config.Timeout;



                                if (config.QueryIdn)
                                {
                                    session.RawIO.Write("*IDN?\n");
                                    string response = session.RawIO.ReadString();
                                    found.Add(new DeviceInfo { DeviceName = response.Trim(), Address = $"{ip}" });
                                }
                                else
                                {
                                    found.Add(new DeviceInfo { DeviceName = ip, Address = $"{ip}" });
                                }
                            }
                        }
                        catch
                        {
                            // 连接失败，忽略
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    await Task.WhenAll(tasks);
                    return found;
                }*/

    }
}
