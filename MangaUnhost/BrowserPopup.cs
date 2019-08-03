using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MangaUnhost {
    public partial class BrowserPopup : Form, IMessageFilter {

        Graphics Graphics;
        ChromiumWebBrowser ChromiumBrowser;
        IBrowserHost BrowserHost;
        Rectangle ViewRectangle;

        Func<bool> Verify;

        int Clicks = 0;
        public BrowserPopup(ChromiumWebBrowser ChromiumBrowser, Func<bool> FinishVerify) {
            InitializeComponent();

            this.ChromiumBrowser = ChromiumBrowser;
            BrowserHost = ChromiumBrowser.GetBrowserHost();
            ChromiumBrowser.GetBrowser().WaitForLoad();

            Verify = FinishVerify;
            var Screenshot = ChromiumBrowser.ScreenshotOrNull();
            ViewRectangle = new Rectangle(0, 0, Screenshot.Width, Screenshot.Height);

            Shown += (a, b) => {
                Initialize();
            };
        }
        public BrowserPopup(ChromiumWebBrowser ChromiumBrowser, Rectangle ViewArea, Func<bool> FinishVerify) {
            InitializeComponent();

            this.ChromiumBrowser = ChromiumBrowser;
            BrowserHost = ChromiumBrowser.GetBrowserHost();
            ChromiumBrowser.GetBrowser().WaitForLoad();

            Verify = FinishVerify;
            ViewRectangle = ViewArea;

            Shown += (a, b) => {
                Initialize();
            };
        }
        void Initialize() {
            Application.AddMessageFilter(this);
            FormClosed += (s, e) => Application.RemoveMessageFilter(this);

            UpdateRects();

            Refresh.Enabled = true;
            StatusCheck.Enabled = true;
            LoadingMode(false);
            System.Media.SystemSounds.Beep.Play();
        }

        void UpdateRects() {
            if (Graphics != null)
                Graphics.Dispose();

            try {
                ScreenBox.Image = new Bitmap(ViewRectangle.Width, ViewRectangle.Height);
                Graphics = Graphics.FromImage(ScreenBox.Image);
                Width += ViewRectangle.Width - ScreenBox.Width;
                Height += ViewRectangle.Height - ScreenBox.Height;
                ScreenBox.Size = ViewRectangle.Size;

                if (!InvokeRequired)
                    RadialProgBar.Location = new Point((Width / 2) - (RadialProgBar.Width / 2), (Height / 2) - (RadialProgBar.Height / 2) + ScreenBox.Location.Y);
            } catch {
                if (!StatusCheck.Enabled)
                    Close();
            }
        }

        private void RefreshTick(object sender, EventArgs e) {
            if (ScreenBox.Visible) {
                using (var Screenshot = ChromiumBrowser.ScreenshotOrNull()) {
                    if (Screenshot == null)
                        return;
                    try {
                        Graphics.DrawImage(Screenshot, 0, 0, ViewRectangle, GraphicsUnit.Pixel);
                        Graphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
                        ScreenBox.Invalidate();
                    } catch { }
                }
            } else {
                RadialProgBar.StartingAngle += 7;
                if (RadialProgBar.StartingAngle > 359)
                    RadialProgBar.StartingAngle = 0;
            }
        }

        private void StatusCheckTick(object sender, EventArgs e) {
            if (Verify())
                Close();

            if (Clicks > 0) {
                Clicks--;
                RefreshTick(null, null);
            }
        }

        private void ScreenMouseMove(object sender, MouseEventArgs e) {
            CefEventFlags Flags = CefEventFlags.None;
            if ((e.Button & MouseButtons.Left) != 0)
                Flags |= CefEventFlags.LeftMouseButton;
            if ((e.Button & MouseButtons.Right) != 0)
                Flags |= CefEventFlags.RightMouseButton;
            if ((e.Button & MouseButtons.Middle) != 0)
                Flags |= CefEventFlags.MiddleMouseButton;

            BrowserHost.SendMouseMoveEvent(new MouseEvent(e.X + ViewRectangle.X, e.Y + ViewRectangle.Y, Flags), false);
        }

        private void ScreenMouseDown(object sender, MouseEventArgs e) {
            var ClickPos = ScreenBox.PointToClient(Cursor.Position);
            ClickPos = new Point(ClickPos.X + ViewRectangle.X, ClickPos.Y + ViewRectangle.Y);
            if (e.Button == MouseButtons.Middle)
                return;

            var Button = e.Button == MouseButtons.Left ? MouseButtonType.Left : MouseButtonType.Right;
            BrowserHost.SendMouseClickEvent(new MouseEvent(ClickPos.X, ClickPos.Y, CefEventFlags.None), Button, false, 1);
        }

        private void ScreenMouseUp(object sender, MouseEventArgs e) {
            var ClickPos = ScreenBox.PointToClient(Cursor.Position);
            ClickPos = new Point(ClickPos.X + ViewRectangle.X, ClickPos.Y + ViewRectangle.Y);
            if (e.Button == MouseButtons.Middle)
                return;

            var Button = e.Button == MouseButtons.Left ? MouseButtonType.Left : MouseButtonType.Right;
            BrowserHost.SendMouseClickEvent(new MouseEvent(ClickPos.X, ClickPos.Y, CefEventFlags.None), Button, true, 1);
        }

        public void LoadingMode(bool Enabled) {
            ScreenBox.Visible =    !Enabled;
            RadialProgBar.Visible = Enabled;
            Refresh.Interval = Enabled ? 25 : 35;
        }

        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (msg.Msg == WM_KEYDOWN || msg.Msg == WM_KEYUP || msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_SYSKEYUP) {
                if (!Repeating) {
                    Repeating = true;
                    BrowserHost.SendKeyEvent(msg.Msg, msg.WParam.ToInt32(), msg.LParam.ToInt32());
                    //return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        bool Repeating = false;
        public bool PreFilterMessage(ref Message msg) {
            if (msg.Msg == WM_KEYDOWN || msg.Msg == WM_KEYUP || msg.Msg == WM_SYSKEYDOWN || msg.Msg == WM_SYSKEYUP)
                Repeating = false;
            return false;
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e) {
            BrowserHost.SendChar(e.KeyChar);
            e.Handled = true;
        }
    }
}
