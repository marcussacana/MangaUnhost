using CefSharp;
using CefSharp.Internals;
using CefSharp.WinForms;
using MangaUnhost.Browser;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace MangaUnhost
{
    public partial class WCRWindow : Form
    {
        int CurrentID = -1;
        string[] Chapters;
        public int ID
        {
            get => CurrentID;
            set
            {
                if (InvokeRequired)
                    Invoke(new MethodInvoker(() => ID = value));

                bool Refresh = CurrentID != value;
                CurrentID = value;

                if (Refresh)
                    LoadChapter();
            }
        }
        string Title
        {
            set
            {
                if (InvokeRequired)
                    Invoke(new MethodInvoker(() => Title = value));
                Text = value;
            }
        }
        ChromiumWebBrowser Browser;
        public WCRWindow(int ID, string[] Chapters)
        {
            InitializeComponent();
            this.Chapters = Chapters;

            Shown += (sender, args) => WindowShown(ID);

            EnterFullScreenMode();
        }

        public void Navigate(string URL)
        {
            if (Browser == null)
            {
                Browser = new ChromiumWebBrowser(URL);

                Browser.BrowserSettings = new BrowserSettings() { WebSecurity = CefState.Disabled };

                Browser.JavascriptObjectRepository.ResolveObject += (sender, args) =>
                {
                    if (args.ObjectName == "embedded")
                        args.ObjectRepository.Register("embedded", new WCRAPI(this), true);
                };

                Browser.MenuHandler = new MenuHandler(this);

                Browser.TitleChanged += (sender, Args) => Title = Args.Title;
                Browser.KeyDown += (sender, args) => OnKeyDown(args);
                Browser.KeyUp += (sender, args) => OnKeyUp(args);
                Browser.KeyPress += (sender, args) => OnKeyPress(args);
                Browser.Disposed += (sender, args) => Invoke(new MethodInvoker(() => Close()));

                Browser.Dock = DockStyle.Fill;

                Controls.Add(Browser);

#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached) { 
                    Browser.WaitInitialize();
                    Browser.ShowDevTools();
                }
#endif
            }
            else
                Browser.Load(URL);

            Browser.WaitForLoad();
            Browser.ExecuteScriptAsync("eval", "CefSharp.BindObjectAsync('embedded');", false);
        }

        public void WindowShown(int ID)
        {
            this.ID = ID;
        }
        public void EnterFullScreenMode()
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }

        public void LeaveFullScreenMode()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            Browser?.Dispose();
        }

        void LoadChapter()
        {
            if (ID >= Chapters.Length)
            {
                Close();
                return;
            }

            var Chapter = Chapters[ID];
            var Pages = string.Join("|", (from x in Directory.GetFiles(Chapter)
                                          where
                                            x.ToLower().EndsWith(".jpg")  ||
                                            x.ToLower().EndsWith(".jpeg") ||
                                            x.ToLower().EndsWith(".png")  ||
                                            x.ToLower().EndsWith(".bmp")  ||
                                            x.ToLower().EndsWith(".gif")
                                          orderby Path.GetFileName(x)
                                          select Path.GetFileName(x)));

            var Mode = Main.Reader switch
            {
                ReaderMode.Manga => "Manga",
                ReaderMode.Comic => "Comic",
                _ => "Other"
            };

            string URL = $"https://res/WebComicReader/Embedded/#Input=NativeImages&Mode={Mode}&Embedded=true&Base={HttpUtility.UrlEncode(Chapter)}&Pages={HttpUtility.UrlEncode(Pages)}";
            Navigate(URL);
        }
    }

    class WCRAPI
    {
        WCRWindow Window;
        public WCRAPI(WCRWindow Window)
        {
            this.Window = Window;
        }
        public async Task close()
        {
            Window.Close();
        }
        public async Task next()
        {
            Window.ID++;
        }
    }

    class MenuHandler : IContextMenuHandler
    {
        Form Parent;
        public MenuHandler(Form Parent)
        {
            this.Parent = Parent;
        }
        public void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.AddItem(CefMenuCommand.UserFirst, "Close");
        }

        public bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            if (commandId == CefMenuCommand.UserFirst)
            {
                Parent.Invoke(new MethodInvoker(() => Parent.Close()));
                return true;
            }
            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
        }

        public bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }
}
