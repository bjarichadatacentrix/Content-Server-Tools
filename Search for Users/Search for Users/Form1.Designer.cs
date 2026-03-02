namespace Search_for_Users
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label labelUsername;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.Label labelUrl;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label labelMessage;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            labelUsername = new Label();
            labelPassword = new Label();
            labelUrl = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            txtUrl = new TextBox();
            btnLogin = new Button();
            labelMessage = new Label();
            groupBox1 = new GroupBox();
            pictureBox1 = new PictureBox();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // labelUsername
            // 
            labelUsername.AutoSize = true;
            labelUsername.Location = new Point(22, 36);
            labelUsername.Name = "labelUsername";
            labelUsername.Size = new Size(60, 15);
            labelUsername.TabIndex = 0;
            labelUsername.Text = "Username";
            // 
            // labelPassword
            // 
            labelPassword.AutoSize = true;
            labelPassword.Location = new Point(22, 76);
            labelPassword.Name = "labelPassword";
            labelPassword.Size = new Size(57, 15);
            labelPassword.TabIndex = 2;
            labelPassword.Text = "Password";
            // 
            // labelUrl
            // 
            labelUrl.AutoSize = true;
            labelUrl.Location = new Point(22, 116);
            labelUrl.Name = "labelUrl";
            labelUrl.Size = new Size(28, 15);
            labelUrl.TabIndex = 4;
            labelUrl.Text = "URL";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(112, 33);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(250, 23);
            txtUsername.TabIndex = 1;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(112, 73);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(250, 23);
            txtPassword.TabIndex = 3;
            txtPassword.UseSystemPasswordChar = true;
            // 
            // txtUrl
            // 
            txtUrl.Location = new Point(112, 113);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(250, 23);
            txtUrl.TabIndex = 5;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(263, 152);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(100, 30);
            btnLogin.TabIndex = 7;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // labelMessage
            // 
            labelMessage.AutoSize = true;
            labelMessage.ForeColor = Color.Red;
            labelMessage.Location = new Point(130, 150);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(0, 15);
            labelMessage.TabIndex = 6;
            // 
            // groupBox1
            // 
            groupBox1.BackColor = SystemColors.GradientActiveCaption;
            groupBox1.Controls.Add(labelUsername);
            groupBox1.Controls.Add(btnLogin);
            groupBox1.Controls.Add(txtUsername);
            groupBox1.Controls.Add(labelPassword);
            groupBox1.Controls.Add(txtUrl);
            groupBox1.Controls.Add(txtPassword);
            groupBox1.Controls.Add(labelUrl);
            groupBox1.Location = new Point(77, 111);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(368, 187);
            groupBox1.TabIndex = 8;
            groupBox1.TabStop = false;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(349, 304);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(174, 50);
            pictureBox1.TabIndex = 9;
            pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(524, 351);
            Controls.Add(pictureBox1);
            Controls.Add(groupBox1);
            Controls.Add(labelMessage);
            Name = "Form1";
            Text = "Content Server Login";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private PictureBox pictureBox1;
    }
}
