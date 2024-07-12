using CefSharp;
using CefSharp.DevTools.Debugger;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ContentTypeHeader = System.Net.Mime.ContentType;

namespace MangaUnhost.Browser
{
    public class WebRequestResourceHandler : ResourceHandler
    {

        private class CallbackAwaiter : ICallback
        {
            private TaskCompletionSource<bool> TaskCompletionSource { get; set; }

            public Task<bool> Task => TaskCompletionSource.Task;

            private ICallback TargetCallback;

            public CallbackAwaiter(ICallback Target)
            {
                TargetCallback = Target;
                TaskCompletionSource = new TaskCompletionSource<bool>();
            }

            public bool IsDisposed { get; private set; }

            public void Cancel()
            {
                TargetCallback.Cancel();
                TaskCompletionSource.SetResult(false);
            }

            public void Continue()
            {
                TargetCallback.Continue();
                TaskCompletionSource.SetResult(true);
            }

            public void Dispose()
            {
                TaskCompletionSource.Task?.Dispose();
                TargetCallback?.Dispose();
                IsDisposed = true;
            }
        }

        List<IDisposable> Objects = new List<IDisposable>();

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
                        
                        httpWebRequest.Headers.Remove("host");
                        httpWebRequest.Headers["host"] = httpWebRequest.RequestUri.Host;
                        
                        httpWebRequest.UserAgent = headers["User-Agent"];
                        httpWebRequest.Accept = headers["Accept"];
                        httpWebRequest.Method = request.Method;
                        httpWebRequest.Referer = request.ReferrerUrl;
                        httpWebRequest.Timeout = 1000 * 30;

                        var RawPostData = ParsePostData(request.PostData);
                        var RequestMod = new OnWebRequestEventArgs(httpWebRequest, headers, RawPostData);

                        OnRequest?.Invoke(this, RequestMod);

                        foreach (var Header in headers.ToPair())
                        {
                            if (httpWebRequest.Headers.AllKeys.Contains(Header.Key))
                                httpWebRequest.Headers.Remove(Header.Key);
                        }

                        httpWebRequest.Headers.Add(RequestMod.Headers);

                        await UrlTools.HttpRequestLocker.WaitAsync();

                        if (RawPostData != null)
                        {
                            using var RequestStream = await httpWebRequest.GetRequestStreamAsync();
                            await RequestStream.WriteAsync(RawPostData, 0, RawPostData.Length);
                        }

                        //Can't be disposed because the browser will read the data after continue
                        var httpWebResponse = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
                        
                        FinishResponse(httpWebResponse, httpWebRequest);
                        callback.Continue();

                        Objects.Add(httpWebResponse);
                    }
                    catch (NotSupportedException ex)
                    {
                        using (var awaiter = new CallbackAwaiter(callback))
                        {
                            if (DefaultHandler(request, awaiter) == CefReturnValue.ContinueAsync)
                                await awaiter.Task;
                        }
                    }
                    catch (WebException ex) {
                        var Response = ex.Response;

                        if (Response != null)
                        {
                            FinishResponse(Response as HttpWebResponse, httpWebRequest);
                            callback.Continue();
                        }

                        Objects.Add(Response);
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

            var contentType = new ContentTypeHeader("text/plain");
            try
            {
                contentType = new ContentTypeHeader(httpWebResponse.ContentType);
            }
            catch { }

            var mimeType = contentType.MediaType;
            var charSet = contentType.CharSet;
            var statusCode = httpWebResponse.StatusCode;

            var Stream = Response.OverrideData ?? Response.ResponseData;

            Objects.Add(Stream);

            if (Response.OverrideData == null)
            {
                if (httpWebResponse.ContentLength >= 0)
                {
                    ResponseLength = (long?)httpWebResponse.ContentLength;
                }
                else
                {
                    ResponseLength = null;
                }
            }
            else
            {
                ResponseLength = Response.OverrideData.Length;
                Objects.Add(Response.ResponseData);
            }

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

            if (postData.Elements.Count == 0)
                return new byte[0];

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

        bool Disposed = false;
        public override void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            foreach (var Obj in Objects)
            {
                Obj?.Dispose();
            }

            UrlTools.HttpRequestLocker.Release();
            base.Dispose();
        }
    }

    public class OnBeforeWebRequestEventArgs : EventArgs
    {
        public IPostData PostData { get; set; }
        public NameValueCollection Headers { get; }
        public IRequest WebRequest { get; set; }

        /// <summary>
        /// When true allows the Request be processed by the CEF
        /// If true the OnWebResponseEvent will not be triggered.
        /// </summary>
        public bool DefaultHandler { get; set; }

        public OnBeforeWebRequestEventArgs(IRequest Request, NameValueCollection Headers, IPostData PostData)
        {
            this.PostData = PostData;
            this.Headers = Headers;
            this.WebRequest = Request;
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

        public Stream OverrideData { get; set; }
        public OnWebResponseEventArgs(HttpWebResponse Response, HttpWebRequest Request, NameValueCollection Headers, Stream ResponseData)
        {
            this.WebRequest = Request;
            this.WebResponse = Response;
            this.Headers = Headers;
            this.ResponseData = ResponseData;
        }
    }
}
