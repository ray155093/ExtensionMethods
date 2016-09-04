using System;
using System.Data;
using System.Collections.Generic;

namespace Richi.Library.ADO
{
    public interface IADO
    {
        int Count(string connStr, string sqlStr, Dictionary<string, object> slParams);
        int Modify(string sqlStr, Dictionary<string, object> slParams);
        #region -- Select --
        List<T> Select<T>(string connStr, string sqlStr, Dictionary<string, object> slParams);
        List<T> Select<T>(string sqlStr, Dictionary<string, object> slParams);
        DataTable Select_Table(string sqlStr, Dictionary<string, object> slParams);
        #endregion
        #region -- Insert --
        bool Insert(string sqlStr, Dictionary<string, object> slParams);
        bool Insert(string sqlStr, List<Dictionary<string, object>> slParams);
        bool Insert(string tableName, string[] tableProperty, Dictionary<string, object> slParams);
        bool Insert(string tableName, string[] tableProperty, List<Dictionary<string, object>> slParams);
        //bool Insert<T>(string tableName, T saveObj);
        //bool Insert<T>(T saveObj);
        //bool Insert<T>(string tableName, List<T> saveObjs);
        //bool Insert<T>(List<T> saveObjs);
        //bool BulkCopy<T>(List<T> sabeObjs);
        #endregion
        #region -- Update --
        bool Update(string sqlStr, Dictionary<string, object> slParams);
        bool Update(string sqlStr, List<Dictionary<string, object>> slParams);
        #endregion
        #region -- Delete --
        bool Delete(string sqlStr);
        bool Delete(string sqlStr, Dictionary<string, object> slParams);
        bool Delete(string sqlStr, List<Dictionary<string, object>> slParams);
        #endregion
        #region -- Procedure --
        List<T> Procedure_Generic<T>(string sqlStr, Dictionary<string, object> slParams) where T : class;
        DataTable Procedure_Table(string sqlStr, Dictionary<string, object> slParams);
        #endregion
        string SaveChange();
    }
}
