using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Richi.Library.Factory
{
    public static class ClassParMapping
    {
        private static IDictionary<string, string> _classParameterMapping;
        public static IDictionary<string, string> ClassParameterMapping
        {
            get
            {
                return _classParameterMapping;
            }
        }
        static ClassParMapping()
        {
            if (_classParameterMapping == null)
            {
                _classParameterMapping = new Dictionary<string, string>();
                NameValueCollection _collection =
                        (NameValueCollection)System.Configuration.ConfigurationManager.GetSection(@"classConfig/classParameterMapping");
                _classParameterMapping = _collection.Cast<string>()
                                                    .Select(s => new { Key = s, Value = _collection[s] })
                                                    .ToDictionary(p => p.Key, p => p.Value);
            }
        }
    }
}
