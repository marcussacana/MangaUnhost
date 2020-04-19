using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MangaUnhost {
    public partial class PluginInfoPreview : Form {
        public PluginInfoPreview(IHost Host, ILanguage CurrentLanguage) {
            InitializeComponent();

            var Info = Host.GetPluginInfo();

            if (Info.Icon != null && Info.Icon.Length > 0) {
                using (MemoryStream Stream = new MemoryStream(Info.Icon)) {
                    var Icon = Image.FromStream(Stream);
                    pbIcon.Image = Icon;
                }
            }

            lblAuthor.Text = CurrentLanguage.AuthorLbl;
            lblPluginName.Text = CurrentLanguage.PluginLbl;
            lblSupportComic.Text = CurrentLanguage.SupportComicLbl;
            lblSupportNovel.Text = CurrentLanguage.SupportNovelLbl;
            lblGenericPlugin.Text = CurrentLanguage.GenericPluginLbl;
            lblVersion.Text = CurrentLanguage.VersionLbl;

            lblAuthorVal.Text = Info.Author;
            lblPluginNameVal.Text = Info.Name;
            lblSupportComicVal.Text = Info.SupportComic ? CurrentLanguage.Yes : CurrentLanguage.No;
            lblSupportNovelVal.Text = Info.SupportNovel ? CurrentLanguage.Yes : CurrentLanguage.No;
            lblGenericPluginValue.Text = Info.GenericPlugin ? CurrentLanguage.Yes : CurrentLanguage.No;
            lblVersionVal.Text = Info.Version.ToString();
        }
    }
}
