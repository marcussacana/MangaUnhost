using System;
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
using System.Collections;

namespace MangaUnhost {
    public partial class Main : Form {

        public static Main Instance = null;

        Host.IHost[] Hosts = (from Asm in AppDomain.CurrentDomain.GetAssemblies()
                              from Typ in Asm.GetTypes()
                              where typeof(Host.IHost).IsAssignableFrom(Typ) && !Typ.IsInterface
                              select (Host.IHost)Activator.CreateInstance(Typ)).ToArray();

        static Host.IHost AtualHost = null;
        
        public Main() {
            InitializeComponent();

            Instance = this;

            string Title = "MangaUnhost - " + (Program.Debug ? "[DEBUG]" : $"v{GitHub.CurrentVersion}");
            Text = Title;
            iTalk_ThemeContainer1.Text = Title;
            TBSaveAs.Text = AppDomain.CurrentDomain.BaseDirectory + "Biblioteca";
            
            foreach (var Host in Hosts) {
                SupportList.Items.Add(Host.HostName);
            }

            if (!Program.Debug) {
                BntTestHosts.Visible = false;
                bntTestTrim.Visible = false;
                bntGenReader.Visible = false;
            }

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)0x00000FF0;

            Tools.OnLoadProxies += (a, b) => {
                if (LastStatus != null)
                    return;

                LastStatus = Status;
                Status = "Obtendo Proxies...";
            };
            Tools.OnProxiesLoaded += (a, b) => {
                if (LastStatus == null)
                    return;

                Status = LastStatus;
                LastStatus = null;
            };

            new Thread(CropWorker).Start();
        }
        

        private void CheckUrl_Tick(object sender, EventArgs e) {
            try {
                string ClipboardContent = Clipboard.GetText();

                if (string.IsNullOrEmpty(ClipboardContent))
                    return;

                foreach (var Host in Hosts)
                    if (Host.IsValidLink(ClipboardContent)) {
                        Clipboard.Clear();

                        AtualHost = Host;
                        new Thread(() => {
                            try {
                                string Name, URL;//Fuck you AppVeyor
                                AtualHost.Initialize(ClipboardContent, out Name, out URL);
                                ShowManga(Name, URL);
                            } catch { Status = "Aguardando Link..."; }
                        }).Start();
                        break;
                    }
            } catch { }
        }

        delegate void Invoker();
        public string Status {
            get {
                return StatusLBL.Text;
            }
            set {
                Invoke(new Invoker(() => { StatusLBL.Text = value; }));
            }
        }
        private string LastStatus = null;

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
                Download(value, MEM, UserAgent: AtualHost.UserAgent, Cookies: AtualHost.Cookies, Referrer: AtualHost.Referrer);
                Invoker Delegate = new Invoker(() => {
                    Poster.Image = Image.FromStream(MEM);
                    MEM.Close();
                });
                Invoke(Delegate);
            }
        }

        string OpenMangaUrl = null;
        private void ShowManga(string Name, string Page) {
            Status = "Connecting...";

            OpenMangaUrl = Page;
            AtualHost.LoadPage(Page);
            Name = HttpUtility.HtmlDecode(AtualHost.GetFullName());

            Status = "Downloading Info of the " + Name;
            PictureURL = AtualHost.GetPosterUrl();

            Invoke(new Invoker(ButtonLst.Controls.Clear));
            Invoke(new Invoker(() => { MainPanel.Visible = true; }));

            Title = Name;
            string[] Chapters = AtualHost.GetChapters();
            Actions = new List<Action>();

            for (int i = 0; i < Chapters.Length; i ++)
                Invoke(new Invoker(() => {
                    Control Button = RegisterChapter(Chapters[i], i - 1 >= 0 ? Chapters[i-1] : null);
                    ButtonLst.Controls.Add(Button);
                }));

            if (Chapters.Length > 1)
                Invoke(new Invoker(() => {
                    Control Button = RegisterGetAll();
                    ButtonLst.Controls.Add(Button);
                }));

            Status = "Waiting Url...";
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
            string ID = AtualHost.GetChapterName(Chapter).TrimStart(' ', '0'), ID2 = null;

            if (NextChapter != null) {
                ID2 = AtualHost.GetChapterName(NextChapter).TrimStart(' ', '0');
            }


            string Text = "Chapter " + ID;
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
            Button.Text = "Download All";
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
            string ID = AtualHost.GetChapterName(URL).Trim().TrimStart('0');
            string NID = Next == null ? null : AtualHost.GetChapterName(Next).Trim().TrimStart('0');
            if (string.IsNullOrWhiteSpace(ID))
                ID = "0";
            if (string.IsNullOrWhiteSpace(NID))
                NID = "0";


            Status = string.Format("Downloading Info of the Chapter {0}...", ID);
            string Manga = URL;

            char[] Removes = Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()).ToArray();
            string Validated = string.Empty;
            foreach (char c in Title)
                if (!Removes.Contains(c))
                    Validated += c;
                else
                    Validated += '-';

            string CapDir = TBSaveAs.Text + $"\\{Validated}\\";
            string WorkDir = $"{CapDir}Capítulos\\Capítulo {ID}\\";            
            string CapName = $"Capítulo {ID}";
            string BookPath = CapDir + "Capítulos\\" + CapName + ".html";

            if ((File.Exists(CapDir + CapName + ".html") || File.Exists(BookPath)) && ckResume.Checked) {
                Status = "Waiting Url...";
                return;
            }

            string[] Pages;
            if (AtualHost.SelfChapterDownload) {
                Pages = AtualHost.GetChapterPages(Manga);
            } else {
                string HTML = Download(Manga, Encoding.UTF8, UserAgent: AtualHost.UserAgent, Cookies: AtualHost.Cookies, Referrer: AtualHost.Referrer);
                Pages = AtualHost.GetChapterPages(HTML);
            }

            if (Pages == null)
            {
                Status = "Waiting Url...";
                return;
            }

            int Pag = 0;

            if (!Directory.Exists(WorkDir)) {
                Directory.CreateDirectory(WorkDir);
            }

            if (File.Exists(CapDir + "Online.url"))
                File.Delete(CapDir + "Online.url");

            if (File.Exists(WorkDir + "Pages.lst"))
                File.Delete(WorkDir + "Pages.lst");

            if (!File.Exists(CapDir + "Cover.png"))
                DownloadImage(AtualHost.GetPosterUrl(), CapDir + "Cover.png");

            File.WriteAllText(CapDir + "Online.url", string.Format(Properties.Resources.UrlFile, OpenMangaUrl));
            TextWriter FileList = File.CreateText(WorkDir + "Pages.lst");
            foreach (string Page in Pages) {
                Application.DoEvents();
                Status = string.Format("Downloading Chapter {2} ({0}/{1})...", Pag++, Pages.Length, ID);
                string SaveAs = WorkDir + Pag.ToString("D3") + Path.GetExtension(Page.Split('?')[0]);
                if (string.IsNullOrEmpty(Path.GetExtension(SaveAs)))
                    SaveAs += ".png";

                SaveAs = DownloadImage(Page, SaveAs);

                FileList.WriteLine(SaveAs.Substring(WorkDir.Length, SaveAs.Length - WorkDir.Length));
            }
            FileList.Close();

            if (ckGenReader.Checked) {
                GenerateBook(WorkDir, CapDir, NID, CapName, BookPath);

                if (NID != null)
                    AppendIndex(CapDir, CapName);
            }

            Status = "Waiting Url...";
        }

        private string DownloadImage(string Url, string SaveAs) {
            DateTime? LastModify = null;

            if (Path.GetExtension(Url).ToLower().EndsWith(".webp")) {
                SaveAs = Path.GetDirectoryName(SaveAs) + "\\" + Path.GetFileNameWithoutExtension(SaveAs) + ".png";
                if (File.Exists(SaveAs))
                    LastModify = new FileInfo(SaveAs).LastWriteTimeUtc;

                MemoryStream MEM = new MemoryStream();
                bool Rst = Download(Url, MEM, UserAgent: AtualHost.UserAgent, Cookies: AtualHost.Cookies, Referrer: AtualHost.Referrer, LastModify: LastModify);
                if (LastModify.HasValue && !Rst)
                    return SaveAs;

                MEM.Seek(0, SeekOrigin.Begin);
                Bitmap PageTexture = DecodeWebP(MEM);
                PageTexture.Save(SaveAs, System.Drawing.Imaging.ImageFormat.Png);

                new Thread(() => {
                    ValidateWebP(SaveAs, PageTexture, MEM.ToArray());
                    CropFile(SaveAs);
                }).Start();

                MEM.Close();
            } else {
                if (File.Exists(SaveAs))
                    LastModify = new FileInfo(SaveAs).LastWriteTimeUtc;

                Download(Url, SaveAs, UserAgent: AtualHost.UserAgent, Cookies: AtualHost.Cookies, Referrer: AtualHost.Referrer, LastModify: LastModify);
                CropFile(SaveAs);
            }
            return SaveAs;
        }

        Queue<string> CropQueue = new Queue<string>();

        private object CropLocker = new object();
        private void CropFile(string ImagePath) {
            if (InvokeRequired) {
                Invoke(new MethodInvoker(() => CropFile(ImagePath)));
                return;
            }
            if (!ckCropSpace.Checked)
                return;

            CropQueue.Enqueue(ImagePath);
        }

        bool Exited = false;

        private void CropWorker() {
            while (!Exited) {
                while (CropQueue.Count == 0)
                    Thread.Sleep(500);

                string ImagePath = CropQueue.Dequeue();

                int Tries = 1;
                Retry:;
                try {
                    BitmapTrim Cropper;
                    Bitmap Result;
                    int Height;
                    using (Bitmap Source = Image.FromFile(ImagePath) as Bitmap) {
                        Height = Source.Height;
                        using (Cropper = new BitmapTrim(Source)) {
                            Cropper.BufferLenght /= Tries;
                            Result = Cropper.Trim();
                            Source.Dispose();
                            Cropper.Dispose();
                        }
                    }

                    using (Cropper = new BitmapTrim(Result)) {
                        Cropper.BufferLenght /= Tries;
                        Result = Cropper.Trim(false);

                        if (Height == Result.Height) {
                            Result.Dispose();
                            continue;
                        }

                        Result.Save(ImagePath);
                        Result.Dispose();
                    }
                } catch {
                    if (Tries++ <= 3)
                        goto Retry;
                }
            }
        }

        private void AppendIndex(string CapDir, string CapName) {
            string IndexPath = CapDir + "Índice.html";
            if (!File.Exists(IndexPath)) {
                const string Prefix = "<DOCTYPE HTML>\r\n<html><meta charset=\"utf-8\">\r\n<head>\r\n<title>Índice de Capítulos</title>\r\n<style>body{background-color: #000000;}</style>\r\n</head>\r\n<body>\r\n<div align=\"center\">\r\n<img src=\"Cover.png\" style=\"max-width:100%;\"/><br/>\r\n";
                File.WriteAllText(IndexPath, Prefix, Encoding.UTF8);
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
            New += $"\r\n<a href=\".\\Capítulos\\{CapName}.html\" style=\"color: #FFF;\">{CapName}</a></br>";
            New += "\r\n</div>\r\n</body>\r\n</html>";

            File.WriteAllText(IndexPath, New, Encoding.UTF8);
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

        private string GenerateBook(string MangDir, string CapsDir, string Next, string CapName, string HtmlFile = null) {
            const string PrefixMask = "<DOCTYPE HTML>\r\n<html>\r\n<head><meta charset=\"utf-8\">\r\n<title>{0} - HTML Reader</title>";
            const string PrefixPart2 = "\r\n<style>body{background-color: #000000;}</style>\r\n</head>\r\n<body>\r\n<div align=\"center\">";
            const string PageMask = "<img src=\"{0}\" style=\"max-width:100%;\"/><br/>";
            const string ChapMask = "<a href=\".\\{0}\" style=\"color: #FFF;\">Next Chapter</a>";
            const string SufixMask = "</div>\r\n</body>\r\n</html>";
            Status = "Gerando Leitor...";
            string FileList = MangDir + "Pages.lst";

            if (HtmlFile == null)
                HtmlFile = CapsDir + CapName + ".html";

            if (File.Exists(HtmlFile))
                File.Delete(HtmlFile);

            using (TextWriter Generator = File.CreateText(HtmlFile)) {
                Generator.WriteLine(PrefixMask, CapName);
                Generator.WriteLine(PrefixPart2);

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

            return HtmlFile;
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
        internal static string Download(string URL, Encoding CodePage, int Tries = 4, bool AllowRedirect = true, string UserAgent = null, string Referrer = null, CookieContainer Cookies = null, DateTime? LastModify = null) {
            byte[] Data = Download(URL, Tries, AllowRedirect, UserAgent: UserAgent, Referrer: Referrer, Cookies: Cookies, LastModify: LastModify);
            if (LastModify.HasValue && Data.Length == 0)
                return string.Empty;

            return CodePage.GetString(Data);
        }
        internal static void Download(string URL, string SaveAs, int Tries = 4, bool AllowRedirect = true, string UserAgent = null, string Referrer = null, CookieContainer Cookies = null, DateTime? LastModify = null) {
            byte[] Content = Download(URL, Tries, AllowRedirect, UserAgent: UserAgent, Referrer: Referrer, Cookies: Cookies, LastModify: LastModify);
            if (LastModify.HasValue && Content.Length == 0)
                return;

            File.WriteAllBytes(SaveAs, Content);
        }
        internal static byte[] Download(string URL, int tries = 4, bool AllowRedirect = true, string UserAgent = null, string Referrer = null, CookieContainer Cookies = null, DateTime? LastModify = null) {
            MemoryStream MEM = new MemoryStream();
            bool Rst = Download(URL, MEM, tries, ThrownRedirect: !AllowRedirect, UserAgent: UserAgent, Referrer: Referrer, Cookies: Cookies, LastModify: LastModify);
            if (LastModify.HasValue && !Rst)
                return new byte[0];

            byte[] DATA = MEM.ToArray();
            MEM.Close();
            return DATA;
        }
        
        internal static bool Download(string URL, Stream Output, int tries = 4, bool ProxyChanged = false, bool ThrownRedirect = false, string UserAgent = null, string Referrer = null, CookieContainer Cookies = null, DateTime? LastModify = null) {
            string CurrentProxy = null;
            try {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(URL);
                if (Request.Address.AbsoluteUri.Trim('\\', '/') != URL.Trim('\\', '/') && tries <= 2) {
                    if (System.Diagnostics.Debugger.IsAttached)
                        MessageBox.Show($"ERROR\nABURI: {Request.Address.AbsoluteUri}\n\nURL: {URL}", "DEBUG MESSAGE");
                }

                if (AtualHost?.NeedsProxy == true && tries > 1) {
                    do {
                        if (CurrentProxy != null)
                            Tools.BlackListProxy(CurrentProxy);                        

                        CurrentProxy = Tools.Proxy;
                    } while (!AtualHost.ValidateProxy(CurrentProxy));

                    Request.Proxy = new WebProxy(CurrentProxy);
                }

                if (Cookies != null)
                    Request.CookieContainer = Cookies;

                if (UserAgent == null)
                    Request.UserAgent = Tools.UserAgent;
                else
                    Request.UserAgent = UserAgent; 

                if (LastModify != null)
                    Request.IfModifiedSince = LastModify.Value;

                if (Referrer != null)
                    Request.Referer = Referrer;

                Request.UseDefaultCredentials = true;
                Request.Method = "GET";
                Request.Timeout = (15 + (10 * (4 - tries))) * 1000;
                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

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

                if (Response.ResponseUri != Request.RequestUri && ThrownRedirect && tries > 0) 
                    throw new Exception();
                

                if (Response.StatusCode != HttpStatusCode.OK && tries > 0)
                    throw new Exception();

                if (CurrentProxy != null)
                    Tools.WorkingProxy = CurrentProxy;

                return true;
            } catch (Exception ex) {
                try {
                    if (ex is WebException && ((HttpWebResponse)((WebException)ex).Response).StatusCode == HttpStatusCode.NotModified)
                        return false;
                } catch { }

                if (tries < 0) {
                    if (DialogResult.Yes == MessageBox.Show(string.Format("Connection Error: {0}\nIgnore?", ex.Message), "MangaUnhost", MessageBoxButtons.YesNo, MessageBoxIcon.Error))
                        return true;
                    else
                        throw ex;
                }
                if (tries - 1 < 0 && !ProxyChanged && AtualHost?.NeedsProxy == true) {
                    Tools.RefreshProxy();
                    ProxyChanged = true;
                    tries = 4;
                }

                if (CurrentProxy != null)
                    Tools.BlackListProxy(CurrentProxy);

                Thread.Sleep(1000);
                
                return Download(URL, Output, tries - 1, ProxyChanged, ThrownRedirect, UserAgent, Referrer, Cookies);
            }

        }

        internal static string[] GetElementsByClasses(string HTML, params string[] Class) => GetElementsByClasses(HTML, false, 0, Class);
        internal static string[] GetElementsByClasses(string HTML, int BeginIndex, params string[] Class) => GetElementsByClasses(HTML, false, BeginIndex, Class);
        internal static string[] GetElementsByClasses(string HTML, bool Full, params string[] Class) => GetElementsByClasses(HTML, Full, 0, Class);
        internal static string[] GetElementsByClasses(string HTML, bool Full, int BeginIndex, params string[] Class) {
            if (Class == null || Class.Length == 0)
                return new string[0];

            List<string> Tags = new List<string>();
            string[] Elements = GetElements(HTML, BeginIndex, true);
            for (int i = 0; i < Elements.Length; i++) {
                if (!Elements[i].StartsWith("<") || Elements[i].StartsWith("</"))
                    continue;

                string Element = Full ? GetElementFullContent(Elements, i) : GetElementContent(Elements, i);

                if (!Element.ToLower().Contains("class"))
                    continue;

                string[] Names = GetElementAttribute(Element, "class").Split(' ');
                bool Equal = EqualsArray(Class, Names);
                if (Equal) 
                    Tags.Add(Element);
                
            }
            return Tags.ToArray();
        }

        internal static string[] GetElementsByAttribute(string HTML, string Attribute, string Value, bool StartsWithOnly = false, bool ContainsOnly = false, bool Full = false, int StartIndex = 0) {
            List<string> Tags = new List<string>();
            string[] Elements = GetElements(HTML, StartIndex, true);
            for (int i = 0; i < Elements.Length; i++) {
                if (!Elements[i].StartsWith("<") || Elements[i].StartsWith("</"))
                    continue;
                string Element = Full ? GetElementFullContent(Elements, i) : GetElementContent(Elements, i);
                if (!Element.ToLower().Contains(Attribute.ToLower()))
                    continue;

                string Attrib = GetElementAttribute(Element, Attribute);
                if ((StartsWithOnly ? Attrib.StartsWith(Value) : Attrib == Value) || ContainsOnly && Attrib.Contains(Value))
                    Tags.Add(Element);
            }
            return Tags.ToArray();
        }

        internal static string[] GetElementsByContent(string HTML, string Content, int StartIndex = 0, bool SkipJavascript = true, bool Full = false) {
            List<string> Elms = new List<string>();

            HTML = HTML.Substring(StartIndex);

            if (SkipJavascript) {
                int Index;
                while ((Index = HTML.ToLower().IndexOf("<script")) > 0) {
                    int EndInd = HTML.ToLower().IndexOf("</script>", Index) + "</script>".Length;
                    HTML = HTML.Substring(0, Index) + HTML.Substring(EndInd, HTML.Length - EndInd);
                }
            }

            string[] Elements = GetElements(HTML, 0, true);
            string LastValid = string.Empty;
            for (int i = 0; i < Elements.Length; i++) {
                if (!Elements[i].StartsWith("<") || Elements[i].StartsWith("</"))
                    continue;
                string Elm = Full ? GetElementFullContent(Elements, i) : GetElementContent(Elements, i);
                if (Elm.Contains(Content)) {
                    LastValid = Elm;
                } else if (!string.IsNullOrWhiteSpace(LastValid)) {
                    Elms.Add(LastValid);
                    LastValid = string.Empty;
                }
            }

            return Elms.ToArray();
        }

        internal static string GetElementContent(string[] Elements, int ElementIndex) {
            if (ElementIndex >= Elements.Length)
                return string.Empty;

            string Element = string.Empty;
            int TargetLevel = GetChildLevel(Elements, ElementIndex);

            if (!Elements[ElementIndex].StartsWith("<") && !Elements[ElementIndex].StartsWith("</")) {
                int i = ElementIndex;
                while (i >= 0 && !Elements[i].StartsWith("<") && !Elements[i].StartsWith("</")) {
                    i--;
                }

                if (GetChildLevel(Elements, i) == TargetLevel)
                    ElementIndex = i;
            }
            
            for (int x = ElementIndex; x < Elements.Length; x++) {
                Element += Elements[x];
                int Level = GetChildLevel(Elements, x);
                if (Level == TargetLevel && x != ElementIndex) {
                    break;
                }
                if (x + 1 < Elements.Length) {
                    int NxLevel = GetChildLevel(Elements, x + 1);
                    if (NxLevel > TargetLevel && Elements[x + 1].StartsWith("</")) {
                        Element += Elements[x + 1];
                        break;
                    }
                }
            }

            return Element;
        }

        internal static string GetElementFullContent(string[] Elements, int ElementIndex) {
            if (ElementIndex >= Elements.Length)
                return string.Empty;

            string Element = string.Empty;
            int TargetLevel = GetChildLevel(Elements, ElementIndex);

            if (!Elements[ElementIndex].StartsWith("<") && !Elements[ElementIndex].StartsWith("</")) {
                int i = ElementIndex;
                while (i >= 0 && !Elements[i].StartsWith("<") && !Elements[i].StartsWith("</")) {
                    i--;
                }

                if (GetChildLevel(Elements, i) == TargetLevel)
                    ElementIndex = i;
            }

            string Tag = Elements[ElementIndex].Split(' ')[0].Trim('>', '<').ToLower();
            for (int x = ElementIndex, d = 0; x < Elements.Length; x++) {
                if (Elements[x].ToLower().StartsWith($"<{Tag}"))
                    d++;
                Element += Elements[x];
                int Level = GetChildLevel(Elements, x);
                if (Level == TargetLevel && x != ElementIndex) {
                    break;
                }
                if (x + 1 < Elements.Length) {
                    int NxLevel = GetChildLevel(Elements, x + 1);
                    if (NxLevel > TargetLevel && Elements[x + 1].StartsWith("</")) {
                        if (Elements[x + 1].ToLower() == $"</{Tag}>") {
                            d--;
                            if (d == 0) {
                                Element += Elements[x + 1];
                                break;
                            }
                        }
                    }
                }
            }

            return Element;
        }

        //Optmization
        static int LastLevel = 0, LastLevelSearch = 0;
        internal static int GetChildLevel(string[] Elements, int ElementIndex) {
            int Level = 0;
            if (ElementIndex >= Elements.Length)
                return -1;

            int BeginInd = 0;
            if (LastLevelSearch < ElementIndex) {
                Level = LastLevel;
                BeginInd = LastLevelSearch;
            }            

            for (int i = BeginInd; i < ElementIndex; i++) {
                string Element = Elements[i];
                bool HasChild = Element.StartsWith("<") && !Element.StartsWith("</") && !Element.EndsWith("/>");
                if (HasChild)
                    Level++;
                bool CloseChild = Element.StartsWith("</");
                if (CloseChild)
                    Level--;
            }

            LastLevel = Level;
            LastLevelSearch = ElementIndex;

            return Level;
        }
        internal static bool EqualsArray(string[] Class, string[] Names) {
            if (Class.Length != Names.Length)
                return false;
            for (int i = 0; i < Class.Length; i++)
                if (Class[i].ToLower() != Names[i].ToLower())
                    return false;
            return true;
        }

        internal static string GetElementAttribute(string Element, string AttributeName) {
            if (!AttributeName.EndsWith("="))
                AttributeName += '=';
            int Index = Element.ToLower().IndexOf(AttributeName.ToLower());
            if (Index < 0)
                return string.Empty;
            if (Element.IndexOf(">") < Index)
                return string.Empty;

            char Quote = '\x0';
                while (Quote != '\'' && Quote != '"' && Index < Element.Length)
                    Quote = Element[Index++];
            string Value = string.Empty;
            while (Index < Element.Length) {
                if (Element[Index] == Quote)
                    break;
                else
                    Value += Element[Index++];
            }
            return Value;
        }

        internal static string[] GetElements(string HTML, int StartOfIndex = 0, bool NoLimit = false) {
            HtmlAgilityPack.HtmlDocument Document = new HtmlAgilityPack.HtmlDocument();
            Document.LoadHtml(HTML.Substring(StartOfIndex));
            List<string> Elements = new List<string>();
            TraceChilds(Document.DocumentNode, ref Elements, NoLimit);
            return (from x in Elements where !string.IsNullOrWhiteSpace(x) select x).ToArray();
        }

        private static void TraceChilds(HtmlNode Node, ref List<string> Elements, bool NoLimit) {
            try {
                if (Node.Descendants().Count() > 2 && !NoLimit)
                    return;
                string SHTML = Node.OuterHtml;
                if (string.IsNullOrWhiteSpace(SHTML.Trim(' ', '\t', '\n', '\r')))
                    return;

                if (Node.HasChildNodes) {
                    int ContentBegin = Node.OuterHtml.IndexOf(Node.InnerHtml);
                    string Open = Node.OuterHtml.Substring(0, ContentBegin).Trim();
                    int ClosePos = ContentBegin + Node.InnerHtml.Length;
                    string Close;
                    if (ClosePos >= Node.OuterHtml.Length)
                        Close = "";
                    else
                        Close = Node.OuterHtml.Substring(ClosePos).Trim();
                    Elements.Add(Open);
                    foreach (HtmlNode Child in Node.ChildNodes)
                        TraceChilds(Child, ref Elements, NoLimit);
                    Elements.Add(Close);
                } else Elements.Add(SHTML);
            } catch (Exception ex){

            }
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
            if (Status.ToLower().StartsWith("waiting url") && CropQueue.Count > 0)
                Status = "Cropping Pages...";
            else if (Status.ToLower().StartsWith("cropping pages") && CropQueue.Count == 0)
                Status = "Waiting Url...";
            

            if (Status.EndsWith("..."))
                Status = Status.Substring(0, Status.Length - 3) + "   ";
            else if (Status.EndsWith(".. "))
                Status = Status.Substring(0, Status.Length - 3) + "...";
            else if (Status.EndsWith(".  "))
                Status = Status.Substring(0, Status.Length - 3) + ".. ";
            else if (Status.EndsWith("   "))
                Status = Status.Substring(0, Status.Length - 3) + ".  ";
        }

        private void OnClosing(object sender, FormClosingEventArgs e) {
            if (CropQueue.Count > 0) {
                bool Interrupt = MessageBox.Show("The MangaUnhost still cutting the new pages, do you want to interrupt?", "MangaUnhost", MessageBoxButtons.YesNo) == DialogResult.Yes;

                if (!Interrupt) {
                    e.Cancel = true;
                    return;
                }
            }
            Exited = true;
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
            string Def = "Paste your Url Here";
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
            //ProgressBar.Maximum = ListView.Items.Count;

            string Dir = FolderPicker.SelectedPath;
            if (!Dir.EndsWith("\\"))
                Dir += "\\";
            for (int i = 0; i < ListView.Items.Count; i++) {
                Application.DoEvents();
                string Cap = ListView.Items[i].SubItems[0].Text;
                string Link = ListView.Items[i].SubItems[1].Text;
                string[] Images;
                string Text = DumpNovel(Link, out Images);

                if (string.IsNullOrEmpty(Text)) {
                    MessageBox.Show("Failed to dump the text, check your configs.", "Novel Dumper", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    goto Abort;
                }

                if (AutoTl.Checked)
                    Text = Translate(Text);

                Text += "\r\nFrom: " + Link;

                int Pic = 0;
                File.WriteAllText(Dir + string.Format("{0}.txt", Cap), Text, Encoding.UTF8);
                if (cbSaveImages.Checked)
                    foreach (string Image in Images) {
                        try {
                            Download(Image, Dir + string.Format("{0}-{1}.png", Cap, ++Pic));
                        } catch { }
                    }
                ProgressBar.Value = (int)(((double)i / ListView.Items.Count) * 100);
#if !DEBUG
            } catch (Exception ex) {
                MessageBox.Show("Falha ao baixar os capítulos, Razão:\n" + ex.Message);
            }
#else
            }
#endif
            ProgressBar.Maximum = 100;
            ProgressBar.Value = 100;

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

        private string DumpNovel(string Url, out string[] Images) {
            HtmlAgilityPack.HtmlDocument Document = new HtmlAgilityPack.HtmlDocument();
            string HTML = Download(Url, Encoding.UTF8);
            Document.LoadHtml(HTML);
            HtmlNode[] Nodes;
            if (RadioTagId.Checked) {
                Nodes = new HtmlNode[] { Document.GetElementbyId(HtmlFilter.Text) };
            } else {
                Nodes = GetTagsWithClass(Document, new List<string>(HtmlFilter.Text.Split(' '))).ToArray();
            }
            List<string> Pics = new List<string>();
            List<string> BlackList = new List<string>() {
                "script", "style"
            };
            string ResultNovel = string.Empty;
            string Novel = string.Empty;
            foreach (HtmlNode Node in Nodes) {
                bool Breaked = false;
                uint Spans = 0;
                foreach (HtmlNode SubNode in Node.DescendantsAndSelf()) {
                    if (SubNode.NodeType == HtmlNodeType.Element && SubNode.Name.ToLower() == "img") {
                        for (int i = 0; i < SubNode.Attributes.Count; i++) {
                            var Element = SubNode.Attributes.ElementAt(i);
                            if (Element.Name.ToLower() != "src")
                                continue;

                            Pics.Add(Element.Value);
                        }
                    }

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
            Images = Pics.ToArray();
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

        public static string[] ExtractHtmlLinks(string Html, string Domain, string Attribute = null) {
            if (Domain != null) {
                if (!Domain.StartsWith("http"))
                    Domain = "http://" + Domain;
                if (Domain.EndsWith("//"))
                    Domain = Domain.Substring(0, Domain.Length - 1);
            }

            List<string> Links = new List<string>();
            int Index = 0;
            while ((Index = Html.IndexOf("http", ++Index)) > 0) {
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

            if (Attribute != null)
                Links.AddRange(ExtractTagLinks(Html, Domain, Attribute));

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

            return (from x in Links select HttpUtility.HtmlDecode(x)).ToArray();
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
                    if (MessageBox.Show("Process the HTML in the clipboard?", "MangaUnhost", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
            if (MessageBox.Show("Are you sure to remove all entries of the list?", "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                return;

            foreach (int I in ListView.SelectedIndices.Cast<int>()) {
                BlackList.Add(ListView.Items[I].SubItems[1].Text);
            }

            LoadBlackList();
        }

        private void ForceDephClicked(object sender, EventArgs e) {
            if (MessageBox.Show("Are you sure you want to limit the depth of the path only to the currently selected items in the list?", "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
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

        private void BntTestHosts_Click(object sender, EventArgs e) {
            SupportList.Items.Clear();
            foreach (var Host in Hosts) {
                string Demo = Host.DemoUrl;
                string Name, ChapterName = string.Empty, ChapterUrl = string.Empty;

                int Loop = 0;
                bool Atention = false;
                bool Online = true;
                bool Sucess = false;
                while (Online && !Sucess) {
                    Application.DoEvents();
                    try {
                        switch (Loop++) {
                            case 0:
                                Online = Host.IsValidLink(Demo);
                                break;
                            case 1:
                                Host.Initialize(Demo, out Name, out Demo);
                                Atention |= SuspectBadName(Name);
                                break;
                            case 2:
                                Host.LoadPage(Demo);
                                break;
                            case 3:
                                Online = Uri.IsWellFormedUriString(Host.GetPosterUrl(), UriKind.Absolute);
                                break;
                            case 4:
                                ChapterUrl = Host.GetChapters().First();
                                if (!Uri.IsWellFormedUriString(ChapterUrl, UriKind.Absolute))
                                    Online = false;
                                break;
                            case 5:
                                ChapterName = Host.GetChapterName(ChapterUrl);
                                Atention |= SuspectBadName(ChapterName);
                                break;
                            case 6:
                                string[] Pages = Host.GetChapterPages(Download(ChapterUrl, Encoding.UTF8, UserAgent: Host.UserAgent, Cookies: Host.Cookies, Referrer: AtualHost.Referrer));
                                foreach (string Page in Pages) {
                                    if (string.IsNullOrEmpty(Page) || !Uri.IsWellFormedUriString(Page, UriKind.Absolute))
                                        Online = false;
                                    string FN = Page.Split('?')[0].Trim().ToLower();
                                    if (FN.EndsWith(".png") || FN.EndsWith(".jpg") || FN.EndsWith(".bmp") || FN.EndsWith(".webp") || FN.EndsWith(".tiff"))
                                        continue;

                                    Atention |= true;
                                }
                                break;

                            default:
                                Sucess = true;
                                break;
                        }
                    } catch { Online = false; }
                }
                Loop--;

                SupportList.Items.Add(string.Format("Online: {1} | Atention: {2} | LastStep: {3} | Host: ({0})", Host.HostName, Online ? "True " : "False" , Atention ? "True " : "False", Loop));
            }
            MessageBox.Show("Host test cleared", "MangaUnhost Debugger");
        }
        

        private static bool SuspectBadName(string Name) {
            if (Name.Contains("(") && Name.Contains(","))
                return true;
            if (Name.Length > 50)
                return true;

            return false;
        }


        delegate CloudflareData BypassCloudflareDel(string URL, int Tries = 3);

        private void bntTestTrim_Click(object sender, EventArgs e) {
            OpenFileDialog FD = new OpenFileDialog();
            FD.Filter = "All Image Files|*.jpg;*.png;*.jpeg;*.bmp";
            if (FD.ShowDialog() != DialogResult.OK)
                return;

            string Out = Path.GetDirectoryName(FD.FileName) + "\\" + Path.GetFileNameWithoutExtension(FD.FileName) + "-trim" + Path.GetExtension(FD.FileName);

            using (Bitmap Original = Image.FromFile(FD.FileName) as Bitmap) {
                var Tool = new BitmapTrim(Original);
                var Bitmap = Tool.Trim(true);
                Tool = new BitmapTrim(Bitmap);
                Bitmap = Tool.Trim(false);

                Bitmap.Save(Out);

                MessageBox.Show("Saved");
            }
        }

        private void bntGenReader_Click(object sender, EventArgs e) {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.SelectedPath = TBSaveAs.Text;
            if (fb.ShowDialog() != DialogResult.OK)
                return;

            string[] PageMaps= Directory.GetFiles(fb.SelectedPath, "Pages.lst", SearchOption.AllDirectories);
            foreach (string Map in PageMaps)
                LinkPages(Map);

            MessageBox.Show("Finished.");
        }

        private void LinkPages(string PageListPath) {
            string Dir = Path.GetDirectoryName(PageListPath) + "\\";
            string[] Pages = File.ReadAllLines(PageListPath, Encoding.UTF8);
            Pages = Pages.Where(x => !string.IsNullOrWhiteSpace(x) && File.Exists(Dir + x)).ToArray();
            for (int i = 0; i < Pages.Length; i++) {
                 string Current = "" + Pages[i];
                string Next = i + 1 < Pages.Length ? "" + Path.GetFileNameWithoutExtension(Pages[i + 1]) + ".html" : "";
                string Out = Dir + Path.GetFileNameWithoutExtension(Pages[i]) + ".html";

                const string HTML = "<DOCTYPE HTML>\r\n<html>\r\n<meta charset=\"utf-8\">\r\n<head>\r\n<title>HTML Reader</title>\r\n<style>body{{background-color: #000000;}}</style>\r\n</head>\r\n<body>\r\n<div align=\"center\">\r\n<a href=\"{0}\"><img src=\"{1}\" style=\"max-width:100%;\"/></a>\r\n</div>\r\n</body>\r\n</html>";
                File.WriteAllText(Out, string.Format(HTML, Next, Current));
            }

        }

        public static CloudflareData BypassCloudflare(string URL, int Tries = 3) {
            if (Instance.InvokeRequired)
                return (CloudflareData)Instance.Invoke(new BypassCloudflareDel(BypassCloudflare), URL, Tries - 1);

            string Status = Instance.Status;
            Instance.Status = "Bypassing Cloudflare...";
            

            var Browser = new WebBrowser() {
                ScriptErrorsSuppressed = true
            };

            Browser.Navigate(URL);
            Browser.WaitForLoad();
            bool Checking = Browser.DocumentText.Contains("5 seconds...") || Browser.DocumentText.Contains("Checking your browser");
            if (Checking)
            {
                Browser.WaitForRedirect();
                Browser.WaitForLoad();
            }

            bool Fail = Browser.DocumentText.Contains("5 seconds...") || Browser.DocumentText.Contains("Checking your browser");

            Instance.Status = Status;

            if (Fail && Tries <= 0)
                throw new Exception("Failed to Bypass the Anti-Bot");

            var Bypass = new CloudflareData() {
                UserAgent = (string)Browser.InjectAndRunScript("return clientInformation.userAgent;"),
                Cookie = (from x in Browser.GetCookies() where x.Name == "cf_clearance" select x).Single(),
                AllCookies = Browser.GetCookies()
            };

            Bypass.Cookie.Domain = new Uri(URL).Host;

            return Bypass;
        }

        
    }

    public struct CloudflareData {
        public string UserAgent;
        public Cookie Cookie;

        public Cookie[] AllCookies;
    }
}
