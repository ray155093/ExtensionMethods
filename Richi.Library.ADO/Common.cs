using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using MySql.Data.MySqlClient;

namespace Richi.Library.ADO
{
    internal class Common
    {
        public string GetParam(string SearchParam, string[] sqlparams)
        {
            string paramsValue = "";
            foreach (string p in sqlparams)
            {
                if (p.IndexOf(SearchParam) != -1)
                {
                    if (p.IndexOf('=', 0) != -1)
                    {
                        int spos = p.IndexOf('=', 0) + 1;
                        paramsValue = p.Substring(spos);
                    }
                }
            }
            return paramsValue;
        }
        public bool ExistParam(string SearchParam, string[] sqlparams)
        {
            bool isExist = false;
            foreach (string p in sqlparams)
            {
                if (p.IndexOf(SearchParam) != -1) isExist = true;
            }
            return isExist;
        }
        
        public void SqlParameterSet(ref SqlCommand oCmd, Dictionary<string, object> sqlParams)
        {
            foreach (var item in sqlParams)
            {
                SqlParameter _p = new SqlParameter();
                _p.ParameterName = item.Key;
                var _type = item.Value.GetType().GetProperty("type").GetValue(item.Value, null);
                if(_type != null)
                    _p.SqlDbType = (SqlDbType)_type;
                _p.Direction = ParameterDirection.Input;
                _p.Value = item.Value.GetType().GetProperty("value").GetValue(item.Value, null);
                oCmd.Parameters.Add(_p);
            }
        }
        public void SqlParameterSet(ref MySqlCommand oCmd, Dictionary<string, object> sqlParams)
        {
            foreach (var item in sqlParams)
            {
                MySqlParameter _p = new MySqlParameter();
                _p.ParameterName = item.Key;
                var _type = item.Value.GetType().GetProperty("type").GetValue(item.Value, null);
                if (_type != null)
                    _p.MySqlDbType = (MySqlDbType)_type;
                _p.Direction = ParameterDirection.Input;
                _p.Value = item.Value.GetType().GetProperty("value").GetValue(item.Value, null);
                oCmd.Parameters.Add(_p);
            }
        }

        public string CombindTablePropertyStr(string[] tableProperty)
        {
            string _propertyStr = string.Join(",", tableProperty);
            return string.Format("({0})", _propertyStr);
        }
        public string CombindParamsStr(List<string> parameters)
        {
            string _propertyStr = string.Empty;
            _propertyStr = string.Join(",", parameters);
            return "(" + _propertyStr + ")";
        }

        public bool AddCommend(string connStr, string sqlStr, ref Dictionary<string, List<Dictionary<string, object>>> cmds, Dictionary<string, object> slParams)
        {
            try
            {
                if (connStr == string.Empty)
                    return false;
                List<Dictionary<string, object>> _paramList = new List<Dictionary<string, object>>();
                _paramList.Add(slParams);
                if (cmds.Keys.Contains(sqlStr))
                    cmds[sqlStr].Add(slParams);
                else
                    cmds.Add(sqlStr, _paramList);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool AddCommend(string connStr, string sqlStr, ref Dictionary<string, List<Dictionary<string, object>>> cmds, List<Dictionary<string, object>> slParams)
        {
            try
            {
                if (connStr == string.Empty)
                    return false;
                if (cmds.Keys.Contains(sqlStr))
                {
                    cmds[sqlStr].AddRange(slParams);
                }
                else
                    cmds.Add(sqlStr, slParams);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
    }
}
