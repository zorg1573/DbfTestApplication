using System;

namespace DbfTest.MODEL
{
    /// <summary>
    /// 温度回传帧解析结果。
    ///
    /// 协议（共 40 字节，详见《串口回传.txt》）：
    ///   byte0      = 0x55      字头
    ///   byte1      = 0xAA      字头
    ///   byte2      = 0x23      数据内容长度（温度到异常计数共 35 字节）
    ///   byte3~31   =           前一组 SD 数据（224 位 + 8 位填充，共 29 字节）
    ///   byte32/33  = 温度2      高8位 + 低8位（16 位有符号补码）
    ///   byte34/35  = 温度3      高8位 + 低8位（16 位有符号补码）
    ///   byte36/37  = 温度4      高8位 + 低8位（16 位有符号补码）
    ///   byte38     = 0xAA      字尾
    ///   byte39     = 0x55      字尾
    ///
    /// 温度传感器为 DS18B20，下位机直接回传其 16 位有符号寄存器值，
    /// 实际温度（℃）= 原始值 / 16（LSB = 0.0625℃）。
    /// </summary>
    public class TemperatureReturnFrame
    {
        public const int FrameLength = 40;
        public const byte Header0 = 0x55;
        public const byte Header1 = 0xAA;
        public const byte ContentLength = 0x23;
        public const byte Tail0 = 0xAA;
        public const byte Tail1 = 0x55;

        /// <summary>DS18B20 分辨率：每个 LSB 对应 1/16 ℃。</summary>
        public const double CelsiusPerLsb = 1.0 / 16.0;

        /// <summary>完整的 40 字节原始帧。</summary>
        public byte[] Raw { get; set; }

        /// <summary>byte3~31，前一组 SD 的原始字节（含填充位，未拆位）。</summary>
        public byte[] SdData { get; set; }

        /// <summary>温度2 原始 16 位有符号值。</summary>
        public short Temperature2Raw { get; set; }

        /// <summary>温度3 原始 16 位有符号值。</summary>
        public short Temperature3Raw { get; set; }

        /// <summary>温度4 原始 16 位有符号值。</summary>
        public short Temperature4Raw { get; set; }

        /// <summary>温度2，单位 ℃。</summary>
        public double Temperature2C => Temperature2Raw * CelsiusPerLsb;

        /// <summary>温度3，单位 ℃。</summary>
        public double Temperature3C => Temperature3Raw * CelsiusPerLsb;

        /// <summary>温度4，单位 ℃。</summary>
        public double Temperature4C => Temperature4Raw * CelsiusPerLsb;

        /// <summary>解析时间（本机时间）。</summary>
        public DateTime Timestamp { get; set; }
    }
}
