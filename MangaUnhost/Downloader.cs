using CefSharp;
using EPubFactory;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MangaUnhost
{
    public partial class Main : Form
    {

        public void AutoDownload(Uri URL)
        {
            var Host = (from x in Hosts where x.IsValidUri(URL) select x).Single();
            try
            {
                var DownloadAll = LoadUri(URL, Host);
                DownloadAll.PerformClick();
            }
            catch (Exception ex)
            {
                if (Program.Debug)
                    throw ex;
            }

            StatusBar.SecondLabelText = string.Empty;
            Status = CurrentLanguage.IDLE;

        }
        public void Reload(IHost Host = null) => LoadUri(CurrentInfo.Url, Host ?? CurrentHost);

        public VSButton LoadUri(Uri Uri, IHost Host)
        {
            Status = CurrentLanguage.LoadingComic;

            CurrentHost = CreateInstance(Host);

            CurrentInfo = CurrentHost.LoadUri(Uri);
            TitleLabel.Text = CurrentInfo.Title;
            CoverBox.Image = CurrentHost.GetDecoder().Decode(CurrentInfo.Cover);

            CurrentInfo.Url = Uri;

            CoverBox.Cursor = Cursors.Default;

            string Path = DataTools.GetRawName(CurrentInfo.Title.Trim(), FileNameMode: true);
            ChapterTools.MatchLibraryPath(ref Path, Settings.LibraryPath);
            RefreshCoverLink(Path);

            ButtonsContainer.Controls.Clear();

            VSButton DownAll = null;

            var Chapters = new Dictionary<int, string>();
            foreach (var Chapter in CurrentHost.EnumChapters())
            {
                if (Chapters.ContainsValue(Chapter.Value))
                    continue;

                VSButton Button = new VSButton()
                {
                    Size = new Size(110, 30),
                    Text = string.Format(CurrentLanguage.ChapterName, Chapter.Value),
                    Indentifier = CurrentHost
                };

                Button.Click += (sender, args) => {
                    try
                    {
                        IHost HostIsnt = (IHost)((VSButton)sender).Indentifier;
                        DownloadChapter(Chapters, Chapter.Key, HostIsnt, CurrentInfo);
                    }
                    catch (Exception ex)
                    {
                        if (Program.Debug)
                            throw ex;
                    }
                    StatusBar.SecondLabelText = string.Empty;
                    Status = CurrentLanguage.IDLE;
                };

                DownAll = Button;

                ButtonsContainer.Controls.Add(Button);
                Chapters.Add(Chapter.Key, Chapter.Value);
            }

            if (Chapters.Count > 1)
            {
                VSButton Button = new VSButton()
                {
                    Size = new Size(110, 30),
                    Text = CurrentLanguage.DownloadAll,
                    Indentifier = CurrentHost
                };

                Button.Click += (sender, args) => {
                    foreach (var Chapter in Chapters.Reverse())
                    {
                        try
                        {
                            IHost HostIsnt = (IHost)((VSButton)sender).Indentifier;
                            DownloadChapter(Chapters, Chapter.Key, HostIsnt, CurrentInfo);
                        }
                        catch (Exception ex)
                        {
                            if (Program.Debug)
                                throw;
                        }

                        StatusBar.SecondLabelText = string.Empty;
                        Status = CurrentLanguage.IDLE;
                    }
                };
                DownAll = Button;
                ButtonsContainer.Controls.Add(Button);
            }

            var PActions = CurrentHost.GetPluginInfo().Actions;
            if (PActions != null)
            {
                var Actions = (from x in PActions where x.Availability.HasFlag(ActionTo.ChapterList) select x);

                if (Actions.Any())
                {
                    foreach (var Action in Actions)
                    {
                        VSButton Bnt = new VSButton()
                        {
                            Size = new Size(110, 30),
                            Text = Action.Name,
                            Indentifier = CurrentHost
                        };
                        Bnt.Click += (sender, args) => Action.Action();
                        ButtonsContainer.Controls.Add(Bnt);
                    };
                }
            }

            Status = CurrentLanguage.IDLE;

            return DownAll;
        }

        private void DownloadChapter(Dictionary<int, string> Chapters, int ID, IHost Host, ComicInfo Info)
        {
            string Name = DataTools.GetRawName(Chapters[ID], FileNameMode: true);

            StatusBar.SecondLabelText = $"{Info.Title} - {string.Format(CurrentLanguage.ChapterName, Name)}";
            Status = CurrentLanguage.Loading;

            int KIndex = Chapters.IndexOfKey(ID);
            string NName = null;
            string NextChapterPath = null;
            if (KIndex > 0)
                NName = DataTools.GetRawName(Chapters.Values.ElementAt(KIndex - 1), FileNameMode: true);

            string Title = DataTools.GetRawName(Info.Title.Trim(), FileNameMode: true);

            ChapterTools.MatchLibraryPath(ref Title, Settings.LibraryPath);
            RefreshCoverLink(Title);

            string TitleDir = Path.Combine(Settings.LibraryPath, Title);
            if (!Directory.Exists(TitleDir))
                Directory.CreateDirectory(TitleDir);

            ChapterTools.GetChapterPath(Languages, CurrentLanguage, TitleDir, Name, out string ChapterPath, false);

            string AbsolueChapterPath = Path.Combine(TitleDir, ChapterPath);

            if (Settings.SkipDownloaded && File.Exists(AbsolueChapterPath.TrimEnd('\\', '/') + ".html"))
                return;

            int PageCount = 1;

            if (Info.ContentType == ContentType.Comic)
            {
                PageCount = Host.GetChapterPageCount(ID);

                if (Directory.Exists(AbsolueChapterPath) && Directory.GetFiles(AbsolueChapterPath, "*").Length < PageCount)
                    Directory.Delete(AbsolueChapterPath, true);

                if (Settings.SkipDownloaded && Directory.Exists(AbsolueChapterPath))
                {
                    var Pages = (from x in Directory.GetFiles(AbsolueChapterPath) select Path.GetFileName(x)).ToArray();
                    ChapterTools.GenerateComicReader(CurrentLanguage, Pages, NextChapterPath, TitleDir, ChapterPath, Name);
                    return;
                }
            }

            if (Info.ContentType == ContentType.Novel)
            {
                ChapterPath = Path.Combine(AbsolueChapterPath, string.Format(CurrentLanguage.ChapterName, Name) + ".epub");
                if (Settings.SkipDownloaded && File.Exists(ChapterPath))
                    return;

            }


            if (!Directory.Exists(AbsolueChapterPath))
                Directory.CreateDirectory(AbsolueChapterPath);

            if (NName != null)
                ChapterTools.GetChapterPath(Languages, CurrentLanguage, TitleDir, NName, out NextChapterPath, false);

            string UrlData = string.Format(Properties.Resources.UrlFile, Info.Url.AbsoluteUri);
            File.WriteAllText(Path.Combine(TitleDir, "Online.url"), UrlData);


            switch (Info.ContentType)
            {
                case ContentType.Comic:
                    var Decoder = Host.GetDecoder();
                    List<string> Pages = new List<string>();
                    foreach (var Data in Host.DownloadPages(ID))
                    {
                        Status = string.Format(CurrentLanguage.Downloading, Pages.Count + 1, PageCount);
                        Application.DoEvents();

                        try
                        {
                            string PageName = $"{Pages.Count:D3}.png";
                            string PagePath = Path.Combine(TitleDir, ChapterPath, PageName);

                            if ((SaveAs)Settings.SaveAs == SaveAs.RAW)
                            {
                                File.WriteAllBytes(PagePath, Data);
                            }
                            else
                            {
                                using (Bitmap Result = Decoder.Decode(Data))
                                {
                                    PageName = $"{Pages.Count:D3}.{GetExtension(Result, out ImageFormat Format)}";
                                    PagePath = Path.Combine(TitleDir, ChapterPath, PageName);
                                    Result.Save(PagePath, Format);
                                }
                            }

                            if (Settings.ImageClipping)
                                ClipQueue.Enqueue(PagePath);

                            Pages.Add(PageName);
                        }
                        catch (Exception ex)
                        {
                            if (Program.Debug)
                                throw;
                        }
                    }

                    if (Settings.ReaderGenerator)
                    {
                        ChapterTools.GenerateComicReader(CurrentLanguage, Pages.ToArray(), NextChapterPath, TitleDir, ChapterPath, Name);
                        ChapterTools.GenerateReaderIndex(Languages, CurrentLanguage, Info, TitleDir, Name);
                    }

                    break;
                case ContentType.Novel:
                    var Chapter = Host.DownloadChapter(ID);
                    AsyncContext.Run(async () => {
                        using (var Epub = await EPubWriter.CreateWriterAsync(File.Create(ChapterPath), Info.Title ?? "Untitled", Chapter.Author ?? "Anon", Chapter.URL ?? "None"))
                        {
                            Chapter.HTML.RemoveNodes("//script");
                            Chapter.HTML.RemoveNodes("//iframe");

                            foreach (var Node in Chapter.HTML.DocumentNode.DescendantsAndSelf())
                            {
                                if (Node.Name == "img" && Node.GetAttributeValue("src", string.Empty) != string.Empty)
                                {
                                    string Src = Node.GetAttributeValue("src", string.Empty);
                                    string RName = Path.GetFileName(Src.Substring(null, "?", IgnoreMissmatch: true));
                                    string Mime = ResourceHandler.GetMimeType(Path.GetExtension(RName));

                                    if (Node.GetAttributeValue("type", string.Empty) != string.Empty)
                                        Mime = Node.GetAttributeValue("type", string.Empty);

                                    Uri Link = new Uri(Info.Url, Src);

                                    if (string.IsNullOrWhiteSpace(RName))
                                        continue;

                                    byte[] Buffer = await Link.TryDownloadAsync();

                                    if (Buffer == null)
                                        continue;

                                    await Epub.AddResourceAsync(RName, Mime, Buffer);
                                    Node.SetAttributeValue("src", RName);
                                }

                                if (Node.Name == "link" && Node.GetAttributeValue("rel", string.Empty) == "stylesheet" && Node.GetAttributeValue("href", string.Empty) != string.Empty)
                                {
                                    string Src = Node.GetAttributeValue("href", string.Empty);
                                    string RName = Path.GetFileName(Src.Substring(null, "?", IgnoreMissmatch: true));
                                    string Mime = ResourceHandler.GetMimeType(Path.GetExtension(RName));

                                    if (Node.GetAttributeValue("type", string.Empty) != string.Empty)
                                        Mime = Node.GetAttributeValue("type", string.Empty);

                                    Uri Link = new Uri(Info.Url, Src);

                                    if (string.IsNullOrWhiteSpace(RName))
                                        continue;

                                    byte[] Buffer = await Link.TryDownloadAsync();

                                    if (Buffer == null)
                                        continue;

                                    await Epub.AddResourceAsync(RName, Mime, Buffer);
                                    Node.SetAttributeValue("href", RName);
                                }
                            }

                            Epub.Publisher = lblTitle.Text;

                            await Epub.AddChapterAsync("chapter.xhtml", Chapter.Title, Chapter.HTML.ToHTML());


                            await Epub.WriteEndOfPackageAsync();
                        }
                    });
                    break;
                default:
                    throw new Exception("Invalid Content Type");
            }
        }

        private string GetExtension(Bitmap Bitmap, out ImageFormat Format)
        {
            switch ((SaveAs)Settings.SaveAs)
            {
                case SaveAs.PNG:
                    Format = ImageFormat.Png;
                    return "png";
                case SaveAs.JPG:
                    Format = ImageFormat.Jpeg;
                    return "jpg";
                case SaveAs.BMP:
                    Format = ImageFormat.Bmp;
                    return "bmp";
                case SaveAs.AUTO:
                    Format = Bitmap.RawFormat;
                    return Bitmap.GetImageExtension();
                default:
                    throw new Exception("Invalid Save As Image Format");
            }
        }
    }
}
