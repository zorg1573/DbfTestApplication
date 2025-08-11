using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp.MODEL
{
    [Table("test_batch")] // 数据库中的表名
    public class MeasurementBatch
    {
        [Key]
        public int Id { get; set; }
        public string TestType { get; set; }
        public string ComponentName { get; set; }
        public string Operator { get; set; }
        public string Description { get; set; }
        public string UpdateTime { get; set; }

    }
}
