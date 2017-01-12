/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
            this.repackME1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateME1ConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeGamePathME1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modME1ExportDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuME2 = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repackME2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateME2ConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeGamePathME2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modME2ExportDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuME3 = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractME3DLCPackagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.packME3DLCPackagesLZMAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateME3ConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeGamePathME3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.modME3ExportDataToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.comparatorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusStrip2 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuGame.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.statusStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuGame
            // 
            this.menuGame.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuME1,
            this.toolStripMenuME2,
            this.toolStripMenuME3,
            this.comparatorToolStripMenuItem});
            this.menuGame.Location = new System.Drawing.Point(0, 0);
            this.menuGame.Name = "menuGame";
            this.menuGame.Size = new System.Drawing.Size(1177, 24);
            this.menuGame.TabIndex = 0;
            // 
            // toolStripMenuME1
            // 
            this.toolStripMenuME1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect1ToolStripMenuItem,
            this.repackME1ToolStripMenuItem,
            this.updateME1ConfigToolStripMenuItem,
            this.changeGamePathME1ToolStripMenuItem,
            this.modME1ExportDataToolStripMenuItem});
            this.toolStripMenuME1.Name = "toolStripMenuME1";
            this.toolStripMenuME1.Size = new System.Drawing.Size(88, 20);
            this.toolStripMenuME1.Text = "Mass Effect 1";
            // 
            // massEffect1ToolStripMenuItem
            // 
            this.massEffect1ToolStripMenuItem.Name = "massEffect1ToolStripMenuItem";
            this.massEffect1ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.massEffect1ToolStripMenuItem.Text = "Texture Explorer";
            this.massEffect1ToolStripMenuItem.Click += new System.EventHandler(this.massEffect1ToolStripMenuItem_Click);
            // 
            // repackME1ToolStripMenuItem
            // 
            this.repackME1ToolStripMenuItem.Name = "repackME1ToolStripMenuItem";
            this.repackME1ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.repackME1ToolStripMenuItem.Text = "Repack (Zlib)";
            this.repackME1ToolStripMenuItem.Click += new System.EventHandler(this.repackME1ToolStripMenuItem_Click);
            // 
            // updateME1ConfigToolStripMenuItem
            // 
            this.updateME1ConfigToolStripMenuItem.Name = "updateME1ConfigToolStripMenuItem";
            this.updateME1ConfigToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.updateME1ConfigToolStripMenuItem.Text = "Update LOD Settings";
            this.updateME1ConfigToolStripMenuItem.Click += new System.EventHandler(this.updateME1ConfigToolStripMenuItem_Click);
            // 
            // changeGamePathME1ToolStripMenuItem
            // 
            this.changeGamePathME1ToolStripMenuItem.Name = "changeGamePathME1ToolStripMenuItem";
            this.changeGamePathME1ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.changeGamePathME1ToolStripMenuItem.Text = "Change Game Path";
            this.changeGamePathME1ToolStripMenuItem.Click += new System.EventHandler(this.changeGamePathME1ToolStripMenuItem_Click);
            // 
            // modME1ExportDataToolStripMenuItem
            // 
            this.modME1ExportDataToolStripMenuItem.Name = "modME1ExportDataToolStripMenuItem";
            this.modME1ExportDataToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.modME1ExportDataToolStripMenuItem.Text = "Load Data Mod";
            this.modME1ExportDataToolStripMenuItem.Click += new System.EventHandler(this.modME1ExportDataToolStripMenuItem_Click);
            // 
            // toolStripMenuME2
            // 
            this.toolStripMenuME2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect2ToolStripMenuItem,
            this.repackME2ToolStripMenuItem,
            this.updateME2ConfigToolStripMenuItem,
            this.changeGamePathME2ToolStripMenuItem,
            this.modME2ExportDataToolStripMenuItem});
            this.toolStripMenuME2.Name = "toolStripMenuME2";
            this.toolStripMenuME2.Size = new System.Drawing.Size(88, 20);
            this.toolStripMenuME2.Text = "Mass Effect 2";
            // 
            // massEffect2ToolStripMenuItem
            // 
            this.massEffect2ToolStripMenuItem.Name = "massEffect2ToolStripMenuItem";
            this.massEffect2ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.massEffect2ToolStripMenuItem.Text = "Texture Explorer";
            this.massEffect2ToolStripMenuItem.Click += new System.EventHandler(this.massEffect2ToolStripMenuItem_Click);
            // 
            // repackME2ToolStripMenuItem
            // 
            this.repackME2ToolStripMenuItem.Name = "repackME2ToolStripMenuItem";
            this.repackME2ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.repackME2ToolStripMenuItem.Text = "Repack (Zlib)";
            this.repackME2ToolStripMenuItem.Click += new System.EventHandler(this.repackME2ToolStripMenuItem_Click);
            // 
            // updateME2ConfigToolStripMenuItem
            // 
            this.updateME2ConfigToolStripMenuItem.Name = "updateME2ConfigToolStripMenuItem";
            this.updateME2ConfigToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.updateME2ConfigToolStripMenuItem.Text = "Update LOD Settings";
            this.updateME2ConfigToolStripMenuItem.Click += new System.EventHandler(this.updateME2ConfigToolStripMenuItem_Click);
            // 
            // changeGamePathME2ToolStripMenuItem
            // 
            this.changeGamePathME2ToolStripMenuItem.Name = "changeGamePathME2ToolStripMenuItem";
            this.changeGamePathME2ToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.changeGamePathME2ToolStripMenuItem.Text = "Change Game Path";
            this.changeGamePathME2ToolStripMenuItem.Click += new System.EventHandler(this.changeGamePathME2ToolStripMenuItem_Click);
            // 
            // modME2ExportDataToolStripMenuItem
            // 
            this.modME2ExportDataToolStripMenuItem.Name = "modME2ExportDataToolStripMenuItem";
            this.modME2ExportDataToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.modME2ExportDataToolStripMenuItem.Text = "Load Data Mod";
            this.modME2ExportDataToolStripMenuItem.Click += new System.EventHandler(this.modME2ExportDataToolStripMenuItem_Click);
            // 
            // toolStripMenuME3
            // 
            this.toolStripMenuME3.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect3ToolStripMenuItem,
            this.extractME3DLCPackagesToolStripMenuItem,
            this.packME3DLCPackagesLZMAToolStripMenuItem,
            this.updateME3ConfigToolStripMenuItem,
            this.changeGamePathME3ToolStripMenuItem,
            this.modME3ExportDataToolStripMenuItem1});
            this.toolStripMenuME3.Name = "toolStripMenuME3";
            this.toolStripMenuME3.Size = new System.Drawing.Size(88, 20);
            this.toolStripMenuME3.Text = "Mass Effect 3";
            // 
            // massEffect3ToolStripMenuItem
            // 
            this.massEffect3ToolStripMenuItem.Name = "massEffect3ToolStripMenuItem";
            this.massEffect3ToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.massEffect3ToolStripMenuItem.Text = "Texture Explorer";
            this.massEffect3ToolStripMenuItem.Click += new System.EventHandler(this.massEffect3ToolStripMenuItem_Click);
            // 
            // extractME3DLCPackagesToolStripMenuItem
            // 
            this.extractME3DLCPackagesToolStripMenuItem.Name = "extractME3DLCPackagesToolStripMenuItem";
            this.extractME3DLCPackagesToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.extractME3DLCPackagesToolStripMenuItem.Text = "Unpack DLC Archives";
            this.extractME3DLCPackagesToolStripMenuItem.Click += new System.EventHandler(this.extractME3DLCPackagesToolStripMenuItem_Click);
            // 
            // packME3DLCPackagesLZMAToolStripMenuItem
            // 
            this.packME3DLCPackagesLZMAToolStripMenuItem.Name = "packME3DLCPackagesLZMAToolStripMenuItem";
            this.packME3DLCPackagesLZMAToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.packME3DLCPackagesLZMAToolStripMenuItem.Text = "Pack DLC Archives";
            this.packME3DLCPackagesLZMAToolStripMenuItem.Click += new System.EventHandler(this.packME3DLCPackagesLZMAToolStripMenuItem_Click);
            // 
            // updateME3ConfigToolStripMenuItem
            // 
            this.updateME3ConfigToolStripMenuItem.Name = "updateME3ConfigToolStripMenuItem";
            this.updateME3ConfigToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.updateME3ConfigToolStripMenuItem.Text = "Update LOD Settings";
            this.updateME3ConfigToolStripMenuItem.Click += new System.EventHandler(this.updateME3ConfigToolStripMenuItem_Click);
            // 
            // changeGamePathME3ToolStripMenuItem
            // 
            this.changeGamePathME3ToolStripMenuItem.Name = "changeGamePathME3ToolStripMenuItem";
            this.changeGamePathME3ToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
            this.changeGamePathME3ToolStripMenuItem.Text = "Change Game Path";
            this.changeGamePathME3ToolStripMenuItem.Click += new System.EventHandler(this.changeGamePathME3ToolStripMenuItem_Click);
            // 
            // modME3ExportDataToolStripMenuItem1
            // 
            this.modME3ExportDataToolStripMenuItem1.Name = "modME3ExportDataToolStripMenuItem1";
            this.modME3ExportDataToolStripMenuItem1.Size = new System.Drawing.Size(187, 22);
            this.modME3ExportDataToolStripMenuItem1.Text = "Load Data Mod";
            this.modME3ExportDataToolStripMenuItem1.Click += new System.EventHandler(this.modME3ExportDataToolStripMenuItem1_Click);
            // 
            // comparatorToolStripMenuItem
            // 
            this.comparatorToolStripMenuItem.Name = "comparatorToolStripMenuItem";
            this.comparatorToolStripMenuItem.Size = new System.Drawing.Size(83, 20);
            this.comparatorToolStripMenuItem.Text = "Comparator";
            this.comparatorToolStripMenuItem.Click += new System.EventHandler(this.comparatorToolStripMenuItem_Click);
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 489);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1177, 22);
            this.statusStrip.TabIndex = 1;
            // 
            // statusStrip2
            // 
            this.statusStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel2});
            this.statusStrip2.Location = new System.Drawing.Point(0, 467);
            this.statusStrip2.Name = "statusStrip2";
            this.statusStrip2.Size = new System.Drawing.Size(1177, 22);
            this.statusStrip2.SizingGrip = false;
            this.statusStrip2.TabIndex = 3;
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1177, 511);
            this.Controls.Add(this.statusStrip2);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuGame);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuGame;
            this.MinimumSize = new System.Drawing.Size(1080, 550);
            this.Name = "MainWindow";
            this.Text = "MassEffectModder";
            this.menuGame.ResumeLayout(false);
            this.menuGame.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.statusStrip2.ResumeLayout(false);
            this.statusStrip2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuGame;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuME1;
        private System.Windows.Forms.ToolStripMenuItem massEffect1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuME2;
        private System.Windows.Forms.ToolStripMenuItem massEffect2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuME3;
        private System.Windows.Forms.ToolStripMenuItem massEffect3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateME1ConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem repackME2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extractME3DLCPackagesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem packME3DLCPackagesLZMAToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateME2ConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateME3ConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.StatusStrip statusStrip2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripMenuItem modME2ExportDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modME1ExportDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem modME3ExportDataToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem comparatorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeGamePathME1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeGamePathME2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem changeGamePathME3ToolStripMenuItem;
    }
}

