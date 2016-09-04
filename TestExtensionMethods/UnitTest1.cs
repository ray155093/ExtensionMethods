using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExtensionMethods;
using System.Text;
using ExtensionMethods.DataModel;
using System.Web.Script.Serialization;
using System.Data;
using System.Linq;

namespace TestExtensionMethods
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void 測試台灣日期()
        {
            //Arrange
            string Ans = "105/06/12 週日";
            DateTime dt = Convert.ToDateTime("2016/6/12");

            //Action
            string s = dt.ChangeToTaiwanYear("yyy/MM/dd ddd");
            //Assert
            Assert.AreEqual(Ans, s);
        }
        [TestMethod]
        public void 測試字串切割()
        {
            //Arrange
            string ans = "12345";
            string sourceStr = "12345||1234545";
            //Action
            string[] result = sourceStr.SplitByString("||");

            //Assert
            Assert.AreEqual(ans, result[0]);
            //SplitByString
        }
        [TestMethod]
        public void 測試爬蟲程式()
        {
            //Arrange
            string ans = "{\"COUN\":\"新竹市\",\"TOWN\":\"\",\"X97\":\"250962.75\",\"Y97\":\"2740788.87\",\"X84\":\"121.009520533\",\"Y84\":\"24.7743887936\",\"ACCURACY\":\"2\"}";
            string sourceStr = "http://egis.moea.gov.tw/MoeaEGFxData/GetAddr/SearchAddr.ashx?addr=%E6%96%B0%E7%AB%B9%E5%B8%82%E5%B7%A5%E6%A5%AD%E6%9D%B1%E5%9B%9B%E8%B7%AF24%E4%B9%8B1%E8%99%9F";
            //Action
            string responseStr = sourceStr.GetResponseStr("GET", "[application/x-www-form-urlencoded]", "", Encoding.UTF8);
            //Assert
            Assert.AreEqual(ans, responseStr);


        }
        [TestMethod]
        public void 測試地址轉換取得座標()
        {
            System.Web.Script.Serialization.JavaScriptSerializer jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //Arrange
            AddrXY testAddrxy = new AddrXY();
            testAddrxy.COUN = "新北市";
            testAddrxy.TOWN = "中和區";
            testAddrxy.X97 = "299241.5705";
            testAddrxy.Y97 = "2765498.4886";
            testAddrxy.X84 = "121.487813204";
            testAddrxy.Y84 = "24.9966806709";
            testAddrxy.ACCURACY = "1";

            string sourceStr = "http://egis.moea.gov.tw/MoeaEGFxData/GetAddr/SearchAddr.ashx?addr=%E6%96%B0%E5%8C%97%E5%B8%82%E4%B8%AD%E5%92%8C%E5%8D%80%E5%BB%BA%E4%B8%80%E8%B7%AF186%E8%99%9F";
            //Action
            string responseStr = sourceStr.GetResponseStr("GET", "[application/x-www-form-urlencoded]", "", Encoding.UTF8);
            AddrXY addrXY = jsonSerializer.Deserialize<AddrXY>(responseStr);
            //Assert
            Assert.IsTrue(testAddrxy.EqualsObject(addrXY));
        }

        [TestMethod]
        public void 測試經緯度轉經濟區()
        {
            //Arrange
            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
            AddrXY testAddrxy = new AddrXY();
            testAddrxy.COUN = "新北市";
            testAddrxy.TOWN = "中和區";
            testAddrxy.X97 = "299241.5705";
            testAddrxy.Y97 = "2765498.4886";
            testAddrxy.X84 = "121.487813204";
            testAddrxy.Y84 = "24.9966806709";
            testAddrxy.ACCURACY = "1";
            //一級、二級、三級劃設統計區(透過ArcGis，透過面回傳屬性資料)
            string SearchCodeURL = @"http://124.219.79.204/arcgis/rest/services/EGIS/MoeaCode_TW/MapServer/1/query?geometry=";
            // 是最小單元統計區(同上)
            string SearchCodeBaseURL = @"http://124.219.79.158/arcgis/rest/services/MoeaCode_TW/MapServer/2/query?geometry=";
            string GetCodeurl = SearchCodeURL;
            string GetCodeBaseurl = SearchCodeBaseURL;
            string Serice_CodePer = "&geometryType=esriGeometryPoint&spatialRel=esriSpatialRelIntersects&returnCountOnly=false&returnIdsOnly=false&returnGeometry=124,302false&f=json&outFields=COUN_ID,COUN_NA,TOWN_ID,TOWN_NA,CODE3,CODE2,CODE1";
            string Serice_Codebaseper = "&geometryType=esriGeometryPoint&spatialRel=esriSpatialRelIntersects&returnCountOnly=false&returnIdsOnly=false&returnGeometry=false&f=json&outFields=CODEBASE";
            AddrUnit resultAddrUnit = new AddrUnit();

            resultAddrUnit.COUN_ID = "65000";
            resultAddrUnit.COUN_NA = "新北市";
            resultAddrUnit.TOWN_ID = "6500300";
            resultAddrUnit.TOWN_NA = "中和區";
            resultAddrUnit.CODE3 = "A650030044";
            resultAddrUnit.CODE2 = "A65003004424";
            resultAddrUnit.CODE1 = "A6500300442401";
            resultAddrUnit.CODEBASE = "A0103-0802-00";
            //Action
            string CodeServer_url = GetCodeurl + testAddrxy.X97 + "," + testAddrxy.Y97 + Serice_CodePer;
            string CodebaseServer_url = GetCodeBaseurl + testAddrxy.X97 + "," + testAddrxy.Y97 + Serice_Codebaseper;
            string ResultCodeServer_url = CodeServer_url.GetResponseStr("GET", CodeServer_url, "", Encoding.UTF8);
            AddrCode addrCode = jsonSerializer.Deserialize<AddrCode>(ResultCodeServer_url);
            string ResultCodebaseServer_url = CodebaseServer_url.GetResponseStr("GET", CodebaseServer_url, "", Encoding.UTF8);
            AddrCodeBase addrCodeBase = jsonSerializer.Deserialize<AddrCodeBase>(ResultCodebaseServer_url);

            AddrUnit addrUnit = new AddrUnit();

            addrUnit.COUN_ID = addrCode.features[0].attributes.COUN_ID.ToString().Trim();
            addrUnit.COUN_NA = addrCode.features[0].attributes.COUN_NA.ToString().Trim();
            addrUnit.TOWN_ID = addrCode.features[0].attributes.TOWN_ID.ToString().Trim();
            addrUnit.TOWN_NA = addrCode.features[0].attributes.TOWN_NA.ToString().Trim();
            addrUnit.CODE3 = addrCode.features[0].attributes.CODE3.ToString().Trim();
            addrUnit.CODE2 = addrCode.features[0].attributes.CODE2.ToString().Trim();
            addrUnit.CODE1 = addrCode.features[0].attributes.CODE1.ToString().Trim();
            addrUnit.CODEBASE = addrCodeBase.features[0].attributes.CODEBASE.ToString().Trim();
            //Assert
            Assert.IsTrue(addrUnit.EqualsObject(resultAddrUnit));

        }
        [TestMethod]
        public void 測試取得地址統計區()
        {
            //Arrange
            AddrUnit resultAddrUnit = new AddrUnit();
            resultAddrUnit.COUN_ID = "65000";
            resultAddrUnit.COUN_NA = "新北市";
            resultAddrUnit.TOWN_ID = "6500300";
            resultAddrUnit.TOWN_NA = "中和區";
            resultAddrUnit.CODE3 = "A650030044";
            resultAddrUnit.CODE2 = "A65003004424";
            resultAddrUnit.CODE1 = "A6500300442401";
            resultAddrUnit.CODEBASE = "A0103-0802-00";
            string sourceStr = "新北市中和區建一路186號";

            //Action
            AddrUnit addrUnit = sourceStr.GetAddrUnit();
            //Assert
            Assert.IsTrue(resultAddrUnit.EqualsObject(addrUnit));
        }
        public static void 測試物件相等()
        {
            Class1 c1 = new Class1();
            Class1 c1Cory = new Class1();
            Class2 c2 = new Class2();
            c2.ChildID = 21;
            bool Iseqals= c1.Equals(c1Cory);
            
        }
        [TestMethod]
        public void 測試dt轉物件()
        {
            //Arrange
            DataTable table = new DataTable("childTable");
            DataColumn column;
            DataRow row;
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.Int32");
            column.ColumnName = "ChildID";
            column.AutoIncrement = true;
            column.Caption = "ID";
            column.ReadOnly = true;
            column.Unique = true;
            table.Columns.Add(column);
            row = table.NewRow();
            row["childID"] = 1;
            table.Rows.Add(row);

            //Action
            Class2 o = table.DataTableToEntities<Class2>().First();

            //Assert
            Assert.AreEqual(o.ChildID, 1);


        }

        private class Class1
        {
            public int ParentID { get; set; }
            public Class2 ChildID { get; set; }
            public Class1()
            {
                this.ChildID = new Class2();

            }

        }
        private class Class2
        {
            public int ChildID { get; set; }
            public Class2()
            {
                this.ChildID = 1;
            }
        }
        //Arrange
        //Action
        //Assert

    }
}
