using CefSharp;
using CefSharp.OffScreen;
using CefSharp.RequestEventHandler;
using MangaUnhost.Others;
using Nito.AsyncEx;
using System;
using System.Drawing;

namespace MangaUnhost.Browser {
    public static class reCaptcha {

        public const int BFrameHidden = -9999;
        public static bool IsCaptchaSolved(this ChromiumWebBrowser Browser, bool v3 = false) => Browser.GetBrowser().IsCaptchaSolved(v3);
        public static bool IsCaptchaSolved(this IBrowser Browser, bool v3 = false) {
            if (Browser.GetReCaptchaBFrame() == null)
                return true;

            if (v3)
            {
                try
                {
                    var Point = Browser.GetReCaptchaBFrameRectangle();
                    if (Point.Y < -999 && Point.Height <= 200)
                        return true;
                }
                catch { return true; }

                return false;
            }

            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaGetResponse).GetAwaiter().GetResult().Result;
            if (string.IsNullOrWhiteSpace(Result))
                return false;
            return true;
        }

        /// <summary>
        /// Try Solve the Captcha, And request the user help if needed.
        /// </summary>
        /// <param name="OBrowser">The Browser Instance With the Captcha</param>
        /// <param name="Enforce">Persistent captcha, Don't return while isn't solved.</param>
        public static void TrySolveCaptcha(this ChromiumWebBrowser OBrowser, CaptchaSolverType SolverType = CaptchaSolverType.SemiAuto, bool v3 = false) {
            var Browser = OBrowser.GetBrowser();
            Browser.WaitForLoad();
            ThreadTools.Wait(1500);
            if (SolverType != CaptchaSolverType.Manual) {
                if (!Browser.IsCaptchaSolved(v3)) {
                    Point Cursor = new Point(0, 0);

                    if (!v3)
                    {
                        do
                        {
                            OBrowser.ClickImNotRobot(out Cursor);
                            if (Browser.IsCaptchaSolved(v3))
                                return;
                        } while (Browser.GetReCaptchaBFramePosition().Y == BFrameHidden);
                    }

                    for (int i = 0; i < 3 && !Browser.IsCaptchaSolved(v3); i++) {
                        try
                        {
                            OBrowser.ClickAudioChallenge(Cursor, out Cursor);
                            var Response = Browser.DecodeAudioChallenge();
                            if (Response == null)
                                continue;
                            OBrowser.SolveSoundCaptcha(Response, Cursor, out Cursor);
                        }
                        catch (Exception ex) {
                            if (!Browser.IsCaptchaSolved(v3))
                                throw ex;
                            else break;
                        }
                    }
                }
            }

            if (Browser.IsCaptchaSolved(v3))
                return;

            do {
                using (var Solver = new SolveCaptcha(OBrowser, v3))
                    Solver.ShowDialog();
            } while (!Browser.IsCaptchaSolved(v3));
        }

        public static bool ClickImNotRobot(this ChromiumWebBrowser ChromiumBrowser, out Point NewCursorPos) {
            var BHost = ChromiumBrowser.GetBrowserHost();
            var Browser = ChromiumBrowser.GetBrowser();

            Random Random = new Random();

            ThreadTools.Wait(Random.Next(999, 1999));

            //Get Recaptcha Position in the page
            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaIframeSearch + "\r\n" + Properties.Resources.reCaptchaGetAnchorPosition).GetAwaiter().GetResult().Result;
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            //Create Positions
            Point InitialPos = new Point(Random.Next(15, 20), Random.Next(25, 30));
            Point CaptchaClick = new Point(X + Random.Next(15, 20), Y + Random.Next(15, 20));
            var CaptchaRegion = new Rectangle(X, Y, Width, Height);
            Point PostClick = new Point(CaptchaRegion.Right + Random.Next(-10, 10), Y + Random.Next(-10, 10));

            //Ensure the fake click don't will click in the "i'm not a robot"
            Point? FakeClick = null;
            while (FakeClick == null || CaptchaRegion.Contains(FakeClick.Value)) {
                FakeClick = new Point(Random.Next(10, 500), Random.Next(20, 500));
            }

            //Create Macros
            var MoveA = CursorTools.CreateMove(InitialPos, FakeClick.Value);
            var MoveB = CursorTools.CreateMove(FakeClick.Value, CaptchaClick);
            var MoveC = CursorTools.CreateMove(CaptchaClick, PostClick);

            //Execute Macros
            BHost.ExecuteMove(MoveA);
            ThreadTools.Wait(Random.Next(999, 1999));
            BHost.ExecuteClick(FakeClick.Value);

            BHost.ExecuteMove(MoveB);
            ThreadTools.Wait(Random.Next(999, 1999));
            BHost.ExecuteClick(CaptchaClick);

            BHost.ExecuteMove(MoveC);
            NewCursorPos = PostClick;

            ThreadTools.Wait(Random.Next(3599, 4599));

            return Browser.IsCaptchaSolved();
        }

        public static void ClickAudioChallenge(this ChromiumWebBrowser ChromiumBrowser, Point CursorPos, out Point NewCursorPos) {
            var BHost = ChromiumBrowser.GetBrowserHost();
            var Browser = ChromiumBrowser.GetBrowser();
            var BFrame = Browser.GetReCaptchaBFrame();

            NewCursorPos = CursorPos;

            if (Browser.IsReCaptchaFailed())
                return;

            Random Random = new Random();            

            //Get Audio Challenge Button Pos
            var Result = (string)BFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaGetSpeakPosition).GetAwaiter().GetResult().Result;
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            Point AudioBntPos = new Point(X + Random.Next(5, Width - 5), Y + Random.Next(5, Height - 5));

            //Parse the relative position of the iframe to Window
            Point BFramePos = Browser.GetReCaptchaBFramePosition();
            AudioBntPos = new Point(AudioBntPos.X + BFramePos.X, AudioBntPos.Y + BFramePos.Y);

            //Create and Execute the Macro
            var Move = CursorTools.CreateMove(CursorPos, AudioBntPos, 7);

            BHost.ExecuteMove(Move);
            ThreadTools.Wait(Random.Next(999, 1999));
            BHost.ExecuteClick(AudioBntPos);

            ThreadTools.Wait(Random.Next(2111, 3599));

            NewCursorPos = AudioBntPos;
        }

        public static string DecodeAudioChallenge(this IBrowser Browser) {
            var BFrame = Browser.GetReCaptchaBFrame();

            if (Browser.IsReCaptchaFailed())
                return null;

            string URL = Browser.GetReCaptchaAudioUrl();

            if (URL == null)
                return null;

            byte[] MP3 = AsyncContext.Run(async () => await URL.TryDownloadAsync(BFrame.Url, Browser.GetUserAgent()));
            if (MP3 == null)
                return null;

            return DataTools.GetTextFromSpeech(DataTools.Mp3ToWav(MP3));
        }

        public static void SolveSoundCaptcha(this ChromiumWebBrowser ChromiumBrowser, string Response, Point CursorPos, out Point NewCursorPos) {
            var BHost = ChromiumBrowser.GetBrowserHost();
            var Browser = ChromiumBrowser.GetBrowser();
            var BFrame = Browser.GetReCaptchaBFrame();

            NewCursorPos = CursorPos;

            //Get Sound Reponse Textbox Position
            var Result = (string)BFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaGetSoundResponsePosition).GetAwaiter().GetResult().Result;
            if (Result == null)
                return;

            Random Random = new Random();

            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            //Parse the relative position of the iframe to Window
            Point BFramePos = Browser.GetReCaptchaBFramePosition();
            Point RespBoxPos = new Point(X + BFramePos.X + Random.Next(5, 100), Y + BFramePos.Y + Random.Next(5, 15));

            //Create Macro
            var Move = CursorTools.CreateMove(CursorPos, RespBoxPos, 6);

            //Execute Macro
            BHost.ExecuteMove(Move);
            ThreadTools.Wait(Random.Next(999, 1999));
            BHost.ExecuteClick(RespBoxPos);

            ThreadTools.Wait(Random.Next(511, 999));

            foreach (char Char in Response) {
                BHost.SendChar(Char);
            }

            //Get Verify Button Position
            var VerifyBntRect = Browser.GetReCaptchaVerifyButtonRectangle();

            //Parse the relative position of the iframe to Window
            Point VerifyBntPos = new Point(VerifyBntRect.X + BFramePos.X + Random.Next(5, 40), VerifyBntRect.Y + BFramePos.Y + Random.Next(5, 15));

            //Create and Execute Macro
            Move = CursorTools.CreateMove(RespBoxPos, VerifyBntPos, 6);
            BHost.ExecuteMove(Move);
            ThreadTools.Wait(Random.Next(999, 1999));
            BHost.ExecuteClick(VerifyBntPos);

            NewCursorPos = VerifyBntPos;

            ThreadTools.Wait(Random.Next(3599, 4599));
        }

        public static IFrame GetReCaptchaBFrame(this IBrowser Browser) {
            foreach (var ID in Browser.GetFrameIdentifiers()) {
                var Frame = Browser.GetFrame(ID);
                if (Frame == null)
                    continue;
                if (Frame.Url.Contains("/bframe?"))
                    return Frame;
            }
            return null;
        }

        public static Point GetReCaptchaBFramePosition(this IBrowser Browser) {
            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaIframeSearch + "\r\n" + Properties.Resources.reCaptchaGetBFramePosition).GetAwaiter().GetResult().Result;
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);

            return new Point(X, Y);
        }

        public static Rectangle GetReCaptchaBFrameRectangle(this IBrowser Browser) {
            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaIframeSearch + "\r\n" + Properties.Resources.reCaptchaGetBFramePosition).GetAwaiter().GetResult().Result;
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);
        }

        public static Rectangle GetReCaptchaVerifyButtonRectangle(this IBrowser Browser) {
            var Result = (string)Browser.GetReCaptchaBFrame().EvaluateScriptAsync(Properties.Resources.reCaptchaGetVerifyButtonPosition).GetAwaiter().GetResult().Result;
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);

        }

        public static string GetReCaptchaAudioUrl(this IBrowser Browser) {
            var Result = (string)Browser.GetReCaptchaBFrame().EvaluateScriptAsync(Properties.Resources.reCaptchaGetAudioUrl).GetAwaiter().GetResult().Result;
            if (string.IsNullOrWhiteSpace(Result))
                return null;
            return Result;
        }
 
        public static bool IsReCaptchaFailed(this IBrowser Browser) {
            return (bool)Browser.GetReCaptchaBFrame().EvaluateScriptAsync(Properties.Resources.reCaptchaIsFailed).GetAwaiter().GetResult().Result;
        }

        public static void HookReCaptcha(this ChromiumWebBrowser Browser) {
            if (Browser.RequestHandler == null || !(Browser.RequestHandler is RequestEventHandler)) {
                Browser.RequestHandler = new RequestEventHandler();
            }

            ((RequestEventHandler)Browser.RequestHandler).OnBeforeBrowseEvent += (sender, args) => {
                if (!args.Request.Url.Contains("www.google.com/recaptcha"))
                    return;
                if (!args.Request.Url.Contains("hl="))
                    return;
                args.Request.Url = args.Request.Url.SetUrlParameter("hl", "en");
            };
        }
    }


}
