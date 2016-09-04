using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Richi.Library.ADO
{
    public class FloatConverter : ITypeConverter
    {
        public object Convert(object ValueToConvert)
        {
            if (ValueToConvert == null || ValueToConvert == DBNull.Value)
                return 0.0f;

            return System.Convert.ToSingle(ValueToConvert);
        }
    }
}
