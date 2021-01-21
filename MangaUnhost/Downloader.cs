using CefSharp;
using EPubFactory;
using MangaUnhost.Browser;
using MangaUnhost.Others;
using Microsoft.VisualBasic.Devices;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MangaUnhost
{
    public partial class Main : Form
    {
        public static ulong AvailablePhysicalMemory => new ComputerInfo().AvailablePhysicalMemory;

        public void AutoDownload(Uri URL)
        {
            var HostQuery = (from x in Hosts where x.IsValidUri(URL) select x);
            if (!HostQuery.Any())
            {
                try
                {
                    var HTML = Encoding.UTF8.GetString(URL.TryDownload(URL.Host, ProxyTools.UserAgent));
                    HostQuery = (from x in Hosts where x.GetPluginInfo().GenericPlugin && x.IsValidPage(HTML, URL) select x);

                }
                catch { }
            }
            var Host = HostQuery.FirstOrDefault();
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

            if (CurrentInfo.Url == null)
                CurrentInfo.Url = Uri;

            CoverBox.Cursor = Cursors.Default;

            string Dir = DataTools.GetRawName(CurrentInfo.Title.Trim(), FileNameMode: true);
            ChapterTools.MatchLibraryPath(ref Dir, Settings.LibraryPath, CurrentInfo.Url, ReplaceMode.UpdateURL, CurrentLanguage);
            RefreshCoverLink(Dir);

            var OnlinePath = Path.Combine(Settings.LibraryPath, Dir, "Online.url");
            if (Directory.Exists(Dir) && !File.Exists(OnlinePath)) {
                var OnlineData = string.Format(Properties.Resources.UrlFile, CurrentInfo.Url.AbsoluteUri);
                File.WriteAllText(OnlinePath, OnlineData);
            }

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

                Button.Click += (sender, args) =>
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

                Button.Click += (sender, args) =>
                {
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
            ChapterTools.MatchLibraryPath(ref Title, Settings.LibraryPath, CurrentInfo.Url, (ReplaceMode)Settings.ReplaceMode, CurrentLanguage);
            RefreshCoverLink(Title);

            string TitleDir = Path.Combine(Settings.LibraryPath, Title);
            if (!Directory.Exists(TitleDir))
            {
                Directory.CreateDirectory(TitleDir);
                RefreshLibrary();
            }

            ChapterTools.GetChapterPath(Languages, CurrentLanguage, TitleDir, Name, out string ChapterPath, false);

            string AbsoluteChapterPath = Path.Combine(TitleDir, ChapterPath);

            if (Settings.SkipDownloaded && File.Exists(AbsoluteChapterPath.TrimEnd('\\', '/') + ".html"))
                return;

            int PageCount = 1;

            if (Info.ContentType == ContentType.Comic)
            {
                PageCount = Host.GetChapterPageCount(ID);

                if (Directory.Exists(AbsoluteChapterPath) && Directory.GetFiles(AbsoluteChapterPath, "*").Length < PageCount)
                    Directory.Delete(AbsoluteChapterPath, true);

                if (Settings.SkipDownloaded && Directory.Exists(AbsoluteChapterPath))
                {
                    var Pages = (from x in Directory.GetFiles(AbsoluteChapterPath) select Path.GetFileName(x)).ToArray();
                    ChapterTools.GenerateComicReader(CurrentLanguage, Pages, NextChapterPath, TitleDir, ChapterPath, Name);
                    string OnlineData = string.Format(Properties.Resources.UrlFile, Info.Url.AbsoluteUri);
                    File.WriteAllText(Path.Combine(TitleDir, "Online.url"), OnlineData);
                    return;
                }
            }

            if (Info.ContentType == ContentType.Novel)
            {
                ChapterPath = Path.Combine(AbsoluteChapterPath, string.Format(CurrentLanguage.ChapterName, Name) + ".epub");
                if (Settings.SkipDownloaded && File.Exists(ChapterPath))
                    return;

            }


            if (!Directory.Exists(AbsoluteChapterPath))
                Directory.CreateDirectory(AbsoluteChapterPath);

            if (NName != null)
                ChapterTools.GetChapterPath(Languages, CurrentLanguage, TitleDir, NName, out NextChapterPath, false);

            string UrlData = string.Format(Properties.Resources.UrlFile, Info.Url.AbsoluteUri);
            File.WriteAllText(Path.Combine(TitleDir, "Online.url"), UrlData);


            switch (Info.ContentType)
            {
                case ContentType.Comic:
                    var Decoder = Host.GetDecoder();
                    List<string> Pages = new List<string>();
                    foreach (var Data in Host.DownloadPages(ID).CatchExceptions())
                    {
                        Status = string.Format(CurrentLanguage.Downloading, Pages.Count + 1, PageCount);
                        Application.DoEvents();

                        try
                        {
#if NOASYNCSAVE
                            string PageName = $"{Pages.Count:D3}.png";
                            string PagePath = Path.Combine(TitleDir, ChapterPath, PageName);
                            
                            Page OutPage = new Page();
                            OutPage.Data = Data;

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
                                PostProcessQueue.Enqueue(OutPage);
                            else
                                File.WritaAllData(OutPage.Path, OutPage.Data);

                            while (PostProcessQueue.Count > 0) {
                                ThreadTools.Wait(1000, true);
                            }
#else
                            string PageName = $"{Pages.Count:D3}.png";
                            string PagePath = Path.Combine(TitleDir, ChapterPath, PageName);
                            
                            Page OutPage = new Page();
                            OutPage.Data = Data;

                            if ((SaveAs)Settings.SaveAs != SaveAs.RAW) {
                                using (MemoryStream Buffer = new MemoryStream())
                                using (Bitmap Result = Decoder.Decode(Data))
                                {
                                    PageName = $"{Pages.Count:D3}.{GetExtension(Result, out ImageFormat Format)}";
                                    PagePath = Path.Combine(TitleDir, ChapterPath, PageName);
                                    Result.Save(Buffer, Format);
                                    OutPage.Data = Buffer.ToArray();
                                }
                            }

                            OutPage.Path = PagePath;

                            if (PostProcessQueue.Count > 5)
                            {
                                const ulong MinMemory = 400000000;
                                while (AvailablePhysicalMemory < MinMemory && PostProcessQueue.Count > 0 || (Main.Config.MaxPagesBuffer != 0 && PostProcessQueue.Count && Main.Config.MaxPagesBuffer >= MaxPagesBuffer)) {
                                    ThreadTools.Wait(1000, true);
                                }
                            }

                            PostProcessQueue.Enqueue(OutPage);
#endif
                            Pages.Add(PageName);
                        }
                        catch (Exception ex)
                        {
                            if (Program.Debug)
                                throw;
                            Console.Error.WriteLine(ex.ToString());
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
                    AsyncContext.Run(async () =>
                    {
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
                    throw new Exception("Invalid Save As Image Format: " + Settings.SaveAs);
            }
        }
    }
}
