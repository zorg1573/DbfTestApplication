using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestApp.MODEL;

namespace TestApp.DAL
{
    public class Main_DAL
    {
        #region test_data
        public int InsertTestData_DT(MeasurementResult model)
        {
            //string sql = $@"insert test_data
            //     ([TestType],[ComponentName],[BatchId],[PointIndex],[PointFreq],[Gain],[InitialPhase],[InputSWR],[OutputSWR],[Person],[UpdateTime])  
            //    values('{model.TestType}','{model.ComponentName}','{model.BatchId}','{model.PointIndex}','{model.PointFreq}','{model.Gain}','{model.InitialPhase}','{model.InputSWR}','{model.OutputSWR}','{model.Person}','{model.UpdateTime}')";
            //return Dapper.DbHelper.UpdateBySql(sql);
            return 1;
        }
        public int UpdateTestDataFreq_DT(string testType, string componentName, double pointFreq, double freq)
        {
            //int batchId = GetBatchId(testType, componentName);
            //double pointFreqRounded = Math.Round(pointFreq, 2);

            //string sql = $@"
            //    UPDATE test_data 
            //    SET Frequency = '{freq}'
            //    WHERE BatchId = '{batchId}' AND PointFreq = '{pointFreqRounded}' AND TestType = '{testType}' AND ComponentName = '{componentName}'";
            //return Dapper.DbHelper.UpdateBySql(sql);
            return 1;
        }
        public int UpdateTestDataZaosheng_DT(string testType, string componentName, double pointFreq, double? zaosheng)
        {
            //int batchId = GetBatchId(testType, componentName);
            //double pointFreqRounded = Math.Round(pointFreq, 2);

            //string sql = $@"
            //    UPDATE test_data 
            //    SET Zaosheng = '{zaosheng}'
            //    WHERE BatchId = '{batchId}' AND PointFreq = '{pointFreqRounded}' AND TestType = '{testType}' AND ComponentName = '{componentName}'";
            //return Dapper.DbHelper.UpdateBySql(sql);
            return 1;
        }
        public int GetBatchId(string testType, string componentName)
        {
            //string sql = $@"
            //    SELECT TOP 1 
            //        BatchId
            //    FROM test_data
            //    WHERE TestType = '{testType}' and ComponentName = '{componentName}'
            //    ORDER BY id DESC";
            //Dapper.SqlUtil Sqlutil = new Dapper.SqlUtil();
            //var result = Sqlutil.Query<MeasurementResult>(sql);
            //if(result.Count == 0)
            //{
            //    return 0;
            //}
            //else
            //{
            //    return result.First().BatchId;
            //}
            return 1; 
        }

        public DataTable GetTestData_DT()
        {
            //string sql = $"select * from test_data order by Id";
            //return Dapper.DbHelper.GetDataTableBySql(sql);
            return new DataTable();
        }

        public List<MeasurementResult> GetTestData_List()
        {
            //string sql = $@"select * from test_data";
            //Dapper.SqlUtil Sqlutil = new Dapper.SqlUtil();
            //var result = Sqlutil.Query<MeasurementResult>(sql);
            //return result;
            return new List<MeasurementResult>();
        }
        #endregion

        public int InsertTestBatch_DT(MeasurementBatch model)
        {
            string sql = $@"insert test_batch
                 ([Operator],[Description],[UpdateTime])  
                values('{model.Operator}','{model.Description}','{model.UpdateTime}')";
            return Dapper.DbHelper.UpdateBySql(sql);
            //return 1;
        }


    }
}
