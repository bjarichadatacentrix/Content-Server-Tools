using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Creates the report form with the specified log file and title.
        /// </summary>
        /// <param name="logFilePath">Path to the log file to parse.</param>
        /// <param name="title">Title to display in the form's title bar.</param>
        /// <param name="selectedAction">The action that generated the log file.</param>
        public ReportForm(string logFilePath, string title, ContentServerAction selectedAction = ContentServerAction.SearchForUsers)
        {
            _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
            _selectedAction = selectedAction;

            InitializeComponent();

            this.Text = title;
            LoadLogFile();
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
                        user.Cn);
                }

                lblRecordCount.Text = $"Records: {users.Count}";
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
                dataGridViewReport.Rows.Add(group.GroupName, group.DateCreated);
            }

            lblRecordCount.Text = $"Groups: {groups.Count}";
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
            var cn = string.Empty;
            var createTimestamp = string.Empty;

            // Helper to extract first value from an array property
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

            // Try to get cn directly from the group element
            cn = GetFirstArrayValue(groupElement, "cn");

            // If not found, try inside "attributes" object
            if (string.IsNullOrEmpty(cn) &&
                groupElement.TryGetProperty("attributes", out var attributes))
            {
                cn = GetFirstArrayValue(attributes, "cn");
            }

            // If cn is still empty, try to get name directly from the element
            if (string.IsNullOrEmpty(cn) &&
                groupElement.TryGetProperty("name", out var nameProp) &&
                nameProp.ValueKind == JsonValueKind.String)
            {
                cn = nameProp.GetString() ?? string.Empty;
            }

            // Try to get createTimestamp directly from the group element
            createTimestamp = GetFirstArrayValue(groupElement, "createTimestamp");

            // If not found, try inside "attributes" object
            if (string.IsNullOrEmpty(createTimestamp) &&
                groupElement.TryGetProperty("attributes", out var attrs))
            {
                createTimestamp = GetFirstArrayValue(attrs, "createTimestamp");
            }

            // Format the timestamp for display if present
            if (!string.IsNullOrEmpty(createTimestamp))
            {
                // Try to parse and format the timestamp
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

                    // Check if this JSON has a "users" array (search results).
                    if (root.TryGetProperty("users", out var usersArray) &&
                        usersArray.ValueKind == JsonValueKind.Array)
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
                    Cn = cn
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
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                var logEntry = row.Cells[0].Value?.ToString() ?? string.Empty;
                                // Escape values for CSV.
                                writer.WriteLine($"\"{logEntry.Replace("\"", "\"\"")}\"");
                            }
                        }
                    }
                    else if (_selectedAction == ContentServerAction.AddUserToGroup)
                    {
                        // Write header for user-group assignments view.
                        writer.WriteLine("User,Group");

                        // Write data rows.
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                var user = row.Cells[0].Value?.ToString() ?? string.Empty;
                                var group = row.Cells[1].Value?.ToString() ?? string.Empty;

                                // Escape values for CSV.
                                writer.WriteLine(
                                    $"\"{user.Replace("\"", "\"\"")}\","
                                    + $"\"{group.Replace("\"", "\"\"")}\"");
                            }
                        }
                    }
                    else if (_selectedAction == ContentServerAction.SearchGroups)
                    {
                        // Write header for groups view.
                        writer.WriteLine("Group Name,Date Created");

                        // Write data rows.
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                var groupName = row.Cells[0].Value?.ToString() ?? string.Empty;
                                var dateCreated = row.Cells[1].Value?.ToString() ?? string.Empty;

                                // Escape values for CSV.
                                writer.WriteLine(
                                    $"\"{groupName.Replace("\"", "\"\"")}\","
                                    + $"\"{dateCreated.Replace("\"", "\"\"")}\"");
                            }
                        }
                    }
                    else if (_selectedAction == ContentServerAction.CreateGroups)
                    {
                        // Write header for created groups view.
                        writer.WriteLine("Partition Name,Group Name");

                        // Write data rows.
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                var partitionName = row.Cells[0].Value?.ToString() ?? string.Empty;
                                var groupName = row.Cells[1].Value?.ToString() ?? string.Empty;

                                // Escape values for CSV.
                                writer.WriteLine(
                                    $"\"{partitionName.Replace("\"", "\"\"")}\","
                                    + $"\"{groupName.Replace("\"", "\"\"")}\"");
                            }
                        }
                    }
                    else if (_selectedAction == ContentServerAction.UpdateGroups)
                    {
                        // Write header for updated groups view.
                        writer.WriteLine("Partition Name,Group Name,Description,Name");

                        // Write data rows.
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
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
                    }
                    else if (_selectedAction == ContentServerAction.CreateSubGroups)
                    {
                        // Write header for created subgroups view.
                        writer.WriteLine("Partition Name,Parent Group Name,Subgroup Name");

                        // Write data rows.
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
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
                    }
                    else
                    {
                        // Write header for standard user data view.
                        writer.WriteLine("User ID,User Partition ID,Name,Surname,Display Name,Mail,CN");

                        // Write data rows.
                        foreach (DataGridViewRow row in dataGridViewReport.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                var userId = row.Cells[0].Value?.ToString() ?? string.Empty;
                                var userPartitionID = row.Cells[1].Value?.ToString() ?? string.Empty;
                                var name = row.Cells[2].Value?.ToString() ?? string.Empty;
                                var surname = row.Cells[3].Value?.ToString() ?? string.Empty;
                                var displayName = row.Cells[4].Value?.ToString() ?? string.Empty;
                                var mail = row.Cells[5].Value?.ToString() ?? string.Empty;
                                var cn = row.Cells[6].Value?.ToString() ?? string.Empty;
                                
                                // Escape values for CSV.
                                writer.WriteLine(
                                    $"\"{userId.Replace("\"", "\"\"")}\","
                                    + $"\"{userPartitionID.Replace("\"", "\"\"")}\","
                                    + $"\"{name.Replace("\"", "\"\"")}\","
                                    + $"\"{surname.Replace("\"", "\"\"")}\","
                                    + $"\"{displayName.Replace("\"", "\"\"")}\","
                                    + $"\"{mail.Replace("\"", "\"\"")}\","
                                    + $"\"{cn.Replace("\"", "\"\"")}\"");
                            }
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
        }

        /// <summary>
        /// Simple record class to hold extracted group data.
        /// </summary>
        private class GroupRecord
        {
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
