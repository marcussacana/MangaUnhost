using System.Windows.Forms;

namespace MangaUnhost {
    public partial class PluginInfoPreview : Form {
        public PluginInfoPreview(IHost Host, ILanguage CurrentLanguage) {
            InitializeComponent();

            var Info = Host.GetPluginInfo();

            lblAuthor.Text = CurrentLanguage.AuthorLbl;
            lblPluginName.Text = CurrentLanguage.PluginLbl;
            lblSupportComic.Text = CurrentLanguage.SupportComicLbl;
            lblSupportNovel.Text = CurrentLanguage.SupportNovelLbl;
            lblVersion.Text = CurrentLanguage.VersionLbl;

            lblAuthorVal.Text = Info.Author;
            lblPluginNameVal.Text = Info.Name;
            lblSupportComicVal.Text = Info.SupportComic ? CurrentLanguage.Yes : CurrentLanguage.No;
            lblSupportNovelVal.Text = Info.SupportNovel ? CurrentLanguage.Yes : CurrentLanguage.No;
            lblVersionVal.Text = Info.Version.ToString();
        }
    }
}
