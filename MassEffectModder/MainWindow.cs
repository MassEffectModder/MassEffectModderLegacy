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

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class MainWindow : Form
    {
        const uint ExportModTag = 0x444F4D45;
        const uint ExportModVersion = 1;
        const uint ExportModHeaderLength = 16;

        public ConfIni _configIni;

        public MainWindow()
        {
            InitializeComponent();
            _configIni = new ConfIni();
        }

        public void enableGameDataMenu(bool enable)
        {
            toolStripMenuME1.Enabled = enable;
            toolStripMenuME2.Enabled = enable;
            toolStripMenuME3.Enabled = enable;
            comparatorToolStripMenuItem.Enabled = enable;
        }

        public void updateStatusLabel(string text)
        {
            toolStripStatusLabel.Text = text;
            Application.DoEvents();
        }

        public void updateStatusLabel2(string text)
        {
            toolStripStatusLabel2.Text = text;
            Application.DoEvents();
        }

        public TexExplorer CreateTextureExplorer(MeType type)
        {
            TexExplorer explorer = new TexExplorer(this, type);
            explorer.Text = "Mass Effect " + (int)type;
            explorer.MdiParent = this;
            explorer.WindowState = FormWindowState.Maximized;
            explorer.Show();
            return explorer;
        }

        private void massEffect1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            CreateTextureExplorer(MeType.ME1_TYPE).Run();
        }

        private void massEffect2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            CreateTextureExplorer(MeType.ME2_TYPE).Run();
        }

        private void massEffect3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            CreateTextureExplorer(MeType.ME3_TYPE).Run();
        }

        private void updateME1ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME1_TYPE, engineConf);
            enableGameDataMenu(true);
        }

        public bool GetPackages(GameData gameData)
        {
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return false;
            }
            updateStatusLabel("Finding packages in game data...");
            if (!gameData.getPackages())
            {
                MessageBox.Show("Unable get packages from game data.");
                updateStatusLabel("");
                return false;
            }
            if (GameData.gameType != MeType.ME1_TYPE)
            {
                if (!gameData.getTfcTextures())
                {
                    updateStatusLabel("");
                    return false;
                }
            }
            updateStatusLabel("Done.");
            return true;
        }

        private void replaceExportDataMod(MeType gameType)
        {
            GameData gameData = new GameData(gameType, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select Mod file";
                modFile.Filter = "MOD file | *.mem";
                if (modFile.ShowDialog() != DialogResult.OK)
                    return;
                updateStatusLabel("Processing mod: " + modFile.FileName);
                using (FileStream fs = new FileStream(modFile.FileName, FileMode.Open))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag == TexExplorer.TextureModTag)
                    {
                        MessageBox.Show("This is textures Mod!");
                        return;
                    }
                    if (tag != ExportModTag || version != ExportModVersion)
                    {
                        MessageBox.Show("Mod not compatible!");
                        return;
                    }
                    else
                    {
                        if ((MeType)fs.ReadUInt32() != gameType)
                        {
                            MessageBox.Show("Mod for different game!");
                            return;
                        }
                        int numEntries = fs.ReadInt32();
                        for (int i = 0; i < numEntries; i++)
                        {
                            string package = fs.ReadStringASCIINull();
                            int expId = fs.ReadInt32();
                            uint uncSize = fs.ReadUInt32();
                            uint compSize = fs.ReadUInt32();
                            byte[] src = fs.ReadToBuffer(compSize);
                            byte[] dst = new byte[uncSize];
                            ZlibHelper.Zlib.Decompress(src, (uint)src.Length, dst);
                            string[] packages = Directory.GetFiles(GameData.MainData, package, SearchOption.AllDirectories);
                            if (packages.Count() != 0)
                            {
                                Package pkg = new Package(packages[0]);
                                pkg.setExportData(expId, dst);
                                pkg.SaveToFile();
                            }
                        }
                    }
                }
                updateStatusLabel("Mod applied");
            }
        }

        public void repackME12(MeType gametype)
        {
            GameData gameData = new GameData(gametype, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            GetPackages(gameData);
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                updateStatusLabel("Repack file " + (i + 1) + " of " + GameData.packageFiles.Count);
                Package package = new Package(GameData.packageFiles[i]);
                if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                    package.SaveToFile(true);
            }
            updateStatusLabel("Done");
            updateStatusLabel2("");
        }

        private void repackME1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            repackME12(MeType.ME1_TYPE);
            enableGameDataMenu(true);
        }

        private void modME1ExportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME1_TYPE);
            enableGameDataMenu(true);
        }

        private void repackME2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            repackME12(MeType.ME2_TYPE);
            enableGameDataMenu(true);
        }

        private void updateME2ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME2_TYPE, engineConf);
            enableGameDataMenu(true);
        }

        private void modME2ExportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME2_TYPE);
            enableGameDataMenu(true);
        }

        private void extractME3DLCPackagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            ME3DLC.unpackAllDLC(this, null);
            updateStatusLabel("Done");
            updateStatusLabel2("");
            enableGameDataMenu(true);
        }

        private void PackME3DLC(string inPath, string DLCname)
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            string outPath = Path.Combine(Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp"), DLCname, "CookedPCConsole", "Default.sfar");
            ME3DLC dlc = new ME3DLC(this);
            dlc.fullRePack(inPath, outPath, DLCname);
        }

        private void PackAllME3DLC()
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Wrong game path!");
                return;
            }
            List<string> dlcs = Directory.GetFiles(GameData.DLCData, "Mount.dlc", SearchOption.AllDirectories).ToList();
            if (dlcs.Count() == 0)
            {
                MessageBox.Show("There is nothing to pack.");
                return;
            }
            List<string> DLCs = Directory.GetDirectories(GameData.DLCData).ToList();
            for (int i = 0; i < DLCs.Count; i++)
            {
                List<string> files = Directory.GetFiles(DLCs[i], "Mount.dlc", SearchOption.AllDirectories).ToList();
                if (files.Count == 0)
                    DLCs.RemoveAt(i--);
            }
            for (int i = 0; i < DLCs.Count; i++)
            {
                string DLCname = Path.GetFileName(DLCs[i]);
                updateStatusLabel("SFAR packing - DLC " + (i + 1) + " of " + DLCs.Count);
                PackME3DLC(DLCs[i], DLCname);
            }
            Directory.Delete(GameData.DLCData, true);
            Directory.Move(Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp"), GameData.DLCData);

            updateStatusLabel("Done");
            updateStatusLabel2("");
            enableGameDataMenu(true);
        }

        private void packME3DLCPackagesLZMAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            PackAllME3DLC();
            enableGameDataMenu(true);
        }

        private void updateME3ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME3_TYPE, engineConf);
            enableGameDataMenu(true);
        }

        private void modME3ExportDataToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME3_TYPE);
            enableGameDataMenu(true);
        }

        private void comparatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            Comparator comparator = new Comparator(this);
            comparator.Text = "Texture Comparator";
            comparator.MdiParent = this;
            comparator.WindowState = FormWindowState.Maximized;
            comparator.Show();
        }

        private void changeGamePathME1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni, true);
            enableGameDataMenu(true);
        }

        private void changeGamePathME2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni, true);
            enableGameDataMenu(true);
        }

        private void changeGamePathME3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni, true);
            enableGameDataMenu(true);
        }
    }
}
