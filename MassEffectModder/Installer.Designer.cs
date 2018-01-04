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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Installer));
            this.buttonNormal = new System.Windows.Forms.Button();
            this.labelFinalStatus = new System.Windows.Forms.Label();
            this.checkBoxOptionRepack = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionSkipScan = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionLimit2K = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionVanilla = new System.Windows.Forms.CheckBox();
            this.buttonSTART = new System.Windows.Forms.Button();
            this.labelCurrentStatus = new System.Windows.Forms.Label();
            this.labelOptions = new System.Windows.Forms.Label();
            this.pictureBoxBG = new System.Windows.Forms.PictureBox();
            this.labelOptionRepack = new System.Windows.Forms.Label();
            this.labelOptionLimit2K = new System.Windows.Forms.Label();
            this.labelOptionVanilla = new System.Windows.Forms.Label();
            this.labelOptionSkipScan = new System.Windows.Forms.Label();
            this.labelDesc = new System.Windows.Forms.Label();
            this.buttonMute = new System.Windows.Forms.Button();
            this.imageListAudio = new System.Windows.Forms.ImageList(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBG)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonNormal
            // 
            this.buttonNormal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonNormal.BackColor = System.Drawing.SystemColors.Control;
            this.buttonNormal.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonNormal.ForeColor = System.Drawing.SystemColors.ControlText;
            this.buttonNormal.Location = new System.Drawing.Point(16, 504);
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
            this.labelFinalStatus.Font = new System.Drawing.Font("Segoe UI Light", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelFinalStatus.ForeColor = System.Drawing.Color.White;
            this.labelFinalStatus.Location = new System.Drawing.Point(9, 264);
            this.labelFinalStatus.Name = "labelFinalStatus";
            this.labelFinalStatus.Size = new System.Drawing.Size(909, 45);
            this.labelFinalStatus.TabIndex = 25;
            this.labelFinalStatus.Text = "final status";
            this.labelFinalStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelFinalStatus.Visible = false;
            // 
            // checkBoxOptionRepack
            // 
            this.checkBoxOptionRepack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionRepack.AutoSize = true;
            this.checkBoxOptionRepack.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionRepack.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionRepack.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionRepack.Location = new System.Drawing.Point(16, 418);
            this.checkBoxOptionRepack.Name = "checkBoxOptionRepack";
            this.checkBoxOptionRepack.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionRepack.TabIndex = 35;
            this.checkBoxOptionRepack.TabStop = false;
            this.checkBoxOptionRepack.UseVisualStyleBackColor = false;
            // 
            // checkBoxOptionSkipScan
            // 
            this.checkBoxOptionSkipScan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionSkipScan.AutoSize = true;
            this.checkBoxOptionSkipScan.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionSkipScan.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionSkipScan.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionSkipScan.Location = new System.Drawing.Point(16, 481);
            this.checkBoxOptionSkipScan.Name = "checkBoxOptionSkipScan";
            this.checkBoxOptionSkipScan.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionSkipScan.TabIndex = 52;
            this.checkBoxOptionSkipScan.TabStop = false;
            this.checkBoxOptionSkipScan.UseVisualStyleBackColor = false;
            this.checkBoxOptionSkipScan.CheckedChanged += new System.EventHandler(this.checkBoxOptionSkipScan_CheckedChanged);
            // 
            // checkBoxOptionLimit2K
            // 
            this.checkBoxOptionLimit2K.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionLimit2K.AutoSize = true;
            this.checkBoxOptionLimit2K.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionLimit2K.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionLimit2K.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionLimit2K.Location = new System.Drawing.Point(16, 439);
            this.checkBoxOptionLimit2K.Name = "checkBoxOptionLimit2K";
            this.checkBoxOptionLimit2K.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionLimit2K.TabIndex = 39;
            this.checkBoxOptionLimit2K.TabStop = false;
            this.checkBoxOptionLimit2K.UseVisualStyleBackColor = false;
            this.checkBoxOptionLimit2K.Visible = false;
            // 
            // checkBoxOptionVanilla
            // 
            this.checkBoxOptionVanilla.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionVanilla.AutoSize = true;
            this.checkBoxOptionVanilla.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionVanilla.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionVanilla.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionVanilla.Location = new System.Drawing.Point(16, 460);
            this.checkBoxOptionVanilla.Name = "checkBoxOptionVanilla";
            this.checkBoxOptionVanilla.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionVanilla.TabIndex = 38;
            this.checkBoxOptionVanilla.TabStop = false;
            this.checkBoxOptionVanilla.UseVisualStyleBackColor = false;
            this.checkBoxOptionVanilla.CheckedChanged += new System.EventHandler(this.checkBoxOptionVanilla_CheckedChanged);
            // 
            // buttonSTART
            // 
            this.buttonSTART.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSTART.BackColor = System.Drawing.SystemColors.Control;
            this.buttonSTART.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonSTART.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonSTART.ForeColor = System.Drawing.Color.Black;
            this.buttonSTART.Location = new System.Drawing.Point(871, 504);
            this.buttonSTART.Name = "buttonSTART";
            this.buttonSTART.Size = new System.Drawing.Size(75, 23);
            this.buttonSTART.TabIndex = 2;
            this.buttonSTART.Text = "Install";
            this.buttonSTART.UseVisualStyleBackColor = false;
            this.buttonSTART.Click += new System.EventHandler(this.buttonSTART_Click);
            // 
            // labelCurrentStatus
            // 
            this.labelCurrentStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCurrentStatus.BackColor = System.Drawing.Color.Transparent;
            this.labelCurrentStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelCurrentStatus.Font = new System.Drawing.Font("Segoe UI Light", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelCurrentStatus.ForeColor = System.Drawing.Color.White;
            this.labelCurrentStatus.Location = new System.Drawing.Point(9, 297);
            this.labelCurrentStatus.Name = "labelCurrentStatus";
            this.labelCurrentStatus.Size = new System.Drawing.Size(909, 45);
            this.labelCurrentStatus.TabIndex = 28;
            this.labelCurrentStatus.Text = "current status";
            this.labelCurrentStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelCurrentStatus.Visible = false;
            // 
            // labelOptions
            // 
            this.labelOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptions.AutoSize = true;
            this.labelOptions.BackColor = System.Drawing.Color.Black;
            this.labelOptions.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelOptions.ForeColor = System.Drawing.Color.White;
            this.labelOptions.Location = new System.Drawing.Point(12, 381);
            this.labelOptions.Name = "labelOptions";
            this.labelOptions.Size = new System.Drawing.Size(74, 21);
            this.labelOptions.TabIndex = 54;
            this.labelOptions.Text = "Options:";
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
            this.pictureBoxBG.Size = new System.Drawing.Size(960, 540);
            this.pictureBoxBG.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxBG.TabIndex = 29;
            this.pictureBoxBG.TabStop = false;
            // 
            // labelOptionRepack
            // 
            this.labelOptionRepack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionRepack.AutoSize = true;
            this.labelOptionRepack.BackColor = System.Drawing.Color.Black;
            this.labelOptionRepack.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionRepack.ForeColor = System.Drawing.Color.White;
            this.labelOptionRepack.Location = new System.Drawing.Point(34, 413);
            this.labelOptionRepack.Name = "labelOptionRepack";
            this.labelOptionRepack.Size = new System.Drawing.Size(184, 21);
            this.labelOptionRepack.TabIndex = 55;
            this.labelOptionRepack.Text = "Recompress game files";
            // 
            // labelOptionLimit2K
            // 
            this.labelOptionLimit2K.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionLimit2K.AutoSize = true;
            this.labelOptionLimit2K.BackColor = System.Drawing.Color.Black;
            this.labelOptionLimit2K.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionLimit2K.ForeColor = System.Drawing.Color.White;
            this.labelOptionLimit2K.Location = new System.Drawing.Point(34, 434);
            this.labelOptionLimit2K.Name = "labelOptionLimit2K";
            this.labelOptionLimit2K.Size = new System.Drawing.Size(158, 21);
            this.labelOptionLimit2K.TabIndex = 56;
            this.labelOptionLimit2K.Text = "Limit textures to 2K";
            // 
            // labelOptionVanilla
            // 
            this.labelOptionVanilla.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionVanilla.AutoSize = true;
            this.labelOptionVanilla.BackColor = System.Drawing.Color.Black;
            this.labelOptionVanilla.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionVanilla.ForeColor = System.Drawing.Color.White;
            this.labelOptionVanilla.Location = new System.Drawing.Point(34, 455);
            this.labelOptionVanilla.Name = "labelOptionVanilla";
            this.labelOptionVanilla.Size = new System.Drawing.Size(147, 21);
            this.labelOptionVanilla.TabIndex = 57;
            this.labelOptionVanilla.Text = "Skip vanilla check";
            // 
            // labelOptionSkipScan
            // 
            this.labelOptionSkipScan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionSkipScan.AutoSize = true;
            this.labelOptionSkipScan.BackColor = System.Drawing.Color.Black;
            this.labelOptionSkipScan.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionSkipScan.ForeColor = System.Drawing.Color.White;
            this.labelOptionSkipScan.Location = new System.Drawing.Point(34, 476);
            this.labelOptionSkipScan.Name = "labelOptionSkipScan";
            this.labelOptionSkipScan.Size = new System.Drawing.Size(187, 21);
            this.labelOptionSkipScan.TabIndex = 58;
            this.labelOptionSkipScan.Text = "Skip scan/remove mips";
            // 
            // labelDesc
            // 
            this.labelDesc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDesc.BackColor = System.Drawing.Color.Transparent;
            this.labelDesc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDesc.Font = new System.Drawing.Font("Segoe UI Light", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelDesc.ForeColor = System.Drawing.Color.White;
            this.labelDesc.Location = new System.Drawing.Point(9, 229);
            this.labelDesc.Name = "labelDesc";
            this.labelDesc.Size = new System.Drawing.Size(909, 45);
            this.labelDesc.TabIndex = 59;
            this.labelDesc.Text = "description";
            this.labelDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelDesc.Visible = false;
            // 
            // buttonMute
            // 
            this.buttonMute.BackColor = System.Drawing.Color.Transparent;
            this.buttonMute.FlatAppearance.BorderSize = 0;
            this.buttonMute.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonMute.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonMute.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMute.ForeColor = System.Drawing.Color.Transparent;
            this.buttonMute.ImageIndex = 0;
            this.buttonMute.ImageList = this.imageListAudio;
            this.buttonMute.Location = new System.Drawing.Point(911, 12);
            this.buttonMute.Name = "buttonMute";
            this.buttonMute.Size = new System.Drawing.Size(35, 36);
            this.buttonMute.TabIndex = 60;
            this.buttonMute.UseVisualStyleBackColor = false;
            this.buttonMute.Visible = false;
            this.buttonMute.Click += new System.EventHandler(this.buttonMute_Click);
            // 
            // imageListAudio
            // 
            this.imageListAudio.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListAudio.ImageStream")));
            this.imageListAudio.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListAudio.Images.SetKeyName(0, "speaker.png");
            this.imageListAudio.Images.SetKeyName(1, "mute.png");
            // 
            // Installer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(958, 539);
            this.Controls.Add(this.buttonMute);
            this.Controls.Add(this.labelDesc);
            this.Controls.Add(this.labelOptionSkipScan);
            this.Controls.Add(this.labelOptionVanilla);
            this.Controls.Add(this.labelOptionLimit2K);
            this.Controls.Add(this.labelOptionRepack);
            this.Controls.Add(this.labelOptions);
            this.Controls.Add(this.checkBoxOptionSkipScan);
            this.Controls.Add(this.labelCurrentStatus);
            this.Controls.Add(this.checkBoxOptionLimit2K);
            this.Controls.Add(this.buttonSTART);
            this.Controls.Add(this.checkBoxOptionVanilla);
            this.Controls.Add(this.checkBoxOptionRepack);
            this.Controls.Add(this.buttonNormal);
            this.Controls.Add(this.labelFinalStatus);
            this.Controls.Add(this.pictureBoxBG);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1920, 1080);
            this.MinimumSize = new System.Drawing.Size(974, 578);
            this.Name = "Installer";
            this.Text = "MEM Installer for ALOT";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBG)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonNormal;
        private System.Windows.Forms.Label labelFinalStatus;
        private System.Windows.Forms.CheckBox checkBoxOptionRepack;
        private System.Windows.Forms.CheckBox checkBoxOptionVanilla;
        private System.Windows.Forms.CheckBox checkBoxOptionLimit2K;
        private System.Windows.Forms.CheckBox checkBoxOptionSkipScan;
        private System.Windows.Forms.Button buttonSTART;
        private System.Windows.Forms.Label labelCurrentStatus;
        private System.Windows.Forms.PictureBox pictureBoxBG;
        private System.Windows.Forms.Label labelOptions;
        private System.Windows.Forms.Label labelOptionRepack;
        private System.Windows.Forms.Label labelOptionLimit2K;
        private System.Windows.Forms.Label labelOptionVanilla;
        private System.Windows.Forms.Label labelOptionSkipScan;
        private System.Windows.Forms.Label labelDesc;
        private System.Windows.Forms.Button buttonMute;
        private System.Windows.Forms.ImageList imageListAudio;
    }
}