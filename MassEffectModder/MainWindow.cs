/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2019 Pawel Kolodziejski
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
using System.Collections;
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
            Text = "Mass Effect Modder v" + Application.ProductVersion;
            if (runAsAdmin)
                Text += " (run as Administrator)";
            _configIni = new ConfIni();
            if (runAsAdmin)
                MessageBox.Show("The program should be run as standard user to avoid user account issues.\n" +
                    "The program will ask for administrative rights when necessary.");
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

        public bool GetPackages(GameData gameData)
        {
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                return false;
            }
            updateStatusLabel("Finding packages in game setup...");
            if (!gameData.getPackages(true))
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

        private void repackME2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            repackME23(MeType.ME2_TYPE);
            enableGameDataMenu(true);
        }

        public void repackME23(MeType gameId)
        {
            string errors = "";
            GameData gameData = new GameData(gameId, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                return;
            }
            GetPackages(gameData);
            string path = "";
            if (gameId == MeType.ME2_TYPE)
                path = @"\BioGame\CookedPC\BIOC_Materials.pcc".ToLowerInvariant();
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                if (path != "" && GameData.packageFiles[i].ToLowerInvariant().Contains(path))
                    continue;
                updateStatusLabel("Repack PCC file " + (i + 1) + " of " + GameData.packageFiles.Count);
                try
                {
                    Package package = new Package(GameData.GamePath + GameData.packageFiles[i], true);
                    if (!package.compressed || package.compressed && package.compressionType != Package.CompressionType.Zlib)
                    {
                        package.Dispose();
                        package = new Package(GameData.GamePath + GameData.packageFiles[i]);
                        package.SaveToFile(true, false, false);
                    }
                    package.Dispose();
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Problem with PCC file header:"))
                        continue;
                    errors += "The file is propably broken, skipped: " + GameData.packageFiles[i] + Environment.NewLine;
                }
            }
            if (gameId == MeType.ME3_TYPE)
                TOCBinFile.UpdateAllTOCBinFiles();
            if (errors != "")
            {
                string filename = "pcc-errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                MessageBox.Show("WARNING: Some errors have occured!");
                Process.Start(filename);
            }
            updateStatusLabel("Done");
            updateStatusLabel2("");
        }

        private void repackME3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            repackME23(MeType.ME3_TYPE);
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

            Misc.startTimer();
            ME3DLC.unpackAllDLC(this, null, false);
            var time = Misc.stopTimer();
            updateStatusLabel("DLCs extracted. Process total time: " + Misc.getTimerFormat(time));
            updateStatusLabel2("");
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

        private void removeLODSettings(MeType gameId)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(gameId, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (exist)
            {
                ConfIni engineConf = new ConfIni(path);
                LODSettings.removeLOD(gameId, engineConf);
                MessageBox.Show("INFO: Game configuration file at " + path + " updated.");
            }
            else
            {
                MessageBox.Show("INFO: Game configuration file at " + path + " not exist, nothing done.");
            }
            enableGameDataMenu(true);
        }

        private void removeLODSetME1MenuItem_Click(object sender, EventArgs e)
        {
            removeLODSettings(MeType.ME1_TYPE);
        }

        private void removeLODSetME2MenuItem_Click(object sender, EventArgs e)
        {
            removeLODSettings(MeType.ME2_TYPE);
        }

        private void removeLODSetME3MenuItem_Click(object sender, EventArgs e)
        {
            removeLODSettings(MeType.ME3_TYPE);
        }

        private void toolStripMenuItemUpdateTOCs_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (Directory.Exists(GameData.GamePath))
            {
                TOCBinFile.UpdateAllTOCBinFiles();
                MessageBox.Show("TOC files updated.");
            }
            else
            {
                MessageBox.Show("Game path is wrong!");
            }

            enableGameDataMenu(true);
        }

        void updateMEConfig(MeType gameId, bool limit2k)
        {
            enableGameDataMenu(false);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filename = Path.Combine(path, "me" + (int)gameId + "map.bin");
            if (!File.Exists(filename))
            {
                MessageBox.Show("Unable to update LOD settings.\nYou must scan your game using Texture Manager first always!");
                enableGameDataMenu(true);
                return;
            }
            GameData gameData = new GameData(gameId, _configIni);
            path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
            {
                if (gameId == MeType.ME1_TYPE)
                {
                    MessageBox.Show("Missing game configuration file.\nYou need atleast once launch the game first.");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            ConfIni engineConf = new ConfIni(path);
            LODSettings.removeLOD(gameId, engineConf);
            LODSettings.updateLOD(gameId, engineConf, limit2k);
            MessageBox.Show("Game configuration file at " + path + " updated.");
            enableGameDataMenu(true);
        }

        private void updateLODSetME1MenuItem_Click(object sender, EventArgs e)
        {
            updateMEConfig(MeType.ME1_TYPE, false);
        }

        private void updateLODSet2KME1MenuItem_Click(object sender, EventArgs e)
        {
            updateMEConfig(MeType.ME1_TYPE, true);
        }

        private void updateLODSetME2MenuItem_Click(object sender, EventArgs e)
        {
            updateMEConfig(MeType.ME2_TYPE, false);
        }

        private void updateLODSetME3MenuItem_Click(object sender, EventArgs e)
        {
            updateMEConfig(MeType.ME3_TYPE, false);
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

        void checkGameFiles(MeType gameType)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(gameType, _configIni);
            if (Directory.Exists(GameData.GamePath))
            {
                string filename = "errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                string errors = "";
                List<string> mods = new List<string>();
                bool vanilla = Misc.checkGameFiles(gameType, ref errors, ref mods, this, false);
                updateStatusLabel("");
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    if (mods.Count != 0)
                    {
                        fs.WriteStringASCII(Environment.NewLine + "------- Detected mods --------" + Environment.NewLine);
                        for (int l = 0; l < mods.Count; l++)
                        {
                            fs.WriteStringASCII(mods[l] + Environment.NewLine);
                        }
                        fs.WriteStringASCII("------------------------------" + Environment.NewLine + Environment.NewLine);
                    }

                    if (!vanilla)
                    {
                        fs.WriteStringASCII("===========================================================================" + Environment.NewLine);
                        fs.WriteStringASCII("WARNING: looks like the following file(s) are not vanilla or not recognized" + Environment.NewLine);
                        fs.WriteStringASCII("===========================================================================" + Environment.NewLine + Environment.NewLine);
                        fs.WriteStringASCII(errors);
                        MessageBox.Show("Finished checking game files.\n\nWARNING: Some errors have occured!");
                        Process.Start(filename);
                    }
                    else
                    {
                        MessageBox.Show("Finished checking game files.");
                    }
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

        private void updateGfxME(MeType gameId, bool softShadowsMode = false)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(gameId, _configIni);
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateGFXSettings(gameId, engineConf, false, false);
            MessageBox.Show("Game configuration file at " + path + " updated.");
            enableGameDataMenu(true);
        }

        private void toolStripUpdateGfxME1MenuItem_Click(object sender, EventArgs e)
        {
            updateGfxME(MeType.ME1_TYPE, false);
        }

        private void toolStripUpdateGfxME1MenuItemSoftMode_Click(object sender, EventArgs e)
        {
            updateGfxME(MeType.ME1_TYPE, true);
        }

        private void toolStripUpdateGfxME2MenuItem_Click(object sender, EventArgs e)
        {
            updateGfxME(MeType.ME2_TYPE);
        }

        private void toolStripUpdateGfxME3MenuItem_Click(object sender, EventArgs e)
        {
            updateGfxME(MeType.ME3_TYPE);
        }

        private void toolStripExtractMEMMenuItem(MeType gameType)
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
                    modDir.Description = "Please select destination directory for MEM extraction";
                    if (modDir.ShowDialog() != DialogResult.OK)
                        return;

                    enableGameDataMenu(false);
                    GameData.gameType = gameType;

                    string errors = "";
                    string log = "";
                    string[] files = modFile.FileNames;
                    long diskFreeSpace = Misc.getDiskFreeSpace(modDir.SelectedPath);
                    long diskUsage = 0;
                    foreach (string file in files)
                    {
                        diskUsage += new FileInfo(file).Length;
                    }
                    diskUsage = (long)(diskUsage * 2.5);
                    if (diskUsage >= diskFreeSpace)
                    {
                        MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                    }
                    else
                    {
                        Misc.startTimer();
                        foreach (string file in files)
                        {
                            string outDir = Path.Combine(modDir.SelectedPath, Path.GetFileNameWithoutExtension(file));
                            Directory.CreateDirectory(outDir);
                            updateStatusLabel("MOD: " + file + " - extracting...");
                            updateStatusLabel2("");
                            errors += new MipMaps().extractTextureMod(file, outDir, null, null, ref log);
                        }
                        var time = Misc.stopTimer();
                        updateStatusLabel("MODs extracted. Process total time: " + Misc.getTimerFormat(time));
                        updateStatusLabel2("");
                        if (errors != "")
                        {
                            MessageBox.Show("WARNING: Some errors have occured!");
                        }
                    }
                }
            }
            enableGameDataMenu(true);
        }

        private void toolStripExtractME1MEMMenuItem_Click(object sender, EventArgs e)
        {
            toolStripExtractMEMMenuItem(MeType.ME1_TYPE);
        }

        private void toolStripExtractME2MEMMenuItem_Click(object sender, EventArgs e)
        {
            toolStripExtractMEMMenuItem(MeType.ME2_TYPE);
        }

        private void toolStripExtractME3MEMMenuItem_Click(object sender, EventArgs e)
        {
            toolStripExtractMEMMenuItem(MeType.ME3_TYPE);
        }

        void toolStripCreateBinaryMod(MeType gameType)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(gameType, _configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                enableGameDataMenu(true);
                return;
            }
            updateStatusLabel("Finding packages in game setup...");
            gameData.getPackages(true);
            updateStatusLabel("");

            using (FolderBrowserDialog modDir = new FolderBrowserDialog())
            {
                modDir.Description = "Please select source directory of modded package files";
                if (modDir.ShowDialog() != DialogResult.OK)
                {
                    updateStatusLabel("");
                    enableGameDataMenu(true);
                    return;
                }

                List<string> exe = Directory.GetFiles(modDir.SelectedPath, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".exe",
                    StringComparison.OrdinalIgnoreCase)).ToList();
                if (exe.Count != 0)
                {
                    MessageBox.Show("The source directory doesn't seems right, aborting...");
                    updateStatusLabel("");
                    enableGameDataMenu(true);
                    return;
                }

                List< string> mods = Directory.GetFiles(modDir.SelectedPath, "*.*",
                    SearchOption.AllDirectories).Where(s =>
                    s.EndsWith(".upk", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();

                for (int i = 0; i < mods.Count; i++)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(mods[i], FileMode.Open, FileAccess.Read))
                        {
                            fs.SeekEnd();
                            fs.Seek(-Package.MEMendFileMarker.Length, SeekOrigin.Current);
                            string marker = fs.ReadStringASCII(Package.MEMendFileMarker.Length);
                            if (marker == Package.MEMendFileMarker)
                            {
                                MessageBox.Show("Mod files must be based on vanilla game data, aborting...");
                                updateStatusLabel("");
                                enableGameDataMenu(true);
                                return;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                updateStatusLabel("Scanning mods...");
                List<string> files = Directory.GetFiles(modDir.SelectedPath, "*.*",
                    SearchOption.AllDirectories).Where(s =>
                    s.EndsWith(".upk", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                List<BinaryMod> modFiles = new List<BinaryMod>();
                for (int i = 0; i < mods.Count; i++)
                {
                    Package vanillaPkg = null;
                    Package modPkg = null;
                    bool found = false;
                    try
                    {
                        for (int v = 0; v < GameData.packageFiles.Count; v++)
                        {
                            if (Path.GetFileName(mods[i]).ToLowerInvariant() == Path.GetFileName(GameData.packageFiles[v]).ToLowerInvariant())
                            {
                                modPkg = new Package(GameData.GamePath + mods[i]);
                                vanillaPkg = new Package(GameData.GamePath + GameData.packageFiles[v]);
                                if (modPkg.exportsTable.Count != vanillaPkg.exportsTable.Count ||
                                    modPkg.namesTable.Count != vanillaPkg.namesTable.Count ||
                                    modPkg.importsTable.Count != vanillaPkg.importsTable.Count)
                                {
                                    found = true;
                                    vanillaPkg.Dispose();
                                    vanillaPkg = null;
                                    continue;
                                }
                                found = true;
                                break;
                            }
                        }
                        if (found && vanillaPkg == null)
                        {
                            modPkg.Dispose();
                            MessageBox.Show("Package file not compatible: " + mods[i] + ", aborting...");
                            updateStatusLabel("");
                            enableGameDataMenu(true);
                            return;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Problem opening file: " + mods[i] + ", aborting...");
                        updateStatusLabel("");
                        enableGameDataMenu(true);
                        return;
                    }
                    if (!found)
                    {
                        MessageBox.Show("Package not present in vanilla game data: " + mods[i] + ", aborting...");
                        updateStatusLabel("");
                        enableGameDataMenu(true);
                        return;
                    }

                    for (int e = 0; e < modPkg.exportsTable.Count; e++)
                    {
                        byte[] vanillaExport = vanillaPkg.getExportData(e);
                        byte[] modExport = modPkg.getExportData(e);
                        if (vanillaExport.Length == modExport.Length)
                        {
                            if (StructuralComparisons.StructuralEqualityComparer.Equals(vanillaExport, modExport))
                                continue;
                        }

                        BinaryMod mod = new BinaryMod();
                        mod.packagePath = vanillaPkg.packagePath;
                        mod.exportId = e;

                        if (vanillaExport.Length == modExport.Length)
                        {
                            mod.data = new Xdelta3Helper.Xdelta3().Compress(vanillaExport, modExport);
                            mod.binaryModType = 2;
                        }
                        else
                        {
                            mod.data = new byte[modExport.Length];
                            Array.Copy(modExport, mod.data, modExport.Length);
                            mod.binaryModType = 1;
                        }

                        string name;
                        if (mod.packagePath.Contains("\\DLC\\"))
                        {
                            string dlcName = mod.packagePath.Split('\\')[3];
                            name = "D" + dlcName.Length + "-" + dlcName + "-";
                        }
                        else
                        {
                            name = "B";
                        }
                        name += Path.GetFileName(mod.packagePath).Length + "-" +
                            Path.GetFileName(mod.packagePath) + "-E" + mod.exportId;
                        if (mod.binaryModType == 1)
                            name += ".bin";
                        else if (mod.binaryModType == 2)
                            name += ".xdelta";

                        mod.textureName = name;
                        modFiles.Add(mod);
                    }
                    vanillaPkg.Dispose();
                    modPkg.Dispose();
                }

                if (modFiles.Count == 0)
                {
                    MessageBox.Show("Nothing to mod, exiting...");
                    updateStatusLabel("");
                    enableGameDataMenu(true);
                    return;
                }

                updateStatusLabel("Creating mem...");
                using (SaveFileDialog modFile = new SaveFileDialog())
                {
                    modFile.Title = "Please selecct new MEM mod file";
                    modFile.Filter = "MEM mod file | *.mem";
                    if (modFile.ShowDialog() != DialogResult.OK)
                    {
                        updateStatusLabel("");
                        enableGameDataMenu(true);
                        return;
                    }

                    if (File.Exists(modFile.FileName))
                        File.Delete(modFile.FileName);

                    using (FileStream outFs = new FileStream(modFile.FileName, FileMode.CreateNew, FileAccess.Write))
                    {
                        outFs.WriteUInt32(TreeScan.TextureModTag);
                        outFs.WriteUInt32(TreeScan.TextureModVersion);
                        outFs.WriteInt64(0); // filled later

                        for (int i = 0; i < modFiles.Count; i++)
                        {
                            Stream dst = MipMaps.compressData(modFiles[i].data);
                            dst.SeekBegin();
                            BinaryMod bmod = modFiles[i];
                            bmod.offset = outFs.Position;
                            bmod.size = dst.Length;
                            modFiles[i] = bmod;
                            outFs.WriteInt32(modFiles[i].exportId);
                            outFs.WriteStringASCIINull(modFiles[i].packagePath);
                            outFs.WriteFromStream(dst, dst.Length);
                        }

                        long pos = outFs.Position;
                        outFs.SeekBegin();
                        outFs.WriteUInt32(TreeScan.TextureModTag);
                        outFs.WriteUInt32(TreeScan.TextureModVersion);
                        outFs.WriteInt64(pos);
                        outFs.JumpTo(pos);
                        outFs.WriteUInt32((uint)gameType);
                        outFs.WriteInt32(modFiles.Count);

                        for (int i = 0; i < modFiles.Count; i++)
                        {
                            if (modFiles[i].binaryModType == 1)
                                outFs.WriteUInt32(MipMaps.FileBinaryTag);
                            else if (modFiles[i].binaryModType == 2)
                                outFs.WriteUInt32(MipMaps.FileXdeltaTag);
                            outFs.WriteStringASCIINull(modFiles[i].textureName);
                            outFs.WriteInt64(modFiles[i].offset);
                            outFs.WriteInt64(modFiles[i].size);
                        }
                    }
                }
            }
            updateStatusLabel("Finished");
            enableGameDataMenu(true);
        }

        private void toolStripMenuItemCreateBinME1_Click(object sender, EventArgs e)
        {
            toolStripCreateBinaryMod(MeType.ME1_TYPE);
        }

        private void toolStripMenuItemCreateBinME2_Click(object sender, EventArgs e)
        {
            toolStripCreateBinaryMod(MeType.ME2_TYPE);
        }

        private void toolStripMenuItemCreateBinME3_Click(object sender, EventArgs e)
        {
            toolStripCreateBinaryMod(MeType.ME3_TYPE);
        }
    }
}
