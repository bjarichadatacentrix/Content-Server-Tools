namespace Search_for_Users
{
    partial class ReportForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer? components = null;

        private System.Windows.Forms.DataGridView dataGridViewReport;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Label lblRecordCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUserId;
        private System.Windows.Forms.DataGridViewTextBoxColumn colUserPartitionID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSurname;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDisplayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCn;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAccountLocked;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDomain;
        private System.Windows.Forms.ComboBox cboGenerateOption;
        private System.Windows.Forms.Button btnGenerateInputFile;
        private System.Windows.Forms.Button btnClearSelected;

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
        /// Sets up the report form with a DataGridView to display user data.
        /// </summary>
        private void InitializeComponent()
        {
            dataGridViewReport = new DataGridView();
            btnClose = new Button();
            btnExport = new Button();
            lblRecordCount = new Label();
            cboGenerateOption = new ComboBox();
            btnGenerateInputFile = new Button();
            btnClearSelected = new Button();
            colUserId = new DataGridViewTextBoxColumn();
            colUserPartitionID = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colSurname = new DataGridViewTextBoxColumn();
            colDisplayName = new DataGridViewTextBoxColumn();
            colMail = new DataGridViewTextBoxColumn();
            colCn = new DataGridViewTextBoxColumn();
            colAccountLocked = new DataGridViewTextBoxColumn();
            colDomain = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)dataGridViewReport).BeginInit();
            SuspendLayout();
            // 
            // dataGridViewReport
            // 
            dataGridViewReport.AllowUserToAddRows = false;
            dataGridViewReport.AllowUserToDeleteRows = false;
            dataGridViewReport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewReport.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewReport.BackgroundColor = SystemColors.GradientActiveCaption;
            dataGridViewReport.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewReport.Columns.AddRange(new DataGridViewColumn[] { colUserId, colUserPartitionID, colName, colSurname, colDisplayName, colMail, colCn, colAccountLocked, colDomain });
            dataGridViewReport.Location = new Point(12, 12);
            dataGridViewReport.Name = "dataGridViewReport";
            dataGridViewReport.ReadOnly = true;
            dataGridViewReport.RowHeadersVisible = false;
            dataGridViewReport.Size = new Size(1350, 400);
            dataGridViewReport.TabIndex = 0;
            // 
            // colUserId
            // 
            colUserId.HeaderText = "User ID";
            colUserId.Name = "colUserId";
            colUserId.ReadOnly = true;
            // 
            // colUserPartitionID
            // 
            colUserPartitionID.HeaderText = "User Partition ID";
            colUserPartitionID.Name = "colUserPartitionID";
            colUserPartitionID.ReadOnly = true;
            // 
            // colName
            // 
            colName.HeaderText = "Name";
            colName.Name = "colName";
            colName.ReadOnly = true;
            // 
            // colSurname
            // 
            colSurname.HeaderText = "Surname";
            colSurname.Name = "colSurname";
            colSurname.ReadOnly = true;
            // 
            // colDisplayName
            // 
            colDisplayName.HeaderText = "Display Name";
            colDisplayName.Name = "colDisplayName";
            colDisplayName.ReadOnly = true;
            // 
            // colMail
            // 
            colMail.HeaderText = "Mail";
            colMail.Name = "colMail";
            colMail.ReadOnly = true;
            // 
            // colCn
            // 
            colCn.HeaderText = "CN";
            colCn.Name = "colCn";
            colCn.ReadOnly = true;
            // 
            // colAccountLocked
            // 
            colAccountLocked.HeaderText = "Account Locked";
            colAccountLocked.Name = "colAccountLocked";
            colAccountLocked.ReadOnly = true;
            // 
            // colDomain
            // 
            colDomain.HeaderText = "Domain";
            colDomain.Name = "colDomain";
            colDomain.ReadOnly = true;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(1152, 428);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(100, 30);
            btnClose.TabIndex = 1;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // btnExport
            // 
            btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnExport.Location = new Point(796, 428);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(100, 30);
            btnExport.TabIndex = 2;
            btnExport.Text = "Export to CSV";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // lblRecordCount
            // 
            lblRecordCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblRecordCount.AutoSize = true;
            lblRecordCount.Location = new Point(12, 436);
            lblRecordCount.Name = "lblRecordCount";
            lblRecordCount.Size = new Size(60, 15);
            lblRecordCount.TabIndex = 3;
            lblRecordCount.Text = "Records: 0";
            // 
            // cboGenerateOption
            // 
            cboGenerateOption.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cboGenerateOption.DropDownStyle = ComboBoxStyle.DropDownList;
            cboGenerateOption.FormattingEnabled = true;
            cboGenerateOption.Location = new Point(200, 432);
            cboGenerateOption.Name = "cboGenerateOption";
            cboGenerateOption.Size = new Size(150, 23);
            cboGenerateOption.TabIndex = 4;
            cboGenerateOption.SelectedIndexChanged += cboGenerateOption_SelectedIndexChanged;
            // 
            // btnGenerateInputFile
            // 
            btnGenerateInputFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnGenerateInputFile.Enabled = false;
            btnGenerateInputFile.Location = new Point(494, 428);
            btnGenerateInputFile.Name = "btnGenerateInputFile";
            btnGenerateInputFile.Size = new Size(130, 30);
            btnGenerateInputFile.TabIndex = 5;
            btnGenerateInputFile.Text = "Generate Input File";
            btnGenerateInputFile.UseVisualStyleBackColor = true;
            btnGenerateInputFile.Click += btnGenerateInputFile_Click;
            // 
            // btnClearSelected
            // 
            btnClearSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnClearSelected.Location = new Point(360, 428);
            btnClearSelected.Name = "btnClearSelected";
            btnClearSelected.Size = new Size(130, 30);
            btnClearSelected.TabIndex = 6;
            btnClearSelected.Text = "Clear Selected";
            btnClearSelected.UseVisualStyleBackColor = true;
            btnClearSelected.Click += btnClearSelected_Click;
            btnClearSelected.Visible = false;
            
            // 
            // ReportForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1350, 480);
            Controls.Add(btnClearSelected);
            Controls.Add(btnGenerateInputFile);
            Controls.Add(cboGenerateOption);
            Controls.Add(lblRecordCount);
            Controls.Add(btnExport);
            Controls.Add(btnClose);
            Controls.Add(dataGridViewReport);
            BackColor = SystemColors.GradientActiveCaption;
            MinimumSize = new Size(1280, 400);
            Name = "ReportForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Report";
            ((System.ComponentModel.ISupportInitialize)dataGridViewReport).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
