using CefSharp;
using CefSharp.EventHandler;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using System;
using System.Drawing;
using System.IO;

namespace MangaUnhost.Browser
{
    internal class ImageTranslator : IDisposable
    {
        string SourceLang, TargetLang;

        ChromiumWebBrowser Browser;
        public ImageTranslator(string SourceLang, string TargetLang)
        {
            this.SourceLang = SourceLang.ToLower();
            this.TargetLang = TargetLang.ToLower();

            Browser = new ChromiumWebBrowser();
            Browser.DialogHandler = new DialogEventHandler();
            ((DialogEventHandler)Browser.DialogHandler).FileDialog += ImageTranslator_FileDialog;
            Browser.WaitInitialize();
            Browser.SetUserAgent(ProxyTools.UserAgent);
            Reload(this.SourceLang, this.TargetLang);
        }

        string TargetFile = null;
        private void ImageTranslator_FileDialog(object sender, FileDialogEventArgs e)
        {
            if (e.IsFolder || TargetFile == null || !File.Exists(TargetFile))
                return;

            e.Handled = true;
            e.SelectedPath = TargetFile;

            TargetFile = null;
        }

        public void Reload() => Reload(SourceLang, TargetLang);
        private void Reload(string SourceLang, string TargetLang)
        {
            Browser.WaitForLoad($"https://translate.google.com.br/?sl={SourceLang}&tl={TargetLang}&op=images");
            Browser.InjectXPATH();
            Browser.EvaluateScript(Properties.Resources.toDataURL);
        }

        public byte[] TranslateImage(byte[] Image)
        {
            var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), "png");
            File.WriteAllBytes(tmpPath, Image);

            //select image
            TargetFile = tmpPath;

            string Bounds = null;
            while (Bounds == null)
            {
                Bounds = Browser.EvaluateScript<string>("var target = XPATH(\"//input[@type='file' and contains(@accept, 'image')]/..\", false); " + Properties.Resources.targetGetBounds);
                ThreadTools.Wait(1000, true);
            }

            InputTools.GetBoundsCoords(Bounds, out int X, out int Y, out int Width, out int Height);

            var Target = new Point(X + (Width / 2), Y + (Height / 2));

            var Rand = new Random();
            var Move = CursorTools.CreateMove(Rand.Next(0, 50), Rand.Next(5, 60), Target.X, Target.Y, 10);
            Browser.ExecuteMove(Move);
            Browser.ExecuteClick(Target);

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
                if (waiting++ > 15)
                    throw new Exception("Image Translation Failed");

                ThreadTools.Wait(1000, true);
                var Url = Browser.EvaluateScript<string>("XPATH(\"(//img[starts-with(@src, 'blob:')])[last()]\", false).src");
                
                if (Url == null || !Url.StartsWith("blob:"))
                    continue;
                Browser.EvaluateScript<string>($"toDataURL('{Url}', function(dataUrl) {{ globalThis.currentImage = dataUrl; }})");
                break;
            }

            string Data = null;

            do
            {
                Data = Browser.EvaluateScript<string>("currentImage");

                if (Data == null)
                    throw new Exception("Image Translation Failed");

            } while (!Data.StartsWith("data:"));

            Data = Data.Substring(";base64,");

            var NewData = Convert.FromBase64String(Data);

            //close current image
            Browser.EvaluateScript("XPATH(\"(//span[@data-is-tooltip-wrapper]/button[@jsaction and @jscontroller and @aria-label])[last()]\", false).click();");

            return NewData;
        }

        private bool IsLoading()
        {
            return Browser.EvaluateScript<bool>("XPATH(\"(//c-wiz//div[@role='progressbar'])[last()]\", false)?.outerHTML.indexOf('div role') == 1");
        }

        public void Dispose()
        {
            Browser.Dispose();
        }
    }
}
