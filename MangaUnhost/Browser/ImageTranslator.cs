using CefSharp;
using CefSharp.EventHandler;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MangaUnhost.Browser
{
    internal class ImageTranslator : IDisposable
    {
        string SourceLang, TargetLang;

        ChromiumWebBrowser Browser;
        public ImageTranslator(string SourceLang, string TargetLang)
        {
            this.SourceLang = SourceLang.ToLowerInvariant();
            this.TargetLang = TargetLang.ToLowerInvariant();

            Browser = new ChromiumWebBrowser();
            Browser.WaitInitialize();
            Browser.SetUserAgent(ProxyTools.UserAgent);
            //Browser.BypassGoogleCEFBlock();
            Reload(this.SourceLang, this.TargetLang);
        }

        public void Reload() => Reload(SourceLang, TargetLang);
        private void Reload(string SourceLang, string TargetLang)
        {
            Browser.WaitForLoad($"https://translate.google.com.br/?sl={SourceLang}&tl={TargetLang}&op=images");
            Browser.InjectXPATH();
            Browser.EvaluateScriptUnsafe(Properties.Resources.toDataURL);
        }

        private static bool DevVisible = false;

        public byte[] TranslateImage(byte[] Image)
        {
            var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), "png");
            File.WriteAllBytes(tmpPath, Image);

            try
            {
#if DEBUG
                /*if (!Debugger.IsAttached)
                {
                    if (!DevVisible)
                    {
                        DevVisible = true;
                        Debugger.Launch();
                        Browser.ShowDevTools();

                        var test = new BrowserPopup(Browser, () => { return false; });
                        test.Show();
                    }
                }
                */
#endif


                var Rand = new Random();
                var Move = CursorTools.CreateMove(Rand.Next(0, 50), Rand.Next(5, 60), Rand.Next(0, 50), Rand.Next(5, 60), 10);
                Browser.ExecuteMove(Move);
                Browser.ExecuteClick(Move.Last().Location);

                var ID = Browser.EvaluateScriptUnsafe<string>("XPATH('//input[contains(@accept, \\'image\\')]', false).id");

                if (ID == null)
                    throw new Exception("File Input Not Found");

                AsyncContext.Run(() => Browser.SetInputFile(ID, tmpPath));


                int waiting = 0;

                while (IsLoading())
                {
                    if (waiting++ > 15)
                        throw new Exception("Image Translation Failed");

                    ThreadTools.Wait(1000, true);
                }

                waiting = 0;

                while (true)
                {
                    if (hasFailed())
                        throw new Exception("Image Translation Failed");

                    if (waiting++ > 15)
                        throw new Exception("Image Translation Failed");

                    ThreadTools.Wait(1000, true);
                    var Url = Browser.EvaluateScriptUnsafe<string>("XPATH(\"(//img[starts-with(@src, 'blob:')])[last()]\", false).src");

                    if (Url == null || !Url.StartsWith("blob:"))
                        continue;

                    Browser.EvaluateScriptUnsafe<string>($"toDataURL('{Url}', function(dataUrl) {{ globalThis.currentImage = dataUrl; }})");
                    break;
                }

                waiting = 0;

                string Data = null;

                do
                {
                    Data = Browser.EvaluateScriptUnsafe<string>("currentImage");


                    if (Data == null)
                    {
                        if (waiting++ > 5)
                            throw new Exception("Image Translation Failed");

                        ThreadTools.Wait(1000, true);
                        Data = "";
                    }

                } while (!Data.StartsWith("data:"));

                Data = Data.Substring(";base64,");

                var NewData = Convert.FromBase64String(Data);

                Reload();

                return NewData;
            }
            finally
            {
                File.Delete(tmpPath);
            }
        }

        private bool hasFailed()
        {
            return Browser.EvaluateScriptUnsafe<bool>("XPATH(\"//div[@role='status']//button\", true).length > 0");
        }

        private bool IsLoading()
        {
            return Browser.EvaluateScriptUnsafe<bool>("XPATH(\"(//c-wiz//div[@role='progressbar'])[last()]\", false)?.outerHTML.indexOf('div role') == 1");
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
