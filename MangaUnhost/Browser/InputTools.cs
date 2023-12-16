using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Others;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Web.UI.WebControls;
using System.Windows.Input;

namespace MangaUnhost.Browser {
    public static class InputTools {
        static Random random = new Random();
        public static void ExecuteMove(this ChromiumWebBrowser Browser, List<MimicStep> Steps) => Browser.GetBrowserHost().ExecuteMove(Steps);

        public static void ExecuteMove(this IBrowser Browser, List<MimicStep> Steps) => Browser.GetHost().ExecuteMove(Steps);

        public static void ExecuteMove(this IBrowserHost Browser, List<MimicStep> Steps) {
            foreach (var Step in Steps) {
                Browser.SendMouseMoveEvent(new MouseEvent(Step.Location.X, Step.Location.Y, CefEventFlags.None), false);
                ThreadTools.Wait(Step.Delay, true);
            }
        }
        public static void ExecuteClick(this ChromiumWebBrowser Browser, Point Position) => Browser.GetBrowserHost().ExecuteClick(Position);

        public static void ExecuteClick(this IBrowser Browser, Point Position) => Browser.GetHost().ExecuteClick(Position);

        public static void ExecuteClick(this IBrowserHost Browser, Point Position) {
            Browser.SendMouseClickEvent(new MouseEvent(Position.X, Position.Y, CefEventFlags.LeftMouseButton), MouseButtonType.Left, false, 1);
            ThreadTools.Wait(random.Next(49, 101), true);
            Browser.SendMouseClickEvent(new MouseEvent(Position.X, Position.Y, CefEventFlags.LeftMouseButton), MouseButtonType.Left, true, 1);
        }

        public static void SendChar(this ChromiumWebBrowser Browser, char Char) => Browser.GetBrowserHost().SendChar(Char);

        public static void SendChar(this IBrowser Browser, char Char) => Browser.GetHost().SendChar(Char);
        public static void SendChar(this IBrowserHost Browser, char Char)
        {

            Browser.SendKeyEvent(new KeyEvent()
            {
                FocusOnEditableField = true,
                IsSystemKey = false,
                Modifiers = CefEventFlags.None,
                WindowsKeyCode = Char,
                Type = KeyEventType.Char
            });
            ThreadTools.Wait(random.Next(40, 100));
        }

        public static bool TypeInInput(this ChromiumWebBrowser Browser, string ElementGetter, string ValueToType, bool UseKeyDown = false)
        {
            var JS = $"var target = {ElementGetter}; {Properties.Resources.targetGetBounds}";
            var Bounds = Browser.EvaluateScript<string>(JS);
            GetBoundsCoords(Bounds, out int X, out int Y, out int Width, out int Height);

            Browser.ExecuteClick(new Point(X + (Width / 2), Y + (Height/2)));
            ThreadTools.Wait(100, true);

            foreach (char Char in ValueToType)
            {
                Browser.SendChar(Char);
            }

            return Browser.EvaluateScript<string>($"{ElementGetter}.value") == ValueToType;
        }

        public static void GetBoundsCoords(string JSON, out int X, out int Y, out int Width, out int Height)
        {
            X = int.Parse(DataTools.ReadJson(JSON, "x").Split('.', ',')[0]);
            Y = int.Parse(DataTools.ReadJson(JSON, "y").Split('.', ',')[0]);
            Width = int.Parse(DataTools.ReadJson(JSON, "width").Split('.', ',')[0]);
            Height = int.Parse(DataTools.ReadJson(JSON, "height").Split('.', ',')[0]);
        }
    }
}
