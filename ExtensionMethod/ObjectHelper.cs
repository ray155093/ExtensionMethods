using System;
using System.ComponentModel;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

namespace ExtensionMethods
{


    public static class ObjectHelper
    {
        /// <summary>
        /// 若值為DBNull.Value , 則轉成null
        /// </summary>
        /// <param name="original">The original.</param>
        /// <returns></returns>
        public static object DbNullToNull(this object original)
        {
            return original == DBNull.Value ? null : original;
        }

        /// <summary>
        /// 若值為null, 則轉成DBNull.Value
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static object NullToDbNull(this object original)
        {
            return original ?? DBNull.Value;
        }
        /// <summary>
        /// 將DataTable轉換成對應的Entity集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static IEnumerable<T> DataTableToEntities<T>(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                T result = Activator.CreateInstance<T>();
                foreach (DataColumn column in dt.Columns)
                {
                    typeof(T).GetProperty(column.ColumnName).SetValue(result, row[column.ColumnName].DbNullToNull(), null);
                }
                yield return result;
                string a = "";
            }
        }


        /// <summary>
        /// 判斷傳入值是否為 Nothing 或 DBNull。
        /// </summary>
        /// <param name="Value">傳入值。</param>
        public static bool IsNullOrDBNull(this object Value)
        {
            if ((Value == null) || Value == System.DBNull.Value)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 轉型為 String，若傳入值為 null 或 DBNull 則傳回預設值。
        /// </summary>
        /// <param name="Value">傳入值。</param>
        /// <param name="DefaultValue">預設值。</param> 
        public static string ConvertToString(this object Value, string DefaultValue)
        {
            if (IsNullOrDBNull(Value))
            {
                return DefaultValue;
            }
            else
            {
                return Convert.ToString(Value);
            }
        }

        /// <summary>
        /// 轉型為 String，若傳入值為 null 或 DBNull 則傳回空字串。
        /// </summary>
        /// <param name="Value">傳入值。</param>
        public static string ConvertToString(this object Value)
        {
            return ConvertToString(Value, string.Empty);
        }


        /// <summary>
        /// 取得物件的屬性值。
        /// </summary>
        /// <param name="Component">具有要擷取屬性的物件。</param>
        /// <param name="PropertyName">屬性名稱。</param>
        public static object GetPropertyValue(this object Component, string PropertyName)
        {
            PropertyDescriptor Prop = TypeDescriptor.GetProperties(Component)[PropertyName];
            return Prop.GetValue(Component);
        }

        /// <summary>
        /// 檢查物件的屬性值是否符合。
        /// </summary>
        /// <param name="Component">具有要擷取屬性的物件。</param>
        /// <param name="PropertyName">屬性名稱。</param>
        /// <param name="PropertyValue">欲判斷的屬性值。</param>
        public static bool CheckPropertyValue(this object Component, string PropertyName, object PropertyValue)
        {
            object oValue = null;

            oValue = GetPropertyValue(Component, PropertyName);
            if (object.ReferenceEquals(oValue.GetType(), typeof(string)))
            {
                if (StringHelper.IsSameText(ConvertToString(oValue), ConvertToString(PropertyValue)))
                {
                    return true;
                }
            }
            else
            {
                if (oValue.Equals(PropertyValue))
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// 將物件轉換成byte比對物件是否一致
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns></returns>
        public static bool EqualsObject(this object obj1, object obj2)
        {
            var targetArray = getObjectByte(obj1);
            var expectedArray = getObjectByte(obj2);
            var equals = expectedArray.SequenceEqual(targetArray);
            return equals;
        }

        private static byte[] getObjectByte(object model)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                XmlSerializer xs = new XmlSerializer(model.GetType());
                xs.Serialize(memory, model);
                var array = memory.ToArray();
                return array;
            }
        }

    }
}