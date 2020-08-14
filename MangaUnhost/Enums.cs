using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost
{
    public enum ContentType
    {
        Novel, Comic
    }

    public enum SaveAs : int
    {
        PNG, JPG, BMP, RAW, AUTO
    }

    public enum ReplaceMode : int
    {
        UpdateURL = 1,
        NewFolder = 2,
        Ask = 3
    }

    public enum ReaderMode : int
    {
        Legacy, Manga, Comic, Other
    }

    public enum CaptchaSolverType
    {
        /// <summary>
        /// Don't try automatically solve the Captcha
        /// </summary>
        Manual,
        /// <summary>
        /// Try automatically solve the Captcha
        /// </summary>
        SemiAuto
    }

    [Flags]
    public enum ActionTo
    {
        About = 0b0001,
        ChapterList = 0b0010
    }
}
