/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
    partial class TexExplorer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TexExplorer));
            this.contextMenuStripTextures = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.replaceTextureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceTextureToolStripMenuItemConvert = new System.Windows.Forms.ToolStripMenuItem();
            this.extractToDDSFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractToPNGFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.previewTextureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.infoTextureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.info2TextureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listViewMods = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStripMods = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.applyModToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteModToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractModsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.applyModsWithVerificationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listViewResults = new System.Windows.Forms.ListView();
            this.columnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.treeViewPackages = new System.Windows.Forms.TreeView();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.listViewTextures = new System.Windows.Forms.ListView();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.richTextBoxInfo = new System.Windows.Forms.RichTextBox();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.MODsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadMODsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearMODsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packMODToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packMODToolStripMenuItemConvert = new System.Windows.Forms.ToolStripMenuItem();
            this.createMODBatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.convertME3ExplorermodForMEMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.batchConvertME3ExplorermodForMEMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.convertME3ExplorermodForMEMToolStripMenuItemConvert = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripTextures.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStripMods.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStripTextures
            // 
            this.contextMenuStripTextures.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.replaceTextureToolStripMenuItem,
            this.replaceTextureToolStripMenuItemConvert,
            this.extractToDDSFileToolStripMenuItem,
            this.extractToPNGFileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.contextMenuStripTextures.Name = "contextMenuStripTextures";
            this.contextMenuStripTextures.Size = new System.Drawing.Size(244, 114);
            // 
            // replaceTextureToolStripMenuItem
            // 
            this.replaceTextureToolStripMenuItem.Name = "replaceTextureToolStripMenuItem";
            this.replaceTextureToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            this.replaceTextureToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.replaceTextureToolStripMenuItem.Text = "Replace Texture";
            this.replaceTextureToolStripMenuItem.Click += new System.EventHandler(this.replaceTextureToolStripMenuItem_Click);
            // 
            // replaceTextureToolStripMenuItemConvert
            // 
            this.replaceTextureToolStripMenuItemConvert.Name = "replaceTextureToolStripMenuItemConvert";
            this.replaceTextureToolStripMenuItemConvert.Size = new System.Drawing.Size(243, 22);
            this.replaceTextureToolStripMenuItemConvert.Text = "Replace Texture (Convert mode)";
            this.replaceTextureToolStripMenuItemConvert.Click += new System.EventHandler(this.replaceTextureToolStripMenuItemConvert_Click);
            // 
            // extractToDDSFileToolStripMenuItem
            // 
            this.extractToDDSFileToolStripMenuItem.Name = "extractToDDSFileToolStripMenuItem";
            this.extractToDDSFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.extractToDDSFileToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.extractToDDSFileToolStripMenuItem.Text = "Extract to DDS file";
            this.extractToDDSFileToolStripMenuItem.Click += new System.EventHandler(this.extractToDDSFileToolStripMenuItem_Click);
            // 
            // extractToPNGFileToolStripMenuItem
            // 
            this.extractToPNGFileToolStripMenuItem.Name = "extractToPNGFileToolStripMenuItem";
            this.extractToPNGFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.extractToPNGFileToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.extractToPNGFileToolStripMenuItem.Text = "Extract to PNG file";
            this.extractToPNGFileToolStripMenuItem.Click += new System.EventHandler(this.extractToPNGFileToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.previewTextureToolStripMenuItem,
            this.infoTextureToolStripMenuItem,
            this.info2TextureToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // previewTextureToolStripMenuItem
            // 
            this.previewTextureToolStripMenuItem.Name = "previewTextureToolStripMenuItem";
            this.previewTextureToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
            this.previewTextureToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.previewTextureToolStripMenuItem.Text = "Preview";
            this.previewTextureToolStripMenuItem.Click += new System.EventHandler(this.previewToolStripMenuItem_Click);
            // 
            // infoTextureToolStripMenuItem
            // 
            this.infoTextureToolStripMenuItem.Name = "infoTextureToolStripMenuItem";
            this.infoTextureToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.infoTextureToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.infoTextureToolStripMenuItem.Text = "Info";
            this.infoTextureToolStripMenuItem.Click += new System.EventHandler(this.infoToolStripMenuItem_Click);
            // 
            // info2TextureToolStripMenuItem
            // 
            this.info2TextureToolStripMenuItem.Name = "info2TextureToolStripMenuItem";
            this.info2TextureToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.info2TextureToolStripMenuItem.Text = "Info (all)";
            this.info2TextureToolStripMenuItem.Click += new System.EventHandler(this.info2TextureToolStripMenuItem_Click);
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "Folder.png");
            this.imageList.Images.SetKeyName(1, "Folder Open.png");
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listViewMods);
            this.splitContainer1.Panel1.Controls.Add(this.listViewResults);
            this.splitContainer1.Panel1.Controls.Add(this.treeViewPackages);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1140, 462);
            this.splitContainer1.SplitterDistance = 338;
            this.splitContainer1.TabIndex = 4;
            // 
            // listViewMods
            // 
            this.listViewMods.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewMods.BackColor = System.Drawing.Color.White;
            this.listViewMods.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listViewMods.ContextMenuStrip = this.contextMenuStripMods;
            this.listViewMods.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.listViewMods.Location = new System.Drawing.Point(0, 28);
            this.listViewMods.Name = "listViewMods";
            this.listViewMods.ShowGroups = false;
            this.listViewMods.Size = new System.Drawing.Size(338, 431);
            this.listViewMods.TabIndex = 2;
            this.listViewMods.UseCompatibleStateImageBehavior = false;
            this.listViewMods.View = System.Windows.Forms.View.List;
            this.listViewMods.SelectedIndexChanged += new System.EventHandler(this.listViewMods_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 500;
            // 
            // contextMenuStripMods
            // 
            this.contextMenuStripMods.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.applyModToolStripMenuItem,
            this.deleteModToolStripMenuItem,
            this.extractModsToolStripMenuItem,
            this.toolStripSeparator2,
            this.applyModsWithVerificationToolStripMenuItem});
            this.contextMenuStripMods.Name = "contextMenuStripTextures";
            this.contextMenuStripMods.Size = new System.Drawing.Size(161, 98);
            // 
            // applyModToolStripMenuItem
            // 
            this.applyModToolStripMenuItem.Name = "applyModToolStripMenuItem";
            this.applyModToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.applyModToolStripMenuItem.Text = "Apply Mods";
            this.applyModToolStripMenuItem.Click += new System.EventHandler(this.applyModToolStripMenuItem_Click);
            // 
            // deleteModToolStripMenuItem
            // 
            this.deleteModToolStripMenuItem.Name = "deleteModToolStripMenuItem";
            this.deleteModToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.deleteModToolStripMenuItem.Text = "Clear Mods";
            this.deleteModToolStripMenuItem.Click += new System.EventHandler(this.deleteModToolStripMenuItem_Click);
            // 
            // extractModsToolStripMenuItem
            // 
            this.extractModsToolStripMenuItem.Name = "extractModsToolStripMenuItem";
            this.extractModsToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.extractModsToolStripMenuItem.Text = "Extract Mods";
            this.extractModsToolStripMenuItem.Click += new System.EventHandler(this.extractModsToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(157, 6);
            // 
            // applyModsWithVerificationToolStripMenuItem
            // 
            this.applyModsWithVerificationToolStripMenuItem.Name = "applyModsWithVerificationToolStripMenuItem";
            this.applyModsWithVerificationToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.applyModsWithVerificationToolStripMenuItem.Text = "Apply and Verify";
            this.applyModsWithVerificationToolStripMenuItem.Click += new System.EventHandler(this.applyModsWithVerificationToolStripMenuItem_Click);
            // 
            // listViewResults
            // 
            this.listViewResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewResults.BackColor = System.Drawing.Color.White;
            this.listViewResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader});
            this.listViewResults.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.listViewResults.Location = new System.Drawing.Point(0, 28);
            this.listViewResults.MultiSelect = false;
            this.listViewResults.Name = "listViewResults";
            this.listViewResults.ShowGroups = false;
            this.listViewResults.Size = new System.Drawing.Size(338, 431);
            this.listViewResults.TabIndex = 1;
            this.listViewResults.UseCompatibleStateImageBehavior = false;
            this.listViewResults.View = System.Windows.Forms.View.List;
            this.listViewResults.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listViewResults_MouseDoubleClick);
            // 
            // columnHeader
            // 
            this.columnHeader.Width = 500;
            // 
            // treeViewPackages
            // 
            this.treeViewPackages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewPackages.ImageKey = "Folder.png";
            this.treeViewPackages.ImageList = this.imageList;
            this.treeViewPackages.Location = new System.Drawing.Point(3, 28);
            this.treeViewPackages.Name = "treeViewPackages";
            this.treeViewPackages.SelectedImageIndex = 1;
            this.treeViewPackages.Size = new System.Drawing.Size(336, 431);
            this.treeViewPackages.TabIndex = 0;
            this.treeViewPackages.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.treeViewPackages_AfterCollapse);
            this.treeViewPackages.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.treeViewPackages_AfterExpand);
            this.treeViewPackages.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewPackages_AfterSelect);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.listViewTextures);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.pictureBoxPreview);
            this.splitContainer2.Panel2.Controls.Add(this.richTextBoxInfo);
            this.splitContainer2.Size = new System.Drawing.Size(798, 462);
            this.splitContainer2.SplitterDistance = 334;
            this.splitContainer2.TabIndex = 0;
            // 
            // listViewTextures
            // 
            this.listViewTextures.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewTextures.BackColor = System.Drawing.Color.White;
            this.listViewTextures.ContextMenuStrip = this.contextMenuStripTextures;
            this.listViewTextures.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.listViewTextures.Location = new System.Drawing.Point(3, 28);
            this.listViewTextures.MultiSelect = false;
            this.listViewTextures.Name = "listViewTextures";
            this.listViewTextures.Size = new System.Drawing.Size(328, 431);
            this.listViewTextures.TabIndex = 0;
            this.listViewTextures.UseCompatibleStateImageBehavior = false;
            this.listViewTextures.View = System.Windows.Forms.View.List;
            this.listViewTextures.SelectedIndexChanged += new System.EventHandler(this.listViewTextures_SelectedIndexChanged);
            this.listViewTextures.DoubleClick += new System.EventHandler(this.listViewTextures_DoubleClick);
            this.listViewTextures.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.listViewTextures_KeyPress);
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxPreview.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.pictureBoxPreview.Location = new System.Drawing.Point(3, 30);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(457, 429);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPreview.TabIndex = 1;
            this.pictureBoxPreview.TabStop = false;
            // 
            // richTextBoxInfo
            // 
            this.richTextBoxInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxInfo.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxInfo.Location = new System.Drawing.Point(3, 28);
            this.richTextBoxInfo.Name = "richTextBoxInfo";
            this.richTextBoxInfo.Size = new System.Drawing.Size(457, 432);
            this.richTextBoxInfo.TabIndex = 0;
            this.richTextBoxInfo.Text = "";
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MODsToolStripMenuItem,
            this.searchToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1140, 24);
            this.menuStrip.TabIndex = 5;
            this.menuStrip.Text = "menuStrip1";
            // 
            // MODsToolStripMenuItem
            // 
            this.MODsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadMODsToolStripMenuItem,
            this.clearMODsToolStripMenuItem,
            this.packMODToolStripMenuItem,
            this.packMODToolStripMenuItemConvert,
            this.createMODBatchToolStripMenuItem,
            this.toolStripSeparator1,
            this.convertME3ExplorermodForMEMToolStripMenuItem,
            this.convertME3ExplorermodForMEMToolStripMenuItemConvert,
            this.batchConvertME3ExplorermodForMEMToolStripMenuItem});
            this.MODsToolStripMenuItem.Name = "MODsToolStripMenuItem";
            this.MODsToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            this.MODsToolStripMenuItem.Text = "MODs";
            // 
            // loadMODsToolStripMenuItem
            // 
            this.loadMODsToolStripMenuItem.Name = "loadMODsToolStripMenuItem";
            this.loadMODsToolStripMenuItem.Size = new System.Drawing.Size(303, 22);
            this.loadMODsToolStripMenuItem.Text = "Load MODs";
            this.loadMODsToolStripMenuItem.Click += new System.EventHandler(this.loadMODsToolStripMenuItem_Click);
            // 
            // clearMODsToolStripMenuItem
            // 
            this.clearMODsToolStripMenuItem.Name = "clearMODsToolStripMenuItem";
            this.clearMODsToolStripMenuItem.Size = new System.Drawing.Size(303, 22);
            this.clearMODsToolStripMenuItem.Text = "Clear MODs";
            this.clearMODsToolStripMenuItem.Click += new System.EventHandler(this.clearMODsToolStripMenuItem_Click);
            // 
            // packMODToolStripMenuItem
            // 
            this.packMODToolStripMenuItem.Name = "packMODToolStripMenuItem";
            this.packMODToolStripMenuItem.Size = new System.Drawing.Size(303, 22);
            this.packMODToolStripMenuItem.Text = "Create MOD";
            this.packMODToolStripMenuItem.Click += new System.EventHandler(this.createMODToolStripMenuItem_Click);
            // 
            // packMODToolStripMenuItemConvert
            // 
            this.packMODToolStripMenuItemConvert.Name = "packMODToolStripMenuItemConvert";
            this.packMODToolStripMenuItemConvert.Size = new System.Drawing.Size(303, 22);
            this.packMODToolStripMenuItemConvert.Text = "Create MOD (Convert mode)";
            this.packMODToolStripMenuItemConvert.Click += new System.EventHandler(this.packMODToolStripMenuItemConvert_Click);
            // 
            // createMODBatchToolStripMenuItem
            // 
            this.createMODBatchToolStripMenuItem.Name = "createMODBatchToolStripMenuItem";
            this.createMODBatchToolStripMenuItem.Size = new System.Drawing.Size(303, 22);
            this.createMODBatchToolStripMenuItem.Text = "Create multiple MODs";
            this.createMODBatchToolStripMenuItem.Click += new System.EventHandler(this.createMODBatchToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(300, 6);
            // 
            // convertME3ExplorermodForMEMToolStripMenuItem
            // 
            this.convertME3ExplorermodForMEMToolStripMenuItem.Name = "convertME3ExplorermodForMEMToolStripMenuItem";
            this.convertME3ExplorermodForMEMToolStripMenuItem.Size = new System.Drawing.Size(303, 22);
            this.convertME3ExplorermodForMEMToolStripMenuItem.Text = "Convert .mod .tpf to .mem";
            this.convertME3ExplorermodForMEMToolStripMenuItem.Click += new System.EventHandler(this.convertME3ExplorermodForMEMToolStripMenuItem_Click);
            // 
            // batchConvertME3ExplorermodForMEMToolStripMenuItem
            // 
            this.batchConvertME3ExplorermodForMEMToolStripMenuItem.Name = "batchConvertME3ExplorermodForMEMToolStripMenuItem";
            this.batchConvertME3ExplorermodForMEMToolStripMenuItem.Size = new System.Drawing.Size(303, 22);
            this.batchConvertME3ExplorermodForMEMToolStripMenuItem.Text = "Batch convert .mod .tpf to .mem";
            this.batchConvertME3ExplorermodForMEMToolStripMenuItem.Click += new System.EventHandler(this.batchConvertME3ExplorermodForMEMToolStripMenuItem_Click);
            // 
            // searchToolStripMenuItem
            // 
            this.searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            this.searchToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.searchToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.searchToolStripMenuItem.Text = "Search";
            this.searchToolStripMenuItem.Click += new System.EventHandler(this.searchToolStripMenuItem_Click);
            // 
            // convertME3ExplorermodForMEMToolStripMenuItemConvert
            // 
            this.convertME3ExplorermodForMEMToolStripMenuItemConvert.Name = "convertME3ExplorermodForMEMToolStripMenuItemConvert";
            this.convertME3ExplorermodForMEMToolStripMenuItemConvert.Size = new System.Drawing.Size(303, 22);
            this.convertME3ExplorermodForMEMToolStripMenuItemConvert.Text = "Convert .mod .tpf to .mem (Convert mode)";
            this.convertME3ExplorermodForMEMToolStripMenuItemConvert.Click += new System.EventHandler(this.convertME3ExplorermodForMEMToolStripMenuItemConvert_Click);
            // 
            // TexExplorer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1140, 462);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.splitContainer1);
            this.MinimumSize = new System.Drawing.Size(1024, 500);
            this.Name = "TexExplorer";
            this.ShowIcon = false;
            this.Text = "Texture Manager ";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TexExplorer_FormClosed);
            this.contextMenuStripTextures.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStripMods.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTextures;
        private System.Windows.Forms.ToolStripMenuItem replaceTextureToolStripMenuItem;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.MenuStrip menuStrip;
        public System.Windows.Forms.ListView listViewTextures;
        private System.Windows.Forms.RichTextBox richTextBoxInfo;
        public System.Windows.Forms.PictureBox pictureBoxPreview;
        public System.Windows.Forms.TreeView treeViewPackages;
        private System.Windows.Forms.ListView listViewResults;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem previewTextureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem infoTextureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader;
        private System.Windows.Forms.ToolStripMenuItem MODsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadMODsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearMODsToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripMods;
        private System.Windows.Forms.ToolStripMenuItem applyModToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteModToolStripMenuItem;
        private System.Windows.Forms.ListView listViewMods;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ToolStripMenuItem extractModsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem packMODToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem createMODBatchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractToDDSFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractToPNGFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem info2TextureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem convertME3ExplorermodForMEMToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem batchConvertME3ExplorermodForMEMToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem applyModsWithVerificationToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem replaceTextureToolStripMenuItemConvert;
        private System.Windows.Forms.ToolStripMenuItem packMODToolStripMenuItemConvert;
        private System.Windows.Forms.ToolStripMenuItem convertME3ExplorermodForMEMToolStripMenuItemConvert;
    }
}