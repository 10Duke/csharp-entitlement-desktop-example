using System;
using System.Security.Cryptography;

namespace Tenduke.EntitlementClient.Config
{
    /// <summary>
    /// Base class for OAuth 2.0 configuration objects used with OAuth 2.0 flows that may
    /// be implemented using a browser-based authorization flow.
    /// </summary>
    public abstract class BrowserBasedAuthorizationConfig : OAuthConfig
    {
        /// <summary>
        /// The redirect Uri for redirecting back to the client from the server.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Uri of the OAuth 2.0 authorization endpoint of the 10Duke Entitlement service.
        /// </summary>
        public string AuthzUri { get; set; }

        /// <summary>
        /// RSA public key for verifying signatures of OpenID Connect ID Tokens received from
        /// the 10Duke Entitlement Service.
        /// </summary>
        public RSA SignerKey { get; set; }
    }
}
