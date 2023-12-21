using CefSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Management;
using System.Web.Security;
using ContentTypeHeader = System.Net.Mime.ContentType;

namespace MangaUnhost.Browser
{
    public class WebRequestResourceHandler : ResourceHandler
    {
        public event EventHandler<OnWebRequestEventArgs> OnRequest;
        public event EventHandler<OnWebResponseEventArgs> OnResponse;
        public override CefReturnValue ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(async () =>
            {
                using (callback)
                {
                    HttpWebRequest httpWebRequest = null;
                    try
                    {
                        //Create a clone of the headers so we can modify it
                        var headers = new NameValueCollection(request.Headers);

                        httpWebRequest = (HttpWebRequest)WebRequest.Create(request.Url);
                        httpWebRequest.UserAgent = headers["User-Agent"];
                        httpWebRequest.Accept = headers["Accept"];
                        httpWebRequest.Method = request.Method;
                        httpWebRequest.Referer = request.ReferrerUrl;

                        var RawPostData = ParsePostData(request.PostData);
                        var Request = new OnWebRequestEventArgs(httpWebRequest, headers, RawPostData);
                        ;
                        OnRequest?.Invoke(this, Request);

                        if (RawPostData != null)
                        {
                            using var RequestStream = httpWebRequest.GetRequestStream();
                            RequestStream.Write(RawPostData, 0, RawPostData.Length);
                        }

                        foreach (var Header in request.Headers.ToPair())
                        {
                            if (httpWebRequest.Headers.AllKeys.Contains(Header.Key))
                                httpWebRequest.Headers.Remove(Header.Key);
                        }

                        httpWebRequest.Headers.Add(request.Headers);

                        var httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
                        
                        FinishResponse(httpWebResponse, httpWebRequest);

                        callback.Continue();
                    }
                    catch (NotSupportedException ex)
                    {
                        DefaultHandler(request, callback);
                    }
                    catch (WebException ex) {
                        var Response = ex.Response;
                        if (Response != null)
                        {
                            FinishResponse(Response as HttpWebResponse, httpWebRequest);
                            callback.Continue();
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            });

            return CefReturnValue.ContinueAsync;
        }

        private void FinishResponse(HttpWebResponse httpWebResponse, HttpWebRequest httpWebRequest)
        {
            // Get the stream associated with the response.
            var receiveStream = httpWebResponse.GetResponseStream();

            var Response = new OnWebResponseEventArgs(httpWebResponse, httpWebRequest, new NameValueCollection(httpWebResponse.Headers), receiveStream);
            OnResponse?.Invoke(this, Response);

            var contentType = new ContentTypeHeader(httpWebResponse.ContentType);
            var mimeType = contentType.MediaType;
            var charSet = contentType.CharSet;
            var statusCode = httpWebResponse.StatusCode;

            var Stream = Response.ResponseData;

            ResponseLength = httpWebResponse.ContentLength >= 0 ? (long?)httpWebResponse.ContentLength : null;
            MimeType = mimeType;
            Charset = charSet ?? "UTF-8";
            StatusCode = (int)statusCode;
            base.Stream = Stream;
            AutoDisposeStream = true;


            Headers.Clear();
            Headers.Add(Response.Headers);
        }

        private CefReturnValue DefaultHandler(IRequest request, ICallback callback)
        {
            var ResHandler = new ResourceHandler(MimeType, Stream, AutoDisposeStream, Charset);
            return ResHandler.ProcessRequestAsync(request, callback);
        }

        private byte[] ParsePostData(IPostData postData)
        {
            if (postData == null)
                return null;

            if (postData.Elements.Count == 1)
            {
                var Element = postData.Elements.Single();
                if (string.IsNullOrEmpty(Element.File))
                {
                    return Element.Bytes;
                }
            }

            //TODO: Support for post data with multiple elements
            throw new NotImplementedException();
        }
    }

    public class OnWebRequestEventArgs : EventArgs
    {
        public byte[] PostData { get; set; }
        public NameValueCollection Headers { get; }
        public HttpWebRequest WebRequest { get; set; }
        public OnWebRequestEventArgs(HttpWebRequest Request, NameValueCollection Headers, byte[] PostData) { 
            this.PostData = PostData;
            this.Headers = Headers;
            this.WebRequest = Request;
        }
    }
    public class OnWebResponseEventArgs : EventArgs
    {
        public byte[] PostData { get; set; }
        public NameValueCollection Headers { get; }
        public HttpWebResponse WebResponse { get; set; }
        public HttpWebRequest WebRequest { get; set; }

        public Stream ResponseData { get; private set; }
        public OnWebResponseEventArgs(HttpWebResponse Response, HttpWebRequest Request, NameValueCollection Headers, Stream ResponseData)
        {
            this.WebRequest = Request;
            this.WebResponse = Response;
            this.Headers = Headers;
            this.ResponseData = ResponseData;
        }
    }
}
