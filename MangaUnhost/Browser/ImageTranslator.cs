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
            Browser.EvaluateScriptUnsafe(Properties.Resources.toDataURL);
        }

        public byte[] TranslateImage(byte[] Image)
        {
            var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), "png");
            File.WriteAllBytes(tmpPath, Image);

            /*            
            Browser.ShowDevTools();

            var test = new BrowserPopup(Browser, () => { return false; });
            test.Show();
            */
            

            //select image
            TargetFile = tmpPath;

            int waiting = 0;

            string Bounds = null;
            while (Bounds == null)
            {
                if (waiting++ > 15)
                    throw new Exception("Image Translation Failed");

                Bounds = Browser.EvaluateScriptUnsafe<string>("var target = XPATH(\"//input[@type='file' and contains(@accept, 'image')]/..\", false); " + Properties.Resources.targetGetBounds);
                ThreadTools.Wait(1000, true);
            }

            InputTools.GetBoundsCoords(Bounds, out int X, out int Y, out int Width, out int Height);

            var Target = new Point(X + (Width / 2), Y + (Height / 2));

            var Rand = new Random();
            var Move = CursorTools.CreateMove(Rand.Next(0, 50), Rand.Next(5, 60), Target.X, Target.Y, 10);
            Browser.ExecuteMove(Move);
            Browser.ExecuteClick(Target);


            waiting = 0;

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
