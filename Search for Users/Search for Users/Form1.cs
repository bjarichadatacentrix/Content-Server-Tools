using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Search_for_Users
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Holds the authentication ticket returned by OpenText
        /// Content Server after a successful login. This can be
        /// reused for subsequent API calls.
        /// </summary>
        private string? _authTicket;

        /// <summary>
        /// Initializes the login form and all UI components.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Login button click by validating input,
        /// attempting to log in to the content server, and updating
        /// the UI with a success or failure message.
        /// </summary>
        private async void btnLogin_Click(object? sender, EventArgs e)
        {
            // Reset message label to a neutral state before a new attempt.
            labelMessage.ForeColor = System.Drawing.Color.Red;
            labelMessage.Text = string.Empty;

            // Read values entered by the user.
            var url = txtUrl.Text?.Trim() ?? string.Empty;
            var username = txtUsername.Text?.Trim() ?? string.Empty;
            var password = txtPassword.Text ?? string.Empty;

            // Require all three fields to be filled in before trying to log in.
            if (string.IsNullOrWhiteSpace(url) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                labelMessage.Text = "Please enter URL, Username and Password.";
                return;
            }

            // Disable the button and show feedback while the request is in progress.
            btnLogin.Enabled = false;
            btnLogin.Text = "Logging in...";

            // Perform the actual login attempt against the content server.
            bool success = await TryLoginAsync(url, username, password);

            // Show a green success message or red failure message.
            if (success)
            {
                labelMessage.ForeColor = System.Drawing.Color.Green;
                labelMessage.Text = "Login Successful";

                // Navigate to the action selection screen after a successful login.
                var actionForm = new ActionSelectionForm(this);
                actionForm.Show();
                this.Hide();
            }
            else
            {
                labelMessage.ForeColor = System.Drawing.Color.Red;
                labelMessage.Text = "Login Unsuccessful";
            }

            // Re-enable the button and restore its text after the attempt finishes.
            btnLogin.Enabled = true;
            btnLogin.Text = "Login";
        }

        /// <summary>
        /// Tries to log in to an OpenText Content Server using the REST
        /// authentication endpoint `/api/v1/auth`. It POSTs the username
        /// and password and returns true only when a ticket is returned.
        /// The ticket value is stored in the private field `_authTicket`.
        /// </summary>
        private async Task<bool> TryLoginAsync(string url, string username, string password)
        {
            try
            {
                // Ensure the URL typed in the textbox is a valid absolute URI.
                if (!Uri.TryCreate(url, UriKind.Absolute, out var baseUri))
                {
                    return false;
                }

                // Build the full authentication URL for OpenText Content Server.
                // If the user already typed the full /api/v1/auth URL we use it as-is.
                // Otherwise we assume they entered the server URL and append the
                // standard OTCS path `/otcs/cs.exe/api/v1/auth`.
                Uri authUri;
                if (baseUri.AbsolutePath.Contains("/api/v1/auth", StringComparison.OrdinalIgnoreCase))
                {
                    authUri = baseUri;
                }
                else
                {
                    var builder = new UriBuilder(baseUri);

                    // Ensure the path ends with /otcs/cs.exe before we add /api/v1/auth.
                    var path = builder.Path.TrimEnd('/');
                    if (!path.EndsWith("/otcs/cs.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        path += "/otcs/cs.exe";
                    }

                    builder.Path = path + "/api/v1/auth";
                    authUri = builder.Uri;
                }

                // Create an HTTP client with a short timeout so the UI does not hang.
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                // Prepare the POST body expected by OTCS REST:
                // username=<user>&password=<password>
                using var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password)
                });

                // Send the POST request to /api/v1/auth.
                using var response = await client.PostAsync(authUri, content);

                // If the HTTP status is not successful, the login failed.
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // Read and parse the JSON response to extract the ticket.
                var json = await response.Content.ReadAsStringAsync();

                // Example successful response:
                // { "ticket": "ABC123...", ... }
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("ticket", out var ticketElement))
                {
                    var ticket = ticketElement.GetString();
                    if (!string.IsNullOrWhiteSpace(ticket))
                    {
                        // Store the ticket so it can be reused later.
                        _authTicket = ticket;
                        return true;
                    }
                }

                // If we reach here there was no usable ticket in the response.
                return false;
            }
            catch
            {
                // Any exception (network error, timeout, etc.) is treated as an unsuccessful login.
                return false;
            }
        }
    }
}
