using Excel;
using Ivi.Visa;
using Keysight.KtNA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using DbfTest.MODEL;
using System.Net.Sockets;

namespace DbfTest.FUNCTION
{
/*    
      电源1        keysight    E36312A     TCPIP0::192.168.0.100::inst0::INSTR ch1 5V 0.5A
      电源2        keysight    E36312A     TCPIP0::192.168.0.105::inst0::INSTR ch1 28V 2A ch2 5V 0.5A
      频谱         ceyear      4052H       TCPIP0::192.168.0.106::inst0::INSTR
      矢网         keysight    N5244B      TCPIP0::192.168.0.101::inst0::INSTR
      信号发生器    玖锦        ASG3000B    TCPIP0::192.168.0.102::5010::SOCKET
      功率计       ceyear      87234D	   USB0::0x3399::0x3800::QZMD000089::INSTR
*/
    public class ScpiDevice
    {
        private IMessageBasedSession _visaSession;
        private DeviceInfo _device;

        public ScpiDevice(DeviceInfo device)
        {
            _device = device;
        }

        public ScpiDevice() { }

        public bool IsConnected => _visaSession != null;

        // 新增：公开设备信息属性
        public DeviceInfo Device => _device;

        // 新增：方便直接获取地址
        public string Address => _device?.Address;


        public async Task<bool> ConnectAsync(string resourceString)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var rm = new NationalInstruments.Visa.ResourceManager();
                    _visaSession = (IMessageBasedSession)rm.Open(resourceString);

                    _visaSession.TerminationCharacter = (byte)'\n';
                    _visaSession.TerminationCharacterEnabled = true;

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"VISA连接失败: {ex.Message}");
                    return false;
                }
            });
        }



        public async Task<string> QueryAsync(string command)
        {
            return await Task.Run(() =>
            {
                if (!IsConnected) return null;
                try
                {
                    _visaSession.FormattedIO.WriteLine(command);
                    return _visaSession.FormattedIO.ReadLine();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("查询失败，请检查设备连接或命令格式。" + ex);
                    return null;
                }
            });
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            return await Task.Run(() =>
            {
                if (!IsConnected) return false;
                try
                {
                    _visaSession.FormattedIO.WriteLine(command);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public void Disconnect()
        {
            try
            {
                _visaSession?.Dispose();
                _visaSession = null;
            }
            catch { }
        }

        public static async Task<string> IdentifyAsync(string resourceString)
        {
            var device = new ScpiDevice();
            bool connected = await device.ConnectAsync(resourceString);
            if (!connected) return null;

            string idn = await device.QueryAsync("*IDN?");
            device.Disconnect();
            return idn;
        }

        #region 通用命令
        public async Task SelectChannel(int ch) => await SendCommandAsync($":INST:NSEL {ch}");
        public async Task<string> GetInfo() => await QueryAsync("*IDN?");
        public async Task ClearInfo() => await SendCommandAsync("*CLS");
        public async Task SetVoltage(double voltage) => await SendCommandAsync($":VOLT {voltage}");
        public async Task SetCurrent(double current) => await SendCommandAsync($":CURR {current}");
        public async Task SetFrequency(double freq) => await SendCommandAsync($":FREQ {freq}");
        public async Task SetPower(double power) => await SendCommandAsync($":POW {power}");
        public async Task EnableOutput() => await SendCommandAsync(":OUTP ON");
        public async Task DisableOutput() => await SendCommandAsync(":OUTP OFF");
        public async Task ModON() => await SendCommandAsync(":OUTP:MOD ON");
        public async Task ModOFF() => await SendCommandAsync(":OUTP:MOD OFF");

        #region 信号源（_xinhaoAddress）专用：AMPL / RF:STAT
        /// <summary>设置功率（dBm）。指令：AMPL &lt;amplitude&gt;，范围约 -120～+20/+18 dBm。</summary>
        public async Task SetAmplitude(double amplitudeDb) => await SendCommandAsync($"AMPL {amplitudeDb}");
        public async Task SetAmplitudeMaximum() => await SendCommandAsync("AMPL MAX");
        public async Task SetAmplitudeMinimum() => await SendCommandAsync("AMPL MIN");
        public async Task AmplitudeUp() => await SendCommandAsync("AMPL UP");
        public async Task AmplitudeDown() => await SendCommandAsync("AMPL DOWN");
        public async Task<double?> ReadAmplitude()
        {
            string resp = await QueryAsync("AMPL?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        /// <summary>使能 RF 输出。指令：:RF:STAT 1</summary>
        public async Task EnableRfOutput() => await SendCommandAsync(":RF:STAT 1");
        /// <summary>关闭 RF 输出。指令：:RF:STAT 0</summary>
        public async Task DisableRfOutput() => await SendCommandAsync(":RF:STAT 0");
        public async Task SetRfState(bool enable) => await SendCommandAsync($":RF:STAT {(enable ? 1 : 0)}");
        public async Task<bool?> QueryRfState()
        {
            string resp = (await QueryAsync(":RF:STAT?"))?.Trim();
            if (string.IsNullOrEmpty(resp))
                return null;
            if (resp == "1" || resp.Equals("ON", StringComparison.OrdinalIgnoreCase))
                return true;
            if (resp == "0" || resp.Equals("OFF", StringComparison.OrdinalIgnoreCase))
                return false;
            return null;
        }
        #endregion

        public async Task QueryOpc()
        {
            // 发出操作完成查询命令，直到设备响应
            string result = await QueryAsync("*OPC?");
            if (string.IsNullOrWhiteSpace(result) || !result.Trim().Equals("1"))
                throw new Exception("设备未返回 *OPC 完成标志");
        }

        public async Task<double?> ReadVoltage()
        {
            string resp = await QueryAsync("MEAS:VOLT?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        public async Task<double?> ReadCurrent()
        {
            string resp = await QueryAsync("MEAS:CURR?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        public async Task<double?> ReadFrequency()
        {
            string resp = await QueryAsync("FREQ?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        public async Task<double?> ReadPower()
        {
            string resp = await QueryAsync("POW?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }
        #endregion

        #region 频谱
        // 设置频谱分析相关参数
        //public async Task SetCenterFrequencyAsync(double freqHz) => await SendCommandAsync($"FREQ:CENT {freqHz}");
        public async Task SetCenterFrequencyAsync(double freqHz) => await SendCommandAsync($":SENS:FREQ:CENT {freqHz}");
        public async Task<double?> GetCenterFrequencyAsync()
        {
            string resp = await QueryAsync(":SENS:FREQ:CENT?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task SetSpanAsync(double spanHz) => await SendCommandAsync($"FREQ:SPAN {spanHz}");
        public async Task SetStartFrequencyAsync(double startHz) => await SendCommandAsync($"FREQ:STAR {startHz}");
        public async Task SetStopFrequencyAsync(double stopHz) => await SendCommandAsync($"FREQ:STOP {stopHz}");

        public async Task SetRBWAsync(double rbwHz) => await SendCommandAsync($"BAND:RES {rbwHz}");
        public async Task SetVBWAsync(double vbwHz) => await SendCommandAsync($"BAND:VID {vbwHz}");

        public async Task SetSweepTimeAsync(double timeSeconds) => await SendCommandAsync($"SWE:TIME {timeSeconds}");

        // 设置检波器：如 "NORM", "AVER", "POS", "NEG", "SAMP", "RMS"
        public async Task SetDetectorAsync(string mode) => await SendCommandAsync($":DET {mode}");

        // 设置触发源：如 "IMM", "EXT", "VID", "LINE"
        public async Task SetTriggerSourceAsync(string source) => await SendCommandAsync($"TRIG:SOUR {source}");

        // 启动单次扫描并等待完成
        public async Task StartSingleSweepAsync() => await SendCommandAsync(":INIT:CONT OFF; INIT; *WAI");
        // 连续扫描
        public async Task StartSweepAsync() => await SendCommandAsync(":INIT:CONT ON");
        // 标记相关功能
        public async Task SetMarkerToMaxAsync() => await SendCommandAsync(":CALC:MARK1:MAX");
        public async Task EnableMarkerAsync() => await SendCommandAsync(":CALC:MARK1:STATE ON");
        public async Task SetMarkerAsync(double frequency) => await SendCommandAsync($":CALC:MARK1:X {frequency}");

        public async Task<double?> ReadMarkerFrequencyAsync()
        {
            string resp = await QueryAsync(":CALC:MARK1:X?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        public async Task<double?> ReadMarkerPowerAsync()
        {
            //string resp = await QueryAsync(":CALC:MARK1:Y?");
            string resp = await QueryAsync(":CALC:MARK:Y?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task<double?> ReadMarkerPowerAsync(int index)
        {
            //string resp = await QueryAsync(":CALC:MARK1:Y?");
            string resp = await QueryAsync($":CALC:MARK{index}:Y?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task<double?> ReadPowerAtFrequencyAsync(double freqHz)
        {
            await SendCommandAsync(":CALC:MARK1:STATE ON");
            await SendCommandAsync($":CALC:MARK1:X {freqHz}");
            string result = await QueryAsync(":CALC:MARK1:Y?");
            if (double.TryParse(result, out double value))
                return value;
            return null;
        }
        #region 相位噪声测量

        // 设置仪器进入相位噪声测量模式
        public async Task EnterPhaseNoiseModeAsync() => await SendCommandAsync(":INST PNOISE");

        // 启用相位噪声测量功能
        public async Task EnablePhaseNoiseMeasurementAsync() => await SendCommandAsync(":CONF:LPL");

        // 设置载波频率
        public async Task SetCarrierFrequencyAsync(double freqHz) => await SendCommandAsync($":FREQ:CARR {freqHz}");

        // 设置频偏范围
        public async Task SetOffsetStartAsync(double startHz) => await SendCommandAsync($":LPL:FREQ:OFFS:STAR {startHz}");
        public async Task SetOffsetStopAsync(double stopHz) => await SendCommandAsync($":LPL:FREQ:OFFS:STOP {stopHz}");

        // 设置平均次数与开关
        public async Task SetAverageCountAsync(int count) => await SendCommandAsync($":LPL:AVER:COUN {count}");
        public async Task EnableAveragingAsync(bool enable) => await SendCommandAsync($":LPL:AVER:STAT {(enable ? "ON" : "OFF")}");

        // 设置平滑度
        public async Task SetSmoothingAsync(double factor) => await SendCommandAsync($":LPL:SMO {factor}");

        // 设置测量方法（PN: 相位噪声，DANL: 本底噪声）
        public async Task SetMeasurementMethodAsync(string method) => await SendCommandAsync($":LPL:METH {method}");

        // 启动单次测量
        public async Task StartSinglePhaseNoiseMeasurementAsync() => await SendCommandAsync(":INIT:CONT OFF; INIT; *WAI");

        // 启动连续测量
        public async Task StartContinuousPhaseNoiseMeasurementAsync() => await SendCommandAsync(":INIT:CONT ON");

        // 重新启动测量
        public async Task RestartMeasurementAsync() => await SendCommandAsync(":INIT:REST");

        // 暂停测量
        public async Task PauseMeasurementAsync() => await SendCommandAsync(":INIT:PAUS");

        // 获取基础测量结果（载波功率、频率、抖动等）
        public async Task<string> FetchBasicPhaseNoiseResultAsync() => await QueryAsync(":FETC:LPL1?");

        // 获取轨迹点数信息
        public async Task<string> FetchTracePointsInfoAsync() => await QueryAsync(":FETC:LPL2?");

        // 获取第一条轨迹数据（频偏+相噪）
        public async Task<string> FetchTrace1DataAsync() => await QueryAsync(":FETC:LPL3?");

        // 获取第二、三条轨迹数据
        public async Task<string> FetchTrace2DataAsync() => await QueryAsync(":FETC:LPL4?");
        public async Task<string> FetchTrace3DataAsync() => await QueryAsync(":FETC:LPL5?");

        // 获取三条轨迹汇总数据
        public async Task<string> FetchAllTracesDataAsync() => await QueryAsync(":FETC:LPL6?");

        // 清除所有标记
        public async Task ClearAllMarkersAsync() => await SendCommandAsync(":CALC:LPL:MARK:AOFF");

        #endregion

        #region 噪声系数测量（思仪 3986）

        // 设置仪器进入噪声系数测量模式
        public async Task EnterNoiseFigureModeAsync() => await SendCommandAsync(":INST:SEL NFIGURE");

        // 加载噪声系数测量预设状态文件
        public async Task LoadPinpuStateAsync(string filePath) => await SendCommandAsync($":MMEMory:LOAD:STATe \"{filePath}\"");

        // 设置是否连续测量
        public async Task SetContinuousMeasurementAsync(bool enable) =>
            await SendCommandAsync($":INIT:CONT {(enable ? "ON" : "OFF")}");

        // 手动触发一次噪声系数测量
        public async Task TriggerSingleMeasurementAsync() => await SendCommandAsync(":INIT:REST");

        // 查询仪器是否完成测量
        public async Task<bool> QueryOperationCompleteAsync()
        {
            var response = await QueryAsync("*OPC?");
            return !string.IsNullOrWhiteSpace(response) && response.Trim() == "1";
        }

        /// <summary>
        /// 按 3986 手册 4.1.7 示例：先指定轨迹为噪声系数，再单次扫描并读取校正 NF。
        /// 步骤：:DISP:DATA:TRAC1 NFIG → :INIT:CONT OFF → :INIT:REST → :FETC:CORR:NFIG? DB
        /// </summary>
        public async Task PrepareNoiseFigureTraceAsync()
        {
            await SendCommandAsync(":DISP:DATA:TRAC1 NFIG");
            await SendCommandAsync(":INIT:CONT OFF");
            await SendCommandAsync(":INIT:REST");
            await QueryAsync("*OPC?");
        }

        /// <summary>
        /// 扫频校正噪声系数数组（仅 NF 值）。手册：:FETCh:CORRected:NFIGure? DB
        /// </summary>
        public async Task<string> FetchCorrectedNoiseFigureAsync() =>
            await QueryAsync(":FETC:CORR:NFIG? DB");

        /// <summary>
        /// 扫频校正频率+噪声系数数据对。手册：:FETCh:CORRected:NFIGure:DATA? DB
        /// 返回格式：freq,nf,freq,nf,...
        /// </summary>
        public async Task<string> FetchCorrectedNoiseFigureDataAsync() =>
            await QueryAsync(":FETC:CORR:NFIG:DATA? DB");

        /// <summary>
        /// 定点校正噪声系数。手册：:FETCh:SCALar:CORRected:NFIGure? DB
        /// </summary>
        public async Task<string> FetchCorrectedNoiseFigureScalarAsync() =>
            await QueryAsync(":FETC:SCAL:CORR:NFIG? DB");

        /// <summary>
        /// 查询已修正噪声系数迹线在指定频率点的幅度。手册：:TRAC:CORR:AMPL:VAL? NFIG,&lt;freq&gt;
        /// </summary>
        public async Task<string> FetchCorrectedNoiseFigureAtFreqAsync(double freqHz) =>
            await QueryAsync($":TRAC:CORR:AMPL:VAL? NFIG,{freqHz}");

        private static bool IsNoiseFigureResponseValid(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return false;
            string[] parts = data.Split(',');
            foreach (string p in parts)
            {
                if (!double.TryParse(p.Trim(), out double v))
                    continue;
                if (double.IsNaN(v) || Math.Abs(v) < 1e-30)
                    continue;
                if (Math.Abs(v - 9.9099995E+37) < 1e30)
                    continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 从 :FETC:CORR:NFIG:DATA? 的 freq,nf 成对数据中提取噪声系数数组。
        /// </summary>
        private static string[] ParseNoiseFigureDataPairs(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;
            string[] parts = data.Split(',');
            if (parts.Length < 2)
                return null;

            // 成对：偶数下标频率，奇数下标 NF；若长度为奇数则按纯 NF 数组处理
            if (parts.Length % 2 == 0 &&
                double.TryParse(parts[0].Trim(), out double first) &&
                first > 1e6) // 首值像频率(Hz)则按数据对解析
            {
                var nfs = new List<string>();
                for (int i = 1; i < parts.Length; i += 2)
                    nfs.Add(parts[i].Trim());
                return nfs.ToArray();
            }

            return parts.Select(p => p.Trim()).ToArray();
        }

        /// <summary>
        /// 触发并读取扫频校正噪声系数（优先 DATA 对，再纯 NF 数组）。
        /// </summary>
        public async Task<string[]> GetZaoshengData()
        {
            await PrepareNoiseFigureTraceAsync();

            const int maxTry = 20;
            for (int n = 1; n <= maxTry; n++)
            {
                // 手册推荐：表格/轨迹对应数据对
                string dataPairs = await FetchCorrectedNoiseFigureDataAsync();
                string[] values = null;
                if (IsNoiseFigureResponseValid(dataPairs))
                    values = ParseNoiseFigureDataPairs(dataPairs);

                if (values == null || !values.Any(s => IsNoiseFigureResponseValid(s)))
                {
                    string data = await FetchCorrectedNoiseFigureAsync();
                    if (IsNoiseFigureResponseValid(data))
                        values = data.Split(',').Select(p => p.Trim()).ToArray();
                }

                if (values != null && values.Length > 0 &&
                    values.Any(s => double.TryParse(s, out double v) && Math.Abs(v) > 1e-30))
                {
                    return values.Select(s =>
                    {
                        if (double.TryParse(s, out double v) &&
                            Math.Abs(v) > 1e-30 &&
                            Math.Abs(v - 9.9099995E+37) > 1e30)
                            return v.ToString();
                        return "";
                    }).ToArray();
                }

                await SendCommandAsync(":INIT:REST");
                await QueryAsync("*OPC?");
                await Task.Delay(1000);
            }

            return null;
        }

        /// <summary>
        /// 单次扫描后读取校正噪声系数（与 GetZaoshengData 相同流程，兼容旧调用）。
        /// </summary>
        public async Task<string[]> GetZaoshengSinglePointAsync() => await GetZaoshengData();

        /// <summary>
        /// 读取指定频率点的校正噪声系数。手册：:TRAC:CORR:AMPL:VAL? NFIG,&lt;freq&gt;
        /// </summary>
        public async Task<double> GetZaoshengPointAsync(double freq, int pointIndex)
        {
            await PrepareNoiseFigureTraceAsync();

            const int maxTries = 10;
            for (int attempt = 1; attempt <= maxTries; attempt++)
            {
                string raw = await FetchCorrectedNoiseFigureAtFreqAsync(freq);
                if (double.TryParse(raw?.Trim(), out double val) &&
                    !double.IsNaN(val) &&
                    Math.Abs(val) > 1e-30 &&
                    Math.Abs(val - 9.9099995E+37) > 1e30)
                    return val;

                // 回退：读整段数组取索引
                string[] arr = await GetZaoshengData();
                if (arr != null && pointIndex >= 0 && pointIndex < arr.Length &&
                    double.TryParse(arr[pointIndex], out double v) && Math.Abs(v) > 1e-30)
                    return v;

                await Task.Delay(500);
            }

            return double.NaN;
        }
        #endregion
        #endregion
        #region 三阶交调
        public async Task<double?> GetIP3()
        {
            string resp = await QueryAsync(":FETC:TOI:IP3?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        #endregion


        #region 矢网
        // 设置测量通道并选择参数（如 S12）
        public async Task<bool> SelectSParameterAsync(string name, string sparam)
        {
            await SendCommandAsync($"CALC:PAR:DEF '{name}', {sparam}");
            return await SendCommandAsync($"CALC:PAR:SEL '{name}'");
        }

        // 读取某格式下的 S 参数数据（返回第一点，或者你可以扩展为返回数组）:SENS:SWE:MODE SINGle
        public async Task ScanOnce()
        {
            //await SendCommandAsync(":SENS:SWE:MODE SINGle");
            await SendCommandAsync(":INIT:CONT OFF");
            await SendCommandAsync(":INIT:IMM; *WAI");
        }
        public async Task ScanOnce(int channel)
        {
            await SendCommandAsync($":SENS{channel}:SWE:MODE SINGle"); 
        }
        public async Task ScanOnce0()
        {
            await SendCommandAsync($":SENS:SWE:MODE SINGle");
        }
        public async Task SendGainStart()
        {
            await SendCommandAsync(":SENS4:SWE:MODE CONTinuous");
            await SendCommandAsync(":TRIG:SEQ:SOUR IMMediate");
        }
        public async Task SendsjjtStart()
        {
            await SendCommandAsync(":SENS5:SWE:MODE CONTinuous");
            //await SendCommandAsync(":TRIG:SEQ:SOUR IMMediate");
        }
        public async Task ScanStart()
        {
            await SendCommandAsync("INIT:CONT ON");
        }
        public async Task SetPower(double power,int portNum)
        {
            await SendCommandAsync($"SOUR:POW{portNum} {power}");
        }
        public async Task SetNormalize()
        {
            // 触发单次测量
            await ScanOnce(2);
            await Task.Delay(1000);
            // 选中 Trace 3 再 normalize
            await SendCommandAsync(":CALC2:PAR:SEL 'TRC5'");
            await SendCommandAsync(":CALC2:MEAS5:MATH:NORM");

            // 选中 Trace 4 再 normalize
            await SendCommandAsync(":CALC2:PAR:SEL 'TRC6'");
            await SendCommandAsync(":CALC2:MEAS6:MATH:NORM");
        }

        public async Task SetNormalize_Send()
        {
            // 触发单次测量
            await ScanOnce0();
            await QueryOperationCompleteAsync();
            // 选中 Trace 3 再 normalize
            await SendCommandAsync(":CALC1:PAR:SEL 'CH1_S11_1'");
            await SendCommandAsync(":CALC1:MATH:FUNC NORM");

            // 选中 Trace 4 再 normalize
            //await SendCommandAsync(":CALC4:PAR:SEL 'TRC9'");
            //await SendCommandAsync(":CALC4:MEAS9:MATH:NORM");
        }

        // 获取 S12 增益（对数幅度，dB）
        public async Task<string[]> GetGainStringAsync()
        {
            await SendCommandAsync(":CALC:PAR:SEL 'TRC1'");
            await SendCommandAsync(":CALC:FORM MLOG");
            string data = await QueryAsync(":CALC:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        public async Task<string[]> GetGain_Yasuodian()
        {
            await SendCommandAsync(":CALC:PAR:SEL 'TRC1'");
            await SendCommandAsync(":CALC:FORM MLOG");
            string data = await QueryAsync(":CALC:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        public async Task<string[]> GetGain_Send()
        {
            await SendCommandAsync(":CALC1:PAR:SEL 'CH1_S11_2'");
            await SendCommandAsync(":CALC1:FORM MLOG");
            string data = await QueryAsync(":CALC1:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        public async Task<string[]> GetGainStringAsync_New()
        {
            await SendCommandAsync(":CALC2:PAR:SEL 'TRC5'");
            await SendCommandAsync(":CALC2:FORM MLOG");
            string data = await QueryAsync(":CALC2:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        // 获取 S11 驻波比（VSWR，扫描点数为 1，返回单值）
        public async Task<string> GetInputVSWRStringAsync()
        {
            await SendCommandAsync("CALC1:PAR:SEL 'CH1_S11_1'");
            await SendCommandAsync("CALC1:FORM SWR");
            string data = await QueryAsync("CALC1:DATA? FDATA");
            if (string.IsNullOrWhiteSpace(data))
                return "";
            return data.Split(',')[0].Trim();
        }
        // 获取 S22 驻波比（VSWR，扫描点数为 1，返回单值）
        public async Task<string> GetOutputVSWRStringAsync()
        {
            await SendCommandAsync("CALC2:PAR:SEL 'CH2_S11_3'");
            await SendCommandAsync("CALC2:FORM SWR");
            string data = await QueryAsync("CALC2:DATA? FDATA");
            if (string.IsNullOrWhiteSpace(data))
                return "";
            return data.Split(',')[0].Trim();
        }
        // 获取初始相位
        public async Task<string[]> GetInitialPhaseStringAsync()
        {
            await SendCommandAsync(":CALC:PAR:SEL 'TRC2'");
            await SendCommandAsync(":CALC:FORM UPH");
            string data = await QueryAsync(":CALC:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        public async Task<string[]> GetPhase_Send()
        {
            await SendCommandAsync(":CALC1:PAR:SEL 'CH1_S11_1'");
            await TriggerVnaSingleSweepAndWaitAsync();
            await SendCommandAsync(":CALC1:FORM PHAS");
            //await SendCommandAsync(":CALC1:FORM UPH");
            string data = await QueryAsync(":CALC1:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        /// <summary>
        /// 触发单次扫描并等待完成。调用前须已通过 SetVNACenterFreqForSweepAsync 进入 HOLD。
        /// </summary>
        private async Task TriggerVnaSingleSweepAndWaitAsync(int timeoutMs = 10000)
        {
            if (!await TriggerSingleSweepAfterHoldAsync(timeoutMs))
                throw new TimeoutException("等待矢网扫描完成超时。");
        }
        /// <summary>
        /// HOLD 设频后的单次扫描：SINGle → *OPC? → 等待完成。
        /// 不再 ABOR/强制等待“扫描中”，避免首次 OPER:DEV? 在模式切换未完成时长时间阻塞。
        /// </summary>
        public async Task<bool> TriggerSingleSweepAfterHoldAsync(int timeoutMs = 10000)
        {
            if (!await SendCommandAsync(":SENSe:SWEep:MODE SINGle"))
                return false;

            await QueryAsync("*OPC?");

            return await WaitForSweepCompleteAsync(timeoutMs);
        }
        private async Task<bool> WaitForSweepCompleteAsync(int timeoutMs)
        {
            const int pollMs = 10;
            int maxWaitCount = Math.Max(1, timeoutMs / pollMs);

            for (int waitCount = 0; waitCount < maxWaitCount; waitCount++)
            {
                if (IsScanComplete(await QueryOperationStateAsync()))
                    return true;
                await Task.Delay(pollMs);
            }

            return false;
        }
        /// <summary>
        /// 查询操作状态（对应Python的operation_state_query）
        /// </summary>
        /// <returns>状态值，16表示扫描完成</returns>
        public async Task<string> QueryOperationStateAsync()
        {
            return await QueryAsync(":STAT:OPER:DEV?");
        }

        /// <summary>
        /// 判断 :STAT:OPER:DEV? 返回值是否表示扫描已完成（bit4=16）。
        /// </summary>
        public static bool IsScanComplete(string operDevResponse)
        {
            if (string.IsNullOrWhiteSpace(operDevResponse))
                return false;

            string s = operDevResponse.Trim();
            if (s == "+16" || s == "16")
                return true;

            if (int.TryParse(s, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int val))
                return (val & 16) != 0;

            return false;
        }
        public async Task<string[]> GetInitialPhaseStringAsync_New()
        {
            await SendCommandAsync(":CALC2:PAR:SEL 'TRC6'");
            await SendCommandAsync(":CALC2:FORM UPH");
            string data = await QueryAsync(":CALC2:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }


        public async Task<double?> GetFreqStart()
        {
            string freq = await QueryAsync(":SENS:FREQ:STAR?");
            return double.TryParse(freq?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task<double?> GetFreqStop()
        {
            string freq = await QueryAsync(":SENS:FREQ:STOP?");
            return double.TryParse(freq?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task<int?> GetPointCount()
        {
            string count = await QueryAsync(":SENS:SWE:POIN?");
            return int.TryParse(count?.Trim(), out int val) ? (int?)val : null;
        }
        public async Task<bool> AutoScan()
        {
            return await SendCommandAsync("INIT:IMM; *WAI");
        }
        public async Task<bool> LoadStateFile(string vnaPath)
        {
            string command = $":MMEM:LOAD:FILE \"{vnaPath}\"";
            return await SendCommandAsync(command);
        }

        public async Task<bool> SaveStateFile(string vnaPath)
        {
            string command = $":MMEM:STOR:FILE \"{vnaPath}\"";
            return await SendCommandAsync(command);
        }
        public async Task<bool> SetPointCount(int count)
        {
            return await SendCommandAsync($":SENS:SWE:POIN {count}");
        }
        public async Task<bool> SetVNAStartFreq(double freq)
        {
            return await SendCommandAsync($":SENS:FREQ:STAR {freq}");
        }
        public async Task<bool> SetVNAStopFreq(double freq)
        {
            return await SendCommandAsync($":SENS:FREQ:STOP {freq}");
        }
        public async Task<bool> SetVNACWFreq(double freq)
        {
            return await SendCommandAsync($":SENS5:FREQ:CW {freq}");
        }
        #endregion

        #region 功率计
        // 获取当前功率值（单位 dBm）
        public async Task<double?> GetPowerDbm()
        {
            string result = await QueryAsync("MEAS:POW?");
            return double.TryParse(result?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task<double?> GetDingjiang()
        {
            string result = await QueryAsync("FETC:DRO?");
            if (!double.TryParse(result?.Trim(), out double val))
                return null;

            // 判断是否为无效值（功率计定义的 NAN 值）
            if (Math.Abs(val - 9.91e37) < 1e30)
                return null; 

            return val;
        }
        public async Task<double?> GetPositiveDuration()
        {
            string result = await QueryAsync(":TRAC1:MEAS:TRAN:POS:DUR?");
            return double.TryParse(result?.Trim(), out double val) ? (double?)val : null;
        }
        public async Task<double?> GetNegativeDuration()
        {
            string result = await QueryAsync(":TRAC1:MEAS:TRAN:NEG:DUR?");
            return double.TryParse(result?.Trim(), out double val) ? (double?)val : null;
        }
        // 设置频率
        public async Task<bool> SetFreq(double freq)
        {
            return await SendCommandAsync($":SENS:FREQ {freq}");
        }
        // 设置功率单位（如 DBM、WATT）
        public async Task<bool> SetPowerUnit(string unit)
        {
            return await SendCommandAsync($":UNIT:POW {unit}");
        }

        // 查询功率单位
        public async Task<string> GetPowerUnit()
        {
            return await QueryAsync(":UNIT:POW?");
        }

        // 启用/关闭自动校零
        public async Task<bool> SetAutoZero(bool enable)
        {
            return await SendCommandAsync($":ZERO:AUTO {(enable ? "ON" : "OFF")}");
        }

        // 查询是否开启自动校零
        public async Task<bool?> IsAutoZeroEnabled()
        {
            string result = await QueryAsync(":ZERO:AUTO?");
            if (string.IsNullOrWhiteSpace(result))
                return null;

            string val = result.Trim().ToUpper();
            if (val == "ON")
                return true;
            else if (val == "OFF")
                return false;
            else
                return null;
        }


        // 设置触发方式（IMM 或 EXT）
        public async Task<bool> SetTriggerSource(string mode)
        {
            return await SendCommandAsync($":TRIG:SOUR {mode}");
        }

        // 获取触发方式
        public async Task<string> GetTriggerSource()
        {
            return await QueryAsync(":TRIG:SOUR?");
        }

        // 启动测量（立即触发）
        public async Task<bool> TriggerImmediate()
        {
            return await SendCommandAsync("INIT:IMM; *WAI");
        }

        public async Task<string> ReadData()
        {
            return await QueryAsync("READ?");
        }
        public async Task<bool> LoadGonglvState()
        {
            return await SendCommandAsync("*RCL 3");
        }
        public async Task<bool> SaveGonglvState()
        {
            return await SendCommandAsync("*SAV 3");
        }
        public async Task<double[]> ReadPulsePowerArrayAsync()
        {
            string result = await QueryAsync(":FETC:ARR:AME:POW?");
            if (string.IsNullOrWhiteSpace(result))
                return Array.Empty<double>();

            var parts = result.Split(',');
            var values = new List<double>();

            foreach (var part in parts)
            {
                if (double.TryParse(part.Trim(), out double val))
                {
                    // 过滤无效值（9.91e37 表示 NAN）
                    if (Math.Abs(val - 9.91e37) > 1e30)
                        values.Add(val);
                    else
                        values.Add(double.NaN);
                }
                else
                {
                    values.Add(double.NaN);
                }
            }

            return values.ToArray();
        }

        #endregion

    }
}