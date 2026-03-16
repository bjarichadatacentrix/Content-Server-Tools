using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Search_for_Users
{
    /// <summary>
    /// Displays a report of user data extracted from the log file.
    /// Shows userPartitionID, name, surname, displayName, and mail columns in a DataGridView.
    /// For Delete User action, shows a plain single-column view with log entries.
    /// </summary>
    public partial class ReportForm : Form
    {
        private readonly string _logFilePath;
        private readonly ContentServerAction _selectedAction;
        private readonly bool _enableFiltering;
        
        // Filter controls for Search for Users
        private Panel? _filterPanel;
        private TextBox? _filterUserId;
        private ComboBox? _filterPartition;
        private TextBox? _filterName;
        private TextBox? _filterSurname;
        private TextBox? _filterDisplayName;
        private TextBox? _filterMail;
        private TextBox? _filterCn;
        private Button? _btnFilter;
        private Button? _btnClearFilter;

        // Filter controls for Search Groups
        private TextBox? _filterGroupId;
        private TextBox? _filterGroupName;
        private TextBox? _filterGroupDateCreated;
        
        // Store original data for filtering
        private List<UserRecord>? _allUsers;
        private List<GroupRecord>? _allGroups;

        // Protect selection on Edit Data: only Clear Screen clears it; clicking to edit does not reduce selection
        private HashSet<int> _protectedSelectionIndices = new HashSet<int>();
        private bool _isUpdatingSelection;

        /// <summary>True when report shows user grid and uses the same Edit Data behavior (Search for Users, Search User by ID, Create User, Update User).</summary>
        private bool IsUserSearchReport => _selectedAction == ContentServerAction.SearchForUsers
            || _selectedAction == ContentServerAction.SearchUserById
            || _selectedAction == ContentServerAction.CreateUser
            || _selectedAction == ContentServerAction.UpdateUser;

        /// <summary>True when report shows the Search Groups grid with Group ID, Group Name, Date Created.</summary>
        private bool IsGroupSearchReport => _selectedAction == ContentServerAction.SearchGroups;

        /// <summary>
        /// Creates the report form with the specified log file and title.
        /// </summary>
        /// <param name="logFilePath">Path to the log file to parse.</param>
        /// <param name="title">Title to display in the form's title bar.</param>
        /// <param name="selectedAction">The action that generated the log file.</param>
        /// <param name="enableFiltering">Whether to enable filtering UI.</param>
        public ReportForm(string logFilePath, string title, ContentServerAction selectedAction = ContentServerAction.SearchForUsers, bool enableFiltering = false)
        {
            _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
            _selectedAction = selectedAction;
            _enableFiltering = enableFiltering;

            InitializeComponent();

            // Allow users to highlight one or more rows for targeted export/generation.
            dataGridViewReport.MultiSelect = true;
            dataGridViewReport.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            this.Text = title;
            
            // Set up the "Generate Input File" dropdown based on the action
            SetupGenerateOptions();
            
            LoadLogFile();
            
            // Make the grid editable for Search Users / Search Groups when opened via "Edit Data"
            if (_enableFiltering)
            {
                MakeGridEditable();
            }
            
            // Add filter UI if filtering is enabled for supported actions
            if (_enableFiltering && IsUserSearchReport)
            {
                AddFilterControls();
                PopulatePartitionFilter(); // Fill partition dropdown from report data (after combo exists)
            }
            else if (_enableFiltering && IsGroupSearchReport)
            {
                AddGroupFilterControls();
            }

            // Clear any default row selection when form is shown, so Filter only selects matching rows
            if (_enableFiltering && (IsUserSearchReport || IsGroupSearchReport))
            {
                Shown += ReportForm_Shown;
            }
        }

        private void ReportForm_Shown(object? sender, EventArgs e)
        {
            Shown -= ReportForm_Shown;
            if (_enableFiltering && (IsUserSearchReport || IsGroupSearchReport))
            {
                dataGridViewReport.ClearSelection();
                dataGridViewReport.CurrentCell = null;
                UpdateRecordCountLabel();
            }
        }

        /// <summary>
        /// Populates the "Generate Input File" dropdown based on the selected action.
        /// Only relevant options are shown.
        /// </summary>
        private void SetupGenerateOptions()
        {
            cboGenerateOption.Items.Clear();
            cboGenerateOption.Items.Add("Select Option");

            if (IsUserSearchReport)
            {
                cboGenerateOption.Items.Add("Update Users");
                cboGenerateOption.Items.Add("Delete User");
            }
            else if (_selectedAction == ContentServerAction.SearchGroups)
            {
                cboGenerateOption.Items.Add("Update Groups");
                cboGenerateOption.Items.Add("Delete Group");
            }

            cboGenerateOption.SelectedIndex = 0;

            // Hide the controls entirely if no generate options apply
            var hasOptions = cboGenerateOption.Items.Count > 1;
            cboGenerateOption.Visible = hasOptions;
            btnGenerateInputFile.Visible = hasOptions;
        }

        /// <summary>
        /// Makes the DataGridView editable for Search Users / Search Groups.
        /// User ID (or Group ID) column stays read-only.
        /// </summary>
        private void MakeGridEditable()
        {
            dataGridViewReport.ReadOnly = false;

            if (IsUserSearchReport)
            {
                foreach (DataGridViewColumn col in dataGridViewReport.Columns)
                {
                    // User ID, Account Locked, and Domain stay read-only; all other columns are editable
                    col.ReadOnly = col.Name == "colUserId" || col.Name == "colAccountLocked" || col.Name == "colDomain";
                }
            }
            else if (_selectedAction == ContentServerAction.SearchGroups)
            {
                foreach (DataGridViewColumn col in dataGridViewReport.Columns)
                {
                    // Group ID stays read-only; all other columns are editable
                    col.ReadOnly = col.Name == "colGroupId" || col.Name == "colDateCreated";
                }
            }
        }


        /// colDateCreated
        /// <summary>
        /// Enables the Generate Input File button only when a valid option is selected.
        /// </summary>
        private void cboGenerateOption_SelectedIndexChanged(object? sender, EventArgs e)
        {
            btnGenerateInputFile.Enabled = cboGenerateOption.SelectedIndex > 0;
        }

        /// <summary>
        /// Generates a CSV input file from the grid data, using OTDS field names as headers
        /// so the file can be directly used with the Update Users or Update Groups actions.
        /// </summary>
        private void btnGenerateInputFile_Click(object? sender, EventArgs e)
        {
            var selectedOption = cboGenerateOption.SelectedItem?.ToString() ?? string.Empty;

            if (selectedOption == "Update Users")
            {
                GenerateUpdateUsersCsv();
            }
            else if (selectedOption == "Delete User")
            {
                GenerateDeleteUsersCsv();
            }
            else if (selectedOption == "Update Groups")
            {
                GenerateUpdateGroupsCsv();
            }
            else if (selectedOption == "Delete Group")
            {
                GenerateDeleteGroupsCsv();
            }
        }

        /// <summary>
        /// Clears all currently highlighted rows/cells in the grid (same as Clear Screen for Edit Data).
        /// </summary>
        private void btnClearSelected_Click(object? sender, EventArgs e)
        {
            _isUpdatingSelection = true;
            _protectedSelectionIndices.Clear();
            dataGridViewReport.ClearSelection();
            dataGridViewReport.CurrentCell = null;
            _isUpdatingSelection = false;
            if (_enableFiltering)
                UpdateRecordCountLabel();
        }

        /// <summary>
        /// Moves all selected rows to the top of the grid; keeps selection unchanged.
        /// Supports both user and group reports.
        /// </summary>
        private void BtnSortData_Click(object? sender, EventArgs e)
        {
            if (!_enableFiltering) return;

            // Collect currently selected row indices in grid order
            var selectedIndices = new HashSet<int>(
                dataGridViewReport.SelectedRows.Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow && r.Index >= 0)
                    .Select(r => r.Index));

            if (selectedIndices.Count == 0) return;

            if (IsUserSearchReport && _allUsers != null)
            {
                var selectedRecords = new List<UserRecord>();
                var unselectedRecords = new List<UserRecord>();
                for (var i = 0; i < _allUsers.Count; i++)
                {
                    if (selectedIndices.Contains(i))
                        selectedRecords.Add(_allUsers[i]);
                    else
                        unselectedRecords.Add(_allUsers[i]);
                }

                var newOrder = new List<UserRecord>(selectedRecords);
                newOrder.AddRange(unselectedRecords);
                _allUsers = newOrder;

                _isUpdatingSelection = true;
                // Remove all rows so data starts directly under headers (avoid empty row at top)
                var wasAllowAdd = dataGridViewReport.AllowUserToAddRows;
                dataGridViewReport.AllowUserToAddRows = false;
                while (dataGridViewReport.Rows.Count > 0)
                    dataGridViewReport.Rows.RemoveAt(dataGridViewReport.Rows.Count - 1);
                foreach (var user in _allUsers)
                {
                    dataGridViewReport.Rows.Add(
                        user.UserId,
                        user.UserPartitionID,
                        user.Name,
                        user.Surname,
                        user.DisplayName,
                        user.Mail,
                        user.Cn,
                        user.AccountLocked,
                        user.Domain);
                }
                dataGridViewReport.AllowUserToAddRows = wasAllowAdd;
                var countUsers = selectedRecords.Count;
                for (var i = 0; i < countUsers && i < dataGridViewReport.Rows.Count; i++)
                {
                    if (!dataGridViewReport.Rows[i].IsNewRow)
                        dataGridViewReport.Rows[i].Selected = true;
                }
                _protectedSelectionIndices = new HashSet<int>(Enumerable.Range(0, countUsers));
                _isUpdatingSelection = false;
                UpdateRecordCountLabel();
            }
            else if (IsGroupSearchReport && _allGroups != null)
            {
                var selectedRecords = new List<GroupRecord>();
                var unselectedRecords = new List<GroupRecord>();
                for (var i = 0; i < _allGroups.Count; i++)
                {
                    if (selectedIndices.Contains(i))
                        selectedRecords.Add(_allGroups[i]);
                    else
                        unselectedRecords.Add(_allGroups[i]);
                }

                var newOrder = new List<GroupRecord>(selectedRecords);
                newOrder.AddRange(unselectedRecords);
                _allGroups = newOrder;

                _isUpdatingSelection = true;
                var wasAllowAdd = dataGridViewReport.AllowUserToAddRows;
                dataGridViewReport.AllowUserToAddRows = false;
                while (dataGridViewReport.Rows.Count > 0)
                    dataGridViewReport.Rows.RemoveAt(dataGridViewReport.Rows.Count - 1);
                foreach (var group in _allGroups)
                {
                    dataGridViewReport.Rows.Add(group.GroupId, group.GroupName, group.DateCreated);
                }
                dataGridViewReport.AllowUserToAddRows = wasAllowAdd;
                var countGroups = selectedRecords.Count;
                for (var i = 0; i < countGroups && i < dataGridViewReport.Rows.Count; i++)
                {
                    if (!dataGridViewReport.Rows[i].IsNewRow)
                        dataGridViewReport.Rows[i].Selected = true;
                }
                _protectedSelectionIndices = new HashSet<int>(Enumerable.Range(0, countGroups));
                _isUpdatingSelection = false;
                UpdateRecordCountLabel();
            }
        }

        /// <summary>
        /// Clears the screen of any selected rows (clears grid selection).
        /// This is the only way to clear the "held" selection for export.
        /// </summary>
        private void btnClearScreen_Click(object? sender, EventArgs e)
        {
            _isUpdatingSelection = true;
            _protectedSelectionIndices.Clear();
            dataGridViewReport.ClearSelection();
            dataGridViewReport.CurrentCell = null;
            _isUpdatingSelection = false;
            if (_enableFiltering)
                UpdateRecordCountLabel();
        }

        /// <summary>
        /// Generates a CSV file for the "Update Users" action.
        /// Headers use OTDS field names: user_id, displayName, mail, givenName, sn, cn
        /// </summary>
        private void GenerateUpdateUsersCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Generate Update Users Input File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"update_users_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);

                // OTDS field name headers that match what UpdateOtdsUserFromCsvAsync expects
                writer.WriteLine("user_id,displayName,mail,givenName,sn,cn");

                foreach (var row in GetRowsForExport())
                {
                    // Map grid columns to OTDS field names
                    // Grid: User ID | User Partition ID | Name | Surname | Display Name | Mail | CN
                    var userId = EscapeCsvField(row.Cells[0].Value?.ToString() ?? string.Empty);
                    var displayName = EscapeCsvField(row.Cells[4].Value?.ToString() ?? string.Empty);
                    var mail = EscapeCsvField(row.Cells[5].Value?.ToString() ?? string.Empty);
                    var givenName = EscapeCsvField(row.Cells[2].Value?.ToString() ?? string.Empty);
                    var sn = EscapeCsvField(row.Cells[3].Value?.ToString() ?? string.Empty);
                    var cn = EscapeCsvField(row.Cells[6].Value?.ToString() ?? string.Empty);

                    writer.WriteLine($"{userId},{displayName},{mail},{givenName},{sn},{cn}");
                }

                MessageBox.Show(
                    $"Update Users input file saved to:\n{dialog.FileName}",
                    "File Generated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error generating file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Generates a CSV file for the "Delete User" action.
        /// Contains only header "user_id" and one column with user IDs from selected rows.
        /// </summary>
        private void GenerateDeleteUsersCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Generate Delete Users Input File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"delete_users_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var rows = GetRowsForExport();
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);

                writer.WriteLine("user_id");

                foreach (var row in rows)
                {
                    var userId = EscapeCsvField(row.Cells[0].Value?.ToString() ?? string.Empty);
                    writer.WriteLine(userId);
                }

                MessageBox.Show(
                    $"Delete Users input file saved to:\n{dialog.FileName}",
                    "File Generated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error generating file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Generates a CSV file for the "Delete Group" action.
        /// Contains only header "Group_id" and one column with group IDs from selected rows.
        /// </summary>
        private void GenerateDeleteGroupsCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Generate Delete Groups Input File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"delete_groups_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var rows = GetRowsForExport();
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);

                writer.WriteLine("Group_id");

                foreach (var row in rows)
                {
                    var groupId = EscapeCsvField(row.Cells[0].Value?.ToString() ?? string.Empty);
                    writer.WriteLine(groupId);
                }

                MessageBox.Show(
                    $"Delete Groups input file saved to:\n{dialog.FileName}",
                    "File Generated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error generating file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Generates a CSV file for the "Update Groups" action.
        /// Headers use OTDS field names: group_id, cn, description
        /// </summary>
        private void GenerateUpdateGroupsCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Generate Update Groups Input File",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"update_groups_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);

                // OTDS field name headers that match what UpdateOtdsGroupFromCsvAsync expects
                // Grid: Group ID | Group Name | Date Created
                writer.WriteLine("group_id,cn");

                foreach (var row in GetRowsForExport())
                {
                    var groupId = EscapeCsvField(row.Cells[0].Value?.ToString() ?? string.Empty);
                    var cn = EscapeCsvField(row.Cells[1].Value?.ToString() ?? string.Empty);

                    writer.WriteLine($"{groupId},{cn}");
                }

                MessageBox.Show(
                    $"Update Groups input file saved to:\n{dialog.FileName}",
                    "File Generated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error generating file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Escapes a value for inclusion in a CSV field.
        /// Wraps in quotes if it contains commas, quotes, or newlines.
        /// </summary>
        private static string EscapeCsvField(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        /// <summary>
        /// Returns rows for export/generation:
        /// 1) highlighted rows (or rows owning highlighted cells), else
        /// 2) all currently visible rows.
        /// </summary>
        private List<DataGridViewRow> GetRowsForExport()
        {
            var selectedRows = dataGridViewReport.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .OrderBy(r => r.Index)
                .ToList();

            if (selectedRows.Count == 0 && dataGridViewReport.SelectedCells.Count > 0)
            {
                selectedRows = dataGridViewReport.SelectedCells
                    .Cast<DataGridViewCell>()
                    .Select(c => c.OwningRow)
                    .Where(r => r != null && !r.IsNewRow)
                    .Distinct()
                    .OrderBy(r => r.Index)
                    .ToList();
            }

            if (selectedRows.Count > 0)
            {
                return selectedRows;
            }

            return dataGridViewReport.Rows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow && r.Visible)
                .OrderBy(r => r.Index)
                .ToList();
        }

        /// <summary>
        /// Loads the log file, extracts JSON blocks, parses user data,
        /// and populates the DataGridView with user information.
        /// For Delete User action, shows a plain single-column view with log entries.
        /// </summary>
        private void LoadLogFile()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    MessageBox.Show(
                        $"Log file not found: {_logFilePath}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                var logContent = File.ReadAllText(_logFilePath);

                // For Delete User, Delete Group, or Remove User from Group,
                // show a plain single-column view with log entries
                if (_selectedAction == ContentServerAction.DeleteUser || 
                    _selectedAction == ContentServerAction.DeleteGroup ||
                    _selectedAction == ContentServerAction.RemoveUserFromGroup)
                {
                    LoadPlainLogView(logContent);
                    return;
                }

                // For Add User to Group, show User and Group columns
                if (_selectedAction == ContentServerAction.AddUserToGroup)
                {
                    LoadAddUserToGroupView(logContent);
                    return;
                }

                // For Search Groups, show Group Name and Date Created columns
                if (_selectedAction == ContentServerAction.SearchGroups)
                {
                    LoadGroupsView(logContent);
                    return;
                }

                // For Create Groups, show Partition Name and Group Name columns
                if (_selectedAction == ContentServerAction.CreateGroups)
                {
                    LoadCreatedGroupsView(logContent);
                    return;
                }

                // For Update Groups, show Partition Name, Group Name, and updated fields
                if (_selectedAction == ContentServerAction.UpdateGroups)
                {
                    LoadUpdatedGroupsView(logContent);
                    return;
                }

                // For Create SubGroups, show Partition Name, Parent Group Name, and Subgroup Name
                if (_selectedAction == ContentServerAction.CreateSubGroups)
                {
                    LoadCreatedSubGroupsView(logContent);
                    return;
                }

                var users = ExtractUsersFromLog(logContent);
                
                // Store users for filtering if enabled
                if (_enableFiltering)
                {
                    _allUsers = users;
                    PopulatePartitionFilter();
                }

                // Populate the DataGridView with extracted data.
                foreach (var user in users)
                {
                    dataGridViewReport.Rows.Add(
                        user.UserId,
                        user.UserPartitionID,
                        user.Name,
                        user.Surname,
                        user.DisplayName,
                        user.Mail,
                        user.Cn,
                        user.AccountLocked,
                        user.Domain);
                }

                if (_enableFiltering)
                {
                    dataGridViewReport.ClearSelection();
                    UpdateRecordCountLabel();
                }
                else
                {
                    lblRecordCount.Text = $"Records: {users.Count}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading log file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Adds filter controls above the DataGridView for Search for Users-style user reports.
        /// </summary>
        private void AddFilterControls()
        {
            // Create filter panel
            _filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(10, 5, 10, 5)
            };

            // Create filter text boxes for each column
            var filterLabel = new Label
            {
                Text = "Filters:",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            _filterPanel.Controls.Add(filterLabel);

            var startX = 60;
            var spacing = 130;

            _filterUserId = CreateFilterTextBox("User ID", startX, spacing * 0);
            _filterPartition = CreatePartitionFilterComboBox(startX, spacing * 1);
            _filterName = CreateFilterTextBox("Name", startX, spacing * 2);
            _filterSurname = CreateFilterTextBox("Surname", startX, spacing * 3);
            _filterDisplayName = CreateFilterTextBox("Display Name", startX, spacing * 4);
            _filterMail = CreateFilterTextBox("Mail", startX, spacing * 5);
            _filterCn = CreateFilterTextBox("CN", startX, spacing * 6);

            _filterPanel.Controls.Add(_filterUserId);
            _filterPanel.Controls.Add(_filterPartition);
            _filterPanel.Controls.Add(_filterName);
            _filterPanel.Controls.Add(_filterSurname);
            _filterPanel.Controls.Add(_filterDisplayName);
            _filterPanel.Controls.Add(_filterMail);
            _filterPanel.Controls.Add(_filterCn);

            // Create Filter button
            _btnFilter = new Button
            {
                Text = "Filter",
                Location = new Point(startX + spacing * 7, 25),
                Size = new Size(70, 25),
                BackColor = Color.Green,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            _btnFilter.Click += BtnFilter_Click;
            _filterPanel.Controls.Add(_btnFilter);

            // Create Clear button (clears filter text boxes)
            _btnClearFilter = new Button
            {
                Text = "Clear Filter",
                Location = new Point(startX + spacing * 7 + 75, 25),
                Size = new Size(80, 25),
                BackColor = Color.Yellow,
                ForeColor = Color.Black,
                UseVisualStyleBackColor = false
            };
            _btnClearFilter.Click += BtnClearFilter_Click;
            _filterPanel.Controls.Add(_btnClearFilter);

            // Create Clear Screen button (clears selected rows), next to Clear
            var btnClearScreen = new Button
            {
                Text = "Clear Screen",
                Location = new Point(startX + spacing * 7 + 75 + 90, 25),
                Size = new Size(90, 25),
                BackColor = Color.BlueViolet,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnClearScreen.Click += btnClearScreen_Click;
            _filterPanel.Controls.Add(btnClearScreen);

            // Create Sort Data button (moves selected rows to top)
            var btnSortData = new Button
            {
                Text = "Sort Data",
                Location = new Point(startX + spacing * 7 + 75 + 90 + 95, 25),
                Size = new Size(80, 25),
                BackColor = Color.Purple,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnSortData.Click += BtnSortData_Click;
            _filterPanel.Controls.Add(btnSortData);

            // Add panel to form (above DataGridView)
            this.Controls.Add(_filterPanel);
            _filterPanel.BringToFront();
            
            // Adjust DataGridView position
            dataGridViewReport.Top += 70;
            dataGridViewReport.Height -= 70;

            // Protect selection: only Clear Screen clears it; clicking to edit keeps the held rows selected
            dataGridViewReport.SelectionChanged += DataGridViewReport_SelectionChangedProtected;
        }

        /// <summary>
        /// Adds filter controls above the DataGridView for Search Groups action.
        /// </summary>
        private void AddGroupFilterControls()
        {
            _filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(10, 5, 10, 5)
            };

            var filterLabel = new Label
            {
                Text = "Filters:",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            _filterPanel.Controls.Add(filterLabel);

            var startX = 60;
            var spacing = 130;

            _filterGroupId = CreateFilterTextBox("Group ID", startX, spacing * 0);
            _filterGroupName = CreateFilterTextBox("Group Name", startX, spacing * 1);
            _filterGroupDateCreated = CreateFilterTextBox("Date Created", startX, spacing * 2);

            _filterPanel.Controls.Add(_filterGroupId);
            _filterPanel.Controls.Add(_filterGroupName);
            _filterPanel.Controls.Add(_filterGroupDateCreated);

            // Place buttons next to the fields initially so they are visible; move to far right when panel has real width
            const int gap = 10;
            var btnY = 25;
            var filterW = 70;
            var clearFilterW = 80;
            var clearScreenW = 90;
            var sortDataW = 80;

            // Filter button
            _btnFilter = new Button
            {
                Text = "Filter",
                Location = new Point(startX + spacing * 3, btnY),
                Size = new Size(filterW, 25),
                BackColor = Color.Green,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            _btnFilter.Click += BtnFilterGroups_Click;
            _filterPanel.Controls.Add(_btnFilter);

            // Clear Filter button
            _btnClearFilter = new Button
            {
                Text = "Clear Filter",
                Location = new Point(startX + spacing * 3 + filterW + gap, btnY),
                Size = new Size(clearFilterW, 25),
                BackColor = Color.Yellow,
                ForeColor = Color.Black,
                UseVisualStyleBackColor = false
            };
            _btnClearFilter.Click += BtnClearFilterGroups_Click;
            _filterPanel.Controls.Add(_btnClearFilter);

            // Clear Screen button
            var btnClearScreen = new Button
            {
                Text = "Clear Screen",
                Location = new Point(startX + spacing * 3 + filterW + gap + clearFilterW + gap, btnY),
                Size = new Size(clearScreenW, 25),
                BackColor = Color.BlueViolet,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnClearScreen.Click += btnClearScreen_Click;
            _filterPanel.Controls.Add(btnClearScreen);

            // Sort Data button
            var btnSortData = new Button
            {
                Text = "Sort Data",
                Location = new Point(startX + spacing * 3 + filterW + gap + clearFilterW + gap + clearScreenW + gap, btnY),
                Size = new Size(sortDataW, 25),
                BackColor = Color.Purple,
                ForeColor = Color.White,
                UseVisualStyleBackColor = false
            };
            btnSortData.Click += BtnSortData_Click;
            _filterPanel.Controls.Add(btnSortData);

            // When panel is resized (e.g. after dock), position buttons at the far right
            void PositionGroupButtonsAtRight()
            {
                if (_filterPanel == null) return;
                var w = _filterPanel.ClientSize.Width;
                if (w < 400) return;
                var right = w - 10;
                btnSortData.Location = new Point(right - sortDataW, btnY);
                btnClearScreen.Location = new Point(right - sortDataW - gap - clearScreenW, btnY);
                _btnClearFilter!.Location = new Point(right - sortDataW - gap - clearScreenW - gap - clearFilterW, btnY);
                _btnFilter!.Location = new Point(right - sortDataW - gap - clearScreenW - gap - clearFilterW - gap - filterW, btnY);
            }

            _filterPanel.Resize += (_, _) => PositionGroupButtonsAtRight();
            _filterPanel.Layout += (_, _) => PositionGroupButtonsAtRight();

            this.Controls.Add(_filterPanel);
            _filterPanel.BringToFront();

            // Position buttons at far right once the form is shown (handle exists)
            Shown += (_, _) => PositionGroupButtonsAtRight();

            dataGridViewReport.Top += 70;
            dataGridViewReport.Height -= 70;

            dataGridViewReport.SelectionChanged += DataGridViewReport_SelectionChangedProtected;
        }

        /// <summary>
        /// Keeps the "held" selection when user clicks a cell to edit; only Clear Screen clears all.
        /// Ctrl+click on a selected row unselects it; Ctrl+click on an unselected row selects it.
        /// </summary>
        private void DataGridViewReport_SelectionChangedProtected(object? sender, EventArgs e)
        {
            if (!_enableFiltering) return;

            // Re-entrant call while we're programmatically restoring selection; don't clear the flag here
            if (_isUpdatingSelection)
            {
                UpdateRecordCountLabel();
                return;
            }

            var current = new HashSet<int>(
                dataGridViewReport.SelectedRows.Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow && r.Index >= 0)
                    .Select(r => r.Index));

            // User expanded selection (Ctrl+click to add): remember as new protected set
            if (current.Count >= _protectedSelectionIndices.Count && _protectedSelectionIndices.Count > 0 && current.IsSupersetOf(_protectedSelectionIndices))
            {
                _protectedSelectionIndices = new HashSet<int>(current);
                UpdateRecordCountLabel();
                return;
            }

            // User reduced selection: either Ctrl+click to deselect (subset, multiple or fewer rows) or single-click to edit (single row)
            if (_protectedSelectionIndices.Count > 0 && current.Count < _protectedSelectionIndices.Count)
            {
                bool singleRowClick = current.Count == 1 && _protectedSelectionIndices.Count > 1;
                bool ctrlDeselect = current.IsSubsetOf(_protectedSelectionIndices) && !singleRowClick;

                if (ctrlDeselect)
                {
                    // Ctrl+click on selected row(s) to unselect: accept the new selection
                    _protectedSelectionIndices = new HashSet<int>(current);
                }
                else
                {
                    // Single-click to edit: restore full selection and keep focus on clicked cell (defer to avoid re-entrant SelectionChanged)
                    var indicesToRestore = new HashSet<int>(_protectedSelectionIndices);
                    var cellToRestore = dataGridViewReport.CurrentCell;
                    BeginInvoke(() =>
                    {
                        _isUpdatingSelection = true;
                        try
                        {
                            foreach (DataGridViewRow row in dataGridViewReport.Rows)
                            {
                                if (row.IsNewRow) continue;
                                row.Selected = indicesToRestore.Contains(row.Index);
                            }
                            if (cellToRestore != null && cellToRestore.RowIndex >= 0 && cellToRestore.RowIndex < dataGridViewReport.Rows.Count)
                                dataGridViewReport.CurrentCell = dataGridViewReport[cellToRestore.ColumnIndex, cellToRestore.RowIndex];
                        }
                        finally
                        {
                            _isUpdatingSelection = false;
                        }
                        UpdateRecordCountLabel();
                    });
                    return;
                }
            }
            else if (current.Count > 0)
                _protectedSelectionIndices = new HashSet<int>(current);

            UpdateRecordCountLabel();
        }

        /// <summary>
        /// Creates a filter text box with a placeholder label.
        /// </summary>
        private TextBox CreateFilterTextBox(string placeholder, int baseX, int offsetX)
        {
            var textBox = new TextBox
            {
                Location = new Point(baseX + offsetX, 27),
                Size = new Size(120, 23),
                PlaceholderText = placeholder
            };
            
            // Allow Enter key to trigger filter
            textBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnFilter_Click(s, e);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            
            return textBox;
        }

        /// <summary>
        /// Creates the partition filter dropdown with "All" as default. Call PopulatePartitionFilter() after loading user data.
        /// </summary>
        private ComboBox CreatePartitionFilterComboBox(int baseX, int offsetX)
        {
            var combo = new ComboBox
            {
                Location = new Point(baseX + offsetX, 27),
                Size = new Size(120, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            combo.Items.Add("All");
            combo.SelectedIndex = 0;
            combo.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    BtnFilter_Click(s, e);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            return combo;
        }

        /// <summary>
        /// Fills the partition filter dropdown with "All" plus unique partitions from the current report data.
        /// </summary>
        private void PopulatePartitionFilter()
        {
            if (_filterPartition == null || _allUsers == null) return;
            var selected = _filterPartition.SelectedItem?.ToString();
            _filterPartition.Items.Clear();
            _filterPartition.Items.Add("All");
            var partitions = _allUsers
                .Select(u => u.UserPartitionID)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var p in partitions)
                _filterPartition.Items.Add(p);
            if (!string.IsNullOrEmpty(selected) && _filterPartition.Items.Contains(selected))
                _filterPartition.SelectedItem = selected;
            else
                _filterPartition.SelectedIndex = 0;
        }

        /// <summary>
        /// Checks whether a cell value matches a filter string.
        /// Supports special keywords: &lt;empty&gt; matches blank values,
        /// &lt;not empty&gt; matches non-blank values.
        /// An empty filter matches everything.
        /// </summary>
        private static bool MatchesFilter(string? value, string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            var v = value ?? string.Empty;

            if (filter.Equals("<empty>", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(v);

            if (filter.Equals("<not empty>", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(v);

            return v.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Applies filters: adds rows that match ANY of the non-empty criteria to the current selection (hold).
        /// Selection is cumulative across filter runs; use Clear Screen to clear selection.
        /// Supports special keywords: &lt;empty&gt; and &lt;not empty&gt;.
        /// </summary>
        private void BtnFilter_Click(object? sender, EventArgs e)
        {
            if (_allUsers == null) return;

            var filterUserId = _filterUserId?.Text?.Trim() ?? string.Empty;
            var filterPartition = _filterPartition?.SelectedItem?.ToString()?.Trim() ?? string.Empty;
            var filterName = _filterName?.Text?.Trim() ?? string.Empty;
            var filterSurname = _filterSurname?.Text?.Trim() ?? string.Empty;
            var filterDisplayName = _filterDisplayName?.Text?.Trim() ?? string.Empty;
            var filterMail = _filterMail?.Text?.Trim() ?? string.Empty;
            var filterCn = _filterCn?.Text?.Trim() ?? string.Empty;

            // Do not clear selection: add matching rows to current selection (cumulative hold).

            var partitionIsAll = string.IsNullOrEmpty(filterPartition) || filterPartition.Equals("All", StringComparison.OrdinalIgnoreCase);
            var anyTextFilter = !string.IsNullOrEmpty(filterUserId) || !string.IsNullOrEmpty(filterName) ||
                                !string.IsNullOrEmpty(filterSurname) || !string.IsNullOrEmpty(filterDisplayName) ||
                                !string.IsNullOrEmpty(filterMail) || !string.IsNullOrEmpty(filterCn);

            // When partition is "All" and no other filters, select all rows. Otherwise use OR logic.
            var selectAllPartitions = partitionIsAll && !anyTextFilter;

            _isUpdatingSelection = true;
            for (var i = 0; i < _allUsers.Count && i < dataGridViewReport.Rows.Count; i++)
            {
                var u = _allUsers[i];
                var partitionMatch = !partitionIsAll && MatchesFilter(u.UserPartitionID, filterPartition);
                var matches = selectAllPartitions ||
                    (!string.IsNullOrEmpty(filterUserId) && MatchesFilter(u.UserId, filterUserId)) ||
                    partitionMatch ||
                    (!string.IsNullOrEmpty(filterName) && MatchesFilter(u.Name, filterName)) ||
                    (!string.IsNullOrEmpty(filterSurname) && MatchesFilter(u.Surname, filterSurname)) ||
                    (!string.IsNullOrEmpty(filterDisplayName) && MatchesFilter(u.DisplayName, filterDisplayName)) ||
                    (!string.IsNullOrEmpty(filterMail) && MatchesFilter(u.Mail, filterMail)) ||
                    (!string.IsNullOrEmpty(filterCn) && MatchesFilter(u.Cn, filterCn));
                if (matches && !dataGridViewReport.Rows[i].IsNewRow)
                {
                    dataGridViewReport.Rows[i].Selected = true;
                }
            }
            _protectedSelectionIndices = new HashSet<int>(
                dataGridViewReport.SelectedRows.Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow && r.Index >= 0)
                    .Select(r => r.Index));
            _isUpdatingSelection = false;

            UpdateRecordCountLabel();
        }

        /// <summary>
        /// Clears only the filter text boxes. Grid and selection are left unchanged
        /// so "held" results stay selected for cumulative filtering.
        /// </summary>
        private void BtnClearFilter_Click(object? sender, EventArgs e)
        {
            if (_filterUserId != null) _filterUserId.Text = string.Empty;
            if (_filterPartition != null && _filterPartition.Items.Count > 0) _filterPartition.SelectedIndex = 0; // "All"
            if (_filterName != null) _filterName.Text = string.Empty;
            if (_filterSurname != null) _filterSurname.Text = string.Empty;
            if (_filterDisplayName != null) _filterDisplayName.Text = string.Empty;
            if (_filterMail != null) _filterMail.Text = string.Empty;
            if (_filterCn != null) _filterCn.Text = string.Empty;
        }

        /// <summary>
        /// Applies filters for the Search Groups grid, adding matching rows to the current selection.
        /// </summary>
        private void BtnFilterGroups_Click(object? sender, EventArgs e)
        {
            if (_allGroups == null) return;

            var filterGroupId = _filterGroupId?.Text?.Trim() ?? string.Empty;
            var filterGroupName = _filterGroupName?.Text?.Trim() ?? string.Empty;
            var filterDate = _filterGroupDateCreated?.Text?.Trim() ?? string.Empty;

            var anyFilter = !string.IsNullOrEmpty(filterGroupId) ||
                            !string.IsNullOrEmpty(filterGroupName) ||
                            !string.IsNullOrEmpty(filterDate);

            if (!anyFilter)
            {
                UpdateRecordCountLabel();
                return;
            }

            _isUpdatingSelection = true;
            for (var i = 0; i < _allGroups.Count && i < dataGridViewReport.Rows.Count; i++)
            {
                var g = _allGroups[i];
                var matches =
                    (!string.IsNullOrEmpty(filterGroupId) && MatchesFilter(g.GroupId, filterGroupId)) ||
                    (!string.IsNullOrEmpty(filterGroupName) && MatchesFilter(g.GroupName, filterGroupName)) ||
                    (!string.IsNullOrEmpty(filterDate) && MatchesFilter(g.DateCreated, filterDate));

                if (matches && !dataGridViewReport.Rows[i].IsNewRow)
                {
                    dataGridViewReport.Rows[i].Selected = true;
                }
            }

            _protectedSelectionIndices = new HashSet<int>(
                dataGridViewReport.SelectedRows.Cast<DataGridViewRow>()
                    .Where(r => !r.IsNewRow && r.Index >= 0)
                    .Select(r => r.Index));

            _isUpdatingSelection = false;
            UpdateRecordCountLabel();
        }

        /// <summary>
        /// Clears only the group filter text boxes for Search Groups.
        /// </summary>
        private void BtnClearFilterGroups_Click(object? sender, EventArgs e)
        {
            if (_filterGroupId != null) _filterGroupId.Text = string.Empty;
            if (_filterGroupName != null) _filterGroupName.Text = string.Empty;
            if (_filterGroupDateCreated != null) _filterGroupDateCreated.Text = string.Empty;
        }

        /// <summary>
        /// Updates the record count label to show total rows and selected count when filtering is enabled.
        /// </summary>
        private void UpdateRecordCountLabel()
        {
            if (!_enableFiltering) return;

            int total;
            if (IsUserSearchReport && _allUsers != null)
            {
                total = _allUsers.Count;
            }
            else if (IsGroupSearchReport && _allGroups != null)
            {
                total = _allGroups.Count;
            }
            else
            {
                return;
            }

            var selected = dataGridViewReport.SelectedRows
                .Cast<DataGridViewRow>()
                .Count(r => !r.IsNewRow);
            lblRecordCount.Text = selected > 0
                ? $"Records: {total} | Selected: {selected}"
                : $"Records: {total}";
        }

        /// <summary>
        /// Loads the log file in a plain single-column view.
        /// Used for Delete User action to show simple log entries without parsing.
        /// </summary>
        private void LoadPlainLogView(string logContent)
        {
            // Clear existing columns and set up a single column for log entries
            dataGridViewReport.Columns.Clear();
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            
            var logEntryColumn = new DataGridViewTextBoxColumn
            {
                Name = "colLogEntry",
                HeaderText = "Log Entry",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };
            dataGridViewReport.Columns.Add(logEntryColumn);

            // Split content into lines and add non-empty lines to the grid
            var lines = logContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var entryCount = 0;

            foreach (var line in lines)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                dataGridViewReport.Rows.Add(line);
                entryCount++;
            }

            lblRecordCount.Text = $"Entries: {entryCount}";
        }

        /// <summary>
        /// Loads the log file in a groups view with Group Name (cn) and Date Created columns.
        /// Used for Search Groups action.
        /// </summary>
        private void LoadGroupsView(string logContent)
        {
            // Clear existing columns and set up columns for groups
            dataGridViewReport.Columns.Clear();
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var groupIdColumn = new DataGridViewTextBoxColumn
            {
                Name = "colGroupId",
                HeaderText = "Group ID",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(groupIdColumn);

            var groupNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colGroupName",
                HeaderText = "Group Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(groupNameColumn);

            var dateCreatedColumn = new DataGridViewTextBoxColumn
            {
                Name = "colDateCreated",
                HeaderText = "Date Created",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(dateCreatedColumn);

            // Extract groups from JSON blocks
            var groups = ExtractGroupsFromLog(logContent);

            foreach (var group in groups)
            {
                dataGridViewReport.Rows.Add(group.GroupId, group.GroupName, group.DateCreated);
            }

            if (_enableFiltering && IsGroupSearchReport)
            {
                _allGroups = groups;
                dataGridViewReport.ClearSelection();
                UpdateRecordCountLabel();
            }
            else
            {
                lblRecordCount.Text = $"Groups: {groups.Count}";
            }
        }

        /// <summary>
        /// Loads the log file in a created groups view with Partition Name and Group Name columns.
        /// Used for Create Groups action.
        /// </summary>
        private void LoadCreatedGroupsView(string logContent)
        {
            // Clear existing columns and set up columns for created groups
            dataGridViewReport.Columns.Clear();
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var partitionNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colPartitionName",
                HeaderText = "Partition Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(partitionNameColumn);

            var groupNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colGroupName",
                HeaderText = "Group Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(groupNameColumn);

            // Extract created groups from JSON blocks
            var groups = ExtractCreatedGroupsFromLog(logContent);

            foreach (var group in groups)
            {
                dataGridViewReport.Rows.Add(group.PartitionName, group.GroupName);
            }

            lblRecordCount.Text = $"Groups: {groups.Count}";
        }

        /// <summary>
        /// Loads the log file in an add user to group view with User and Group columns.
        /// Used for Add User to Group action.
        /// </summary>
        private void LoadAddUserToGroupView(string logContent)
        {
            // Clear existing columns and set up columns for user-group assignments
            dataGridViewReport.Columns.Clear();
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var userColumn = new DataGridViewTextBoxColumn
            {
                Name = "colUser",
                HeaderText = "User",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(userColumn);

            var groupColumn = new DataGridViewTextBoxColumn
            {
                Name = "colGroup",
                HeaderText = "Group",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(groupColumn);

            // Extract user-group assignments from log
            var assignments = ExtractUserGroupAssignmentsFromLog(logContent);

            foreach (var assignment in assignments)
            {
                dataGridViewReport.Rows.Add(assignment.User, assignment.Group);
            }

            lblRecordCount.Text = $"Assignments: {assignments.Count}";
        }

        /// <summary>
        /// Extracts user-group assignment records from the log file content.
        /// Parses the success messages to extract user and group names.
        /// </summary>
        private List<UserGroupAssignment> ExtractUserGroupAssignmentsFromLog(string logContent)
        {
            var assignments = new List<UserGroupAssignment>();

            // Parse the log content for success messages like:
            // [SUCCESS] ... - Added user 'BJaricha@Active Directory' to group 'HarareGroup@Content Server Members' ...
            var successPattern = new System.Text.RegularExpressions.Regex(
                @"\[SUCCESS\].*Added user '([^']+)' to group '([^']+)'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var matches = successPattern.Matches(logContent);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var userId = match.Groups[1].Value;
                    var groupId = match.Groups[2].Value;

                    // Extract just the name part (before the @ symbol) for cleaner display
                    var userName = ExtractNameFromId(userId);
                    var groupName = ExtractNameFromId(groupId);

                    assignments.Add(new UserGroupAssignment
                    {
                        User = userName,
                        Group = groupName
                    });
                }
            }

            return assignments;
        }

        /// <summary>
        /// Extracts the name part from an ID in the format "name@partition".
        /// </summary>
        private static string ExtractNameFromId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            var atIndex = id.IndexOf('@');
            return atIndex > 0 ? id.Substring(0, atIndex) : id;
        }

        /// <summary>
        /// Loads the log file in an updated groups view with Partition Name, Group Name, and updated fields.
        /// Used for Update Groups action.
        /// </summary>
        private void LoadUpdatedGroupsView(string logContent)
        {
            // Clear existing columns and set up columns for updated groups
            dataGridViewReport.Columns.Clear();
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var partitionNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colPartitionName",
                HeaderText = "Partition Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(partitionNameColumn);

            var groupNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colGroupName",
                HeaderText = "Group Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(groupNameColumn);

            var descriptionColumn = new DataGridViewTextBoxColumn
            {
                Name = "colDescription",
                HeaderText = "Description",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(descriptionColumn);

            var nameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(nameColumn);

            // Extract updated groups from JSON blocks
            var groups = ExtractUpdatedGroupsFromLog(logContent);

            foreach (var group in groups)
            {
                dataGridViewReport.Rows.Add(group.PartitionName, group.GroupName, group.Description, group.Name);
            }

            lblRecordCount.Text = $"Groups: {groups.Count}";
        }

        /// <summary>
        /// Loads the log file in a created subgroups view with Partition Name, Parent Group Name, and Subgroup Name.
        /// Used for Create SubGroups action.
        /// </summary>
        private void LoadCreatedSubGroupsView(string logContent)
        {
            // Clear existing columns and set up columns for created subgroups
            dataGridViewReport.Columns.Clear();
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var partitionNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colPartitionName",
                HeaderText = "Partition Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(partitionNameColumn);

            var parentGroupColumn = new DataGridViewTextBoxColumn
            {
                Name = "colParentGroup",
                HeaderText = "Parent Group Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(parentGroupColumn);

            var subGroupNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "colSubGroupName",
                HeaderText = "Subgroup Name",
                ReadOnly = true
            };
            dataGridViewReport.Columns.Add(subGroupNameColumn);

            // Extract created subgroups from JSON blocks
            var subgroups = ExtractCreatedSubGroupsFromLog(logContent);

            foreach (var subgroup in subgroups)
            {
                dataGridViewReport.Rows.Add(subgroup.PartitionName, subgroup.ParentGroupName, subgroup.SubGroupName);
            }

            lblRecordCount.Text = $"Subgroups: {subgroups.Count}";
        }

        /// <summary>
        /// Extracts created subgroup records from the log file content.
        /// </summary>
        private List<SubGroupRecord> ExtractCreatedSubGroupsFromLog(string logContent)
        {
            var subgroups = new List<SubGroupRecord>();

            var jsonBlocks = ExtractJsonBlocks(logContent);

            foreach (var jsonBlock in jsonBlocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonBlock);
                    var root = doc.RootElement;

                    var subgroup = ExtractSubGroupRecord(root);
                    if (subgroup != null)
                    {
                        subgroups.Add(subgroup);
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON blocks.
                }
            }

            return subgroups;
        }

        /// <summary>
        /// Extracts a SubGroupRecord from a JSON element representing a created subgroup.
        /// </summary>
        private SubGroupRecord? ExtractSubGroupRecord(JsonElement groupElement)
        {
            var partitionName = string.Empty;
            var subGroupName = string.Empty;
            var parentGroupName = string.Empty;

            // Get userPartitionID directly from the element
            if (groupElement.TryGetProperty("userPartitionID", out var partitionProp) &&
                partitionProp.ValueKind == JsonValueKind.String)
            {
                partitionName = partitionProp.GetString() ?? string.Empty;
            }

            // Get subgroup name from cn in values array or from name field
            if (groupElement.TryGetProperty("values", out var valuesArray) &&
                valuesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var valueEntry in valuesArray.EnumerateArray())
                {
                    if (valueEntry.TryGetProperty("name", out var fieldName) &&
                        fieldName.GetString() == "cn" &&
                        valueEntry.TryGetProperty("values", out var innerValues) &&
                        innerValues.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var val in innerValues.EnumerateArray())
                        {
                            subGroupName = val.GetString() ?? string.Empty;
                            break;
                        }
                        break;
                    }
                }
            }

            // If cn not found, try name field
            if (string.IsNullOrEmpty(subGroupName) &&
                groupElement.TryGetProperty("name", out var nameProp) &&
                nameProp.ValueKind == JsonValueKind.String)
            {
                subGroupName = nameProp.GetString() ?? string.Empty;
            }

            // Try to extract parent group name from location
            if (groupElement.TryGetProperty("location", out var locationProp) &&
                locationProp.ValueKind == JsonValueKind.String)
            {
                var location = locationProp.GetString() ?? string.Empty;
                parentGroupName = ExtractParentFromLocation(location);
            }

            // Only add if we have at least some data
            if (!string.IsNullOrEmpty(partitionName) || !string.IsNullOrEmpty(subGroupName))
            {
                return new SubGroupRecord
                {
                    PartitionName = partitionName,
                    ParentGroupName = parentGroupName,
                    SubGroupName = subGroupName
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts the parent group information from a location DN string.
        /// </summary>
        private static string ExtractParentFromLocation(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return string.Empty;
            }

            // The location contains the group's position in the hierarchy
            // Try to extract the parent oTGroup UUID or meaningful info
            var match = System.Text.RegularExpressions.Regex.Match(location, @"oTGroup=([^,]+)");
            if (match.Success)
            {
                // Return the UUID - in a real scenario you might want to look this up
                return match.Groups[1].Value;
            }

            // Try to extract orgunit as fallback
            var orgunitMatch = System.Text.RegularExpressions.Regex.Match(location, @"orgunit=([^,]+)");
            if (orgunitMatch.Success)
            {
                return orgunitMatch.Groups[1].Value;
            }

            return location.Length > 50 ? location.Substring(0, 47) + "..." : location;
        }

        /// <summary>
        /// Extracts updated group records from the log file content.
        /// </summary>
        private List<UpdatedGroupRecord> ExtractUpdatedGroupsFromLog(string logContent)
        {
            var groups = new List<UpdatedGroupRecord>();

            var jsonBlocks = ExtractJsonBlocks(logContent);

            foreach (var jsonBlock in jsonBlocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonBlock);
                    var root = doc.RootElement;

                    var group = ExtractUpdatedGroupRecord(root);
                    if (group != null)
                    {
                        groups.Add(group);
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON blocks.
                }
            }

            return groups;
        }

        /// <summary>
        /// Extracts an UpdatedGroupRecord from a JSON element representing an updated group.
        /// </summary>
        private UpdatedGroupRecord? ExtractUpdatedGroupRecord(JsonElement groupElement)
        {
            var partitionName = string.Empty;
            var groupName = string.Empty;
            var description = string.Empty;
            var name = string.Empty;

            // Get userPartitionID directly from the element
            if (groupElement.TryGetProperty("userPartitionID", out var partitionProp) &&
                partitionProp.ValueKind == JsonValueKind.String)
            {
                partitionName = partitionProp.GetString() ?? string.Empty;
            }

            // Get name directly from the element
            if (groupElement.TryGetProperty("name", out var nameProp) &&
                nameProp.ValueKind == JsonValueKind.String)
            {
                name = nameProp.GetString() ?? string.Empty;
            }

            // Get cn and description from the values array
            if (groupElement.TryGetProperty("values", out var valuesArray) &&
                valuesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var valueEntry in valuesArray.EnumerateArray())
                {
                    if (valueEntry.TryGetProperty("name", out var fieldName) &&
                        valueEntry.TryGetProperty("values", out var innerValues) &&
                        innerValues.ValueKind == JsonValueKind.Array)
                    {
                        var fieldNameStr = fieldName.GetString();
                        foreach (var val in innerValues.EnumerateArray())
                        {
                            if (fieldNameStr == "cn")
                            {
                                groupName = val.GetString() ?? string.Empty;
                            }
                            else if (fieldNameStr == "description")
                            {
                                description = val.GetString() ?? string.Empty;
                            }
                            break;
                        }
                    }
                }
            }

            // If cn not found in values, use name
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = name;
            }

            // Only add if we have at least some data
            if (!string.IsNullOrEmpty(partitionName) || !string.IsNullOrEmpty(groupName))
            {
                return new UpdatedGroupRecord
                {
                    PartitionName = partitionName,
                    GroupName = groupName,
                    Description = description,
                    Name = name
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts created group records from the log file content.
        /// Looks for single group objects in the response (not in an array).
        /// </summary>
        private List<CreatedGroupRecord> ExtractCreatedGroupsFromLog(string logContent)
        {
            var groups = new List<CreatedGroupRecord>();

            var jsonBlocks = ExtractJsonBlocks(logContent);

            foreach (var jsonBlock in jsonBlocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonBlock);
                    var root = doc.RootElement;

                    // For created groups, the response is a single group object (not in an array)
                    var group = ExtractCreatedGroupRecord(root);
                    if (group != null)
                    {
                        groups.Add(group);
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON blocks.
                }
            }

            return groups;
        }

        /// <summary>
        /// Extracts a CreatedGroupRecord from a JSON element representing a created group.
        /// </summary>
        private CreatedGroupRecord? ExtractCreatedGroupRecord(JsonElement groupElement)
        {
            var partitionName = string.Empty;
            var groupName = string.Empty;

            // Get userPartitionID directly from the element
            if (groupElement.TryGetProperty("userPartitionID", out var partitionProp) &&
                partitionProp.ValueKind == JsonValueKind.String)
            {
                partitionName = partitionProp.GetString() ?? string.Empty;
            }

            // Get cn from the values array
            if (groupElement.TryGetProperty("values", out var valuesArray) &&
                valuesArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var valueEntry in valuesArray.EnumerateArray())
                {
                    if (valueEntry.TryGetProperty("name", out var nameProperty) &&
                        nameProperty.GetString() == "cn" &&
                        valueEntry.TryGetProperty("values", out var innerValues) &&
                        innerValues.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var val in innerValues.EnumerateArray())
                        {
                            groupName = val.GetString() ?? string.Empty;
                            break;
                        }
                        break;
                    }
                }
            }

            // If cn not found in values, try name directly
            if (string.IsNullOrEmpty(groupName) &&
                groupElement.TryGetProperty("name", out var nameProp) &&
                nameProp.ValueKind == JsonValueKind.String)
            {
                groupName = nameProp.GetString() ?? string.Empty;
            }

            // Only add if we have at least some data
            if (!string.IsNullOrEmpty(partitionName) || !string.IsNullOrEmpty(groupName))
            {
                return new CreatedGroupRecord
                {
                    PartitionName = partitionName,
                    GroupName = groupName
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts group records from the log file content.
        /// Looks for groups in the "groups" array of the OTDS response.
        /// </summary>
        private List<GroupRecord> ExtractGroupsFromLog(string logContent)
        {
            var groups = new List<GroupRecord>();

            var jsonBlocks = ExtractJsonBlocks(logContent);

            foreach (var jsonBlock in jsonBlocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonBlock);
                    var root = doc.RootElement;

                    // Check if this JSON has a "groups" array (OTDS groups response).
                    if (root.TryGetProperty("groups", out var groupsArray) &&
                        groupsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var groupElement in groupsArray.EnumerateArray())
                        {
                            var group = ExtractGroupRecord(groupElement);
                            if (group != null)
                            {
                                groups.Add(group);
                            }
                        }
                    }
                    else
                    {
                        // Try to parse as a single group object
                        var group = ExtractGroupRecord(root);
                        if (group != null)
                        {
                            groups.Add(group);
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON blocks.
                }
            }

            return groups;
        }

        /// <summary>
        /// Extracts a GroupRecord from a JSON element representing a group.
        /// The OTDS groups response may have cn and createTimestamp as direct properties
        /// or inside the "attributes" object.
        /// </summary>
        private GroupRecord? ExtractGroupRecord(JsonElement groupElement)
        {
            var groupId = string.Empty;
            var cn = string.Empty;
            var createTimestamp = string.Empty;

            // Helper to extract first value from an array property (e.g. "cn": ["value"])
            string GetFirstArrayValue(JsonElement parent, string propertyName)
            {
                if (parent.TryGetProperty(propertyName, out var array) &&
                    array.ValueKind == JsonValueKind.Array)
                {
                    foreach (var val in array.EnumerateArray())
                    {
                        return val.GetString() ?? string.Empty;
                    }
                }
                return string.Empty;
            }

            // Extract group id
            if (groupElement.TryGetProperty("id", out var idProp) &&
                idProp.ValueKind == JsonValueKind.String)
            {
                groupId = idProp.GetString() ?? string.Empty;
            }

            // --- Strategy 1: "values" array with { "name": "...", "values": [...] } objects ---
            // The "Search All Groups" response uses this format.
            cn = ExtractValueByName(groupElement, "cn") ?? string.Empty;
            createTimestamp = ExtractValueByName(groupElement, "createTimestamp") ?? string.Empty;

            // --- Strategy 2: "attributes" object with direct array properties ---
            // Some OTDS responses nest data here instead.
            if (string.IsNullOrEmpty(cn) || string.IsNullOrEmpty(createTimestamp))
            {
                if (groupElement.TryGetProperty("attributes", out var attrsObj) &&
                    attrsObj.ValueKind == JsonValueKind.Object)
                {
                    if (string.IsNullOrEmpty(cn))
                    {
                        cn = GetFirstArrayValue(attrsObj, "cn");
                    }
                    if (string.IsNullOrEmpty(createTimestamp))
                    {
                        createTimestamp = GetFirstArrayValue(attrsObj, "createTimestamp");
                    }
                }
            }

            // --- Strategy 3: direct properties on the group element ---
            if (string.IsNullOrEmpty(cn))
            {
                cn = GetFirstArrayValue(groupElement, "cn");
            }

            if (string.IsNullOrEmpty(cn) &&
                groupElement.TryGetProperty("name", out var nameProp) &&
                nameProp.ValueKind == JsonValueKind.String)
            {
                cn = nameProp.GetString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(createTimestamp))
            {
                createTimestamp = GetFirstArrayValue(groupElement, "createTimestamp");
            }

            // Format the timestamp for display if present
            if (!string.IsNullOrEmpty(createTimestamp))
            {
                if (DateTime.TryParse(createTimestamp, out var parsedDate))
                {
                    createTimestamp = parsedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            // Only add if we have at least a group name
            if (!string.IsNullOrEmpty(cn))
            {
                return new GroupRecord
                {
                    GroupId = groupId,
                    GroupName = cn,
                    DateCreated = createTimestamp
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts user records from the log file content.
        /// The log contains text lines followed by JSON blocks.
        /// Handles both search results (users array) and create results (single user object).
        /// </summary>
        private List<UserRecord> ExtractUsersFromLog(string logContent)
        {
            var users = new List<UserRecord>();

            // Find all JSON blocks in the log (they start with '{' and end with '}').
            // The log format has lines like [START]..., [SUCCESS]...: followed by JSON.
            var jsonBlocks = ExtractJsonBlocks(logContent);

            foreach (var jsonBlock in jsonBlocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonBlock);
                    var root = doc.RootElement;

                    // Check if this JSON has a users array (search results).
                    // OTDS responses can use either "users" or "_users".
                    if (TryGetUsersArray(root, out var usersArray))
                    {
                        foreach (var userElement in usersArray.EnumerateArray())
                        {
                            var user = ExtractUserRecord(userElement);
                            if (user != null)
                            {
                                users.Add(user);
                            }
                        }
                    }
                    // Check if this is a single user object (create result).
                    else if (root.TryGetProperty("userPartitionID", out _) ||
                             root.TryGetProperty("values", out _))
                    {
                        var user = ExtractUserRecord(root);
                        if (user != null)
                        {
                            users.Add(user);
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON blocks.
                }
            }

            return users;
        }

        /// <summary>
        /// Tries to locate a users array on the JSON root. Supports both "users" and "_users".
        /// </summary>
        private static bool TryGetUsersArray(JsonElement root, out JsonElement usersArray)
        {
            if (root.TryGetProperty("users", out usersArray) &&
                usersArray.ValueKind == JsonValueKind.Array)
            {
                return true;
            }

            if (root.TryGetProperty("_users", out usersArray) &&
                usersArray.ValueKind == JsonValueKind.Array)
            {
                return true;
            }

            usersArray = default;
            return false;
        }

        /// <summary>
        /// Extracts a UserRecord from a JSON element representing a user.
        /// </summary>
        private UserRecord? ExtractUserRecord(JsonElement userElement)
        {
            // Get userPartitionID directly from the element.
            var userPartitionID = string.Empty;
            if (userElement.TryGetProperty("userPartitionID", out var partitionProp) &&
                partitionProp.ValueKind == JsonValueKind.String)
            {
                userPartitionID = partitionProp.GetString() ?? string.Empty;
            }

            // Get id (user_id) directly from the element.
            var userId = string.Empty;
            if (userElement.TryGetProperty("id", out var idProp) &&
                idProp.ValueKind == JsonValueKind.String)
            {
                userId = idProp.GetString() ?? string.Empty;
            }

            // Get name directly from the element (this is the full name/display name at root level).
            var nameFromRoot = string.Empty;
            if (userElement.TryGetProperty("name", out var nameProp) &&
                nameProp.ValueKind == JsonValueKind.String)
            {
                nameFromRoot = nameProp.GetString() ?? string.Empty;
            }

            // Extract values from the values array.
            var displayName = ExtractValueByName(userElement, "displayName") ?? nameFromRoot;
            var surname = ExtractValueByName(userElement, "sn") ?? string.Empty;
            var mail = ExtractValueByName(userElement, "mail") ?? string.Empty;
            var givenName = ExtractValueByName(userElement, "givenName") ?? string.Empty;
            var cn = ExtractValueByName(userElement, "cn") ?? string.Empty;
            var accountLocked = ExtractValueByName(userElement, "accountLockedOut") ?? string.Empty;
            var domain = ExtractValueByName(userElement, "oTDomainName") ?? string.Empty;

            // Use givenName as name if we don't have it from root.
            var name = !string.IsNullOrEmpty(givenName) ? givenName : nameFromRoot;

            // Only add if we have at least some data.
            if (!string.IsNullOrEmpty(userPartitionID) ||
                !string.IsNullOrEmpty(userId) ||
                !string.IsNullOrEmpty(name) ||
                !string.IsNullOrEmpty(surname) ||
                !string.IsNullOrEmpty(displayName) ||
                !string.IsNullOrEmpty(mail))
            {
                return new UserRecord
                {
                    UserId = userId,
                    UserPartitionID = userPartitionID,
                    Name = name,
                    Surname = surname,
                    DisplayName = displayName,
                    Mail = mail,
                    Cn = cn,
                    AccountLocked = accountLocked,
                    Domain = domain
                };
            }

            return null;
        }

        /// <summary>
        /// Extracts all JSON object blocks from the log content.
        /// Uses brace counting to find complete JSON objects.
        /// </summary>
        private List<string> ExtractJsonBlocks(string content)
        {
            var blocks = new List<string>();
            var sb = new StringBuilder();
            var braceCount = 0;
            var inJson = false;

            foreach (var ch in content)
            {
                if (ch == '{')
                {
                    if (!inJson)
                    {
                        inJson = true;
                        sb.Clear();
                    }
                    braceCount++;
                    sb.Append(ch);
                }
                else if (ch == '}')
                {
                    if (inJson)
                    {
                        braceCount--;
                        sb.Append(ch);

                        if (braceCount == 0)
                        {
                            blocks.Add(sb.ToString());
                            inJson = false;
                        }
                    }
                }
                else if (inJson)
                {
                    sb.Append(ch);
                }
            }

            return blocks;
        }

        /// <summary>
        /// Extracts a value from a user element's "values" array by searching
        /// for an entry with the specified "name" property.
        /// </summary>
        private string? ExtractValueByName(JsonElement userElement, string name)
        {
            if (!userElement.TryGetProperty("values", out var valuesArray) ||
                valuesArray.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var valueEntry in valuesArray.EnumerateArray())
            {
                if (valueEntry.TryGetProperty("name", out var nameProperty) &&
                    nameProperty.GetString() == name &&
                    valueEntry.TryGetProperty("values", out var innerValues) &&
                    innerValues.ValueKind == JsonValueKind.Array)
                {
                    // Return the first value in the array.
                    foreach (var val in innerValues.EnumerateArray())
                    {
                        return val.GetString();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Closes the report form.
        /// </summary>
        private void btnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Exports the DataGridView data to a CSV file.
        /// </summary>
        private void btnExport_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export Report to CSV",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"report_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                    
                    // Handle plain log view (Delete User, Delete Group, Remove User from Group) vs standard user data view
                    if (_selectedAction == ContentServerAction.DeleteUser ||
                        _selectedAction == ContentServerAction.DeleteGroup ||
                        _selectedAction == ContentServerAction.RemoveUserFromGroup)
                    {
                        // Write header for plain log view.
                        writer.WriteLine("Log Entry");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var logEntry = row.Cells[0].Value?.ToString() ?? string.Empty;
                            // Escape values for CSV.
                            writer.WriteLine($"\"{logEntry.Replace("\"", "\"\"")}\"");
                        }
                    }
                    else if (_selectedAction == ContentServerAction.AddUserToGroup)
                    {
                        // Write header for user-group assignments view.
                        writer.WriteLine("User,Group");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var user = row.Cells[0].Value?.ToString() ?? string.Empty;
                            var group = row.Cells[1].Value?.ToString() ?? string.Empty;

                            // Escape values for CSV.
                            writer.WriteLine(
                                $"\"{user.Replace("\"", "\"\"")}\","
                                + $"\"{group.Replace("\"", "\"\"")}\"");
                        }
                    }
                    else if (_selectedAction == ContentServerAction.SearchGroups)
                    {
                        // Write header for groups view.
                        writer.WriteLine("Group ID,Group Name,Date Created");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var groupId = row.Cells[0].Value?.ToString() ?? string.Empty;
                            var groupName = row.Cells[1].Value?.ToString() ?? string.Empty;
                            var dateCreated = row.Cells[2].Value?.ToString() ?? string.Empty;

                            // Escape values for CSV.
                            writer.WriteLine(
                                $"\"{groupId.Replace("\"", "\"\"")}\","
                                + $"\"{groupName.Replace("\"", "\"\"")}\","
                                + $"\"{dateCreated.Replace("\"", "\"\"")}\"");
                        }
                    }
                    else if (_selectedAction == ContentServerAction.CreateGroups)
                    {
                        // Write header for created groups view.
                        writer.WriteLine("Partition Name,Group Name");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var partitionName = row.Cells[0].Value?.ToString() ?? string.Empty;
                            var groupName = row.Cells[1].Value?.ToString() ?? string.Empty;

                            // Escape values for CSV.
                            writer.WriteLine(
                                $"\"{partitionName.Replace("\"", "\"\"")}\","
                                + $"\"{groupName.Replace("\"", "\"\"")}\"");
                        }
                    }
                    else if (_selectedAction == ContentServerAction.UpdateGroups)
                    {
                        // Write header for updated groups view.
                        writer.WriteLine("Partition Name,Group Name,Description,Name");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var partitionName = row.Cells[0].Value?.ToString() ?? string.Empty;
                            var groupName = row.Cells[1].Value?.ToString() ?? string.Empty;
                            var description = row.Cells[2].Value?.ToString() ?? string.Empty;
                            var name = row.Cells[3].Value?.ToString() ?? string.Empty;

                            // Escape values for CSV.
                            writer.WriteLine(
                                $"\"{partitionName.Replace("\"", "\"\"")}\","
                                + $"\"{groupName.Replace("\"", "\"\"")}\","
                                + $"\"{description.Replace("\"", "\"\"")}\","
                                + $"\"{name.Replace("\"", "\"\"")}\"");
                        }
                    }
                    else if (_selectedAction == ContentServerAction.CreateSubGroups)
                    {
                        // Write header for created subgroups view.
                        writer.WriteLine("Partition Name,Parent Group Name,Subgroup Name");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var partitionName = row.Cells[0].Value?.ToString() ?? string.Empty;
                            var parentGroupName = row.Cells[1].Value?.ToString() ?? string.Empty;
                            var subGroupName = row.Cells[2].Value?.ToString() ?? string.Empty;

                            // Escape values for CSV.
                            writer.WriteLine(
                                $"\"{partitionName.Replace("\"", "\"\"")}\","
                                + $"\"{parentGroupName.Replace("\"", "\"\"")}\","
                                + $"\"{subGroupName.Replace("\"", "\"\"")}\"");
                        }
                    }
                    else
                    {
                        // Write header for standard user data view.
                        writer.WriteLine("User ID,User Partition ID,Name,Surname,Display Name,Mail,CN,Account Locked,Domain");

                        // Write data rows.
                        foreach (var row in GetRowsForExport())
                        {
                            var userId = row.Cells[0].Value?.ToString() ?? string.Empty;
                            var userPartitionID = row.Cells[1].Value?.ToString() ?? string.Empty;
                            var name = row.Cells[2].Value?.ToString() ?? string.Empty;
                            var surname = row.Cells[3].Value?.ToString() ?? string.Empty;
                            var displayName = row.Cells[4].Value?.ToString() ?? string.Empty;
                            var mail = row.Cells[5].Value?.ToString() ?? string.Empty;
                            var cn = row.Cells[6].Value?.ToString() ?? string.Empty;
                            var accountLocked = row.Cells[7].Value?.ToString() ?? string.Empty;
                            var domain = row.Cells[8].Value?.ToString() ?? string.Empty;

                            // Escape values for CSV.
                            writer.WriteLine(
                                $"\"{userId.Replace("\"", "\"\"")}\","
                                + $"\"{userPartitionID.Replace("\"", "\"\"")}\","
                                + $"\"{name.Replace("\"", "\"\"")}\","
                                + $"\"{surname.Replace("\"", "\"\"")}\","
                                + $"\"{displayName.Replace("\"", "\"\"")}\","
                                + $"\"{mail.Replace("\"", "\"\"")}\","
                                + $"\"{cn.Replace("\"", "\"\"")}\","
                                + $"\"{accountLocked.Replace("\"", "\"\"")}\","
                                + $"\"{domain.Replace("\"", "\"\"")}\"");
                        }
                    }

                    MessageBox.Show(
                        $"Report exported to: {dialog.FileName}",
                        "Export Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error exporting report: {ex.Message}",
                        "Export Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Simple record class to hold extracted user data.
        /// </summary>
        private class UserRecord
        {
            public string UserId { get; set; } = string.Empty;
            public string UserPartitionID { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Surname { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Mail { get; set; } = string.Empty;
            public string Cn { get; set; } = string.Empty;
            public string AccountLocked { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
        }

        /// <summary>
        /// Simple record class to hold extracted group data.
        /// </summary>
        private class GroupRecord
        {
            public string GroupId { get; set; } = string.Empty;
            public string GroupName { get; set; } = string.Empty;
            public string DateCreated { get; set; } = string.Empty;
        }

        /// <summary>
        /// Simple record class to hold extracted created group data.
        /// </summary>
        private class CreatedGroupRecord
        {
            public string PartitionName { get; set; } = string.Empty;
            public string GroupName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Simple record class to hold extracted updated group data.
        /// </summary>
        private class UpdatedGroupRecord
        {
            public string PartitionName { get; set; } = string.Empty;
            public string GroupName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// Simple record class to hold extracted subgroup data.
        /// </summary>
        private class SubGroupRecord
        {
            public string PartitionName { get; set; } = string.Empty;
            public string ParentGroupName { get; set; } = string.Empty;
            public string SubGroupName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Simple record class to hold user-group assignment data.
        /// </summary>
        private class UserGroupAssignment
        {
            public string User { get; set; } = string.Empty;
            public string Group { get; set; } = string.Empty;
        }
    }
}
