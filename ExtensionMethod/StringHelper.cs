using System;

namespace ExtensionMethods
{

    /// <summary>
    /// 字串輔助函式
    /// </summary>
    public static class StringHelper
    {
        #region 擷取子字串
        /// <summary>
        /// 由左邊取得指定數量字元
        /// </summary>
        /// <param name="param"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string Left(this string param, int length)
        {
            string result;
            if (length > param.Length)
                result = param;
            else
                result = param.Substring(0, length);
            return result;
        }


        #endregion

        #region 比對字串是否相同
        /// <summary>
        /// 比對字串是否相同(不分大小寫)
        /// </summary>
        /// <param name="src1"></param>
        /// <param name="src2"></param>
        /// <returns></returns>
        public static bool IsSameText(this string src1, string src2)
        {
            if (src1 == null)
                return src2 == null;

            return src1.Equals(src2, StringComparison.CurrentCultureIgnoreCase);

        }
        #endregion

        /// <summary>
        /// 計算字串長度(Char)
        /// </summary>
        /// <remarks>非ASCII(如中文字)固定長度為2</remarks>
        /// <param name="aDataStr"></param>
        /// <returns></returns>
        public static int CountStringForDBCS(this string aDataStr)
        {
            int tCount = 0;
            foreach (char tStr in aDataStr)
            {
                if ((int)tStr < 255)
                    tCount += 1;
                else
                    tCount += 2;
            }
            return tCount;
        }
        /// <summary>
        /// 切割String By String
        /// </summary>
        ///  /// <param name="Str">需切割的字串</param>
        /// <param name="SplitStr">切割的字串</param>
        /// <returns></returns>
        public static string[] SplitByString(this string Str, string SplitStr)
        {
            return Str.Split(new string[] { SplitStr }, StringSplitOptions.RemoveEmptyEntries);
        }

    }
}