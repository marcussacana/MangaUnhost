using CefSharp;
using CefSharp.WinForms;
using MangaUnhost.Browser;
using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace MangaUnhost
{
    public partial class CEFWindow : Form
    {
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
        public CEFWindow(ChromiumWebBrowser Browser)
        {
            InitializeComponent();

            Browser.MenuHandler = new MenuHandler(this);

            Browser.TitleChanged += (sender, Args) => Title = Args.Title;
            Browser.KeyDown += (sender, args) => OnKeyDown(args);
            Browser.KeyUp += (sender, args) => OnKeyUp(args);
            Browser.KeyPress += (sender, args) => OnKeyPress(args);
            Browser.Disposed += (sender, args) => Invoke(new MethodInvoker(() => Close()));

            Browser.Dock = DockStyle.Fill;
            this.Browser = Browser;
            Controls.Add(Browser);

            EnterFullScreenMode();
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
