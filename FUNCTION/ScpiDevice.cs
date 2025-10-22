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

namespace DbfTest.FUNCTION
{
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
        public async Task QueryOpc()
        {
            // 发出操作完成查询命令，直到设备响应
            string result = await QueryAsync("*OPC?");
            if (!result.Trim().Equals("1"))
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
        public async Task SetCenterFrequencyAsync(double freqHz) => await SendCommandAsync($"FREQ:CENT {freqHz}");
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

        #region 噪声系数测量

        // 设置仪器进入噪声系数测量模式
        public async Task EnterNoiseFigureModeAsync() => await SendCommandAsync(":INST:SEL NFIGURE");

        // 加载噪声系数测量预设状态文件
        public async Task LoadPinpuStateAsync(string filePath) => await SendCommandAsync($":MMEM:LOAD:STAT 1,'{filePath}'");

        // 设置是否连续测量
        public async Task SetContinuousMeasurementAsync(bool enable) =>
            await SendCommandAsync($":INIT:CONT {(enable ? "ON" : "OFF")}");

        // 手动触发一次噪声系数测量
        public async Task TriggerSingleMeasurementAsync() => await SendCommandAsync(":INIT:REST");

        // 查询仪器是否完成测量
        public async Task<bool> QueryOperationCompleteAsync()
        {
            var response = await QueryAsync("*OPC?");
            return response.Trim() == "1";
        }

        // 获取已校正的噪声系数结果（单位 dB）
        public async Task<string> FetchCorrectedNoiseFigureAsync() =>
            await QueryAsync(":FETCH:CORR:NFIG?DB");

        public async Task<string[]> GetZaoshengData(double freq)
        {
            /*            await SendCommandAsync(":INST:SEL NFIGURE");
                        await SendCommandAsync(":MMEM:LOAD:STATe '/usrdata/Data/1517.sta'");
                        await SendCommandAsync(":INIT:CONT OFF");
                        await SendCommandAsync(":INIT:REST");
                        string opc = await QueryAsync("*OPC?");

                        string data = await QueryAsync(":FETCH:CORR:NFIG? DB");
                        if (string.IsNullOrWhiteSpace(data)) return null;

                        string[] parts = data.Split(',');
                        return parts;*/
            await SendCommandAsync(":MMEM:LOAD:STAT 1,'C:/R_S/Instr/user/QuickSave/dbfzaosheng.dfl'");
            await SendCommandAsync("*OPC");
            double benZhen = freq - 175 * 1e6;
            await SendCommandAsync($":SENS:CONF:MODE:SYST:LO:FREQ {benZhen}");
            await SendCommandAsync($":SENS:FREQ:CENT {freq}");
            await SendCommandAsync("INIT:IMM");
            await Task.Delay(10000);

            //string data = await QueryAsync("RESULTS:TRACe1:DATA? TRAC1,NOISe");
            string data = await QueryAsync("TRAC? TRACE1, NOISe");
            await SendCommandAsync("*OPC");
            if (string.IsNullOrWhiteSpace(data)) return null;

            string[] parts = data.Split(',');

            // 将每个字符串尝试转换为 double，否则设为 NaN
            string[] values = parts
                .Select(p =>
                {
                    if (double.TryParse(p, out double val))
                    {
                        // 检查是否是无效的特殊值
                        if (Math.Abs(val - 9.9099995E+37) < 1e30)
                            return double.NaN.ToString();
                        else
                            return val.ToString();
                    }
                    else
                    {
                        return double.NaN.ToString();
                    }
                })
                .ToArray();
            return values;
        }
        #endregion
        #region 三阶交调
        public async Task<double?> GetIP3()
        {
            string resp = await QueryAsync(":FETC:TOI:IP3?");
            return double.TryParse(resp?.Trim(), out double val) ? (double?)val : null;
        }

        #endregion
        #endregion


        #region 矢网
        // 设置测量通道并选择参数（如 S12）
        public async Task<bool> SelectSParameterAsync(string name, string sparam)
        {
            await SendCommandAsync($"CALC:PAR:DEF '{name}', {sparam}");
            return await SendCommandAsync($"CALC:PAR:SEL '{name}'");
        }

        // 读取某格式下的 S 参数数据（返回第一点，或者你可以扩展为返回数组）
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
            await ScanOnce(4);
            await Task.Delay(1000);
            // 选中 Trace 3 再 normalize
            await SendCommandAsync(":CALC4:PAR:SEL 'TRC8'");
            await SendCommandAsync(":CALC4:MEAS8:MATH:NORM");

            // 选中 Trace 4 再 normalize
            await SendCommandAsync(":CALC4:PAR:SEL 'TRC9'");
            await SendCommandAsync(":CALC4:MEAS9:MATH:NORM");
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
            await SendCommandAsync(":CALC4:PAR:SEL 'TRC8'");
            await SendCommandAsync(":CALC4:FORM MLOG");
            string data = await QueryAsync(":CALC4:DATA? FDATA");
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
        // 获取 S11 驻波比（VSWR）
        public async Task<string[]> GetInputVSWRStringAsync()
        {
            await SendCommandAsync("CALC:PAR:SEL 'TRC3'");
            await SendCommandAsync("CALC:FORM SWR");
            string data = await QueryAsync("CALC:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
        }
        // 获取 S22 驻波比（VSWR）
        public async Task<string[]> GetOutputVSWRStringAsync()
        {
            await SendCommandAsync("CALC:PAR:SEL 'TRC4'");
            await SendCommandAsync("CALC:FORM SWR");
            string data = await QueryAsync("CALC:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
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
            await SendCommandAsync(":CALC4:PAR:SEL 'TRC9'");
            await SendCommandAsync(":CALC4:FORM UPH");
            string data = await QueryAsync(":CALC4:DATA? FDATA");
            string[] parts = data?.Split(',');
            return parts;
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
            return await SendCommandAsync("*RCL 1");
        }
        public async Task<bool> SaveGonglvState()
        {
            return await SendCommandAsync("*SAV 1");
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
