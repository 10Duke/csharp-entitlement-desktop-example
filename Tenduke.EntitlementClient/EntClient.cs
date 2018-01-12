using System;
using Tenduke.EntitlementClient.Authorization;
using Tenduke.EntitlementClient.Config;
using Tenduke.EntitlementClient.EntApi.Authz;
using Tenduke.EntitlementClient.Util;

namespace Tenduke.EntitlementClient
{
    /// <summary>
    /// Basic client for working directly against the 10Duke Entitlement service.
    /// This client uses the OAuth 2.0 Authorization Code Grant flow for authorizing
    /// this client directly against the 10Duke Entitlement service.
    /// </summary>
    public class EntClient
    {
        #region Private fields

        /// <summary>
        /// Internal flag tracking if CefSharp initialization has been done.
        /// </summary>
        private static bool cefSharpInitialized;

        /// <summary>
        /// Identifier of this system, used when communicating with the authorization
        /// endpoint of the 10Duke Entitlement service.
        /// </summary>
        private string computerId;

        /// <summary>
        /// Configuration for communicating with the <c>/authz/</c> API of the 10Duke Entitlement service.
        /// </summary>
        private AuthzApiConfig authzApiConfig;

        #endregion

        #region Properties

        /// <summary>
        /// OAuth 2.0 configuration to use for communicating with the 10Duke Entitlement service
        /// using the Authorization Code Grant flow.
        /// </summary>
        public AuthorizationCodeGrantConfig OAuthConfig { get; set; }

        /// <summary>
        /// Configuration for communicating with the <c>/authz/</c> API of the 10Duke Entitlement service.
        /// If not specified by explicitly setting this property value, default configuration is inferred from
        /// <see cref="OAuthConfig"/>.
        /// </summary>
        public AuthzApiConfig AuthzApiConfig
        {
            get
            {
                return authzApiConfig ?? AuthzApiConfig.FromOAuthConfig(OAuthConfig);
            }

            set
            {
                authzApiConfig = value;
            }
        }

        /// <summary>
        /// Configuration specifying how this system is identified when communicating with the authorization
        /// endpoint of the 10Duke Entitlement service. If <c>null</c>, default configuration for computer
        /// identity computing is used.
        /// </summary>
        public ComputerIdentityConfig ComputerIdentityConfig { get; set; }

        /// <summary>
        /// Gets the identifier of this system, used when communicating with the authorization
        /// endpoint of the 10Duke Entitlement service.
        /// </summary>
        public string ComputerId
        {
            get
            {
                var retValue = computerId;
                if (retValue == null)
                {
                    ComputerIdentity.ComputerIdentifier[] idComponents =
                        ComputerIdentityConfig == null || (ComputerIdentityConfig.ComputeBy == null && ComputerIdentityConfig.AdditionalIdentifier == null)
                        ? new[] { ComputerIdentity.ComputerIdentifier.BaseboardSerialNumber } // Uses BaseboardSerialNumber as default
                        : ComputerIdentityConfig.ComputeBy;
                    if (ComputerIdentityConfig == null)
                    {
                        retValue = ComputerIdentity.BuildComputerId(null, idComponents);
                    }
                    else if (ComputerIdentityConfig.ComputerId == null)
                    {
                        retValue = ComputerIdentity.BuildComputerId(ComputerIdentityConfig.AdditionalIdentifier, idComponents);
                    }
                    else
                    {
                        retValue = ComputerIdentityConfig.ComputerId;
                    }

                    computerId = retValue;
                }

                return retValue;
            }
        }

        /// <summary>
        /// Authorization process result information received from the 10Duke Entitlement service.
        /// </summary>
        public AuthorizationInfo Authorization { get; protected set; }

        /// <summary>
        /// Gets an <see cref="AuthzApi"/> object for accessing the <c>/authz/</c> API of the 10Duke Entitlement service.
        /// Please note that the OAuth authentication / authorization process must be successfully completed before
        /// getting the <see cref="AuthzApi"/> object.
        /// </summary>
        public AuthzApi AuthzApi
        {
            get
            {
                var authzApiConfig = AuthzApiConfig;
                if (authzApiConfig == null)
                {
                    throw new InvalidOperationException("Configuration for AuthzApi missing, please specify either AuthzApiConfig or OAuthConfig");
                }

                if (Authorization == null)
                {
                    throw new InvalidOperationException("OAuth authorization must be negotiated with the server before accessing the AuthzApi");
                }

                if (Authorization.Error != null)
                {
                    throw new InvalidOperationException(
                        string.Format("OAuth authorization has not been completed successfully (error code {0}, error message \"{1}\")",
                        Authorization.Error,
                        Authorization.ErrorDescription ?? ""));
                }

                return new AuthzApi()
                {
                    AuthzApiConfig = authzApiConfig,
                    AccessToken = Authorization.AccessTokenResponse,
                    ComputerId = ComputerId
                };
            }
        }

        #endregion

        #region Application-wide initialization and clean-up

        /// <summary>
        /// <para>Executes initialization necessary for using <see cref="EntClient"/> in an application.
        /// This method (any overload of this method) must be called once during application lifecycle,
        /// before using <see cref="EntClient"/> for the first time.</para>
        /// <para><see cref="EntClient"/> uses <see cref="https://github.com/cefsharp/CefSharp"/> for displaying
        /// an embedded browser window for operations that require displaying server-side user interface,
        /// especially a sign-on window. <see cref="EntClient"/> supports using <c>AnyCPU</c> as target architecture,
        /// and with <c>CefSharp</c> this means that loading unmanaged CefSharp resources is required. This
        /// method assumes that the required CefSharp dependencies can be found under <c>x84</c> or <c>x64</c>
        /// subdirectories.</para>
        /// <para>This overload uses the default settings, using
        /// <see cref="AppDomain.CurrentDomain.SetupInformation.ApplicationBase"/> as the base directory
        /// under which the architecture dependent CefSharp resource subdirectories must be found.</para>
        /// </summary>
        public static void Initialize()
        {
            Initialize(new CefSharpResolverArgs() { BaseDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase });
        }

        /// <summary>
        /// <para>Executes initialization necessary for using <see cref="EntClient"/> in an application.
        /// This method (any overload of this method) must be called once during application lifecycle,
        /// before using <see cref="EntClient"/> for the first time.</para>
        /// <para><see cref="EntClient"/> uses <see cref="https://github.com/cefsharp/CefSharp"/> for displaying
        /// an embedded browser window for operations that require displaying server-side user interface,
        /// especially a sign-on window. <see cref="EntClient"/> supports using <c>AnyCPU</c> as target architecture,
        /// and with <c>CefSharp</c> this means that loading unmanaged CefSharp resources is required. This
        /// method assumes that the required CefSharp dependencies can be found under <c>x84</c> or <c>x64</c>
        /// subdirectories.</para>
        /// </summary>
        /// <param name="resolverArgs">Arguments for customizing how CefSharp / cef resources are searched,
        /// or <c>null</c> for not initializing CefSharp.</param>
        public static void Initialize(CefSharpResolverArgs resolverArgs)
        {
            if (resolverArgs != null)
            {
                // Add default custom assembly resolver for loading unmanaged CefSharp dependencies
                // when running using AnyCPU. This default resolver attempts to load CefSharp assemblies
                // from x86 or x64 subdirectories. Add your own resolver before calling this method
                // if CefSharp is provided by other means.
                AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => CefSharpResolver.ResolveCefSharp(sender, eventArgs, resolverArgs);

                CefSharpUtil.InitializeCefSharp(resolverArgs);

                cefSharpInitialized = true;
            }
        }

        /// <summary>
        /// Cleans up resources required for using <see cref="EntClient"/> in an application.
        /// This method must be called once when <see cref="EntClient"/> is no more used by the application.
        /// </summary>
        public static void Shutdown()
        {
            if (cefSharpInitialized)
            {
                CefSharpUtil.ShutdownCefSharp();
                cefSharpInitialized = false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Checks if this client object currently contains a valid access token in <see cref="Authorization"/>.
        /// Access token is used for 10Duke Entitlement Service API requests.
        /// </summary>
        /// <returns><c>true</c> if authorized, <c>false</c> otherwise.</returns>
        public bool IsAuthorized()
        {
            return Authorization != null && Authorization.AccessTokenResponse != null;
        }

        /// <summary>
        /// Discards authorization information received from the server by setting <see cref="Authorization"/> to <c>null</c>.
        /// </summary>
        public void ClearAuthorization()
        {
            Authorization = null;
        }

        /// <summary>
        /// Starts the authorization process and waits for the process to complete before returning.
        /// When authorization has been completed, the <see cref="Authorization"/> property is populated
        /// and the access token in <see cref="AuthorizationInfo.AccessTokenResponse"/> is used for the
        /// subsequent API requests.
        /// </summary>
        public void AuthorizeSync()
        {
            if (OAuthConfig == null)
            {
                throw new InvalidOperationException("OAuthConfig must be specified");
            }

            var authorization = new AuthorizationCodeGrant() { OAuthConfig = OAuthConfig };
            var args = new AuthorizationCodeGrantArgs();
            authorization.AuthorizeSync(args);
            Authorization = authorization;
        }

        #endregion
    }
}
