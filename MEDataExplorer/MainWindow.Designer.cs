/*
 * MEDataExplorer
 *
 * Copyright (C) 2014 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

namespace MEDataExplorer
{
    partial class MainWindow
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
            this.menuGame = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuME1 = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripMenuME2 = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuME3 = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateME1ConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repackME1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repackME2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateME2DLCCacheToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repackME3MainDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repackME3DLCDatauncompressedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repackME3DLCDataZlibToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractME3DLCPackagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packME3DLCPackagesUncompressedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packME3DLCPackagesLZMAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuGame.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuGame
            // 
            this.menuGame.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuME1,
            this.toolStripMenuME2,
            this.toolStripMenuME3});
            this.menuGame.Location = new System.Drawing.Point(0, 0);
            this.menuGame.Name = "menuGame";
            this.menuGame.Size = new System.Drawing.Size(796, 24);
            this.menuGame.TabIndex = 0;
            // 
            // toolStripMenuME1
            // 
            this.toolStripMenuME1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect1ToolStripMenuItem,
            this.repackME1ToolStripMenuItem,
            this.updateME1ConfigToolStripMenuItem});
            this.toolStripMenuME1.Name = "toolStripMenuME1";
            this.toolStripMenuME1.Size = new System.Drawing.Size(88, 20);
            this.toolStripMenuME1.Text = "Mass Effect 1";
            // 
            // massEffect1ToolStripMenuItem
            // 
            this.massEffect1ToolStripMenuItem.Name = "massEffect1ToolStripMenuItem";
            this.massEffect1ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.massEffect1ToolStripMenuItem.Text = "Texture Explorer";
            this.massEffect1ToolStripMenuItem.Click += new System.EventHandler(this.massEffect1ToolStripMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 271);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(796, 22);
            this.statusStrip.TabIndex = 1;
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStripMenuME2
            // 
            this.toolStripMenuME2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect2ToolStripMenuItem,
            this.repackME2ToolStripMenuItem,
            this.updateME2DLCCacheToolStripMenuItem});
            this.toolStripMenuME2.Name = "toolStripMenuME2";
            this.toolStripMenuME2.Size = new System.Drawing.Size(88, 20);
            this.toolStripMenuME2.Text = "Mass Effect 2";
            // 
            // massEffect2ToolStripMenuItem
            // 
            this.massEffect2ToolStripMenuItem.Name = "massEffect2ToolStripMenuItem";
            this.massEffect2ToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.massEffect2ToolStripMenuItem.Text = "Texture Explorer";
            this.massEffect2ToolStripMenuItem.Click += new System.EventHandler(this.massEffect2ToolStripMenuItem_Click);
            // 
            // toolStripMenuME3
            // 
            this.toolStripMenuME3.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect3ToolStripMenuItem,
            this.repackME3MainDataToolStripMenuItem,
            this.repackME3DLCDatauncompressedToolStripMenuItem,
            this.repackME3DLCDataZlibToolStripMenuItem,
            this.extractME3DLCPackagesToolStripMenuItem,
            this.packME3DLCPackagesUncompressedToolStripMenuItem,
            this.packME3DLCPackagesLZMAToolStripMenuItem});
            this.toolStripMenuME3.Name = "toolStripMenuME3";
            this.toolStripMenuME3.Size = new System.Drawing.Size(88, 20);
            this.toolStripMenuME3.Text = "Mass Effect 3";
            // 
            // massEffect3ToolStripMenuItem
            // 
            this.massEffect3ToolStripMenuItem.Name = "massEffect3ToolStripMenuItem";
            this.massEffect3ToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.massEffect3ToolStripMenuItem.Text = "Texture Explorer";
            this.massEffect3ToolStripMenuItem.Click += new System.EventHandler(this.massEffect3ToolStripMenuItem_Click);
            // 
            // updateME1ConfigToolStripMenuItem
            // 
            this.updateME1ConfigToolStripMenuItem.Name = "updateME1ConfigToolStripMenuItem";
            this.updateME1ConfigToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.updateME1ConfigToolStripMenuItem.Text = "Update Config";
            this.updateME1ConfigToolStripMenuItem.Click += new System.EventHandler(this.updateME1ConfigToolStripMenuItem_Click);
            // 
            // repackME1ToolStripMenuItem
            // 
            this.repackME1ToolStripMenuItem.Name = "repackME1ToolStripMenuItem";
            this.repackME1ToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
            this.repackME1ToolStripMenuItem.Text = "Repack (Zlib)";
            this.repackME1ToolStripMenuItem.Click += new System.EventHandler(this.repackME1ToolStripMenuItem_Click);
            // 
            // repackME2ToolStripMenuItem
            // 
            this.repackME2ToolStripMenuItem.Name = "repackME2ToolStripMenuItem";
            this.repackME2ToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.repackME2ToolStripMenuItem.Text = "Repack (Zlib)";
            this.repackME2ToolStripMenuItem.Click += new System.EventHandler(this.repackME2ToolStripMenuItem_Click);
            // 
            // updateME2DLCCacheToolStripMenuItem
            // 
            this.updateME2DLCCacheToolStripMenuItem.Name = "updateME2DLCCacheToolStripMenuItem";
            this.updateME2DLCCacheToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.updateME2DLCCacheToolStripMenuItem.Text = "Update DLC Cache";
            this.updateME2DLCCacheToolStripMenuItem.Click += new System.EventHandler(this.updateME2DLCCacheToolStripMenuItem_Click);
            // 
            // repackME3MainDataToolStripMenuItem
            // 
            this.repackME3MainDataToolStripMenuItem.Name = "repackME3MainDataToolStripMenuItem";
            this.repackME3MainDataToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.repackME3MainDataToolStripMenuItem.Text = "Repack Main Data";
            this.repackME3MainDataToolStripMenuItem.Click += new System.EventHandler(this.repackME3MainDataToolStripMenuItem_Click);
            // 
            // repackME3DLCDatauncompressedToolStripMenuItem
            // 
            this.repackME3DLCDatauncompressedToolStripMenuItem.Name = "repackME3DLCDatauncompressedToolStripMenuItem";
            this.repackME3DLCDatauncompressedToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.repackME3DLCDatauncompressedToolStripMenuItem.Text = "Repack DLC Data (uncompressed)";
            this.repackME3DLCDatauncompressedToolStripMenuItem.Click += new System.EventHandler(this.repackME3DLCDatauncompressedToolStripMenuItem_Click);
            // 
            // repackME3DLCDataZlibToolStripMenuItem
            // 
            this.repackME3DLCDataZlibToolStripMenuItem.Name = "repackME3DLCDataZlibToolStripMenuItem";
            this.repackME3DLCDataZlibToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.repackME3DLCDataZlibToolStripMenuItem.Text = "Repack DLC Data (Zlib)";
            this.repackME3DLCDataZlibToolStripMenuItem.Click += new System.EventHandler(this.repackME3DLCDataZlibToolStripMenuItem_Click);
            // 
            // extractME3DLCPackagesToolStripMenuItem
            // 
            this.extractME3DLCPackagesToolStripMenuItem.Name = "extractME3DLCPackagesToolStripMenuItem";
            this.extractME3DLCPackagesToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.extractME3DLCPackagesToolStripMenuItem.Text = "Extract DLC Packages";
            this.extractME3DLCPackagesToolStripMenuItem.Click += new System.EventHandler(this.extractME3DLCPackagesToolStripMenuItem_Click);
            // 
            // packME3DLCPackagesUncompressedToolStripMenuItem
            // 
            this.packME3DLCPackagesUncompressedToolStripMenuItem.Name = "packME3DLCPackagesUncompressedToolStripMenuItem";
            this.packME3DLCPackagesUncompressedToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.packME3DLCPackagesUncompressedToolStripMenuItem.Text = "Pack DLC Packages (Uncompressed)";
            this.packME3DLCPackagesUncompressedToolStripMenuItem.Click += new System.EventHandler(this.packME3DLCPackagesUncompressedToolStripMenuItem_Click);
            // 
            // packME3DLCPackagesLZMAToolStripMenuItem
            // 
            this.packME3DLCPackagesLZMAToolStripMenuItem.Name = "packME3DLCPackagesLZMAToolStripMenuItem";
            this.packME3DLCPackagesLZMAToolStripMenuItem.Size = new System.Drawing.Size(266, 22);
            this.packME3DLCPackagesLZMAToolStripMenuItem.Text = "Pack DLC Packages (LZMA)";
            this.packME3DLCPackagesLZMAToolStripMenuItem.Click += new System.EventHandler(this.packME3DLCPackagesLZMAToolStripMenuItem_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(796, 293);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuGame);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuGame;
            this.Name = "MainWindow";
            this.Text = "MEDataExplorer";
            this.menuGame.ResumeLayout(false);
            this.menuGame.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuGame;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuME1;
        private System.Windows.Forms.ToolStripMenuItem massEffect1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuME2;
        private System.Windows.Forms.ToolStripMenuItem massEffect2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuME3;
        private System.Windows.Forms.ToolStripMenuItem massEffect3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateME1ConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateME2DLCCacheToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME3MainDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME3DLCDatauncompressedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME3DLCDataZlibToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractME3DLCPackagesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem packME3DLCPackagesUncompressedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem packME3DLCPackagesLZMAToolStripMenuItem;
    }
}

