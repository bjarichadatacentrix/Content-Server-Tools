namespace Search_for_Users
{
    partial class SearchForUsersForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer? components = null;

        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label lblSectionTitle;
        private System.Windows.Forms.Label lblLogLocation;
        private System.Windows.Forms.TextBox txtLogLocation;
        private System.Windows.Forms.Button btnBrowseLogLocation;
        private System.Windows.Forms.Label lblInputFile;
        private System.Windows.Forms.TextBox txtInputFile;
        private System.Windows.Forms.Button btnChooseInputFile;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblCurrentFile;
        private System.Windows.Forms.TextBox txtCurrentFile;
        private System.Windows.Forms.Label lblStartRow;
        private System.Windows.Forms.TextBox txtStartRow;
        private System.Windows.Forms.GroupBox grpLogs;
        private System.Windows.Forms.RadioButton radioInfoLog;
        private System.Windows.Forms.RadioButton radioErrorLog;
        private System.Windows.Forms.Button btnOpenLogLocation;
        private System.Windows.Forms.Label lblLogStatus;
        private System.Windows.Forms.Label lblTotalFiles;
        private System.Windows.Forms.Label lblProcessedFiles;
        private System.Windows.Forms.Label lblErrorFiles;
        private System.Windows.Forms.Label lblTotalFilesValue;
        private System.Windows.Forms.Label lblProcessedFilesValue;
        private System.Windows.Forms.Label lblErrorFilesValue;
        private System.Windows.Forms.Label lblRowsProcessed;
        private System.Windows.Forms.Label lblRowsProcessedValue;

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
        /// Sets up the "Upload Document and Metadata" style screen that
        /// controls how the CSV-based user search runs.
        /// </summary>
        private void InitializeComponent()
        {
            btnBack = new Button();
            panelMain = new Panel();
            lblErrorFilesValue = new Label();
            lblProcessedFilesValue = new Label();
            lblTotalFilesValue = new Label();
            lblErrorFiles = new Label();
            lblProcessedFiles = new Label();
            lblRowsProcessedValue = new Label();
            lblRowsProcessed = new Label();
            lblTotalFiles = new Label();
            lblLogStatus = new Label();
            grpLogs = new GroupBox();
            btnOpenLogLocation = new Button();
            radioErrorLog = new RadioButton();
            radioInfoLog = new RadioButton();
            lblStartRow = new Label();
            txtStartRow = new TextBox();
            lblCurrentFile = new Label();
            txtCurrentFile = new TextBox();
            btnStop = new Button();
            btnReset = new Button();
            btnStart = new Button();
            btnChooseInputFile = new Button();
            txtInputFile = new TextBox();
            lblInputFile = new Label();
            btnBrowseLogLocation = new Button();
            txtLogLocation = new TextBox();
            lblLogLocation = new Label();
            lblSectionTitle = new Label();
            panelMain.SuspendLayout();
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
            // panelMain
            // 
            panelMain.BackColor = SystemColors.ControlLightLight;
            panelMain.BorderStyle = BorderStyle.FixedSingle;
            panelMain.Controls.Add(lblErrorFilesValue);
            panelMain.Controls.Add(lblProcessedFilesValue);
            panelMain.Controls.Add(lblTotalFilesValue);
            panelMain.Controls.Add(lblErrorFiles);
            panelMain.Controls.Add(lblProcessedFiles);
            panelMain.Controls.Add(lblRowsProcessedValue);
            panelMain.Controls.Add(lblRowsProcessed);
            panelMain.Controls.Add(lblTotalFiles);
            panelMain.Controls.Add(lblLogStatus);
            panelMain.Controls.Add(grpLogs);
            panelMain.Controls.Add(lblStartRow);
            panelMain.Controls.Add(txtStartRow);
            panelMain.Controls.Add(lblCurrentFile);
            panelMain.Controls.Add(txtCurrentFile);
            panelMain.Controls.Add(btnStop);
            panelMain.Controls.Add(btnReset);
            panelMain.Controls.Add(btnStart);
            panelMain.Controls.Add(btnChooseInputFile);
            panelMain.Controls.Add(txtInputFile);
            panelMain.Controls.Add(lblInputFile);
            panelMain.Controls.Add(btnBrowseLogLocation);
            panelMain.Controls.Add(txtLogLocation);
            panelMain.Controls.Add(lblLogLocation);
            panelMain.Controls.Add(lblSectionTitle);
            panelMain.Location = new Point(93, 40);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(780, 380);
            panelMain.TabIndex = 1;
            // 
            // lblErrorFilesValue
            // 
            lblErrorFilesValue.AutoSize = true;
            lblErrorFilesValue.Location = new Point(700, 235);
            lblErrorFilesValue.Name = "lblErrorFilesValue";
            lblErrorFilesValue.Size = new Size(13, 15);
            lblErrorFilesValue.TabIndex = 21;
            lblErrorFilesValue.Text = "0";
            // 
            // lblProcessedFilesValue
            // 
            lblProcessedFilesValue.AutoSize = true;
            lblProcessedFilesValue.Location = new Point(700, 210);
            lblProcessedFilesValue.Name = "lblProcessedFilesValue";
            lblProcessedFilesValue.Size = new Size(13, 15);
            lblProcessedFilesValue.TabIndex = 20;
            lblProcessedFilesValue.Text = "0";
            // 
            // lblTotalFilesValue
            // 
            lblTotalFilesValue.AutoSize = true;
            lblTotalFilesValue.Location = new Point(700, 188);
            lblTotalFilesValue.Name = "lblTotalFilesValue";
            lblTotalFilesValue.Size = new Size(13, 15);
            lblTotalFilesValue.TabIndex = 19;
            lblTotalFilesValue.Text = "0";
            // 
            // lblErrorFiles
            // 
            lblErrorFiles.AutoSize = true;
            lblErrorFiles.Location = new Point(600, 235);
            lblErrorFiles.Name = "lblErrorFiles";
            lblErrorFiles.Size = new Size(61, 15);
            lblErrorFiles.TabIndex = 18;
            lblErrorFiles.Text = "Error Files:";
            // 
            // lblProcessedFiles
            // 
            lblProcessedFiles.AutoSize = true;
            lblProcessedFiles.Location = new Point(600, 210);
            lblProcessedFiles.Name = "lblProcessedFiles";
            lblProcessedFiles.Size = new Size(89, 15);
            lblProcessedFiles.TabIndex = 17;
            lblProcessedFiles.Text = "Processed Files:";
            // 
            // lblRowsProcessedValue
            // 
            lblRowsProcessedValue.AutoSize = true;
            lblRowsProcessedValue.Location = new Point(700, 255);
            lblRowsProcessedValue.Name = "lblRowsProcessedValue";
            lblRowsProcessedValue.Size = new Size(13, 15);
            lblRowsProcessedValue.TabIndex = 22;
            lblRowsProcessedValue.Text = "0";
            // 
            // lblRowsProcessed
            // 
            lblRowsProcessed.AutoSize = true;
            lblRowsProcessed.Location = new Point(600, 255);
            lblRowsProcessed.Name = "lblRowsProcessed";
            lblRowsProcessed.Size = new Size(94, 15);
            lblRowsProcessed.TabIndex = 21;
            lblRowsProcessed.Text = "Rows Processed:";
            // 
            // lblTotalFiles
            // 
            lblTotalFiles.AutoSize = true;
            lblTotalFiles.Location = new Point(600, 188);
            lblTotalFiles.Name = "lblTotalFiles";
            lblTotalFiles.Size = new Size(61, 15);
            lblTotalFiles.TabIndex = 16;
            lblTotalFiles.Text = "Total Files:";
            // 
            // lblLogStatus
            // 
            lblLogStatus.AutoSize = true;
            lblLogStatus.Location = new Point(600, 115);
            lblLogStatus.Name = "lblLogStatus";
            lblLogStatus.Size = new Size(67, 15);
            lblLogStatus.TabIndex = 15;
            lblLogStatus.Text = "Not Started";
            // 
            // grpLogs
            // 
            grpLogs.Controls.Add(btnOpenLogLocation);
            grpLogs.Controls.Add(radioErrorLog);
            grpLogs.Controls.Add(radioInfoLog);
            grpLogs.Location = new Point(430, 80);
            grpLogs.Name = "grpLogs";
            grpLogs.Size = new Size(143, 211);
            grpLogs.TabIndex = 14;
            grpLogs.TabStop = false;
            grpLogs.Text = "Logs";
            // 
            // btnOpenLogLocation
            // 
            btnOpenLogLocation.Location = new Point(6, 170);
            btnOpenLogLocation.Name = "btnOpenLogLocation";
            btnOpenLogLocation.Size = new Size(124, 30);
            btnOpenLogLocation.TabIndex = 2;
            btnOpenLogLocation.Text = "Open Log Location";
            btnOpenLogLocation.UseVisualStyleBackColor = true;
            btnOpenLogLocation.Click += btnOpenLogLocation_Click;
            // 
            // radioErrorLog
            // 
            radioErrorLog.AutoSize = true;
            radioErrorLog.Location = new Point(20, 65);
            radioErrorLog.Name = "radioErrorLog";
            radioErrorLog.Size = new Size(70, 19);
            radioErrorLog.TabIndex = 1;
            radioErrorLog.TabStop = true;
            radioErrorLog.Text = "Error log";
            radioErrorLog.UseVisualStyleBackColor = true;
            // 
            // radioInfoLog
            // 
            radioInfoLog.AutoSize = true;
            radioInfoLog.Location = new Point(20, 35);
            radioInfoLog.Name = "radioInfoLog";
            radioInfoLog.Size = new Size(66, 19);
            radioInfoLog.TabIndex = 0;
            radioInfoLog.TabStop = true;
            radioInfoLog.Text = "Info log";
            radioInfoLog.UseVisualStyleBackColor = true;
            // 
            // lblStartRow
            // 
            lblStartRow.AutoSize = true;
            lblStartRow.Location = new Point(600, 40);
            lblStartRow.Name = "lblStartRow";
            lblStartRow.Size = new Size(57, 15);
            lblStartRow.TabIndex = 13;
            lblStartRow.Text = "Start Row";
            // 
            // txtStartRow
            // 
            txtStartRow.Location = new Point(670, 37);
            txtStartRow.Name = "txtStartRow";
            txtStartRow.Size = new Size(80, 23);
            txtStartRow.TabIndex = 12;
            // 
            // lblCurrentFile
            // 
            lblCurrentFile.AutoSize = true;
            lblCurrentFile.Location = new Point(20, 300);
            lblCurrentFile.Name = "lblCurrentFile";
            lblCurrentFile.Size = new Size(71, 15);
            lblCurrentFile.TabIndex = 11;
            lblCurrentFile.Text = "Current File:";
            // 
            // txtCurrentFile
            // 
            txtCurrentFile.Location = new Point(110, 297);
            txtCurrentFile.Name = "txtCurrentFile";
            txtCurrentFile.ReadOnly = true;
            txtCurrentFile.Size = new Size(640, 23);
            txtCurrentFile.TabIndex = 10;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(280, 245);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(100, 35);
            btnStop.TabIndex = 9;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(160, 245);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(100, 35);
            btnReset.TabIndex = 8;
            btnReset.Text = "Reset";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(40, 245);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(100, 35);
            btnStart.TabIndex = 7;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnChooseInputFile
            // 
            btnChooseInputFile.Location = new Point(40, 150);
            btnChooseInputFile.Name = "btnChooseInputFile";
            btnChooseInputFile.Size = new Size(140, 30);
            btnChooseInputFile.TabIndex = 6;
            btnChooseInputFile.Text = "Choose Input File";
            btnChooseInputFile.UseVisualStyleBackColor = true;
            btnChooseInputFile.Click += btnChooseInputFile_Click;
            // 
            // txtInputFile
            // 
            txtInputFile.Location = new Point(190, 154);
            txtInputFile.Name = "txtInputFile";
            txtInputFile.ReadOnly = true;
            txtInputFile.Size = new Size(220, 23);
            txtInputFile.TabIndex = 5;
            // 
            // lblInputFile
            // 
            lblInputFile.AutoSize = true;
            lblInputFile.Location = new Point(40, 130);
            lblInputFile.Name = "lblInputFile";
            lblInputFile.Size = new Size(97, 15);
            lblInputFile.TabIndex = 4;
            lblInputFile.Text = "Choose Input file";
            // 
            // btnBrowseLogLocation
            // 
            btnBrowseLogLocation.Location = new Point(40, 90);
            btnBrowseLogLocation.Name = "btnBrowseLogLocation";
            btnBrowseLogLocation.Size = new Size(140, 30);
            btnBrowseLogLocation.TabIndex = 3;
            btnBrowseLogLocation.Text = "Log Location";
            btnBrowseLogLocation.UseVisualStyleBackColor = true;
            btnBrowseLogLocation.Click += btnBrowseLogLocation_Click;
            // 
            // txtLogLocation
            // 
            txtLogLocation.Location = new Point(190, 94);
            txtLogLocation.Name = "txtLogLocation";
            txtLogLocation.ReadOnly = true;
            txtLogLocation.Size = new Size(220, 23);
            txtLogLocation.TabIndex = 2;
            // 
            // lblLogLocation
            // 
            lblLogLocation.AutoSize = true;
            lblLogLocation.Location = new Point(40, 70);
            lblLogLocation.Name = "lblLogLocation";
            lblLogLocation.Size = new Size(76, 15);
            lblLogLocation.TabIndex = 1;
            lblLogLocation.Text = "Log Location";
            // 
            // lblSectionTitle
            // 
            lblSectionTitle.AutoSize = true;
            lblSectionTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSectionTitle.Location = new Point(40, 20);
            lblSectionTitle.Name = "lblSectionTitle";
            lblSectionTitle.Size = new Size(187, 15);
            lblSectionTitle.TabIndex = 0;
            lblSectionTitle.Text = "Upload Document and Metadata";
            // 
            // SearchForUsersForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 450);
            Controls.Add(panelMain);
            Controls.Add(btnBack);
            Name = "SearchForUsersForm";
            Text = "Search for Users";
            panelMain.ResumeLayout(false);
            panelMain.PerformLayout();
            grpLogs.ResumeLayout(false);
            grpLogs.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}

