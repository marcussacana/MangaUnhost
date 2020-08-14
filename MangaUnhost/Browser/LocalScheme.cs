using CefSharp;
using CefSharp.Callback;
using System;
using System.IO;
using System.Web;

namespace MangaUnhost.Browser
{
    public class LocalSchemeFactory : ISchemeHandlerFactory, IResourceHandler
    {
        const int BufferSize = 1024 * 100;
        public const string SchemeName = "https";

        readonly (string Name, string Root, string DefaultFile)[] SpecialFolders = new[] {
            ("WebComicReader", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WCR"), "index.html")
        };

        Stream Input;
        string Extension;
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            if (request.Method != "GET" && request.Method != "HEAD")
                return null;

            if (!request.Url.ToLowerInvariant().StartsWith($"{SchemeName}://"))
                return null;

            string file = GetFilePath(request.Url);

            if (!File.Exists(file))
                return null;

            return new LocalSchemeFactory();
        }

        public void Cancel()
        {
            Input.Close();
        }

        public void Dispose()
        {
            Input?.Close();
            Input?.Dispose();
        }

        public void GetResponseHeaders(IResponse response, out long responseLength, out string redirectUrl)
        {
            try
            {
                response.StatusCode = 200;
                response.MimeType = GetMime();
                response.Headers["Content-Type"] = GetMime();
                response.Headers["Content-Length"] = Input.Length.ToString();
                responseLength = Input.Length;
                redirectUrl = null;
            }
            catch
            {
                responseLength = -1;
                response.ErrorCode = CefErrorCode.InvalidUrl;
                redirectUrl = null;
                return;
            }
        }

        private string GetMime()
        {
            return Extension switch
            {
                "xhtml" => "application/xhtml+xml",
                "html" => "text/html",
                "htm" => "text/html",
                "css" => "text/css",
                "js" => "text/javascript",
                "json" => "application/json",
                "txt" => "text/plain",
                "svg" => "image/svg+xml",
                "png" => "image/png",
                "jpg" => "image/jpeg",
                "bmp" => "image/bmp",
                "gif" => "image/gif",
                "tif" => "image/tiff",
                "tiff" => "image/tiff",
                "webp" => "image/webp",
                "bz" => "application/x-bzip",
                "bz2" => "application/x-bzip2",
                "gz" => "application/gzip",
                "ico" => "image/vnd.microsoft.icon",
                _ => "application/octet-stream"
            };
        }

        public bool Open(IRequest request, out bool handleRequest, ICallback callback)
        {
            handleRequest = false;
            if (request.Method != "GET" && request.Method != "HEAD")
                return false;

            if (!request.Url.ToLowerInvariant().StartsWith($"{SchemeName}://"))
                return false;

            string file = GetFilePath(request.Url);

            if (!File.Exists(file))
                return false;

            Input = File.OpenRead(file);
            Extension = Path.GetExtension(file).TrimStart('.').ToLowerInvariant().Trim();

            handleRequest = true;
            return true;
        }

        private string GetFilePath(string URL)
        {
            URL = HttpUtility.UrlDecode(URL);
            if (URL.StartsWith(SchemeName))
                URL = URL.Substring(SchemeName.Length + 3);

            if (URL.StartsWith("res/"))
                URL = URL.Substring(4);

            string Default = null;

            foreach (var Folder in SpecialFolders)
            {
                if (!URL.StartsWith(Folder.Name))
                    continue;

                URL = Folder.Root.TrimEnd('\\', '/') + URL.Substring(Folder.Name.Length);
                Default = Folder.Root.TrimEnd('\\', '/') + "/" + Folder.DefaultFile.TrimStart('\\', '/');
            }

            URL = URL.Replace('/', Path.DirectorySeparatorChar);

            if (Default != null && !File.Exists(URL))
            {
                Default = Default.Replace('/', Path.DirectorySeparatorChar);
                if (File.Exists(Default))
                    return Default;
            }

            return URL;
        }

        public bool ProcessRequest(IRequest request, ICallback callback)
        {
            if (request.Method != "GET" && request.Method != "HEAD")
                return false;

            if (!request.Url.ToLowerInvariant().StartsWith($"{SchemeName}://"))
                return false;

            if (!File.Exists(GetFilePath(request.Url)))
                return false;

            return true;
        }

        public bool Read(Stream dataOut, out int bytesRead, IResourceReadCallback callback)
        {
            int BufferSize = dataOut.Length < LocalSchemeFactory.BufferSize ? (int)dataOut.Length : LocalSchemeFactory.BufferSize;
            try
            {
                var Buffer = new byte[BufferSize];
                bytesRead = Input.Read(Buffer, 0, BufferSize);
                dataOut.Write(Buffer, 0, bytesRead);
            }
            catch
            {
                bytesRead = 0;
                return false;
            }

            return bytesRead > 0;
        }

        public bool ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            int BufferSize = dataOut.Length < LocalSchemeFactory.BufferSize ? (int)dataOut.Length : LocalSchemeFactory.BufferSize;
            try
            {
                var Buffer = new byte[BufferSize];
                bytesRead = Input.Read(Buffer, 0, BufferSize);
                dataOut.Write(Buffer, 0, bytesRead);
            }
            catch
            {
                bytesRead = 0;
                return false;
            }
            return bytesRead > 0;
        }

        public bool Skip(long bytesToSkip, out long bytesSkipped, IResourceSkipCallback callback)
        {
            Input.Position += bytesSkipped = bytesToSkip;
            return true;
        }
    }
}
