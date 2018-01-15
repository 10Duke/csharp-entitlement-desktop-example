using NUnit.Framework;
using Tenduke.EntitlementClient.Config;
using Tenduke.EntitlementClient.Util;

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

        [Test]
        public void ComputerIdFromComputerIdentityConfig()
        {
            var instance = new EntClient();

            var computerIdentityConfig = new ComputerIdentityConfig() { ComputerId = "TestComputerId" };
            instance.ComputerIdentityConfig = computerIdentityConfig;

            Assert.AreEqual("TestComputerId", instance.ComputerId, "Must return the computer id value specified in the configuration");
        }

        [Test]
        public void ComputerIdWithDefaultSettings()
        {
            var instance = new EntClient();

            var computerId = instance.ComputerId;
            Assert.IsNotNull(computerId, "Must return a computer id using default configuration");
            Assert.AreEqual(computerId, instance.ComputerId,  "Must return the same value for subsequent calls");
        }

        [Test]
        public void ComputerIdByComputerIdentityConfig()
        {
            var instance = new EntClient();

            var computerIdentityConfig = new ComputerIdentityConfig()
            {
                AdditionalIdentifier = "MyCustomHwIdentifier",
                ComputeBy = new ComputerIdentity.ComputerIdentifier[]
                {
                    ComputerIdentity.ComputerIdentifier.BaseboardSerialNumber,
                    ComputerIdentity.ComputerIdentifier.ComputerSystemProductUuid,
                    ComputerIdentity.ComputerIdentifier.WindowsDigitalProductId,
                    ComputerIdentity.ComputerIdentifier.WindowsProductId
                },
                Salt = "MyAppSalt"
            };
            instance.ComputerIdentityConfig = computerIdentityConfig;

            var computerId = instance.ComputerId;
            Assert.IsNotNull(computerId, "Must return a computer id using the given configuration");
            Assert.AreEqual(computerId, instance.ComputerId, "Must return the same value for subsequent calls");
            Assert.AreNotEqual(new EntClient().ComputerId, computerId, "Must return different value than with default configuration");
        }
    }
}
