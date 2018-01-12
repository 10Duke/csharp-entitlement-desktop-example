using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Tenduke.EntitlementClient;
using Tenduke.EntitlementClient.Config;
using Tenduke.EntitlementClient.EntApi;
using Tenduke.EntitlementClient.EntApi.Authz;
using Tenduke.EntitlementClient.Util;

namespace SampleApp
{
    /// <summary>
    /// Form that uses <see cref="Tenduke.EntitlementClient.EntClient"/> for the purpose of giving basic usage examples.
    /// </summary>
    public partial class SampleAppForm : Form
    {
        public static readonly RSA EntServerPublicKey = CryptoUtil.ReadRsaPublicKey(
            "-----BEGIN PUBLIC KEY-----\n"
            + "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA1wRc5dsWBbIJfxay3SYP\n"
            + "MYp/BaLEt0b26/QtwQbrKq6hgVH+euMWsSk6gf0GZiwHMFF+t8/WcsNOfcYMBEHV\n"
            + "mGWSFeYb63IcFN+v3h2580kANzuKuqYnBeBOCN56lJf8q5FOUYQKFVuX/bvEKp+L\n"
            + "1KkMErmIm9e5fkw70zCngxoXGK6qyWX01SPTVfd3UZdPv1H+VOoEpbDsI2yhg5xR\n"
            + "jFAAsqyTYvHQaixiJqqw/T8+2/ond8AlxpzCa1UK9x2l1lMezlwHTHXyPh2ZMpwe\n"
            + "lDBIosKLPHbaZyNwpU0iGOvrDJo8xlw4qGm/fClbaEWM8BCdbn/aKjWMN/t7FEaQ\n"
            + "TQIDAQAB\n"
            + "-----END PUBLIC KEY-----");

        /// <summary>
        /// OAuth 2.0 configuration for connecting this sample application to the 10Duke Entitlement service.
        /// </summary>
        public readonly AuthorizationCodeGrantConfig OAuthConfig = new AuthorizationCodeGrantConfig()
        {
            AuthzUri = "https://test-account.10duke.com/oauth2/authz/",
            TokenUri = "https://test-account.10duke.com/oauth2/access/",
            UserInfoUri = "https://test-account.10duke.com/userinfo/",
            ClientID = "SampleApp",
            ClientSecret = "YohSi-u3",
            RedirectUri = "oob:SampleApp",
            Scope = "openid profile email",
            SignerKey = EntServerPublicKey
        };

        /// <summary>
        /// <para>The <see cref="Tenduke.EntitlementClient.EntClient"/> instance used by this sample application.</para>
        /// <para>Please note that </para>
        /// </summary>
        protected EntClient EntClient { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleAppForm"/> class.
        /// </summary>
        public SampleAppForm()
        {
            InitializeComponent();
            comboBoxConsumeMode.SelectedIndex = 0;
            comboBoxResponseFormat.SelectedIndex = 0;
            listViewAuthorizationDecisions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            EntClient = new EntClient() { OAuthConfig = OAuthConfig };
        }

        /// <summary>
        /// Called when the form is first displayed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SampleAppForm_Shown(object sender, EventArgs e)
        {
            // This sample application always requires sign-on / authorization against the 10Duke entitlement service.
            // Call AuthorizeSync for running the sign-on process in an embedded browser opened by the EntClient instance.
            EntClient.AuthorizeSync();
            if (EntClient.IsAuthorized())
            {
                ShowWelcomeMessage();
                ShowComputerId();
            }
            else
            {
                // If the authorization process was cancelled, close this form. This will cause the whole application
                // to be closed.
                Close();
            }
        }

        /// <summary>
        /// Populates welcome message shown by this form using user attributes received from the 10Duke entitlement
        /// service in the received OpenID Connect ID token.
        /// </summary>
        private void ShowWelcomeMessage()
        {
            var name = (string)EntClient.Authorization.AccessTokenResponse.IDToken["name"];
            if (string.IsNullOrEmpty(name))
            {
                var givenName = (string)EntClient.Authorization.AccessTokenResponse.IDToken["given_name"];
                var familyName = (string)EntClient.Authorization.AccessTokenResponse.IDToken["family_name"];

                var builder = new StringBuilder();
                if (!string.IsNullOrEmpty(givenName))
                {
                    builder.Append(givenName);
                }
                if (!string.IsNullOrEmpty(familyName))
                {
                    if (builder.Length > 0)
                    {
                        builder.Append(' ');
                    }
                    builder.Append(familyName);
                }

                name = builder.Length == 0 ? null : builder.ToString();
            }

            name = name ?? "anonymous";
            labelWelcome.Text = string.Format("Welcome {0}", name);
        }

        /// <summary>
        /// Computes a computer id (identifier for this system) and displays it on the form.
        /// </summary>
        private void ShowComputerId()
        {
            textBoxComputerId.Text = EntClient.ComputerId;
        }

        /// <summary>
        /// Called when button for requesting an authorization decision is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void buttonRequestAuthorizationDecision_Click(object sender, EventArgs e)
        {
            var authorizedItem = textBoxAuthorizedItemName.Text;
            var responseType = ResponseType.FromExtension(comboBoxResponseFormat.Text);
            var consumeMode = comboBoxConsumeMode.Text;
            var consume = consumeMode == "consume";
            var authorizationDecision = EntClient.AuthzApi.CheckOrConsume(authorizedItem, consume, responseType);
            ShowAuthorizationDecision(authorizedItem, authorizationDecision);
        }

        /// <summary>
        /// Adds an item for the received <see cref="AuthorizationDecision"/> in the list view.
        /// </summary>
        /// <param name="authorizedItem">Name of the authorized item.</param>
        /// <param name="authorizationDecision"><see cref="AuthorizationDecision"/> object describing authorization decision
        /// response received for the authorized item.</param>
        private void ShowAuthorizationDecision(string authorizedItem, AuthorizationDecision authorizationDecision)
        {
            var listViewItem = BuildListViewItemForAuthorizationDecision(authorizedItem, authorizationDecision);
            listViewAuthorizationDecisions.Items.Add(listViewItem);
        }

        /// <summary>
        /// Builds a <see cref="ListViewItem"/> for displaying an <see cref="AuthorizationDecision"/> in the list view.
        /// </summary>
        /// <param name="authorizedItem">Name of the authorized item.</param>
        /// <param name="authorizationDecision"><see cref="AuthorizationDecision"/> object describing authorization decision
        /// response received for the authorized item.</param>
        /// <returns>The <see cref="ListViewItem"/>.</returns>
        private ListViewItem BuildListViewItemForAuthorizationDecision(string authorizedItem, AuthorizationDecision authorizationDecision)
        {
            return new AuthorizationDecisionListViewItem(authorizedItem, authorizationDecision);
        }

        /// <summary>
        /// Called when selection is changed in the list view.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void listViewAuthorizationDecisions_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableButtons();
        }

        /// <summary>
        /// Enables / disables buttons on the form.
        /// </summary>
        private void EnableButtons()
        {
            var selectedItem = (AuthorizationDecisionListViewItem)
                (listViewAuthorizationDecisions.SelectedItems.Count == 1
                ? listViewAuthorizationDecisions.SelectedItems[0]
                : null);

            bool releaseLicenseButtonEnabled = false;
            bool showDataButtonEnabled = false;

            if (selectedItem != null)
            {
                showDataButtonEnabled = true;
                if (selectedItem.Granted && selectedItem.AuthorizationDecision["jti"] != null)
                {
                    releaseLicenseButtonEnabled = true;
                }
            }

            buttonReleaseLicense.Enabled = releaseLicenseButtonEnabled;
            buttonShowData.Enabled = showDataButtonEnabled;
        }

        /// <summary>
        /// Called when the show authorization decision details button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void buttonShowData_Click(object sender, EventArgs e)
        {
            var selectedItem = (AuthorizationDecisionListViewItem)listViewAuthorizationDecisions.SelectedItems[0];
            var dataForm = new AuthorizationDecisionDetailsForm() { AuthorizationDecision = selectedItem.AuthorizationDecision };
            dataForm.ShowDialog();
        }

        /// <summary>
        /// Called when the release license button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void buttonReleaseLicense_Click(object sender, EventArgs e)
        {
            var selectedItem = (AuthorizationDecisionListViewItem)listViewAuthorizationDecisions.SelectedItems[0];
            var tokenId = (string)selectedItem.AuthorizationDecision["jti"];
            var response = EntClient.AuthzApi.ReleaseLicense(tokenId, ResponseType.JWT);
            bool successfullyReleased = response[tokenId] != null && (bool)response[tokenId] == true;
            bool noConsumptionFound = "noConsumptionFoundById" == (string)response[tokenId + "_errorCode"];
            if (successfullyReleased || noConsumptionFound)
            {
                listViewAuthorizationDecisions.Items.Remove(selectedItem);
            }
            else
            {
                MessageBox.Show(response.ToString(), "Error");
            }
        }

        /// <summary>
        /// Item for displaying an <see cref="AuthorizationDecision"/> in the list view and for storing data of the authorization decision.
        /// </summary>
        private class AuthorizationDecisionListViewItem : ListViewItem
        {
            /// <summary>
            /// Name of the authorized item.
            /// </summary>
            public string AuthorizedItem { get; set; }

            /// <summary>
            /// Flag indicating if authorization was granted.
            /// </summary>
            public bool Granted { get; set; }

            /// <summary>
            /// The authorization decision response from the server.
            /// </summary>
            public AuthorizationDecision AuthorizationDecision { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="AuthorizationDecisionListViewItem"/> class.
            /// </summary>
            /// <param name="authorizedItem">Name of the authorized item.</param>
            /// <param name="authorizationDecision"><see cref="AuthorizationDecision"/> object describing authorization decision
            /// response received for the authorized item.</param>
            public AuthorizationDecisionListViewItem(string authorizedItem, AuthorizationDecision authorizationDecision)
                : base(new string[] { authorizedItem, authorizationDecision[authorizedItem].ToString(), authorizationDecision.ToString() })
            {
                AuthorizedItem = authorizedItem;
                Granted = (bool)authorizationDecision[authorizedItem];
                AuthorizationDecision = authorizationDecision;
            }
        }
    }
}
