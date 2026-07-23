using System;
using DbfTest.MODEL;

namespace DbfTest.FUNCTION
{
    /// <summary>
    /// 温度回传帧解析器。把下位机经 UDP 回传的负载解析成 <see cref="TemperatureReturnFrame"/>。
    /// 解析逻辑与下位机 mac_test.v 中 uart_return_frame 的组帧一致（字头 55 AA、字尾 AA 55）。
    /// </summary>
    public static class TemperatureReturnParser
    {
        private const int SdDataOffset = 3;   // byte3 起
        private const int SdDataLength = 29;  // byte3~31，共 29 字节
        private const int Temp2Offset = 32;   // byte32/33
        private const int Temp3Offset = 34;   // byte34/35
        private const int Temp4Offset = 36;   // byte36/37
        private const int Tail0Offset = 38;   // byte38
        private const int Tail1Offset = 39;   // byte39

        /// <summary>
        /// 尝试从 UDP 负载中解析温度回传帧。会在负载中扫描 55 AA 23 … AA 55 的完整 40 字节帧，
        /// 因此对前后存在多余字节的情况同样适用；若未找到有效帧则返回 false。
        /// </summary>
        public static bool TryParse(byte[] payload, out TemperatureReturnFrame frame)
        {
            frame = null;
            if (payload == null)
                return false;

            int start = FindFrameStart(payload);
            if (start < 0)
                return false;

            var raw = new byte[TemperatureReturnFrame.FrameLength];
            Array.Copy(payload, start, raw, 0, TemperatureReturnFrame.FrameLength);

            var sd = new byte[SdDataLength];
            Array.Copy(raw, SdDataOffset, sd, 0, SdDataLength);

            frame = new TemperatureReturnFrame
            {
                Raw = raw,
                SdData = sd,
                Temperature2Raw = ReadInt16BigEndian(raw, Temp2Offset),
                Temperature3Raw = ReadInt16BigEndian(raw, Temp3Offset),
                Temperature4Raw = ReadInt16BigEndian(raw, Temp4Offset),
                Timestamp = DateTime.Now
            };
            return true;
        }

        /// <summary>在负载中查找符合字头/长度/字尾的 40 字节帧起始位置，找不到返回 -1。</summary>
        private static int FindFrameStart(byte[] data)
        {
            int last = data.Length - TemperatureReturnFrame.FrameLength;
            for (int i = 0; i <= last; i++)
            {
                if (data[i] == TemperatureReturnFrame.Header0 &&
                    data[i + 1] == TemperatureReturnFrame.Header1 &&
                    data[i + 2] == TemperatureReturnFrame.ContentLength &&
                    data[i + Tail0Offset] == TemperatureReturnFrame.Tail0 &&
                    data[i + Tail1Offset] == TemperatureReturnFrame.Tail1)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>按大端（高字节在前）读取 16 位有符号整数。</summary>
        private static short ReadInt16BigEndian(byte[] data, int offset)
        {
            return (short)((data[offset] << 8) | data[offset + 1]);
        }
    }
}
