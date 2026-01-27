using System;

namespace Search_for_Users
{
    /// <summary>
    /// Displays the screen that lets the user choose
    /// what they want to do after logging into the
    /// content server (for now, only "Search for Users").
    /// </summary>
    public partial class ActionSelectionForm : Form
    {
        // Reference to the login form so we can return to it.
        private readonly Form _loginForm;

        /// <summary>
        /// Creates the action selection form and remembers the
        /// login form instance so that the Back button can show it.
        /// </summary>
        public ActionSelectionForm(Form loginForm)
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
        /// Handles the Next button click. For now this is just a
        /// placeholder and will be filled in when we know what
        /// should happen after choosing "Search for Users".
        /// </summary>
        private void btnNext_Click(object? sender, EventArgs e)
        {
            // TODO: Implement what should happen after clicking Next
            // when "Search for Users" is selected.
        }
    }
}

