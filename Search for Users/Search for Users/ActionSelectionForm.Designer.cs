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
            this.btnBack = new System.Windows.Forms.Button();
            this.panelContainer = new System.Windows.Forms.Panel();
            this.btnNext = new System.Windows.Forms.Button();
            this.radioSearchForUsers = new System.Windows.Forms.RadioButton();
            this.labelHeading = new System.Windows.Forms.Label();
            this.panelContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(12, 12);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 25);
            this.btnBack.TabIndex = 0;
            this.btnBack.Text = "Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // panelContainer
            // 
            this.panelContainer.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panelContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelContainer.Controls.Add(this.btnNext);
            this.panelContainer.Controls.Add(this.radioSearchForUsers);
            this.panelContainer.Controls.Add(this.labelHeading);
            this.panelContainer.Location = new System.Drawing.Point(93, 40);
            this.panelContainer.Name = "panelContainer";
            this.panelContainer.Size = new System.Drawing.Size(640, 320);
            this.panelContainer.TabIndex = 1;
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(520, 265);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(90, 30);
            this.btnNext.TabIndex = 3;
            this.btnNext.Text = "Next";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // radioSearchForUsers
            // 
            this.radioSearchForUsers.AutoSize = true;
            this.radioSearchForUsers.Location = new System.Drawing.Point(60, 110);
            this.radioSearchForUsers.Name = "radioSearchForUsers";
            this.radioSearchForUsers.Size = new System.Drawing.Size(110, 19);
            this.radioSearchForUsers.TabIndex = 2;
            this.radioSearchForUsers.TabStop = true;
            this.radioSearchForUsers.Text = "Search for Users";
            this.radioSearchForUsers.UseVisualStyleBackColor = true;
            // 
            // labelHeading
            // 
            this.labelHeading.AutoSize = true;
            this.labelHeading.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelHeading.Location = new System.Drawing.Point(150, 40);
            this.labelHeading.Name = "labelHeading";
            this.labelHeading.Size = new System.Drawing.Size(292, 20);
            this.labelHeading.TabIndex = 1;
            this.labelHeading.Text = "Please Select the action to be performed";
            // 
            // ActionSelectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 400);
            this.Controls.Add(this.panelContainer);
            this.Controls.Add(this.btnBack);
            this.Name = "ActionSelectionForm";
            this.Text = "Content Server Actions";
            this.panelContainer.ResumeLayout(false);
            this.panelContainer.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}

