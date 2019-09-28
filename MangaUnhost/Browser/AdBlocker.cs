using CefSharp.OffScreen;
using CefSharp.RequestEventHandler;
using DistillNET;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Browser
{
    public static class AdBlocker
    {
        static FilterDbCollection Filters;

        static AdBlocker()
        {
            try
            {
                var parser = new AbpFormatRuleParser();

                var Filter = "https://easylist.to/easylist/easylist.txt".TryDownload();

                var FilterList = Encoding.UTF8.GetString(Filter).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var CFilters = new List<Filter>(FilterList.Length);

                foreach (var entry in FilterList)
                {
                    CFilters.Add(parser.ParseAbpFormattedRule(entry, 1));
                }

                Filters = new FilterDbCollection();

                Filters.ParseStoreRules(FilterList, 1);
                Filters.FinalizeForRead();
            }
            catch { }
        }

        public static void InstallAdBlock(this ChromiumWebBrowser Browser)
        {
            if (Browser.RequestHandler == null || !(Browser.RequestHandler is RequestEventHandler))
            {
                Browser.RequestHandler = new RequestEventHandler();
            }

            /*
            ((RequestEventHandler)Browser.RequestHandler).OnResourceResponseEvent += (obj, args) => {
                if (IsAdUri(new Uri(args.TargetUrl), args.Request.Headers, args.Request.ReferrerUrl))
                {
                    args.Cancel = true;
                }
            };

            ((RequestEventHandler)Browser.RequestHandler).OnBeforeResourceLoadEvent += (obj, args) => {
                if (IsAdUri(new Uri(args.TargetUrl), args.Request.Headers, args.Request.ReferrerUrl))
                {
                    args.ReturnValue = CefSharp.CefReturnValue.Cancel;
                }
            };
            */

            ((RequestEventHandler)Browser.RequestHandler).OnResourceRequestEvent += (obj, args) => {
                if (IsAdUri(new Uri(args.TargetUrl), args.Request.Headers, args.Request.ReferrerUrl))
                {
                    args.Cancel = true;
                }
            };
        }
        public static bool IsAdUri(Uri Url, NameValueCollection Headers, string Referer = "")
        {
            if (Filters == null)
                return false;

            var filters = Filters.GetFiltersForDomain(Url.Host);
            if (Referer != string.Empty)
                filters = filters.Concat(Filters.GetFiltersForDomain(Referer));

            if (filters == null) return false;

            foreach (Filter filter in filters)
            {
                if (filter is UrlFilter urlFilter)
                {
                    if (urlFilter.IsMatch(Url, Headers))
                        return true;
                    if (Referer != string.Empty && urlFilter.IsMatch(new Uri(Referer), Headers))
                        return true;
                }
            }

            return false;
        }
    }
}
