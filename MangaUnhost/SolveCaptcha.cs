using CefSharp;
using CefSharp.OffScreen;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MangaUnhost
{
    public partial class SolveCaptcha : Form
    {

        Graphics Graphics;
        ChromiumWebBrowser ChromiumBrowser;
        Action Submit;
        IBrowser Browser => ChromiumBrowser.GetBrowser();
        IBrowserHost BrowserHost => ChromiumBrowser.GetBrowserHost();
        Rectangle FrameRect;
        Rectangle VerifyRect;

        bool hCaptcha = false;
        bool cfCaptcha = false;
        bool v3 = false;

        int Clicks = 0;
        public SolveCaptcha(ChromiumWebBrowser ChromiumBrowser, bool v3 = false, bool hCaptcha = false, bool cfCaptcha = false, Action Submit = null)
        {
            InitializeComponent();

            this.ChromiumBrowser = ChromiumBrowser;
            this.Submit = Submit;

            this.v3 = v3;
            this.hCaptcha = hCaptcha;
            this.cfCaptcha = cfCaptcha;

            Shown += (a, b) =>
            {
                if (IsCaptchaSolved())
                {
                    Close();
                    return;
                }
                Initialize();
            };
        }

        bool IsCaptchaSolved()
        {
            try
            {
                if (hCaptcha)
                    return Browser.hCaptchaIsSolved();
                if (cfCaptcha)
                    return Browser.cfCaptchaIsSolved();
                return Browser.ReCaptchaIsSolved(v3);
            }
            catch
            {
                return true;
            }
        }

        bool IsCaptchaFailed()
        {
            if (hCaptcha)
                return Browser.hCaptchaIsFailed();
            if (cfCaptcha)
                return false;
            return Browser.ReCaptchaIsFailed();
        }

        void ResetCaptcha()
        {
            if (hCaptcha)
            {
                Browser.hCaptchaReset();
                return;
            }
            if (cfCaptcha)
            {
                Browser.Reload();
                return;
            }
            Browser.ReCaptchaReset();
        }

        Rectangle GetCaptchaFrameRectangle()
        {
            if (hCaptcha)
                return Browser.GethCaptchaChallengeRectangle();
            if (cfCaptcha)
                return new Rectangle(Point.Empty, ChromiumBrowser.Size);
            return Browser.ReCaptchaGetBFrameRectangle();
        }

        Rectangle GetVerifyButtonRectangle()
        {
            if (hCaptcha)
                return Browser.GethCaptchaVerifyButtonRectangle();
            if (cfCaptcha)
                return Browser.GetcfCaptchaRectangle();
            return Browser.ReCaptchaGetVerifyButtonRectangle();
        }

        void ClickImNotRobot()
        {
            try
            {
                if (hCaptcha)
                    Browser.hCaptchaClickImHuman(out _);
                else
                    ChromiumBrowser.ReCaptchaClickImNotRobot(out _);
            }
            catch { }
        }
        void Initialize()
        {
            Application.DoEvents();
            LoadingMode(true);
            Refresh.Enabled = true;

            if (!v3 || hCaptcha)
            {
                var Thread = new System.Threading.Thread(() =>
                {
                    ResetCaptcha();
                    ThreadTools.Wait(1500);
                    ClickImNotRobot();
                });

                Thread.Start();

                while (Thread.IsRunning())
                {
                    ThreadTools.Wait(5, true);
                }

                UpdateRects();
            }
            else
            {
                Submit();
                UpdateRects();
            }

            LoadingMode(false);
            System.Media.SystemSounds.Beep.Play();
            StatusCheck.Enabled = true;
        }

        void UpdateRects()
        {
            if (Graphics != null)
                Graphics.Dispose();

            try
            {
                FrameRect = GetCaptchaFrameRectangle();
                VerifyRect = GetVerifyButtonRectangle();

                ScreenBox.Image = new Bitmap(FrameRect.Width, FrameRect.Height);
                Graphics = Graphics.FromImage(ScreenBox.Image);
                Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                Width += FrameRect.Width - ScreenBox.Width;
                Height += FrameRect.Height - ScreenBox.Height;
                ScreenBox.Size = FrameRect.Size;

                if (!InvokeRequired)
                    RadialProgBar.Location = new Point((Width / 2) - (RadialProgBar.Width / 2), (Height / 2) - (RadialProgBar.Height / 2) + ScreenBox.Location.Y);
            }
            catch
            {
                if (!StatusCheck.Enabled)
                    Invoke(new MethodInvoker(Close));
            }
        }

        bool Frameskip = false;

        private void RefreshTick(object sender, EventArgs e)
        {
            if (ScreenBox.Visible)
            {
                using (var Screenshot = ChromiumBrowser.ScreenshotOrNull())
                {
                    if (Screenshot == null)
                        return;
                    try
                    {
                        Graphics.DrawImage(Screenshot, 0, 0, FrameRect, GraphicsUnit.Pixel);
                        Graphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
                        
                        Frameskip = !Frameskip;
                        
                        if (Frameskip)
                            ScreenBox.Invalidate();
                    }
                    catch { }
                }
            }
            else
            {
                RadialProgBar.StartingAngle += 7;
                if (RadialProgBar.StartingAngle > 359)
                    RadialProgBar.StartingAngle = 0;
            }
        }

        private void StatusCheckTick(object sender, EventArgs e)
        {
            if (IsCaptchaSolved())
            {
                Close();
                return;
            }
            if (IsCaptchaFailed())
            {
                ResetCaptcha();
                ThreadTools.Wait(500);
                if (Submit != null)
                {
                    Submit();
                    ThreadTools.Wait(500, true);
                }
                Close();
                return;
            }


            if (Clicks > 0)
            {
                Clicks--;

                UpdateRects();
                RefreshTick(null, null);
            }
        }

        private void ScreenMouseMove(object sender, MouseEventArgs e)
        {
            CefEventFlags Flags = CefEventFlags.None;
            if ((e.Button & MouseButtons.Left) != 0)
                Flags |= CefEventFlags.LeftMouseButton;
            if ((e.Button & MouseButtons.Right) != 0)
                Flags |= CefEventFlags.RightMouseButton;
            if ((e.Button & MouseButtons.Middle) != 0)
                Flags |= CefEventFlags.MiddleMouseButton;

            BrowserHost.SendMouseMoveEvent(new MouseEvent(e.X + FrameRect.X, e.Y + FrameRect.Y, Flags), false);
        }

        private void ScreenClicked(object sender, EventArgs e)
        {
            var ClickPos = ScreenBox.PointToClient(Cursor.Position);
            bool Verify = VerifyRect.Contains(ClickPos);
            ClickPos = new Point(ClickPos.X + FrameRect.X, ClickPos.Y + FrameRect.Y);
            BrowserHost.ExecuteClick(ClickPos);

            if (Verify)
            {
                LoadingMode(true);
                ThreadTools.Wait(3000, true);
                if (IsCaptchaSolved())
                    Close();
                else
                {
                    UpdateRects();
                    LoadingMode(false);
                }
            }
            else
            {
                StatusCheck.Enabled = false;
                StatusCheck.Enabled = true;
                Clicks++;
            }
        }

        public void LoadingMode(bool Enabled)
        {
            ScreenBox.Visible = !Enabled;
            RadialProgBar.Visible = Enabled;
            Refresh.Interval = Enabled ? 25 : 35;
        }
    }
}
