using System;
using Tenduke.Client;
using Tenduke.Client.Authorization;
using Tenduke.Client.Config;
using Tenduke.Client.EntApi.Authz;
using Tenduke.EntitlementClient.Authorization;
using Tenduke.EntitlementClient.Config;
using Tenduke.EntitlementClient.Util;

namespace Tenduke.EntitlementClient
{
    /// <summary>
    /// Basic client for working directly against the 10Duke Entitlement service.
    /// This client uses the OAuth 2.0 Authorization Code Grant flow for authorizing
    /// this client directly against the 10Duke Entitlement service.
    /// </summary>
    public class EntClient : BaseClient<EntClient, IAuthorizationCodeGrantConfig>
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

        #endregion

        #region Properties

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
                        retValue = ComputerIdentity.BuildComputerId(null, null, idComponents);
                    }
                    else if (ComputerIdentityConfig.ComputerId == null)
                    {
                        retValue = ComputerIdentity.BuildComputerId(ComputerIdentityConfig.AdditionalIdentifier, ComputerIdentityConfig.Salt, idComponents);
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
        public AuthorizationInfo Authorization { get; set; }

        /// <summary>
        /// OAuth 2.0 access token for accessing APIs that require authorization.
        /// </summary>
        public new string AccessToken
        {
            get
            {
                return Authorization == null || Authorization.AccessTokenResponse == null ? null : Authorization.AccessTokenResponse.AccessToken;
            }

            set
            {
                throw new InvalidOperationException("AccessToken can not be set directly, set Authorization instead");
            }
        }

        /// <summary>
        /// Gets an <see cref="EntClientAuthorizationSerializer"/> for reading and writing <see cref="Authorization"/>
        /// of this object by binary serialization.
        /// </summary>
        public EntClientAuthorizationSerializer AuthorizationSerializer
        {
            get
            {
                return new EntClientAuthorizationSerializer() { EntClient = this };
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
        /// Discards authorization information received from the server by setting <see cref="Authorization"/> to <c>null</c>.
        /// </summary>
        public new void ClearAuthorization()
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
