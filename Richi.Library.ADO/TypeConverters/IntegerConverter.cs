using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Richi.Library.ADO
{
    public class IntegerConverter : ITypeConverter
    {
        public object Convert(object ValueToConvert) 
        {
            if (ValueToConvert == null || ValueToConvert == DBNull.Value)
                return 0;

            return System.Convert.ToInt32(ValueToConvert);
        }
    }
}
