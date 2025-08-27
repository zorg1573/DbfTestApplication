using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using DbfTest.MODEL;

namespace DbfTest.DAL.Dapper
{
    /// <summary>
    /// 
    /// </summary>
    public static class Sqlconn_MES
    {
        public static SqlConnection conns_MES;

    }


    public class SqlUtil : IDisposable
    {
        #region 成员变量
        private SqlConnection conn_MES = null;
        private SqlConnection conn_EasyFASDAAB = null;
        private SqlConnection conn_KeLePack = null;
        private SqlConnection conn_EasyFAS_Kohler = null;
        private SqlCommand cmd = null;
        private string connectionString;

        #endregion

        #region Getter Setter
        /// <summary>
        /// 获取连接
        /// </summary>
        public SqlConnection Conn
        {
            get { return conn_MES; }
        }
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        public string ConnectionString
        {
            get { return this.connectionString; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strcon">连接字符串</param>
        public SqlUtil()
        {
            //this.connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TestSystem"].ToString();
            DbConfig dbConfig = new DbConfig();

            this.connectionString = dbConfig.GetConnectionString();;

            cmd = new SqlCommand();
            cmd.CommandTimeout = 999;

            //Open();
            conn_MES = Sqlconn_MES.conns_MES;
            if (conn_MES == null)
            {
                conn_MES = new SqlConnection(connectionString);

                conn_MES.Open();

                Sqlconn_MES.conns_MES = conn_MES;
            }


            if (conn_MES.State == ConnectionState.Closed)
            {

                conn_MES = new SqlConnection(connectionString);
                conn_MES.Open();
                Sqlconn_MES.conns_MES = conn_MES;

            }

            if (conn_MES.State == ConnectionState.Broken)
            {

                conn_MES = new SqlConnection(connectionString);
                //conn.Close();
                conn_MES.Open();
                Sqlconn_MES.conns_MES = conn_MES;
            }



            if (conn_MES.State == ConnectionState.Open)
            {
                Console.WriteLine();
            }
            cmd.Connection = conn_MES;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strcon">连接字符串</param>




        #endregion

        #region ExecuteScalar param
        /// <summary>
        /// 在指定的连接上用指定的参数执行SQL命令
        /// </summary>
        /// <param name="sql">T-SQL语句</param>
        /// <param name="param">命令的参数</param>
        /// <returns>返回一个单值</returns>
        public object ExecuteScalar(string sql, params SqlParameter[] param)
        {
            PreparedCommand(sql, param);
            return cmd.ExecuteScalar();
        }
        #endregion

        #region ExecuteScalar
        /// <summary>
        /// 在指定的连接上用指定的参数执行SQL命令
        /// </summary>
        /// <param name="sql">T-SQL语句</param>
        /// <param name="param">命令的参数</param>
        /// <returns>返回一个单值</returns>
        public object ExecuteScalar(string sql)
        {
            PreparedCommand(sql, null);
            return cmd.ExecuteScalar();
        }
        #endregion

        #region Count param
        /// <summary>
        /// 查询结果的记录数
        /// </summary>
        /// <param name="sql">T-SQL语句</param>
        /// <param name="param">命令的参数</param>
        /// <returns>int</returns>
        public int Count(string sql, params SqlParameter[] param)
        {
            Object obj = ExecuteScalar(sql, param);
            if (Convert.IsDBNull(obj))
            {
                return 0;
            }
            return Convert.ToInt32(obj);
        }
        #endregion

        #region Count
        /// <summary>
        /// 查询结果的记录数
        /// </summary>
        /// <param name="sql">T-SQL语句</param>
        /// <returns>int</returns>
        public int Count(string sql)
        {
            Object obj = ExecuteScalar(sql);
            if (Convert.IsDBNull(obj))
            {
                return 0;
            }
            return Convert.ToInt32(obj);
        }
        #endregion

        #region ExecuteNonQuery param
        /// <summary>
        /// 执行非查询语句
        /// </summary>
        /// <param name="sql">insert,update,delete语句</param>
        /// <param name="param">参数</param>
        /// <returns>影响的行数</returns>
        public int ExecuteNonQuery(string sql, params SqlParameter[] param)
        {
            PreparedCommand(sql, param);
            return cmd.ExecuteNonQuery();
        }
        #endregion

        #region ExecuteNonQuery
        /// <summary>
        /// 执行非查询语句
        /// </summary>
        /// <param name="sql">insert,update,delete语句</param>
        /// <returns>影响的行数</returns>
        public int ExecuteNonQuery(string sql)
        {
            PreparedCommand(sql, null);
            return cmd.ExecuteNonQuery();
        }
        #endregion

        #region ExecuteQuery param
        /// <summary>
        /// 执行查询
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <param name="param">参数</param>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader ExecuteQuery(string sql, params SqlParameter[] param)
        {
            PreparedCommand(sql, param);
            return cmd.ExecuteReader();
        }
        #endregion

        #region ExecuteQuery
        /// <summary>
        /// 执行查询
        /// </summary>
        /// <param name="sql">查询语句</param>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader ExecuteQuery(string sql)
        {
            PreparedCommand(sql, null);
            return cmd.ExecuteReader();
        }
        #endregion

        public DataSet Load(string sql, params SqlParameter[] param)
        {
            PreparedCommand(sql, param);
            SqlDataAdapter adap = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adap.Fill(ds);
            return ds;
        }

        public DataSet Load(string sql)
        {
            var watch = Stopwatch.StartNew();
            PreparedCommand(sql, null);
            SqlDataAdapter adap = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adap.Fill(ds);
            //conn.Close();

            watch.Stop();
            var elapsed = watch.Elapsed;
            return ds;
        }

        public List<T> Query<T>(string sql, params SqlParameter[] param)
        {
            DataSet ds = Load(sql, param);
            return DBUtil.DataSetToList<T>(ds, 0).ToList();
        }

        public List<T> Query<T>(string sql)
        {
            DataSet ds = Load(sql);
            return DBUtil.DataSetToList<T>(ds, 0).ToList();
        }

        public object Get<T>(string sql, params SqlParameter[] param)
        {
            List<T> list = Query<T>(sql, param);
            if (list != null && list.Count > 0)
            {
                return list.First();
            }
            return null;
        }

        public object Get<T>(string sql)
        {
            List<T> list = Query<T>(sql);
            if (list != null && list.Count > 0)
            {
                return list.First();
            }
            return null;
        }

        #region Insert
        /// <summary>
        /// 插入对象
        /// </summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="entity">实体类对象</param>
        /// <param name="identityName">自增长主键名</param>
        /// <returns></returns>
        public int Insert<T>(T entity, string identityName)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO ");
            Type type = entity.GetType();
            string tableName = type.Name;//表名称
            PropertyInfo[] pros = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);//所有字段名称
            StringBuilder fieldStr = new StringBuilder();//拼接需要插入数据库的字段
            StringBuilder paramStr = new StringBuilder();//拼接每个字段对应的参数
            int len = pros.Length;
            SqlParameter[] param;
            if (!"".Equals(identityName) && null != identityName)
                param = new SqlParameter[len - 1];//如果有自动增长的字段,则该字段不需要SqlParameter
            else
            {
                param = new SqlParameter[len];
            }
            int paramLIndex = 0;
            for (int i = 0; i < len; i++)
            {
                string fieldName = pros[i].Name;
                if (null == identityName || !fieldName.ToUpper().Equals(identityName.ToUpper()))
                {//非自动增长字段才加入SQL语句
                    fieldStr.Append(fieldName);
                    string paramName = "@" + fieldName;//SQL语句的字段名称和参数名称保持一致
                    paramStr.Append(paramName);
                    if (i < (len - 1))
                    {
                        fieldStr.Append(",");//参数和字段用逗号隔开
                        paramStr.Append(",");
                    }
                    object val = type.GetProperty(fieldName).GetValue(entity, null);// 根据属性名称获取当前属性的值
                    if (val == null)
                        val = DBNull.Value;//如果该值为空的话,则将其转化为数据库的NULL
                    param[paramLIndex] = new SqlParameter(fieldName, val);//给每个参数赋值
                    paramLIndex++;
                }
            }

            sql.Append(tableName);
            sql.Append(" ( ");
            sql.Append(fieldStr);
            sql.Append(" ) VALUES ( ");
            sql.Append(paramStr);
            sql.Append(" ) ");//拼接成完整的字符串
            return ExecuteNonQuery(sql.ToString(), param);//执行该INSERT SQL 语句
        }
        #endregion

        #region PreparedCommand
        /// <summary>
        /// 用来初始化Command对象
        /// </summary>
        /// <param name="sql">T-SQL语句</param>
        /// <param name="param">命令的参数</param>
        private void PreparedCommand(string sql, params SqlParameter[] param)
        {
            cmd.CommandText = sql;
            //清空Parameters中的参数
            cmd.Parameters.Clear();
            if (param != null)
            {
                cmd.Parameters.AddRange(param);
                //foreach (SqlParameter p in param)
                //{
                //    cmd.Parameters.Add(p);
                //}
            }

        }
        #endregion

        #region Open
        /// <summary>
        /// 打开数据库
        /// </summary>
        //private void Open()
        //{
        //    conn.Open();

        //}
        #endregion

        #region Close
        /// <summary>
        /// 关闭数据库
        /// </summary>
        //public void Close()
        //{
        //    cmd.Dispose();
        //    conn.Close();
        //    conn.Dispose();

        //}
        #endregion

        #region Dispose
        /// <summary>
        /// 关闭数据库
        /// </summary>
        public void Dispose()
        {
            cmd.Dispose();
            conn_MES.Close();
            conn_MES.Dispose();
            conn_EasyFASDAAB.Close();
            conn_EasyFASDAAB.Dispose();
            conn_EasyFAS_Kohler.Close();
            conn_EasyFAS_Kohler.Dispose();
            conn_KeLePack.Close();
            conn_KeLePack.Dispose();

        }
        #endregion
    }
}
