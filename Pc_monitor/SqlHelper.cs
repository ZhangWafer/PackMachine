using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Pc_monitor
{
    class SqlHelper
    {
        //获取配置文件中连接sql字符串
        public static string GetSqlConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["Sql"].ConnectionString;
        }


        // sql执行方法，参数说明:sqlText:需要执行的sql脚本，parameters:需要的参数集合
        public static int ExecuteNonQuery(string sqlText)
        {
            using (SqlConnection conn=new SqlConnection(GetSqlConnectionString()))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sqlText;  //对命令语句进行赋值
                    return cmd.ExecuteNonQuery();  
                }
            }
        }

        //该方法的返回值第object,所以当我们查询的数据不知道是什么类型的时候可以使用该类。
        public static object ExecuteScalar(string sqlText)
        {
            using (SqlConnection conn = new SqlConnection(GetSqlConnectionString()))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = sqlText;
                    return cmd.ExecuteScalar();
                }
            }
        }


        // 该方法主要用于一些查询数据，dt将被填充查询出来的数据，然后返回数据。
        public static DataTable ExecuteDataTable(string sqlText)
        {
            using (SqlDataAdapter adapter = new SqlDataAdapter(sqlText, GetSqlConnectionString()))
            {
                DataTable dt=new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }
        //初始化dataReader
        public static SqlDataReader ExecuteReader(string sqlText)
        {
            //SqlDataReader要求，它读取数据的时候有，它独占它的SqlConnection对象，而且SqlConnection必须是Open状态
            SqlConnection conn=new SqlConnection(GetSqlConnectionString());
            SqlCommand cmd = new SqlCommand(sqlText,conn);
            conn.Open();
            cmd.CommandText = sqlText;
            //CommandBehavior.CloseConnection当SqlDataReader释放的时候，顺便把SqlConnection对象也释放掉
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }


    }
}
