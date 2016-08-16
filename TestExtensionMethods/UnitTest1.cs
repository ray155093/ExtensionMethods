using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExtensionMethods;
using System.Text;
using ExtensionMethods.DataModel;


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
        public void 測試地址轉換()
        {
            //System.Web.Script.Serialization. JavaScriptSerializer jsonSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            ////Arrange
            //AddrXY addrxy = new AddrXY();
            //addrxy.COUN = "新北市";
            //addrxy.TOWN = "中和區";
            //addrxy.X97 = "299241.5705";
            //addrxy.Y97 = "2765498.4886";
            //addrxy.X84 = "121.487813204";
            //addrxy.Y84 = "24.9966806709";
            //addrxy.ACCURACY = "1";

            //string sourceStr = "http://egis.moea.gov.tw/MoeaEGFxData/GetAddr/SearchAddr.ashx?addr=%E6%96%B0%E5%8C%97%E5%B8%82%E4%B8%AD%E5%92%8C%E5%8D%80%E5%BB%BA%E4%B8%80%E8%B7%AF186%E8%99%9F";
            ////Action
            //string responseStr = sourceStr.GetResponseStr("GET", "[application/x-www-form-urlencoded]", "", Encoding.UTF8);
            //AddrXY addrXY = jsonSerializer.Deserialize<AddrXY>(responseStr);
            ////Assert
            //Assert.AreEqual(addrxy.COUN, addrxy);
        }
        public static void 測試物件相等()
        {
            
        }
        //Arrange
        //Action
        //Assert
      
    }
}
