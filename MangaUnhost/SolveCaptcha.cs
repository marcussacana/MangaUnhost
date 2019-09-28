using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MangaUnhost {
    public partial class SolveCaptcha : Form {

        Graphics Graphics;
        ChromiumWebBrowser ChromiumBrowser;
        IBrowser Browser;
        IBrowserHost BrowserHost;
        Rectangle BFrameRect;
        Rectangle VerifyRect;

        bool v3 = false;

        int Clicks = 0;
        public SolveCaptcha(ChromiumWebBrowser ChromiumBrowser, bool v3 = false) {
            InitializeComponent();

            this.ChromiumBrowser = ChromiumBrowser;
            Browser = ChromiumBrowser.GetBrowser();
            BrowserHost = ChromiumBrowser.GetBrowserHost();

            this.v3 = v3;

            Shown += (a, b) => {
                if (Browser.IsCaptchaSolved(v3)) {
                    Close();
                    return;
                }
                Initialize();
            };
        }
        void Initialize() {
            Application.DoEvents();
            LoadingMode(true);
            Refresh.Enabled = true;

            if (!v3)
            {
                var Thread = new System.Threading.Thread(() =>
                {
                    Browser.MainFrame.EvaluateScriptAsync(Properties.Resources.reCaptchaReset).GetAwaiter().GetResult();
                    ThreadTools.Wait(1500);
                    ChromiumBrowser.ClickImNotRobot(out _);
                    UpdateRects();
                });

                Thread.Start();

                while (Thread.IsRunning())
                {
                    ThreadTools.Wait(5, true);
                }

            }
            else
                UpdateRects(); 

            LoadingMode(false);
            System.Media.SystemSounds.Beep.Play();
            StatusCheck.Enabled = true;
        }

        void UpdateRects() {
            if (Graphics != null)
                Graphics.Dispose();

            try {
                BFrameRect = Browser.GetReCaptchaBFrameRectangle();
                VerifyRect = Browser.GetReCaptchaVerifyButtonRectangle();

                ScreenBox.Image = new Bitmap(BFrameRect.Width, BFrameRect.Height);
                Graphics = Graphics.FromImage(ScreenBox.Image);
                Width += BFrameRect.Width - ScreenBox.Width;
                Height += BFrameRect.Height - ScreenBox.Height;
                ScreenBox.Size = BFrameRect.Size;

                if (!InvokeRequired)
                    RadialProgBar.Location = new Point((Width / 2) - (RadialProgBar.Width / 2), (Height / 2) - (RadialProgBar.Height / 2) + ScreenBox.Location.Y);
            } catch { 
                if (!StatusCheck.Enabled)
                    Invoke(new MethodInvoker(Close));
            }
        }

        private void RefreshTick(object sender, EventArgs e) {
            if (ScreenBox.Visible) {
                using (var Screenshot = ChromiumBrowser.ScreenshotOrNull()) {
                    if (Screenshot == null)
                        return;
                    try {
                        Graphics.DrawImage(Screenshot, 0, 0, BFrameRect, GraphicsUnit.Pixel);
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
            if (Browser.IsCaptchaSolved(v3) || Browser.IsReCaptchaFailed())
                Close();

            if (Clicks > 0) {
                Clicks--;

                UpdateRects();
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

            BrowserHost.SendMouseMoveEvent(new MouseEvent(e.X + BFrameRect.X, e.Y + BFrameRect.Y, Flags), false);
        }

        private void ScreenClicked(object sender, EventArgs e) {
            var ClickPos = ScreenBox.PointToClient(Cursor.Position);
            bool Verify = VerifyRect.Contains(ClickPos);
            ClickPos = new Point(ClickPos.X + BFrameRect.X, ClickPos.Y + BFrameRect.Y);
            BrowserHost.ExecuteClick(ClickPos);

            if (Verify) {
                LoadingMode(true);
                ThreadTools.Wait(3000, true);
                if (Browser.IsCaptchaSolved(v3))
                    Close();
                else {
                    UpdateRects();
                    LoadingMode(false);
                }
            } else {
                StatusCheck.Enabled = false;
                StatusCheck.Enabled = true;
                Clicks++;
            }
        }

        public void LoadingMode(bool Enabled) {
            ScreenBox.Visible =    !Enabled;
            RadialProgBar.Visible = Enabled;
            Refresh.Interval = Enabled ? 25 : 35;
        }
    }
}
