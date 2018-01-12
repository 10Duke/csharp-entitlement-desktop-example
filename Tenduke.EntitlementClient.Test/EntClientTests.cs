using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tenduke.EntitlementClient.Config;

namespace Tenduke.EntitlementClient.Test
{
    [TestFixture]
    public class EntClientTests
    {
        [Test]
        public void AuthzApiConfigWithExplicitConfiguration()
        {
            var instance = new EntClient();

            var authzApiConfig = new AuthzApiConfig();
            instance.AuthzApiConfig = authzApiConfig;

            Assert.AreSame(authzApiConfig, instance.AuthzApiConfig, "If AuthzApiConfig is explicity set, must return the config as set");
        }

        [Test]
        public void AuthzApiConfigFromOAuthConfig()
        {
            var instance = new EntClient();

            var oauthConfig = new AuthorizationCodeGrantConfig()
            {
                AuthzUri = "https://test/authz/"
            };
            instance.OAuthConfig = oauthConfig;

            Assert.IsNotNull(instance.AuthzApiConfig, "AuthzApiConfig must be derived from OAuthConfig");
        }
    }
}
