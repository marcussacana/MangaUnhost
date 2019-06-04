﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MangaUnhost.Host {
    class UnionMangas : IHost {
        string HTML;

        public bool NeedsProxy => false;

        public string HostName => "Union Mangás";

        public string DemoUrl => "http://unionmangas.net/manga/karakai-jouzu-no-takagi-san";

        public CookieContainer Cookies => cks.ToContainer();

        public string UserAgent => UA;

        public string Referrer => null;

        public bool SelfChapterDownload => false;

        private string UA;
        private Cookie[] cks;

        public string GetChapterName(string ChapterURL) {
            //unionmangas.cc/leitor/Karakai_Jouzu_no_Takagi-san/01
            const string Prefix = "/leitor/";
            int Index = ChapterURL.IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            return ChapterURL.Substring(Index).Split('/')[1];
        }

        public string[] GetChapterPages(string HTML) {
            int Index = HTML.IndexOf("img-responsive img-manga pag_");
            while (Index > 0 && HTML[Index] != '<')
                Index--;

            string[] Pages = (from x in Main.ExtractHtmlLinks(HTML.Substring(Index), "unionmangas.cc") where x.Contains("/leitor/") && x.Contains("/mangas/") select x.Replace(" ", "%20")).Distinct().ToArray();

            return Pages;
        }

        public string[] GetChapters() {
            int Index = HTML.IndexOf("<div class=\"row lancamento-linha\">", 0);
            int EndIndex = HTML.IndexOf("<div class=\"row\">", Index);

            string[] Links = (from x in Main.ExtractHtmlLinks(HTML.Substring(Index, EndIndex - Index), "unionmangas.cc") where x.Contains("/leitor/") select x).Distinct().ToArray();
            return Links;
        }

        public string GetFullName() {
            string Title = Main.GetElementsByContent(HTML, "<title>").First();

            const string Sufix = " - Union Mangás";
            Title = Title.Between('>', '<');
            Title = Title.Substring(0, Title.Length - Sufix.Length);
            return Title;
        }

        public string GetName(string CodedName) {
            return Main.GetRawNameFromUrlFolder(CodedName);
        }

        public string GetPosterUrl() {
            int Index = HTML.IndexOf("col-md-4 col-xs-12 text-center col-md-perfil");
            string Poster = Main.ExtractHtmlLinks(HTML.Substring(Index), "unionmangas.cc").First();
            return Poster;
        }

        public void Initialize(string URL, out string Name, out string Page) {
            if (!IsValidLink(URL))
                throw new Exception();

            const string Prefix = "/manga/";
            int Index = URL.ToLower().IndexOf(Prefix);
            if (Index < 0)
                throw new Exception();
            Index += Prefix.Length;

            Name = GetName(URL.Substring(Index).Split('/')[0]);
            Page = URL.Split('?')[0];
        }

        public bool IsValidLink(string URL) {
            URL = URL.ToLower();
            return Uri.IsWellFormedUriString(URL, UriKind.Absolute) && URL.Contains("unionmangas") && URL.Contains("/manga/");
        }

        public void LoadPage(string URL) {
            HTML = Main.Download(URL, Encoding.UTF8);
            while (HTML.IsCloudflareTriggered())
            {
                var Byp = Main.BypassCloudflare(URL);
                cks = Byp.AllCookies;
                UA = Byp.UserAgent;
                HTML = Byp.HTML;
            }
        }

        public bool ValidateProxy(string Proxy) {
            throw new NotImplementedException();
        }
    }
}
