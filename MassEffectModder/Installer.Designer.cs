/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

 namespace MassEffectModder
{
    partial class Installer
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Installer));
            this.buttonExit = new System.Windows.Forms.Button();
            this.buttonNormal = new System.Windows.Forms.Button();
            this.labelFinalStatus = new System.Windows.Forms.Label();
            this.checkBoxPreEnableRepack = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionSkipScan = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionLimit2K = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionVanilla = new System.Windows.Forms.CheckBox();
            this.buttonSTART = new System.Windows.Forms.Button();
            this.labelCurrentStatus = new System.Windows.Forms.Label();
            this.pictureBoxBG = new System.Windows.Forms.PictureBox();
            this.labelOptions = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBG)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonExit
            // 
            this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonExit.BackColor = System.Drawing.Color.DarkRed;
            this.buttonExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonExit.ForeColor = System.Drawing.Color.White;
            this.buttonExit.Location = new System.Drawing.Point(16, 402);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(75, 23);
            this.buttonExit.TabIndex = 3;
            this.buttonExit.Text = "EXIT";
            this.buttonExit.UseVisualStyleBackColor = false;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // buttonNormal
            // 
            this.buttonNormal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNormal.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.buttonNormal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNormal.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonNormal.ForeColor = System.Drawing.Color.White;
            this.buttonNormal.Location = new System.Drawing.Point(661, 402);
            this.buttonNormal.Name = "buttonNormal";
            this.buttonNormal.Size = new System.Drawing.Size(111, 23);
            this.buttonNormal.TabIndex = 4;
            this.buttonNormal.Text = "Standard Mode";
            this.buttonNormal.UseVisualStyleBackColor = false;
            this.buttonNormal.Click += new System.EventHandler(this.buttonNormal_Click);
            // 
            // labelFinalStatus
            // 
            this.labelFinalStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinalStatus.BackColor = System.Drawing.Color.Transparent;
            this.labelFinalStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelFinalStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelFinalStatus.ForeColor = System.Drawing.Color.White;
            this.labelFinalStatus.Location = new System.Drawing.Point(109, 145);
            this.labelFinalStatus.Name = "labelFinalStatus";
            this.labelFinalStatus.Size = new System.Drawing.Size(572, 47);
            this.labelFinalStatus.TabIndex = 25;
            // 
            // checkBoxPreEnableRepack
            // 
            this.checkBoxPreEnableRepack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxPreEnableRepack.AutoSize = true;
            this.checkBoxPreEnableRepack.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxPreEnableRepack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxPreEnableRepack.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxPreEnableRepack.ForeColor = System.Drawing.Color.White;
            this.checkBoxPreEnableRepack.Location = new System.Drawing.Point(16, 303);
            this.checkBoxPreEnableRepack.Name = "checkBoxPreEnableRepack";
            this.checkBoxPreEnableRepack.Size = new System.Drawing.Size(369, 24);
            this.checkBoxPreEnableRepack.TabIndex = 35;
            this.checkBoxPreEnableRepack.TabStop = false;
            this.checkBoxPreEnableRepack.Text = "Repack game files with better compression";
            this.checkBoxPreEnableRepack.UseVisualStyleBackColor = false;
            // 
            // checkBoxOptionSkipScan
            // 
            this.checkBoxOptionSkipScan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionSkipScan.AutoSize = true;
            this.checkBoxOptionSkipScan.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionSkipScan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxOptionSkipScan.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionSkipScan.ForeColor = System.Drawing.Color.White;
            this.checkBoxOptionSkipScan.Location = new System.Drawing.Point(16, 367);
            this.checkBoxOptionSkipScan.Name = "checkBoxOptionSkipScan";
            this.checkBoxOptionSkipScan.Size = new System.Drawing.Size(208, 24);
            this.checkBoxOptionSkipScan.TabIndex = 52;
            this.checkBoxOptionSkipScan.TabStop = false;
            this.checkBoxOptionSkipScan.Text = "Skip scan/remove mips";
            this.checkBoxOptionSkipScan.UseVisualStyleBackColor = false;
            this.checkBoxOptionSkipScan.CheckedChanged += new System.EventHandler(this.checkBoxOptionSkipScan_CheckedChanged);
            // 
            // checkBoxOptionLimit2K
            // 
            this.checkBoxOptionLimit2K.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionLimit2K.AutoSize = true;
            this.checkBoxOptionLimit2K.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionLimit2K.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxOptionLimit2K.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionLimit2K.ForeColor = System.Drawing.Color.White;
            this.checkBoxOptionLimit2K.Location = new System.Drawing.Point(16, 325);
            this.checkBoxOptionLimit2K.Name = "checkBoxOptionLimit2K";
            this.checkBoxOptionLimit2K.Size = new System.Drawing.Size(180, 24);
            this.checkBoxOptionLimit2K.TabIndex = 39;
            this.checkBoxOptionLimit2K.TabStop = false;
            this.checkBoxOptionLimit2K.Text = "Limit textures to 2K";
            this.checkBoxOptionLimit2K.UseVisualStyleBackColor = false;
            this.checkBoxOptionLimit2K.Visible = false;
            // 
            // checkBoxOptionVanilla
            // 
            this.checkBoxOptionVanilla.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionVanilla.AutoSize = true;
            this.checkBoxOptionVanilla.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionVanilla.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxOptionVanilla.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionVanilla.ForeColor = System.Drawing.Color.White;
            this.checkBoxOptionVanilla.Location = new System.Drawing.Point(16, 346);
            this.checkBoxOptionVanilla.Name = "checkBoxOptionVanilla";
            this.checkBoxOptionVanilla.Size = new System.Drawing.Size(167, 24);
            this.checkBoxOptionVanilla.TabIndex = 38;
            this.checkBoxOptionVanilla.TabStop = false;
            this.checkBoxOptionVanilla.Text = "Skip vanilla check";
            this.checkBoxOptionVanilla.UseVisualStyleBackColor = false;
            this.checkBoxOptionVanilla.CheckedChanged += new System.EventHandler(this.checkBoxOptionVanilla_CheckedChanged);
            // 
            // buttonSTART
            // 
            this.buttonSTART.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonSTART.BackColor = System.Drawing.Color.LimeGreen;
            this.buttonSTART.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSTART.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonSTART.ForeColor = System.Drawing.Color.White;
            this.buttonSTART.Location = new System.Drawing.Point(353, 402);
            this.buttonSTART.Name = "buttonSTART";
            this.buttonSTART.Size = new System.Drawing.Size(75, 23);
            this.buttonSTART.TabIndex = 2;
            this.buttonSTART.Text = "START";
            this.buttonSTART.UseVisualStyleBackColor = false;
            this.buttonSTART.Click += new System.EventHandler(this.buttonSTART_Click);
            // 
            // labelCurrentStatus
            // 
            this.labelCurrentStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrentStatus.BackColor = System.Drawing.Color.Transparent;
            this.labelCurrentStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelCurrentStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelCurrentStatus.ForeColor = System.Drawing.Color.White;
            this.labelCurrentStatus.Location = new System.Drawing.Point(109, 211);
            this.labelCurrentStatus.Name = "labelCurrentStatus";
            this.labelCurrentStatus.Size = new System.Drawing.Size(572, 45);
            this.labelCurrentStatus.TabIndex = 28;
            // 
            // pictureBoxBG
            // 
            this.pictureBoxBG.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxBG.InitialImage = null;
            this.pictureBoxBG.Location = new System.Drawing.Point(-1, 0);
            this.pictureBoxBG.MaximumSize = new System.Drawing.Size(1920, 1080);
            this.pictureBoxBG.Name = "pictureBoxBG";
            this.pictureBoxBG.Size = new System.Drawing.Size(785, 440);
            this.pictureBoxBG.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxBG.TabIndex = 29;
            this.pictureBoxBG.TabStop = false;
            // 
            // labelOptions
            // 
            this.labelOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptions.AutoSize = true;
            this.labelOptions.BackColor = System.Drawing.Color.Transparent;
            this.labelOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptions.ForeColor = System.Drawing.Color.White;
            this.labelOptions.Location = new System.Drawing.Point(12, 280);
            this.labelOptions.Name = "labelOptions";
            this.labelOptions.Size = new System.Drawing.Size(76, 20);
            this.labelOptions.TabIndex = 54;
            this.labelOptions.Text = "Options:";
            // 
            // Installer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 438);
            this.Controls.Add(this.labelOptions);
            this.Controls.Add(this.checkBoxOptionSkipScan);
            this.Controls.Add(this.labelCurrentStatus);
            this.Controls.Add(this.checkBoxOptionLimit2K);
            this.Controls.Add(this.buttonSTART);
            this.Controls.Add(this.checkBoxOptionVanilla);
            this.Controls.Add(this.checkBoxPreEnableRepack);
            this.Controls.Add(this.buttonNormal);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.labelFinalStatus);
            this.Controls.Add(this.pictureBoxBG);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1920, 1080);
            this.MinimumSize = new System.Drawing.Size(800, 477);
            this.Name = "Installer";
            this.Text = "MEM Installer for ALOT";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBG)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button buttonNormal;
        private System.Windows.Forms.Label labelFinalStatus;
        private System.Windows.Forms.CheckBox checkBoxPreEnableRepack;
        private System.Windows.Forms.CheckBox checkBoxOptionVanilla;
        private System.Windows.Forms.CheckBox checkBoxOptionLimit2K;
        private System.Windows.Forms.CheckBox checkBoxOptionSkipScan;
        private System.Windows.Forms.Button buttonSTART;
        private System.Windows.Forms.Label labelCurrentStatus;
        private System.Windows.Forms.PictureBox pictureBoxBG;
        private System.Windows.Forms.Label labelOptions;
    }
}