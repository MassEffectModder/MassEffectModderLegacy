/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2017 Pawel Kolodziejski
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
            this.buttonSTART = new System.Windows.Forms.Button();
            this.labelCurrentStatus = new System.Windows.Forms.Label();
            this.labelOptions = new System.Windows.Forms.Label();
            this.pictureBoxBG = new System.Windows.Forms.PictureBox();
            this.labelDesc = new System.Windows.Forms.Label();
            this.buttonMute = new System.Windows.Forms.Button();
            this.imageListAudio = new System.Windows.Forms.ImageList(this.components);
            this.labelModsSelection = new System.Windows.Forms.Label();
            this.comboBoxMod0 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod1 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod2 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod3 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod4 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod5 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod6 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod7 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod8 = new System.Windows.Forms.ComboBox();
            this.comboBoxMod9 = new System.Windows.Forms.ComboBox();
            this.labelOptionIndirectSound = new System.Windows.Forms.Label();
            this.checkBoxOptionIndirectSound = new System.Windows.Forms.CheckBox();
            this.labelOptionReshade = new System.Windows.Forms.Label();
            this.checkBoxOptionReshade = new System.Windows.Forms.CheckBox();
            this.labelOptionBik = new System.Windows.Forms.Label();
            this.checkBoxOptionBik = new System.Windows.Forms.CheckBox();
            this.labelOptionRepack = new System.Windows.Forms.Label();
            this.checkBoxOptionRepack = new System.Windows.Forms.CheckBox();
            this.labelOption2kLimit = new System.Windows.Forms.Label();
            this.checkBoxOption2kLimit = new System.Windows.Forms.CheckBox();
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
            this.buttonNormal.TabIndex = 2;
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
            this.labelFinalStatus.Location = new System.Drawing.Point(9, 267);
            this.labelFinalStatus.Name = "labelFinalStatus";
            this.labelFinalStatus.Size = new System.Drawing.Size(909, 45);
            this.labelFinalStatus.TabIndex = 25;
            this.labelFinalStatus.Text = "final status";
            this.labelFinalStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelFinalStatus.Visible = false;
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
            this.buttonSTART.TabIndex = 1;
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
            this.labelCurrentStatus.Location = new System.Drawing.Point(9, 302);
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
            this.labelOptions.Location = new System.Drawing.Point(12, 360);
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
            this.pictureBoxBG.Location = new System.Drawing.Point(-2, 1);
            this.pictureBoxBG.MinimumSize = new System.Drawing.Size(960, 540);
            this.pictureBoxBG.Name = "pictureBoxBG";
            this.pictureBoxBG.Size = new System.Drawing.Size(960, 540);
            this.pictureBoxBG.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxBG.TabIndex = 29;
            this.pictureBoxBG.TabStop = false;
            // 
            // labelDesc
            // 
            this.labelDesc.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDesc.BackColor = System.Drawing.Color.Transparent;
            this.labelDesc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelDesc.Font = new System.Drawing.Font("Segoe UI Light", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelDesc.ForeColor = System.Drawing.Color.White;
            this.labelDesc.Location = new System.Drawing.Point(9, 228);
            this.labelDesc.Name = "labelDesc";
            this.labelDesc.Size = new System.Drawing.Size(909, 45);
            this.labelDesc.TabIndex = 59;
            this.labelDesc.Text = "description";
            this.labelDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelDesc.Visible = false;
            // 
            // buttonMute
            // 
            this.buttonMute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
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
            this.buttonMute.TabStop = false;
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
            // labelModsSelection
            // 
            this.labelModsSelection.AutoSize = true;
            this.labelModsSelection.BackColor = System.Drawing.Color.Black;
            this.labelModsSelection.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelModsSelection.ForeColor = System.Drawing.Color.White;
            this.labelModsSelection.Location = new System.Drawing.Point(8, 5);
            this.labelModsSelection.Name = "labelModsSelection";
            this.labelModsSelection.Size = new System.Drawing.Size(187, 21);
            this.labelModsSelection.TabIndex = 61;
            this.labelModsSelection.Text = "Mod variants selection:";
            // 
            // comboBoxMod0
            // 
            this.comboBoxMod0.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod0.DisplayMember = "0";
            this.comboBoxMod0.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod0.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod0.FormattingEnabled = true;
            this.comboBoxMod0.Location = new System.Drawing.Point(12, 26);
            this.comboBoxMod0.Name = "comboBoxMod0";
            this.comboBoxMod0.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod0.TabIndex = 62;
            this.comboBoxMod0.TabStop = false;
            this.comboBoxMod0.ValueMember = "0";
            // 
            // comboBoxMod1
            // 
            this.comboBoxMod1.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod1.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod1.FormattingEnabled = true;
            this.comboBoxMod1.Location = new System.Drawing.Point(12, 46);
            this.comboBoxMod1.Name = "comboBoxMod1";
            this.comboBoxMod1.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod1.TabIndex = 63;
            this.comboBoxMod1.TabStop = false;
            // 
            // comboBoxMod2
            // 
            this.comboBoxMod2.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod2.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod2.FormattingEnabled = true;
            this.comboBoxMod2.Location = new System.Drawing.Point(12, 66);
            this.comboBoxMod2.Name = "comboBoxMod2";
            this.comboBoxMod2.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod2.TabIndex = 64;
            this.comboBoxMod2.TabStop = false;
            // 
            // comboBoxMod3
            // 
            this.comboBoxMod3.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod3.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod3.FormattingEnabled = true;
            this.comboBoxMod3.Location = new System.Drawing.Point(12, 86);
            this.comboBoxMod3.Name = "comboBoxMod3";
            this.comboBoxMod3.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod3.TabIndex = 65;
            this.comboBoxMod3.TabStop = false;
            // 
            // comboBoxMod4
            // 
            this.comboBoxMod4.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod4.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod4.FormattingEnabled = true;
            this.comboBoxMod4.Location = new System.Drawing.Point(12, 106);
            this.comboBoxMod4.Name = "comboBoxMod4";
            this.comboBoxMod4.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod4.TabIndex = 66;
            this.comboBoxMod4.TabStop = false;
            // 
            // comboBoxMod5
            // 
            this.comboBoxMod5.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod5.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod5.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod5.FormattingEnabled = true;
            this.comboBoxMod5.Location = new System.Drawing.Point(12, 126);
            this.comboBoxMod5.Name = "comboBoxMod5";
            this.comboBoxMod5.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod5.TabIndex = 67;
            this.comboBoxMod5.TabStop = false;
            // 
            // comboBoxMod6
            // 
            this.comboBoxMod6.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod6.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod6.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod6.FormattingEnabled = true;
            this.comboBoxMod6.Location = new System.Drawing.Point(12, 146);
            this.comboBoxMod6.Name = "comboBoxMod6";
            this.comboBoxMod6.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod6.TabIndex = 68;
            this.comboBoxMod6.TabStop = false;
            // 
            // comboBoxMod7
            // 
            this.comboBoxMod7.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod7.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod7.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod7.FormattingEnabled = true;
            this.comboBoxMod7.Location = new System.Drawing.Point(12, 166);
            this.comboBoxMod7.Name = "comboBoxMod7";
            this.comboBoxMod7.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod7.TabIndex = 69;
            this.comboBoxMod7.TabStop = false;
            // 
            // comboBoxMod8
            // 
            this.comboBoxMod8.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod8.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod8.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod8.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod8.FormattingEnabled = true;
            this.comboBoxMod8.Location = new System.Drawing.Point(12, 186);
            this.comboBoxMod8.Name = "comboBoxMod8";
            this.comboBoxMod8.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod8.TabIndex = 70;
            this.comboBoxMod8.TabStop = false;
            // 
            // comboBoxMod9
            // 
            this.comboBoxMod9.BackColor = System.Drawing.Color.Black;
            this.comboBoxMod9.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMod9.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxMod9.ForeColor = System.Drawing.Color.White;
            this.comboBoxMod9.FormattingEnabled = true;
            this.comboBoxMod9.Location = new System.Drawing.Point(12, 206);
            this.comboBoxMod9.Name = "comboBoxMod9";
            this.comboBoxMod9.Size = new System.Drawing.Size(209, 21);
            this.comboBoxMod9.TabIndex = 71;
            this.comboBoxMod9.TabStop = false;
            // 
            // labelOptionIndirectSound
            // 
            this.labelOptionIndirectSound.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionIndirectSound.AutoSize = true;
            this.labelOptionIndirectSound.BackColor = System.Drawing.Color.Black;
            this.labelOptionIndirectSound.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelOptionIndirectSound.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionIndirectSound.ForeColor = System.Drawing.Color.White;
            this.labelOptionIndirectSound.Location = new System.Drawing.Point(34, 423);
            this.labelOptionIndirectSound.Name = "labelOptionIndirectSound";
            this.labelOptionIndirectSound.Size = new System.Drawing.Size(173, 21);
            this.labelOptionIndirectSound.TabIndex = 73;
            this.labelOptionIndirectSound.Text = "Install Indirect Sound";
            // 
            // checkBoxOptionIndirectSound
            // 
            this.checkBoxOptionIndirectSound.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionIndirectSound.AutoSize = true;
            this.checkBoxOptionIndirectSound.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionIndirectSound.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionIndirectSound.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionIndirectSound.Location = new System.Drawing.Point(16, 428);
            this.checkBoxOptionIndirectSound.Name = "checkBoxOptionIndirectSound";
            this.checkBoxOptionIndirectSound.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionIndirectSound.TabIndex = 72;
            this.checkBoxOptionIndirectSound.TabStop = false;
            this.checkBoxOptionIndirectSound.UseVisualStyleBackColor = false;
            // 
            // labelOptionReshade
            // 
            this.labelOptionReshade.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionReshade.AutoSize = true;
            this.labelOptionReshade.BackColor = System.Drawing.Color.Black;
            this.labelOptionReshade.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelOptionReshade.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionReshade.ForeColor = System.Drawing.Color.White;
            this.labelOptionReshade.Location = new System.Drawing.Point(34, 444);
            this.labelOptionReshade.Name = "labelOptionReshade";
            this.labelOptionReshade.Size = new System.Drawing.Size(127, 21);
            this.labelOptionReshade.TabIndex = 75;
            this.labelOptionReshade.Text = "Install ReShade";
            // 
            // checkBoxOptionReshade
            // 
            this.checkBoxOptionReshade.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionReshade.AutoSize = true;
            this.checkBoxOptionReshade.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionReshade.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionReshade.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionReshade.Location = new System.Drawing.Point(16, 449);
            this.checkBoxOptionReshade.Name = "checkBoxOptionReshade";
            this.checkBoxOptionReshade.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionReshade.TabIndex = 74;
            this.checkBoxOptionReshade.TabStop = false;
            this.checkBoxOptionReshade.UseVisualStyleBackColor = false;
            // 
            // labelOptionBik
            // 
            this.labelOptionBik.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionBik.AutoSize = true;
            this.labelOptionBik.BackColor = System.Drawing.Color.Black;
            this.labelOptionBik.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelOptionBik.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionBik.ForeColor = System.Drawing.Color.White;
            this.labelOptionBik.Location = new System.Drawing.Point(34, 465);
            this.labelOptionBik.Name = "labelOptionBik";
            this.labelOptionBik.Size = new System.Drawing.Size(173, 21);
            this.labelOptionBik.TabIndex = 77;
            this.labelOptionBik.Text = "Install MEUITM video";
            // 
            // checkBoxOptionBik
            // 
            this.checkBoxOptionBik.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionBik.AutoSize = true;
            this.checkBoxOptionBik.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionBik.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionBik.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionBik.Location = new System.Drawing.Point(16, 470);
            this.checkBoxOptionBik.Name = "checkBoxOptionBik";
            this.checkBoxOptionBik.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionBik.TabIndex = 76;
            this.checkBoxOptionBik.TabStop = false;
            this.checkBoxOptionBik.UseVisualStyleBackColor = false;
            // 
            // labelOptionRepack
            // 
            this.labelOptionRepack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOptionRepack.AutoSize = true;
            this.labelOptionRepack.BackColor = System.Drawing.Color.Black;
            this.labelOptionRepack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelOptionRepack.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOptionRepack.ForeColor = System.Drawing.Color.White;
            this.labelOptionRepack.Location = new System.Drawing.Point(34, 381);
            this.labelOptionRepack.Name = "labelOptionRepack";
            this.labelOptionRepack.Size = new System.Drawing.Size(151, 21);
            this.labelOptionRepack.TabIndex = 79;
            this.labelOptionRepack.Text = "Repack Game Files";
            // 
            // checkBoxOptionRepack
            // 
            this.checkBoxOptionRepack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOptionRepack.AutoSize = true;
            this.checkBoxOptionRepack.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOptionRepack.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOptionRepack.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOptionRepack.Location = new System.Drawing.Point(16, 386);
            this.checkBoxOptionRepack.Name = "checkBoxOptionRepack";
            this.checkBoxOptionRepack.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOptionRepack.TabIndex = 78;
            this.checkBoxOptionRepack.TabStop = false;
            this.checkBoxOptionRepack.UseVisualStyleBackColor = false;
            // 
            // labelOption2kLimit
            // 
            this.labelOption2kLimit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOption2kLimit.AutoSize = true;
            this.labelOption2kLimit.BackColor = System.Drawing.Color.Black;
            this.labelOption2kLimit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.labelOption2kLimit.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelOption2kLimit.ForeColor = System.Drawing.Color.White;
            this.labelOption2kLimit.Location = new System.Drawing.Point(34, 402);
            this.labelOption2kLimit.Name = "labelOption2kLimit";
            this.labelOption2kLimit.Size = new System.Drawing.Size(160, 21);
            this.labelOption2kLimit.TabIndex = 81;
            this.labelOption2kLimit.Text = "Limit Textures to 2K";
            // 
            // checkBoxOption2kLimit
            // 
            this.checkBoxOption2kLimit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxOption2kLimit.AutoSize = true;
            this.checkBoxOption2kLimit.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxOption2kLimit.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.checkBoxOption2kLimit.ForeColor = System.Drawing.SystemColors.ControlText;
            this.checkBoxOption2kLimit.Location = new System.Drawing.Point(16, 407);
            this.checkBoxOption2kLimit.Name = "checkBoxOption2kLimit";
            this.checkBoxOption2kLimit.Size = new System.Drawing.Size(15, 14);
            this.checkBoxOption2kLimit.TabIndex = 80;
            this.checkBoxOption2kLimit.TabStop = false;
            this.checkBoxOption2kLimit.UseVisualStyleBackColor = false;
            // 
            // Installer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(958, 539);
            this.Controls.Add(this.labelOption2kLimit);
            this.Controls.Add(this.checkBoxOption2kLimit);
            this.Controls.Add(this.labelOptionRepack);
            this.Controls.Add(this.checkBoxOptionRepack);
            this.Controls.Add(this.labelOptionBik);
            this.Controls.Add(this.checkBoxOptionBik);
            this.Controls.Add(this.labelOptionReshade);
            this.Controls.Add(this.checkBoxOptionReshade);
            this.Controls.Add(this.labelOptionIndirectSound);
            this.Controls.Add(this.checkBoxOptionIndirectSound);
            this.Controls.Add(this.comboBoxMod9);
            this.Controls.Add(this.comboBoxMod8);
            this.Controls.Add(this.comboBoxMod7);
            this.Controls.Add(this.comboBoxMod6);
            this.Controls.Add(this.comboBoxMod5);
            this.Controls.Add(this.comboBoxMod4);
            this.Controls.Add(this.comboBoxMod3);
            this.Controls.Add(this.comboBoxMod2);
            this.Controls.Add(this.comboBoxMod1);
            this.Controls.Add(this.comboBoxMod0);
            this.Controls.Add(this.labelModsSelection);
            this.Controls.Add(this.buttonMute);
            this.Controls.Add(this.labelDesc);
            this.Controls.Add(this.labelOptions);
            this.Controls.Add(this.labelCurrentStatus);
            this.Controls.Add(this.buttonSTART);
            this.Controls.Add(this.buttonNormal);
            this.Controls.Add(this.labelFinalStatus);
            this.Controls.Add(this.pictureBoxBG);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1920, 1080);
            this.MinimumSize = new System.Drawing.Size(974, 578);
            this.Name = "Installer";
            this.Text = "MEM Installer";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxBG)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonNormal;
        private System.Windows.Forms.Label labelFinalStatus;
        private System.Windows.Forms.Button buttonSTART;
        private System.Windows.Forms.Label labelCurrentStatus;
        private System.Windows.Forms.PictureBox pictureBoxBG;
        private System.Windows.Forms.Label labelOptions;
        private System.Windows.Forms.Label labelDesc;
        private System.Windows.Forms.Button buttonMute;
        private System.Windows.Forms.ImageList imageListAudio;
        private System.Windows.Forms.Label labelModsSelection;
        private System.Windows.Forms.ComboBox comboBoxMod0;
        private System.Windows.Forms.ComboBox comboBoxMod1;
        private System.Windows.Forms.ComboBox comboBoxMod2;
        private System.Windows.Forms.ComboBox comboBoxMod3;
        private System.Windows.Forms.ComboBox comboBoxMod4;
        private System.Windows.Forms.ComboBox comboBoxMod5;
        private System.Windows.Forms.ComboBox comboBoxMod6;
        private System.Windows.Forms.ComboBox comboBoxMod7;
        private System.Windows.Forms.ComboBox comboBoxMod8;
        private System.Windows.Forms.ComboBox comboBoxMod9;
        private System.Windows.Forms.Label labelOptionIndirectSound;
        private System.Windows.Forms.CheckBox checkBoxOptionIndirectSound;
        private System.Windows.Forms.Label labelOptionReshade;
        private System.Windows.Forms.CheckBox checkBoxOptionReshade;
        private System.Windows.Forms.Label labelOptionBik;
        private System.Windows.Forms.CheckBox checkBoxOptionBik;
        private System.Windows.Forms.Label labelOptionRepack;
        private System.Windows.Forms.CheckBox checkBoxOptionRepack;
        private System.Windows.Forms.Label labelOption2kLimit;
        private System.Windows.Forms.CheckBox checkBoxOption2kLimit;
    }
}