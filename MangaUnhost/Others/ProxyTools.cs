using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Web.Script.Serialization;

namespace MangaUnhost.Others
{
    internal static class ProxyTools
    {

        static List<string> BlackList = new List<string>();

        const int PROXIES = 4;//Big values = more slow but more safe, small values = more fast, but less safe
        static string[] ProxList = new string[PROXIES + 1];
        static int pid = 0;
        internal static string Proxy
        {
            get
            {
                try
                {
                    if (ProxList[1] == null)
                        RefreshProxy();

                    if (pid >= ProxList.Length)
                        pid = 0;

                    return ProxList[pid++];
                }
                catch { return null; }
            }
        }



        internal static void BlackListProxy(string Proxy) => BlackList.Add(Proxy);


        internal static void RefreshProxy()
        {
            ProxList = new string[PROXIES + 1];
            string[] Proxies = FreeProxy();
            for (int i = 0, x = 0; i < PROXIES; i++)
            {
                Proxies[i] = Proxies[i].ToLower().Replace("http://", "").Replace("https://", "");
                if (BlackList.Contains(Proxies[i]) || !ValidateProxy(Proxies[i]))
                {
                    Proxies[i] = Proxies[PROXIES + x++];
                    continue;
                }

                ProxList[i + 1] = Proxies[i];
            }
            ProxList[0] = null;
        }

        internal const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36";

        const string GimmeProxyAPI = "http://gimmeproxy.com/api/getProxy?get=true&post=true&cookies=true&supportsHttps=true&protocol=http&minSpeed=60";
        const string PubProxyAPI = "http://pubproxy.com/api/proxy?google=true&post=true&limit=10&format=txt&speed=20&type=http";
        const string ProxyListApi = "https://www.proxy-list.download/api/v0/get?l=en&t=http";
        internal static string[] FreeProxy()
        {
            var Json = new WebClient().DownloadString(ProxyListApi);
            var Obj = new JavaScriptSerializer().DeserializeObject(Json);
            Obj = ((object[])Obj).First();
            var Resp = (Dictionary<string, object>)Obj;
            var Listas = (from x in (object[])Resp["LISTA"] select (Dictionary<string, object>)x);
            return (from x in Listas select $"{x["IP"]}:{x["PORT"]}").ToArray();
        }


        internal static string GimmeProxy()
        {
            string Reply = string.Empty;
            string Proxy = null;
            while (Reply == string.Empty)
            {
                string Response = new WebClient().DownloadString(GimmeProxyAPI).Replace(@" ", "");
                Proxy = ReadJson(Response, "curl");
                if (string.IsNullOrWhiteSpace(Proxy))
                    return FreeProxy().First();

                if (ValidateProxy(Proxy))
                    break;
            }
            return Proxy;
        }

        internal static bool ValidateProxy(string Proxy)
        {
            bool Result = false;

            var Thread = new Thread(() =>
            {
                try
                {
                    HttpWebRequest Request = WebRequest.Create("http://clients3.google.com/generate_204") as HttpWebRequest;
                    Request.Timeout = 10000;
                    Request.Proxy = new WebProxy(Proxy);
                    var Response = (HttpWebResponse)Request.GetResponse();
                    if (Response.StatusCode == HttpStatusCode.NoContent)
                        Result = true;
                }
                catch { }
            });


            DateTime Begin = DateTime.Now;
            Thread.Start();

            while ((DateTime.Now - Begin).TotalSeconds <= 10 && Thread.IsAlive)
            {
                Thread.Sleep(10);
            }
            Thread?.Abort();

            return Result;
        }


        static string ReadJson(string JSON, string Name)
        {
            string Finding = string.Format("\"{0}\":", Name);
            int Pos = JSON.IndexOf(Finding) + Finding.Length;
            if (Pos - Finding.Length == -1)
                return null;

            string Cutted = JSON.Substring(Pos, JSON.Length - Pos).TrimStart(' ', '\n', '\r');
            char Close = Cutted.StartsWith("\"") ? '"' : ',';
            Cutted = Cutted.TrimStart('"');
            string Data = string.Empty;
            foreach (char c in Cutted)
            {
                if (c == Close)
                    break;
                Data += c;
            }
            if (Data.Contains("\\"))
                throw new Exception("Ops... Unsupported Json Format...");

            return Data;
        }

    }
}
