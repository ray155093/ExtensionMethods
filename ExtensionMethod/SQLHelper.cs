using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections;

namespace ExtensionMethods.Data
{
    #region SQLHelper
    /// <summary>
    /// The SQLDataAccess class is intended to encapsulate high performance, 
    /// scalable best practices for common uses of SqlClient.
    /// </summary>
    public abstract class SQLHelper
    {
        #region GenerateSQLTransaction
        /// <summary>
        /// Generate a SqlTransaction against the database specified in the connection string.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  SqlTransaction tSQLTrans = GenerateSQLTransaction(connString);
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <returns>an SqlTransaction</returns>
        public static SqlTransaction GenerateSQLTransaction(string connectionString)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            SqlTransaction transaction;

            // Start a local transaction.
            transaction = connection.BeginTransaction("SampleTransaction");

            return transaction;
        }

        #endregion

        #region ExecuteNonQuery
        /// <summary>
        /// 依參數執行query並產生被影響的資料列列數 (使用現有ConnectionString)
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">資料庫連線Connection String</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>回傳查詢後被影響的資料列列數(return int)</returns>
        public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                return ExecuteNonQuery(connectionString, cmdType, cmdText, null, commandParameters);
            }
        }

        /// <summary>
        /// 依參數執行query並產生被影響的資料列列數 (使用現有ConnectionString)
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", 60, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">資料庫連線Connection String</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeout">command timeout</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>回傳查詢後被影響的資料列列數(return int)</returns>
        public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, int? cmdTimeout, params SqlParameter[] commandParameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                return ExecuteNonQuery(connection, cmdType, cmdText, cmdTimeout, commandParameters);
            }
        }

        /// <summary>
        /// 依參數執行query並產生被影響的資料列列數 (使用現有Connection)
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">資料庫連線Connection</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeout">command timeout</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>回傳查詢後被影響的資料列列數(return int)</returns>
        public static int ExecuteNonQuery(SqlConnection connection, CommandType cmdType, string cmdText, int? cmdTimeout, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();

            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters, cmdTimeout);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }

        /// <summary>
        /// 依參數執行query並產生被影響的資料列列數 (使用現有Transaction) 
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="trans">an existing sql transaction</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>回傳查詢後被影響的資料列列數(return int)</returns>
        public static int ExecuteNonQuery(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteNonQuery(trans, cmdType, cmdText, null, commandParameters);
        }

        /// <summary>
        /// 依參數執行query並產生被影響的資料列列數 (使用現有Transaction) 
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="trans">an existing sql transaction</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeout">command timeout</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>回傳查詢後被影響的資料列列數(return int)</returns>
        public static int ExecuteNonQuery(SqlTransaction trans, CommandType cmdType, string cmdText, int? cmdTimeout, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters, cmdTimeout);
            int val = cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
            return val;
        }
        #endregion

        #region ExecuteScalar
        /// <summary>
        /// 依參數執行query並產生使用者自訂物件型別資料 (使用ConnectionString)
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                return ExecuteScalar(connection, cmdType, cmdText, null, commandParameters);
            }
        }


        /// <summary>
        /// 依參數執行query並產生使用者自訂物件型別資料 (使用connection) 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(connection, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">an existing database connection</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <param name="cmdTimeout">command timeout</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlConnection connection, CommandType cmdType, string cmdText, int? cmdTimeout, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();

            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters, cmdTimeout);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against an existing database connection 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(oSQLTransaction, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="trans">an existing database transaction</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlTransaction trans, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            return ExecuteScalar(trans, cmdType, cmdText, null, commandParameters);
        }

        /// <summary>
        /// Execute a SqlCommand that returns the first column of the first record against an existing database connection 
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteScalar(oSQLTransaction, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="trans">an existing database transaction</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeout">command timeout</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>An object that should be converted to the expected type using Convert.To{Type}</returns>
        public static object ExecuteScalar(SqlTransaction trans, CommandType cmdType, string cmdText, int? cmdTimeout, params SqlParameter[] commandParameters)
        {

            SqlCommand cmd = new SqlCommand();

            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters, cmdTimeout);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }
        #endregion

        #region ExecuteDataSet
        /// <summary>
        /// 依參數執行query並產生DataSet (使用ConnectionString)
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteDataSet(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">資料庫連線Connection String</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>A DataSet containing the results</returns>
        public static DataSet ExecuteDataSet(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                DataSet val = ExecuteDataSet(connection, cmdType, cmdText, null, commandParameters);
                return val;
            }
        }

        /// <summary>
        /// 依參數執行query並產生DataSet (使用現有Connection)
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  Object obj = ExecuteDataSet(connection, CommandType.StoredProcedure, "PublishOrders", 60, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">an existing database connection</param>
        /// <param name="cmdType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="cmdText">the stored procedure name or T-SQL command</param>
        /// <param name="cmdTimeout">execute command timeout</param>
        /// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
        /// <returns>A DataSet containing the results</returns>
        public static DataSet ExecuteDataSet(SqlConnection connection, CommandType cmdType, string cmdText, int? cmdTimeout, params SqlParameter[] commandParameters)
        {

            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters, cmdTimeout);
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            return ds;
        }

        #endregion

        /// <summary>
        /// Prepare a command for execution
        /// </summary>
        /// <param name="cmd">SqlCommand object</param>
        /// <param name="conn">SqlConnection object</param>
        /// <param name="trans">SqlTransaction object</param>
        /// <param name="cmdType">Cmd type e.g. stored procedure or text</param>
        /// <param name="cmdText">Command text, e.g. Select * from Products</param>
        /// <param name="cmdParms">SqlParameters to use in the command</param>
        /// <param name="cmdTimeout">command timeout</param>
        private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms, int? cmdTimeout)
        {

            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = cmdType;
            if (cmdTimeout != null)
                cmd.CommandTimeout = (int)cmdTimeout;
            else
            {
                if (ConfigurationManager.AppSettings["sqlCmdTimeout"] != null)
                    cmd.CommandTimeout = Convert.ToInt16(ConfigurationManager.AppSettings["sqlCmdTimeout"]);
            }

            if (cmdParms != null)
            {
                foreach (SqlParameter parm in cmdParms)
                {
                    if (parm != null)
                        cmd.Parameters.Add(parm);
                }
            }
        }

    }
    #endregion

    #region SQLParamList
    /// <summary>
    /// 透過List的方式產生SqlParameter Objects
    /// </summary>
    /// <remarks>
    /// SQLStr = "SELECT * FROM Table1 WHERE KeyField = @KEY1 ";
    /// SQLParamList oSQLParam = new SQLParamList();
    /// oSQLParam.Add("@KEY1", "VALUE");
    /// SQLDataAccess.ExecuteDataSet(SQLDataAccess.DefaultConnectionString, CommandType.Text, SQLStr, oSQLParam.ToArray();
    /// </remarks>
    public class SQLParamList
    {

        private System.Collections.Generic.List<SqlParameter> _SQLParam;

        /// <summary>
        /// SQLParamList Constrator
        /// </summary>
        public SQLParamList()
        {
            _SQLParam = new System.Collections.Generic.List<SqlParameter>();
        }

        /// <summary>
        /// 新增SqlParameter
        /// </summary>
        /// <param name="parameterName">參數名稱</param>
        /// <param name="value">參數值</param>
        /// <remarks>若value為null, 則會轉換成DBNull.Value</remarks>
        public void Add(string parameterName, object value)
        {
            object addValue;
            if (value == null)
                addValue = DBNull.Value;
            else
                addValue = value;
            _SQLParam.Add(new SqlParameter(parameterName, addValue));
        }

        /// <summary>
        /// 將List 的SqlParameter 轉成手SqlParameter[]
        /// </summary>
        /// <returns></returns>
        public SqlParameter[] ToArray()
        {
            SqlParameter[] oParam = new SqlParameter[_SQLParam.Count];
            _SQLParam.CopyTo(oParam, 0);
            return oParam;
        }
    }
    #endregion


}
