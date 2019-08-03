using CefSharp;
using CefSharp.OffScreen;
using CefSharp.RequestEventHandler;
using System;
using System.Collections.Generic;
using System.Net;

namespace MangaUnhost.Browser {
    public static class RequestTools {
        public static void UseProxy(this ChromiumWebBrowser Browser, WebProxy Proxy) {
            if (Browser.RequestHandler == null || !(Browser.RequestHandler is RequestEventHandler)) {
                Browser.RequestHandler = new RequestEventHandler();
            }

            Browser.BrowserInitialized += (Sender, Args) => {
                var WBroser = (ChromiumWebBrowser)Sender;
                var HBrowser = WBroser.GetBrowserHost();

                var ProxyInfo = new Dictionary<string, object>();
                ProxyInfo["mode"] = "fixed_servers";
                ProxyInfo["server"] = $"{Proxy.Address.Scheme}://{Proxy.Address.Host}:{Proxy.Address.Port}";

                if (!HBrowser.RequestContext.SetPreference("proxy", ProxyInfo, out string Error))
                    throw new Exception("Failed to Set the Proxy: " + Error);
            };

            ((RequestEventHandler)Browser.RequestHandler).SetCredentials(Proxy.Credentials as NetworkCredential);
            ((RequestEventHandler)Browser.RequestHandler).GetAuthCredentialsEvent += (Sender, Args) => {
                var ReqEvHandler = ((RequestEventHandler)Sender);
                if (Args.IsProxy) {
                    Args.Callback.Continue(ReqEvHandler.Credential.UserName, ReqEvHandler.Credential.Password);
                    Args.ContinueAsync = true;
                    return;
                }
                Args.ContinueAsync = false;
            };
        }
    }
}
