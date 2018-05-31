using NUnit.Framework;
using System;
using Tenduke.Client.Authorization;
using Tenduke.Client.Config;
using Tenduke.EntitlementClient.Authorization;
using Tenduke.EntitlementClient.Config;
using Tenduke.EntitlementClient.Util;

namespace Tenduke.EntitlementClient.Test
{
    [TestFixture]
    class EntClientTests
    {
        [Test]
        public void AuthzApiConfig_WithExplicitConfiguration()
        {
            var instance = new EntClient();

            var authzApiConfig = new AuthzApiConfig();
            instance.AuthzApiConfig = authzApiConfig;

            Assert.AreSame(authzApiConfig, instance.AuthzApiConfig, "If AuthzApiConfig is explicity set, must return the config as set");
        }

        [Test]
        public void AuthzApiConfig_FromOAuthConfig()
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
        public void ComputerId_FromComputerIdentityConfig()
        {
            var instance = new EntClient();

            var computerIdentityConfig = new ComputerIdentityConfig() { ComputerId = "TestComputerId" };
            instance.ComputerIdentityConfig = computerIdentityConfig;

            Assert.AreEqual("TestComputerId", instance.ComputerId, "Must return the computer id value specified in the configuration");
        }

        [Test]
        public void ComputerId_WithDefaultSettings()
        {
            var instance = new EntClient();

            var computerId = instance.ComputerId;
            Assert.IsNotNull(computerId, "Must return a computer id using default configuration");
            Assert.AreEqual(computerId, instance.ComputerId,  "Must return the same value for subsequent calls");
        }

        [Test]
        public void ComputerId_ByComputerIdentityConfig()
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

        [Test]
        public void AuthzApi_SuccessWithDerivedAuthzApiConfig()
        {
            var instance = new EntClient();

            // Specify only OAuth configuration here, configuration for accessing AuthzApi is derived from this configuration
            var oauthConfig = new AuthorizationCodeGrantConfig()
            {
                AuthzUri = "https://test/oauth2/oauth_authorization/"
            };
            instance.OAuthConfig = oauthConfig;

            var accessTokenResponse = AccessTokenResponse.FromJsonStringResponse("{\"access_token\":\"testat\"}", null);
            var authorization = new AuthorizationCodeGrant()
            {
                AccessTokenResponse = accessTokenResponse
            };
            instance.Authorization = authorization;

            var result = instance.AuthzApi;
            Assert.IsNotNull(result, "Initializing an AuthzApi instance for accessing the /authz/ API failed");
            Assert.AreEqual("https://test/authz/", result.AuthzApiConfig.EndpointUri, "Invalid /authz/ API endpoint Uri");
            Assert.AreEqual(accessTokenResponse.AccessToken, result.AccessToken, "Must use the specified access token response");
            Assert.IsNotNull(result.ComputerId, "Computer id missing");
        }

        [Test]
        public void AuthzApi_SuccessWithExplicitAuthzApiConfig()
        {
            var instance = new EntClient();

            var authzApiConfig = new AuthzApiConfig()
            {
                EndpointUri = "https://test/authz/"
            };
            instance.AuthzApiConfig = authzApiConfig;

            var accessTokenResponse = AccessTokenResponse.FromJsonStringResponse("{\"access_token\":\"testat\"}", null);
            var authorization = new AuthorizationCodeGrant()
            {
                AccessTokenResponse = accessTokenResponse
            };
            instance.Authorization = authorization;

            var result = instance.AuthzApi;
            Assert.IsNotNull(result, "Initializing an AuthzApi instance for accessing the /authz/ API failed");
            Assert.AreEqual(authzApiConfig, result.AuthzApiConfig, "Must use the specified AuthzApiConfig object");
            Assert.AreEqual(accessTokenResponse.AccessToken, result.AccessToken, "Must use the specified access token response");
            Assert.IsNotNull(result.ComputerId, "Computer id missing");
        }

        [Test]
        public void AuthzApi_WithOAuthAuthorizationError()
        {
            var instance = new EntClient();

            var authzApiConfig = new AuthzApiConfig()
            {
                EndpointUri = "https://test/authz/"
            };
            instance.AuthzApiConfig = authzApiConfig;

            var authorization = new AuthorizationCodeGrant()
            {
                Error = "test_error"
            };
            instance.Authorization = authorization;

            Assert.That(() => instance.AuthzApi, Throws.InvalidOperationException);
        }

        [Test]
        public void AuthzApi_WithoutOAuthAuthorization()
        {
            var instance = new EntClient();

            var authzApiConfig = new AuthzApiConfig()
            {
                EndpointUri = "https://test/authz/"
            };
            instance.AuthzApiConfig = authzApiConfig;

            Assert.That(() => instance.AuthzApi, Throws.InvalidOperationException);
        }

        [Test]
        public void AuthzApi_WithoutConfig()
        {
            var instance = new EntClient();

            Assert.That(() => instance.AuthzApi, Throws.InvalidOperationException);
        }

        [Test]
        public void IsAuthorized()
        {
            var instance = new EntClient();

            var accessTokenResponse = AccessTokenResponse.FromJsonStringResponse("{\"access_token\":\"testat\"}", null);
            var authorization = new AuthorizationCodeGrant()
            {
                AccessTokenResponse = accessTokenResponse
            };
            instance.Authorization = authorization;

            Assert.IsTrue(instance.IsAuthorized(), "OAuth authorization with AccessTokenResponse is present, must return true");
        }

        [Test]
        public void IsAuthorized_MissingAccessTokenResponse()
        {
            var instance = new EntClient();

            var authorization = new AuthorizationCodeGrant();
            instance.Authorization = authorization;

            Assert.IsFalse(instance.IsAuthorized(), "AccessTokenResponse is missing, must return false");
        }

        [Test]
        public void IsAuthorized_MissingOAuthAuthorization()
        {
            var instance = new EntClient();

            Assert.IsFalse(instance.IsAuthorized(), "OAuth authorization is missing, must return false");
        }

        [Test]
        public void ClearAuthorization()
        {
            var instance = new EntClient();

            var accessTokenResponse = AccessTokenResponse.FromJsonStringResponse("{\"access_token\":\"testat\"}", null);
            var authorization = new AuthorizationCodeGrant()
            {
                AccessTokenResponse = accessTokenResponse
            };
            instance.Authorization = authorization;

            Assert.IsTrue(instance.IsAuthorized(), "OAuth authorization with AccessTokenResponse is present, must return true");

            instance.ClearAuthorization();

            Assert.IsFalse(instance.IsAuthorized(), "OAuth authorization cleared, must return false");
        }

        [Test]
        public void AuthorizationSerializer_ReadAndWrite()
        {
            var instance = new EntClient();

            var accessTokenResponse = AccessTokenResponse.FromJsonStringResponse("{\"access_token\":\"testat\"}", null);
            var authorization = new AuthorizationCodeGrant()
            {
                AccessTokenResponse = accessTokenResponse
            };
            instance.Authorization = authorization;

            var serialized = instance.AuthorizationSerializer.ReadAuthorizationToBase64();
            instance.ClearAuthorization();
            instance.AuthorizationSerializer.WriteAuthorizationFromBase64(serialized);

            var deserialized = instance.Authorization;
            Assert.AreEqual(
                "testat",
                deserialized.AccessTokenResponse.AccessToken,
                "Must have the original access token after serialization and deserialization");
        }
    }
}
