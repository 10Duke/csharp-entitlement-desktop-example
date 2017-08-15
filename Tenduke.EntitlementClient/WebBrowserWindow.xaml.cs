using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Security.Cryptography.X509Certificates;

namespace Tenduke.EntitlementClient
{
    /// <summary>
    /// Interaction logic for WebBrowserWindow.xaml
    /// </summary>
    public partial class WebBrowserWindow : Window
    {
        /// <summary>
        /// Redirect URL used for OAuth 2.0 redirect giving the OAuth 2.0 Implicit Grant Flow response.
        /// </summary>
        public const string APP_REDIRECT_URL = "tenduke-app://oauthcb/";

        /// <summary>
        /// The OAuth 2.0 Implicit Grant Flow response received from the server.
        /// </summary>
        public string OAuthResponse { get; set; }

        public WebBrowserWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows modal dialog with embedded web browser for starting the OAuth 2.0 Implicit Grant Flow
        /// process.
        /// </summary>
        /// <param name="authzUrl">URL of server OAuth 2.0 authorization endpoint.</param>
        /// <returns>Returns value returned by <see cref="Window.ShowDialog"/>.</returns>
        public bool? ShowAuthzDialog(Uri authzUrl)
        {
            webBrowser.Address = authzUrl.ToString();
            webBrowser.RequestHandler = new AuthzRequestHandler(this);
            return ShowDialog();
        }

        /// <summary>
        /// Called to handle the OAuth Implicit Grant response redirect.
        /// </summary>
        /// <param name="browserControl">The browser control.</param>
        /// <param name="browser">The browser.</param>
        /// <param name="url">The redirect URL used for sending the OAuth response.</param>
        /// <returns>Always returns <c>false</c>, never letting the browser continue handling after this handler.</returns>
        private bool HandleOAuthCallback(IWebBrowser browserControl, IBrowser browser, string url)
        {
            if (url.StartsWith(APP_REDIRECT_URL))
            {
                var fragmentStartIndex = url.IndexOf('#');
                var fragment = fragmentStartIndex == -1 ? null : url.Substring(fragmentStartIndex + 1);
                OAuthResponse = fragment;
                Application.Current.Dispatcher.Invoke(delegate
                {
                    DialogResult = true;
                    Close();
                });
            }

            return false;
        }

        /// <summary>
        /// Implementation of CefSharp request handler, used for capturing the OAuth response.
        /// </summary>
        private class AuthzRequestHandler : IRequestHandler
        {
            private readonly WebBrowserWindow parent;

            public AuthzRequestHandler(WebBrowserWindow parent)
            {
                this.parent = parent;
            }

            public bool GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
            {
                return false;
            }

            public IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            {
                return null;
            }

            public bool OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
            {
                return false;
            }

            public CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
            {
                return CefReturnValue.Continue;
            }

            public bool OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
            {
                return false;
            }

            public bool OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
            {
                return true;
            }

            public void OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
            {
            }

            public bool OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
            {
                return parent.HandleOAuthCallback(browserControl, browser, url);
            }

            public bool OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
            {
                return false;
            }

            public void OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
            {
            }

            public void OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
            {
            }

            public void OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
            {
            }

            public void OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
            {
            }

            public bool OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
            {
                return false;
            }

            public bool OnSelectClientCertificate(IWebBrowser browserControl, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
            {
                return false;
            }
        }
    }
}
