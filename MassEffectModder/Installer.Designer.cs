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
            this.checkBoxPreMods = new System.Windows.Forms.CheckBox();
            this.checkBoxLOD = new System.Windows.Forms.CheckBox();
            this.buttonSTART = new System.Windows.Forms.Button();
            this.checkBoxScan = new System.Windows.Forms.CheckBox();
            this.checkBoxMipMaps = new System.Windows.Forms.CheckBox();
            this.checkBoxTextures = new System.Windows.Forms.CheckBox();
            this.checkBoxStore = new System.Windows.Forms.CheckBox();
            this.labelStatusLOD = new System.Windows.Forms.Label();
            this.labelStatusScan = new System.Windows.Forms.Label();
            this.labelStatusMipMaps = new System.Windows.Forms.Label();
            this.labelStatusTextures = new System.Windows.Forms.Label();
            this.labelStatusStore = new System.Windows.Forms.Label();
            this.labelPreGamePath = new System.Windows.Forms.Label();
            this.label0 = new System.Windows.Forms.Label();
            this.labelStore = new System.Windows.Forms.Label();
            this.labelTextures = new System.Windows.Forms.Label();
            this.labelMipMaps = new System.Windows.Forms.Label();
            this.labelScan = new System.Windows.Forms.Label();
            this.labelLOD = new System.Windows.Forms.Label();
            this.labelPrepare = new System.Windows.Forms.Label();
            this.labelStatusPrepare = new System.Windows.Forms.Label();
            this.checkBoxPrepare = new System.Windows.Forms.CheckBox();
            this.groupBoxPreInstallCheck = new System.Windows.Forms.GroupBox();
            this.checkBoxPreVanilla = new System.Windows.Forms.CheckBox();
            this.checkBoxPreSpace = new System.Windows.Forms.CheckBox();
            this.checkBoxPreAccess = new System.Windows.Forms.CheckBox();
            this.checkBoxPrePath = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.labelPreMods = new System.Windows.Forms.Label();
            this.labelPreVanilla = new System.Windows.Forms.Label();
            this.labelPreSpace = new System.Windows.Forms.Label();
            this.labelPreAccess = new System.Windows.Forms.Label();
            this.labelPrePath = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonPreChangePath = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonPreInstallCheck = new System.Windows.Forms.Button();
            this.groupBoxInstaller = new System.Windows.Forms.GroupBox();
            this.checkBoxPackDLC = new System.Windows.Forms.CheckBox();
            this.checkBoxRepackZlib = new System.Windows.Forms.CheckBox();
            this.labelStatusPackDLC = new System.Windows.Forms.Label();
            this.labelME3DLCPack = new System.Windows.Forms.Label();
            this.labelStatusRepackZlib = new System.Windows.Forms.Label();
            this.labelMERepackZlib = new System.Windows.Forms.Label();
            this.buttonExit = new System.Windows.Forms.Button();
            this.buttonNormal = new System.Windows.Forms.Button();
            this.labelFinalStatus = new System.Windows.Forms.Label();
            this.checkBoxPreEnableRepack = new System.Windows.Forms.CheckBox();
            this.checkBoxPreEnablePack = new System.Windows.Forms.CheckBox();
            this.groupBoxOptions = new System.Windows.Forms.GroupBox();
            this.checkBoxOptionVanilla = new System.Windows.Forms.CheckBox();
            this.checkBoxOptionFaster = new System.Windows.Forms.CheckBox();
            this.groupBoxPreInstallCheck.SuspendLayout();
            this.groupBoxInstaller.SuspendLayout();
            this.groupBoxOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxPreMods
            // 
            this.checkBoxPreMods.AutoSize = true;
            this.checkBoxPreMods.Enabled = false;
            this.checkBoxPreMods.FlatAppearance.CheckedBackColor = System.Drawing.Color.Green;
            this.checkBoxPreMods.Location = new System.Drawing.Point(166, 52);
            this.checkBoxPreMods.Name = "checkBoxPreMods";
            this.checkBoxPreMods.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPreMods.TabIndex = 2;
            this.checkBoxPreMods.TabStop = false;
            this.checkBoxPreMods.UseVisualStyleBackColor = true;
            // 
            // checkBoxLOD
            // 
            this.checkBoxLOD.AutoSize = true;
            this.checkBoxLOD.Enabled = false;
            this.checkBoxLOD.Location = new System.Drawing.Point(166, 164);
            this.checkBoxLOD.Name = "checkBoxLOD";
            this.checkBoxLOD.Size = new System.Drawing.Size(15, 14);
            this.checkBoxLOD.TabIndex = 3;
            this.checkBoxLOD.UseVisualStyleBackColor = true;
            // 
            // buttonSTART
            // 
            this.buttonSTART.BackColor = System.Drawing.Color.LimeGreen;
            this.buttonSTART.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonSTART.Location = new System.Drawing.Point(16, 19);
            this.buttonSTART.Name = "buttonSTART";
            this.buttonSTART.Size = new System.Drawing.Size(75, 23);
            this.buttonSTART.TabIndex = 2;
            this.buttonSTART.Text = "START";
            this.buttonSTART.UseVisualStyleBackColor = false;
            this.buttonSTART.Click += new System.EventHandler(this.buttonSTART_Click);
            // 
            // checkBoxScan
            // 
            this.checkBoxScan.AutoSize = true;
            this.checkBoxScan.Enabled = false;
            this.checkBoxScan.Location = new System.Drawing.Point(166, 74);
            this.checkBoxScan.Name = "checkBoxScan";
            this.checkBoxScan.Size = new System.Drawing.Size(15, 14);
            this.checkBoxScan.TabIndex = 5;
            this.checkBoxScan.UseVisualStyleBackColor = true;
            // 
            // checkBoxMipMaps
            // 
            this.checkBoxMipMaps.AutoSize = true;
            this.checkBoxMipMaps.Enabled = false;
            this.checkBoxMipMaps.Location = new System.Drawing.Point(166, 141);
            this.checkBoxMipMaps.Name = "checkBoxMipMaps";
            this.checkBoxMipMaps.Size = new System.Drawing.Size(15, 14);
            this.checkBoxMipMaps.TabIndex = 6;
            this.checkBoxMipMaps.UseVisualStyleBackColor = true;
            // 
            // checkBoxTextures
            // 
            this.checkBoxTextures.AutoSize = true;
            this.checkBoxTextures.Enabled = false;
            this.checkBoxTextures.Location = new System.Drawing.Point(166, 97);
            this.checkBoxTextures.Name = "checkBoxTextures";
            this.checkBoxTextures.Size = new System.Drawing.Size(15, 14);
            this.checkBoxTextures.TabIndex = 7;
            this.checkBoxTextures.UseVisualStyleBackColor = true;
            // 
            // checkBoxStore
            // 
            this.checkBoxStore.AutoSize = true;
            this.checkBoxStore.Enabled = false;
            this.checkBoxStore.Location = new System.Drawing.Point(166, 119);
            this.checkBoxStore.Name = "checkBoxStore";
            this.checkBoxStore.Size = new System.Drawing.Size(15, 14);
            this.checkBoxStore.TabIndex = 8;
            this.checkBoxStore.UseVisualStyleBackColor = true;
            // 
            // labelStatusLOD
            // 
            this.labelStatusLOD.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusLOD.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusLOD.Location = new System.Drawing.Point(187, 165);
            this.labelStatusLOD.Name = "labelStatusLOD";
            this.labelStatusLOD.Size = new System.Drawing.Size(252, 13);
            this.labelStatusLOD.TabIndex = 9;
            this.labelStatusLOD.Text = "label";
            // 
            // labelStatusScan
            // 
            this.labelStatusScan.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusScan.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusScan.Location = new System.Drawing.Point(187, 74);
            this.labelStatusScan.Name = "labelStatusScan";
            this.labelStatusScan.Size = new System.Drawing.Size(252, 13);
            this.labelStatusScan.TabIndex = 10;
            this.labelStatusScan.Text = "label";
            // 
            // labelStatusMipMaps
            // 
            this.labelStatusMipMaps.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusMipMaps.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusMipMaps.Location = new System.Drawing.Point(187, 142);
            this.labelStatusMipMaps.Name = "labelStatusMipMaps";
            this.labelStatusMipMaps.Size = new System.Drawing.Size(252, 13);
            this.labelStatusMipMaps.TabIndex = 11;
            this.labelStatusMipMaps.Text = "label";
            // 
            // labelStatusTextures
            // 
            this.labelStatusTextures.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusTextures.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusTextures.Location = new System.Drawing.Point(187, 97);
            this.labelStatusTextures.Name = "labelStatusTextures";
            this.labelStatusTextures.Size = new System.Drawing.Size(252, 13);
            this.labelStatusTextures.TabIndex = 12;
            this.labelStatusTextures.Text = "label";
            // 
            // labelStatusStore
            // 
            this.labelStatusStore.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusStore.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusStore.Location = new System.Drawing.Point(187, 119);
            this.labelStatusStore.Name = "labelStatusStore";
            this.labelStatusStore.Size = new System.Drawing.Size(252, 13);
            this.labelStatusStore.TabIndex = 13;
            this.labelStatusStore.Text = "label";
            // 
            // labelPreGamePath
            // 
            this.labelPreGamePath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelPreGamePath.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelPreGamePath.Location = new System.Drawing.Point(187, 75);
            this.labelPreGamePath.Name = "labelPreGamePath";
            this.labelPreGamePath.Size = new System.Drawing.Size(252, 13);
            this.labelPreGamePath.TabIndex = 15;
            this.labelPreGamePath.Text = "label";
            // 
            // label0
            // 
            this.label0.AutoSize = true;
            this.label0.Location = new System.Drawing.Point(13, 75);
            this.label0.Name = "label0";
            this.label0.Size = new System.Drawing.Size(60, 13);
            this.label0.TabIndex = 21;
            this.label0.Text = "Game Path";
            // 
            // labelStore
            // 
            this.labelStore.AutoSize = true;
            this.labelStore.Location = new System.Drawing.Point(13, 120);
            this.labelStore.Name = "labelStore";
            this.labelStore.Size = new System.Drawing.Size(89, 13);
            this.labelStore.TabIndex = 20;
            this.labelStore.Text = "Store Game Data";
            // 
            // labelTextures
            // 
            this.labelTextures.AutoSize = true;
            this.labelTextures.Location = new System.Drawing.Point(13, 97);
            this.labelTextures.Name = "labelTextures";
            this.labelTextures.Size = new System.Drawing.Size(103, 13);
            this.labelTextures.TabIndex = 19;
            this.labelTextures.Text = "Processing Textures";
            // 
            // labelMipMaps
            // 
            this.labelMipMaps.AutoSize = true;
            this.labelMipMaps.Location = new System.Drawing.Point(13, 142);
            this.labelMipMaps.Name = "labelMipMaps";
            this.labelMipMaps.Size = new System.Drawing.Size(124, 13);
            this.labelMipMaps.TabIndex = 18;
            this.labelMipMaps.Text = "Remove Empty Mipmaps";
            // 
            // labelScan
            // 
            this.labelScan.AutoSize = true;
            this.labelScan.Location = new System.Drawing.Point(13, 74);
            this.labelScan.Name = "labelScan";
            this.labelScan.Size = new System.Drawing.Size(76, 13);
            this.labelScan.TabIndex = 17;
            this.labelScan.Text = "Scan Textures";
            // 
            // labelLOD
            // 
            this.labelLOD.AutoSize = true;
            this.labelLOD.Location = new System.Drawing.Point(13, 165);
            this.labelLOD.Name = "labelLOD";
            this.labelLOD.Size = new System.Drawing.Size(105, 13);
            this.labelLOD.TabIndex = 16;
            this.labelLOD.Text = "Apply Game Settings";
            // 
            // labelPrepare
            // 
            this.labelPrepare.AutoSize = true;
            this.labelPrepare.Location = new System.Drawing.Point(13, 52);
            this.labelPrepare.Name = "labelPrepare";
            this.labelPrepare.Size = new System.Drawing.Size(101, 13);
            this.labelPrepare.TabIndex = 24;
            this.labelPrepare.Text = "Prepare Game Data";
            // 
            // labelStatusPrepare
            // 
            this.labelStatusPrepare.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusPrepare.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusPrepare.Location = new System.Drawing.Point(187, 52);
            this.labelStatusPrepare.Name = "labelStatusPrepare";
            this.labelStatusPrepare.Size = new System.Drawing.Size(252, 13);
            this.labelStatusPrepare.TabIndex = 23;
            this.labelStatusPrepare.Text = "label";
            // 
            // checkBoxPrepare
            // 
            this.checkBoxPrepare.AutoSize = true;
            this.checkBoxPrepare.Enabled = false;
            this.checkBoxPrepare.Location = new System.Drawing.Point(166, 52);
            this.checkBoxPrepare.Name = "checkBoxPrepare";
            this.checkBoxPrepare.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPrepare.TabIndex = 22;
            this.checkBoxPrepare.UseVisualStyleBackColor = true;
            // 
            // groupBoxPreInstallCheck
            // 
            this.groupBoxPreInstallCheck.Controls.Add(this.checkBoxPreVanilla);
            this.groupBoxPreInstallCheck.Controls.Add(this.checkBoxPreSpace);
            this.groupBoxPreInstallCheck.Controls.Add(this.checkBoxPreAccess);
            this.groupBoxPreInstallCheck.Controls.Add(this.checkBoxPrePath);
            this.groupBoxPreInstallCheck.Controls.Add(this.label7);
            this.groupBoxPreInstallCheck.Controls.Add(this.labelPreMods);
            this.groupBoxPreInstallCheck.Controls.Add(this.checkBoxPreMods);
            this.groupBoxPreInstallCheck.Controls.Add(this.labelPreVanilla);
            this.groupBoxPreInstallCheck.Controls.Add(this.labelPreSpace);
            this.groupBoxPreInstallCheck.Controls.Add(this.labelPreAccess);
            this.groupBoxPreInstallCheck.Controls.Add(this.labelPrePath);
            this.groupBoxPreInstallCheck.Controls.Add(this.label3);
            this.groupBoxPreInstallCheck.Controls.Add(this.label2);
            this.groupBoxPreInstallCheck.Controls.Add(this.buttonPreChangePath);
            this.groupBoxPreInstallCheck.Controls.Add(this.label1);
            this.groupBoxPreInstallCheck.Controls.Add(this.buttonPreInstallCheck);
            this.groupBoxPreInstallCheck.Controls.Add(this.label0);
            this.groupBoxPreInstallCheck.Controls.Add(this.labelPreGamePath);
            this.groupBoxPreInstallCheck.Location = new System.Drawing.Point(12, 12);
            this.groupBoxPreInstallCheck.Name = "groupBoxPreInstallCheck";
            this.groupBoxPreInstallCheck.Size = new System.Drawing.Size(569, 195);
            this.groupBoxPreInstallCheck.TabIndex = 25;
            this.groupBoxPreInstallCheck.TabStop = false;
            this.groupBoxPreInstallCheck.Text = "Pre-Installer Checks";
            // 
            // checkBoxPreVanilla
            // 
            this.checkBoxPreVanilla.AutoSize = true;
            this.checkBoxPreVanilla.Enabled = false;
            this.checkBoxPreVanilla.FlatAppearance.CheckedBackColor = System.Drawing.Color.Green;
            this.checkBoxPreVanilla.Location = new System.Drawing.Point(166, 165);
            this.checkBoxPreVanilla.Name = "checkBoxPreVanilla";
            this.checkBoxPreVanilla.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPreVanilla.TabIndex = 50;
            this.checkBoxPreVanilla.TabStop = false;
            this.checkBoxPreVanilla.UseVisualStyleBackColor = true;
            // 
            // checkBoxPreSpace
            // 
            this.checkBoxPreSpace.AutoSize = true;
            this.checkBoxPreSpace.Enabled = false;
            this.checkBoxPreSpace.FlatAppearance.CheckedBackColor = System.Drawing.Color.Green;
            this.checkBoxPreSpace.Location = new System.Drawing.Point(166, 142);
            this.checkBoxPreSpace.Name = "checkBoxPreSpace";
            this.checkBoxPreSpace.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPreSpace.TabIndex = 49;
            this.checkBoxPreSpace.TabStop = false;
            this.checkBoxPreSpace.UseVisualStyleBackColor = true;
            // 
            // checkBoxPreAccess
            // 
            this.checkBoxPreAccess.AutoSize = true;
            this.checkBoxPreAccess.Enabled = false;
            this.checkBoxPreAccess.FlatAppearance.CheckedBackColor = System.Drawing.Color.Green;
            this.checkBoxPreAccess.Location = new System.Drawing.Point(166, 120);
            this.checkBoxPreAccess.Name = "checkBoxPreAccess";
            this.checkBoxPreAccess.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPreAccess.TabIndex = 48;
            this.checkBoxPreAccess.TabStop = false;
            this.checkBoxPreAccess.UseVisualStyleBackColor = true;
            // 
            // checkBoxPrePath
            // 
            this.checkBoxPrePath.AutoSize = true;
            this.checkBoxPrePath.Enabled = false;
            this.checkBoxPrePath.FlatAppearance.CheckedBackColor = System.Drawing.Color.Green;
            this.checkBoxPrePath.Location = new System.Drawing.Point(166, 75);
            this.checkBoxPrePath.Name = "checkBoxPrePath";
            this.checkBoxPrePath.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPrePath.TabIndex = 47;
            this.checkBoxPrePath.TabStop = false;
            this.checkBoxPrePath.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(13, 52);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(61, 13);
            this.label7.TabIndex = 46;
            this.label7.Text = "MEM Mods";
            // 
            // labelPreMods
            // 
            this.labelPreMods.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelPreMods.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelPreMods.Location = new System.Drawing.Point(187, 52);
            this.labelPreMods.Name = "labelPreMods";
            this.labelPreMods.Size = new System.Drawing.Size(252, 13);
            this.labelPreMods.TabIndex = 45;
            this.labelPreMods.Text = "label";
            // 
            // labelPreVanilla
            // 
            this.labelPreVanilla.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelPreVanilla.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelPreVanilla.Location = new System.Drawing.Point(187, 165);
            this.labelPreVanilla.Name = "labelPreVanilla";
            this.labelPreVanilla.Size = new System.Drawing.Size(252, 13);
            this.labelPreVanilla.TabIndex = 40;
            this.labelPreVanilla.Text = "label";
            // 
            // labelPreSpace
            // 
            this.labelPreSpace.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelPreSpace.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelPreSpace.Location = new System.Drawing.Point(187, 142);
            this.labelPreSpace.Name = "labelPreSpace";
            this.labelPreSpace.Size = new System.Drawing.Size(252, 13);
            this.labelPreSpace.TabIndex = 39;
            this.labelPreSpace.Text = "label";
            // 
            // labelPreAccess
            // 
            this.labelPreAccess.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelPreAccess.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelPreAccess.Location = new System.Drawing.Point(187, 120);
            this.labelPreAccess.Name = "labelPreAccess";
            this.labelPreAccess.Size = new System.Drawing.Size(252, 13);
            this.labelPreAccess.TabIndex = 38;
            this.labelPreAccess.Text = "label";
            // 
            // labelPrePath
            // 
            this.labelPrePath.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.labelPrePath.Location = new System.Drawing.Point(13, 97);
            this.labelPrePath.Name = "labelPrePath";
            this.labelPrePath.Size = new System.Drawing.Size(426, 13);
            this.labelPrePath.TabIndex = 29;
            this.labelPrePath.Text = "label";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 165);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 13);
            this.label3.TabIndex = 31;
            this.label3.Text = "Game Files";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 142);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 30;
            this.label2.Text = "Drive Space";
            // 
            // buttonPreChangePath
            // 
            this.buttonPreChangePath.Location = new System.Drawing.Point(445, 92);
            this.buttonPreChangePath.Name = "buttonPreChangePath";
            this.buttonPreChangePath.Size = new System.Drawing.Size(111, 23);
            this.buttonPreChangePath.TabIndex = 1;
            this.buttonPreChangePath.Text = "Change Path...";
            this.buttonPreChangePath.UseVisualStyleBackColor = true;
            this.buttonPreChangePath.Click += new System.EventHandler(this.buttonChangePath_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 13);
            this.label1.TabIndex = 28;
            this.label1.Text = "Write Access";
            // 
            // buttonPreInstallCheck
            // 
            this.buttonPreInstallCheck.BackColor = System.Drawing.Color.DarkBlue;
            this.buttonPreInstallCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonPreInstallCheck.ForeColor = System.Drawing.Color.White;
            this.buttonPreInstallCheck.Location = new System.Drawing.Point(14, 19);
            this.buttonPreInstallCheck.Name = "buttonPreInstallCheck";
            this.buttonPreInstallCheck.Size = new System.Drawing.Size(75, 23);
            this.buttonPreInstallCheck.TabIndex = 0;
            this.buttonPreInstallCheck.Text = "CHECK";
            this.buttonPreInstallCheck.UseVisualStyleBackColor = false;
            this.buttonPreInstallCheck.Click += new System.EventHandler(this.buttonPreInstallCheck_Click);
            // 
            // groupBoxInstaller
            // 
            this.groupBoxInstaller.Controls.Add(this.checkBoxPackDLC);
            this.groupBoxInstaller.Controls.Add(this.checkBoxRepackZlib);
            this.groupBoxInstaller.Controls.Add(this.labelStatusPackDLC);
            this.groupBoxInstaller.Controls.Add(this.labelME3DLCPack);
            this.groupBoxInstaller.Controls.Add(this.labelStatusRepackZlib);
            this.groupBoxInstaller.Controls.Add(this.labelMERepackZlib);
            this.groupBoxInstaller.Controls.Add(this.buttonSTART);
            this.groupBoxInstaller.Controls.Add(this.labelPrepare);
            this.groupBoxInstaller.Controls.Add(this.checkBoxLOD);
            this.groupBoxInstaller.Controls.Add(this.labelStatusPrepare);
            this.groupBoxInstaller.Controls.Add(this.checkBoxScan);
            this.groupBoxInstaller.Controls.Add(this.checkBoxPrepare);
            this.groupBoxInstaller.Controls.Add(this.checkBoxMipMaps);
            this.groupBoxInstaller.Controls.Add(this.checkBoxTextures);
            this.groupBoxInstaller.Controls.Add(this.labelStore);
            this.groupBoxInstaller.Controls.Add(this.checkBoxStore);
            this.groupBoxInstaller.Controls.Add(this.labelTextures);
            this.groupBoxInstaller.Controls.Add(this.labelStatusLOD);
            this.groupBoxInstaller.Controls.Add(this.labelMipMaps);
            this.groupBoxInstaller.Controls.Add(this.labelStatusScan);
            this.groupBoxInstaller.Controls.Add(this.labelScan);
            this.groupBoxInstaller.Controls.Add(this.labelStatusMipMaps);
            this.groupBoxInstaller.Controls.Add(this.labelLOD);
            this.groupBoxInstaller.Controls.Add(this.labelStatusTextures);
            this.groupBoxInstaller.Controls.Add(this.labelStatusStore);
            this.groupBoxInstaller.Location = new System.Drawing.Point(12, 292);
            this.groupBoxInstaller.Name = "groupBoxInstaller";
            this.groupBoxInstaller.Size = new System.Drawing.Size(569, 233);
            this.groupBoxInstaller.TabIndex = 26;
            this.groupBoxInstaller.TabStop = false;
            this.groupBoxInstaller.Text = "Installer";
            // 
            // checkBoxPackDLC
            // 
            this.checkBoxPackDLC.AutoSize = true;
            this.checkBoxPackDLC.Enabled = false;
            this.checkBoxPackDLC.Location = new System.Drawing.Point(166, 210);
            this.checkBoxPackDLC.Name = "checkBoxPackDLC";
            this.checkBoxPackDLC.Size = new System.Drawing.Size(15, 14);
            this.checkBoxPackDLC.TabIndex = 30;
            this.checkBoxPackDLC.UseVisualStyleBackColor = true;
            // 
            // checkBoxRepackZlib
            // 
            this.checkBoxRepackZlib.AutoSize = true;
            this.checkBoxRepackZlib.Enabled = false;
            this.checkBoxRepackZlib.Location = new System.Drawing.Point(166, 187);
            this.checkBoxRepackZlib.Name = "checkBoxRepackZlib";
            this.checkBoxRepackZlib.Size = new System.Drawing.Size(15, 14);
            this.checkBoxRepackZlib.TabIndex = 29;
            this.checkBoxRepackZlib.UseVisualStyleBackColor = true;
            // 
            // labelStatusPackDLC
            // 
            this.labelStatusPackDLC.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusPackDLC.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusPackDLC.Location = new System.Drawing.Point(187, 211);
            this.labelStatusPackDLC.Name = "labelStatusPackDLC";
            this.labelStatusPackDLC.Size = new System.Drawing.Size(252, 13);
            this.labelStatusPackDLC.TabIndex = 27;
            this.labelStatusPackDLC.Text = "label";
            // 
            // labelME3DLCPack
            // 
            this.labelME3DLCPack.AutoSize = true;
            this.labelME3DLCPack.Location = new System.Drawing.Point(13, 211);
            this.labelME3DLCPack.Name = "labelME3DLCPack";
            this.labelME3DLCPack.Size = new System.Drawing.Size(74, 13);
            this.labelME3DLCPack.TabIndex = 28;
            this.labelME3DLCPack.Text = "Repack DLCs";
            // 
            // labelStatusRepackZlib
            // 
            this.labelStatusRepackZlib.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStatusRepackZlib.ForeColor = System.Drawing.Color.LimeGreen;
            this.labelStatusRepackZlib.Location = new System.Drawing.Point(187, 188);
            this.labelStatusRepackZlib.Name = "labelStatusRepackZlib";
            this.labelStatusRepackZlib.Size = new System.Drawing.Size(252, 13);
            this.labelStatusRepackZlib.TabIndex = 25;
            this.labelStatusRepackZlib.Text = "label";
            // 
            // labelMERepackZlib
            // 
            this.labelMERepackZlib.AutoSize = true;
            this.labelMERepackZlib.Location = new System.Drawing.Point(13, 188);
            this.labelMERepackZlib.Name = "labelMERepackZlib";
            this.labelMERepackZlib.Size = new System.Drawing.Size(100, 13);
            this.labelMERepackZlib.TabIndex = 26;
            this.labelMERepackZlib.Text = "Repack Game Files";
            // 
            // buttonExit
            // 
            this.buttonExit.BackColor = System.Drawing.Color.DarkRed;
            this.buttonExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonExit.ForeColor = System.Drawing.Color.White;
            this.buttonExit.Location = new System.Drawing.Point(26, 535);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(75, 23);
            this.buttonExit.TabIndex = 3;
            this.buttonExit.Text = "EXIT";
            this.buttonExit.UseVisualStyleBackColor = false;
            this.buttonExit.Click += new System.EventHandler(this.buttonExit_Click);
            // 
            // buttonNormal
            // 
            this.buttonNormal.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.buttonNormal.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.buttonNormal.ForeColor = System.Drawing.Color.Black;
            this.buttonNormal.Location = new System.Drawing.Point(457, 535);
            this.buttonNormal.Name = "buttonNormal";
            this.buttonNormal.Size = new System.Drawing.Size(111, 23);
            this.buttonNormal.TabIndex = 4;
            this.buttonNormal.Text = "Standard Mode";
            this.buttonNormal.UseVisualStyleBackColor = false;
            this.buttonNormal.Click += new System.EventHandler(this.buttonNormal_Click);
            // 
            // labelFinalStatus
            // 
            this.labelFinalStatus.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.labelFinalStatus.Location = new System.Drawing.Point(176, 540);
            this.labelFinalStatus.Name = "labelFinalStatus";
            this.labelFinalStatus.Size = new System.Drawing.Size(275, 13);
            this.labelFinalStatus.TabIndex = 25;
            this.labelFinalStatus.Text = "label";
            // 
            // checkBoxPreEnableRepack
            // 
            this.checkBoxPreEnableRepack.AutoSize = true;
            this.checkBoxPreEnableRepack.Location = new System.Drawing.Point(18, 19);
            this.checkBoxPreEnableRepack.Name = "checkBoxPreEnableRepack";
            this.checkBoxPreEnableRepack.Size = new System.Drawing.Size(119, 17);
            this.checkBoxPreEnableRepack.TabIndex = 35;
            this.checkBoxPreEnableRepack.TabStop = false;
            this.checkBoxPreEnableRepack.Text = "Repack Game Files";
            this.checkBoxPreEnableRepack.UseVisualStyleBackColor = true;
            this.checkBoxPreEnableRepack.CheckedChanged += new System.EventHandler(this.checkBoxPreEnableRepack_CheckedChanged);
            // 
            // checkBoxPreEnablePack
            // 
            this.checkBoxPreEnablePack.AutoSize = true;
            this.checkBoxPreEnablePack.Location = new System.Drawing.Point(18, 42);
            this.checkBoxPreEnablePack.Name = "checkBoxPreEnablePack";
            this.checkBoxPreEnablePack.Size = new System.Drawing.Size(93, 17);
            this.checkBoxPreEnablePack.TabIndex = 37;
            this.checkBoxPreEnablePack.TabStop = false;
            this.checkBoxPreEnablePack.Text = "Repack DLCs";
            this.checkBoxPreEnablePack.UseVisualStyleBackColor = true;
            this.checkBoxPreEnablePack.CheckedChanged += new System.EventHandler(this.checkBoxPreEnablePack_CheckedChanged);
            // 
            // groupBoxOptions
            // 
            this.groupBoxOptions.Controls.Add(this.checkBoxOptionFaster);
            this.groupBoxOptions.Controls.Add(this.checkBoxOptionVanilla);
            this.groupBoxOptions.Controls.Add(this.checkBoxPreEnableRepack);
            this.groupBoxOptions.Controls.Add(this.checkBoxPreEnablePack);
            this.groupBoxOptions.Location = new System.Drawing.Point(12, 214);
            this.groupBoxOptions.Name = "groupBoxOptions";
            this.groupBoxOptions.Size = new System.Drawing.Size(569, 72);
            this.groupBoxOptions.TabIndex = 27;
            this.groupBoxOptions.TabStop = false;
            this.groupBoxOptions.Text = "Options";
            // 
            // checkBoxOptionVanilla
            // 
            this.checkBoxOptionVanilla.AutoSize = true;
            this.checkBoxOptionVanilla.Location = new System.Drawing.Point(167, 19);
            this.checkBoxOptionVanilla.Name = "checkBoxOptionVanilla";
            this.checkBoxOptionVanilla.Size = new System.Drawing.Size(113, 17);
            this.checkBoxOptionVanilla.TabIndex = 38;
            this.checkBoxOptionVanilla.TabStop = false;
            this.checkBoxOptionVanilla.Text = "Skip vanilla check";
            this.checkBoxOptionVanilla.UseVisualStyleBackColor = true;
            // 
            // checkBoxOptionFaster
            // 
            this.checkBoxOptionFaster.AutoSize = true;
            this.checkBoxOptionFaster.Location = new System.Drawing.Point(167, 42);
            this.checkBoxOptionFaster.Name = "checkBoxOptionFaster";
            this.checkBoxOptionFaster.Size = new System.Drawing.Size(283, 17);
            this.checkBoxOptionFaster.TabIndex = 39;
            this.checkBoxOptionFaster.TabStop = false;
            this.checkBoxOptionFaster.Text = "Faster method (8GB RAM required) - EXPERIMENTAL";
            this.checkBoxOptionFaster.UseVisualStyleBackColor = true;
            // 
            // Installer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(595, 570);
            this.Controls.Add(this.groupBoxOptions);
            this.Controls.Add(this.buttonNormal);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.labelFinalStatus);
            this.Controls.Add(this.groupBoxInstaller);
            this.Controls.Add(this.groupBoxPreInstallCheck);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Installer";
            this.Text = "MEM Installer for ALOT";
            this.groupBoxPreInstallCheck.ResumeLayout(false);
            this.groupBoxPreInstallCheck.PerformLayout();
            this.groupBoxInstaller.ResumeLayout(false);
            this.groupBoxInstaller.PerformLayout();
            this.groupBoxOptions.ResumeLayout(false);
            this.groupBoxOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.CheckBox checkBoxPreMods;
        private System.Windows.Forms.CheckBox checkBoxLOD;
        private System.Windows.Forms.Button buttonSTART;
        private System.Windows.Forms.CheckBox checkBoxScan;
        private System.Windows.Forms.CheckBox checkBoxMipMaps;
        private System.Windows.Forms.CheckBox checkBoxTextures;
        private System.Windows.Forms.CheckBox checkBoxStore;
        private System.Windows.Forms.Label labelStatusLOD;
        private System.Windows.Forms.Label labelStatusScan;
        private System.Windows.Forms.Label labelStatusMipMaps;
        private System.Windows.Forms.Label labelStatusTextures;
        private System.Windows.Forms.Label labelStatusStore;
        private System.Windows.Forms.Label labelPreGamePath;
        private System.Windows.Forms.Label label0;
        private System.Windows.Forms.Label labelStore;
        private System.Windows.Forms.Label labelTextures;
        private System.Windows.Forms.Label labelMipMaps;
        private System.Windows.Forms.Label labelScan;
        private System.Windows.Forms.Label labelLOD;
        private System.Windows.Forms.Label labelPrepare;
        private System.Windows.Forms.Label labelStatusPrepare;
        private System.Windows.Forms.CheckBox checkBoxPrepare;
        private System.Windows.Forms.GroupBox groupBoxPreInstallCheck;
        private System.Windows.Forms.GroupBox groupBoxInstaller;
        private System.Windows.Forms.Button buttonPreInstallCheck;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button buttonNormal;
        private System.Windows.Forms.Label labelFinalStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonPreChangePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxPreEnableRepack;
        private System.Windows.Forms.Label labelPrePath;
        private System.Windows.Forms.CheckBox checkBoxPreEnablePack;
        private System.Windows.Forms.Label labelPreVanilla;
        private System.Windows.Forms.Label labelPreSpace;
        private System.Windows.Forms.Label labelPreAccess;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelPreMods;
        private System.Windows.Forms.Label labelStatusRepackZlib;
        private System.Windows.Forms.Label labelMERepackZlib;
        private System.Windows.Forms.Label labelStatusPackDLC;
        private System.Windows.Forms.Label labelME3DLCPack;
        private System.Windows.Forms.CheckBox checkBoxRepackZlib;
        private System.Windows.Forms.CheckBox checkBoxPackDLC;
        private System.Windows.Forms.CheckBox checkBoxPrePath;
        private System.Windows.Forms.CheckBox checkBoxPreAccess;
        private System.Windows.Forms.CheckBox checkBoxPreVanilla;
        private System.Windows.Forms.CheckBox checkBoxPreSpace;
        private System.Windows.Forms.GroupBox groupBoxOptions;
        private System.Windows.Forms.CheckBox checkBoxOptionVanilla;
        private System.Windows.Forms.CheckBox checkBoxOptionFaster;
    }
}