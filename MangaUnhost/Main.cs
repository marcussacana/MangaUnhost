﻿using System;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Noesis.Drawing.Imaging.WebP;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic;
using System.Web;
using TLIB;

namespace MangaUnhost {
    public partial class Main : Form {

        Host.IHost[] Hosts = new Host.IHost[] {
            new Host.Mangahost(),
            new Host.MangaHere()
        };

        Host.IHost AtualHost = null;
        
        public Main() {
            InitializeComponent();

            string Title = "MangaUnhost - v" + AppVeyor.CurrentVersion;
            Text = Title;
            iTalk_ThemeContainer1.Text = Title;
            TBSaveAs.Text = AppDomain.CurrentDomain.BaseDirectory + "Biblioteca";
            
            foreach (var Host in Hosts) {
                SupportList.Items.Add(Host.HostName);
            }
        }
        

        private void CheckUrl_Tick(object sender, EventArgs e) {
            string ClipboardContent = Clipboard.GetText();

            if (string.IsNullOrEmpty(ClipboardContent))
                return;           

            foreach (var Host in Hosts)
                if (Host.IsValidLink(ClipboardContent)) {
                    Clipboard.Clear();

                    AtualHost = Host;
                    new Thread(() => {
                        string Name, URL;
                        AtualHost.Initialize(ClipboardContent, out Name, out URL);
                        ShowManga(Name, URL);
                    }).Start();
                    break;
                }
        }

        delegate void Invoker();
        private string Status {
            get {
                return StatusLBL.Text;
            }
            set {
                Invoke(new Invoker(() => { StatusLBL.Text = value; }));
            }
        }

        private string Title {
            get {
                return TitleLBL.Text;
            }
            set {
                Invoke(new Invoker(() => { TitleLBL.Text = value; }));
            }
        }
        private string LP = string.Empty;
        private string PictureURL { get { return LP; } set {
                LP = value;
                Stream MEM = new MemoryStream();
                Download(value, MEM);
                Invoker Delegate = new Invoker(() => {
                    Poster.Image = Image.FromStream(MEM);
                    MEM.Close();
                });
                Invoke(Delegate);
            } }
        
        private void ShowManga(string Name, string Page) {
            Status = "Conectando...";

            AtualHost.LoadPage(Page);
            Name = AtualHost.GetFullName();

            Status = "Obtendo informações do: " + Name;
            PictureURL = AtualHost.GetPosterUrl();
            Invoke(new Invoker(() => { MainPanel.Visible = true; }));
            Title = Name;
            string[] Chapters = AtualHost.GetChapters();
            Actions = new List<Action>();

            ButtonLst.Controls.Clear();
            for (int i = 0; i < Chapters.Length; i ++)
                Invoke(new Invoker(() => {
                    Control Button = RegisterChapter(Chapters[i], i - 1 >= 0 ? Chapters[i-1] : null);
                    ButtonLst.Controls.Add(Button);
                }));
            Invoke(new Invoker(() => {
                Control Button = RegisterGetAll();
                ButtonLst.Controls.Add(Button);
            }));

            Status = "Aguardando Link...";
        }

        internal static string ClearFileName(string Name) {
            string[] Reps = new string[] {
                ":", "-", "\\", "＼", "/", "／", "?", "？", "<", "＜", ">", "＞"
            };
            string Rst = Name.Trim(); 
            for (int i = 0; i < Reps.Length; i+= 2) {
                Rst = Rst.Replace(Reps[i], Reps[i + 1]);
            }

            return Rst;
        }
        private Control RegisterChapter(string Chapter, string NextChapter) {
            string ID = AtualHost.GetChapterName(Chapter), ID2 = null;

            if (NextChapter != null) {
                ID2 = AtualHost.GetChapterName(NextChapter);
            }


            string Text = "Capítulo " + ID;
            iTalk_Button_1 Button = new iTalk_Button_1() {
                Text = Text
            };

            Action Action = new Action(() => {
                DownloadChapter(Chapter, Next: NextChapter);
            });

            int Index = Actions.Count;
            Actions.Add(Action);
            Button.Click += (a, b) => { Actions[Index].Invoke(); };
            return Button;
        }

        List<Action> Actions = new List<Action>();
        private Control RegisterGetAll() {
            iTalk_Button_1 Button = new iTalk_Button_1();
            Button.Text = "Baixar Tudo";
            Button.Click += (a, b) => {
                new Thread(() => {
                    Action[] Chapters = Actions.ToArray(); 
                    for (int i = Chapters.Length - 1; i >= 0; i--) {
                        Chapters[i].Invoke();
                    }
                }).Start();
            };
            return Button;
        }

        private void DownloadChapter(string URL, bool Open = false, string Next = null) {
            string ID = AtualHost.GetChapterName(URL);

            Status = string.Format("Baixando Informações do Capítulo {0}...", ID);
            string Manga = URL;
            string CapDir = TBSaveAs.Text + string.Format("\\{0}\\", Title);
            string WorkDir = TBSaveAs.Text + string.Format("\\{0}\\Capítulo {1}\\", Title, ID);            
            string CapName = string.Format("Capítulo {0}", ID);

            if (File.Exists(CapDir + CapName + ".html") && ckResume.Checked) {
                Status = "Aguardando Link...";
                return;
            }

            string HTML = Download(Manga, Encoding.UTF8);
            string[] Pages = AtualHost.GetChapterPages(HTML);
            int Pag = 0;

            if (!Directory.Exists(WorkDir))
                Directory.CreateDirectory(WorkDir);
            if (File.Exists(WorkDir + "Pages.lst"))
                File.Delete(WorkDir + "Pages.lst");

            TextWriter FileList = File.CreateText(WorkDir + "Pages.lst");
            foreach (string Page in Pages) {
                Application.DoEvents();
                Status = string.Format("Baixando Capítulo {2} ({0}/{1})...", Pag++, Pages.Length, ID);
                string SaveAs = WorkDir + Pag.ToString("D3") + Path.GetExtension(Page.Split('?')[0]);
                if (!File.Exists(SaveAs))
                    if (Path.GetExtension(Page).EndsWith(".webp")) {
                        SaveAs = WorkDir + Pag.ToString("D3") + ".png";
                        if (!File.Exists(SaveAs)) {
                            MemoryStream MEM = new MemoryStream();
                            Download(Page, MEM);
                            MEM.Seek(0, SeekOrigin.Begin);
                            Bitmap PageTexture = DecodeWebP(MEM);
                            PageTexture.Save(SaveAs, System.Drawing.Imaging.ImageFormat.Png);
                             
                            new Thread(() => ValidateWebP(SaveAs, PageTexture, MEM.ToArray())).Start();

                            MEM.Close();
                        }
                    } else
                        Download(Page, SaveAs);
                FileList.WriteLine(SaveAs.Substring(WorkDir.Length, SaveAs.Length - WorkDir.Length));
            }
            FileList.Close();
            if (ckGenReader.Checked)
                GenerateBook(WorkDir, CapDir, Next, CapName);
            Status = "Aguardando Link...";
        }

        //Prevent Decoder Bugs
        private void ValidateWebP(string SaveAs, Bitmap PageTexture, byte[] WebP) {
            Thread.Sleep(500);
            Bitmap NewText = DecodeWebP(new MemoryStream(WebP));
            bool Equals = PageTexture.Size == NewText.Size;
            for (int x = 0; x < NewText.Width && Equals; x += 2)
                for (int y = 0; y < NewText.Height && Equals; y += 2) {
                    Color NPixel = NewText.GetPixel(x, y);
                    Color OPixel = PageTexture.GetPixel(x, y);
                    if (NPixel != OPixel)
                        Equals = false;
                }

            if (Equals)
                return;

            Thread.Sleep(500);
            Bitmap NewText2 = DecodeWebP(new MemoryStream(WebP));
            Equals = PageTexture.Size == NewText2.Size;
            for (int x = 0; x < NewText2.Width && Equals; x += 2)
                for (int y = 0; y < NewText2.Height && Equals; x += 2) {
                    Color NPixel = NewText2.GetPixel(x, y);
                    Color OPixel = PageTexture.GetPixel(x, y);
                    if (NPixel != OPixel)
                        Equals = false;
                }

            if (Equals)
                return;

            NewText2.Save(SaveAs, System.Drawing.Imaging.ImageFormat.Png);
        }
        private Bitmap DecodeWebP(Stream Stream) {
            return WebPFormat.LoadFromStream(Stream);
        }

        private void GenerateBook(string MangDir, string CapsDir, string Next, string CapName) {
            const string PrefixMask = "<DOCTYPE HTML>\r\n<html>\r\n<head>\r\n<title>{0} - HTML Reader</title>";
            const string PrefixPart2 = "\r\n<style>body{background-color: #000000;}</style>\r\n</head>\r\n<body>\r\n<div align=\"center\">";
            const string PageMask = "<img src=\"{0}\" style=\"max-width:100%;\"/><br/>";
            const string ChapMask = "<a href=\".\\{0}\" style=\"color: #FFF;\">Next Chapter</a>";
            const string SufixMask = "</div>\r\n</body>\r\n</html>";
            const string JQUERY = "<script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.4.3/jquery.min.js\">";
            const string AutoAdv = "  <script type=\"text/javascript\" language=\"javascript\">\r\n         $(function () {\r\n             var $win = $(window);\r\n\r\n             $win.scroll(function () {\r\n                 if ($win.height() + $win.scrollTop() == $(document).height()) {\r\n                     document.location = \"{0}\";\r\n                 }\r\n             });\r\n         });\r\n    </script>";
            Status = "Gerando Leitor...";
            string FileList = MangDir + "Pages.lst";
            string HtmlFile = CapsDir + CapName + ".html";
            if (File.Exists(HtmlFile))
                File.Delete(HtmlFile);
            using (TextWriter Generator = File.CreateText(HtmlFile)) {
                Generator.WriteLine(PrefixMask, CapName);
                Generator.WriteLine(PrefixPart2);
                //Generator.WriteLine(JQUERY);
                //Generator.WriteLine(AutoAdv, string.Format(".\\Capítulo {0}.html", Next));

                Generator.WriteLine();
                foreach (string Line in File.ReadAllLines(FileList)) {
                    if (string.IsNullOrWhiteSpace(Line))
                        continue;
                    Generator.WriteLine(PageMask, CapName + "\\" + Line);
                }
                if (Next != null) {
                    string NextName = string.Format("Capítulo {0}.html", Next);
                    Generator.WriteLine(ChapMask, NextName);
                }
                Generator.WriteLine(SufixMask);
                Generator.Flush();
            }
        }       
        

        internal static bool CanSort(List<string> Entries) {
            int TMP;
            foreach (string Entry in Entries)
                if (!int.TryParse(GetFileName(Entry), out TMP))
                    return false;
            return true;
        }

        internal static string GetFileName(string Link) {
            return Path.GetFileNameWithoutExtension(Link).Trim(' ', '(', ')', '[', ']');
        }
        internal static string Download(string URL, Encoding CodePage) {
            return CodePage.GetString(Download(URL));
        }
        internal static void Download(string URL, string SaveAs) {
            byte[] Content = Download(URL);
            System.IO.File.WriteAllBytes(SaveAs, Content);
        }
        internal static byte[] Download(string URL) {
            MemoryStream MEM = new MemoryStream();
            Download(URL, MEM);
            byte[] DATA = MEM.ToArray();
            MEM.Close();
            return DATA;
        }
        
        internal static void Download(string URL, Stream Output, int tries = 4) {
            try {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);
                //Bypass a fucking bug in the fucking .net framework
                if (Request.Address.AbsoluteUri != URL && tries <= 2) {
                    /*
                    WebClient WC = new WebClient();
                    WC.QueryString.Add("action", "shorturl");
                    WC.QueryString.Add("format", "simple");
                    WC.QueryString.Add("url", URL);
                    URL = WC.DownloadString("https://u.nu/api.php");*/

                    Request = (HttpWebRequest)WebRequest.Create("http://proxy-it.nordvpn.com/browse.php?u=" + URL);
                    Request.Referer = "http://proxy-it.nordvpn.com";
                }

                Request.UseDefaultCredentials = true;
                Request.Method = "GET";
                WebResponse Response = Request.GetResponse();
                byte[] FC = new byte[0];
                using (Stream Reader = Response.GetResponseStream()) {
                    byte[] Buffer = new byte[1024];
                    int bytesRead;
                    do {
                        Application.DoEvents();
                        bytesRead = Reader.Read(Buffer, 0, Buffer.Length);
                        Output.Write(Buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
            }
            catch (Exception ex){
                if (tries < 0)
                    throw new Exception(string.Format("Connection Error: {0}", ex.Message));

                Thread.Sleep(1000);
                Download(URL, Output, tries-1);
            }
        }

        internal static string[] GetElementsByClassName(string HTML, int BeginIndex, params string[] Class) {
            if (Class == null || Class.Length == 0)
                return new string[0];

            List<string> Tags = new List<string>();
            string[] Elements = GetElements(HTML, BeginIndex);
            foreach (string Element in Elements) {
                if (!Element.ToLower().Contains("class="))
                    continue;
                string[] Names = GetElementTag(Element, "class").Split(' ');
                bool Equal = EqualsArray(Class, Names);
                if (Equal)
                    Tags.Add(Element);
            }
            return Tags.ToArray();
        }

        internal static string[] GetElementsByTag(string HTML, string Tag, string Content, bool StartsWithOnly = false, bool ContainsOnly = false) {
            List<string> Tags = new List<string>();
            string[] Elements = GetElements(HTML);
            foreach (string Element in Elements) {
                if (!Element.ToLower().Contains(Tag.ToLower()))
                    continue;
                string ETAG = GetElementTag(Element, Tag);
                if ((StartsWithOnly ? ETAG.StartsWith(Content) : ETAG == Content) || (ContainsOnly && Element.Contains(Content)))
                    Tags.Add(Element);
            }
            return Tags.ToArray();
        }
        internal static bool EqualsArray(string[] Class, string[] Names) {
            if (Class.Length != Names.Length)
                return false;
            for (int i = 0; i < Class.Length; i++)
                if (Class[i].ToLower() != Names[i].ToLower())
                    return false;
            return true;
        }

        internal static string GetElementTag(string Element, string Name) {
            if (!Name.EndsWith("="))
                Name += '=';
            int Index = Element.ToLower().IndexOf(Name);
            if (Index < 0)
                return string.Empty;
            char Quote = '\x0';
                while (Quote != '\'' && Quote != '"' && Index < Element.Length)
                    Quote = Element[Index++];
            string Tag = string.Empty;
            while (Index < Element.Length) {
                if (Element[Index] == Quote)
                    break;
                else
                    Tag += Element[Index++];
            }
            return Tag;
        }

        internal static string[] GetElements(string HTML, int StartOfIndex = 0) {
            HtmlAgilityPack.HtmlDocument Document = new HtmlAgilityPack.HtmlDocument();
            Document.LoadHtml(HTML.Substring(StartOfIndex));
            List<string> Elements = new List<string>();
            foreach (HtmlNode Node in Document.DocumentNode.DescendantsAndSelf()) {
                if (Node.Descendants().Count() > 2)
                    continue;
                string SHTML = Node.OuterHtml;
                if (string.IsNullOrWhiteSpace(SHTML.Trim(' ', '\t', '\n', '\r')))
                    continue;
                Elements.Add(SHTML);
            }
            return Elements.ToArray();
        }
        

        private void OnMouseEnter(object sender, EventArgs e) {
            ButtonLst.Focus();
        }
        
        private void SelDirBnt_Click(object sender, EventArgs e) {
            FolderBrowserDialog BD = new FolderBrowserDialog();
            if (BD.ShowDialog() == DialogResult.OK)
                TBSaveAs.Text = BD.SelectedPath;
        }

        private void UpDot_Tick(object sender, EventArgs e) {
            switch (Status.ToLower()) {
                case "aguardando link...":
                    Status = "Aguardando Link   ";
                    break;
                case "aguardando link   ":
                    Status = "Aguardando Link.  ";
                    break;
                case "aguardando link.  ":
                    Status = "Aguardando Link.. ";
                    break;
                case "aguardando link.. ":
                    Status = "Aguardando Link...";
                    break;
            }
        }

        private void OnClosing(object sender, FormClosingEventArgs e) {
            Environment.Exit(0);
        }

        private void OnShowing(object sender, EventArgs e) {
            string bak = Text;
            Text = "";
            Application.DoEvents();
            Text = bak;

            Visible = false;
            Visible = true;
        }

        private void OnFocused(object sender, EventArgs e) {
            string Def = "Cole seu link aqui";
            if (tbNovelLink.Text == Def)
                tbNovelLink.Text = "";
        }

        private void BntDumpText_Click(object sender, EventArgs e) {
            tbNovelLink.Enabled = false;
            GroupConfig.Enabled = false;
#if !DEBUG
            try {
#endif
            if (FolderPicker.ShowDialog() != DialogResult.OK) {
                GroupConfig.Enabled = true;
                tbNovelLink.Enabled = true;

                return;
            }

            BntListLink_Click(null, null);
            ProgressBar.Maximum = ListView.Items.Count;

            string Dir = FolderPicker.SelectedPath;
            if (!Dir.EndsWith("\\"))
                Dir += "\\";
            for (int i = 0; i < ListView.Items.Count; i++) {
                Application.DoEvents();
                string Cap = ListView.Items[i].SubItems[0].Text;
                string Link = ListView.Items[i].SubItems[1].Text;
                string Text = DumpNovel(Link);

                if (string.IsNullOrEmpty(Text)) {
                    MessageBox.Show("Failed to dump the text, check your configs.", "Novel Dumper", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto Abort;
                }

                if (AutoTl.Checked)
                    Text = Translate(Text);
                File.WriteAllText(Dir + string.Format("{0}.txt", Cap), Text, Encoding.UTF8);
                ProgressBar.Value = (int)(((double)ProgressBar.Maximum*i)/100);
            }
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 100;
#if !DEBUG
            } catch (Exception ex) {
                MessageBox.Show("Falha ao baixar os capítulos, Razão:\n" + ex.Message);
            }
#endif
            Abort:;
            GroupConfig.Enabled = true;
            tbNovelLink.Enabled = true;
        }

        private string Translate(string Text) {
            string[] Lines = Text.Replace("\r\n", "\n").Split('\n');
            string[] Result = TranslateAsync(Lines);
            string Novel = string.Empty;
            foreach (string str in Result)
                Novel += str + "\r\n";
            return Novel.Trim('\r', '\n', ' ');
        }

        private string DumpNovel(string Url) {
            HtmlAgilityPack.HtmlDocument Document = new HtmlAgilityPack.HtmlDocument();
            string HTML = Download(Url, Encoding.UTF8);
            Document.LoadHtml(HTML);
            HtmlNode[] Nodes;
            if (RadioTagId.Checked) {
                Nodes = new HtmlNode[] { Document.GetElementbyId(HtmlFilter.Text) };
            } else {
                Nodes = GetTagsWithClass(Document, new List<string>(HtmlFilter.Text.Split(' '))).ToArray();
            }

            List<string> BlackList = new List<string>() {
                "script", "style"
            };
            string ResultNovel = string.Empty;
            string Novel = string.Empty;
            foreach (HtmlNode Node in Nodes) {
                bool Breaked = false;
                uint Spans = 0;
                foreach (HtmlNode SubNode in Node.DescendantsAndSelf()) {
                    if (SubNode.NodeType == HtmlNodeType.Text) {
                        try {
                            if (BlackList.Contains(SubNode.Name.ToLower()) || BlackList.Contains(SubNode.ParentNode.Name.ToLower())) {
                                Spans = 0;
                                continue;
                            }
                        } catch { }

                        if (SubNode.ParentNode.Name.ToLower().Trim() == "span" && Spans > 0 && MergeSpan.Checked) {
                            string TMP = Novel.TrimEnd('\r', '\n', ' ');
                            if (!(TMP.EndsWith(".") || TMP.EndsWith("\"") || TMP.EndsWith("”") || TMP.EndsWith(")")))
                                Novel = TMP + " ";

                        } 

                        string Line = SubNode.InnerText.Trim();
                        if (Line != "") {
                            Novel += Line + "\r\n";
                            Breaked = false;
                        } else if (!Breaked) {
                            Novel += "\r\n";
                            Breaked = true;
                        }
                    } else if (!Breaked) {
                        Novel += "\r\n";
                        Breaked = true;
                    }
                    if (Spans > 3) {
                        Spans = 0;
                        continue;
                    }
                    if (SubNode.ParentNode.Name.ToLower().Trim() == "span")
                        Spans++;
                }

                if (ResultNovel != string.Empty) {
                    if (ResultNovel.Contains(Novel)) {
                        Novel = ResultNovel;
                        continue;
                    }
                }
                ResultNovel = Novel;
            }

            string Result = Novel + " ";
            while (Result != Novel) {
                Novel = Result;
                Result = HttpUtility.HtmlDecode(Result);
            }

            return Result.Trim('\r', '\n', ' ');
        }
        public static List<HtmlNode> GetTagsWithClass(HtmlAgilityPack.HtmlDocument Document, List<string> Class) {        
            var Result = (from x in Document.DocumentNode.Descendants()
                          where x.Attributes.Contains("class") && //Include only elements with class 
                         
                          //And
                          ((from s in Class
                            where x.Attributes["class"].Value.Split(' ').Contains(s) //Include only element contains any tag quoted in the Class list
                            select s).Count() > 0)

                          select x);
            return Result.ToList();
        }

        public static string[] ExtractHtmlLinks(string Html, string Domain) {
            if (!Domain.StartsWith("http"))
                Domain = "http://" + Domain;
            if (Domain.EndsWith("//"))
                Domain = Domain.Substring(0, Domain.Length - 1);

            List<string> Links = new List<string>();
            int Index = 1;
            while ((Index = Html.IndexOf("http", Index)) > 0) {
                char End = Html[Index - 1];
                if (End != '\'' && End != '"')
                    continue;

                string Link = string.Empty;
                while (Index < Html.Length && Html[Index] != End)
                    Link += Html[Index++];

                if (Index >= Html.Length)
                    break;

                string Result = Link;

                while (!string.IsNullOrEmpty(Result)) {
                    Link = Result;
                    Result = HttpUtility.UrlDecode(Result);

                    if (Result == Link)
                        break;
                }

                if (!Link.Contains(Result))
                    Links.Add(Result);
            }

            Links.AddRange(ExtractTagLinks(Html, Domain, "value"));
            Links.AddRange(ExtractTagLinks(Html, Domain, "src"));
            Links.AddRange(ExtractTagLinks(Html, Domain, "href"));


            return Links.Distinct().ToArray();
        }

        private static string[] ExtractTagLinks(string HTML, string Domain, string Tag) {
            Tag = Tag.Trim('\'', '"');

            if (!Tag.EndsWith("="))
                Tag += "=";

            List<string> Links = new List<string>();
            int Index = 0;
            while ((Index = HTML.IndexOf(Tag, Index)) > 0) {
                Index += Tag.Length;
                char End = HTML[Index++];
                if (End != '\'' && End != '"')
                    continue;

                string Link = string.Empty;
                while (Index < HTML.Length && HTML[Index] != End)
                    Link += HTML[Index++];

                if (Index >= HTML.Length)
                    break;

                string Result = Link;
                if (Link.StartsWith("//"))
                    Result = "http:" + Link;
                else if (!Link.ToLower().StartsWith("http"))
                    Result = Domain + '/' + Link.TrimStart('/');

                while (!string.IsNullOrEmpty(Result)) {
                    Link = Result;
                    Result = HttpUtility.UrlDecode(Result);

                    if (Result == Link)
                        break;
                }

                if (!Links.Contains(Link))
                    Links.Add(Link);
            }

            return Links.ToArray();
        }

        public List<uint> Depth = new List<uint>();
        private void NovelTextChanged(object sender, EventArgs e) {
            string Link = tbNovelLink.Text;
            if (Link.StartsWith("http") && Link.Contains(".") && Uri.IsWellFormedUriString(Link, UriKind.Absolute)) {
                Depth = new List<uint>();
                Linkfilter.Text = Link + "*";
                if (!Linkfilter.Text.EndsWith("/"))
                    Linkfilter.Text += "/";
                SiteConfig Config = Database.Search(Link);
                if (!string.IsNullOrWhiteSpace(Config.Filter)) {
                    HtmlFilter.Text = Config.Filter;
                    RadioTagClass.Checked = Config.FilterMode != Mode.ID;
                    RadioTagId.Checked = Config.FilterMode == Mode.ID;
                }
            }

        }

        internal static string GetRawNameFromUrlFolder(string Folder, bool DeupperlizerOnly = false) {
            string Name = DeupperlizerOnly ? Folder.ToLower() : Folder.Replace("-", " ").Replace("_", " ");
            bool Spaced = true;
            string ResultName = string.Empty;
            foreach (char c in Name) {
                if (c == ' ') {
                    Spaced = true;
                    ResultName += ' ';
                    continue;
                }
                if (Spaced) {
                    ResultName += c.ToString().ToUpper();
                    Spaced = false;
                } else
                    ResultName += c;
            }

            return ResultName;
        }

        private void BntListLink_Click(object sender, EventArgs e) {
            try {
                string Domain = new Uri(tbNovelLink.Text).Host;
                string MainHtml = Download(tbNovelLink.Text, Encoding.UTF8);

                if (Clipboard.GetText().ToLower().Contains("html")) {
                    if (MessageBox.Show("Processar HTML da área de transferência?", "MangaUnhost", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        MainHtml = Clipboard.GetText();
                }

                string[] Links = ExtractHtmlLinks(MainHtml, Domain);
                Links = (from x in Links
                         where LikeOperator.LikeString(x, Linkfilter.Text, CompareMethod.Text)
                         select x).ToArray();

                List<string> Included = new List<string>();
                int Count = 0;
                ListView.Items.Clear();
                foreach (string Link in Links) {
                    if (Included.Contains(Link))
                        continue;
                    Included.Add(Link);
                    ListView.Items.Add(new ListViewItem(new string[] { string.Format("Unk-{0}", ++Count), Link }));
                }
                LoadBlackList();
                LoadDeph();
            } catch (Exception ex) {
                if (sender == null)
                    throw ex;

                MessageBox.Show("Falha ao se conectar com o site.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void RenameCaps() {
            for (int i = 0; i < ListView.Items.Count; i++) {
                string Name = "c" + (i + BaseCap.Value);
                ListView.Items[i].SubItems[0].Text = Name;
            }
        }

        List<string> BlackList = new List<string>();

        private void LoadBlackList() {
            for (int i = 0; i < ListView.Items.Count; i++) {
                if (BlackList.Contains(ListView.Items[i].SubItems[1].Text))
                    ListView.Items[i--].Remove();
            }
            RenameCaps();
        }

        private void LoadDeph() {
            if (Depth.Count == 0)
                return;
            for (int i = 0; i < ListView.Items.Count; i++) {
                string Link = ListView.Items[i].SubItems[1].Text;
                if (!Depth.Contains(GetDepth(Link)))
                    ListView.Items.RemoveAt(i--);
            }
            RenameCaps();
        }

        private void DelItemsClicked(object sender, EventArgs e) {
            if (MessageBox.Show("Tem certeza que deseja remover os itens selecionados da lista?", "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                return;

            foreach (int I in ListView.SelectedIndices.Cast<int>()) {
                BlackList.Add(ListView.Items[I].SubItems[1].Text);
            }

            LoadBlackList();
        }

        private void ForceDephClicked(object sender, EventArgs e) {
            if (MessageBox.Show("Tem certeza que deseja limitar a profundidade do caminho apenas as dos items atualmente selecionados na lista?", "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                return;

            foreach (int I in ListView.SelectedIndices.Cast<int>()) {
                Depth.Add(GetDepth(ListView.Items[I].SubItems[1].Text));
            }
            LoadDeph();
        }

        private uint GetDepth(string URL) {
            URL = URL.ToLower();
            if (URL.StartsWith("http://"))
                URL = URL.Substring(7, URL.Length - 7);
            else if (URL.StartsWith("https://"))
                URL = URL.Substring(8, URL.Length - 8);

            return (uint)URL.Split('?')[0].Trim('/').Split('/').LongLength;
        }

        private string[] TranslateAsync(string[] Input, string SourceLang = "auto", string TargetLang = null, bool Fast = false) {
            int Tries = 0;

            if (TargetLang == null)
                TargetLang = TlLang.Text;

            again:;
            string[] Rst = null;
            DateTime Begin = DateTime.Now;
            bool Running = true;
            Thread TL = new Thread(() => {
                try {
                    if (Tries == 1) {
                        Rst = new string[Input.Length];
                        for (uint i = 0; i < Rst.Length; i++) {
                            if (string.IsNullOrWhiteSpace(Input[i]))
                                Rst[i] = Input[i];
                            else
                                Rst[i] = Google.Translate(Input[i], SourceLang, TargetLang);
                        }
                    } else if (Tries == 2) {
                        Rst = Bing.Translate(Input, SourceLang, TargetLang);
                    } else
                        Rst = Google.Translate(Input, SourceLang, TargetLang);
                } catch { }
                Running = false;
            });
            TL.Start();
            while (Running && (DateTime.Now - Begin).TotalSeconds < 200) {
                Application.DoEvents();
                Thread.Sleep(10);
            }
            if (Rst == null) {
                TL.Abort();
                if (Tries++ < 4) {
                    goto again;
                }
            }
            return Rst;
        }

        private void CaptureClipboardChanged(object sender, EventArgs e) {
            CheckUrl.Enabled = ckCaptureClipboard.Checked;
        }
        
    }
}
