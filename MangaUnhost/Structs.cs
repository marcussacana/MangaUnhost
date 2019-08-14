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
}
