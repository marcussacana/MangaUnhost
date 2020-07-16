using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MangaUnhost.Browser
{
    public static class hCaptcha
    {
        public static bool hCaptchaIsSolved(this ChromiumWebBrowser Browser) => Browser.GetBrowser().hCaptchaIsSolved();
        public static bool hCaptchaIsSolved(this IBrowser Browser)
        {
            return Browser.EvaluateScript<bool>(Properties.Resources.hCaptchaIsSolved);
        }

        public static void hCaptchaSolve(this ChromiumWebBrowser Browser)
        {
            if (Browser.hCaptchaIsSolved())
                return;

            var Solver = new SolveCaptcha(Browser, hCaptcha: true);
            while (!Browser.hCaptchaIsSolved())
                Solver.ShowDialog();
        }

        public static void hCaptchaClickImHuman(this ChromiumWebBrowser Browser, out Point Cursor) => Browser.GetBrowser().hCaptchaClickImHuman(out Cursor);

        public static void hCaptchaClickImHuman(this IBrowser Browser, out Point Cursor)
        {
            var Rnd = new Random();
            var Begin = new Point(Rnd.Next(5, 25), Rnd.Next(5, 25));
            var Target = Browser.GethCaptchaImHumanButtonPosition();
            var Move = CursorTools.CreateMove(Begin, Target, MouseSpeed: 10);
            Browser.ExecuteMove(Move);
            ThreadTools.Wait(Rnd.Next(100, 150), true);
            Browser.ExecuteClick(Target);
            ThreadTools.Wait(Rnd.Next(500, 650), true);
            Cursor = Target;
        }

        public static bool hCaptchaIsFailed(this IBrowser Browser)
        {
            var Challenge = Browser.GetFrameByUrl("hcaptcha-challenge");
            return Challenge.EvaluateScript<bool>(Properties.Resources.hCaptchaIsFailed);
        }
        public static void hCaptchaReset(this IBrowser Browser) => Browser.EvaluateScript(Properties.Resources.hCaptchaReset);

        public static Point GethCaptchaImHumanButtonPosition(this IBrowser Browser)
        {
            var Rect = Browser.GethCaptchaRectangle();
            return new Point(Rect.X + 35, Rect.Y + 41);
        }
        public static Rectangle GethCaptchaRectangle(this IBrowser Browser)
        {
            var Result = Browser.EvaluateScript<string>(Properties.Resources.hCaptchaGetMainFramePosition);
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);

        }
        public static Rectangle GethCaptchaChallengeRectangle(this IBrowser Browser)
        {
            var Result = Browser.EvaluateScript<string>(Properties.Resources.hCaptchaGetChallengeFramePosition);
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);

        }
        public static Rectangle GethCaptchaVerifyButtonRectangle(this IBrowser Browser)
        {
            var Result = Browser.GetFrameByUrl("hcaptcha-challenge").EvaluateScript<string>(Properties.Resources.hCaptchaGetVerifyButtonPosition);
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);

        }
    }
}
