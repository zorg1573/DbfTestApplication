using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace TestApp.DAL.Dapper
{
    public static class Log
    {
        /// <summary>
        /// 错误日志
        /// </summary>
        public static void ErrLog(string errMsg)
        {
            string Path = "";
            Path = System.AppDomain.CurrentDomain.BaseDirectory + "Log\\" + "时间" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            FileStream fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter m_streamWriter = new StreamWriter(fs);
            m_streamWriter.BaseStream.Seek(0, SeekOrigin.End);
            m_streamWriter.WriteLine(DateTime.Now.ToString() + "：" + errMsg.Replace("\r\n", "") + "\n");
            m_streamWriter.Flush();
            m_streamWriter.Close();
            fs.Close();
            GC.Collect();
        }
    }
}
