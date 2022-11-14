using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace MangaUnhost.Others {
    public static class ChapterTools {
        public static void GenerateComicReader(ILanguage CurrentLanguage, string[] Pages, string LastChapter, string NextChapterPath, string ComicDir, string ChapterPath, string ChapterName) {
            string HtmlPath = Path.Combine(ComicDir, ChapterPath.TrimEnd('\\', '/') + ".html");
            string LastHtmlPath = LastChapter == null ? null : Path.Combine(ComicDir, LastChapter.TrimEnd('\\', '/') + ".html");

            if (LastChapter != null && !File.Exists(LastHtmlPath) && File.Exists(LastChapter))
                LastHtmlPath = LastChapter;

            ChapterPath = Path.GetFileName(ChapterPath);

            if (LastChapter != null && File.Exists(LastHtmlPath)) {
                var LastHtml = File.ReadAllText(LastHtmlPath);
                if (!LastHtml.Contains("<a href=")) {
                    int EndReader = LastHtml.IndexOf("</div>");
                    if (EndReader > 0)
                    {
                        while (LastHtml[EndReader - 1] != '>')
                            EndReader--;

                        string RelativeHtmlPath = $".{Path.AltDirectorySeparatorChar}{Path.GetFileName(HtmlPath.TrimEnd('\\', '/'))}";

                        LastHtml = LastHtml.Insert(EndReader, string.Format(Properties.Resources.ComicReaderNextChapterBase, RelativeHtmlPath, HttpUtility.HtmlEncode(CurrentLanguage.NextChapter)));
                        File.WriteAllText(LastHtmlPath, LastHtml);
                    }
                }
            }

            string NextHtmlPath = null;
            if (NextChapterPath != null)
                NextHtmlPath = $".{Path.AltDirectorySeparatorChar}{Path.GetFileName(NextChapterPath.TrimEnd('\\', '/') + ".html")}";

            string Reader = Properties.Resources.ComicReaderHtmlBase;
            string Content = string.Empty;

            for (int i = 0; i < Pages.Length; i++) {

                bool Last = i + 1 >= Pages.Length;
                string Page = $".{Path.AltDirectorySeparatorChar}{Path.Combine(ChapterPath, Pages[i])}";

                if (Last && i != 0) {
                    Content += string.Format(Properties.Resources.ComicReaderLastPageBase, null, i);
                } else {
                    string NextPage = Last ? "" : $".{Path.AltDirectorySeparatorChar}{Path.Combine(ChapterPath, Pages[i + 1])}";
                    Content += string.Format(Properties.Resources.ComicReaderPageBase, i == 0 ? Page : null, i, i + 1, NextPage.ToLiteral());
                }
            }

            if (NextHtmlPath != null)
                Content += "\r\n" + string.Format(Properties.Resources.ComicReaderNextChapterBase, NextHtmlPath, HttpUtility.HtmlEncode(CurrentLanguage.NextChapter));

            Reader = string.Format(Reader, HttpUtility.HtmlEncode(string.Format(CurrentLanguage.ChapterName, ChapterName)), Content);

            File.WriteAllText(HtmlPath, Reader, Encoding.UTF8);
        }

        public static void GenerateReaderIndex(ILanguage[] Language, ILanguage CurrentLanguage, ComicInfo Info, string ComicDir, string ChapterName) {
            string IndexPath = MatchFile(ComicDir, CurrentLanguage.Index + ".html", (from x in Language select x.Index + ".html").ToArray());
            string CoverName = MatchFile(ComicDir, CurrentLanguage.Cover + ".png", (from x in Language select x.Cover + ".png").ToArray(), false);

            string CoverPath = Path.Combine(ComicDir, CoverName);

            if (!File.Exists(IndexPath)) {
                string Base = string.Format(Properties.Resources.ComicReaderIndexBase, HttpUtility.HtmlEncode(Info.Title), $".{Path.AltDirectorySeparatorChar}{CoverName}");
                File.WriteAllText(IndexPath, Base, Encoding.UTF8);
            }

            if (!File.Exists(CoverPath)) {
                using (MemoryStream Stream = new MemoryStream(Info.Cover)) {
                    Bitmap Cover = Image.FromStream(Stream) as Bitmap;
                    Cover.Save(CoverPath, ImageFormat.Png);
                }
            }

            string Content = null;
            string New = File.ReadAllText(IndexPath, Encoding.UTF8);
            while (New != Content) {
                Content = New;
                New = Content.TrimEnd('\r', '\n', ' ', '\t');
                string[] Sufixes = new string[] { "</html>", "</body>", "</div>" };
                foreach (string Sufix in Sufixes) {
                    if (New.ToLower().EndsWith(Sufix))
                        New = New.Substring(0, New.Length - Sufix.Length);
                }
            }
            GetChapterPath(Language, CurrentLanguage, ComicDir, ChapterName, out string ChapterPath, false);
            New += "\r\n" + string.Format(Properties.Resources.ComicReaderIndexChapterBase, $".{Path.AltDirectorySeparatorChar}{ChapterPath}.html", HttpUtility.HtmlEncode(string.Format(CurrentLanguage.ChapterName, ChapterName))) + "\r\n";
            New += "      </div>\r\n   </body>\r\n</html>";

            File.WriteAllText(IndexPath, New, Encoding.UTF8);
        }

        public static string MatchFile(string Dir, string Default, string[] Names, bool IncludeDir = true) {
            foreach (var Name in Names) {
                string Possibility = Path.Combine(Dir, Name);
                if (File.Exists(Possibility) && IncludeDir)
                    return Possibility;
                else if (File.Exists(Possibility))
                    return Name;
            }
            if (IncludeDir)
                return Path.Combine(Dir, Default);
            else
                return Default;
        }

        public static void MatchLibraryPath(ref string Dir, string BaseDir, Uri Url, ReplaceMode Mode, ILanguage Language) {
            if (Directory.Exists(BaseDir)) {
                string[] Dirs = Directory.GetDirectories(BaseDir, "*", SearchOption.TopDirectoryOnly);
                Dirs = (from x in Dirs select Path.GetFileName(x.TrimEnd('/', '\\'))).ToArray();
                var MDirs = (from x in Dirs select (from y in x.Split(' ') where y.Length > 4 select MinifyString(y)).ToArray()).ToArray();

                var MDir = (from x in Dir.Split(' ') where x.Length > 4 select MinifyString(x)).ToArray();
                for (int i = 0; i < MDirs.Length; i++)
                {
                    string CurrentSavedUrl = null;
                    string CurrentDir = Dirs[i]; 
                    string CurrentOnlineUrl = Path.Combine(BaseDir, CurrentDir, "Online.url");
                    if (File.Exists(CurrentOnlineUrl)) {
                        CurrentSavedUrl = Ini.GetConfig("InternetShortcut", "URL", CurrentOnlineUrl).Substring(null, "#", IgnoreMissmatch: true);
                    }

                    if (CurrentSavedUrl == Url.AbsoluteUri)
                    {
                        Dir = CurrentDir;
                        break;
                    }

                    if (DiffCheck(MDirs[i], MDir) > 0.8) {

                        //Search For Next New Folder Name or if this one is already Downloaded in any possible New Folder
                        string NDir = CurrentDir;
                        int x = 0;
                        do {
                            NDir = CurrentDir + (x++ == 0 ? "" : " (" + x.ToString("D2") + ")");
                            string OnlineUrl = Path.Combine(BaseDir, NDir, "Online.url");
                            if (!File.Exists(OnlineUrl))
                                break;
                            try {
                                var URI = Ini.GetConfig("InternetShortcut", "URL", OnlineUrl).Substring(null, "#", IgnoreMissmatch: true);
                                if (URI.ToLower() == Url.AbsoluteUri.ToLower()) {
                                    Mode = ReplaceMode.NewFolder;
                                    break;
                                }
                            } catch {
                                var OnlineData = string.Format(Properties.Resources.UrlFile, Url.AbsoluteUri);
                                File.WriteAllText(OnlineUrl, OnlineData);
                            }
                        } while (true);


                        if (CurrentDir != NDir) {
                            switch (Mode) {
                                case ReplaceMode.Ask:
                                    var Reply = AccountTools.PromptOption(Language.ReplaceMode, new[] { Language.UpdateURL, Language.NewFolder });
                                    if (Reply == Language.UpdateURL)
                                        goto case ReplaceMode.UpdateURL;
                                    else
                                        goto case ReplaceMode.NewFolder;
                                case ReplaceMode.UpdateURL:
                                    break;
                                case ReplaceMode.NewFolder:
                                    CurrentDir = NDir;
                                    break;
                            }
                        }


                        Dir = CurrentDir;
                        break;
                    }
                }
            }

            Dir = new string((from x in Dir where !Path.GetInvalidFileNameChars().Contains(x) select x).ToArray());
        }

        public static double DiffCheck(string[] ItemsA, string[] ItemsB)
        {
            double MatchCount = 0;
            double MissMatchCount = 0;

            var Primary = ItemsA.Length > ItemsB.Length ? ItemsA : ItemsB;
            var Secondary = ItemsA.Length > ItemsB.Length ? ItemsB : ItemsA;
            for (int i = 0; i < Primary.Length; i++)
            {
                if (Secondary.Contains(Primary[i]))
                    MatchCount++;
                else
                    MissMatchCount++;
            }

            return (MatchCount - MissMatchCount) / Secondary.Length;
        }

        public static void GetChapterPath(ILanguage[] Languages, ILanguage CurrentLanguage, string MangaDir, string ChapterName, out string ChapterPath, bool IncludeDir = true) {
            foreach (var Lang in Languages) {
                string PossiblePath = Path.Combine(MangaDir, Lang.Chapters, string.Format(Lang.ChapterName, ChapterName));

                if (!Directory.Exists(PossiblePath))
                    continue;

                if (IncludeDir)
                    ChapterPath = PossiblePath;
                else
                    ChapterPath = Path.Combine(Lang.Chapters, string.Format(Lang.ChapterName, ChapterName));

                return;
            }

            if (IncludeDir)
                ChapterPath = Path.Combine(MangaDir, CurrentLanguage.Chapters, string.Format(CurrentLanguage.ChapterName, ChapterName));
            else
                ChapterPath = Path.Combine(CurrentLanguage.Chapters, string.Format(CurrentLanguage.ChapterName, ChapterName));

        }

        public static string MinifyString(string String) =>
            new string((from x in String
                        where !char.IsPunctuation(x) &&
                        !char.IsSymbol(x) && !char.IsWhiteSpace(x)
                        select x).ToArray()).ToLower();

    }
}
