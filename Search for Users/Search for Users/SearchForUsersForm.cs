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
            this.Text = selectedAction switch
            {
                ContentServerAction.CreateUser => "Create User",
                ContentServerAction.UpdateUser => "Update User",
                ContentServerAction.DeleteUser => "Delete User",
                _ => "Search for Users"
            };

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

            // Attach the OTCSTicket header so the API knows who we are.
            client.DefaultRequestHeaders.Add("OTCSTicket", _loginForm.AuthTicket);

            // Build the fixed /api/v1/members endpoint URL for both actions.
            var membersUri = new Uri("http://dbscs.dcxeim.local/otcs/cs.exe/api/v1/members");

            // Execute the appropriate API call based on the selected action.
            if (_selectedAction == ContentServerAction.SearchForUsers)
            {
                // For the Search action, call GET /members once per CSV file.
                errorWritten = await LogMembersGetOnceAsync(client, membersUri, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.CreateUser)
            {
                // For the Create action, call POST /members for each CSV data row (starting at startRow).
                errorWritten = await CreateUsersFromCsvAsync(client, membersUri, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.UpdateUser)
            {
                // For the Update action, call PUT /members/{user_id} for each CSV data row.
                // The first column is the user_id, and all other columns are fields to update.
                errorWritten = await UpdateUsersFromCsvAsync(client, membersUri, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }
            else if (_selectedAction == ContentServerAction.DeleteUser)
            {
                // For the Delete action, call DELETE /members/{user_id} for each CSV data row.
                // The user_id comes from the CSV file (typically the first column).
                errorWritten = await DeleteUsersFromCsvAsync(client, membersUri, lines, csvPath, startRow, infoWriter, errorWriter, cancellationToken);
            }

            // Let the caller know whether any error content was written.
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
    }
}

