using CefSharp;
using CefSharp.OffScreen;
using CefSharp.EventHandler;
using MangaUnhost.Others;
using Nito.AsyncEx;
using System;
using System.Drawing;

namespace MangaUnhost.Browser {
    public static class reCaptcha {

        public const int BFrameHidden = -9999;
        public static bool ReCaptchaIsSolved(this ChromiumWebBrowser Browser, bool v3 = false) => Browser.GetBrowser().ReCaptchaIsSolved(v3);
        public static bool ReCaptchaIsSolved(this IBrowser Browser, bool v3 = false) {
            if (Browser.ReCaptchaGetBFrame() == null)
                return true;

            if (v3)
            {
                try
                {
                    var Point = Browser.ReCaptchaGetBFrameRectangle();
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
        public static void ReCaptchaTrySolve(this ChromiumWebBrowser OBrowser, CaptchaSolverType SolverType = CaptchaSolverType.SemiAuto)
        {
            try
            {
                var Browser = OBrowser.GetBrowser();
                Browser.WaitForLoad();
                ThreadTools.Wait(1500, true);
                if (SolverType != CaptchaSolverType.Manual)
                {
                    if (!Browser.ReCaptchaIsSolved())
                    {
                        Point Cursor = new Point(0, 0);

                        do
                        {
                            OBrowser.ReCaptchaClickImNotRobot(out Cursor);
                            if (Browser.ReCaptchaIsSolved())
                                return;
                        } while (Browser.ReCaptchaGetBFramePosition().Y == BFrameHidden);


                        for (int i = 0; i < 3 && !Browser.ReCaptchaIsSolved(); i++)
                        {
                            try
                            {
                                if (Browser.ReCaptchaIsFailed())
                                {
                                    Browser.ReCaptchaReset();
                                    if (Browser.ReCaptchaIsSolved())
                                        break;
                                }
                            }
                            catch { }
                            try
                            {
                                OBrowser.ReCaptchaClickAudioChallenge(Cursor, out Cursor);
                                if (Browser.ReCaptchaIsFailed())
                                    continue;
                                var Response = Browser.DecodeAudioChallenge();
                                if (Response == null)
                                    continue;
                                OBrowser.ReCaptchaSolveSound(Response, Cursor, out Cursor);
                            }
                            catch (Exception ex)
                            {
                                if (!Browser.ReCaptchaIsSolved())
                                    throw ex;
                                else break;
                            }
                        }
                    }
                }

                if (Browser.ReCaptchaIsSolved())
                    return;

                do
                {
                    using (var Solver = new SolveCaptcha(OBrowser))
                        Solver.ShowDialog();
                } while (!Browser.ReCaptchaIsSolved());
            }
            catch { }
        }

        /// <summary>
        /// Try Solve the Recaptcha v3
        /// </summary>
        /// <param name="OBrowser">The Browser with the recaptcha</param>
        /// <param name="Submit">A Event that can trigger the recaptcha</param>
        /// <param name="Validate">A Event that can confirm if the captcha is solved</param>
        public static void ReCaptchaTrySolveV3(this ChromiumWebBrowser OBrowser, Action Submit, Func<bool> Validate = null, CaptchaSolverType SolverType = CaptchaSolverType.SemiAuto)
        {
            var Browser = OBrowser.GetBrowser();
            Browser.WaitForLoad();
            Submit();
            ThreadTools.Wait(100, true);
            if (SolverType != CaptchaSolverType.Manual)
            {
                if (!Validate?.Invoke() ?? !Browser.ReCaptchaIsSolved(true))
                {
                    Point Cursor = new Point(0, 0);

                    for (int i = 0; i < 3 && !(Validate?.Invoke() ?? Browser.ReCaptchaIsSolved(true)); i++)
                    {
                        try
                        {
                            OBrowser.ReCaptchaClickAudioChallenge(Cursor, out Cursor);
                            if (Browser.ReCaptchaIsFailed())
                            {
                                Browser.ReCaptchaReset();
                                ThreadTools.Wait(500, true);
                                Submit();
                                if (!Validate?.Invoke() ?? !Browser.ReCaptchaIsSolved(true))
                                    break;
                            }
                            var Response = Browser.DecodeAudioChallenge();
                            if (Response == null)
                                continue;
                            OBrowser.ReCaptchaSolveSound(Response, Cursor, out Cursor);
                        }
                        catch (Exception ex)
                        {
                            if (!Validate?.Invoke() ?? !Browser.ReCaptchaIsSolved(true))
                                throw ex;
                            else break;
                        }
                    }
                }
            }

            if (Validate?.Invoke() ?? Browser.ReCaptchaIsSolved(true))
                return;

            do
            {
                using (var Solver = new SolveCaptcha(OBrowser, true, Submit: Submit))
                    Solver.ShowDialog();
            } while (!Validate?.Invoke() ?? !Browser.ReCaptchaIsSolved(true));
        }

        public static bool ReCaptchaClickImNotRobot(this ChromiumWebBrowser ChromiumBrowser, out Point NewCursorPos)
        {
            var BHost = ChromiumBrowser.GetBrowserHost();
            var Browser = ChromiumBrowser.GetBrowser();

            Random Random = new Random();

            ThreadTools.Wait(Random.Next(999, 1999), true);

            //Get Recaptcha Position in the page
            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaIframeSearch + "\r\n" + Properties.Resources.reCaptchaGetAnchorPosition).GetAwaiter().GetResult().Result;
            InputTools.GetBoundsCoords(Result, out int X, out int Y, out int Width, out int Height);

            //Create Positions
            Point InitialPos = new Point(Random.Next(15, 20), Random.Next(25, 30));
            Point CaptchaClick = new Point(X + Random.Next(15, 20), Y + Random.Next(15, 20));
            var CaptchaRegion = new Rectangle(X, Y, Width, Height);
            Point PostClick = new Point(CaptchaRegion.Right + Random.Next(-10, 10), Y + Random.Next(-10, 10));

            //Ensure the fake click don't will click in the "i'm not a robot"
            Point? FakeClick = null;
            while (FakeClick == null || CaptchaRegion.Contains(FakeClick.Value))
            {
                FakeClick = new Point(Random.Next(10, 500), Random.Next(20, 500));
            }

            //Create Macros
            var MoveA = CursorTools.CreateMove(InitialPos, FakeClick.Value);
            var MoveB = CursorTools.CreateMove(FakeClick.Value, CaptchaClick);
            var MoveC = CursorTools.CreateMove(CaptchaClick, PostClick);

            //Execute Macros
            BHost.ExecuteMove(MoveA);
            ThreadTools.Wait(Random.Next(999, 1999), true);
            BHost.ExecuteClick(FakeClick.Value);

            BHost.ExecuteMove(MoveB);
            ThreadTools.Wait(Random.Next(999, 1999), true);
            BHost.ExecuteClick(CaptchaClick);

            BHost.ExecuteMove(MoveC);
            NewCursorPos = PostClick;

            ThreadTools.Wait(Random.Next(3599, 4599), true);

            return Browser.ReCaptchaIsSolved();
        }

        public static void ReCaptchaClickAudioChallenge(this ChromiumWebBrowser ChromiumBrowser, Point CursorPos, out Point NewCursorPos) {
            var BHost = ChromiumBrowser.GetBrowserHost();
            var Browser = ChromiumBrowser.GetBrowser();
            var BFrame = Browser.ReCaptchaGetBFrame();

            NewCursorPos = CursorPos;

            if (Browser.ReCaptchaIsFailed())
                return;

            Random Random = new Random();            

            //Get Audio Challenge Button Pos
            var Result = (string)BFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaGetSpeakPosition).GetAwaiter().GetResult().Result;
            InputTools.GetBoundsCoords(Result, out int X, out int Y, out int Width, out int Height);

            Point AudioBntPos = new Point(X + Random.Next(5, Width - 5), Y + Random.Next(5, Height - 5));

            //Parse the relative position of the iframe to Window
            Point BFramePos = Browser.ReCaptchaGetBFramePosition();
            AudioBntPos = new Point(AudioBntPos.X + BFramePos.X, AudioBntPos.Y + BFramePos.Y);

            //Create and Execute the Macro
            var Move = CursorTools.CreateMove(CursorPos, AudioBntPos, 7);

            BHost.ExecuteMove(Move);
            ThreadTools.Wait(Random.Next(999, 1999), true);
            BHost.ExecuteClick(AudioBntPos);

            ThreadTools.Wait(Random.Next(2111, 3599), true);

            NewCursorPos = AudioBntPos;
        }

        public static string DecodeAudioChallenge(this IBrowser Browser) {
            var BFrame = Browser.ReCaptchaGetBFrame();

            if (Browser.ReCaptchaIsFailed())
                return null;

            string URL = Browser.ReCaptchaGetAudioUrl();

            if (URL == null)
                return null;

            byte[] MP3 = AsyncContext.Run(async () => await URL.TryDownloadAsync(BFrame.Url, Browser.GetUserAgent()));
            if (MP3 == null)
                return null;

            return DataTools.GetTextFromSpeech(DataTools.Mp3ToWav(MP3));
        }

        public static void ReCaptchaSolveSound(this ChromiumWebBrowser ChromiumBrowser, string Response, Point CursorPos, out Point NewCursorPos) {
            var BHost = ChromiumBrowser.GetBrowserHost();
            var Browser = ChromiumBrowser.GetBrowser();
            var BFrame = Browser.ReCaptchaGetBFrame();

            NewCursorPos = CursorPos;

            //Get Sound Reponse Textbox Position
            var Result = (string)BFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaGetSoundResponsePosition).GetAwaiter().GetResult().Result;
            if (Result == null)
                return;

            Random Random = new Random();

            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            //Parse the relative position of the iframe to Window
            Point BFramePos = Browser.ReCaptchaGetBFramePosition();
            Point RespBoxPos = new Point(X + BFramePos.X + Random.Next(5, 100), Y + BFramePos.Y + Random.Next(5, 15));

            //Create Macro
            var Move = CursorTools.CreateMove(CursorPos, RespBoxPos, 6);

            //Execute Macro
            BHost.ExecuteMove(Move);
            ThreadTools.Wait(Random.Next(999, 1999), true);
            BHost.ExecuteClick(RespBoxPos);

            ThreadTools.Wait(Random.Next(511, 999), true);

            foreach (char Char in Response) {
                BHost.SendChar(Char);
            }

            //Get Verify Button Position
            var VerifyBntRect = Browser.ReCaptchaGetVerifyButtonRectangle();

            //Parse the relative position of the iframe to Window
            Point VerifyBntPos = new Point(VerifyBntRect.X + BFramePos.X + Random.Next(5, 40), VerifyBntRect.Y + BFramePos.Y + Random.Next(5, 15));

            //Create and Execute Macro
            Move = CursorTools.CreateMove(RespBoxPos, VerifyBntPos, 6);
            BHost.ExecuteMove(Move);
            ThreadTools.Wait(Random.Next(999, 1999), true);
            BHost.ExecuteClick(VerifyBntPos);

            NewCursorPos = VerifyBntPos;

            ThreadTools.Wait(Random.Next(3599, 4599), true);
        }

        public static IFrame ReCaptchaGetBFrame(this IBrowser Browser) => Browser.GetFrameByUrl("/bframe?");

        public static Point ReCaptchaGetBFramePosition(this IBrowser Browser) {
            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaIframeSearch + "\r\n" + Properties.Resources.reCaptchaGetBFramePosition).GetAwaiter().GetResult().Result;
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);

            return new Point(X, Y);
        }

        public static Rectangle ReCaptchaGetBFrameRectangle(this IBrowser Browser) {
            var Result = (string)Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaIframeSearch + "\r\n" + Properties.Resources.reCaptchaGetBFramePosition).GetAwaiter().GetResult().Result;
            InputTools.GetBoundsCoords(Result, out int X, out int Y, out int Width, out int Height);

            return new Rectangle(X, Y, Width, Height);
        }

        public static Rectangle ReCaptchaGetVerifyButtonRectangle(this IBrowser Browser) {
            var Result = (string)Browser.ReCaptchaGetBFrame().EvaluateScriptAsync(Properties.Resources.reCaptchaGetVerifyButtonPosition).GetAwaiter().GetResult().Result;
            InputTools.GetBoundsCoords(Result, out int X, out int Y, out int Width, out int Height);

            return new Rectangle(X, Y, Width, Height);

        }

        public static string ReCaptchaGetAudioUrl(this IBrowser Browser) {
            var Result = (string)Browser.ReCaptchaGetBFrame().EvaluateScriptAsync(Properties.Resources.reCaptchaGetAudioUrl).GetAwaiter().GetResult().Result;
            if (string.IsNullOrWhiteSpace(Result))
                return null;
            return Result;
        }

        public static bool ReCaptchaIsFailed(this IBrowser Browser)
        {
            return (bool)Browser.ReCaptchaGetBFrame().EvaluateScriptAsync(Properties.Resources.reCaptchaIsFailed).GetAwaiter().GetResult().Result;
        }

        public static void ReCaptchaReset(this IBrowser Browser)
        {
            Browser.EvaluateScript(Properties.Resources.reCaptchaReset);
        }

        public static void ReCaptchaHook(this ChromiumWebBrowser Browser) {
            if (Browser.RequestHandler == null || !(Browser.RequestHandler is RequestEventHandler)) {
                Browser.RequestHandler = new RequestEventHandler();
            }

            ((RequestEventHandler)Browser.RequestHandler).OnBeforeBrowseEvent += (sender, args) => {
                if (!args.Request.Url.Contains("www.google.com/recaptcha"))
                    return;
                if (!args.Request.Url.Contains("hl="))
                    return;

                if (!args.Request.IsReadOnly)
                    args.Request.Url = args.Request.Url.SetUrlParameter("hl", "en");
            };
        }
    }


}
