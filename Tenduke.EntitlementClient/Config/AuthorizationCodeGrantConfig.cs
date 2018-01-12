using System;

namespace Tenduke.EntitlementClient.Config
{
    /// <summary>
    /// Configuration for using the OAuth 2.0 Authorization Code Grant flow for
    /// communicating with the 10Duke Entitlement service.
    /// </summary>
    public class AuthorizationCodeGrantConfig : BrowserBasedAuthorizationConfig
    {
        /// <summary>
        /// OAuth 2.0 client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Uri of the OAuth 2.0 token endpoint of the 10Duke Entitlement service.
        /// </summary>
        public string TokenUri { get; set; }
    }
}
