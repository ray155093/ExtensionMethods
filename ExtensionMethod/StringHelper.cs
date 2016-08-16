using System;
using ExtensionMethods.DataModel;
using System.Web.Script.Serialization;
using System.Text;

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

        /// <summary>
        /// 用Richi資源取得統計區(不確定在那些網段可以用，公司可以)
        /// </summary>
        /// <param name="Str">The string.</param>
        /// <param name="Address">地址</param>
        /// <returns></returns>
        public static AddrUnit GetAddrUnit (this  string Address)
        {
            //Arrange
            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
            string sourceStr = "http://egis.moea.gov.tw/MoeaEGFxData/GetAddr/SearchAddr.ashx?addr="+Address;
            //Action
            string responseStr = sourceStr.GetResponseStr("GET", "[application/x-www-form-urlencoded]", "", Encoding.UTF8);
            AddrXY addrXY = jsonSerializer.Deserialize<AddrXY>(responseStr);
          
            //一級、二級、三級劃設統計區(透過ArcGis，透過面回傳屬性資料)
            string SearchCodeURL = @"http://124.219.79.204/arcgis/rest/services/EGIS/MoeaCode_TW/MapServer/1/query?geometry=";
            // 是最小單元統計區(同上)
            string SearchCodeBaseURL = @"http://124.219.79.158/arcgis/rest/services/MoeaCode_TW/MapServer/2/query?geometry=";
            string GetCodeurl = SearchCodeURL;
            string GetCodeBaseurl = SearchCodeBaseURL;
            string Serice_CodePer = "&geometryType=esriGeometryPoint&spatialRel=esriSpatialRelIntersects&returnCountOnly=false&returnIdsOnly=false&returnGeometry=124,302false&f=json&outFields=COUN_ID,COUN_NA,TOWN_ID,TOWN_NA,CODE3,CODE2,CODE1";
            string Serice_Codebaseper = "&geometryType=esriGeometryPoint&spatialRel=esriSpatialRelIntersects&returnCountOnly=false&returnIdsOnly=false&returnGeometry=false&f=json&outFields=CODEBASE";
            AddrUnit resultAddrUnit = new AddrUnit();
            string CodeServer_url = GetCodeurl + addrXY.X97 + "," + addrXY.Y97 + Serice_CodePer;
            string CodebaseServer_url = GetCodeBaseurl + addrXY.X97 + "," + addrXY.Y97 + Serice_Codebaseper;
            string ResultCodeServer_url = CodeServer_url.GetResponseStr("GET", CodeServer_url, "", Encoding.UTF8);
            AddrCode addrCode = jsonSerializer.Deserialize<AddrCode>(ResultCodeServer_url);
            string ResultCodebaseServer_url = CodebaseServer_url.GetResponseStr("GET", CodebaseServer_url, "", Encoding.UTF8);
            AddrCodeBase addrCodeBase = jsonSerializer.Deserialize<AddrCodeBase>(ResultCodebaseServer_url);

        
            resultAddrUnit.COUN_ID = addrCode.features[0].attributes.COUN_ID.ToString().Trim();
            resultAddrUnit.COUN_NA = addrCode.features[0].attributes.COUN_NA.ToString().Trim();
            resultAddrUnit.TOWN_ID = addrCode.features[0].attributes.TOWN_ID.ToString().Trim();
            resultAddrUnit.TOWN_NA = addrCode.features[0].attributes.TOWN_NA.ToString().Trim();
            resultAddrUnit.CODE3 = addrCode.features[0].attributes.CODE3.ToString().Trim();
            resultAddrUnit.CODE2 = addrCode.features[0].attributes.CODE2.ToString().Trim();
            resultAddrUnit.CODE1 = addrCode.features[0].attributes.CODE1.ToString().Trim();
            resultAddrUnit.CODEBASE = addrCodeBase.features[0].attributes.CODEBASE.ToString().Trim();

            return resultAddrUnit;

        }

    }
}