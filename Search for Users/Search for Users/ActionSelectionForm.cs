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
        DeleteUser = 3,

        /// <summary>
        /// Search groups (GET /api/v2/members).
        /// </summary>
        SearchGroups = 4,

        /// <summary>
        /// Create groups (POST /api/v2/members).
        /// </summary>
        CreateGroups = 5,

        /// <summary>
        /// Create subgroups (POST /api/v2/members with parent_id).
        /// </summary>
        CreateSubGroups = 6,

        /// <summary>
        /// Update groups (PUT /api/v2/members/{group_id}).
        /// </summary>
        UpdateGroups = 7,

        /// <summary>
        /// Delete group (DELETE /api/v2/members/{group_id}).
        /// </summary>
        DeleteGroup = 8,

        /// <summary>
        /// Add user to group (POST /api/v2/members/{group_id}/members).
        /// </summary>
        AddUserToGroup = 9,

        /// <summary>
        /// Remove user from group (DELETE /api/v2/members/{group_id}/members/{member_id}).
        /// </summary>
        RemoveUserFromGroup = 10,

        /// <summary>
        /// Search user by ID (GET /otdsws/rest/users/{user_id}).
        /// </summary>
        SearchUserById = 11
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
        /// When a Users radio button is selected, uncheck all Groups and Users^Groups radio buttons.
        /// </summary>
        private void UsersRadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked)
            {
                radioSearchGroups.Checked = false;
                radioCreateGroups.Checked = false;
                radioCreateSubGroups.Checked = false;
                radioUpdateGroups.Checked = false;
                radioDeleteGroup.Checked = false;
                radioAddUserToGroup.Checked = false;
                radioRemoveUserFromGroup.Checked = false;
            }
        }

        /// <summary>
        /// Unchecks all other radio buttons when Search User by ID is selected.
        /// </summary>
        private void SearchUserByIdRadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked)
            {
                // Uncheck Users (except this one)
                radioSearchForUsers.Checked = false;
                radioCreateUser.Checked = false;
                radioUpdateUser.Checked = false;
                radioDeleteUser.Checked = false;
                // Uncheck Groups
                radioSearchGroups.Checked = false;
                radioCreateGroups.Checked = false;
                radioCreateSubGroups.Checked = false;
                radioUpdateGroups.Checked = false;
                radioDeleteGroup.Checked = false;
                // Uncheck Users Groups
                radioAddUserToGroup.Checked = false;
                radioRemoveUserFromGroup.Checked = false;
            }
        }

        /// <summary>
        /// When a Groups radio button is selected, uncheck all Users and Users^Groups radio buttons.
        /// </summary>
        private void GroupsRadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked)
            {
                radioSearchForUsers.Checked = false;
                radioCreateUser.Checked = false;
                radioUpdateUser.Checked = false;
                radioDeleteUser.Checked = false;
                radioAddUserToGroup.Checked = false;
                radioRemoveUserFromGroup.Checked = false;
            }
        }

        /// <summary>
        /// When a Users^Groups radio button is selected, uncheck all Users and Groups radio buttons.
        /// </summary>
        private void UsersGroupsRadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked)
            {
                // Uncheck Users
                radioSearchForUsers.Checked = false;
                radioCreateUser.Checked = false;
                radioUpdateUser.Checked = false;
                radioDeleteUser.Checked = false;
                // Uncheck Groups
                radioSearchGroups.Checked = false;
                radioCreateGroups.Checked = false;
                radioCreateSubGroups.Checked = false;
                radioUpdateGroups.Checked = false;
                radioDeleteGroup.Checked = false;
            }
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
                radioSearchUserById.Checked ? ContentServerAction.SearchUserById :
                radioCreateUser.Checked ? ContentServerAction.CreateUser :
                radioUpdateUser.Checked ? ContentServerAction.UpdateUser :
                radioDeleteUser.Checked ? ContentServerAction.DeleteUser :
                radioSearchGroups.Checked ? ContentServerAction.SearchGroups :
                radioCreateGroups.Checked ? ContentServerAction.CreateGroups :
                radioCreateSubGroups.Checked ? ContentServerAction.CreateSubGroups :
                radioUpdateGroups.Checked ? ContentServerAction.UpdateGroups :
                radioDeleteGroup.Checked ? ContentServerAction.DeleteGroup :
                radioAddUserToGroup.Checked ? ContentServerAction.AddUserToGroup :
                radioRemoveUserFromGroup.Checked ? ContentServerAction.RemoveUserFromGroup :
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

