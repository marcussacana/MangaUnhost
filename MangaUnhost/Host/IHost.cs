using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost.Host {
    public interface IHost {
        /// <summary>
        /// Verifiy if the give URL is supported by the plugin
        /// </summary>
        /// <param name="URL">URL to Verify</param>
        /// <returns>If is supported, returns true, if not, returns false</returns>
        bool IsValidLink(string URL);

        /// <summary>
        /// Get Initial Information about the Given Manga
        /// </summary>
        /// <param name="URL">Manga Main Page URL</param>
        /// <param name="Name">Temporary Manga Name</param>
        /// <param name="Page">Chapter List URL</param>
        void Initialize(string URL, out string Name, out string Page);

        /// <summary>
        /// Get the Poster Picture URL to the Given manga url;
        /// </summary>
        /// <param name="Page">The Manga URL</param>
        /// <returns>The Poster URL</returns>
        string GetPosterUrl();

        /// <summary>
        /// Get a temporary Manga name using the URL
        /// </summary>
        /// <param name="CodedName">URL with Manga Name</param>
        /// <returns>The Temporary Name</returns>
        string GetName(string CodedName);

        /// <summary>
        /// Get the Real Manga name by the HTML content of the Page
        /// </summary>
        /// <param name="Page">URL to the Page</param>
        /// <returns>Manga Name</returns>
        string GetFullName();

        /// <summary>
        /// Get all Pages of a Chapter
        /// </summary>
        /// <param name="HTML">HTML content of the chapter</param>
        /// <returns>Url Array of all Pages of the chapter</returns>
        string[] GetChapterPages(string HTML);

        /// <summary>
        /// Enum All Chapters
        /// </summary>
        /// <param name="HTML">HTML of the TOC</param>
        /// <returns>URL Array of all Chapters</returns>
        string[] GetChapters();


        /// <summary>
        /// Get the Chapter name using the Chapter URL
        /// </summary>
        /// <param name="ChapterURL"></param>
        /// <returns></returns>
        string GetChapterName(string ChapterURL);


        /// <summary>
        /// Preload table of content data
        /// </summary>
        /// <param name="URL"></param>
        void LoadPage(string URL);

        /// <summary>
        /// Plugin Name
        /// </summary>
        string HostName { get; }

        /// <summary>
        /// Valid Demo URL
        /// </summary>
        string DemoUrl { get; }
    }
}
