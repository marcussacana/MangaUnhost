using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost {
    public enum ContentType {
        Novel, Comic
    }

    public enum SaveAs : int {
        PNG, JPG, BMP, RAW, AUTO
    }

    public enum CaptchaSolverType {
        /// <summary>
        /// Don't try automatically solve the Captcha
        /// </summary>
        Manual,
        /// <summary>
        /// Try automatically solve the Captcha
        /// </summary>
        SemiAuto
    }
}
