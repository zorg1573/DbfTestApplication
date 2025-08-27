using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbfTest.MODEL
{
    [Table("test_data")] // 数据库中的表名
    public class MeasurementResult
    {
        [Key]
        public int Id { get; set; }
        public string TestType { get; set; }
        public string ComponentName { get; set; }
        public int BatchId { get; set; }
        public int PointIndex { get; set; }
        public double PointFreq { get; set; }
        public double Gain { get; set; }
        public double InitialPhase { get; set; }
        public double InputSWR { get; set; }
        public double OutputSWR { get; set; }
        public double Frequency { get; set; }
        public double Zaosheng { get; set; }
        public string Person { get; set; }
        public string UpdateTime { get; set; }
    }
}
