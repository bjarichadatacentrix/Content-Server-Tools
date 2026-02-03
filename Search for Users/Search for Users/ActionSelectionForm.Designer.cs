namespace Search_for_Users
{
    partial class ActionSelectionForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer? components = null;

        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Panel panelContainer;
        private System.Windows.Forms.Label labelHeading;
        private System.Windows.Forms.RadioButton radioSearchForUsers;
        private System.Windows.Forms.RadioButton radioCreateUser;
        private System.Windows.Forms.RadioButton radioUpdateUser;
        private System.Windows.Forms.RadioButton radioDeleteUser;
        private System.Windows.Forms.RadioButton radioSearchGroups;
        private System.Windows.Forms.RadioButton radioCreateGroups;
        private System.Windows.Forms.RadioButton radioCreateSubGroups;
        private System.Windows.Forms.RadioButton radioUpdateGroups;
        private System.Windows.Forms.RadioButton radioDeleteGroup;
        private System.Windows.Forms.RadioButton radioAddUserToGroup;
        private System.Windows.Forms.RadioButton radioRemoveUserFromGroup;
        private System.Windows.Forms.RadioButton radioSearchUserById;
        private System.Windows.Forms.Button btnNext;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Sets up the controls that make up the
        /// "Please Select the action to be performed" screen.
        /// </summary>
        private void InitializeComponent()
        {
            btnBack = new Button();
            panelContainer = new Panel();
            groupBox1 = new GroupBox();
            radioDeleteGroup = new RadioButton();
            radioUpdateGroups = new RadioButton();
            radioCreateSubGroups = new RadioButton();
            radioCreateGroups = new RadioButton();
            radioSearchGroups = new RadioButton();
            grpLogs = new GroupBox();
            radioSearchForUsers = new RadioButton();
            radioSearchUserById = new RadioButton();
            radioCreateUser = new RadioButton();
            radioDeleteUser = new RadioButton();
            radioUpdateUser = new RadioButton();
            btnNext = new Button();
            labelHeading = new Label();
            groupBox2 = new GroupBox();
            radioAddUserToGroup = new RadioButton();
            radioRemoveUserFromGroup = new RadioButton();
            panelContainer.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            grpLogs.SuspendLayout();
            SuspendLayout();
            // 
            // btnBack
            // 
            btnBack.Location = new Point(12, 12);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(75, 25);
            btnBack.TabIndex = 0;
            btnBack.Text = "Back";
            btnBack.UseVisualStyleBackColor = true;
            btnBack.Click += btnBack_Click;
            // 
            // panelContainer
            // 
            panelContainer.BackColor = SystemColors.ControlLightLight;
            panelContainer.BorderStyle = BorderStyle.FixedSingle;
            panelContainer.Controls.Add(groupBox2);
            panelContainer.Controls.Add(groupBox1);
            panelContainer.Controls.Add(grpLogs);
            panelContainer.Controls.Add(btnNext);
            panelContainer.Controls.Add(labelHeading);
            panelContainer.Location = new Point(93, 40);
            panelContainer.Name = "panelContainer";
            panelContainer.Size = new Size(640, 320);
            panelContainer.TabIndex = 1;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(radioDeleteGroup);
            groupBox1.Controls.Add(radioUpdateGroups);
            groupBox1.Controls.Add(radioCreateSubGroups);
            groupBox1.Controls.Add(radioCreateGroups);
            groupBox1.Controls.Add(radioSearchGroups);
            groupBox1.Font = new Font("Segoe UI", 10F);
            groupBox1.Location = new Point(202, 84);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(160, 211);
            groupBox1.TabIndex = 16;
            groupBox1.TabStop = false;
            groupBox1.Text = "Groups";
            // 
            // radioDeleteGroup
            // 
            radioDeleteGroup.AutoSize = true;
            radioDeleteGroup.Location = new Point(15, 168);
            radioDeleteGroup.Name = "radioDeleteGroup";
            radioDeleteGroup.Size = new Size(109, 23);
            radioDeleteGroup.TabIndex = 4;
            radioDeleteGroup.TabStop = true;
            radioDeleteGroup.Text = "Delete Group";
            radioDeleteGroup.UseVisualStyleBackColor = true;
            radioDeleteGroup.CheckedChanged += GroupsRadioButton_CheckedChanged;
            // 
            // radioUpdateGroups
            // 
            radioUpdateGroups.AutoSize = true;
            radioUpdateGroups.Location = new Point(15, 134);
            radioUpdateGroups.Name = "radioUpdateGroups";
            radioUpdateGroups.Size = new Size(121, 23);
            radioUpdateGroups.TabIndex = 3;
            radioUpdateGroups.TabStop = true;
            radioUpdateGroups.Text = "Update Groups";
            radioUpdateGroups.UseVisualStyleBackColor = true;
            radioUpdateGroups.CheckedChanged += GroupsRadioButton_CheckedChanged;
            // 
            // radioCreateSubGroups
            // 
            radioCreateSubGroups.AutoSize = true;
            radioCreateSubGroups.Location = new Point(15, 100);
            radioCreateSubGroups.Name = "radioCreateSubGroups";
            radioCreateSubGroups.Size = new Size(139, 23);
            radioCreateSubGroups.TabIndex = 2;
            radioCreateSubGroups.TabStop = true;
            radioCreateSubGroups.Text = "Create SubGroups";
            radioCreateSubGroups.UseVisualStyleBackColor = true;
            radioCreateSubGroups.CheckedChanged += GroupsRadioButton_CheckedChanged;
            // 
            // radioCreateGroups
            // 
            radioCreateGroups.AutoSize = true;
            radioCreateGroups.Location = new Point(15, 66);
            radioCreateGroups.Name = "radioCreateGroups";
            radioCreateGroups.Size = new Size(116, 23);
            radioCreateGroups.TabIndex = 1;
            radioCreateGroups.TabStop = true;
            radioCreateGroups.Text = "Create Groups";
            radioCreateGroups.UseVisualStyleBackColor = true;
            radioCreateGroups.CheckedChanged += GroupsRadioButton_CheckedChanged;
            // 
            // radioSearchGroups
            // 
            radioSearchGroups.AutoSize = true;
            radioSearchGroups.Location = new Point(15, 32);
            radioSearchGroups.Name = "radioSearchGroups";
            radioSearchGroups.Size = new Size(116, 23);
            radioSearchGroups.TabIndex = 0;
            radioSearchGroups.TabStop = true;
            radioSearchGroups.Text = "Search Groups";
            radioSearchGroups.UseVisualStyleBackColor = true;
            radioSearchGroups.CheckedChanged += GroupsRadioButton_CheckedChanged;
            // 
            // grpLogs
            // 
            grpLogs.Controls.Add(radioSearchForUsers);
            grpLogs.Controls.Add(radioSearchUserById);
            grpLogs.Controls.Add(radioCreateUser);
            grpLogs.Controls.Add(radioDeleteUser);
            grpLogs.Controls.Add(radioUpdateUser);
            grpLogs.Font = new Font("Segoe UI", 10F);
            grpLogs.Location = new Point(21, 84);
            grpLogs.Name = "grpLogs";
            grpLogs.Size = new Size(165, 245);
            grpLogs.TabIndex = 15;
            grpLogs.TabStop = false;
            grpLogs.Text = "Users";
            // 
            // radioSearchForUsers
            // 
            radioSearchForUsers.AutoSize = true;
            radioSearchForUsers.Location = new Point(15, 32);
            radioSearchForUsers.Name = "radioSearchForUsers";
            radioSearchForUsers.Size = new Size(126, 23);
            radioSearchForUsers.TabIndex = 0;
            radioSearchForUsers.TabStop = true;
            radioSearchForUsers.Text = "Search for Users";
            radioSearchForUsers.UseVisualStyleBackColor = true;
            radioSearchForUsers.CheckedChanged += UsersRadioButton_CheckedChanged;
            // 
            // radioSearchUserById
            // 
            radioSearchUserById.AutoSize = true;
            radioSearchUserById.Location = new Point(15, 66);
            radioSearchUserById.Name = "radioSearchUserById";
            radioSearchUserById.Size = new Size(138, 23);
            radioSearchUserById.TabIndex = 1;
            radioSearchUserById.TabStop = true;
            radioSearchUserById.Text = "Search User by ID";
            radioSearchUserById.UseVisualStyleBackColor = true;
            radioSearchUserById.CheckedChanged += SearchUserByIdRadioButton_CheckedChanged;
            // 
            // radioCreateUser
            // 
            radioCreateUser.AutoSize = true;
            radioCreateUser.Location = new Point(15, 100);
            radioCreateUser.Name = "radioCreateUser";
            radioCreateUser.Size = new Size(99, 23);
            radioCreateUser.TabIndex = 2;
            radioCreateUser.TabStop = true;
            radioCreateUser.Text = "Create User";
            radioCreateUser.UseVisualStyleBackColor = true;
            radioCreateUser.CheckedChanged += UsersRadioButton_CheckedChanged;
            // 
            // radioDeleteUser
            // 
            radioDeleteUser.AutoSize = true;
            radioDeleteUser.Location = new Point(15, 168);
            radioDeleteUser.Name = "radioDeleteUser";
            radioDeleteUser.Size = new Size(98, 23);
            radioDeleteUser.TabIndex = 4;
            radioDeleteUser.TabStop = true;
            radioDeleteUser.Text = "Delete User";
            radioDeleteUser.UseVisualStyleBackColor = true;
            radioDeleteUser.CheckedChanged += UsersRadioButton_CheckedChanged;
            // 
            // radioUpdateUser
            // 
            radioUpdateUser.AutoSize = true;
            radioUpdateUser.Location = new Point(15, 134);
            radioUpdateUser.Name = "radioUpdateUser";
            radioUpdateUser.Size = new Size(104, 23);
            radioUpdateUser.TabIndex = 3;
            radioUpdateUser.TabStop = true;
            radioUpdateUser.Text = "Update User";
            radioUpdateUser.UseVisualStyleBackColor = true;
            radioUpdateUser.CheckedChanged += UsersRadioButton_CheckedChanged;
            // 
            // btnNext
            // 
            btnNext.Location = new Point(520, 265);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(90, 30);
            btnNext.TabIndex = 3;
            btnNext.Text = "Next";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            // 
            // labelHeading
            // 
            labelHeading.AutoSize = true;
            labelHeading.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            labelHeading.Location = new Point(150, 40);
            labelHeading.Name = "labelHeading";
            labelHeading.Size = new Size(291, 20);
            labelHeading.TabIndex = 1;
            labelHeading.Text = "Please Select the action to be performed";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(radioRemoveUserFromGroup);
            groupBox2.Controls.Add(radioAddUserToGroup);
            groupBox2.Font = new Font("Segoe UI", 10F);
            groupBox2.Location = new Point(410, 84);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(200, 145);
            groupBox2.TabIndex = 17;
            groupBox2.TabStop = false;
            groupBox2.Text = "Users Groups";
            // 
            // radioAddUserToGroup
            // 
            radioAddUserToGroup.AutoSize = true;
            radioAddUserToGroup.Location = new Point(15, 32);
            radioAddUserToGroup.Name = "radioAddUserToGroup";
            radioAddUserToGroup.Size = new Size(138, 23);
            radioAddUserToGroup.TabIndex = 0;
            radioAddUserToGroup.TabStop = true;
            radioAddUserToGroup.Text = "Add User to Group";
            radioAddUserToGroup.UseVisualStyleBackColor = true;
            radioAddUserToGroup.CheckedChanged += UsersGroupsRadioButton_CheckedChanged;
            // 
            // radioRemoveUserFromGroup
            // 
            radioRemoveUserFromGroup.AutoSize = true;
            radioRemoveUserFromGroup.Location = new Point(15, 66);
            radioRemoveUserFromGroup.Name = "radioRemoveUserFromGroup";
            radioRemoveUserFromGroup.Size = new Size(175, 23);
            radioRemoveUserFromGroup.TabIndex = 1;
            radioRemoveUserFromGroup.TabStop = true;
            radioRemoveUserFromGroup.Text = "Remove Users from Group";
            radioRemoveUserFromGroup.UseVisualStyleBackColor = true;
            radioRemoveUserFromGroup.CheckedChanged += UsersGroupsRadioButton_CheckedChanged;
            // 
            // ActionSelectionForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 400);
            Controls.Add(panelContainer);
            Controls.Add(btnBack);
            Name = "ActionSelectionForm";
            Text = "Content Server Actions";
            panelContainer.ResumeLayout(false);
            panelContainer.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            grpLogs.ResumeLayout(false);
            grpLogs.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpLogs;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
    }
}

