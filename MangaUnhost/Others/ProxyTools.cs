using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MangaUnhost.Others {
    public static class ProxyTools {
        public static EventHandler OnLoadProxies;
        public static EventHandler OnProxiesLoaded;
        static List<string> BlackList = new List<string>();

        const int PROXIES = 3;//Big values = more slow but more safe, small values = more fast, but less safe
        static string[] ProxyList = new string[PROXIES + 1];
        static int pid = 0;
        public static string WorkingProxy = null;
        public static string Proxy {
            get {
                try {
                    if (ProxyList[1] == null || EverytingBlacklisted)
                        RefreshProxy();

                    if (pid >= ProxyList.Length)
                        pid = 0;

                    string CurrentProxy = ProxyList[pid++];
                    if (BlackList.Contains(CurrentProxy) && CurrentProxy != null)
                        return Proxy;

                    return CurrentProxy;
                } catch {
                    return null;
                }
            }
        }

        private static bool EverytingBlacklisted => (from x in ProxyList where !BlackList.Contains(x) && x != null select x).Count() == 0;

        public static void BlackListProxy(string Proxy) => BlackList.Add(Proxy);

        public static void RefreshProxy() {
            OnLoadProxies?.Invoke(null, null);

            ProxyList = new string[PROXIES + 1];
            string[] Proxies = FreeProxy();
            if (Proxies.Length < PROXIES) {
                Proxies = Proxies.Concat(new string[PROXIES - Proxies.Length]).ToArray();
            }

            for (int i = 0; i < PROXIES; i++) {
                Proxies[i] = Proxies[i]?.ToLower().Replace("http://", "").Replace("https://", "");
                if (BlackList.Contains(Proxies[i]) || !ValidateProxy(Proxies[i])) {
                    Proxies[i--] = GimmeProxy();
                    continue;
                }

                ProxyList[i + 1] = Proxies[i];
            }
            ProxyList[0] = null;

            OnProxiesLoaded?.Invoke(null, null);
        }

        public static string UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36 Edg/83.0.478.45";

        const string ProxyListAPI = "https://www.proxy-list.download/api/v1/get?type=http";
        const string GimmeProxyAPI = "http://gimmeproxy.com/api/getProxy?get=true&post=true&cookies=true&supportsHttps=true&protocol=http&minSpeed=60";
        const string PubProxyAPI = "http://pubproxy.com/api/proxy?google=true&post=true&limit=10&format=txt&speed=20&type=http";
        public static string[] FreeProxy() {
            string Response = DownloadString(PubProxyAPI);
            if (Response == null) {
                Response = DownloadString(ProxyListAPI);
            }
            return Response.Replace("\r\n", "\n").Split('\n');
        }

        public static string GimmeProxy() {
            string Reply = string.Empty;
            string Proxy = null;
            while (Reply == string.Empty) {
                try {
                    string Response = DownloadString(GimmeProxyAPI).Replace(@" ", "");
                    Proxy = DataTools.ReadJson(Response, "curl");
                    if (string.IsNullOrWhiteSpace(Proxy))
                        continue;

                    if (ValidateProxy(Proxy))
                        break;
                } catch {
                    break;
                }
            }
            return Proxy;
        }

        public static string DownloadString(string URL) {
            try {
                WebClient Client = new WebClient();
                if (WorkingProxy != null)
                    Client.Proxy = new WebProxy(WorkingProxy);
                return Client.DownloadString(URL);
            } catch {
                return null;
            }
        }

        public static bool ValidateProxy(string Proxy) {
            bool? Result = null;

            var Thread = new System.Threading.Thread(() => {
                try {
                    using (var client = new WebClient()) {
                        client.Proxy = new WebProxy(Proxy);
                        using (client.OpenRead("http://clients3.google.com/generate_204"))
                            Result = true;

                    }
                } catch {
                    Result = false;
                }
            });


            DateTime Begin = DateTime.Now;
            Thread.Start();

            while ((DateTime.Now - Begin).TotalSeconds <= 10 && !Result.HasValue) {
                System.Threading.Thread.Sleep(10);
            }
            Thread?.Abort();

            return Result ?? false;
        }
    }
}
