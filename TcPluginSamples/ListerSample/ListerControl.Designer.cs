namespace OY.TotalCommander.TcPlugins.ListerSample
{
    partial class ListerControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabFile = new System.Windows.Forms.TabPage();
			this.txtFile = new System.Windows.Forms.TextBox();
			this.tabLog = new System.Windows.Forms.TabPage();
			this.txtLog = new System.Windows.Forms.TextBox();
			this.tabAbout = new System.Windows.Forms.TabPage();
			this.lblAuthor = new System.Windows.Forms.Label();
			this.lblAbout = new System.Windows.Forms.Label();
			this.tabControl.SuspendLayout();
			this.tabFile.SuspendLayout();
			this.tabLog.SuspendLayout();
			this.tabAbout.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add(this.tabFile);
			this.tabControl.Controls.Add(this.tabLog);
			this.tabControl.Controls.Add(this.tabAbout);
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(447, 522);
			this.tabControl.TabIndex = 0;
			// 
			// tabFile
			// 
			this.tabFile.Controls.Add(this.txtFile);
			this.tabFile.Location = new System.Drawing.Point(4, 22);
			this.tabFile.Name = "tabFile";
			this.tabFile.Size = new System.Drawing.Size(439, 496);
			this.tabFile.TabIndex = 0;
			this.tabFile.Text = "File";
			this.tabFile.UseVisualStyleBackColor = true;
			// 
			// txtFile
			// 
			this.txtFile.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtFile.HideSelection = false;
			this.txtFile.Location = new System.Drawing.Point(0, 0);
			this.txtFile.Margin = new System.Windows.Forms.Padding(0);
			this.txtFile.Multiline = true;
			this.txtFile.Name = "txtFile";
			this.txtFile.ReadOnly = true;
			this.txtFile.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtFile.ShortcutsEnabled = false;
			this.txtFile.Size = new System.Drawing.Size(439, 496);
			this.txtFile.TabIndex = 0;
			this.txtFile.WordWrap = false;
			// 
			// tabLog
			// 
			this.tabLog.Controls.Add(this.txtLog);
			this.tabLog.Location = new System.Drawing.Point(4, 22);
			this.tabLog.Name = "tabLog";
			this.tabLog.Size = new System.Drawing.Size(439, 496);
			this.tabLog.TabIndex = 1;
			this.tabLog.Text = "Log";
			this.tabLog.UseVisualStyleBackColor = true;
			// 
			// txtLog
			// 
			this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtLog.HideSelection = false;
			this.txtLog.Location = new System.Drawing.Point(0, 0);
			this.txtLog.Margin = new System.Windows.Forms.Padding(0);
			this.txtLog.Multiline = true;
			this.txtLog.Name = "txtLog";
			this.txtLog.ReadOnly = true;
			this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtLog.ShortcutsEnabled = false;
			this.txtLog.Size = new System.Drawing.Size(439, 496);
			this.txtLog.TabIndex = 1;
			this.txtLog.WordWrap = false;
			// 
			// tabAbout
			// 
			this.tabAbout.Controls.Add(this.lblAuthor);
			this.tabAbout.Controls.Add(this.lblAbout);
			this.tabAbout.Location = new System.Drawing.Point(4, 22);
			this.tabAbout.Name = "tabAbout";
			this.tabAbout.Padding = new System.Windows.Forms.Padding(7);
			this.tabAbout.Size = new System.Drawing.Size(439, 496);
			this.tabAbout.TabIndex = 2;
			this.tabAbout.Text = "About";
			this.tabAbout.UseVisualStyleBackColor = true;
			// 
			// lblAuthor
			// 
			this.lblAuthor.BackColor = System.Drawing.SystemColors.Control;
			this.lblAuthor.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblAuthor.Location = new System.Drawing.Point(7, 89);
			this.lblAuthor.Name = "lblAuthor";
			this.lblAuthor.Size = new System.Drawing.Size(425, 27);
			this.lblAuthor.TabIndex = 1;
			this.lblAuthor.Text = "Copyright © 2015-16 Oleg Yuvashev";
			// 
			// lblAbout
			// 
			this.lblAbout.Dock = System.Windows.Forms.DockStyle.Top;
			this.lblAbout.Location = new System.Drawing.Point(7, 7);
			this.lblAbout.Margin = new System.Windows.Forms.Padding(5);
			this.lblAbout.Name = "lblAbout";
			this.lblAbout.Size = new System.Drawing.Size(425, 82);
			this.lblAbout.TabIndex = 0;
			this.lblAbout.Text = "Sample Lister plugin -  simple viewer with trace log.\r\nVersion 1.4";
			// 
			// ListerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
			this.Controls.Add(this.tabControl);
			this.Name = "ListerControl";
			this.Size = new System.Drawing.Size(447, 522);
			this.tabControl.ResumeLayout(false);
			this.tabFile.ResumeLayout(false);
			this.tabFile.PerformLayout();
			this.tabLog.ResumeLayout(false);
			this.tabLog.PerformLayout();
			this.tabAbout.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabPage tabFile;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TabPage tabAbout;
        private System.Windows.Forms.TextBox txtFile;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblAuthor;
        private System.Windows.Forms.Label lblAbout;
        internal System.Windows.Forms.TabControl tabControl;
    }
}
