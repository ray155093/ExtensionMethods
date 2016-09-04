using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Richi.Library.Factory;

namespace Richi.Library.ADO
{
    public class MSSQL
    {
        private string _connStr = string.Empty;
        private Common _common = new Common();
        private Dictionary<string, List<Dictionary<string, object>>> _cmds;
        private int _timeout = 60000;

        #region -- Constructor --
        public MSSQL(string connStr)
        {
            _cmds = new Dictionary<string, List<Dictionary<string, object>>>();
            this._connStr = connStr;
        }
        [InjectionConstrurctor]
        public MSSQL(IEncryption encryption)
        {
            _cmds = new Dictionary<string, List<Dictionary<string, object>>>();

            string _conn = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString;
            this._connStr = encryption.Decrypt(_conn);
        }
        public MSSQL(string A1, IEncryption encryption, string connTag)
        {
            _cmds = new Dictionary<string, List<Dictionary<string, object>>>();

            string _conn = System.Web.Configuration.WebConfigurationManager.ConnectionStrings[connTag].ConnectionString;
            this._connStr = encryption.Decrypt(_conn);
        }
        #endregion
        public string setTimeout(int timeout)
        {
            try
            {
                this._timeout = timeout;
                return "0";
            }
            catch (Exception ex)
            {
                return ex.InnerException.Message;
            }
        }
        #region -- Select --
        public List<T> Select<T>(string sqlStr, Dictionary<string, object> slParams)
        {
            if (this._connStr == string.Empty)
                return null;
            SqlConnection Conn = new SqlConnection(this._connStr);

            SqlCommand cmd = new SqlCommand(sqlStr, Conn);
            cmd.CommandTimeout = this._timeout;
            if (slParams != null)
                _common.SqlParameterSet(ref cmd, slParams);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataSet Ds = new DataSet();
            Adpt.Fill(Ds);

            List<T> _tList = new List<T>();

            for (int i = 0; i < Ds.Tables[0].Rows.Count; i++)
            {
                var _tmpObject = Activator.CreateInstance<T>();

                PropertyInfo[] _propertys = _tmpObject.GetType().GetProperties();
                foreach (var _property in _propertys)
                {
                    if (Ds.Tables[0].Columns.Contains(_property.Name))
                    {
                        var _tmp = Ds.Tables[0].Rows[i][_property.Name];
                        if (_tmp is DBNull)
                            _tmpObject.GetType()
                                   .GetProperty(_property.Name)
                                   .SetValue(_tmpObject, null, null);
                        else
                        {
                            if (_property.PropertyType.IsGenericType)
                                _tmpObject.GetType().GetProperty(_property.Name)
                                                    .SetValue(_tmpObject,
                                                              Convert.ChangeType(_tmp, _property.PropertyType.GetGenericArguments()[0]),
                                                              null);
                            else
                                _tmpObject.GetType()
                                  .GetProperty(_property.Name)
                                  .SetValue(_tmpObject,
                                      Convert.ChangeType(_tmp, _property.PropertyType),
                                      null);
                        }
                    }
                }
                if (i > Ds.Tables[0].Rows.Count / 10 && i % 20000 == 0)
                    GC.Collect();
                _tList.Add(_tmpObject);
            }
            Ds.Dispose();
            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            GC.Collect();
            return _tList;
        }
        public List<T> Select<T>(string sqlStr) where T : class
        {

            string[] _opations = new string[] { "=", ">=", "=>", "<=", "=<", "<>", ">", "<" };
            T _tmpObject = Activator.CreateInstance<T>();
            List<T> _reList = new List<T>();
            Dictionary<string, object> _params = new Dictionary<string, object>();
            Dictionary<int, string> _operationRecord = new Dictionary<int, string>();
            Dictionary<string, string> _inParams = new Dictionary<string, string>();
            List<string> _paramPrimalStrs = new List<string>();
            List<string> _paramNewStrs = new List<string>();
            List<List<string>> _paramValues = new List<List<string>>();

            string[] _sqlSplit = sqlStr.Split(' ');
            for (int i = 0; i < _sqlSplit.Length; i++)
            {
                #region -- Process "=" --
                if (_opations.Contains(_sqlSplit[i]))
                {
                    if (_sqlSplit[i + 1].Substring(0, 1) != "@")
                    {
                        _operationRecord.Add(i, "=");
                        _paramNewStrs.Add(GetRandomString(5));
                        _paramPrimalStrs.Add(_sqlSplit[i - 1]);
                        List<string> _tmpValue = new List<string>();
                        string _tmpStr = _sqlSplit[i + 1];
                        if (_tmpStr.Substring(0, 1) == "'")
                            _tmpStr = _tmpStr.Substring(1, _tmpStr.Length - 2);
                        _tmpValue.Add(_tmpStr);
                        _paramValues.Add(_tmpValue);
                        _sqlSplit[i + 1] = string.Format("@{0}", _paramNewStrs[_paramNewStrs.Count - 1]);
                    }
                    else
                    {
                        _sqlSplit[i + 1] = _sqlSplit[i + 1].Substring(1, _sqlSplit[i + 1].Length - 1);
                    }
                }
                #endregion
                #region -- Process "LIKE" --
                if (_sqlSplit[i].ToUpper() == "LIKE")
                {
                    _operationRecord.Add(i, "LIKE");
                    _paramNewStrs.Add(GetRandomString(5));

                    //判斷前後是否有"'"
                    string _tmpStr = _sqlSplit[i + 1];
                    if (_tmpStr.Substring(0, 1) == "'")
                        _tmpStr = _tmpStr.Substring(1, _tmpStr.Length - 2);
                    //判斷前後是否有"%"，如果有一定在第一個字元或最後一個字元
                    int _startIndex = _tmpStr.IndexOf('%');
                    int _endIndex = _tmpStr.LastIndexOf('%');
                    if (_startIndex != 0)
                        _startIndex = -1;
                    if (_endIndex != _tmpStr.Length)
                        _endIndex = -1;

                    _paramPrimalStrs.Add(_sqlSplit[i - 1]);
                    List<string> _tmpValue = new List<string>();
                    _tmpValue.Add(_tmpStr);
                    _paramValues.Add(_tmpValue);

                    _sqlSplit[i + 1] = string.Format("@{0}", _paramNewStrs[_paramNewStrs.Count - 1]);
                }
                #endregion

                //#region -- Process "IN" --
                if (_sqlSplit[i].ToUpper() == "IN")
                {
                    //if (_sqlSplit[i + 1].Substring(0, 1) != "@")
                    //{

                    //    //找出IN作用範圍=>(a,b,c,d)
                    //    //IN關鍵字下一個開始
                    //    for (int j = i + 1; j < _sqlSplit.Length; j++)
                    //    {
                    //        if (_sqlSplit[j] == "(")
                    //        {
                    //            continue;
                    //        }
                    //        if (_sqlSplit[j] == " ")
                    //        {
                    //            continue;
                    //        }
                    //        if (_sqlSplit[j] == ",")
                    //        {
                    //            continue;
                    //        }
                    //        //結束處理
                    //        if (_sqlSplit[j] == ")")
                    //        {
                    //            break;
                    //        }
                    //        //目標物
                    //        _operationRecord.Add(i, "IN");
                    //        string[] _values = _sqlSplit[j].Split(',');
                    //        List<string> _parameTag = new List<string>();
                    //        List<string> _tmpValue = new List<string>();
                    //        foreach (var item in _values)
                    //        {
                    //            string _newStr = GetRandomString(5);
                    //            _parameTag.Add("@" + _newStr);

                    //            _paramNewStrs.Add(_newStr);
                    //            string _tmpStr = item;
                    //            if (_tmpStr.Substring(0, 1) == "'")
                    //                _tmpStr = _tmpStr.Substring(1, _tmpStr.Length - 2);
                    //            _tmpValue.Add(_tmpStr);
                    //        }
                    //        _paramValues.Add(_tmpValue);

                    //        _paramPrimalStrs.Add(_sqlSplit[i - 1]);
                    //        _sqlSplit[j] = string.Format("{0}", string.Join(",", _parameTag));
                    //        break;
                    //    }
                    //}
                    //else
                    //{
                    //    //找出IN作用範圍=>(a,b,c,d)
                    //    //IN關鍵字下一個開始
                    //    for (int j = i + 1; j < _sqlSplit.Length; j++)
                    //    {
                    //        //目標物
                    //        _operationRecord.Add(i, "IN");
                    //        string[] _values = _sqlSplit[j].Split(',');
                    //        List<string> _parameTag = new List<string>();
                    //        //取消_param 作法，避免IN失敗 by Chad 2015/10/15
                    //        foreach (var item in _values)
                    //        {
                    //            string _newStr = item.Split('@')[1].ToString();
                    //            _parameTag.Add(_newStr);
                    //        }
                    //        _sqlSplit[j] = string.Format("{0}", string.Join(",", _parameTag));
                    //        break;
                    //    }
                    //}
                    #region -- Old Process "IN" --
                    //    if (_sqlSplit[i + 1].Substring(0, 1) != "@")
                    //    {
                    //        for (int j = 0; j < _sqlSplit.Length; j++)
                    //        {
                    //            if (_sqlSplit[j].Substring(_sqlSplit[j].Length - 1, 1) == ",")
                    //            {
                    //                _sqlSplit[j] = _sqlSplit[j] + _sqlSplit[j + 1];
                    //                _sqlSplit[j + 1] = "";
                    //                _sqlSplit = ChangeContent(_sqlSplit);
                    //            }
                    //            if (_sqlSplit[j].Substring(_sqlSplit[j].Length - 1, 1) == ")" && j != _sqlSplit.Length - 1)
                    //            {
                    //                _sqlSplit[j - 1] = _sqlSplit[j - 1] + _sqlSplit[j];
                    //                _sqlSplit[j] = "";
                    //                _sqlSplit = ChangeContent(_sqlSplit);
                    //            }
                    //        }
                    //        _operationRecord.Add(i, "IN");
                    //        string[] _values = _sqlSplit[i + 1].Substring(1, _sqlSplit[i + 1].Length - 2).Split(',');
                    //        List<string> _parameTag = new List<string>();
                    //        List<string> _tmpValue = new List<string>();
                    //        foreach (var item in _values)
                    //        {
                    //            string _newStr = GetRandomString(5);
                    //            _parameTag.Add("@" + _newStr);
                    //            _paramNewStrs.Add(_newStr);
                    //            string _tmpStr = item;
                    //            if (_tmpStr.Substring(0, 1) == "'")
                    //                _tmpStr = _tmpStr.Substring(1, _tmpStr.Length - 2);
                    //            _tmpValue.Add(_tmpStr);
                    //        }
                    //        _paramValues.Add(_tmpValue);

                    //        _paramPrimalStrs.Add(_sqlSplit[i - 1]);
                    //        _sqlSplit[i + 1] = string.Format("({0})", string.Join(",", _parameTag));
                    //    }
                    //    else
                    //    {
                    //        _sqlSplit[i + 1] = _sqlSplit[i + 1].Substring(1, _sqlSplit[i + 1].Length - 1);
                    //    }
                    #endregion
                }
                //#endregion
            }
            //Create Sql Command string
            sqlStr = string.Join(" ", _sqlSplit);

            //Create sql command parameter
            int _paramValuesIndex = 0;
            int _paramNewStrsIndex = 0;
            foreach (var item in _paramPrimalStrs)
            {
                PropertyInfo _propertyInfo = _tmpObject.GetType().GetProperties().Where(p => p.Name.ToUpper() == item.ToUpper()).FirstOrDefault();
                Type _type = _propertyInfo != null ? _propertyInfo.PropertyType : typeof(string);
                object _tmpValue;
                if (_type.Name == "Guid")
                {
                    _tmpValue = new Guid(_paramValues[_paramValuesIndex].FirstOrDefault());
                    _params.Add(string.Format("@{0}", _paramNewStrs[_paramNewStrsIndex]), new MSParameters(_tmpValue, _type));
                    _paramValuesIndex++;
                    _paramNewStrsIndex++;
                }
                else
                {
                    if (_paramValues[_paramValuesIndex].Count > 1)
                    {
                        foreach (var value in _paramValues[_paramValuesIndex])
                        {
                            _tmpValue = Convert.ChangeType(value, _type);
                            _params.Add(string.Format("@{0}", _paramNewStrs[_paramNewStrsIndex]), new MSParameters(_tmpValue, _type));
                            _paramNewStrsIndex++;
                        }
                        _paramValuesIndex++;
                    }
                    else
                    {
                        _tmpValue = Convert.ChangeType(_paramValues[_paramValuesIndex].FirstOrDefault(), _type);
                        _params.Add(string.Format("@{0}", _paramNewStrs[_paramNewStrsIndex]), new MSParameters(_tmpValue, _type));
                        _paramValuesIndex++;
                        _paramNewStrsIndex++;
                    }
                }
            }

            SqlConnection Conn = new SqlConnection(this._connStr);

            SqlCommand cmd = new SqlCommand(sqlStr, Conn);
            cmd.CommandTimeout = this._timeout;
            if (_params != null)
                _common.SqlParameterSet(ref cmd, _params);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataSet Ds = new DataSet();
            Adpt.Fill(Ds);

            for (int i = 0; i < Ds.Tables[0].Rows.Count; i++)
            {
                _tmpObject = Activator.CreateInstance<T>();
                PropertyInfo[] _propertys = _tmpObject.GetType().GetProperties();
                foreach (var _property in _propertys)
                {
                    if (Ds.Tables[0].Columns.Contains(_property.Name))
                    {
                        var _tmp = Ds.Tables[0].Rows[i][_property.Name];
                        if (_tmp is DBNull)
                            _tmpObject.GetType()
                                   .GetProperty(_property.Name)
                                   .SetValue(_tmpObject, null, null);
                        else
                        {
                            if (_property.PropertyType.IsGenericType)
                                _tmpObject.GetType().GetProperty(_property.Name)
                                                    .SetValue(_tmpObject,
                                                              Convert.ChangeType(_tmp, _property.PropertyType.GetGenericArguments()[0]),
                                                              null);
                            else
                                _tmpObject.GetType()
                                  .GetProperty(_property.Name)
                                  .SetValue(_tmpObject,
                                      Convert.ChangeType(_tmp, _property.PropertyType),
                                      null);
                        }
                    }
                }
                _reList.Add(_tmpObject);
            }

            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            return _reList;
        }
        public DataTable Select_Table(string sqlStr, Dictionary<string, object> slParams)
        {
            if (this._connStr == string.Empty)
                return null;
            SqlConnection Conn = new SqlConnection(this._connStr);

            SqlCommand cmd = new SqlCommand(sqlStr, Conn);
            cmd.CommandTimeout = this._timeout;
            if (slParams != null)
                _common.SqlParameterSet(ref cmd, slParams);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataSet Ds = new DataSet();
            Adpt.Fill(Ds);


            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            return Ds.Tables[0];
        }
        #endregion
        #region -- Insert --
        public bool Insert(string sqlStr, Dictionary<string, object> slParams)
        {
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, slParams);
        }
        public bool Insert(string sqlStr, List<Dictionary<string, object>> slParams)
        {
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, slParams);
        }
        public bool Insert(string tableName, string[] tableProperty, Dictionary<string, object> slParams)
        {
            if (this._connStr == string.Empty)
                return false;
            string _sqlStr = string.Format("Insert INTO {0} {1} VALUES {2}",
                                           tableName,
                                           _common.CombindTablePropertyStr(tableProperty),
                                           _common.CombindParamsStr(slParams.Keys.ToList()));
            List<Dictionary<string, object>> _paramList = new List<Dictionary<string, object>>();
            _paramList.Add(slParams);
            if (_cmds.Keys.Contains(_sqlStr))
            {
                _cmds[_sqlStr].Add(slParams);
            }
            else
                _cmds.Add(_sqlStr, _paramList);
            return true;
        }
        public bool Insert(string tableName, string[] tableProperty, List<Dictionary<string, object>> slParams)
        {
            if (this._connStr == string.Empty)
                return false;
            foreach (var item in slParams)
            {
                string _sqlStr = string.Format("Insert INTO {0} {1} VALUES {1}",
                                               _common.CombindTablePropertyStr(tableProperty),
                                               _common.CombindParamsStr(item.Keys.ToList()));
                if (_cmds.Keys.Contains(_sqlStr))
                {
                    _cmds[_sqlStr].AddRange(slParams);
                }
                else
                    _cmds.Add(_sqlStr, slParams);
            }
            return true;
        }
        public bool Insert<T>(string tableName, T saveObj)
        {
            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                if (_value != null && !_type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                else if (_value != null && _type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                else if (_value == null && _type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                else if (_value == null && !_type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(T saveObj)
        {
            string _sqlStr = string.Format("Insert INTO {0} (", saveObj.GetType().Name);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                if (_value != null && !_type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                else if (_value != null && _type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                else if (_value == null && _type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                else if (_value == null && !_type.IsGenericType)
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(string tableName, List<T> saveObjs)
        {
            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            T _saveObj = saveObjs[0];
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                    var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(List<T> saveObjs)
        {
            T _saveObj = saveObjs[0];
            string _sqlStr = string.Format("Insert INTO {0} (", _saveObj.GetType().Name);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                    var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(string tableName, List<string> excludeProperty, T saveObj)
        {
            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                    var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                    var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value != null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(string tableName, List<string> excludeProperty, List<T> saveObjs)
        {
            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            T _saveObj = saveObjs[0];
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                }
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    if (excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                    {
                        var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                        var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                        if (_value != null)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        else if (_value == null && _type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        else if (_value == null && !_type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(List<string> excludeProperty, T saveObj)
        {
            string _sqlStr = string.Format("Insert INTO {0} (", saveObj.GetType().Name);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                    var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                    var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value != null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(List<string> excludeProperty, List<T> saveObjs)
        {
            T _saveObj = saveObjs[0];
            string _sqlStr = string.Format("Insert INTO {0} (", _saveObj.GetType().Name);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                }
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    if (excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                    {
                        var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                        var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                        if (_value != null)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        else if (_value == null && _type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        else if (_value == null && !_type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(string tableName, Expression<Func<T, object>> excludeProperty, T saveObj)
        {
            List<Expression<Func<T, object>>> _tmpExclude = new List<Expression<Func<T, object>>>() { excludeProperty };
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpExclude });

            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                    var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                    var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value != null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(string tableName, Expression<Func<T, object>> excludeProperty, List<T> saveObjs)
        {
            List<Expression<Func<T, object>>> _tmpExclude = new List<Expression<Func<T, object>>>() { excludeProperty };
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObjs.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpExclude });

            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            T _saveObj = saveObjs[0];
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                }
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                    {
                        var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                        var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                        if (_value != null)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        else if (_value == null && _type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        else if (_value == null && !_type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(string tableName, List<Expression<Func<T, object>>> excludeProperty, T saveObj)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { excludeProperty });

            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                    var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                    var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value != null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(string tableName, List<Expression<Func<T, object>>> excludeProperty, List<T> saveObjs)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObjs.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { excludeProperty });

            string _sqlStr = string.Format("Insert INTO {0} (", tableName);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            T _saveObj = saveObjs[0];
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                }
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                    {
                        var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                        var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                        if (_value != null)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        else if (_value == null && _type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        else if (_value == null && !_type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(Expression<Func<T, object>> excludeProperty, T saveObj)
        {
            List<Expression<Func<T, object>>> _tmpExclude = new List<Expression<Func<T, object>>>() { excludeProperty };
            MethodInfo _method = this.GetType().GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpExclude });

            string _sqlStr = string.Format("Insert INTO {0} (", saveObj.GetType().Name);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                    var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                    var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value != null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(Expression<Func<T, object>> excludeProperty, List<T> saveObjs)
        {
            List<Expression<Func<T, object>>> _tmpExclude = new List<Expression<Func<T, object>>>() { excludeProperty };
            MethodInfo _method = this.GetType().GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObjs.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpExclude });

            T _saveObj = saveObjs[0];
            string _sqlStr = string.Format("Insert INTO {0} (", _saveObj.GetType().Name);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                }
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                    {
                        var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                        var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                        if (_value != null)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        else if (_value == null && _type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        else if (_value == null && !_type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool Insert<T>(List<Expression<Func<T, object>>> excludeProperty, T saveObj)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { excludeProperty });

            string _sqlStr = string.Format("Insert INTO {0} (", saveObj.GetType().Name);
            string _paramsStr = "(";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            PropertyInfo[] objInfo = saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                    var _value = saveObj.GetType().GetProperty(item.Name).GetValue(saveObj, null);
                    var _type = saveObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    else if (_value != null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    else if (_value == null && _type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    else if (_value == null && !_type.IsGenericType)
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Insert<T>(List<Expression<Func<T, object>>> excludeProperty, List<T> saveObjs)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { saveObjs.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { excludeProperty });

            T _saveObj = saveObjs[0];
            string _sqlStr = string.Format("Insert INTO {0} (", _saveObj.GetType().Name);
            string _paramsStr = "(";
            List<Dictionary<string, object>> _slParamDiary = new List<Dictionary<string, object>>();
            Dictionary<string, object> _slParams;
            PropertyInfo[] objInfo = _saveObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                {
                    _sqlStr = string.Format("{0}{1},", _sqlStr, item.Name);
                    _paramsStr = string.Format("{0}@{1},", _paramsStr, item.Name);
                }
            }
            foreach (var obj in saveObjs)
            {
                _slParams = new Dictionary<string, object>();
                foreach (var item in objInfo)
                {
                    if (_excludeProperty.Where(p => p.ToUpper() == item.Name.ToUpper()).Count() == 0)
                    {
                        var _value = obj.GetType().GetProperty(item.Name).GetValue(obj, null);
                        var _type = obj.GetType().GetProperty(item.Name).PropertyType;
                        if (_value != null)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        else if (_value == null && _type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        else if (_value == null && !_type.IsGenericType)
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                }
                _slParamDiary.Add(_slParams);
            }
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 1);
            _paramsStr = string.Format("{0})", _paramsStr);
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            _sqlStr = string.Format("{0}) values {1}", _sqlStr, _paramsStr);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParamDiary);
            return true;
        }
        public bool BulkCopy<T>(List<T> sabeObjs)
        {
            T _souceObj = Activator.CreateInstance<T>();
            PropertyInfo[] _sPropertys = _souceObj.GetType().GetProperties();
            DataTable _dt = new DataTable();
            foreach (var property in _sPropertys)
            {
                if (property.PropertyType.IsGenericType)
                    _dt.Columns.Add(property.Name.ToUpper(), property.PropertyType.GetGenericArguments()[0]);
                else
                    _dt.Columns.Add(property.Name.ToUpper(), property.PropertyType);
            }
            foreach (var tmpObj in sabeObjs)
            {
                DataRow _dr = _dt.NewRow();
                foreach (var property in _sPropertys)
                {
                    var _value = tmpObj.GetType().GetProperty(property.Name).GetValue(tmpObj, null);
                    if (property.PropertyType.IsGenericType && _value == null)
                        _dr[property.Name] = DBNull.Value;
                    else if (property.PropertyType.IsGenericType && _value != null)
                        _dr[property.Name] = Convert.ChangeType(_value, property.PropertyType.GetGenericArguments()[0]);
                    else
                        _dr[property.Name] = Convert.ChangeType(_value, property.PropertyType);
                }
                _dt.Rows.Add(_dr);
            }
            using (SqlBulkCopy _sqlBulkCopy = new SqlBulkCopy(_connStr))
            {
                _sqlBulkCopy.BatchSize = sabeObjs.Count;
                _sqlBulkCopy.BulkCopyTimeout = 120;

                _sqlBulkCopy.NotifyAfter = sabeObjs.Count;
                //_sqlBulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnSqlRowsCopied);
                _sqlBulkCopy.DestinationTableName = string.Format("dbo.{0}", _souceObj.GetType().Name);

                //對應資料行
                foreach (var property in _sPropertys)
                {
                    _sqlBulkCopy.ColumnMappings.Add(property.Name, property.Name);
                }
                //開始寫入
                _sqlBulkCopy.WriteToServer(_dt);
            }
            return true;
        }
        public bool BulkCopy<T>(string tableName, List<T> sabeObjs)
        {
            T _souceObj = Activator.CreateInstance<T>();
            PropertyInfo[] _sPropertys = _souceObj.GetType().GetProperties();
            DataTable _dt = new DataTable();
            foreach (var property in _sPropertys)
            {
                if (property.PropertyType.IsGenericType)
                    _dt.Columns.Add(property.Name.ToUpper(), property.PropertyType.GetGenericArguments()[0]);
                else
                    _dt.Columns.Add(property.Name.ToUpper(), property.PropertyType);
            }
            foreach (var tmpObj in sabeObjs)
            {
                DataRow _dr = _dt.NewRow();
                foreach (var property in _sPropertys)
                {
                    var _value = tmpObj.GetType().GetProperty(property.Name).GetValue(tmpObj, null);
                    if (property.PropertyType.IsGenericType && _value == null)
                        _dr[property.Name] = DBNull.Value;
                    else if (property.PropertyType.IsGenericType && _value != null)
                        _dr[property.Name] = Convert.ChangeType(_value, property.PropertyType.GetGenericArguments()[0]);
                    else
                        _dr[property.Name] = Convert.ChangeType(_value, property.PropertyType);
                }
                _dt.Rows.Add(_dr);
            }
            using (SqlBulkCopy _sqlBulkCopy = new SqlBulkCopy(_connStr))
            {
                _sqlBulkCopy.BatchSize = sabeObjs.Count;
                _sqlBulkCopy.BulkCopyTimeout = 120;

                _sqlBulkCopy.NotifyAfter = sabeObjs.Count;
                //_sqlBulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(OnSqlRowsCopied);
                _sqlBulkCopy.DestinationTableName = tableName;

                //對應資料行
                foreach (var property in _sPropertys)
                {
                    _sqlBulkCopy.ColumnMappings.Add(property.Name, property.Name);
                }
                //開始寫入
                _sqlBulkCopy.WriteToServer(_dt);
            }
            return true;
        }
        #endregion
        #region -- Update --
        public bool Update(string sqlStr, Dictionary<string, object> slParams)
        {
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, slParams);
        }
        public bool Update(string sqlStr, List<Dictionary<string, object>> slParams)
        {
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, slParams);
        }
        public bool Update<T>(string tableName, T updateObj, List<string> conditionProperty)
        {
            for (int i = 0; i < conditionProperty.Count; i++)
                conditionProperty[i] = conditionProperty[i].ToUpper();

            string _sqlStr = string.Format("Update {0} SET ", tableName);
            string _paramsStr = " where ";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            string _conditionPName = "";
            object _conditionPObj = null;
            PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                if (_value != null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                }
                else if (_value != null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
                if (conditionProperty.Contains(item.Name.ToUpper()))
                {
                    _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                    _conditionParams.Add(_conditionPName, _conditionPObj);
                }
            }
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            //去掉最後一個" and "
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
            _sqlStr = _sqlStr + _paramsStr;
            foreach (var item in _conditionParams)
                _slParams.Add(item.Key, item.Value);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Update<T>(string tableName, T updateObj, Expression<Func<T, object>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpCondition = new List<Expression<Func<T, object>>>() { conditionProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { _tmpCondition });

            for (int i = 0; i < _conditionProperty.Count; i++)
                _conditionProperty[i] = _conditionProperty[i].ToUpper();

            string _sqlStr = string.Format("Update {0} SET ", tableName);
            string _paramsStr = " where ";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            string _conditionPName = "";
            object _conditionPObj = null;
            PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                if (_value != null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                }
                else if (_value != null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
                if (_conditionProperty.Contains(item.Name.ToUpper()))
                {
                    _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                    _conditionParams.Add(_conditionPName, _conditionPObj);
                }
            }
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            //去掉最後一個" and "
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
            _sqlStr = _sqlStr + _paramsStr;
            foreach (var item in _conditionParams)
                _slParams.Add(item.Key, item.Value);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Update<T>(string tableName, T updateObj, List<Expression<Func<T, object>>> conditionProperty)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { conditionProperty });

            for (int i = 0; i < _conditionProperty.Count; i++)
                _conditionProperty[i] = _conditionProperty[i].ToUpper();

            string _sqlStr = string.Format("Update {0} SET ", tableName);
            string _paramsStr = " where ";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            string _conditionPName = "";
            object _conditionPObj = null;
            PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                if (_value != null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                }
                else if (_value != null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
                if (_conditionProperty.Contains(item.Name.ToUpper()))
                {
                    _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                    _conditionParams.Add(_conditionPName, _conditionPObj);
                }
            }
            _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            //去掉最後一個" and "
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
            _sqlStr = _sqlStr + _paramsStr;
            foreach (var item in _conditionParams)
                _slParams.Add(item.Key, item.Value);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool UpdateExclude<T>(string tableName, T updateObj, List<string> excludeProperty, List<string> conditionProperty)
        {
            try
            {
                for (int i = 0; i < conditionProperty.Count; i++)
                    conditionProperty[i] = conditionProperty[i].ToUpper();

                for (int i = 0; i < excludeProperty.Count; i++)
                    excludeProperty[i] = excludeProperty[i].ToUpper();

                string _sqlStr = string.Format("Update {0} SET ", tableName);
                string _paramsStr = " where ";
                Dictionary<string, object> _slParams = new Dictionary<string, object>();
                Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
                string _conditionPName = "";
                object _conditionPObj = null;
                PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
                foreach (var item in objInfo)
                {
                    var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                    var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                    if (!excludeProperty.Contains(item.Name.ToUpper()))
                    {
                        _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                        if (_value != null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        }
                        else if (_value != null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                        }
                    }
                    if (conditionProperty.Contains(item.Name.ToUpper()))
                    {
                        _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _conditionParams.Add(_conditionPName, _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                //去掉最後一個" and "
                _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
                _sqlStr = _sqlStr + _paramsStr;
                foreach (var item in _conditionParams)
                    _slParams.Add(item.Key, item.Value);
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public bool UpdateExclude<T>(string tableName, T updateObj, Expression<Func<T, object>> excludeProperty, Expression<Func<T, object>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpExclude = new List<Expression<Func<T, object>>>() { excludeProperty };
            List<Expression<Func<T, object>>> _tmpCondition = new List<Expression<Func<T, object>>>() { conditionProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpExclude });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { _tmpCondition });

            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                for (int i = 0; i < _excludeProperty.Count; i++)
                    _excludeProperty[i] = _excludeProperty[i].ToUpper();

                string _sqlStr = string.Format("Update {0} SET ", tableName);
                string _paramsStr = " where ";
                Dictionary<string, object> _slParams = new Dictionary<string, object>();
                Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
                string _conditionPName = "";
                object _conditionPObj = null;
                PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
                foreach (var item in objInfo)
                {
                    var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                    var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                    if (!_excludeProperty.Contains(item.Name.ToUpper()))
                    {
                        _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                        if (_value != null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        }
                        else if (_value != null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                        }
                    }
                    if (_conditionProperty.Contains(item.Name.ToUpper()))
                    {
                        _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _conditionParams.Add(_conditionPName, _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                //去掉最後一個" and "
                _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
                _sqlStr = _sqlStr + _paramsStr;
                foreach (var item in _conditionParams)
                    _slParams.Add(item.Key, item.Value);
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateExclude<T>(string tableName, T updateObj, Expression<Func<T, object>> excludeProperty, List<Expression<Func<T, object>>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpExclude = new List<Expression<Func<T, object>>>() { excludeProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpExclude });

            _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { conditionProperty });

            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                for (int i = 0; i < _excludeProperty.Count; i++)
                    _excludeProperty[i] = _excludeProperty[i].ToUpper();

                string _sqlStr = string.Format("Update {0} SET ", tableName);
                string _paramsStr = " where ";
                Dictionary<string, object> _slParams = new Dictionary<string, object>();
                Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
                string _conditionPName = "";
                object _conditionPObj = null;
                PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
                foreach (var item in objInfo)
                {
                    var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                    var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                    if (!_excludeProperty.Contains(item.Name.ToUpper()))
                    {
                        _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                        if (_value != null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        }
                        else if (_value != null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                        }
                    }
                    if (_conditionProperty.Contains(item.Name.ToUpper()))
                    {
                        _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _conditionParams.Add(_conditionPName, _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                //去掉最後一個" and "
                _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
                _sqlStr = _sqlStr + _paramsStr;
                foreach (var item in _conditionParams)
                    _slParams.Add(item.Key, item.Value);
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateExclude<T>(string tableName, T updateObj, List<Expression<Func<T, object>>> excludeProperty, Expression<Func<T, object>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpcondition = new List<Expression<Func<T, object>>>() { conditionProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { excludeProperty });

            _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { _tmpcondition });

            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                for (int i = 0; i < _excludeProperty.Count; i++)
                    _excludeProperty[i] = _excludeProperty[i].ToUpper();

                string _sqlStr = string.Format("Update {0} SET ", tableName);
                string _paramsStr = " where ";
                Dictionary<string, object> _slParams = new Dictionary<string, object>();
                Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
                string _conditionPName = "";
                object _conditionPObj = null;
                PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
                foreach (var item in objInfo)
                {
                    var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                    var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                    if (!_excludeProperty.Contains(item.Name.ToUpper()))
                    {
                        _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                        if (_value != null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        }
                        else if (_value != null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                        }
                    }
                    if (_conditionProperty.Contains(item.Name.ToUpper()))
                    {
                        _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _conditionParams.Add(_conditionPName, _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                //去掉最後一個" and "
                _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
                _sqlStr = _sqlStr + _paramsStr;
                foreach (var item in _conditionParams)
                    _slParams.Add(item.Key, item.Value);
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateExclude<T>(string tableName, T updateObj, List<Expression<Func<T, object>>> excludeProperty, List<Expression<Func<T, object>>> conditionProperty)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _excludeProperty = (List<string>)_method.Invoke(this, new object[] { excludeProperty });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { conditionProperty });

            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                for (int i = 0; i < _excludeProperty.Count; i++)
                    _excludeProperty[i] = _excludeProperty[i].ToUpper();

                string _sqlStr = string.Format("Update {0} SET ", tableName);
                string _paramsStr = " where ";
                Dictionary<string, object> _slParams = new Dictionary<string, object>();
                Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
                string _conditionPName = "";
                object _conditionPObj = null;
                PropertyInfo[] objInfo = updateObj.GetType().GetProperties();
                foreach (var item in objInfo)
                {
                    var _value = updateObj.GetType().GetProperty(item.Name).GetValue(updateObj, null);
                    var _type = updateObj.GetType().GetProperty(item.Name).PropertyType;
                    if (!_excludeProperty.Contains(item.Name.ToUpper()))
                    {
                        _sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                        if (_value != null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                        }
                        else if (_value != null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && _type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                        }
                        else if (_value == null && !_type.IsGenericType)
                        {
                            _conditionPName = string.Format("@C_{0}", item.Name);
                            _conditionPObj = new object();
                            _conditionPObj = new MSParameters(_value, _type);
                            _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                        }
                    }
                    if (_conditionProperty.Contains(item.Name.ToUpper()))
                    {
                        _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _conditionParams.Add(_conditionPName, _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                //去掉最後一個" and "
                _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
                _sqlStr = _sqlStr + _paramsStr;
                foreach (var item in _conditionParams)
                    _slParams.Add(item.Key, item.Value);
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UpdateInclude<T>(string tableName, T updateObj, List<string> includeProperty, List<string> conditionProperty)
        {
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            object _conditionPObj = null;
            string _sqlStr = "Update " + tableName + " set ";
            string _paramsStr = " where ";
            try
            {
                for (int i = 0; i < conditionProperty.Count; i++)
                    conditionProperty[i] = conditionProperty[i].ToUpper();

                //string _sqlStr = string.Format("Update {0} SET ", tableName);
                foreach (var item in includeProperty)
                {
                    _sqlStr += item + " = @" + item + ",";
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    if (_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null),
                                                          _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                    else
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null), _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                List<string> _tmpWhere = new List<string>();
                string _randonStr;
                foreach (var item in conditionProperty)
                {
                    _randonStr = GetRandomString(6);
                    _tmpWhere.Add(item + " = @" + _randonStr);
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    object _value = _property.GetValue(updateObj, null);
                    if (_value != null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value != null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                }
                _sqlStr += _paramsStr + string.Join(" and ", _tmpWhere.ToArray());
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public bool UpdateInclude<T>(string tableName, T updateObj, Expression<Func<T, object>> includeProperty, Expression<Func<T, object>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpInclude = new List<Expression<Func<T, object>>>() { includeProperty };
            List<Expression<Func<T, object>>> _tmpCondition = new List<Expression<Func<T, object>>>() { conditionProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _includeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpInclude });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { _tmpCondition });

            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            object _conditionPObj = null;
            string _sqlStr = "Update " + tableName + " set ";
            string _paramsStr = " where ";
            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                //string _sqlStr = string.Format("Update {0} SET ", tableName);
                foreach (var item in _includeProperty)
                {
                    _sqlStr += item + " = @" + item + ",";
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    if (_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null),
                                                          _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                    else
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null), _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                List<string> _tmpWhere = new List<string>();
                string _randonStr;
                foreach (var item in _conditionProperty)
                {
                    _randonStr = GetRandomString(6);
                    _tmpWhere.Add(item + " = @" + _randonStr);
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    object _value = _property.GetValue(updateObj, null);
                    if (_value != null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value != null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                }
                _sqlStr += _paramsStr + string.Join(" and ", _tmpWhere.ToArray());
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public bool UpdateInclude<T>(string tableName, T updateObj, Expression<Func<T, object>> includeProperty, List<Expression<Func<T, object>>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpInclude = new List<Expression<Func<T, object>>>() { includeProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _includeProperty = (List<string>)_method.Invoke(this, new object[] { _tmpInclude });

            _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { conditionProperty });

            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            object _conditionPObj = null;
            string _sqlStr = "Update " + tableName + " set ";
            string _paramsStr = " where ";
            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                //string _sqlStr = string.Format("Update {0} SET ", tableName);
                foreach (var item in _includeProperty)
                {
                    _sqlStr += item + " = @" + item + ",";
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    if (_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null),
                                                          _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                    else
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null), _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                List<string> _tmpWhere = new List<string>();
                string _randonStr;
                foreach (var item in _conditionProperty)
                {
                    _randonStr = GetRandomString(6);
                    _tmpWhere.Add(item + " = @" + _randonStr);
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    object _value = _property.GetValue(updateObj, null);
                    if (_value != null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value != null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                }
                _sqlStr += _paramsStr + string.Join(" and ", _tmpWhere.ToArray());
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public bool UpdateInclude<T>(string tableName, T updateObj, List<Expression<Func<T, object>>> includeProperty, Expression<Func<T, object>> conditionProperty)
        {
            List<Expression<Func<T, object>>> _tmpCondition = new List<Expression<Func<T, object>>>() { conditionProperty };

            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _includeProperty = (List<string>)_method.Invoke(this, new object[] { includeProperty });

            _method = this.GetType().GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { _tmpCondition });

            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            object _conditionPObj = null;
            string _sqlStr = "Update " + tableName + " set ";
            string _paramsStr = " where ";
            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                //string _sqlStr = string.Format("Update {0} SET ", tableName);
                foreach (var item in _includeProperty)
                {
                    _sqlStr += item + " = @" + item + ",";
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    if (_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null),
                                                          _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                    else
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null), _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                List<string> _tmpWhere = new List<string>();
                string _randonStr;
                foreach (var item in _conditionProperty)
                {
                    _randonStr = GetRandomString(6);
                    _tmpWhere.Add(item + " = @" + _randonStr);
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    object _value = _property.GetValue(updateObj, null);
                    if (_value != null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value != null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                }
                _sqlStr += _paramsStr + string.Join(" and ", _tmpWhere.ToArray());
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public bool UpdateInclude<T>(string tableName, T updateObj, List<Expression<Func<T, object>>> includeProperty, List<Expression<Func<T, object>>> conditionProperty)
        {
            MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
            _method = _method.MakeGenericMethod(new Type[] { updateObj.GetType() });
            List<string> _includeProperty = (List<string>)_method.Invoke(this, new object[] { includeProperty });
            List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { conditionProperty });

            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            object _conditionPObj = null;
            string _sqlStr = "Update " + tableName + " set ";
            string _paramsStr = " where ";
            try
            {
                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                //string _sqlStr = string.Format("Update {0} SET ", tableName);
                foreach (var item in _includeProperty)
                {
                    _sqlStr += item + " = @" + item + ",";
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    if (_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null),
                                                          _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                    else
                    {
                        _conditionPObj = new MSParameters(_property.GetValue(updateObj, null), _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", item), _conditionPObj);
                    }
                }
                _sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                List<string> _tmpWhere = new List<string>();
                string _randonStr;
                foreach (var item in _conditionProperty)
                {
                    _randonStr = GetRandomString(6);
                    _tmpWhere.Add(item + " = @" + _randonStr);
                    PropertyInfo _property = updateObj.GetType().GetProperty(item);
                    object _value = _property.GetValue(updateObj, null);
                    if (_value != null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value != null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(_value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && _property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType.GetGenericArguments()[0]);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                    else if (_value == null && !_property.PropertyType.IsGenericType)
                    {
                        _conditionPObj = new MSParameters(DBNull.Value, _property.PropertyType);
                        _slParams.Add(string.Format("@{0}", _randonStr), _conditionPObj);
                    }
                }
                _sqlStr += _paramsStr + string.Join(" and ", _tmpWhere.ToArray());
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion
        #region -- Delete --
        public bool Delete(string sqlStr)
        {
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, _slParams);
        }
        public bool Delete(string sqlStr, Dictionary<string, object> slParams)
        {
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, slParams);
        }
        public bool Deletes(string sqlStr, List<Dictionary<string, object>> slParams)
        {
            return _common.AddCommend(this._connStr, sqlStr, ref _cmds, slParams);
        }
        public bool Delete<T>(string tableName, T deleteObj, List<string> conditionProperty)
        {
            for (int i = 0; i < conditionProperty.Count; i++)
                conditionProperty[i] = conditionProperty[i].ToUpper();

            string _sqlStr = string.Format("delete from {0}", tableName);
            string _paramsStr = " where ";
            Dictionary<string, object> _slParams = new Dictionary<string, object>();
            Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
            string _conditionPName = "";
            object _conditionPObj = null;
            PropertyInfo[] objInfo = deleteObj.GetType().GetProperties();
            foreach (var item in objInfo)
            {
                //_sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                var _value = deleteObj.GetType().GetProperty(item.Name).GetValue(deleteObj, null);
                var _type = deleteObj.GetType().GetProperty(item.Name).PropertyType;
                if (_value != null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                }
                else if (_value != null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && _type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                }
                else if (_value == null && !_type.IsGenericType)
                {
                    _conditionPName = string.Format("@C_{0}", item.Name);
                    _conditionPObj = new object();
                    _conditionPObj = new MSParameters(_value, _type);
                    _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                }
                if (conditionProperty.Contains(item.Name.ToUpper()))
                {
                    _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                    _conditionParams.Add(_conditionPName, _conditionPObj);
                }
            }
            //_sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
            //去掉最後一個" and "
            _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
            _sqlStr = _sqlStr + _paramsStr;
            foreach (var item in _conditionParams)
                _slParams.Add(item.Key, item.Value);
            _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
            return true;
        }
        public bool Delete<T>(string tableName, T deleteObj, List<Expression<Func<T, object>>> conditionProperty)
        {
            try
            {
                MethodInfo _method = this.GetType().GetMethod("GetPropertyNames", BindingFlags.NonPublic | BindingFlags.Instance);
                _method = _method.MakeGenericMethod(new Type[] { deleteObj.GetType() });
                List<string> _conditionProperty = (List<string>)_method.Invoke(this, new object[] { conditionProperty });

                for (int i = 0; i < _conditionProperty.Count; i++)
                    _conditionProperty[i] = _conditionProperty[i].ToUpper();

                string _sqlStr = string.Format("delete from {0}", tableName);
                string _paramsStr = " where ";
                Dictionary<string, object> _slParams = new Dictionary<string, object>();
                Dictionary<string, object> _conditionParams = new Dictionary<string, object>();
                string _conditionPName = "";
                object _conditionPObj = null;
                PropertyInfo[] objInfo = deleteObj.GetType().GetProperties();
                foreach (var item in objInfo)
                {
                    //_sqlStr = string.Format("{0}{1}=@{2},", _sqlStr, item.Name, item.Name);
                    var _value = deleteObj.GetType().GetProperty(item.Name).GetValue(deleteObj, null);
                    var _type = deleteObj.GetType().GetProperty(item.Name).PropertyType;
                    if (_value != null && !_type.IsGenericType)
                    {
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type));
                    }
                    else if (_value != null && _type.IsGenericType)
                    {
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(_value, _type.GetGenericArguments()[0]));
                    }
                    else if (_value == null && _type.IsGenericType)
                    {
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type.GetGenericArguments()[0]));
                    }
                    else if (_value == null && !_type.IsGenericType)
                    {
                        _conditionPName = string.Format("@C_{0}", item.Name);
                        _conditionPObj = new object();
                        _conditionPObj = new MSParameters(_value, _type);
                        _slParams.Add(string.Format("@{0}", item.Name), new MSParameters(DBNull.Value, _type));
                    }
                    if (_conditionProperty.Contains(item.Name.ToUpper()))
                    {
                        _paramsStr = _paramsStr + item.Name + "=@C_" + item.Name + " and ";
                        _conditionParams.Add(_conditionPName, _conditionPObj);
                    }
                }
                //_sqlStr = _sqlStr.Substring(0, _sqlStr.Length - 1);
                //去掉最後一個" and "
                _paramsStr = _paramsStr.Substring(0, _paramsStr.Length - 5);
                _sqlStr = _sqlStr + _paramsStr;
                foreach (var item in _conditionParams)
                    _slParams.Add(item.Key, item.Value);
                _common.AddCommend(this._connStr, _sqlStr, ref _cmds, _slParams);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion
        #region -- Procedure --
        public List<T> Procedure_Generic<T>(string sqlStr, Dictionary<string, object> slParams) where T : class
        {
            List<T> _tList = new List<T>();
            try
            {
                if (this._connStr == string.Empty)
                    return null;

                SqlConnection Conn = new SqlConnection(this._connStr);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = Conn;
                cmd.CommandTimeout = this._timeout;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = sqlStr;
                if (slParams != null)
                    _common.SqlParameterSet(ref cmd, slParams);

                Conn.Open();
                SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
                DataSet Ds = new DataSet();
                Adpt.Fill(Ds);

                for (int i = 0; i < Ds.Tables[0].Rows.Count; i++)
                {
                    var _tmpObject = Activator.CreateInstance<T>();

                    PropertyInfo[] _propertys = _tmpObject.GetType().GetProperties();
                    foreach (var _property in _propertys)
                    {
                        if (Ds.Tables[0].Columns.Contains(_property.Name))
                        {
                            var _tmp = Ds.Tables[0].Rows[i][_property.Name];
                            if (_tmp is DBNull)
                                _tmpObject.GetType()
                                       .GetProperty(_property.Name)
                                       .SetValue(_tmpObject, null, null);
                            else
                            {
                                if (_property.PropertyType.IsGenericType)
                                    _tmpObject.GetType().GetProperty(_property.Name)
                                                        .SetValue(_tmpObject,
                                                                  Convert.ChangeType(_tmp, _property.PropertyType.GetGenericArguments()[0]),
                                                                  null);
                                else
                                    _tmpObject.GetType()
                                              .GetProperty(_property.Name)
                                              .SetValue(_tmpObject,
                                                        Convert.ChangeType(_tmp, _property.PropertyType),
                                                        null);
                            }
                        }
                        else
                        {
                            _tmpObject.GetType()
                                      .GetProperty(_property.Name)
                                      .SetValue(_tmpObject,
                                                null,
                                                null);
                        }
                    }
                    _tList.Add(_tmpObject);
                }
                Adpt.Dispose();
                cmd.Dispose();
                cmd.Cancel();
                Conn.Close();
                Conn.Dispose();
            }
            catch (Exception ex)
            {
            }

            return _tList;
        }
        public DataTable Procedure_Table(string sqlStr, Dictionary<string, object> slParams)
        {
            if (this._connStr == string.Empty)
                return null;

            SqlConnection Conn = new SqlConnection(this._connStr);
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = Conn;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = sqlStr;
            if (slParams != null)
                _common.SqlParameterSet(ref cmd, slParams);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataSet Ds = new DataSet();
            Adpt.Fill(Ds);


            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            return Ds.Tables[0];
        }
        #endregion
        #region -- Count --
        public double Count(string connStr, string sqlStr, Dictionary<string, object> slParams)
        {
            //解密
            string _count = string.Empty;
            string strResult = string.Empty;

            SqlConnection Conn = new SqlConnection(connStr);

            SqlCommand cmd = new SqlCommand(sqlStr, Conn);
            if (slParams != null)
                _common.SqlParameterSet(ref cmd, slParams);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            Adpt.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                _count = dt.Rows[0].ItemArray[0].ToString();
            }
            else
            {
                strResult = "Record count = 0";
            }

            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            return Convert.ToInt32(_count);
        }
        public double Count(string sqlStr, Dictionary<string, object> slParams)
        {
            //解密
            string _count = string.Empty;
            string strResult = string.Empty;
            string DSNStr = string.Empty;

            SqlConnection Conn = new SqlConnection(this._connStr);

            SqlCommand cmd = new SqlCommand(sqlStr, Conn);
            if (slParams != null)
                _common.SqlParameterSet(ref cmd, slParams);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            Adpt.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                _count = dt.Rows[0].ItemArray[0].ToString();
            }
            else
            {
                _count = "0";
            }

            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            return Convert.ToInt32(_count);
        }
        public double Count<T>(string sqlStr) where T : class
        {
            T _tmpObject = Activator.CreateInstance<T>();
            Dictionary<string, object> _params = new Dictionary<string, object>();
            List<T> _reList = new List<T>();
            List<int> _spaceIndexs = new List<int>();
            List<string> _paramPrimalStrs = new List<string>();
            List<string> _paramNewStrs = new List<string>();
            List<string> _paramValues = new List<string>();

            string[] _sqlSplit = sqlStr.Split(' ');
            for (int i = 0; i < _sqlSplit.Length; i++)
            {
                if (_sqlSplit[i] == "=")
                {
                    _spaceIndexs.Add(i);
                    _paramNewStrs.Add(GetRandomString(5));
                    _paramPrimalStrs.Add(_sqlSplit[i - 1]);
                    _paramValues.Add(_sqlSplit[i + 1]);
                    _sqlSplit[i + 1] = string.Format("@{0}", _paramNewStrs[_paramNewStrs.Count - 1]);
                }
            }
            sqlStr = string.Join(" ", _sqlSplit);
            int _index = 0;
            List<string> _tmpObjProperty = _tmpObject.GetType().GetProperties().Select(p => p.Name.ToUpper()).ToList();
            foreach (var item in _paramPrimalStrs)
            {
                Type _type;
                if (_tmpObjProperty.Contains(item.ToUpper()))
                    _type = _tmpObject.GetType().GetProperties().Where(p => p.Name.ToUpper() == item.ToUpper()).First().PropertyType;
                else
                    _type = typeof(string);

                object _tmpValue;
                if (_type.Name == "Guid")
                    _tmpValue = new Guid(_paramValues[_index]);
                else
                    _tmpValue = Convert.ChangeType(_paramValues[_index], _type);

                _params.Add(string.Format("@{0}", _paramNewStrs[_index]), new MSParameters(_tmpValue, _type));
                _index++;
            }

            SqlConnection Conn = new SqlConnection(this._connStr);

            SqlCommand cmd = new SqlCommand(sqlStr, Conn);
            if (_params != null)
                _common.SqlParameterSet(ref cmd, _params);

            Conn.Open();
            SqlDataAdapter Adpt = new SqlDataAdapter(cmd);
            DataSet Ds = new DataSet();
            Adpt.Fill(Ds);

            double _returnValue = Convert.ToDouble(Ds.Tables[0].Rows[0].ItemArray[0]);

            Adpt.Dispose();
            cmd.Dispose();
            cmd.Cancel();
            Conn.Close();
            Conn.Dispose();
            return _returnValue;
        }
        #endregion
        public string SaveChange()
        {
            using (SqlConnection _conn = new SqlConnection(this._connStr))
            {
                _conn.Open();

                SqlCommand _command = _conn.CreateCommand();
                SqlTransaction _transaction;

                _transaction = _conn.BeginTransaction("Transaction");

                _command.Connection = _conn;
                _command.CommandTimeout = this._timeout;
                _command.Transaction = _transaction;

                try
                {
                    foreach (var cmd in _cmds)
                    {
                        _command.CommandText = cmd.Key.ToString();
                        foreach (Dictionary<string, object> item in cmd.Value)
                        {
                            _common.SqlParameterSet(ref _command, item);
                            _command.ExecuteNonQuery();
                            _command.Parameters.Clear();
                        }
                    }

                    _transaction.Commit();
                    _cmds.Clear();
                    return "0";
                }
                catch (Exception ex)
                {
                    _cmds.Clear();
                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                    Console.WriteLine("  Message: {0}", ex.Message);
                    try
                    {
                        _transaction.Rollback();
                        return string.Format("Commit Exception Type: {0} Message: {1}", ex.GetType(), ex.Message.ToString());
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                        Console.WriteLine("  Message: {0}", ex2.Message);
                        return string.Format("Rollback Exception Type: {0} Message: {1}", ex2.GetType(), ex2.Message.ToString());
                    }
                }
            }
        }
        public int Modify(string sqlStr, Dictionary<string, object> slParams)
        {
            try
            {
                SqlConnection Conn = new SqlConnection(this._connStr);

                SqlCommand cmd = new SqlCommand(sqlStr, Conn);
                if (slParams != null)
                    _common.SqlParameterSet(ref cmd, slParams);

                Conn.Open();
                int count = cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd.Cancel();
                Conn.Close();
                Conn.Dispose();
                return count;
            }
            catch (Exception ex)
            {
                return -1;
                throw;
            }
        }

        #region -- Private --
        /// <summary>產生一個亂數字串</summary>
        /// <param name="length">要產生的亂數字串長度</param>
        private string GetRandomString(int length)
        {
            Random r = new Random(Guid.NewGuid().GetHashCode());
            string code = "";

            for (int i = 0; i < length; ++i)
                switch (r.Next(0, 2))
                {
                    case 0: code += r.Next(1, 10); break;
                    case 1: code += (char)r.Next(65, 78); break;
                    case 2: code += (char)r.Next(97, 122); break;
                }

            return code;
        }
        private string[] ChangeContent(string[] _value)
        {
            int _emptyCount = 0;
            int _index = 0;
            foreach (var item in _value)
            {
                if (item == "")
                    _emptyCount++;
            }
            string[] _newArray = new string[_value.Length - _emptyCount];
            foreach (var item in _value)
            {
                if (item != "")
                {
                    _newArray[_index] = item;
                    _index++;
                }
            }
            return _newArray;
        }
        private List<string> GetPropertyNames<T>(IEnumerable<Expression<Func<T, object>>> propertys)
        {
            List<string> _returnStrs = new List<string>();
            foreach (var item in propertys)
            {
                string _perproteyStr = string.Empty;
                if (item.Body is NewExpression)
                {
                    var _tmp = (NewExpression)item.Body;
                    foreach (var member in _tmp.Members)
                    {
                        _returnStrs.Add(member.Name);
                    }
                }
                if (item.Body is UnaryExpression)
                {
                    _returnStrs.Add(((MemberExpression)((UnaryExpression)item.Body).Operand).Member.Name);
                }
                else if (item.Body is MemberExpression)
                {
                    _returnStrs.Add(((MemberExpression)item.Body).Member.Name);
                }
                else if (item.Body is ParameterExpression)
                {
                    _returnStrs.Add(((ParameterExpression)item.Body).Type.Name);
                }
            }
            return _returnStrs;
        }
        #endregion
    }
}
