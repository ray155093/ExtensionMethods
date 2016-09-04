using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

using System.Collections.Specialized;

namespace Richi.Library.Factory
{
    public static class Factory
    {
        public static object CreateInstance(string path, string dllName, string className) 
        {
            string _dllName = GetNameSpace(dllName);

            string _dllPath = Path.Combine(path, string.Format("{0}.{1}", _dllName, "dll"));
            Assembly _assembly = Assembly.LoadFrom(_dllPath);
            className = string.Format("{0}.{1}", _dllName, className);
            Type MyLoadClass = _assembly.GetType(className); // name of your class
            object obj = Activator.CreateInstance(MyLoadClass);
            return obj;
        }
        public static object CreateInstance(string path, string dllName, string className, object[] parameters)
        {
            string _dllName = GetNameSpace(dllName);
            string _dllPath = Path.Combine(path, string.Format("{0}.{1}", _dllName, "dll"));
            Assembly _assembly = Assembly.LoadFrom(_dllPath);
            className = string.Format("{0}.{1}", _dllName, className);
            Type MyLoadClass = _assembly.GetType(className); // name of your class
            object obj = Activator.CreateInstance(MyLoadClass, parameters);
            return obj;
        }
        public static T CreateInstance<T>(string path, string dllName, string className) where T : class
        {
            string _dllName = GetNameSpace(dllName);

            string _dllPath = Path.Combine(path, string.Format("{0}.{1}", _dllName, "dll"));
            Assembly _assembly = Assembly.LoadFrom(_dllPath);
            className = string.Format("{0}.{1}", _dllName, className);
            Type MyLoadClass = _assembly.GetType(className); // name of your class
            T obj = Activator.CreateInstance(MyLoadClass) as T;
            return obj;
        }
        public static T CreateInstance<T>(string path, string dllName, string className, object[] parameters) where T : class
        {
            string _dllName = GetNameSpace(dllName);
            string _dllPath = Path.Combine(path, string.Format("{0}.{1}", _dllName, "dll"));
            Assembly _assembly = Assembly.LoadFrom(_dllPath);
            className = string.Format("{0}.{1}", _dllName, className);
            Type MyLoadClass = _assembly.GetType(className); // name of your class
          T obj = Activator.CreateInstance(MyLoadClass, parameters) as T;
            return obj;
        }
        public static T CreateInstance<T>()
        {
            Type _inputType = typeof(T);
            
            NameValueCollection _collection =
                        (NameValueCollection)System.Web.Configuration.WebConfigurationManager.GetSection(@"classConfig/ProjectSettings");

            string _dllPath = _collection["DllPath"].ToString();
            IEnumerable<string> _allFileName = Directory.GetFiles(_dllPath, "*.dll").Select(Path.GetFileName);
            Assembly _assembly = GetAssembly(_inputType, _dllPath, _allFileName);
            string _dllName = _assembly.Location.Split('\\').LastOrDefault();
            string[] _dllNamespace = _inputType.FullName.Split('.');
            string _serviceName = _dllNamespace[_dllNamespace.Length - 1];
            _serviceName = _serviceName.Substring(0, 1) == "I" ? _serviceName.Substring(1, _serviceName.Length - 1) : _serviceName;
            _dllNamespace[_dllNamespace.Length - 1] = _serviceName;
            string _className = string.Join(".", _dllNamespace);
            Type _type = _assembly.GetType(_className);

            List<object> _objs = new List<object>();
            T _obj = default(T);
            int _constructorCnt = _type.GetConstructors().Count();
            if(_constructorCnt > 1)
            {
                var _ctors = _type.GetConstructors();
                foreach (var item in _ctors)
                {
                    if (item.GetCustomAttributes(typeof(InjectionConstrurctorAttribute), true).Count() > 0)
                    {
                        if (item.GetParameters().Count() > 0)
                        {
                            if (ClassParMapping.ClassParameterMapping.Keys.Contains(_type.Name))
                            {
                                string _parameterStr = ClassParMapping.ClassParameterMapping[_type.Name];
                                object[] _parObjs = new object[] { _parameterStr };
                                _obj = (T)Activator.CreateInstance(_type, _parObjs);
                            }
                            else
                            {
                                foreach (var parame in item.GetParameters())
                                    _objs.Add(CreateObjectIncParameter(_dllPath, parame.ParameterType.Name, parame.ParameterType, _allFileName));
                                _obj = (T)Activator.CreateInstance(_type, _objs.ToArray());
                            }
                        }
                        else
                            _obj = (T)Activator.CreateInstance(_type);
                    }
                }
            }
            else
            {
                var _ctor = _type.GetConstructors()[0];
                if (_ctor.GetParameters().Count() > 0)
                {
                    if (ClassParMapping.ClassParameterMapping.Keys.Contains(_type.Name))
                    {
                        List<object> _params = new List<object>();
                        foreach (var item in _ctor.GetParameters())
                        {
                            //判斷item type是否為自定義型別
                            if ((!item.ParameterType.IsClass && !item.ParameterType.IsInterface) || item.ParameterType.FullName.StartsWith("System"))
                            {
                                string _parameterStr = ClassParMapping.ClassParameterMapping[_type.Name];
                                string[] _paramStr = _parameterStr.Split(',');
                                foreach (var param in _paramStr)
                                    _params.Add(param);
                            }
                            else
                            {
                                _params.Add(CreateObjectIncParameter(_dllPath, item.ParameterType.Name, item.ParameterType, _allFileName));
                            }
                        }
                        _obj = (T)Activator.CreateInstance(_type, _params.ToArray());
                    }
                    else
                    {
                        foreach (var item in _ctor.GetParameters())
                        {
                            string _tmpDllName = item.ParameterType.Namespace.ToString();
                            string _tmpClassName = item.ParameterType.FullName.Replace(item.ParameterType.Namespace + ".", "");
                            _tmpClassName = _tmpClassName.Substring(0, 1) == "I" ? _tmpClassName.Substring(1, _tmpClassName.Length - 1) : _tmpClassName;
                            _objs.Add(CreateObjectIncParameter(_dllPath, _tmpClassName, item.ParameterType, _allFileName));
                        }
                        _obj = (T)Activator.CreateInstance(_type, _objs.ToArray());
                    }
                }
                else
                    _obj = (T)Activator.CreateInstance(_type);
            }
            return _obj;
        }
        
        #region -- Private --
        private static string GetNameSpace(string dllName)
        {
            string[] _dllSplitNames = dllName.Split('.');
            string _dllName = string.Empty;
            foreach (var name in _dllSplitNames)
            {
                if (name.ToUpper() != "DLL")
                    _dllName = string.Format("{0}.{1}", _dllName, name);
                else
                    break;
            }
            _dllName = _dllName.Substring(1, _dllName.Length - 1);
            return _dllName;
        }
        private static Assembly GetAssembly(Type type, string dllPath, IEnumerable<string> _allFileName)
        {
            Assembly _assembly = default(Assembly);
            string _dllName = string.Empty;
            foreach (var item in _allFileName)
            {
                _dllName = item;
                if (_dllName != "Remotion.dll")
                {
                    _assembly = Assembly.LoadFrom(dllPath + _dllName);
                    IEnumerable<Type> _result = null;
                    if (type.IsClass)
                        _result = _assembly.GetTypes().Where<Type>(p => p == type);
                    if (type.IsInterface)
                        _result = _assembly.GetTypes().Where<Type>(p => p.GetInterfaces().Contains(type));
                    if (_result.Count() > 0)
                        break;
                }
            }
            return _assembly;
        }
        private static object CreateObjectIncParameter(string path, string className, Type type, IEnumerable<string> allFileName)
        {
            Assembly _assembly = GetAssembly(type, path, allFileName);
            //處理介面型別
            className = className.Substring(0, 1) == "I" ? className.Substring(1, className.Length - 1) : className;
            className = _assembly.FullName.Split(',').FirstOrDefault() + "." + className;
            Type _type = _assembly.GetType(className);
            List<object> _objs = new List<object>();
            object _obj = new object();
            int _constructorCnt = _type.GetConstructors().Count();
            if (_constructorCnt > 1)
            {
                var _ctors = _type.GetConstructors();
                foreach (var item in _ctors)
                {
                    if (item.GetCustomAttributes(typeof(InjectionConstrurctorAttribute), true).Count() > 0)
                    {
                        if (item.GetParameters().Count() > 0)
                        {
                            int _strParamIndex = 0;
                            foreach (var parame in item.GetParameters())
                            {
                                if ((!parame.ParameterType.IsClass && !parame.ParameterType.IsInterface) || parame.ParameterType.FullName.StartsWith("System"))
                                {
                                    string _parameterStr = ClassParMapping.ClassParameterMapping[_type.Name];
                                    string[] _tmpStrArray = _parameterStr.Split(',');
                                    _objs.Add(_tmpStrArray[_strParamIndex]);
                                    _strParamIndex++;
                                }
                                else
                                    _objs.Add(CreateObjectIncParameter(path, parame.ParameterType.Name, parame.ParameterType, allFileName));
                            }
                            _obj = Activator.CreateInstance(_type, _objs.ToArray());
                        }
                        else
                            _obj = Activator.CreateInstance(_type);
                    }
                }
            }
            else
            {
                var _ctor = _type.GetConstructors()[0];
                if (_ctor.GetParameters().Count() > 0)
                {
                    if (ClassParMapping.ClassParameterMapping.Keys.Contains(_type.Name))
                    {
                        string _parameterTag = ClassParMapping.ClassParameterMapping[_type.Name];
                        string _parameterStr = string.Empty;
                        var _connectionObj = System.Configuration.ConfigurationManager.ConnectionStrings[_parameterTag];
                        if (_connectionObj != null)
                            _parameterStr = _connectionObj.ConnectionString;

                        if (System.Configuration.ConfigurationManager.AppSettings[_parameterTag] != null)
                            _parameterStr = System.Configuration.ConfigurationManager.AppSettings[_parameterTag].ToString();
                        object[] _parObjs = new object[] { _parameterStr };
                        _obj = Activator.CreateInstance(_type, _parObjs);
                    }
                    else
                    {
                        foreach (var item in _ctor.GetParameters())
                        {
                            string _tmpClassName = item.ParameterType.FullName.Replace(item.ParameterType.Namespace + ".", "");
                            _tmpClassName = _tmpClassName.Substring(0, 1) == "I" ? _tmpClassName.Substring(1, _tmpClassName.Length - 1) : _tmpClassName;
                            _objs.Add(CreateObjectIncParameter(path, _tmpClassName, item.ParameterType, allFileName));
                        }
                        _obj = Activator.CreateInstance(_type, _objs.ToArray());
                    }
                }
                else
                    _obj = Activator.CreateInstance(_type);
            }
            return _obj;
        }
        #endregion
    }
    #region -- Attribute --
    [AttributeUsage(AttributeTargets.Constructor)]
    public class InjectionConstrurctorAttribute : Attribute
    { }
    [AttributeUsage(AttributeTargets.Class)]
    public class InjectionClassAttribute : Attribute
    { } 
    #endregion
}
