using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ExtensionMethods
{
    public static class DateTimerHelper
    {
        /// <summary>
        /// Changes to taiwan year.
        /// </summary>
        /// <param name="datetime">The datetime.</param>
        /// <param name="format">The format.</param>
        /// <returns></returns>
        public static String ChangeToTaiwanYear(this DateTime dt, string format)
        {
            var tc =new  TaiwanCalendar();
            var regex = new Regex(@"[yY]+");
            format = regex.Replace(format, tc.GetYear(dt).ToString("000"));
          
            return dt.ToString(format);
        }
    }
}
