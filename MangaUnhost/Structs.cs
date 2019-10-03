using HtmlAgilityPack;
using System;
using System.Net;

namespace MangaUnhost {
    public struct Page {
        public byte[] Data;
        public IDecoder Decoder;
    }

    public struct ComicInfo {
        public byte[] Cover;
        public string Title;
        public ContentType ContentType;

        public Uri Url;
    }

    public struct CloudflareData {
        public CookieContainer Cookies;
        public string UserAgent;
        public string HTML;
    }

    public struct PluginInfo {
        public string Name;

        public string Author;
        public Version Version;

        public bool SupportComic;
        public bool SupportNovel;

        public CustomAction[] Actions;
    }

    public struct CustomAction
    {
        public string Name;
        public bool Debug;
        public bool AutoRun;
        public Action Action;
    }

    public struct Account
    {
        public string Login;
        public string Password;

        public string Email;
    }

    public struct Settings {
        public string LibraryPath;
        public string Language;

        public bool AutoCaptcha;
        public bool ImageClipping;
        public bool ReaderGenerator;
        public bool SkipDownloaded;

        public int SaveAs;
    }


    public struct NovelChapter
    {
        public string Title;
        public string Author;
        public string URL;
        public HtmlDocument HTML;
        //public NovelScript[] Scripts;
        //public NovelResource[] Resources;
    }

    public struct NovelScript
    {
        public string FileName;
        public string Mime;
        public string Script;
    }

    public struct NovelResource
    {
        public string FileName;
        public string Mime;
        public byte[] Data;
    }
}
