using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace MangaUnhost.Host
{
    class Mangadex : IHost
    {
        string IniPath = AppDomain.CurrentDomain.BaseDirectory + "MangaDex.ini";
        public Mangadex()
        {
            if (!System.IO.File.Exists(IniPath))
                Ini.SetConfig("MangaDex", "Language", "Ask", IniPath);
        }

        public string HostName => "MangaDex";

        public string DemoUrl => "https://mangadex.org/title/32944/i-am-a-child-of-this-house";

        public bool NeedsProxy => false;

        public CookieContainer Cookies => BrowserCookies.ToContainer();

        public string UserAgent => UA;

        public string Referrer => null;

        public bool SelfChapterDownload => true;

        public string GetChapterName(string ChapterURL)
        {
            return NameMap[ChapterURL];
        }

        public string[] GetChapterPages(string URL)
        {
            string ID = URL.Substring("/chapter/");

            string API = $"https://mangadex.org/api/?id={ID}&type=chapter&baseURL=%2Fapi";

            try
            {
                string Response = Main.Download(API, Encoding.UTF8, Tries: int.MinValue, Cookies: Cookies, Referrer: "https://mangadex.org", UserAgent: UA);

                var Result = Extensions.JsonDecode<MangaDexApi>(Response);


                if (Result.status == "delayed")
                    return null;

                if (Result.status != "OK")
                    throw new Exception();



                if (!Result.server.ToLower().Contains(".mangadex.org"))
                    Result.server = "https://mangadex.org" + Result.server;

                List<string> Pages = new List<string>();
                foreach (string Page in Result.page_array)
                {
                    Pages.Add($"{Result.server}{Result.hash}/{Page}");
                }

                return Pages.ToArray();
            }
            catch
            {

                return null;
            }
        }

        public string[] GetChapters()
        {
            List<string> Links = new List<string>();
            foreach (string HTML in HTMLs)
            {
                string[] Chapters = GetChapters(HTML, LID);
                string[] Names = GetChaptersName(HTML, LID);

                if (Names.Length != Chapters.Length)
                    throw new Exception();

                Links.AddRange(Chapters);
                NameMap.AddRange(Chapters, Names);
            }

            return Links.ToArray();
        }

        Dictionary<string, string> NameMap = new Dictionary<string, string>();

        public string GetFullName()
        {
            string HTML = HTMLs.First();

            string Name = HTML.Substring("<span class=\"mx-1\">", "</span>");

            return HttpUtility.HtmlDecode(Name);
        }

        public string GetName(string CodedName)
        {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl()
        {
            string HTML = HTMLs.First();
            HTML = HTML.Substring("title=\"See covers\">", "</a>");
            return Main.ExtractHtmlLinks(HTML, "mangadex.org").First();
           // return $"https://mangadex.org/images/manga/{ID}.jpg";
        }

        public void Initialize(string URL, out string Name, out string Page)
        {
            if (!IsValidLink(URL))
                throw new Exception();

            string Path = URL.Substring("/title/");

            if (Path.Contains("/"))
            {
                Title = Path.Split('/')[1];
                ID = Path.Split('/')[0];
            }
            else
            {
                Title = "Manga";
                ID = Path;
            }

            Page = $"https://mangadex.org/title/{ID}/{Title}";
            Name = Main.GetRawNameFromUrlFolder(Title);
        }

        public bool IsValidLink(string URL)
        {
            //https://mangadex.org/title/32944/i-am-a-child-of-this-house
            return URL.ToLower().Contains("mangadex.org/title/") && !URL.ToLower().EndsWith("/title/");
        }

        public void LoadPage(string URL)
        {
            string PageMask = "https://mangadex.org/title/{0}/{1}/chapters/{2}";

            var CFData = Main.BypassCloudflare(string.Format(PageMask, ID, Title, "1"));
            BrowserCookies = CFData.AllCookies;
            UA = CFData.UserAgent;


            if (System.IO.File.Exists(IniPath))
                LID = Ini.GetConfig("MangaDex", "Language", IniPath);
            else
                LID = null;

            Dictionary<string, string> LangMap = new Dictionary<string, string>();
            List<string> Pages = new List<string>();
            int PageNum = 1;
            while (true)
            {
                string Page = Main.Download(string.Format(PageMask, ID, Title, PageNum++), Encoding.UTF8, Cookies: Cookies, UserAgent: UA);
                if (GetChapters(Page, LID).Length == 0)
                {
                    if (Pages.Count == 0 && LID != "Ask")
                    {
                        LID = "Ask";
                        PageNum--;
                        continue;
                    }
                    break;
                }

                EnumLanguages(Page, LangMap);
                Pages.Add(Page);
            }

            if (LID.Trim().ToLower() == "ask" && LangMap.Count > 1)
                Main.Instance.Invoke(new MethodInvoker(() =>
                {
                    Form Window = new Form();
                    Window.Size = new Size(300, 130);
                    Window.TopMost = true;
                    Window.StartPosition = FormStartPosition.CenterParent;

                    iTalk_ThemeContainer Container = new iTalk_ThemeContainer();
                    Container.Parent = Window;
                    Container.Text = "Select a Language...";
                    Container.Sizable = false;

                    iTalk_ComboBox Combo = new iTalk_ComboBox();
                    Combo.Size = new Size(260, 80);
                    Combo.Location = new Point(10, 30);
                    Combo.Items.AddRange(LangMap.Keys.ToArray());
                    Combo.Parent = Container;
                    Combo.DropDownStyle = ComboBoxStyle.DropDownList;
                    Combo.SelectedValueChanged += (a, b) => Window.Close();

                    while (string.IsNullOrWhiteSpace(Combo.Text))
                        Window.ShowDialog(Main.Instance);

                    LID = LangMap[Combo.Text];
                }));
            else if (LID.Trim().ToLower() == "ask")
                LID = LangMap.Values.First(); 


            HTMLs = Pages;
        }

        public bool ValidateProxy(string Proxy)
        {
            throw new NotImplementedException();
        }

        private void EnumLanguages(string HTML, Dictionary<string, string> Dictionary)
        {
            string[] Elms = GetChapterElms(HTML);

            foreach (string Elm in Elms)
            {
                if (!Elm.Contains("data-id"))
                    continue;

                string ELang = Main.GetElementAttribute(Elm, "data-lang");

                string sHTML = HTML.Substring($"data-lang=\"{ELang}\"", "user_level_guest").Substring("</div>");

                var Span = Main.GetElementsByClasses(sHTML, "rounded", "flag", "*").First();

                string LangName = Main.GetElementAttribute(Span, "title");

                Dictionary[LangName] = ELang;
            }
        }

        private string[] GetChapters(string HTML, string Lang = null)
        {
            string[] Elms = GetChapterElms(HTML);
            List<string> Links = new List<string>();
            foreach (string Elm in Elms)
            {
                if (!Elm.Contains("data-id"))
                    continue;

                if ((Lang != null && Lang != "Ask") && Main.GetElementAttribute(Elm, "data-lang") != Lang)
                    continue;


                Links.Add($"https://mangadex.org/chapter/{Main.GetElementAttribute(Elm, "data-id")}");
            }

            return Links.ToArray();
        }

        private string LastHTML;
        private string[] LastElms;
        private string[] GetChapterElms(string HTML)
        {
            if (HTML == LastHTML)
                return LastElms;

            LastHTML = HTML;
            LastElms = Main.GetElementsByClasses(HTML.Substring("chapter-container", "homepage_settings_modal"), "chapter-row", "d-flex", "row", "no-gutters", "p-2", "align-items-center", "border-bottom", "odd-row");

            return LastElms;
        }
        private string[] GetChaptersName(string HTML, string Lang = null)
        {
            string[] Elms = GetChapterElms(HTML);

            List<string> Names = new List<string>();
            foreach (string Elm in Elms)
            {
                if (!Elm.Contains("data-chapter"))
                    continue;

                if ((Lang != null && Lang != "Ask") && Main.GetElementAttribute(Elm, "data-lang") != Lang)
                    continue;

                Names.Add(Main.GetElementAttribute(Elm, "data-chapter"));
            }

            return Names.ToArray();
        }

        struct MangaDexApi
        {
            public int? id;
            public long? timestamp;
            public string hash;
            public string volume;
            public string chapter;
            public string title;
            public string lang_name;
            public string lang_code;
            public int? manga_id;
            public int? group_id;
            public int? group_id_1;
            public int? group_id_2;
            public int? group_id_3;
            public int? comments;
            public string server;
            public string[] page_array;
            public int? long_strip;
            public string status;
        }

        string LID = null;

        string Title;
        string ID;

        string UA;

        List<string> HTMLs = new List<string>();

        Cookie[] BrowserCookies;
    }
}
