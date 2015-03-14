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
            this.toolStripMenuGame = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.massEffect3ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuGame.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuGame
            // 
            this.menuGame.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuGame});
            this.menuGame.Location = new System.Drawing.Point(0, 0);
            this.menuGame.Name = "menuGame";
            this.menuGame.Size = new System.Drawing.Size(796, 24);
            this.menuGame.TabIndex = 0;
            // 
            // toolStripMenuGame
            // 
            this.toolStripMenuGame.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.massEffect1ToolStripMenuItem,
            this.massEffect2ToolStripMenuItem,
            this.massEffect3ToolStripMenuItem});
            this.toolStripMenuGame.Name = "toolStripMenuGame";
            this.toolStripMenuGame.Size = new System.Drawing.Size(137, 20);
            this.toolStripMenuGame.Text = "Game Texture Explorer";
            // 
            // massEffect1ToolStripMenuItem
            // 
            this.massEffect1ToolStripMenuItem.Name = "massEffect1ToolStripMenuItem";
            this.massEffect1ToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.massEffect1ToolStripMenuItem.Text = "Mass Effect 1";
            this.massEffect1ToolStripMenuItem.Click += new System.EventHandler(this.massEffect1ToolStripMenuItem_Click);
            // 
            // massEffect2ToolStripMenuItem
            // 
            this.massEffect2ToolStripMenuItem.Name = "massEffect2ToolStripMenuItem";
            this.massEffect2ToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.massEffect2ToolStripMenuItem.Text = "Mass Effect 2";
            this.massEffect2ToolStripMenuItem.Click += new System.EventHandler(this.massEffect2ToolStripMenuItem_Click);
            // 
            // massEffect3ToolStripMenuItem
            // 
            this.massEffect3ToolStripMenuItem.Name = "massEffect3ToolStripMenuItem";
            this.massEffect3ToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.massEffect3ToolStripMenuItem.Text = "Mass Effect 3";
            this.massEffect3ToolStripMenuItem.Click += new System.EventHandler(this.massEffect3ToolStripMenuItem_Click);
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
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuGame;
        private System.Windows.Forms.ToolStripMenuItem massEffect1ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem massEffect2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem massEffect3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
    }
}

