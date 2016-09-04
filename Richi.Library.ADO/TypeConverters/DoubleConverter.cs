using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Richi.Library.ADO
{
    public class DoubleConverter : ITypeConverter
    {
        public object Convert(object ValueToConvert)
        {
            if (ValueToConvert == null || ValueToConvert == DBNull.Value)
                return 0.0d;

            return System.Convert.ToDouble(ValueToConvert);
        }
    }
}
