using System;

namespace Search_for_Users
{
    /// <summary>
    /// Represents the supported actions that can be performed after
    /// logging into Content Server.
    /// </summary>
    public enum ContentServerAction
    {
        /// <summary>
        /// View/search members (GET /api/v1/members).
        /// </summary>
        SearchForUsers = 0,

        /// <summary>
        /// Create members (POST /api/v1/members).
        /// </summary>
        CreateUser = 1,

        /// <summary>
        /// Update members (PUT /api/v1/members/{user_id}).
        /// </summary>
        UpdateUser = 2,

        /// <summary>
        /// Delete members (DELETE /api/v1/members/{user_id}).
        /// </summary>
        DeleteUser = 3
    }

    /// <summary>
    /// Displays the screen that lets the user choose
    /// what they want to do after logging into the
    /// content server (for now, only "Search for Users").
    /// </summary>
    public partial class ActionSelectionForm : Form
    {
        // Reference to the login form so we can return to it
        // and access the Content Server authentication ticket.
        private readonly Form1 _loginForm;

        /// <summary>
        /// Creates the action selection form and remembers the
        /// login form instance so that the Back button can show it.
        /// </summary>
        public ActionSelectionForm(Form1 loginForm)
        {
            _loginForm = loginForm ?? throw new ArgumentNullException(nameof(loginForm));
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Back button click by closing this form
        /// and returning to the login screen.
        /// </summary>
        private void btnBack_Click(object? sender, EventArgs e)
        {
            _loginForm.Show();
            this.Close();
        }

        /// <summary>
        /// Handles the Next button click. When "Search for Users" is
        /// selected it opens the Search for Users form, otherwise it
        /// prompts the user to make a selection.
        /// </summary>
        private void btnNext_Click(object? sender, EventArgs e)
        {
            // Determine which action the user selected.
            var selectedAction =
                radioSearchForUsers.Checked ? ContentServerAction.SearchForUsers :
                radioCreateUser.Checked ? ContentServerAction.CreateUser :
                radioUpdateUser.Checked ? ContentServerAction.UpdateUser :
                radioDeleteUser.Checked ? ContentServerAction.DeleteUser :
                (ContentServerAction?)null;

            if (selectedAction is null)
            {
                MessageBox.Show(
                    "Please select an action before continuing.",
                    "Select Action",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Open the Search for Users screen and hide this one.
            var searchForm = new SearchForUsersForm(this, _loginForm, selectedAction.Value);
            searchForm.Show();
            this.Hide();
        }
    }
}

