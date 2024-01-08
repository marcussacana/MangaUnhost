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
    public static class cfCaptcha
    {
        public static bool cfCaptchaIsSolved(this ChromiumWebBrowser Browser) => Browser.GetBrowser().cfCaptchaIsSolved();
        public static bool cfCaptchaIsSolved(this IBrowser Browser)
        {
            return Browser.EvaluateScript<bool>(Properties.Resources.cfCaptchaIsSolved);
        }

        public static void cfCaptchaSolve(this ChromiumWebBrowser Browser)
        {
            if (Browser.cfCaptchaIsSolved())
                return;

            ThreadTools.Wait(5000, true);

            Browser.cfCaptchaClickImHuman(out _);

            ThreadTools.Wait(5000, true);

            if (Browser.cfCaptchaIsSolved())
                return;

            var Solver = new SolveCaptcha(Browser, cfCaptcha: true);
            while (!Browser.cfCaptchaIsSolved())
                Solver.ShowDialog();
        }

        public static void cfCaptchaClickImHuman(this ChromiumWebBrowser Browser, out Point Cursor) => Browser.GetBrowser().cfCaptchaClickImHuman(out Cursor);

        public static void cfCaptchaClickImHuman(this IBrowser Browser, out Point Cursor)
        {
            var Rnd = new Random();
            var Begin = new Point(Rnd.Next(5, 25), Rnd.Next(5, 25));
            var Target = Browser.GetcfCaptchaImHumanButtonPosition();
            var Move = CursorTools.CreateMove(Begin, Target, MouseSpeed: 10);
            Browser.ExecuteMove(Move);
            ThreadTools.Wait(Rnd.Next(100, 150), true);
            Browser.ExecuteClick(Target);
            ThreadTools.Wait(Rnd.Next(500, 650), true);
            Cursor = Target;
        }

        public static Point GetcfCaptchaImHumanButtonPosition(this IBrowser Browser)
        {
            var Rect = Browser.GetcfCaptchaRectangle();
            return new Point(Rect.X + 35, Rect.Y + 41);
        }

        public static Rectangle GetcfCaptchaRectangle(this IBrowser Browser)
        {
            var Result = Browser.EvaluateScript<string>(Properties.Resources.cfCaptchaGetMainFramePosition);
            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);

        }        
    }
}
