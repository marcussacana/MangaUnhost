using System;
using System.Collections.Generic;
using System.Drawing;

namespace MangaUnhost {
    public interface IDecoder {

        /// <summary>
        /// The Bitmap Decoder
        /// </summary>
        /// <param name="Data">The raw data</param>
        /// <returns>The decoded Bitmap</returns>
        Bitmap Decode(byte[] Data);

    }

    public interface IHost {

        /// <summary>
        /// Check if the URI is valid to the current plugin
        /// </summary>
        /// <param name="Uri">The URI to check</param>
        bool IsValidUri(Uri Uri);

        /// <summary>
        /// Load a URI 
        /// </summary>
        /// <param name="Uri">The URI to load</param>
        /// <returns>The info of the given commic</returns>
        ComicInfo LoadUri(Uri Uri);

        /// <summary>
        /// Get the <see cref="ContentType.Comic"/> page count
        /// </summary>
        /// <param name="ID">The Chapter ID</param>
        /// <returns>The Page Count of the given chapter</returns>

        int GetChapterPageCount(int ID);

        /// <summary>
        /// Get <see cref="ContentType.Comic"/> page decoder
        /// </summary>
        /// <returns>The Decoder</returns>

        IDecoder GetDecoder();

        /// <summary>
        /// Enumerate all chapters
        /// </summary>
        /// <returns>A KeyValuePair of the chapter id and name</returns>

        IEnumerable<KeyValuePair<int, string>> EnumChapters();

        /// <summary>
        /// Download a <see cref="ContentType.Comic"/> pages as raw pages byte data
        /// </summary>
        /// <param name="ID">The Chapter ID</param>
        /// <returns>The Raw Chapter Data</returns>

        IEnumerable<byte[]> DownloadPages(int ID);

        /// <summary>
        /// Download a <see cref="ContentType.Novel"/> chapter as HTML string
        /// </summary>
        /// <param name="ID">The Chapter ID</param>
        /// <returns>The Chapter HTML</returns>
        string DownloadChapter(int ID);

        /// <summary>
        /// Get the basic plugin info
        /// </summary>
        /// <returns>The Plugin Info</returns>
        PluginInfo GetPluginInfo();
    }

    public interface ILanguage {
        string LanguageName { get; }
        string DownloaderTab { get; }
        string SettingsTab { get; }
        string AboutTab { get; }

        string EnvironmentBox { get; }
        string FeaturesBox { get; }
        string SupportedHostsBox { get; }

        string LibraryLbl { get; }
        string LanguageLbl { get; }
        string CaptchaSolvingLbl { get; }
        string ImageClippingLbl { get; }
        string ReaderGeneratorLbl { get; }
        string ClipboardWatcherLbl { get; }
        string SaveAsLbl { get; }
        string SkipDownloadedLbl { get; }

        string Enabled { get; }
        string Disabled { get; }
        string Manual { get; }
        string SemiAuto { get; }

        string NextChapter { get; }
        string Chapters { get; }
        string ChapterName { get; }
        string DownloadAll { get; }

        string Loading { get; }
        string Downloading { get; }
        string LoadingComic { get; }
        string IDLE { get; }
        string BypassingCloudFlare { get; }
        string ClippingImages { get; }

        string Library { get; }
        string Cover { get; }
        string Index { get; }

        //Plugin Info Preview
        string AuthorLbl { get; }
        string PluginLbl { get; }
        string SupportComicLbl { get; }
        string SupportNovelLbl { get; }
        string VersionLbl { get; }

        //Comic Preview
        string NewChapters { get; }
        string OpenSite { get; }
        string Download { get; }

        string Yes { get; }
        string No { get; }

        string UpdateFound { get; }

    }
}
