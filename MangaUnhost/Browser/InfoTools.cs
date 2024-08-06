using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using Microsoft.VisualBasic;
using System;
using System.Threading.Tasks;

namespace MangaUnhost.Browser
{
    public static class InfoTools
    {
        public static bool IsLoading(this IBrowser Browser) => Browser.MainFrame.IsLoading();
        public static bool IsLoading(this IFrame Frame)
        {
            try
            {
                if (Frame.Browser.IsLoading)
                    return true;
                var Status = (string)Frame.EvaluateScriptAsync(Properties.Resources.GetDocumentStatus).GetAwaiter().GetResult().Result;
                
                //Bugfix
                if (Status == null && !Frame.IsMain)
                    return false;

                if (Status?.Trim().ToLower() == "complete")
                    return false;
            }
            catch { return false; }

            return true;
        }

        public static void WaitForLoad(this ChromiumWebBrowser Browser, string Url, int MaxSeconds = 60)
        {
            Browser.WaitInitialize();
            Browser.Load(Url);
            Browser.GetBrowser().WaitForLoad(MaxSeconds);
        }

        public static void WaitForLoad(this CefSharp.WinForms.ChromiumWebBrowser Browser, string Url, int MaxSeconds = 60)
        {
            Browser.WaitInitialize();
            Browser.Load(Url);
            Browser.GetBrowser().WaitForLoad(MaxSeconds);
        }

        public static void WaitForLoad(this ChromiumWebBrowser Browser, int MaxSeconds = 60)
        {
            Browser.WaitInitialize();
            Browser.GetBrowser().WaitForLoad();
        }
        public static void WaitForLoad(this CefSharp.WinForms.ChromiumWebBrowser Browser, string Url)
        {
            Browser.WaitInitialize();
            Browser.Load(Url);
            Browser.GetBrowser().WaitForLoad();
        }

        public static void WaitForLoad(this CefSharp.WinForms.ChromiumWebBrowser Browser)
        {
            Browser.WaitInitialize();
            Browser.GetBrowser().WaitForLoad();
        }

        public static void WaitInitialize(this IWebBrowser Browser, int MaxSeconds = 30)
        {
            DateTime Begin = DateTime.Now;
            while (!Browser.IsBrowserInitialized && (DateTime.Now - Begin).TotalSeconds < MaxSeconds)
                ThreadTools.Wait(5, true);

            if (!Browser.IsBrowserInitialized)
                throw new Exception("Failed to Initialize the CEF Instance");
        }

        public static void WaitForLoad(this IBrowser Browser, int MaxSeconds = 60)
        {
            try
            {
                ThreadTools.Wait(100);
                DateTime Begin = DateTime.Now;
                while (Browser.IsLoading() && (DateTime.Now - Begin).TotalSeconds < MaxSeconds)
                    ThreadTools.Wait(50, true);
            }
            catch { }
        }

        public static void WaitForLoad(this IFrame Frame, int MaxSeconds = 60)
        {
            ThreadTools.Wait(100);
            DateTime Begin = DateTime.Now;
            while (Frame.IsLoading() && (DateTime.Now - Begin).TotalSeconds < MaxSeconds)
                ThreadTools.Wait(50, true);
        }
        public static string GetCurrentUrl(this CefSharp.WinForms.ChromiumWebBrowser Browser) => Browser.GetBrowser().GetCurrentUrl();
        public static string GetCurrentUrl(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetCurrentUrl();
        public static string GetCurrentUrl(this IBrowser Browser) => Browser.MainFrame.Url;

        public static string GetUserAgent(this ChromiumWebBrowser Browser) => Browser.GetBrowser().GetUserAgent();

        public static string GetUserAgent(this IBrowser Browser)
        {
            return (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.GetUserAgent).GetAwaiter().GetResult().Result;
        }

        delegate object Invoker();
    }
}
