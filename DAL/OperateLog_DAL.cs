using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbfTest.MODEL;

namespace DbfTest.DAL
{
    public class OperateLog_DAL
    {
        public int InsertOperateLog_DT(string operation, string description, string operatorName)
        {
            //string updateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //string sql = $@"insert operate_log
            //     ([Operation],[Description],[Operator],[UpdateTime])  
            //    values('{operation}','{description}','{operatorName}','{updateTime}')";
            //return Dapper.DbHelper.UpdateBySql(sql);
            return 1;
        }
    }
}
