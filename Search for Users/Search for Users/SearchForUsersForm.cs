using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Search_for_Users
{
    /// <summary>
    /// Implements the "Upload Document and Metadata" style screen used
    /// for searching users in OpenText Content Server from one or more
    /// CSV files and writing info/error logs.
    /// </summary>
    public partial class SearchForUsersForm : Form
    {
        // Reference to the action selection form so we can navigate back.
        private readonly ActionSelectionForm _actionSelectionForm;

        // Reference to the login form so we can reuse the authentication ticket.
        private readonly Form1 _loginForm;

        // Folder where all log and error files will be written.
        private string? _logFolder;

        // List of CSV files chosen by the user.
        private readonly List<string> _inputFiles = new();

        // State used while processing to support Start/Stop.
        private CancellationTokenSource? _processingCancellationSource;

        // Counters to track overall progress across all CSV files.
        private int _totalFiles;
        private int _processedFiles;
        private int _errorFiles;
        private int _rowsProcessed;

        // Selected action that determines which API endpoint/method is used.
        private readonly ContentServerAction _selectedAction;

        // Path to the last created info log file (for "View File" button).
        private string? _lastInfoLogPath;

        /// <summary>
        /// Creates the Search for Users form and remembers the
        /// previous (action selection) and login forms so that
        /// navigation and API calls work correctly.
        /// </summary>
        public SearchForUsersForm(ActionSelectionForm actionSelectionForm, Form1 loginForm, ContentServerAction selectedAction)
        {
            _actionSelectionForm = actionSelectionForm ?? throw new ArgumentNullException(nameof(actionSelectionForm));
            _loginForm = loginForm ?? throw new ArgumentNullException(nameof(loginForm));
            _selectedAction = selectedAction;

            InitializeComponent();

            // Update the window title so the user can see which mode they are in.
            // This keeps the UI the same while making the behavior explicit.
            var actionName = selectedAction switch
            {
                ContentServerAction.SearchUserById => "Search User by ID",
                ContentServerAction.CreateUser => "Create User",
                ContentServerAction.UpdateUser => "Update User",
                ContentServerAction.DeleteUser => "Delete User",
                ContentServerAction.SearchGroups => "Search Groups",
                ContentServerAction.CreateGroups => "Create Groups",
                ContentServerAction.CreateSubGroups => "Create SubGroups",
                ContentServerAction.UpdateGroups => "Update Groups",
                ContentServerAction.DeleteGroup => "Delete Group",
                ContentServerAction.AddUserToGroup => "Add User to Group",
                ContentServerAction.RemoveUserFromGroup => "Remove Users from Group",
                _ => "Search for Users"
            };

            this.Text = actionName;

            // Display the selected action in the readonly text field.
            txtSelectedAction.Text = actionName;

            // Initialize the Error Log color based on the initial error count (should be 0).
            UpdateErrorLogColor();
        }

        /// <summary>
        /// Handles the Back button by closing this form and
        /// returning to the action selection screen.
        /// </summary>
        private void btnBack_Click(object? sender, EventArgs e)
        {
            _actionSelectionForm.Show();
            this.Close();
        }

        /// <summary>
        /// Lets the user choose a folder where info and error
        /// log files will be stored for all processed CSV files.
        /// </summary>
        private void btnBrowseLogLocation_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder where log files will be stored"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _logFolder = dialog.SelectedPath;
                txtLogLocation.Text = _logFolder;
            }
        }

        /// <summary>
        /// Opens Windows Explorer at the selected log folder so
        /// the user can quickly locate info or error log files.
        /// The Info/Error choice is purely informational here
        /// because both log types are written to the same folder.
        /// </summary>
        private void btnOpenLogLocation_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_logFolder) || !Directory.Exists(_logFolder))
            {
                MessageBox.Show(
                    "Please choose a log location first.",
                    "Log Location",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Open the folder in Windows Explorer.
            Process.Start(new ProcessStartInfo
            {
                FileName = _logFolder,
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Opens the report form to display the contents of the last
        /// created info log file in a filtered, tabular view.
        /// </summary>
        private void btnViewFile_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_lastInfoLogPath) || !File.Exists(_lastInfoLogPath))
            {
                MessageBox.Show(
                    "No log file has been created yet. Please run a search first.",
                    "View File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Build the report form title based on the selected action.
            var actionName = _selectedAction switch
            {
                ContentServerAction.SearchUserById => "Search User by ID",
                ContentServerAction.CreateUser => "Create User",
                ContentServerAction.UpdateUser => "Update User",
                ContentServerAction.DeleteUser => "Delete User",
                ContentServerAction.SearchGroups => "Search Groups",
                ContentServerAction.CreateGroups => "Create Groups",
                ContentServerAction.CreateSubGroups => "Create SubGroups",
                ContentServerAction.UpdateGroups => "Update Groups",
                ContentServerAction.DeleteGroup => "Delete Group",
                ContentServerAction.AddUserToGroup => "Add User to Group",
                ContentServerAction.RemoveUserFromGroup => "Remove Users from Group",
                _ => "Search for Users"
            };

            var reportForm = new ReportForm(_lastInfoLogPath, $"{actionName} Report", _selectedAction);
            reportForm.Show();
        }

        /// <summary>
        /// Allows the user to choose one or more CSV input files
        /// that will drive the search against Content Server.
        /// </summary>
        private void btnChooseInputFile_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select CSV files containing user search criteria",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _inputFiles.Clear();
                _inputFiles.AddRange(dialog.FileNames);

                // Show a simple summary of what was selected.
                txtInputFile.Text = string.Join("; ", _inputFiles);

                // Update total file count based on the number of CSV files chosen.
                _totalFiles = _inputFiles.Count;
                lblTotalFilesValue.Text = _totalFiles.ToString();
            }
        }

        /// <summary>
        /// Starts the search process by reading each selected CSV file,
        /// calling the Content Server users/search API, and writing out
        /// info and error logs. This runs asynchronously so the UI stays
        /// responsive and can be stopped.
        /// </summary>
        private async void btnStart_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_loginForm.AuthTicket))
            {
                MessageBox.Show(
                    "You are not logged in. Please log in again.",
                    "Authentication Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_logFolder))
            {
                MessageBox.Show(
                    "Please choose a log location before starting.",
                    "Log Location Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_inputFiles.Count == 0)
            {
                MessageBox.Show(
                    "Please choose at least one CSV input file.",
                    "Input Files Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtStartRow.Text, out var startRow) || startRow < 1)
            {
                // Default to the first data row if the user enters nothing or an invalid value.
                startRow = 1;
                txtStartRow.Text = "1";
            }

            // Prepare UI for processing.
            btnStart.Enabled = false;
            btnChooseInputFile.Enabled = false;
            btnBrowseLogLocation.Enabled = false;
            btnStop.Enabled = true;
            lblLogStatus.Text = "Running";

            // Reset counters for a fresh run.
            _processedFiles = 0;
            _errorFiles = 0;
            _rowsProcessed = 0;
            lblProcessedFilesValue.Text = "0";
            lblErrorFilesValue.Text = "0";
            lblRowsProcessedValue.Text = "0";
            UpdateErrorLogColor();

            _processingCancellationSource = new CancellationTokenSource();
            var token = _processingCancellationSource.Token;

            try
            {
                await ProcessFilesAsync(startRow, token);
                lblLogStatus.Text = token.IsCancellationRequested ? "Stopped" : "Completed";
            }
            finally
            {
                // Restore UI regardless of success, error, or cancellation.
                btnStart.Enabled = true;
                btnChooseInputFile.Enabled = true;
                btnBrowseLogLocation.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        /// <summary>
        /// Resets the form state so the user can configure a fresh
        /// run (clears selected files, counters, and status text).
        /// </summary>
        private void btnReset_Click(object? sender, EventArgs e)
        {
            _processingCancellationSource?.Cancel();
            _inputFiles.Clear();
            txtInputFile.Clear();
            txtCurrentFile.Clear();
            txtStartRow.Clear();

            _totalFiles = 0;
            _processedFiles = 0;
            _errorFiles = 0;
            _rowsProcessed = 0;

            lblTotalFilesValue.Text = "0";
            lblProcessedFilesValue.Text = "0";
            lblErrorFilesValue.Text = "0";
            lblRowsProcessedValue.Text = "0";
            lblLogStatus.Text = "Not Started";
            UpdateErrorLogColor();
        }

        /// <summary>
        /// Requests that the current processing run stop. The log
        /// files written so far are left on disk for review.
        /// </summary>
        private void btnStop_Click(object? sender, EventArgs e)
        {
            _processingCancellationSource?.Cancel();
            lblLogStatus.Text = "Stopping...";
        }

        /// <summary>
        /// Loops through each selected CSV input file and executes the selected
        /// Content Server action. Results are appended to the info log; an error
        /// log is only kept if at least one error or exception is written to it.
        /// </summary>
        private async Task ProcessFilesAsync(int startRow, CancellationToken cancellationToken)
        {
            foreach (var csvPath in _inputFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Track which CSV file is currently being processed.
                txtCurrentFile.Text = csvPath;

                // Create log and error log file names based on the CSV file name.
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(csvPath);
                var timestamp = DateTime.Now.ToString("ddMMyyyy_HHmm");

                var infoLogPath = Path.Combine(_logFolder!, $"{fileNameWithoutExt}_{timestamp}.log");
                var errorLogPath = Path.Combine(_logFolder!, $"{fileNameWithoutExt}_{timestamp}_error.log");

                // Remember the last info log path so "View File" can open it.
                _lastInfoLogPath = infoLogPath;

                using var infoWriter = new StreamWriter(infoLogPath, append: true, Encoding.UTF8);
                using var errorWriter = new StreamWriter(errorLogPath, append: true, Encoding.UTF8);

                var errorWrittenForThisFile = false;

                try
                {
                    // Process this CSV file and find out if any errors were logged.
                    errorWrittenForThisFile = await WriteActionLogsForFileAsync(
                        csvPath,
                        infoWriter,
                        errorWriter,
                        startRow,
                        cancellationToken);

                    _processedFiles++;
                    lblProcessedFilesValue.Text = _processedFiles.ToString();
                }
                catch (OperationCanceledException)
                {
                    // If cancelled, break out and keep whatever logs we have.
                    break;
                }
                catch (Exception ex)
                {
                    // Record unexpected errors for this file into the error log.
                    await errorWriter.WriteLineAsync($"[GENERAL ERROR] {DateTime.Now:u} - {ex}");
                    errorWrittenForThisFile = true;
                }

                // Flush the info log to disk before moving to the next file.
                await infoWriter.FlushAsync();

                if (errorWrittenForThisFile)
                {
                    // Only keep and count the error log file if something was written to it.
                    _errorFiles++;
                    lblErrorFilesValue.Text = _errorFiles.ToString();
                    UpdateErrorLogColor();
                    await errorWriter.FlushAsync();
                }
                else
                {
                    // Nothing was written to the error log, so we delete the empty file.
                    errorWriter.Close();
                    if (File.Exists(errorLogPath))
                    {
                        File.Delete(errorLogPath);
                    }
                }
            }
        }

        /// <summary>
        /// Executes the currently selected Content Server action for a single
        /// CSV file and writes formatted JSON responses to the log files.
        /// The returned boolean indicates whether any errors were written to
        /// the error log (used to decide whether to keep or delete it).
        /// </summary>
        private async Task<bool> WriteActionLogsForFileAsync(
            string csvPath,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            int startRow,
            CancellationToken cancellationToken)
        {
            // Tracks whether anything was actually written to the error log.
            var errorWritten = false;

            // Guard against missing input files.
            if (!File.Exists(csvPath))
            {
                await errorWriter.WriteLineAsync($"[FILE ERROR] {DateTime.Now:u} - File not found: {csvPath}");
                return true;
            }

            // Read all lines so we can easily access header + rows.
            var lines = await File.ReadAllLinesAsync(csvPath, cancellationToken);
            if (lines.Length == 0)
            {
                await errorWriter.WriteLineAsync($"[FILE ERROR] {DateTime.Now:u} - File is empty: {csvPath}");
                return true;
            }

            // Always write a header so the log clearly shows when
            // processing for this CSV file began and how many lines
            // were found in the file (including the header row).
            await infoWriter.WriteLineAsync(
                $"[START] {DateTime.Now:u} - Beginning '{_selectedAction}' for file '{csvPath}' with {lines.Length} total line(s).");

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Attach the ticket header so the API knows who we are.
            // We now authenticate against OTDS (returns an *OTDSSSO* ticket).
            // Keep OTCSTicket as well for any OTCS endpoints still in use.
            client.DefaultRequestHeaders.Add("OTDSTicket", _loginForm.AuthTicket);
            client.DefaultRequestHeaders.Add("OTCSTicket", _loginForm.AuthTicket);

            // Execute the appropriate API call based on the selected action.
            if (_selectedAction == ContentServerAction.SearchForUsers)
            {
                // For the Search for Users action, call POST /otdsws/rest/users/search for each CSV data row.
                // Parameters come from CSV columns (partitionName, state).
                errorWritten = await SearchOtdsUsersFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.SearchGroups)
            {
                // For the Search Groups action, call GET /otdsws/rest/groups.
                // If no CSV data, return all groups. If CSV has parameters, use them.
                errorWritten = await SearchOtdsGroupsAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.CreateGroups)
            {
                // For the Create Groups action, call POST /otdsws/rest/groups with parameters from CSV.
                errorWritten = await CreateOtdsGroupFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.CreateSubGroups)
            {
                // For the Create SubGroups action, call POST /otdsws/rest/groups/subgroup with parameters from CSV.
                errorWritten = await CreateOtdsSubGroupFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.UpdateGroups)
            {
                // For the Update Groups action, call PUT /otdsws/rest/group/{group_id} with parameters from CSV.
                errorWritten = await UpdateOtdsGroupFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.DeleteGroup)
            {
                // For the Delete Group action, call DELETE /otdsws/rest/group/{group_id} with group_id from CSV.
                errorWritten = await DeleteOtdsGroupFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.AddUserToGroup)
            {
                // For the Add User to Group action, call POST /otdsws/rest/users/{user_id}/memberof with parameters from CSV.
                errorWritten = await AddOtdsUserToGroupFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.RemoveUserFromGroup)
            {
                // For the Remove User from Group action, call POST /otdsws/rest/users/{user_id}/memberof/deletionset with parameters from CSV.
                errorWritten = await RemoveOtdsUserFromGroupFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.CreateUser)
            {
                // For the Create User action, call POST /otdsws/rest/users for each CSV data row.
                // Parameters come from CSV columns (userPartitionID, id, name, cn, sn, givenName, displayName, mail, userPassword).
                errorWritten = await CreateOtdsUsersFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.SearchUserById)
            {
                // For the Search User by ID action, call GET /otdsws/rest/users/{user_id} for each CSV data row.
                // The user_id parameter comes from CSV.
                errorWritten = await SearchUserByIdFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.UpdateUser)
            {
                // For the Update User action, call PUT /otdsws/rest/users/{user_id} for each CSV data row.
                // The user_id column identifies which user to update, other columns are fields to update.
                errorWritten = await UpdateOtdsUserFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.DeleteUser)
            {
                // For the Delete User action, call DELETE /otdsws/rest/users/{user_id} for each CSV data row.
                // The user_id comes from the CSV file.
                errorWritten = await DeleteOtdsUserFromCsvAsync(client, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }

            // Let the caller know whether any error content was written.
            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and searches for groups by calling GET /api/v2/members with
        /// query parameters from the CSV file. The CSV should contain columns for
        /// where_type and where_name. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> SearchGroupsFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = new Uri("http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get parameters from CSV columns.
                var whereTypeText = GetColumn(values, "where_type");
                var whereName = GetColumn(values, "where_name");

                // Validate where_type (should be 1 by default, but read from CSV).
                if (string.IsNullOrWhiteSpace(whereTypeText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'where_type' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(whereTypeText, out var whereTypeValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'where_type' value: '{whereTypeText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(whereName))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'where_name' value.");
                    errorWritten = true;
                    continue;
                }

                // Build the query string with parameters from CSV.
                var queryString = $"?where_type={whereTypeValue}&where_name={Uri.EscapeDataString(whereName)}";
                var requestUri = new Uri(baseUri, queryString);

                try
                {
                    using var response = await client.GetAsync(requestUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Searched groups from Row {dataRowNumber} (where_type={whereTypeValue}, where_name={whereName}):{Environment.NewLine}{formatted}");
                        // Count only successful searches toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Searches for groups in OTDS by calling GET /otdsws/rest/groups.
        /// If CSV has parameters, they are added as query parameters.
        /// If no CSV data is provided, returns all groups.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> SearchOtdsGroupsAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;
            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/groups";

            // Check if we have CSV data (more than just a header line)
            var hasData = lines.Length > 1 && lines.Skip(1).Any(l => !string.IsNullOrWhiteSpace(l));

            if (!hasData)
            {
                // No CSV data - return all groups
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var response = await client.GetAsync(baseUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Retrieved all OTDS groups:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - {ex}");
                    errorWritten = true;
                }

                return errorWritten;
            }

            // CSV has data - use parameters from each row
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Iterate data rows
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                try
                {
                    // Build query string from all CSV columns
                    var queryParams = new List<string>();
                    for (var j = 0; j < headers.Length && j < values.Length; j++)
                    {
                        var headerName = headers[j].Trim();
                        var value = values[j].Trim();
                        if (!string.IsNullOrWhiteSpace(headerName) && !string.IsNullOrWhiteSpace(value))
                        {
                            queryParams.Add($"{Uri.EscapeDataString(headerName)}={Uri.EscapeDataString(value)}");
                        }
                    }

                    var requestUri = queryParams.Count > 0
                        ? $"{baseUri}?{string.Join("&", queryParams)}"
                        : baseUri;

                    using var response = await client.GetAsync(requestUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Searched OTDS groups from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Creates groups in OTDS by calling POST /otdsws/rest/groups.
        /// The CSV should contain columns for userPartitionID and name (group name).
        /// Optionally can include description.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> CreateOtdsGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var createGroupUri = new Uri("http://dbscs.dcxeim.local:8080/otdsws/rest/groups");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get required parameters from CSV columns.
                var userPartitionID = GetColumn(values, "userPartitionID");
                var name = GetColumn(values, "name");
                var description = GetColumn(values, "description");

                // Validate required fields.
                if (string.IsNullOrWhiteSpace(userPartitionID))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'userPartitionID' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'name' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the values array with cn (common name).
                    var groupValues = new List<object>
                    {
                        new { name = "cn", values = new[] { name } }
                    };

                    // Build the request body.
                    var bodyObject = new Dictionary<string, object>
                    {
                        ["userPartitionID"] = userPartitionID,
                        ["name"] = name,
                        ["values"] = groupValues
                    };

                    // Add description if provided.
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        bodyObject["description"] = description;
                    }

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PostAsync(createGroupUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Created OTDS group (name='{name}', partition='{userPartitionID}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Creates subgroups in OTDS by calling POST /otdsws/rest/groups/subgroup.
        /// The CSV should contain columns for userPartitionID, name, and location (parent group location).
        /// Optionally can include parent_group_id which will be used to look up the parent location.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> CreateOtdsSubGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var createSubGroupUri = new Uri("http://dbscs.dcxeim.local:8080/otdsws/rest/groups/subgroup");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get required parameters from CSV columns.
                var userPartitionID = GetColumn(values, "userPartitionID");
                var name = GetColumn(values, "name");
                var location = GetColumn(values, "location");
                var parentGroupId = GetColumn(values, "parent_group_id");

                // Validate required fields.
                if (string.IsNullOrWhiteSpace(userPartitionID))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'userPartitionID' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'name' value.");
                    errorWritten = true;
                    continue;
                }

                // If location is not provided but parent_group_id is, look up the parent group's location.
                if (string.IsNullOrWhiteSpace(location) && !string.IsNullOrWhiteSpace(parentGroupId))
                {
                    try
                    {
                        var parentGroupUri = new Uri($"http://dbscs.dcxeim.local:8080/otdsws/rest/group/{Uri.EscapeDataString(parentGroupId)}");
                        using var parentResponse = await client.GetAsync(parentGroupUri, cancellationToken);
                        if (parentResponse.IsSuccessStatusCode)
                        {
                            var parentBody = await parentResponse.Content.ReadAsStringAsync(cancellationToken);
                            using var parentDoc = JsonDocument.Parse(parentBody);
                            if (parentDoc.RootElement.TryGetProperty("location", out var locationProp) &&
                                locationProp.ValueKind == JsonValueKind.String)
                            {
                                location = locationProp.GetString() ?? string.Empty;
                            }
                        }
                        else
                        {
                            await errorWriter.WriteLineAsync(
                                $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Failed to look up parent group '{parentGroupId}'.");
                            errorWritten = true;
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Error looking up parent group: {ex.Message}");
                        errorWritten = true;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing 'location' or 'parent_group_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the values array with cn (common name).
                    var groupValues = new List<object>
                    {
                        new { name = "cn", values = new[] { name } }
                    };

                    // Build the request body.
                    var bodyObject = new Dictionary<string, object>
                    {
                        ["userPartitionID"] = userPartitionID,
                        ["name"] = name,
                        ["location"] = location,
                        ["values"] = groupValues
                    };

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PostAsync(createSubGroupUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        // Extract parent group name from location for logging.
                        var parentName = ExtractParentGroupNameFromLocation(location);
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Created OTDS subgroup (name='{name}', parent='{parentName}', partition='{userPartitionID}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Extracts the parent group name from a location DN string.
        /// Example: "oTGroup=681fded9-2c69-4ea7-acdc-0ab6e8e110ec,orgunit=groups,partition=Content Server Members,dc=identity,dc=opentext,dc=net"
        /// </summary>
        private static string ExtractParentGroupNameFromLocation(string location)
        {
            // The location is a DN - we can try to extract meaningful info from it
            // For now, return a shortened version or the partition name
            if (string.IsNullOrWhiteSpace(location))
            {
                return string.Empty;
            }

            // Try to extract partition name
            var partitionMatch = System.Text.RegularExpressions.Regex.Match(location, @"partition=([^,]+)");
            if (partitionMatch.Success)
            {
                return $"(in {partitionMatch.Groups[1].Value})";
            }

            // Return a truncated version if too long
            return location.Length > 50 ? location.Substring(0, 47) + "..." : location;
        }

        /// <summary>
        /// Updates groups in OTDS by calling PUT /otdsws/rest/group/{group_id}.
        /// The CSV should contain a group_id column plus any columns for fields to update
        /// (e.g., name, description, cn).
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> UpdateOtdsGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/group";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var groupId = GetColumn(values, "group_id");

                if (string.IsNullOrWhiteSpace(groupId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the values array from columns that should go into values array.
                    // Known field names that OTDS supports in the values array for groups.
                    var knownValueFields = new[] { "cn", "description" };
                    var groupValues = new List<object>();

                    foreach (var fieldName in knownValueFields)
                    {
                        var fieldValue = GetColumn(values, fieldName);
                        if (!string.IsNullOrWhiteSpace(fieldValue))
                        {
                            groupValues.Add(new { name = fieldName, values = new[] { fieldValue } });
                        }
                    }

                    // Build the request body.
                    var bodyObject = new Dictionary<string, object>();

                    // Add name if provided (top-level field).
                    var name = GetColumn(values, "name");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        bodyObject["name"] = name;
                    }

                    // Add values array if we have any fields.
                    if (groupValues.Count > 0)
                    {
                        bodyObject["values"] = groupValues;
                    }

                    if (bodyObject.Count == 0)
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: No fields to update. Provide at least one of: name, cn, description");
                        errorWritten = true;
                        continue;
                    }

                    // Build the PUT URL: /otdsws/rest/group/{group_id}
                    var updateGroupUri = new Uri($"{baseUri}/{Uri.EscapeDataString(groupId)}");

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PutAsync(updateGroupUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        // Build a summary of updated fields.
                        var updatedFields = new List<string>();
                        if (!string.IsNullOrWhiteSpace(name)) updatedFields.Add("name");
                        updatedFields.AddRange(knownValueFields.Where(f => !string.IsNullOrWhiteSpace(GetColumn(values, f))));
                        
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Updated OTDS group (group_id='{groupId}', fields: {string.Join(", ", updatedFields)}) from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Deletes groups in OTDS by calling DELETE /otdsws/rest/group/{group_id}.
        /// The CSV should contain a group_id column identifying the group to delete.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> DeleteOtdsGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/group";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var groupId = GetColumn(values, "group_id");

                if (string.IsNullOrWhiteSpace(groupId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the DELETE URL: /otdsws/rest/group/{group_id}
                    var deleteGroupUri = new Uri($"{baseUri}/{Uri.EscapeDataString(groupId)}");

                    using var response = await client.DeleteAsync(deleteGroupUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Deleted OTDS group (group_id='{groupId}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and creates groups by calling POST /api/v2/members with
        /// parameters from the CSV file. The CSV should contain columns for
        /// type and name. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> CreateGroupsFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = new Uri("http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get parameters from CSV columns.
                var typeText = GetColumn(values, "type");
                var name = GetColumn(values, "name");

                // Validate type.
                if (string.IsNullOrWhiteSpace(typeText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'type' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(typeText, out var typeValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'type' value: '{typeText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'name' value.");
                    errorWritten = true;
                    continue;
                }

                // Build the JSON request body for group creation.
                var requestObject = new
                {
                    type = typeValue,
                    name = name
                };

                // Serialize to JSON and send POST /api/v2/members.
                var json = JsonSerializer.Serialize(requestObject);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    using var response = await client.PostAsync(baseUri, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Created group from Row {dataRowNumber} (type={typeValue}, name={name}):{Environment.NewLine}{formatted}");
                        // Count only successful creates toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and creates subgroups by calling POST /api/v2/members with
        /// parameters from the CSV file. The CSV should contain columns for
        /// type, name, and parent_id. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> CreateSubGroupsFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = new Uri("http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get parameters from CSV columns.
                var typeText = GetColumn(values, "type");
                var name = GetColumn(values, "name");
                var parentIdText = GetColumn(values, "parent_id");

                // Validate type.
                if (string.IsNullOrWhiteSpace(typeText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'type' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(typeText, out var typeValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'type' value: '{typeText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'name' value.");
                    errorWritten = true;
                    continue;
                }

                // Validate parent_id.
                if (string.IsNullOrWhiteSpace(parentIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'parent_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(parentIdText, out var parentIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'parent_id' value: '{parentIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Build the JSON request body for subgroup creation.
                var requestObject = new
                {
                    type = typeValue,
                    name = name,
                    parent_id = parentIdValue
                };

                // Serialize to JSON and send POST /api/v2/members.
                var json = JsonSerializer.Serialize(requestObject);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    using var response = await client.PostAsync(baseUri, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Created subgroup from Row {dataRowNumber} (type={typeValue}, name={name}, parent_id={parentIdValue}):{Environment.NewLine}{formatted}");
                        // Count only successful creates toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and updates groups by calling PUT /api/v2/members/{group_id} with
        /// parameters from the CSV file. The CSV should contain columns for
        /// type, group_id, and name. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> UpdateGroupsFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = "http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get parameters from CSV columns.
                var typeText = GetColumn(values, "type");
                var groupIdText = GetColumn(values, "group_id");
                var name = GetColumn(values, "name");

                // Validate type.
                if (string.IsNullOrWhiteSpace(typeText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'type' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(typeText, out var typeValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'type' value: '{typeText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Validate group_id.
                if (string.IsNullOrWhiteSpace(groupIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(groupIdText, out var groupIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'group_id' value: '{groupIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'name' value.");
                    errorWritten = true;
                    continue;
                }

                // Build the PUT URL with the group_id.
                var updateUri = new Uri($"{baseUri}/{groupIdValue}");

                // Build the JSON request body for group update.
                var requestObject = new
                {
                    type = typeValue,
                    name = name
                };

                // Serialize to JSON and send PUT /api/v2/members/{group_id}.
                var json = JsonSerializer.Serialize(requestObject);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    using var response = await client.PutAsync(updateUri, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Updated group from Row {dataRowNumber} (type={typeValue}, group_id={groupIdValue}, name={name}):{Environment.NewLine}{formatted}");
                        // Count only successful updates toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and deletes groups by calling DELETE /api/v2/members/{group_id}.
        /// The CSV should contain a column for group_id. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> DeleteGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can find the group_id column.
            var headers = lines[0].Split(',');

            // Find the group_id column index (case-insensitive search).
            var groupIdColumnIndex = Array.FindIndex(headers, h => h.Trim().Equals("group_id", StringComparison.OrdinalIgnoreCase));

            // If no "group_id" header is found, assume the first column is the group_id.
            if (groupIdColumnIndex < 0)
            {
                groupIdColumnIndex = 0;
            }

            // Local helper: return the value for a column index, or empty string if missing.
            string GetColumnValue(string[] rowValues, int columnIndex)
            {
                return columnIndex >= 0 && columnIndex < rowValues.Length ? rowValues[columnIndex].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = "http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Extract group_id from the CSV row.
                var groupIdText = GetColumnValue(values, groupIdColumnIndex);
                if (string.IsNullOrWhiteSpace(groupIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty group_id.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(groupIdText, out var groupIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'group_id' value: '{groupIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Build the DELETE URL with the group_id from the CSV.
                var deleteUri = new Uri($"{baseUri}/{groupIdValue}");

                try
                {
                    // Send DELETE request to /api/v2/members/{group_id}.
                    using var response = await client.DeleteAsync(deleteUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Deleted group {groupIdValue} from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        // Count only successful deletes toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}, Group ID '{groupIdValue}': HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}, Group ID '{groupIdValue}': {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Adds users to groups in OTDS by calling POST /otdsws/rest/users/{user_id}/memberof.
        /// The CSV should contain columns for user_id and group_id.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> AddOtdsUserToGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/users";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var userId = GetColumn(values, "user_id");
                var groupId = GetColumn(values, "group_id");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'user_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(groupId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the POST URL: /otdsws/rest/users/{user_id}/memberof
                    var addToGroupUri = new Uri($"{baseUri}/{Uri.EscapeDataString(userId)}/memberof");

                    // Build the request body with the group ID in stringList.
                    var bodyObject = new Dictionary<string, object>
                    {
                        ["stringList"] = new[] { groupId }
                    };

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PostAsync(addToGroupUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Added user '{userId}' to group '{groupId}' from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Removes users from groups in OTDS by calling POST /otdsws/rest/users/{user_id}/memberof/deletionset.
        /// The CSV should contain columns for user_id and group_id.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> RemoveOtdsUserFromGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/users";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var userId = GetColumn(values, "user_id");
                var groupId = GetColumn(values, "group_id");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'user_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(groupId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the POST URL: /otdsws/rest/users/{user_id}/memberof/deletionset
                    var removeFromGroupUri = new Uri($"{baseUri}/{Uri.EscapeDataString(userId)}/memberof/deletionset");

                    // Build the request body with the group ID in stringList.
                    var bodyObject = new Dictionary<string, object>
                    {
                        ["stringList"] = new[] { groupId }
                    };

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PostAsync(removeFromGroupUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Removed user '{userId}' from group '{groupId}' from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and adds users to groups by calling POST /api/v2/members/{group_id}/members.
        /// The CSV should contain columns for group_id and member_id. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> AddUserToGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = "http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get parameters from CSV columns.
                var groupIdText = GetColumn(values, "group_id");
                var memberIdText = GetColumn(values, "member_id");

                // Validate group_id.
                if (string.IsNullOrWhiteSpace(groupIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(groupIdText, out var groupIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'group_id' value: '{groupIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Validate member_id.
                if (string.IsNullOrWhiteSpace(memberIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'member_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(memberIdText, out var memberIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'member_id' value: '{memberIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Build the POST URL: /api/v2/members/{group_id}/members
                var postUri = new Uri($"{baseUri}/{groupIdValue}/members");

                // Build the JSON request body for adding user to group.
                var requestObject = new
                {
                    member_id = memberIdValue
                };

                // Serialize to JSON and send POST.
                var json = JsonSerializer.Serialize(requestObject);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    using var response = await client.PostAsync(postUri, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Added user {memberIdValue} to group {groupIdValue} from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        // Count only successful additions toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and removes users from groups by calling DELETE /api/v2/members/{group_id}/members/{member_id}.
        /// The CSV should contain columns for group_id and member_id. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> RemoveUserFromGroupFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Base URI for the groups API endpoint.
            var baseUri = "http://dbscs.dcxeim.local/otcs/cs.exe/api/v2/members";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get parameters from CSV columns.
                var groupIdText = GetColumn(values, "group_id");
                var memberIdText = GetColumn(values, "member_id");

                // Validate group_id.
                if (string.IsNullOrWhiteSpace(groupIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'group_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(groupIdText, out var groupIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'group_id' value: '{groupIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Validate member_id.
                if (string.IsNullOrWhiteSpace(memberIdText))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'member_id' value.");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(memberIdText, out var memberIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid 'member_id' value: '{memberIdText}'. Expected a number.");
                    errorWritten = true;
                    continue;
                }

                // Build the DELETE URL: /api/v2/members/{group_id}/members?group_id=...&member_id=...
                var deleteUri = new Uri($"{baseUri}/{groupIdValue}/members?group_id={groupIdValue}&member_id={memberIdValue}");

                try
                {
                    // Send DELETE request to remove user from group.
                    using var response = await client.DeleteAsync(deleteUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Removed user {memberIdValue} from group {groupIdValue} from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        // Count only successful removals toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and searches for users in OTDS by calling POST /otdsws/rest/users/search.
        /// The CSV should contain columns for partitionName and state. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> SearchOtdsUsersFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var searchUri = new Uri("http://dbscs.dcxeim.local:8080/otdsws/rest/users/search");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var partitionName = GetColumn(values, "partitionName");
                var state = GetColumn(values, "state");

                if (string.IsNullOrWhiteSpace(partitionName))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'partitionName' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(state))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'state' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    var bodyObject = new
                    {
                        partitionName,
                        state
                    };

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PostAsync(searchUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Searched OTDS users (partitionName='{partitionName}', state='{state}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and searches for users by ID by calling GET /otdsws/rest/users/{user_id}.
        /// The CSV should contain a column for user_id. Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> SearchUserByIdFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/users";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var userId = GetColumn(values, "user_id");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'user_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the GET URL: /otdsws/rest/users/{user_id}
                    var getUserUri = new Uri($"{baseUri}/{Uri.EscapeDataString(userId)}");

                    using var response = await client.GetAsync(getUserUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Found OTDS user (user_id='{userId}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and creates users in OTDS by calling POST /otdsws/rest/users.
        /// The CSV should contain columns for userPartitionID, id, name, location, cn, sn, givenName, displayName, mail, userPassword.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> CreateOtdsUsersFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var createUserUri = new Uri("http://dbscs.dcxeim.local:8080/otdsws/rest/users");

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Get required parameters from CSV columns.
                var userPartitionID = GetColumn(values, "userPartitionID");
                var id = GetColumn(values, "id");
                var name = GetColumn(values, "name");
                var location = GetColumn(values, "location");
                var cn = GetColumn(values, "cn");
                var sn = GetColumn(values, "sn");
                var givenName = GetColumn(values, "givenName");
                var displayName = GetColumn(values, "displayName");
                var mail = GetColumn(values, "mail");
                var userPassword = GetColumn(values, "userPassword");

                // Validate required fields.
                if (string.IsNullOrWhiteSpace(userPartitionID))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'userPartitionID' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(id))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'id' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cn))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'cn' value.");
                    errorWritten = true;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sn))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'sn' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the values array for the user attributes.
                    var userValues = new List<object>
                    {
                        new { name = "cn", values = new[] { cn } },
                        new { name = "sn", values = new[] { sn } }
                    };

                    if (!string.IsNullOrWhiteSpace(givenName))
                    {
                        userValues.Add(new { name = "givenName", values = new[] { givenName } });
                    }

                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        userValues.Add(new { name = "displayName", values = new[] { displayName } });
                    }

                    if (!string.IsNullOrWhiteSpace(mail))
                    {
                        userValues.Add(new { name = "mail", values = new[] { mail } });
                    }

                    if (!string.IsNullOrWhiteSpace(userPassword))
                    {
                        userValues.Add(new { name = "userPassword", values = new[] { userPassword } });
                    }

                    // Build the request body.
                    var bodyObject = new Dictionary<string, object>
                    {
                        ["userPartitionID"] = userPartitionID,
                        ["id"] = id,
                        ["values"] = userValues
                    };

                    // Add optional fields if provided.
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        bodyObject["name"] = name;
                    }

                    if (!string.IsNullOrWhiteSpace(location))
                    {
                        bodyObject["location"] = location;
                    }

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PostAsync(createUserUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Created OTDS user (id='{id}', partition='{userPartitionID}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and updates users in OTDS by calling PUT /otdsws/rest/users/{user_id}.
        /// The CSV should contain a user_id column plus any columns for fields to update
        /// (e.g., displayName, mail, givenName, sn, etc.).
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> UpdateOtdsUserFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Find the index of the user_id column.
            var userIdIndex = Array.FindIndex(headers, h => h.Trim().Equals("user_id", StringComparison.OrdinalIgnoreCase));

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/users";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var userId = GetColumn(values, "user_id");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'user_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the values array from all columns except user_id.
                    // Known field names that OTDS supports in the values array.
                    var knownFields = new[] { "displayName", "mail", "givenName", "sn", "cn", "userPassword" };
                    var userValues = new List<object>();

                    foreach (var fieldName in knownFields)
                    {
                        var fieldValue = GetColumn(values, fieldName);
                        if (!string.IsNullOrWhiteSpace(fieldValue))
                        {
                            userValues.Add(new { name = fieldName, values = new[] { fieldValue } });
                        }
                    }

                    if (userValues.Count == 0)
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: No fields to update. Provide at least one of: {string.Join(", ", knownFields)}");
                        errorWritten = true;
                        continue;
                    }

                    // Build the request body.
                    var bodyObject = new Dictionary<string, object>
                    {
                        ["values"] = userValues
                    };

                    // Build the PUT URL: /otdsws/rest/users/{user_id}
                    var updateUserUri = new Uri($"{baseUri}/{Uri.EscapeDataString(userId)}");

                    using var requestContent = new StringContent(
                        JsonSerializer.Serialize(bodyObject),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    using var response = await client.PutAsync(updateUserUri, requestContent, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        // Build a summary of updated fields.
                        var updatedFields = string.Join(", ", knownFields.Where(f => !string.IsNullOrWhiteSpace(GetColumn(values, f))));
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Updated OTDS user (user_id='{userId}', fields: {updatedFields}) from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and deletes users in OTDS by calling DELETE /otdsws/rest/users/{user_id}.
        /// The CSV should contain a user_id column identifying the user to delete.
        /// Each row will result in a separate API call.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> DeleteOtdsUserFromCsvAsync(
            HttpClient client,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            var baseUri = "http://dbscs.dcxeim.local:8080/otdsws/rest/users";

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                var userId = GetColumn(values, "user_id");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty 'user_id' value.");
                    errorWritten = true;
                    continue;
                }

                try
                {
                    // Build the DELETE URL: /otdsws/rest/users/{user_id}
                    var deleteUserUri = new Uri($"{baseUri}/{Uri.EscapeDataString(userId)}");

                    using var response = await client.DeleteAsync(deleteUserUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Deleted OTDS user (user_id='{userId}') from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Calls GET /api/v1/members one time and logs the formatted JSON response.
        /// Returns true if an error was written to the error log.
        /// </summary>
        private async Task<bool> LogMembersGetOnceAsync(
            HttpClient client,
            Uri membersUri,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var response = await client.GetAsync(membersUri, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var formatted = FormatJsonForLog(responseBody);

                if (response.IsSuccessStatusCode)
                {
                    await infoWriter.WriteLineAsync(
                        $"[SUCCESS] {DateTime.Now:u} - Members response:{Environment.NewLine}{formatted}");
                    return false;
                }

                await errorWriter.WriteLineAsync(
                    $"[ERROR] {DateTime.Now:u} - HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await errorWriter.WriteLineAsync($"[EXCEPTION] {DateTime.Now:u} - {ex}");
                return true;
            }
        }

        /// <summary>
        /// Reads CSV rows and creates users by POSTing to /api/v1/members.
        /// Each row must provide the required fields via matching CSV headers:
        /// type,name,first_name,last_name,password,group_id,personal_email,
        /// business_email,gender (case-insensitive). The actual values all
        /// come from the CSV file and are not hard-coded in the code.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> CreateUsersFromCsvAsync(
            HttpClient client,
            Uri membersUri,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            // Local helper: return the value for a header name, or empty string if missing.
            string GetColumn(string[] rowValues, string columnName)
            {
                var index = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
                return index >= 0 && index < rowValues.Length ? rowValues[index].Trim() : string.Empty;
            }

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Build the POST body from CSV columns.
                // These values are NOT hard-coded: they come from the CSV.
                var typeText = GetColumn(values, "type");
                var groupIdText = GetColumn(values, "group_id");

                if (!int.TryParse(typeText, out var typeValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid or missing 'type' value: '{typeText}'");
                    errorWritten = true;
                    continue;
                }

                if (!int.TryParse(groupIdText, out var groupIdValue))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Invalid or missing 'group_id' value: '{groupIdText}'");
                    errorWritten = true;
                    continue;
                }

                // Build the JSON request body for user creation using values from
                // the CSV columns. Nothing here is hard-coded; all example values
                // are expected to come from the CSV file.
                var requestObject = new
                {
                    type = typeValue,
                    name = GetColumn(values, "name"), // login name
                    first_name = GetColumn(values, "first_name"), // first name
                    last_name = GetColumn(values, "last_name"), // last name
                    password = GetColumn(values, "password"), // password
                    group_id = groupIdValue, // group id
                    personal_email = GetColumn(values, "personal_email"), // personal email
                    business_email = GetColumn(values, "business_email"), // business email
                    gender = GetColumn(values, "gender") // gender
                };

                // Serialize to JSON and send POST /members.
                var json = JsonSerializer.Serialize(requestObject);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    using var response = await client.PostAsync(membersUri, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Created user from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        // Count only successful creates toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and updates users by PUTting to /api/v1/members/{user_id}.
        /// The first column in the CSV must be the user_id. All other columns
        /// (from the header row) are treated as fields to update and are included
        /// in the JSON request body. The actual field names and values all come
        /// from the CSV file and are not hard-coded in the code.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> UpdateUsersFromCsvAsync(
            HttpClient client,
            Uri baseMembersUri,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can map column names to values.
            var headers = lines[0].Split(',');

            if (headers.Length < 2)
            {
                await errorWriter.WriteLineAsync(
                    $"[FILE ERROR] {DateTime.Now:u} - File '{csvPath}': CSV must have at least 2 columns (user_id and at least one field to update).");
                return true;
            }

            // The first column is the user_id, all others are update fields.
            var userIdColumnIndex = 0;
            var updateFieldHeaders = new string[headers.Length - 1];
            for (var i = 1; i < headers.Length; i++)
            {
                updateFieldHeaders[i - 1] = headers[i].Trim();
            }

            // Local helper: return the value for a column index, or empty string if missing.
            string GetColumnValue(string[] rowValues, int columnIndex)
            {
                return columnIndex >= 0 && columnIndex < rowValues.Length ? rowValues[columnIndex].Trim() : string.Empty;
            }

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Extract user_id from the first column.
                var userId = GetColumnValue(values, userIdColumnIndex);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty user_id in first column.");
                    errorWritten = true;
                    continue;
                }

                // Build a dictionary of all update fields from the remaining columns.
                // This dynamically includes whatever fields are in the CSV (no hard-coding).
                var updateFields = new Dictionary<string, object?>();
                for (var j = 0; j < updateFieldHeaders.Length; j++)
                {
                    var fieldName = updateFieldHeaders[j];
                    var fieldValue = GetColumnValue(values, j + 1); // +1 because first column is user_id

                    // Skip empty fields (they won't be included in the update).
                    if (string.IsNullOrWhiteSpace(fieldValue))
                    {
                        continue;
                    }

                    // Try to parse as number if it looks like one, otherwise keep as string.
                    if (int.TryParse(fieldValue, out var intValue))
                    {
                        updateFields[fieldName] = intValue;
                    }
                    else if (bool.TryParse(fieldValue, out var boolValue))
                    {
                        updateFields[fieldName] = boolValue;
                    }
                    else
                    {
                        updateFields[fieldName] = fieldValue;
                    }
                }

                if (updateFields.Count == 0)
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: No update fields provided (all columns after user_id are empty).");
                    errorWritten = true;
                    continue;
                }

                // Build the PUT URL with the user_id from the first column.
                var updateUri = new Uri($"{baseMembersUri}/{userId}");

                // Serialize the update fields dictionary to JSON.
                var json = JsonSerializer.Serialize(updateFields);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    using var response = await client.PutAsync(updateUri, content, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Updated user {userId} from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        // Count only successful updates toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}, User ID '{userId}': HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}, User ID '{userId}': {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Reads CSV rows and deletes users by sending DELETE requests to
        /// /api/v1/members/{user_id}. The user_id is extracted from the CSV file
        /// (typically the first column, but can be any column if the header
        /// contains "user_id"). The actual user_id values all come from the CSV
        /// file and are not hard-coded in the code.
        /// Returns true if any errors were written to the error log.
        /// </summary>
        private async Task<bool> DeleteUsersFromCsvAsync(
            HttpClient client,
            Uri baseMembersUri,
            string[] lines,
            string csvPath,
            int startRow,
            StreamWriter infoWriter,
            StreamWriter errorWriter,
            CancellationToken cancellationToken)
        {
            var errorWritten = false;

            // Use the first line as headers so we can find the user_id column.
            var headers = lines[0].Split(',');

            // Find the user_id column index (case-insensitive search).
            var userIdColumnIndex = Array.FindIndex(headers, h => h.Trim().Equals("user_id", StringComparison.OrdinalIgnoreCase));

            // If no "user_id" header is found, assume the first column is the user_id.
            if (userIdColumnIndex < 0)
            {
                userIdColumnIndex = 0;
            }

            // Local helper: return the value for a column index, or empty string if missing.
            string GetColumnValue(string[] rowValues, int columnIndex)
            {
                return columnIndex >= 0 && columnIndex < rowValues.Length ? rowValues[columnIndex].Trim() : string.Empty;
            }

            // Iterate data rows; startRow is 1-based for the first data row after header.
            for (var i = 1; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dataRowNumber = i;
                if (dataRowNumber < startRow)
                {
                    continue;
                }

                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = line.Split(',');

                // Extract user_id from the CSV row.
                var userId = GetColumnValue(values, userIdColumnIndex);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    await errorWriter.WriteLineAsync(
                        $"[ROW ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}: Missing or empty user_id.");
                    errorWritten = true;
                    continue;
                }

                // Build the DELETE URL with the user_id from the CSV.
                var deleteUri = new Uri($"{baseMembersUri}/{userId}");

                try
                {
                    // Send DELETE request to /api/v1/members/{user_id}.
                    using var response = await client.DeleteAsync(deleteUri, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var formatted = FormatJsonForLog(responseBody);

                    if (response.IsSuccessStatusCode)
                    {
                        await infoWriter.WriteLineAsync(
                            $"[SUCCESS] {DateTime.Now:u} - Deleted user {userId} from Row {dataRowNumber}:{Environment.NewLine}{formatted}");
                        // Count only successful deletes toward rows processed (excludes header).
                        _rowsProcessed++;
                        lblRowsProcessedValue.Text = _rowsProcessed.ToString();
                    }
                    else
                    {
                        await errorWriter.WriteLineAsync(
                            $"[ERROR] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}, User ID '{userId}': HTTP {(int)response.StatusCode} {response.ReasonPhrase}{Environment.NewLine}{formatted}");
                        errorWritten = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await errorWriter.WriteLineAsync(
                        $"[EXCEPTION] {DateTime.Now:u} - File '{csvPath}', Row {dataRowNumber}, User ID '{userId}': {ex}");
                    errorWritten = true;
                }
            }

            return errorWritten;
        }

        /// <summary>
        /// Updates the "Error log" radio button and "Error Files" labels to red
        /// when there are errors (error count > 0), or back to the default color
        /// when there are none. This provides visual feedback to the user about error status.
        /// </summary>
        private void UpdateErrorLogColor()
        {
            // Determine the color based on whether errors exist.
            var errorColor = _errorFiles > 0
                ? System.Drawing.Color.Red
                : System.Drawing.SystemColors.ControlText;

            // Change the Error Log radio button text color.
            radioErrorLog.ForeColor = errorColor;

            // Change the "Error Files:" label and its value label to red when errors exist.
            lblErrorFiles.ForeColor = errorColor;
            lblErrorFilesValue.ForeColor = errorColor;
        }

        /// <summary>
        /// Attempts to pretty-print JSON for easier readability in log files.
        /// If parsing fails, returns the original input unchanged.
        /// </summary>
        private static string FormatJsonForLog(string input)
        {
            try
            {
                using var document = JsonDocument.Parse(input);

                // Serialize with indentation so nested objects and arrays are readable.
                return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                // If the response is not valid JSON, keep it as-is.
                return input;
            }
        }

        private void lblTotalFilesValue_Click(object sender, EventArgs e)
        {

        }
    }
}

