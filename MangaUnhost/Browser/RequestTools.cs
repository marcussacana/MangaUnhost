using CefSharp;
using CefSharp.OffScreen;
using CefSharp.RequestEventHandler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

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
