using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

internal static class Tools {

    internal static EventHandler OnLoadProxies;
    internal static EventHandler OnProxiesLoaded;
    static List<string> BlackList = new List<string>();
    
    const int PROXIES = 3;//Big values = more slow but more safe, small values = more fast, but less safe
    static string[] ProxyList = new string[PROXIES + 1];
    static int pid = 0;
    internal static string WorkingProxy = null;
    internal static string Proxy {
        get {
            if (ProxyList[1] == null || EverytingBlacklisted)
                RefreshProxy();

            if (pid >= ProxyList.Length)
                pid = 0;

            string CurrentProxy = ProxyList[pid++];
            if (BlackList.Contains(CurrentProxy) && CurrentProxy != null) 
                return Proxy;
            
            return CurrentProxy;
        }
    }

    private static bool EverytingBlacklisted => (from x in ProxyList where !BlackList.Contains(x) && x != null select x).Count() == 0;

    internal static void BlackListProxy(string Proxy) => BlackList.Add(Proxy);

    internal static void RefreshProxy() {
        OnLoadProxies?.Invoke(null, null);

        ProxyList = new string[PROXIES + 1];
        string[] Proxies = FreeProxy();
        for (int i = 0; i < PROXIES; i++) {
            Proxies[i] = Proxies[i].ToLower().Replace("http://", "").Replace("https://", "");
            if (BlackList.Contains(Proxies[i]) || !ValidateProxy(Proxies[i])) {
                Proxies[i--] = GimmeProxy();
                continue;
            }

            ProxyList[i + 1] = Proxies[i];
        }
        ProxyList[0] = null;

        OnProxiesLoaded?.Invoke(null, null);
    }

    internal const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36 OPR/54.0.2952.64";

    const string GimmeProxyAPI = "http://gimmeproxy.com/api/getProxy?get=true&post=true&cookies=true&supportsHttps=true&protocol=http&minSpeed=60";
    const string PubProxyAPI = "http://pubproxy.com/api/proxy?google=true&post=true&limit=10&format=txt&speed=20&type=http";
    internal static string[] FreeProxy() {
        return DownloadString(PubProxyAPI).Replace("\r\n", "\n").Split('\n');
    }

    internal static string GimmeProxy() {
        string Reply = string.Empty;
        string Proxy = null;
        while (Reply == string.Empty) {
            string Response = DownloadString(GimmeProxyAPI).Replace(@" ", "");
            Proxy = ReadJson(Response, "curl");
            if (string.IsNullOrWhiteSpace(Proxy))
                continue;

            if (ValidateProxy(Proxy))
                break;
        }
        return Proxy;
    }

    internal static string DownloadString(string URL) {
        WebClient Client = new WebClient();
        if (WorkingProxy != null)
            Client.Proxy = new WebProxy(WorkingProxy);
        return Client.DownloadString(URL);
    }

    internal static bool ValidateProxy(string Proxy) {
        bool? Result = null;

        var Thread = new Thread(() => {
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
            Thread.Sleep(10);
        }
        Thread?.Abort();

        return Result ?? false;
    }


    static string ReadJson(string JSON, string Name) {
        string Finding = string.Format("\"{0}\":", Name);
        int Pos = JSON.IndexOf(Finding) + Finding.Length;
        if (Pos - Finding.Length == -1)
            return null;

        string Cutted = JSON.Substring(Pos, JSON.Length - Pos).TrimStart(' ', '\n', '\r');
        char Close = Cutted.StartsWith("\"") ? '"' : ',';
        Cutted = Cutted.TrimStart('"');
        string Data = string.Empty;
        foreach (char c in Cutted) {
            if (c == Close)
                break;
            Data += c;
        }
        if (Data.Contains("\\"))
            throw new Exception("Ops... Unsupported Json Format...");

        return Data;
    }
}
