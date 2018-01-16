using NUnit.Framework;
using System;
using System.IO;
using Tenduke.EntitlementClient.Config;

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
        private static string oldWorkingDir;

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
        }

        [OneTimeTearDown]
        public static void Cleanup()
        {
            Environment.CurrentDirectory = oldWorkingDir;
            EntClient.Shutdown();
        }

        [Test]
        public void AuthorizeSync()
        {
            var instance = new EntClient();

            var oauthConfig = new AuthorizationCodeGrantConfig()
            {
                AuthzUri = "https://test/oauth2/oauth_authorization/",
                TokenUri = "https://test/oauth2/access_token/",
                ClientID = "EntClientIntegrationTests",
                ClientSecret = "VerySecret",
                RedirectUri = "oob:EntClientIntegrationTests",
                Scope = "openid profile email"
            };
            instance.OAuthConfig = oauthConfig;

            instance.AuthorizeSync();
        }
    }
}
