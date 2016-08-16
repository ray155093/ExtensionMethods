using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    public static class CrawlerHelper
    {
   
        /// <summary>
        /// 發出一個Request並取回Response
        /// </summary>
        /// <param name="Url">服務網址</param>
        /// <param name="HttpMethod">使用的httpMethod</param>
        /// <param name="RequestContentType">Request的格式 正常來說應該是用[application/x-www-form-urlencoded]   /// 這邊列出四種
        ///  1.[application/x-www-form-urlencoded]
        //   2.[multipart/form data]
        //   3.[application/json]
        //   4.[text/plain]</param>
        /// <param name="ParamStr">body資訊</param>
        /// <param name="EncodingType">編碼方式</param>
        /// <returns></returns>
        public static string GetResponseStr(this string Url, string HttpMethod, string RequestContentType, string ParamStr, Encoding EncodingType)
        {

            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            string ResponseResultStr = null;
            request.Method = HttpMethod;    // 方法
            request.KeepAlive = false; //是否保持連線
            request.ContentType = RequestContentType;//提交參數的方式，共4種

            if (HttpMethod.ToUpper() == "POST")
            {
                using (Stream reqStream = request.GetRequestStream())
                {
                    byte[] bs = Encoding.ASCII.GetBytes(ParamStr);
                    reqStream.Write(bs, 0, bs.Length);
                }
            }
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    StreamReader sr = new StreamReader(response.GetResponseStream(), EncodingType);
                    ResponseResultStr = sr.ReadToEnd();
                    sr.Close();
                }
            }
            catch (System.Net.WebException)
            {
                ResponseResultStr = "連線出現問題了";
            }
            return ResponseResultStr;
        }
    }
}
