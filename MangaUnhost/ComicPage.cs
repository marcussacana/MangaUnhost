using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing.Imaging;
using Ionic.Zip;
using System.Data;
using MangaUnhost.Others;
using System.Text;
using MangaUnhost.Browser;
using Encoder = System.Drawing.Imaging.Encoder;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Threading;
using System.Web.WebSockets;

namespace MangaUnhost
{
    public partial class ComicPage : UserControl
    {
        ILanguage Language => Main.Language;

        Action Clicked;

        public ComicPage(Action OnClicked)
        {
            InitializeComponent();
            Clicked = OnClicked;
        }

        private void lblMore_Click(object sender, EventArgs e)
        {
            Clicked();
        }
    }
}
