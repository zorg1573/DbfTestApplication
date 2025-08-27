using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbfTest.MODEL
{
    [Table("operate_log")] // 数据库中的表名
    public class OperateLog
    {
        [Key]
        public int Id { get; set; }
        public string Operation { get; set; }
        public string Description { get; set; }
        public string Operator { get; set; }
        public string UpdateTime { get; set; }

    }
}
