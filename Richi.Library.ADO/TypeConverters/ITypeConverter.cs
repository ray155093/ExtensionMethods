using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Richi.Library.ADO
{
    public interface ITypeConverter
    {
        object Convert(object ValueToConvert);
    }
}
