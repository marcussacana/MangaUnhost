using CefSharp;
using CefSharp.OffScreen;
using CefSharp.EventHandler;
using System;
using System.Collections.Generic;
using System.Net;
using MangaUnhost.Others;
using Microsoft.VisualBasic;
using CefSharp.DevTools;
using System.Windows;
using System.Linq;

namespace MangaUnhost.Browser
{
    public static class RequestTools
    {
        public static void UseProxy(this ChromiumWebBrowser Browser, WebProxy Proxy)
        {
            if (Browser.RequestHandler == null || !(Browser.RequestHandler is RequestEventHandler))
            {
                Browser.RequestHandler = new RequestEventHandler();
            }

            Browser.BrowserInitialized += (Sender, Args) =>
            {
                var WBroser = (ChromiumWebBrowser)Sender;
                var HBrowser = WBroser.GetBrowserHost();

                var ProxyInfo = new Dictionary<string, object>();
                ProxyInfo["mode"] = "fixed_servers";
                ProxyInfo["server"] = $"{Proxy.Address.Scheme}://{Proxy.Address.Host}:{Proxy.Address.Port}";

                if (!HBrowser.RequestContext.SetPreference("proxy", ProxyInfo, out string Error))
                    throw new Exception("Failed to Set the Proxy: " + Error);
            };

            ((RequestEventHandler)Browser.RequestHandler).SetCredentials(Proxy.Credentials as NetworkCredential);
            ((RequestEventHandler)Browser.RequestHandler).GetAuthCredentialsEvent += (Sender, Args) =>
            {
                var ReqEvHandler = ((RequestEventHandler)Sender);
                if (Args.IsProxy)
                {
                    Args.Callback.Continue(ReqEvHandler.Credential.UserName, ReqEvHandler.Credential.Password);
                    Args.ContinueAsync = true;
                    return;
                }
                Args.ContinueAsync = false;
            };
        }

        private static void SetUserAgent(this IBrowser Browser, string UserAgent)
        {
            using var DevTools = Browser.GetDevToolsClient();
            DevTools.Emulation.SetUserAgentOverrideAsync(UserAgent);
        }

        public static void SetUserAgent(this IWebBrowser Browser, string UserAgent)
        {
            var UA = UserAgent;
            if (Browser.LifeSpanHandler == null || !(Browser.LifeSpanHandler is LifeSpanEventHandler))
            {
                Browser.LifeSpanHandler = new LifeSpanEventHandler();
            }

            ((LifeSpanEventHandler)Browser.LifeSpanHandler).OnBeforePopupEvent += (Sender, Args) => {

                if (Args.BrowserControl is CefSharp.WinForms.ChromiumWebBrowser wChromeBrowser)
                {
                    Args.NewBrowser = new CefSharp.WinForms.ChromiumWebBrowser(Args.TargetURL);
                }

                if (Args.BrowserControl is ChromiumWebBrowser oChromeBrowser)
                {
                    Args.NewBrowser = new ChromiumWebBrowser(Args.TargetURL);
                }

                Args.NewBrowser.SetUserAgent(UA);
            };

            if (!Browser.IsBrowserInitialized)
            {
                if (Browser is CefSharp.WinForms.ChromiumWebBrowser wChromeBrowser)
                {
                    wChromeBrowser.IsBrowserInitializedChanged += (Sender, This) => { 
                        ((IWebBrowser)Sender).SetUserAgent(UA);
                    };
                }

                if (Browser is ChromiumWebBrowser oChromeBrowser)
                {
                    oChromeBrowser.BrowserInitialized += (Sender, This) => {
                        ((IWebBrowser)Sender).SetUserAgent(UA);
                    };
                }
            }
            else
            {
                Browser.LoadingStateChanged += (Sender, Args) =>
                {
                    Args.Browser.SetUserAgent(UA);
                };

                Browser.GetBrowser().SetUserAgent(UA);
            }
            
            if (!(Browser.RequestHandler is RequestEventHandler))
                Browser.RequestHandler = new RequestEventHandler();


            var UAHelper = MangaUnhost.Browser.UserAgent.Parse(UA);
            var BrowserExt = UAHelper.Extensions.LastOrDefault(x => new string[] { "Edg", "Chrome", "OPR", "Firefox" }.Contains(x.Name));

            if (BrowserExt == null)
                BrowserExt = UAHelper.Extensions.LastOrDefault();

            if (BrowserExt == null)
                return;

            var Name = BrowserExt.Name switch
            {
                "Edg" => "Microsoft Edge",
                "Chrome" => "Google Chrome",
                "OPR" => "Opera",
                "Firefox" => "Firefox",
                _ => BrowserExt.Name
            };

            var Platform = UAHelper.System.First((x) => x.ToLower().Contains("win") || x.ToLower().Contains("linux") || x.ToLower().Contains("android")).ToLower();
            var PlatformName = "Windows";

            var PlatformVer = "10.0";


            if (Platform.Contains("win"))
                PlatformName = "Windows";
            if (Platform.Contains("linux"))
                PlatformName = "Linux";
            if (Platform.Contains("android"))
                PlatformName = "Android";

            var Version = BrowserExt.Version;
            var ShortVersion = Version.Split('.').First();

            Browser.RegisterWebRequestHandlerEvents((sender, args) => {
                args.Headers["Sec-Ch-Ua"] = $"\"Not_A Brand\";v=\"24\", \"Chromium\";v=\"{ShortVersion}\", \"{Name}\";v=\"{ShortVersion}\"";
                args.Headers["Sec-Ch-Ua-Arch"] = Environment.Is64BitProcess ? "x64" : "x86";
                args.Headers["Sec-Ch-Ua-Bitness"] = Environment.Is64BitOperatingSystem ? "64" : "32";
                args.Headers["Sec-Ch-Ua-Full-Version-List"] = $"\"Not_A Brand\";v=\"24\", \"Chromium\";v=\"{Version}\", \"{Name}\";v=\"{Version}\"";
                args.Headers["Sec-Ch-Ua-Full-Version"] = Version;
                args.Headers["Sec-Ch-Ua-Mobile"] = "?0";
                args.Headers["Sec-Ch-Ua-Model"] = "";
                args.Headers["Sec-Ch-Ua-Platform"] = PlatformName;
                args.Headers["Sec-Ch-Ua-Platform-Version"] = PlatformName == "Linux" ? "" : PlatformVer;
                args.Headers["Sec-Ch-Ua-Wow64"] = (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "?1" : "?0");
            }, null);

            Browser.EarlyInjection($"Object.defineProperty(navigator, 'userAgentData', {{value: {{brands:[ {{ brand: 'Not_A Brand', version: '24' }}, {{brand: 'Chromium', version: '{ShortVersion}'}}, {{brand: '{Name}', version: '{ShortVersion}'}}], mobile: false, platform: '{PlatformName}'}}, configurable: false, writable: false,}}); window.cefSharp = undefined; Object.freeze(window.cefSharp); window.CefSharp = undefined; Object.freeze(window.CefSharp); window.cefQuery = undefined; Object.freeze(window.cefQuery);");
        }

        public static void BypassGoogleCEFBlock(this IWebBrowser Browser)
        {
            Browser.SetUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:128.0) Gecko/20100101 Firefox/128.0");
        }

        //public delegate void Func<in T>(T arg);
        //public static void CatchResources(this ChromiumWebBrowser Browser, bool SkipFrames, Func<NovelResource> ResourceEvent, Func<NovelScript> ScriptEvent)
        //{
        //    Browser.RequestHandler = new RequestEventHandler();

        //    ((RequestEventHandler)Browser.RequestHandler).OnResourceRequestEvent += (Sender, Args) =>
        //    {
        //        try
        //        {
        //            if (Args.Request.TransitionType == TransitionType.AutoSubFrame && SkipFrames)
        //                return;

        //            if (Args.Request.Method != "GET" || Args.Cancel)
        //                return;

        //            var Request = WebRequest.CreateHttp(Args.Request.Url);
        //            Request.Method = Args.Request.Method;
        //            Request.Referer = Args.Request.ReferrerUrl;

        //            using (var Response = Request.GetResponse())
        //            using (var Stream = Response.GetResponseStream())
        //            using (MemoryStream Memory = new MemoryStream())
        //            {
        //                string Name = Path.GetFileName(Args.Request.Url.Substring(null, "?", IgnoreMissmatch: true));
        //                bool HasDisposition = Response.Headers.AllKeys.Contains("Content-Disposition");
        //                if (HasDisposition)
        //                {
        //                    string Disposition = Response.Headers["Content-Disposition"];
        //                    if (Disposition.Contains("filename="))
        //                    {
        //                        Disposition = Disposition.Substring("filename=").Trim();
        //                        if (Disposition.StartsWith("\""))
        //                            Name = Disposition.Substring(1).Substring(null, "\"");
        //                        else
        //                            Name = Disposition.Substring(null, " ");
        //                    }
        //                }
        //                if (Name == ".php" || Name == ".aspx" || Name == ".html")
        //                {
        //                    string[] PossibleParamters = new string[] { "FileName", "Filename", "File", "Name", "Storage", "Path" };
        //                    foreach (var Paramter in PossibleParamters)
        //                    {
        //                        if (Args.Request.Url.Contains(Paramter))
        //                        {
        //                            Name = Args.Request.Url.GetUrlParamter(Paramter);
        //                            break;
        //                        }
        //                        if (Args.Request.Url.Contains(Paramter.ToLower()))
        //                        {
        //                            Name = Args.Request.Url.GetUrlParamter(Paramter.ToLower());
        //                            break;
        //                        }
        //                    }
        //                }

        //                string MimeType = Response.ContentType.Substring(null, ";").Trim();
        //                if (string.IsNullOrEmpty(MimeType))
        //                    MimeType = ResourceHandler.GetMimeType(Path.GetExtension(Name));

        //                if (MimeType == "text/html")
        //                    return;

        //                switch (Args.Request.ResourceType)
        //                {
        //                    case ResourceType.Script:
        //                    case ResourceType.Stylesheet:
        //                        Stream.CopyTo(Memory);
        //                        ScriptEvent(new NovelScript()
        //                        {
        //                            FileName = Name,
        //                            Mime = MimeType,
        //                            Script = Encoding.UTF8.GetString(Memory.ToArray())
        //                        });
        //                        break;
        //                    case ResourceType.MainFrame:
        //                        break;
        //                    default:
        //                        ResourceEvent(new NovelResource()
        //                        {
        //                            FileName = Name,
        //                            Mime = MimeType,
        //                            Data = Memory.ToArray()
        //                        });
        //                        break;
        //                }

        //            }
        //        }
        //        catch { }
        //    };
        //}
    }

}
