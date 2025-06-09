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
    public static class Turnstile
    {
        public static bool TurnstileIsSolved(this ChromiumWebBrowser Browser) => Browser.GetBrowser().TurnstileIsSolved();
        public static bool TurnstileIsSolved(this IBrowser Browser)
        {
            try
            {
                return Browser.EvaluateScript<bool>(Properties.Resources.cfCaptchaIsSolved);
            }
            catch
            {
                return true;
            }
        }

        public static bool TurnstileSolve(this IBrowser Browser)
        {
            if (Browser.TurnstileIsSolved())
                return true;

            ThreadTools.Wait(3000, true);

            Browser.TurnstileClickImHuman(out _);

            ThreadTools.Wait(5000, true);

            if (Browser.TurnstileIsSolved())
                return true;

            return false;
            /*
            var Solver = new SolveCaptcha(Browser, cfCaptcha: true);
            while (!Browser.TurnstileIsSolved())
                Solver.ShowDialog();
            */
        }

        public static void TurnstileClickImHuman(this ChromiumWebBrowser Browser, out Point Cursor) => Browser.GetBrowser().TurnstileClickImHuman(out Cursor);

        public static void TurnstileClickImHuman(this IBrowser Browser, out Point Cursor)
        {
            var Rnd = new Random();
            var Begin = new Point(Rnd.Next(5, 25), Rnd.Next(5, 25));
            var Target = Browser.GetTurnstileImHumanButtonPosition();
            var Move = CursorTools.CreateMove(Begin, Target, MouseSpeed: 10);
            Browser.ExecuteMove(Move);
            ThreadTools.Wait(Rnd.Next(100, 150), true);
            Browser.ExecuteClick(Target);
            ThreadTools.Wait(Rnd.Next(100, 150), true);
            Browser.ExecuteClick(Target);
            ThreadTools.Wait(Rnd.Next(500, 650), true);
            Cursor = Target;
        }

        public static Point GetTurnstileImHumanButtonPosition(this IBrowser Browser)
        {
            var Rect = Browser.GetTurnstileRectangle();
            return new Point(Rect.X + 35, Rect.Y + (Rect.Height/2));
        }

        public static Rectangle GetTurnstileRectangle(this IBrowser Browser)
        {
            var Result = Browser.EvaluateScript<string>(Properties.Resources.cfCaptchaGetMainFramePosition);
            if (Result == null)
            {
                return new Rectangle(0, 0, 1280, 720);
            }

            int X = int.Parse(DataTools.ReadJson(Result, "x").Split('.', ',')[0]);
            int Y = int.Parse(DataTools.ReadJson(Result, "y").Split('.', ',')[0]);
            int Width = int.Parse(DataTools.ReadJson(Result, "width").Split('.', ',')[0]);
            int Height = int.Parse(DataTools.ReadJson(Result, "height").Split('.', ',')[0]);

            return new Rectangle(X, Y, Width, Height);

        }        
    }
}
