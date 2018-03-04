using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MangaUnhost {
    struct SiteConfig {
        public string Domain;
        public string HTML;

        public string Filter;
        public Mode FilterMode;
    }
    enum Mode {
        ID = 0, Class = 1
    }

    static class Database {

        private static SiteConfig[] Sites = new SiteConfig[] {
            new SiteConfig() {
                Domain = ".wordpress.com",
                HTML = "wordpress",
                Filter = "entry-content post-content content",
                FilterMode = Mode.Class
            },
            new SiteConfig() {
                Domain = ".blogspot.com",
                HTML = "blogspot",
                Filter = "post-body",
                FilterMode = Mode.Class
            }
        };

        internal static SiteConfig Search(string URL) {
            string TMP = URL.ToLower();
            bool Found = ((from x in Sites where TMP.Contains(x.Domain) select x).Count() != 0);
            if (Found) {
                return (from x in Sites where TMP.Contains(x.Domain) select x).FirstOrDefault();
            }

            TMP = Main.Download(URL, Encoding.UTF8).ToLower();
            Found = ((from x in Sites where TMP.Contains(x.HTML) select x).Count() != 0);
            if (Found) {
                return (from x in Sites where TMP.Contains(x.HTML) select x).FirstOrDefault();
            }

            return new SiteConfig();
        }
        
    }
}
