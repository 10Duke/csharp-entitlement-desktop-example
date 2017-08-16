using JWT;
using JWT.Serializers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tenduke.EntitlementClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var defaultHardwareId = ComputeHardwareId();
            TextBoxHardwareId.Text = defaultHardwareId;
        }

        /// <summary>
        /// Open the URL entered in the URL text box in a web browser. Expects to receive an OAuth 2.0 Implicit Grant response eventually.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ButtonGetToken_Click(object sender, RoutedEventArgs e)
        {
            // URL of the server OAuth 2.0 authorization endpoint
            var oauthAuthzEndpointUrl = new Uri(TextBoxAuthzUrl.Text);

            // parameters for the OAuth 2.0 Implicit Grant Flow
            var clientId = TextBoxClientId.Text;
            var scope = TextBoxOAuthScope.Text;

            // build full URL for connecting to the server and getting the OAuth token
            var oauthUrl = BuildOAuthUrl(oauthAuthzEndpointUrl, clientId, scope, WebBrowserWindow.APP_REDIRECT_URL);

            // open modal browser window, read token if closed successfully
            var webBrowserWindow = new WebBrowserWindow { Owner = this };
            var dialogResult = webBrowserWindow.ShowAuthzDialog(oauthUrl);
            if (true == dialogResult)
            {
                TextBoxToken.Text = webBrowserWindow.OAuthResponse;
            }
        }

        /// <summary>
        /// Builds Uri for OAuth 2.0 Implicit Grant call against the server OAuth 2.0 authorization endpoint.
        /// </summary>
        /// <param name="oauthAuthzEndpointUrl">URL of the server OAuth 2.0 authorization endpoint.</param>
        /// <param name="clientId">OAuth 2.0 client id.</param>
        /// <param name="scope">OAuth 2.0 scope (space separated scope values).</param>
        /// <param name="redirectUrl">URL that where the server is expected to redirect the browser when the OAuth process is ready.</param>
        /// <returns></returns>
        private Uri BuildOAuthUrl(Uri oauthAuthzEndpointUrl, string clientId, string scope, string redirectUrl)
        {
            var uriBuilder = new UriBuilder(oauthAuthzEndpointUrl);
            var baseQuery = uriBuilder.Query == null || uriBuilder.Query.Length <= 1 ? string.Empty : uriBuilder.Query.Substring(1) + "&";
            var queryBuilder = new StringBuilder(baseQuery);
            queryBuilder.Append("response_type=token");
            queryBuilder.Append("&client_id=").Append(HttpUtility.UrlEncode(clientId));
            queryBuilder.Append("&scope=").Append(HttpUtility.UrlEncode(scope));
            queryBuilder.Append("&redirect_uri=").Append(HttpUtility.UrlEncode(redirectUrl));
            uriBuilder.Query = queryBuilder.ToString();
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Handles button click for the button sends authorization check or license consumption request
        /// to the server entitlement endpoint.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ButtonAuthz_Click(object sender, RoutedEventArgs e)
        {
            var accessToken = ReadAccessToken();

            // URL of the server entitlement endpoint
            var entEndpointUrl = new Uri(TextBoxEntUrl.Text);

            // parameters for the entitlement request
            var authzQuery = TextBoxAuthzQuery.Text;
            var hwId = TextBoxHardwareId.Text;
            var consume = CheckBoxConsume.IsChecked.GetValueOrDefault();
            var consumptionMode = ComboBoxConsumptionMode.Text;
            var durationText = Regex.Replace(TextBoxConsumeDuration.Text, @"\s+", "");
            var duration = string.IsNullOrEmpty(durationText) ? (long?)null : long.Parse(durationText);

            // build full URL for an entitlement request
            var entUrl = BuildEntUrl(entEndpointUrl, authzQuery, hwId, consume, consumptionMode, duration);

            // HTTP entitlement request
            var entRequest = WebRequest.CreateHttp(entUrl);
            if (consume)
            {
                entRequest.Method = "POST";
            }
            entRequest.Headers.Add("Authorization", "Bearer " + accessToken);
            using (var response = entRequest.GetResponse())
            {
                using (var responseReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseData = responseReader.ReadToEnd();
                    TextBoxRawResult.Text = responseData;
                }
            }
        }

        /// <summary>
        /// Reads OAuth 2.0 access token from the token text box.
        /// </summary>
        /// <returns>The access token value.</returns>
        private string ReadAccessToken()
        {
            var retValue = TextBoxToken.Text;
            var accessTokenMatch = Regex.Match(retValue, @"access_token=([^&]+)");
            if (accessTokenMatch.Success)
            {
                retValue = accessTokenMatch.Groups[1].Value;
            }
            return retValue;
        }

        /// <summary>
        /// Builds URL for sending authorization check or license consumption request to the server entitlement endpoint.
        /// </summary>
        /// <param name="entEndpointUrl">URL of the server endpoint for the authorization / entitlement request.</param>
        /// <param name="authzQuery">Names of items to authorize, ampersand separated (e.g. MyLicense1&MyLicense2).</param>
        /// <param name="hwId">Unique identifier of the client / hardware doing the authorization / license request.</param>
        /// <param name="consume">If false, only checks if authorization is granted. If true, consumes a license.</param>
        /// <param name="consumptionMode">Consumption mode, <c>cache</c> or <c>checkOut</c>.</param>
        /// <param name="duration">Consumption duration in milliseconds, or <c>null</c> for server default.</param>
        /// <returns>Uri for calling the authz endpoint.</returns>
        private Uri BuildEntUrl(Uri entEndpointUrl, string authzQuery, string hwId, bool consume, string consumptionMode, long? duration)
        {
            var uriBuilder = new UriBuilder(entEndpointUrl);
            var baseQuery = uriBuilder.Query == null || uriBuilder.Query.Length <= 1 ? string.Empty : uriBuilder.Query.Substring(1) + "&";
            var queryBuilder = new StringBuilder(baseQuery);
            queryBuilder.Append("doConsume=").Append(consume ? "true" : "false");
            if (!string.IsNullOrEmpty(hwId))
            {
                queryBuilder.Append("&hw=").Append(HttpUtility.UrlEncode(hwId));
            }
            if (consume)
            {
                queryBuilder.Append("&consumptionMode=").Append(consumptionMode);
                if (duration != null)
                {
                    queryBuilder.Append("&consumptionDuration=").Append(duration);
                }
            }

            // assuming here that URL encoding is not needed, i.e. that authorized item names are URL safe
            queryBuilder.Append("&").Append(authzQuery);
            uriBuilder.Query = queryBuilder.ToString();
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Handles checking the checkbox that is used for controlling whether
        /// authorization / entitlement request is just a check or license consumption.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CheckBoxConsume_Checked(object sender, RoutedEventArgs e)
        {
            CheckBoxConsume_ChangeChecked(true, sender, e);
        }

        /// <summary>
        /// Handles unchecking the checkbox that is used for controlling whether
        /// authorization / entitlement request is just a check or license consumption.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CheckBoxConsume_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBoxConsume_ChangeChecked(false, sender, e);
        }

        /// <summary>
        /// Handles checking or unchecking the checkbox that is used for controlling whether
        /// authorization / entitlement request is just a check or license consumption.
        /// </summary>
        /// <param name="consume"><c>true</c> if the checkbox has been set to checked state and
        /// the request should consume license, <c>false</c> otherwise.</param>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CheckBoxConsume_ChangeChecked(bool consume, object sender, RoutedEventArgs e)
        {
            LabelConsumptionMode.IsEnabled = consume;
            ComboBoxConsumptionMode.IsEnabled = consume;
            LabelConsumeDuration.IsEnabled = consume;
            TextBoxConsumeDuration.IsEnabled = consume;
            ButtonAuthz.Content = consume ? "Consume" : "Check";
        }

        /// <summary>
        /// Handles text input in the consume duration text box, validating that the entered text represent an integer value.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void TextBoxConsumeDuration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!TextBoxConsumeDuration_ValidateInput(e.Text))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Validates text input in the consume duration text box.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <returns><c>true</c> if the given input is valid, <c>false</c> otherwise.</returns>
        private bool TextBoxConsumeDuration_ValidateInput(string text)
        {
            var regex = new Regex("^[0-9]*$");
            return regex.IsMatch(text);
        }

        /// <summary>
        /// Handles value change of text box displaying raw authz / entitlement response.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void TextBoxRawResult_TextChanged(object sender, TextChangedEventArgs e)
        {
            string parsedPayload;
            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                var responses = TextBoxRawResult.Text.Split('&');
                var serializedResponses = new StringBuilder();
                foreach (var response in responses)
                {
                    var parsedPayloadObj = decoder.DecodeToObject(response, (byte[])null, false);
                    var serialized = JsonConvert.SerializeObject(parsedPayloadObj, Formatting.Indented);
                    if (serializedResponses.Length > 0)
                    {
                        serializedResponses.AppendLine();
                    }
                    serializedResponses.Append(serialized);
                }
                parsedPayload = serializedResponses.ToString();
            }
            catch (Exception ex)
            {
                parsedPayload = ex.ToString();
            }

            TextBoxParsedPayload.Text = parsedPayload;
        }

        /// <summary>
        /// Computes a hardware id for the purpose of the test application. This sample implementation simply uses
        /// SHA-1 hash of baseboard serial number. In real implementations, more data could be added before computing
        /// the hash. The data to add is always dependent on customer requirements, but may include data related
        /// to hardware, OS or the user etc.
        /// </summary>
        /// <returns>Hardware id as a string.</returns>
        private string ComputeHardwareId()
        {
            var baseString = new StringBuilder();

            var mngmntObjectSearcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            var mngmntObjects = mngmntObjectSearcher.Get();
            foreach (var mngmntObject in mngmntObjects)
            {
                baseString.Append(mngmntObject["SerialNumber"].ToString());
            }

            string retValue;
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(baseString.ToString()));
                // URL safe Base64
                retValue = Convert.ToBase64String(hash).TrimEnd('=').Replace("+", "-").Replace("/", "_");
            }

            return retValue;
        }
    }
}
