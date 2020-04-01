﻿// Copyright © 2017 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.
// Edited by marcussacana; 2020

using System;
using System.IO;
using System.Net;
using CefSharp.Handler;

namespace CefSharp.EventHandler {

    public class RequestEventHandler : DefaultRequestHandler {

        public event EventHandler<OnBeforeBrowseEventArgs> OnBeforeBrowseEvent;
        public event EventHandler<OnOpenUrlFromTabEventArgs> OnOpenUrlFromTabEvent;
        public event EventHandler<OnCertificateErrorEventArgs> OnCertificateErrorEvent;
        public event EventHandler<OnPluginCrashedEventArgs> OnPluginCrashedEvent;
        public event EventHandler<GetAuthCredentialsEventArgs> GetAuthCredentialsEvent;
        public event EventHandler<OnRenderProcessTerminatedEventArgs> OnRenderProcessTerminatedEvent;
        public event EventHandler<OnQuotaRequestEventArgs> OnQuotaRequestEvent;
        public event EventHandler<OnResourceRequestEventArgs> OnResourceRequestEvent;              


        public NetworkCredential Credential { get; private set; }

        public void SetCredentials(NetworkCredential Credential) {
            this.Credential = Credential;
        }

        public override bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect) {
            var args = new OnBeforeBrowseEventArgs(browserControl, browser, frame, request, userGesture, isRedirect);

            OnBeforeBrowseEvent?.Invoke(this, args);

            return args.CancelNavigation;
        }

        public override bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture) {
            var args = new OnOpenUrlFromTabEventArgs(browserControl, browser, frame, targetUrl, targetDisposition, userGesture);

            OnOpenUrlFromTabEvent?.Invoke(this, args);

            return args.CancelNavigation;
        }

        public override bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback) {
            var args = new OnCertificateErrorEventArgs(browserControl, browser, errorCode, requestUrl, sslInfo, callback);

            OnCertificateErrorEvent?.Invoke(this, args);

            EnsureCallbackDisposal(callback);
            return args.ContinueAsync;
        }

        public override void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath) {
            var args = new OnPluginCrashedEventArgs(browserControl, browser, pluginPath);

            OnPluginCrashedEvent?.Invoke(this, args);
        }

        public override bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            var args = new GetAuthCredentialsEventArgs(browserControl, browser, isProxy, host, port, realm, scheme, callback);

            GetAuthCredentialsEvent?.Invoke(this, args);

            EnsureCallbackDisposal(callback);
            return args.ContinueAsync;
        }
        public override void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status) {
            var args = new OnRenderProcessTerminatedEventArgs(browserControl, browser, status);

            OnRenderProcessTerminatedEvent?.Invoke(this, args);
        }

        public override bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback) {
            var args = new OnQuotaRequestEventArgs(browserControl, browser, originUrl, newSize, callback);
            OnQuotaRequestEvent?.Invoke(this, args);

            EnsureCallbackDisposal(callback);
            return args.ContinueAsync;
        }

        public override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool iNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            var args = new OnResourceRequestEventArgs(browserControl, browser, frame, request, iNavigation, isDownload, requestInitiator, disableDefaultHandling);
            OnResourceRequestEvent?.Invoke(this, args);

            disableDefaultHandling = args.DisableDefaultHandling;

            if (args.Cancel)
                return null;

            return base.GetResourceRequestHandler(browserControl, browser, frame, request, iNavigation, isDownload, requestInitiator, ref disableDefaultHandling);
        }

        private static void EnsureCallbackDisposal(IRequestCallback callbackToDispose) {
            if (callbackToDispose != null && !callbackToDispose.IsDisposed) {
                callbackToDispose.Dispose();
            }
        }

        private static void EnsureCallbackDisposal(IAuthCallback callbackToDispose) {
            if (callbackToDispose != null && !callbackToDispose.IsDisposed) {
                callbackToDispose.Dispose();
            }
        }
    }

    public class ResourceRequestEventHandler : IResourceRequestHandler
    {
        public event EventHandler<OnGetCookieAccessFilterEventArgs> OnGetCookieAccessFilterEvent;
        public event EventHandler<OnGetResourceHandlerEventArgs> OnGetResourceHandlerEvent;
        public event EventHandler<OnGetResourceResponseFilterEventArgs> OnGetResourceResponseFilterEvent;
        public event EventHandler<OnBeforeResourceLoadEventArgs> OnBeforeResourceLoadEvent;
        public event EventHandler<OnProtocolExecutionEventArgs> OnProtocolExecutionEvent;
        public event EventHandler<OnResourceLoadCompleteEventArgs> OnResourceLoadCompleteEvent;
        public event EventHandler<OnResourceRedirectEventArgs> OnResourceRedirectEvent;
        public event EventHandler<OnResourceResponseEventArgs> OnResourceResponseEvent;

        ICookieAccessFilter IResourceRequestHandler.GetCookieAccessFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
        {
            var args = new OnGetCookieAccessFilterEventArgs(browserControl, browser, frame, request);
            OnGetCookieAccessFilterEvent?.Invoke(this, args);

            if (args.CookieAccessFilter != null)
                return args.CookieAccessFilter;

            return null;
        }

        IResourceHandler IResourceRequestHandler.GetResourceHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
        {
            var args = new OnGetResourceHandlerEventArgs(browserControl, browser, frame, request);
            OnGetResourceHandlerEvent?.Invoke(this, args);

            if (args.ResourceHandler != null)
                return args.ResourceHandler;

            return null;
        }

        IResponseFilter IResourceRequestHandler.GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            var args = new OnGetResourceResponseFilterEventArgs(browserControl, browser, frame, request, response);
            OnGetResourceResponseFilterEvent?.Invoke(this, args);

            if (args.ResponseFilter != null)
                return args.ResponseFilter;
            
            return null;
        }

        CefReturnValue IResourceRequestHandler.OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var args = new OnBeforeResourceLoadEventArgs(browserControl, browser, frame, request, callback);
            OnBeforeResourceLoadEvent?.Invoke(this, args);

            if (args.ReturnValue == CefReturnValue.Cancel)
                return args.ReturnValue;

            return CefReturnValue.Continue;
        }
    
        bool IResourceRequestHandler.OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
        {
            var args = new OnProtocolExecutionEventArgs(browserControl, browser, frame, request);
            OnProtocolExecutionEvent?.Invoke(this, args);

            return args.AttemptExecution;
        }     
    
        void IResourceRequestHandler.OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
            var args = new OnResourceLoadCompleteEventArgs(browserControl, browser, frame, request, response, status, receivedContentLength);
            OnResourceLoadCompleteEvent?.Invoke(this, args);
        }     
        void IResourceRequestHandler.OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
        {
            var args = new OnResourceRedirectEventArgs(browserControl, browser, frame, request, response);
            OnResourceRedirectEvent?.Invoke(this, args);
            
            if (args.NewUrl != null)
                newUrl = args.NewUrl;
        }

        bool IResourceRequestHandler.OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            var args = new OnResourceResponseEventArgs(browserControl, browser, frame, request, response);
            OnResourceResponseEvent?.Invoke(this, args);

            return args.Retry;
        }
    }

    public class ResourceRequestEventHandlerFactory : IResourceRequestHandlerFactory
    {
        public event EventHandler<OnGetCookieAccessFilterEventArgs> OnGetCookieAccessFilterEvent;
        public event EventHandler<OnGetResourceHandlerEventArgs> OnGetResourceHandlerEvent;
        public event EventHandler<OnGetResourceResponseFilterEventArgs> OnGetResourceResponseFilterEvent;
        public event EventHandler<OnBeforeResourceLoadEventArgs> OnBeforeResourceLoadEvent;
        public event EventHandler<OnProtocolExecutionEventArgs> OnProtocolExecutionEvent;
        public event EventHandler<OnResourceLoadCompleteEventArgs> OnResourceLoadCompleteEvent;
        public event EventHandler<OnResourceRedirectEventArgs> OnResourceRedirectEvent;
        public event EventHandler<OnResourceResponseEventArgs> OnResourceResponseEvent;

        public bool HasHandlers => true;

        public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        {
            var ResourceRequestEventHandler = new ResourceRequestEventHandler();
            ResourceRequestEventHandler.OnGetCookieAccessFilterEvent += OnGetCookieAccessFilterEvent;
            ResourceRequestEventHandler.OnGetResourceHandlerEvent += OnGetResourceHandlerEvent;
            ResourceRequestEventHandler.OnGetResourceResponseFilterEvent += OnGetResourceResponseFilterEvent;
            ResourceRequestEventHandler.OnBeforeResourceLoadEvent += OnBeforeResourceLoadEvent;
            ResourceRequestEventHandler.OnProtocolExecutionEvent += OnProtocolExecutionEvent;
            ResourceRequestEventHandler.OnResourceLoadCompleteEvent += OnResourceLoadCompleteEvent;
            ResourceRequestEventHandler.OnResourceRedirectEvent += OnResourceRedirectEvent;
            ResourceRequestEventHandler.OnResourceResponseEvent += OnResourceResponseEvent;

            return ResourceRequestEventHandler;
        }
    }

    #region EventArgs

    public abstract class BaseRequestEventArgs : EventArgs {
        protected BaseRequestEventArgs(IWebBrowser browserControl, IBrowser browser) {
            this.browserControl = browserControl;
            Browser = browser;
        }

        public IWebBrowser browserControl { get; private set; }
        public IBrowser Browser { get; private set; }
    }

    public class GetAuthCredentialsEventArgs : BaseRequestEventArgs {
        public GetAuthCredentialsEventArgs(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback) : base(browserControl, browser) {
            IsProxy = isProxy;
            Host = host;
            Port = port;
            Realm = realm;
            Scheme = scheme;
            Callback = callback;

            ContinueAsync = false; // default
        }
        public bool IsProxy { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Realm { get; private set; }
        public string Scheme { get; private set; }

        public IAuthCallback Callback { get; private set; }

        public bool ContinueAsync { get; set; }
    }

    public class OnBeforeBrowseEventArgs : BaseRequestEventArgs {
        public OnBeforeBrowseEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            : base(browserControl, browser) {
            Frame = frame;
            Request = request;
            IsRedirect = isRedirect;
            UserGesture = userGesture;

            CancelNavigation = false; // default
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public bool IsRedirect { get; private set; }
        public bool UserGesture { get; private set; }

        public bool CancelNavigation { get; set; }
    }

    public class OnCertificateErrorEventArgs : BaseRequestEventArgs {
        public OnCertificateErrorEventArgs(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
            : base(browserControl, browser) {
            ErrorCode = errorCode;
            RequestUrl = requestUrl;
            SSLInfo = sslInfo;
            Callback = callback;

            ContinueAsync = false; // default
        }

        public CefErrorCode ErrorCode { get; private set; }
        public string RequestUrl { get; private set; }
        public ISslInfo SSLInfo { get; private set; }

        public IRequestCallback Callback { get; private set; }

        public bool ContinueAsync { get; set; }
    }

    public class OnOpenUrlFromTabEventArgs : BaseRequestEventArgs {
        public OnOpenUrlFromTabEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
            : base(browserControl, browser) {
            Frame = frame;
            TargetUrl = targetUrl;
            TargetDisposition = targetDisposition;
            UserGesture = userGesture;

            CancelNavigation = false; // default
        }

        public IFrame Frame { get; private set; }
        public string TargetUrl { get; private set; }
        public WindowOpenDisposition TargetDisposition { get; private set; }
        public bool UserGesture { get; private set; }

        public bool CancelNavigation { get; set; }
    }

    public class OnPluginCrashedEventArgs : BaseRequestEventArgs {
        public OnPluginCrashedEventArgs(IWebBrowser browserControl, IBrowser browser, string pluginPath) : base(browserControl, browser) {
            PluginPath = pluginPath;
        }

        public string PluginPath { get; private set; }
    }

    public class OnProtocolExecutionEventArgs : BaseRequestEventArgs {
        public OnProtocolExecutionEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request) : base(browserControl, browser) {
            Frame = frame;
            Request = request;

            AttemptExecution = false; // default
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public string TargetUrl => Request.Url;

        public bool AttemptExecution { get; set; }
    }

    public class OnQuotaRequestEventArgs : BaseRequestEventArgs {
        public OnQuotaRequestEventArgs(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
            : base(browserControl, browser) {
            OriginUrl = originUrl;
            NewSize = newSize;
            Callback = callback;

            ContinueAsync = false; // default
        }

        public string OriginUrl { get; private set; }
        public long NewSize { get; private set; }

        public IRequestCallback Callback { get; private set; }

        public bool ContinueAsync { get; set; }
    }

    public class OnRenderProcessTerminatedEventArgs : BaseRequestEventArgs
    {
        public OnRenderProcessTerminatedEventArgs(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
            : base(browserControl, browser)
        {
            Status = status;
        }

        public CefTerminationStatus Status { get; private set; }
    }
    
    public class OnResourceResponseEventArgs : BaseRequestEventArgs
    {
        public OnResourceResponseEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response) : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;
            Response = response;

            Retry = false;
        }
        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public string TargetUrl => Request.Url;

        /// <summary>
        /// If you modify the resource, set true
        /// </summary>
        public bool Retry { get; set; }
    }

    public class OnBeforeResourceLoadEventArgs : BaseRequestEventArgs
    {
        public OnBeforeResourceLoadEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback) : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;
            Callback = callback;

            ReturnValue = CefReturnValue.Continue;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public string TargetUrl => Request.Url;

        public IRequestCallback Callback { get; private set; }

        public CefReturnValue ReturnValue { get; set; }
    }

    public class OnResourceRequestEventArgs : BaseRequestEventArgs
    {
        public OnResourceRequestEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool iNavigation, bool isDownload, string requestInitiator, bool disableDefaultHandling) : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;

            DisableDefaultHandling = disableDefaultHandling;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public string TargetUrl => Request.Url;

        public bool DisableDefaultHandling { get; set; }

        public bool Cancel { get; set; }
    }    

    public class OnGetCookieAccessFilterEventArgs : BaseRequestEventArgs
    {
        public OnGetCookieAccessFilterEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)  : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public string TargetUrl => Request.Url;
        public ICookieAccessFilter CookieAccessFilter { get; set; }
    }
   
    public class OnGetResourceHandlerEventArgs : BaseRequestEventArgs
    {
        public OnGetResourceHandlerEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request) : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public string TargetUrl => Request.Url;
        public IResourceHandler ResourceHandler { get; set; }
    }

    public class OnGetResourceResponseFilterEventArgs : BaseRequestEventArgs
    {
        public OnGetResourceResponseFilterEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response) : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;
            Response = response;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public string TargetUrl => Request.Url;
        public IResponseFilter ResponseFilter { get; set; }
    }

    public class OnResourceLoadCompleteEventArgs : BaseRequestEventArgs
    {
        public OnResourceLoadCompleteEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength) : base(browserControl, browser)
        {
            Frame = frame;
            Request = request;
            Response = response;
            RequestStatus = status;
            RecivedContentLength = receivedContentLength;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public string TargetUrl => Request.Url;
        public UrlRequestStatus RequestStatus { get; private set; }
        public long RecivedContentLength { get; private set; }
    }

    public class OnResourceRedirectEventArgs : BaseRequestEventArgs {
        public OnResourceRedirectEventArgs(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response) : base (browserControl, browser){
            Frame = frame;
            Request = request;
            Response = response;
        }

        public IFrame Frame { get; private set; }
        public IRequest Request { get; private set; }
        public IResponse Response { get; private set; }
        public string TargetUrl => Request.Url;
        public string NewUrl { get; set; } = null;
    }

    #endregion
}