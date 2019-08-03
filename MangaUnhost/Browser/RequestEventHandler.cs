// Copyright © 2017 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Net;
using CefSharp.Handler;

namespace CefSharp.RequestEventHandler {

    /// <summary>
    /// Example class that demos exposing some of the methods of <see cref="RequestHandler"/> as events.
    /// Inheriting from <see cref="RequestHandler"/> requres you only override the methods you are interested in.
    /// You can of course inherit from the interface <see cref="IRequestHandler"/> and implement all the methods
    /// yourself if that's required.
    /// Simply check out the interface method the event was named by (e.g <see cref="OnCertificateErrorEvent" /> corresponds to
    /// <see cref="IRequestHandler.OnCertificateError" />)
    /// inspired by:
    /// https://github.com/cefsharp/CefSharp/blob/fa41529853b2527eb0468a507ab6c5bd0768eb59/CefSharp.Example/RequestHandler.cs
    /// </summary>
    public class RequestEventHandler : DefaultRequestHandler {

        public event EventHandler<OnBeforeBrowseEventArgs> OnBeforeBrowseEvent;
        public event EventHandler<OnOpenUrlFromTabEventArgs> OnOpenUrlFromTabEvent;
        public event EventHandler<OnCertificateErrorEventArgs> OnCertificateErrorEvent;
        public event EventHandler<OnPluginCrashedEventArgs> OnPluginCrashedEvent;
        public event EventHandler<GetAuthCredentialsEventArgs> GetAuthCredentialsEvent;
        public event EventHandler<OnRenderProcessTerminatedEventArgs> OnRenderProcessTerminatedEvent;
        public event EventHandler<OnQuotaRequestEventArgs> OnQuotaRequestEvent;

        public NetworkCredential Credential { get; private set; }

        public void SetCredentials(NetworkCredential Credential) {
            this.Credential = Credential;
        }

        public override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect) {
            var args = new OnBeforeBrowseEventArgs(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);

            OnBeforeBrowseEvent?.Invoke(this, args);

            return args.CancelNavigation;
        }

        public override bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture) {
            var args = new OnOpenUrlFromTabEventArgs(chromiumWebBrowser, browser, frame, targetUrl, targetDisposition, userGesture);

            OnOpenUrlFromTabEvent?.Invoke(this, args);

            return args.CancelNavigation;
        }

        public override bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback) {
            var args = new OnCertificateErrorEventArgs(chromiumWebBrowser, browser, errorCode, requestUrl, sslInfo, callback);

            OnCertificateErrorEvent?.Invoke(this, args);

            EnsureCallbackDisposal(callback);
            return args.ContinueAsync;
        }

        public override void OnPluginCrashed(IWebBrowser chromiumWebBrowser, IBrowser browser, string pluginPath) {
            var args = new OnPluginCrashedEventArgs(chromiumWebBrowser, browser, pluginPath);

            OnPluginCrashedEvent?.Invoke(this, args);
        }

        public override bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback) {
            var args = new GetAuthCredentialsEventArgs(browserControl, browser, isProxy, host, port, realm, scheme, callback);

            GetAuthCredentialsEvent?.Invoke(this, args);

            EnsureCallbackDisposal(callback);
            return args.ContinueAsync;
        }

        public override void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status) {
            var args = new OnRenderProcessTerminatedEventArgs(chromiumWebBrowser, browser, status);

            OnRenderProcessTerminatedEvent?.Invoke(this, args);
        }

        public override bool OnQuotaRequest(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, long newSize, IRequestCallback callback) {
            var args = new OnQuotaRequestEventArgs(chromiumWebBrowser, browser, originUrl, newSize, callback);
            OnQuotaRequestEvent?.Invoke(this, args);

            EnsureCallbackDisposal(callback);
            return args.ContinueAsync;
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

    public abstract class BaseRequestEventArgs : System.EventArgs {
        protected BaseRequestEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser) {
            ChromiumWebBrowser = chromiumWebBrowser;
            Browser = browser;
        }

        public IWebBrowser ChromiumWebBrowser { get; private set; }
        public IBrowser Browser { get; private set; }
    }

    public class GetAuthCredentialsEventArgs : BaseRequestEventArgs {
        public GetAuthCredentialsEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback) : base(chromiumWebBrowser, browser) {
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

        /// <summary>
        ///     Callback interface used for asynchronous continuation of authentication requests.
        /// </summary>
        public IAuthCallback Callback { get; private set; }

        /// <summary>
        ///     Set to true to continue the request and call
        ///     <see cref="T:CefSharp.GetAuthCredentialsEventArgs.Continue(System.String, System.String)" /> when the authentication information
        ///     is available. Set to false to cancel the request.
        /// </summary>
        public bool ContinueAsync { get; set; }
    }

    public class OnBeforeBrowseEventArgs : BaseRequestEventArgs {
        public OnBeforeBrowseEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            : base(chromiumWebBrowser, browser) {
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

        /// <summary>
        ///     Set to true to cancel the navigation or false to allow the navigation to proceed.
        /// </summary>
        public bool CancelNavigation { get; set; }
    }

    public class OnCertificateErrorEventArgs : BaseRequestEventArgs {
        public OnCertificateErrorEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
            : base(chromiumWebBrowser, browser) {
            ErrorCode = errorCode;
            RequestUrl = requestUrl;
            SSLInfo = sslInfo;
            Callback = callback;

            ContinueAsync = false; // default
        }

        public CefErrorCode ErrorCode { get; private set; }
        public string RequestUrl { get; private set; }
        public ISslInfo SSLInfo { get; private set; }

        /// <summary>
        ///     Callback interface used for asynchronous continuation of url requests.
        ///     If empty the error cannot be recovered from and the request will be canceled automatically.
        /// </summary>
        public IRequestCallback Callback { get; private set; }

        /// <summary>
        ///     Set to false to cancel the request immediately. Set to true and use <see cref="T:CefSharp.IRequestCallback" /> to
        ///     execute in an async fashion.
        /// </summary>
        public bool ContinueAsync { get; set; }
    }

    public class OnOpenUrlFromTabEventArgs : BaseRequestEventArgs {
        public OnOpenUrlFromTabEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
            : base(chromiumWebBrowser, browser) {
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

        /// <summary>
        ///     Set to true to cancel the navigation or false to allow the navigation to proceed.
        /// </summary>
        public bool CancelNavigation { get; set; }
    }

    public class OnPluginCrashedEventArgs : BaseRequestEventArgs {
        public OnPluginCrashedEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, string pluginPath) : base(chromiumWebBrowser, browser) {
            PluginPath = pluginPath;
        }

        public string PluginPath { get; private set; }
    }

    public class OnProtocolExecutionEventArgs : BaseRequestEventArgs {
        public OnProtocolExecutionEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, string url) : base(chromiumWebBrowser, browser) {
            Url = url;

            AttemptExecution = false; // default
        }

        public string Url { get; private set; }

        /// <summary>
        ///     Set to true to attempt execution via the registered OS protocol handler, if any. Otherwise set to false.
        /// </summary>
        public bool AttemptExecution { get; set; }
    }

    public class OnQuotaRequestEventArgs : BaseRequestEventArgs {
        public OnQuotaRequestEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
            : base(chromiumWebBrowser, browser) {
            OriginUrl = originUrl;
            NewSize = newSize;
            Callback = callback;

            ContinueAsync = false; // default
        }

        public string OriginUrl { get; private set; }
        public long NewSize { get; private set; }

        /// <summary>
        ///     Callback interface used for asynchronous continuation of url requests.
        /// </summary>
        public IRequestCallback Callback { get; private set; }

        /// <summary>
        ///     Set to false to cancel the request immediately. Set to true to continue the request
        ///     and call <see cref="T:OnQuotaRequestEventArgs.Callback.Continue(System.Boolean)" /> either in this method or at a later
        ///     time to grant or deny the request.
        /// </summary>
        public bool ContinueAsync { get; set; }
    }

    public class OnRenderProcessTerminatedEventArgs : BaseRequestEventArgs {
        public OnRenderProcessTerminatedEventArgs(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status)
            : base(chromiumWebBrowser, browser) {
            Status = status;
        }

        public CefTerminationStatus Status { get; private set; }
    }
}