using NUnit.Framework;
using System;
using System.IO;
using System.Security.Cryptography;
using Tenduke.Client.Config;
using Tenduke.Client.Util;
using Tenduke.EntitlementClient.Config;
using Tenduke.EntitlementClient.Util;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tenduke.EntitlementClient.Test
{
    /// <summary>
    /// Tests using the embedded CefSharp browser. This test must be run in the test project output directory
    /// with <c>nunit3-console.exe Tenduke.EntitlementClient.Test.dll --domain=None --inprocess</c>.
    /// </summary>
    [Ignore("CefSharp must be run in default app domain. In order to run these tests, replace Ignore with TestFixture attribute, " +
        "and run the test using NUnit.ConsoleRunner with --domain=None --inprocess options.")]
    //[TestFixture]
    class EntClientIntegrationTests
    {
        public static readonly RSA TestServerPublicKey = CryptoUtil.ReadRsaPublicKey(
            "-----BEGIN PUBLIC KEY-----\n"
            + "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA1wRc5dsWBbIJfxay3SYP\n"
            + "MYp/BaLEt0b26/QtwQbrKq6hgVH+euMWsSk6gf0GZiwHMFF+t8/WcsNOfcYMBEHV\n"
            + "mGWSFeYb63IcFN+v3h2580kANzuKuqYnBeBOCN56lJf8q5FOUYQKFVuX/bvEKp+L\n"
            + "1KkMErmIm9e5fkw70zCngxoXGK6qyWX01SPTVfd3UZdPv1H+VOoEpbDsI2yhg5xR\n"
            + "jFAAsqyTYvHQaixiJqqw/T8+2/ond8AlxpzCa1UK9x2l1lMezlwHTHXyPh2ZMpwe\n"
            + "lDBIosKLPHbaZyNwpU0iGOvrDJo8xlw4qGm/fClbaEWM8BCdbn/aKjWMN/t7FEaQ\n"
            + "TQIDAQAB\n"
            + "-----END PUBLIC KEY-----");

        private static string oldWorkingDir;

        private static FluentMockServer mockServer;

        [OneTimeSetUp]
        public static void Init()
        {
            oldWorkingDir = Environment.CurrentDirectory;

            var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dirInfo = new DirectoryInfo(appBaseDir);
            var dirName = dirInfo.Name;
            var entClientDir = Path.GetFullPath(appBaseDir + "..\\..\\..\\Tenduke.EntitlementClient\\bin\\" + dirName);
            Environment.CurrentDirectory = entClientDir;

            var cefSharpResolverArgs = new CefSharpResolverArgs()
            {
                BaseDir = entClientDir
            };
            EntClient.Initialize(cefSharpResolverArgs);

            mockServer = FluentMockServer.Start();
        }

        [OneTimeTearDown]
        public static void Cleanup()
        {
            mockServer.Stop();
            Environment.CurrentDirectory = oldWorkingDir;
            EntClient.Shutdown();
        }

        [Test]
        public void AuthorizeSync()
        {
            // Configure the mock server
            var mockServerBaseUriBuilder = new UriBuilder("http://localhost");
            mockServerBaseUriBuilder.Port = mockServer.Ports[0];
            var mockServerBaseUri = mockServerBaseUriBuilder.Uri;

            mockServer
                .Given(Request.Create()
                    .UsingGet()
                    .WithPath("/oauth2/authz"))
                .RespondWith(Response.Create()
                    .WithStatusCode(302)
                    .WithHeader("Location", "oob:EntClientIntegrationTests?code=1234"));
            var accessTokenResponse =
                "{" +
                    "\"access_token\":\"_Lx6aG2tX-M9Yc5Zs5LWfruHea4\"," +
                    "\"refresh_token\":\"03T7ZqLFok2PHHlCSlXzRgZjg5o\"," +
                    "\"id_token\":\"eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiI5NWY0NGZhZC02MzYwLTQ3NGMtYjhmYy03ZDY4MjAyZTIzODAiLCJhdWQiOiJTYW1wbGVBcHAiLCJpc3MiOiJodHRwczovL3NvbGlicmktYWNjb3VudC4xMGR1a2UuY29tOjQ0MyIsImV4cCI6MTUxNjE5NDM2NywiZ2l2ZW5fbmFtZSI6IlRlc3RlcjEiLCJpYXQiOjE1MTYxMDc5NjcsImZhbWlseV9uYW1lIjoiMTBEdWtlIiwiZW1haWwiOiJqYXJra28rdGVzdGVyMUAxMGR1a2UuY29tIn0.N2Lj-8vbr4IjMMd0IradiOjqIRz-IoyW8_-N4-3W_zM0BeFCdr8_3xpjttlZOSPTAoV6jA9l6higXJaSehQqaE0j3gYXYJHBfjMUVqoG2D8fscHrXU1W4ppkDwNzXOT8fuS008FsxHl7XnMo6a6iPPxyrLtDEcOX6M671ZFmYKwuv7CLGS310hRxtWz_lZnsFLTqSPZoPweRP5PlWQz5HAX3XnkZMCchZWm-JBGtARJwEzoXHYKOxNc-r_C44_EaxTplxDJ0SZ3h2F-UtBSjiPrjHazroRWHZg9C3fjqXiMNlmS48CCVbyr3Lp9fksysIZPMVQYnOGfXWtDfjFy2Ng\"," +
                    "\"token_type\":\"Bearer\"," +
                    "\"expires_in\":31536000" +
                "}";
            mockServer
                .Given(Request.Create()
                    .UsingPost()
                    .WithPath("/oauth2/access")
                    .WithBody("grant_type=authorization_code&code=1234&client_id=EntClientIntegrationTests&redirect_uri=oob%3aEntClientIntegrationTests&client_secret=VerySecret"))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(accessTokenResponse));

            // Test EntClient
            var instance = new EntClient();

            var oauthConfig = new AuthorizationCodeGrantConfig()
            {
                AuthzUri = new Uri(mockServerBaseUri, "/oauth2/authz").ToString(),
                TokenUri = new Uri(mockServerBaseUri, "/oauth2/access").ToString(),
                ClientID = "EntClientIntegrationTests",
                ClientSecret = "VerySecret",
                RedirectUri = "oob:EntClientIntegrationTests",
                Scope = "openid profile email",
                SignerKey = TestServerPublicKey
            };
            instance.OAuthConfig = oauthConfig;

            instance.AuthorizeSync();
            Assert.IsTrue(instance.IsAuthorized(), "Must have valid access token response after successful OAuth authorization");
        }
    }
}
