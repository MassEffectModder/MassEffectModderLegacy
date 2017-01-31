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

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class MainWindow : Form
    {
        const uint ExportModTag = 0x444F4D45;
        const uint ExportModVersion = 1;
        const uint ExportModHeaderLength = 16;

        public ConfIni _configIni;

        public MainWindow(bool runAsAdmin)
        {
            InitializeComponent();
            Text = "Mass Effect Modder v1.66";
            if (runAsAdmin)
                Text += " (run as Administrator)";
            _configIni = new ConfIni();
        }

        public void enableGameDataMenu(bool enable)
        {
            helpToolStripMenuItem.Enabled = enable;
            toolStripMenuME1.Enabled = enable;
            toolStripMenuME2.Enabled = enable;
            toolStripMenuME3.Enabled = enable;
            toolStripExtractME1MEMMenuItem.Enabled = enable;
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
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME1_TYPE, engineConf);
            MessageBox.Show("Game configuration file at " + path + " updated." +
                "\n\nYou will have to remove empty mipmaps if you haven't already done.");
            enableGameDataMenu(true);
        }

        public bool GetPackages(GameData gameData)
        {
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                return false;
            }
            updateStatusLabel("Finding packages in game setup...");
            if (!gameData.getPackages())
            {
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

        struct BinaryMod
        {
            public string packagePath;
            public int exportId;
            public byte[] data;
        };

        private string convertDataModtoMem()
        {
            string errors = "";

            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                return "";
            }

            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select ME3Explorer .mod (binary)";
                modFile.Filter = "MOD file | *.mod";
                if (modFile.ShowDialog() != DialogResult.OK)
                    return "";

                updateStatusLabel("Processing mod: " + modFile.FileName);
                int numEntries = 0;
                List<BinaryMod> mods = new List<BinaryMod>();
                using (FileStream fs = new FileStream(modFile.FileName, FileMode.Open))
                {
                    string package = "";
                    int len = fs.ReadInt32();
                    fs.ReadStringASCII(len); // version
                    numEntries = fs.ReadInt32();
                    for (int i = 0; i < numEntries; i++)
                    {
                        BinaryMod mod = new BinaryMod();
                        try
                        {
                            len = fs.ReadInt32();
                            string desc = fs.ReadStringASCII(len); // description
                            if (!desc.Contains("Binary Replacement for file"))
                                throw new Exception();
                            len = fs.ReadInt32();
                            string scriptLegacy = fs.ReadStringASCII(len);
                            string path = "";
                            Misc.ParseLegacyME3ScriptMod(scriptLegacy, ref package, ref mod.exportId, ref path);
                            if (mod.exportId == -1 || package == "" || path == "")
                                throw new Exception();
                            len = fs.ReadInt32();
                            mod.data = fs.ReadToBuffer(len);
                            mod.packagePath = GameData.RelativeGameData(Path.Combine(path, package));
                            mods.Add(mod);
                        }
                        catch
                        {
                            MessageBox.Show("This mod is not compatible!");
                            return "";
                        }
                    }
                }

                using (SaveFileDialog memFile = new SaveFileDialog())
                {
                    memFile.Title = "Select a MEM .mem file, or create a new one";
                    memFile.Filter = "MEM file | *.mem";
                    if (memFile.ShowDialog() != DialogResult.OK)
                        return "";

                    List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();
                    FileStream outFs;
                    if (File.Exists(memFile.FileName))
                    {
                        outFs = new FileStream(memFile.FileName, FileMode.Open, FileAccess.ReadWrite);
                        uint tag = outFs.ReadUInt32();
                        uint version = outFs.ReadUInt32();
                        if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                        {
                            if (version != TexExplorer.TextureModVersion)
                                errors += "File " + memFile.FileName + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                            else
                                errors += "File " + memFile.FileName + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                            return errors;
                        }
                        else
                        {
                            uint gameType = 0;
                            outFs.JumpTo(outFs.ReadInt64());
                            gameType = outFs.ReadUInt32();
                            if ((MeType)gameType != GameData.gameType)
                            {
                                errors += "File " + memFile.FileName + " is not a MEM mod valid for this game" + Environment.NewLine;
                                return errors;
                            }
                        }
                        int numFiles = outFs.ReadInt32();
                        for (int l = 0; l < numFiles; l++)
                        {
                            MipMaps.FileMod fileMod = new MipMaps.FileMod();
                            fileMod.tag = outFs.ReadUInt32();
                            fileMod.name = outFs.ReadStringASCIINull();
                            fileMod.offset = outFs.ReadInt64();
                            fileMod.size = outFs.ReadInt64();
                            modFiles.Add(fileMod);
                        }
                        outFs.SeekEnd();
                    }
                    else
                    {
                        outFs = new FileStream(memFile.FileName, FileMode.Create, FileAccess.Write);
                        outFs.WriteUInt32(TexExplorer.TextureModTag);
                        outFs.WriteUInt32(TexExplorer.TextureModVersion);
                        outFs.WriteInt64(0); // filled later
                    }

                    for (int l = 0; l < mods.Count; l++)
                    {
                        Stream dst = MipMaps.compressData(mods[l].data);
                        dst.SeekBegin();

                        MipMaps.FileMod fileMod = new MipMaps.FileMod();
                        fileMod.tag = MipMaps.FileBinaryTag;
                        fileMod.name = Path.GetFileNameWithoutExtension(mods[l].packagePath) + "_" + mods[l].exportId + ".bin";
                        fileMod.offset = outFs.Position;
                        fileMod.size = dst.Length;
                        modFiles.Add(fileMod);

                        outFs.WriteInt32(mods[l].exportId);
                        outFs.WriteStringASCIINull(mods[l].packagePath);
                        outFs.WriteFromStream(dst, dst.Length);
                    }

                    long pos = outFs.Position;
                    outFs.SeekBegin();
                    outFs.WriteUInt32(TexExplorer.TextureModTag);
                    outFs.WriteUInt32(TexExplorer.TextureModVersion);
                    outFs.WriteInt64(pos);
                    outFs.JumpTo(pos);
                    outFs.WriteUInt32((uint)GameData.gameType);
                    outFs.WriteInt32(modFiles.Count);
                    for (int i = 0; i < modFiles.Count; i++)
                    {
                        outFs.WriteUInt32(modFiles[i].tag);
                        outFs.WriteStringASCIINull(modFiles[i].name);
                        outFs.WriteInt64(modFiles[i].offset);
                        outFs.WriteInt64(modFiles[i].size);
                    }
                    outFs.Close();
                }
                updateStatusLabel("Mod converted");
            }
            return errors;
        }

        public void repackME12(MeType gametype)
        {
            GameData gameData = new GameData(gametype, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                return;
            }
            GetPackages(gameData);
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                updateStatusLabel("Repack PCC file " + (i + 1) + " of " + GameData.packageFiles.Count);
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
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME2_TYPE, engineConf);
            MessageBox.Show("Game configuration file at " + path + " updated." +
                "\n\nYou will have to remove empty mipmaps if you haven't already done.");
            enableGameDataMenu(true);
        }

        private void extractME3DLCPackagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                enableGameDataMenu(true);
                return;
            }
            if (!Directory.Exists(GameData.DLCData))
            {
                MessageBox.Show("No DLCs need to be extracted.");
                enableGameDataMenu(true);
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
                MessageBox.Show("Game path is wrong!");
                return;
            }
            string outPath = Path.Combine(Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp"), DLCname, "CookedPCConsole", "Default.sfar");
            ME3DLC dlc = new ME3DLC(this);
            dlc.fullRePack(inPath, outPath, DLCname, this, null);
        }

        private void PackAllME3DLC()
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                return;
            }
            if (!Directory.Exists(GameData.DLCData))
            {
                MessageBox.Show("No DLCs need to be compressed.");
                return;
            }
            List<string> dlcs = Directory.GetFiles(GameData.DLCData, "Mount.dlc", SearchOption.AllDirectories).ToList();
            if (dlcs.Count() == 0)
            {
                MessageBox.Show("No DLCs need to be compressed.");
                return;
            }
            List<string> DLCs = Directory.GetDirectories(GameData.DLCData).ToList();
            for (int i = 0; i < DLCs.Count; i++)
            {
                List<string> files = Directory.GetFiles(DLCs[i], "Mount.dlc", SearchOption.AllDirectories).ToList();
                if (files.Count == 0)
                    DLCs.RemoveAt(i--);
            }
            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;
            for (int i = 0; i < DLCs.Count; i++)
            {
                diskUsage += Misc.getDirectorySize(DLCs[i]);
            }
            diskUsage = (long)(diskUsage / 1.5);
            if (diskUsage < diskFreeSpace)
            {
                for (int i = 0; i < DLCs.Count; i++)
                {
                    string DLCname = Path.GetFileName(DLCs[i]);
                    updateStatusLabel("DLC compressing - " + (i + 1) + " of " + DLCs.Count);
                    PackME3DLC(DLCs[i], DLCname);
                }

                string tmpDlcDir = Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp");
                DLCs = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                for (int i = 0; i < DLCs.Count; i++)
                {
                    if (new FileInfo(DLCs[i]).Length > 32)
                    {
                        string source = Path.GetDirectoryName(Path.GetDirectoryName(DLCs[i]));
                        Directory.Move(source, tmpDlcDir + "\\" + Path.GetFileName(source));
                    }
                }

                Directory.Delete(GameData.DLCData, true);
                Directory.Move(tmpDlcDir, GameData.DLCData);
                updateStatusLabel("Done");
            }
            else
            {
                MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
            }
            updateStatusLabel2("");
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
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD(MeType.ME3_TYPE, engineConf);
            MessageBox.Show("Game configuration file at " + path + " updated." +
                "\n\nYou will have to remove empty mipmaps if you haven't already done.");
            enableGameDataMenu(true);
        }

        private void modME3ExportDataToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            convertDataModtoMem();
            enableGameDataMenu(true);
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

        private void removeLODSetME1MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(MeType.ME1_TYPE, engineConf);
                MessageBox.Show("INFO: Game configuration file at " + path + " updated.");
            }
            else
            {
                MessageBox.Show("INFO: Game configuration file at " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void removeLODSetME2MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(MeType.ME2_TYPE, engineConf);
                MessageBox.Show("INFO: Game configuration file at " + path + " updated.");
            }
            else
            {
                MessageBox.Show("INFO: Game configuration file at " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void removeLODSetME3MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(MeType.ME3_TYPE, engineConf);
                MessageBox.Show("INFO: Game configuration file at " + path + " updated.");
            }
            else
            {
                MessageBox.Show("INFO: Game configuration: " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void toolStripMenuItemUpdateTOCs_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (Directory.Exists(GameData.GamePath))
            {
                CachePackageMgr.updateMainTOC();
                CachePackageMgr.updateDLCsTOC();
                MessageBox.Show("TOC files updated.");
            }
            else
            {
                MessageBox.Show("Game path is wrong!");
            }

            enableGameDataMenu(true);
        }

        void removeTreeFile(MeType game)
        {
            enableGameDataMenu(false);
            DialogResult result = MessageBox.Show("WARNING: you are going to delete your current textures scan file." +
            "\n\nAfter that, and before scanning your game again, you need to restore game to vanilla state and reinstall vanilla DLCs and DLC mods." +
            "\n\nAre you sure you want to proceed?", "Remove textures map of the game.", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                GameData gameData = new GameData(game, _configIni);
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name);
                string filename = Path.Combine(path, "me" + (int)GameData.gameType + "map.bin");
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    MessageBox.Show("File at " + filename + " deleted.");
                }
                else
                {
                    MessageBox.Show("INFO: File at " + filename + " not found.");
                }
            }
            enableGameDataMenu(true);
        }

        private void toolStripME1RemoveTreeMenuItem_Click(object sender, EventArgs e)
        {
            removeTreeFile(MeType.ME1_TYPE);
        }

        private void toolStripME2RemoveTreeMenuItem_Click(object sender, EventArgs e)
        {
            removeTreeFile(MeType.ME2_TYPE);
        }

        private void toolStripME3RemoveTreeMenuItem_Click(object sender, EventArgs e)
        {
            removeTreeFile(MeType.ME3_TYPE);
        }

        private void wikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/MassEffectModder/MassEffectModder/wiki");
        }

        private void githubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/MassEffectModder/MassEffectModder/");
        }

        private void releasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/MassEffectModder/MassEffectModder/releases");
        }

        private void reportIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/MassEffectModder/MassEffectModder/issues");
        }

        void checkGameFiles(MeType gameType)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(gameType, _configIni);
            if (Directory.Exists(GameData.GamePath))
            {
                string filename = "errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                string errors = Misc.checkGameFiles(gameType, this);
                updateStatusLabel("");
                if (errors != "")
                {
                    using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                    {
                        fs.WriteStringASCII("=========================================================" + Environment.NewLine);
                        fs.WriteStringASCII("WARNING: looks like the following file(s) are not vanilla" + Environment.NewLine);
                        fs.WriteStringASCII("=========================================================" + Environment.NewLine + Environment.NewLine);
                        fs.WriteStringASCII(errors);
                    }
                    MessageBox.Show("Finished checking game files.\n\nWARNING: Some errors have occured!");
                    Process.Start(filename);
                }
                else
                {
                    MessageBox.Show("Finished checking game files.");
                }
            }
            else
            {
                MessageBox.Show("Game path is wrong!");
            }
            enableGameDataMenu(true);
        }

        private void toolStripCheckFilesME1MenuItem_Click(object sender, EventArgs e)
        {
            checkGameFiles(MeType.ME1_TYPE);
        }

        private void toolStripCheckFilesME2MenuItem_Click(object sender, EventArgs e)
        {
            checkGameFiles(MeType.ME2_TYPE);
        }

        private void toolStripCheckFilesME3MenuItem_Click(object sender, EventArgs e)
        {
            checkGameFiles(MeType.ME3_TYPE);
        }

        private void toolStripUpdateGfxME1MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME1_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateGFXSettings(MeType.ME1_TYPE, engineConf);
            MessageBox.Show("Game configuration file at " + path + " updated.");
            enableGameDataMenu(true);
        }

        private void toolStripUpdateGfxME2MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateGFXSettings(MeType.ME2_TYPE, engineConf);
            MessageBox.Show("Game configuration file at " + path + " updated.");
            enableGameDataMenu(true);
        }

        private void toolStripUpdateGfxME3MenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateGFXSettings(MeType.ME3_TYPE, engineConf);
            MessageBox.Show("Game configuration file at " + path + " updated.");
            enableGameDataMenu(true);
        }

        private void toolStripExtractMEMMenuItem()
        {
            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select Mod file";
                modFile.Filter = "MEM mod file | *.mem";
                modFile.Multiselect = true;
                if (modFile.ShowDialog() != DialogResult.OK)
                    return;

                using (FolderBrowserDialog modDir = new FolderBrowserDialog())
                {
                    if (modDir.ShowDialog() != DialogResult.OK)
                        return;

                    enableGameDataMenu(false);

                    string errors = "";
                    string[] files = modFile.FileNames;
                    foreach (string file in files)
                    {
                        long diskFreeSpace = Misc.getDiskFreeSpace(modDir.SelectedPath);
                        long diskUsage = 0;
                        foreach (string item in files)
                        {
                            diskUsage += new FileInfo(item).Length;
                        }
                        diskUsage = (long)(diskUsage * 2.5);
                        if (diskUsage < diskFreeSpace)
                        {
                            Misc.startTimer();
                            foreach (string item in files)
                            {
                                string outDir = Path.Combine(modDir.SelectedPath, Path.GetFileNameWithoutExtension(item));
                                Directory.CreateDirectory(outDir);
                                updateStatusLabel("MOD: " + item + "extracting...");
                                updateStatusLabel2("");
                                errors += new MipMaps().extractTextureMod(item, outDir, null, null, null);
                            }
                            var time = Misc.stopTimer();
                            updateStatusLabel("MODs extracted. Process total time: " + Misc.getTimerFormat(time));
                            updateStatusLabel2("");
                            if (errors != "")
                            {
                                MessageBox.Show("WARNING: Some errors have occured!");
                            }
                        }
                        else
                        {
                            MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                        }
                    }
                }
            }
            enableGameDataMenu(false);
        }

        private void toolStripExtractME1MEMMenuItem_Click(object sender, EventArgs e)
        {
            toolStripExtractMEMMenuItem();
        }

        private void toolStripExtractME2MEMMenuItem_Click(object sender, EventArgs e)
        {
            toolStripExtractMEMMenuItem();
        }

        private void toolStripExtractME3MEMMenuItem_Click(object sender, EventArgs e)
        {
            toolStripExtractMEMMenuItem();
        }
    }
}
