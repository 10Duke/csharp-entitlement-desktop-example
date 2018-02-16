using System;

namespace Tenduke.EntitlementClient.Config
{
    /// <summary>
    /// Base class OAuth configuration objects.
    /// </summary>
    [Serializable]
    public abstract class OAuthConfig
    {
        /// <summary>
        /// The OAuth 2.0 client id.
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// The OAuth 2.0 scope. If using OpenID Connect, <c>openid</c> scope value must be included.
        /// </summary>
        /// <example>openid profile email</example>
        public string Scope { get; set; }

        /// <summary>
        /// Uri of the OpenID Connect userinfo endpoint of the 10Duke Entitlement service.
        /// </summary>
        public string UserInfoUri { get; set; }
    }
}
