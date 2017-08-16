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

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;

namespace MassEffectModder
{
    public partial class Installer : Form
    {
        const uint MEMI_TAG = 0x494D454D;
        public bool exitToModder;
        ConfIni configIni;
        ConfIni installerIni;
        int gameId;
        GameData gameData;
        public List<string> memFiles;
        CachePackageMgr cachePackageMgr;
        List<FoundTexture> textures;
        MipMaps mipMaps;
        TreeScan treeScan;
        string errors = "";
        string log = "";

        public Installer()
        {
            InitializeComponent();
            Text = "MEM Installer v" + Application.ProductVersion + " for ALOT";
            mipMaps = new MipMaps();
            treeScan = new TreeScan();
            cachePackageMgr = new CachePackageMgr(null, this);
        }

        public bool Run(bool runAsAdmin)
        {
            installerIni = new ConfIni(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "installer.ini"));
            string gameIdStr = installerIni.Read("GameId", "Main");
            if (gameIdStr.ToLowerInvariant() == "me1")
                gameId = 1;
            else if (gameIdStr.ToLowerInvariant() == "me2")
                gameId = 2;
            else if (gameIdStr.ToLowerInvariant() == "me3")
                gameId = 3;
            else
            {
                MessageBox.Show("Game ID not recognized in installer.ini, exiting...", "Installer");
                return false;
            }
            string baseModNameStr = installerIni.Read("BaseModName", "Main");
            if (baseModNameStr != "")
                Text = "MEM Installer v" + Application.ProductVersion + " for " + baseModNameStr;
            else
                Text += " ME" + gameId;
            if (runAsAdmin)
                Text += " (run as Administrator)";
            configIni = new ConfIni();

            labelStatusPrepare.Text = "";
            labelStatusScan.Text = "";
            labelStatusTextures.Text = "";
            labelStatusStore.Text = "";
            labelStatusMipMaps.Text = "";
            labelStatusLOD.Text = "";
            labelFinalStatus.Text = "Before beginning, press the CHECK button.";

            buttonsDefault(gameId);

            checkBoxOptionVanilla.Checked = false;
            checkBoxOptionFaster.Checked = false;
            buttonUnpackDLC.Enabled = false;

            clearPreCheckStatus();

            buttonSTART.Enabled = false;

            ulong memorySize = ((new ComputerInfo().TotalPhysicalMemory / 1024 / 1024) + 1023) / 1024;
            if (memorySize < 4 && gameId == 3)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take many hours or fail.", "Installer");
            }
            else if (memorySize <= 8 && gameId == 3)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take several hours.", "Installer");
            }
            else if (memorySize <= 4 && gameId != 3)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take a long time.", "Installer");
            }

            return true;
        }

        bool detectMod(int gameId)
        {
            string path = "";
            if (gameId == (int)MeType.ME1_TYPE)
            {
                path = GameData.GamePath + @"\BioGame\CookedPC\testVolumeLight_VFX.upk";
            }
            if (gameId == (int)MeType.ME2_TYPE)
            {
                path = GameData.GamePath + @"\BioGame\CookedPC\BIOC_Materials.pcc";
            }
            if (gameId == (int)MeType.ME3_TYPE)
            {
                path = GameData.GamePath + @"\BIOGame\CookedPCConsole\adv_combat_tutorial_xbox_D_Int.afc";
            }
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(-4, SeekOrigin.End);
                    if (fs.ReadUInt32() == MEMI_TAG)
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        bool applyModTag(int gameId)
        {
            string path = "";
            if (gameId == (int)MeType.ME1_TYPE)
            {
                path = GameData.GamePath + @"\BioGame\CookedPC\testVolumeLight_VFX.upk";
            }
            if (gameId == (int)MeType.ME2_TYPE)
            {
                path = GameData.GamePath + @"\BioGame\CookedPC\BIOC_Materials.pcc";
            }
            if (gameId == (int)MeType.ME3_TYPE)
            {
                path = GameData.GamePath + @"\BIOGame\CookedPCConsole\adv_combat_tutorial_xbox_D_Int.afc";
            }
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Write))
                {
                    fs.SeekEnd();
                    fs.WriteUInt32(MEMI_TAG);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private void buttonPreInstallCheck_Click(object sender, EventArgs e)
        {
            clearPreCheckStatus();

            buttonPreInstallCheck.Enabled = false;
            buttonsEnable(false);
            labelFinalStatus.Text = "Checking...";
            labelPreMods.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreMods.Text = "Checking...";
            Application.DoEvents();
            memFiles = Directory.GetFiles(".", "*.mem", SearchOption.AllDirectories).Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
            memFiles.Sort();
            if (memFiles.Count == 0)
            {
                labelPreMods.Text = "No MEM mods found!";
                labelPreMods.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonsEnable(true);
                buttonPreInstallCheck.Enabled = true;
                buttonUnpackDLC.Enabled = false;
                return;
            }
            errors = "";
            log = "";
            for (int i = 0; i < memFiles.Count; i++)
            {
                using (FileStream fs = new FileStream(memFiles[i], FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                    {
                        if (version != TexExplorer.TextureModVersion)
                            errors += "File " + memFiles[i] + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                        else
                            errors += "File " + memFiles[i] + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                        continue;
                    }
                    else
                    {
                        uint gameType = 0;
                        fs.JumpTo(fs.ReadInt64());
                        gameType = fs.ReadUInt32();
                        if (gameType != gameId)
                        {
                            errors += "File " + memFiles[i] + " is not a MEM mod valid for this game, skipping..." + Environment.NewLine;
                            continue;
                        }
                    }
                }
            }
            string filename = "errors-precheck.txt";
            if (File.Exists(filename))
                File.Delete(filename);
            if (errors != "")
            {
                labelPreMods.Text = "There are some errors while detecting MEM mods!";
                labelPreMods.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonsEnable(true);
                buttonPreInstallCheck.Enabled = true;
                buttonUnpackDLC.Enabled = false;

                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                Process.Start(filename);
                return;
            }
            labelPreMods.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreMods.Text = "";
            checkBoxPreMods.Checked = true;


            labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreGamePath.Text = "Checking...";
            Application.DoEvents();
            gameData = new GameData((MeType)gameId, configIni);
            labelPrePath.Text = GameData.GamePath;
            if (!Directory.Exists(GameData.GamePath))
            {
                labelPreGamePath.Text = "Game path is wrong!";
                labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonsEnable(true);
                buttonPreInstallCheck.Enabled = true;
                buttonUnpackDLC.Enabled = false;
                return;
            }
            if (!gameData.getPackages(true, true))
            {
                labelPreGamePath.Text = "Missing game data!";
                labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonsEnable(true);
                buttonPreInstallCheck.Enabled = true;
                buttonUnpackDLC.Enabled = false;
                return;
            }
            if (gameId == (int)MeType.ME1_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Startup_int.upk"))
                {
                    labelPreGamePath.Text = "ME1 game not found!";
                    labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                    labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                    buttonsEnable(true);
                    buttonPreInstallCheck.Enabled = true;
                    buttonUnpackDLC.Enabled = false;
                    return;
                }
            }
            if (gameId == (int)MeType.ME2_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Textures.tfc"))
                {
                    labelPreGamePath.Text = "ME2 game not found!";
                    labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                    labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                    buttonsEnable(true);
                    buttonPreInstallCheck.Enabled = true;
                    buttonUnpackDLC.Enabled = false;
                    return;
                }
            }
            if (gameId == (int)MeType.ME3_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BIOGame\\PCConsoleTOC.bin"))
                {
                    labelPreGamePath.Text = "ME3 game not found!";
                    labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                    labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                    buttonsEnable(true);
                    buttonPreInstallCheck.Enabled = true;
                    buttonUnpackDLC.Enabled = false;
                    return;
                }
            }
            labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreGamePath.Text = "";
            checkBoxPrePath.Checked = true;

            labelPreAccess.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreAccess.Text = "Checking...";
            Application.DoEvents();
            bool writeAccess = false;
            if (Misc.checkWriteAccessDir(GameData.MainData))
                writeAccess = true;
            if (gameId == (int)MeType.ME1_TYPE)
            {
                if (Misc.checkWriteAccessFile(GameData.GamePath + @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk"))
                    writeAccess = true;
                else
                    writeAccess = false;
            }
            if (gameId == (int)MeType.ME2_TYPE)
            {
                if (Misc.checkWriteAccessFile(GameData.GamePath + @"\BioGame\CookedPC\BioD_CitAsL.pcc"))
                    writeAccess = true;
                else
                    writeAccess = false;
            }
            if (gameId == (int)MeType.ME3_TYPE)
            {
                if (Misc.checkWriteAccessFile(GameData.GamePath + @"\BioGame\CookedPCConsole\BioA_CitSam_000LevelTrans.pcc"))
                    writeAccess = true;
                else
                    writeAccess = false;
            }
            if (!writeAccess)
            {
                labelPreAccess.Text = "Write access denied to game folders!";
                labelPreAccess.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonsEnable(true);
                buttonPreInstallCheck.Enabled = true;
                buttonUnpackDLC.Enabled = false;
                return;
            }
            labelPreAccess.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreAccess.Text = "";
            checkBoxPreAccess.Checked = true;


            labelPreSpace.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreSpace.Text = "Checking...";
            Application.DoEvents();
            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;

            for (int i = 0; i < memFiles.Count; i++)
            {
                diskUsage += new FileInfo(memFiles[i]).Length;
            }
            diskUsage = (long)(diskUsage * 2.5);

            if (gameId == (int)MeType.ME3_TYPE)
            {
                if (Directory.Exists(GameData.DLCData))
                {
                    long diskUsageDLC = 0;
                    List<string> sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                    for (int i = 0; i < sfarFiles.Count; i++)
                    {
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                            sfarFiles.RemoveAt(i--);
                    }
                    for (int i = 0; i < sfarFiles.Count; i++)
                    {
                        diskUsageDLC += new FileInfo(sfarFiles[i]).Length;
                    }
                    diskUsage = (long)(diskUsageDLC * 2.1);
                }
            }

            if (diskUsage > diskFreeSpace)
            {
                labelPreSpace.Text = "You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.";
                labelPreSpace.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonsEnable(true);
                buttonPreInstallCheck.Enabled = true;
                buttonUnpackDLC.Enabled = false;
                return;
            }
            labelPreSpace.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreSpace.Text = "";
            checkBoxPreSpace.Checked = true;


            labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreVanilla.Text = "Checking...";
            Application.DoEvents();
            if (Misc.detectBrokenMod((MeType)gameId))
            {
                labelPreVanilla.Text = "Detected not compatible mod!";
                labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary check detected issue...";
                buttonPreInstallCheck.Enabled = true;
                buttonsEnable(true);
                return;
            }
            if (detectMod(gameId))
            {
                labelPreVanilla.Text = "Detected modded game ! Please reinstall ME" + gameId + " and restart.";
                labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary check detected issue...";
                buttonPreInstallCheck.Enabled = true;
                buttonsEnable(true);
                return;
            }

            if (!checkBoxOptionVanilla.Checked)
                errors = Misc.checkGameFiles((MeType)gameId, null, this);
            if (errors != "")
            {
                using (FileStream fs = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    fs.SeekEnd();
                    fs.WriteStringASCII("=========================================================" + Environment.NewLine);
                    fs.WriteStringASCII("WARNING: looks like the following file(s) are not vanilla" + Environment.NewLine);
                    fs.WriteStringASCII("=========================================================" + Environment.NewLine + Environment.NewLine);
                    fs.WriteStringASCII(errors);
                }
                Process.Start(filename);
                labelPreVanilla.Text = "Game files are not vanilla!";
                labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary check detected potential issue...";
                string message = "The installer detected that the following game files\n" +
                    "are not unmodded (vanilla) game files\n" +
                    "You can find the list of files in the window that just opened.\n\n" +
                    "The correct installation order is as follows:\n" +
                    "1. Content mods (PCC, DLC mods)\n";
                if (gameId == 1)
                {
                    message += "2. MEUITM\n";
                    message += "2a. ALOT Addon\n";
                }
                else
                    message += "2. ALOT & ALOT Addon\n";
                message += "3. Texture and meshes mods (TPF, DDS, MOD)\n\n" +
                    "- If you have installed texture mods already, revert your game to vanilla,\n" +
                    "  then follow the correct installation order.\n\n" +
                    "- If you have properly installed content mods before this mod,\n" +
                    "  this result is normal and you can continue the installation.\n" +
                    "  It's advised to verify if all items in the list are supposed to be modded.\n" +
                    "  To verify : compare the list of files that failed the check against the list of files you copied\n" +
                    "  from your content mods to the ";
                if (gameId == 3)
                    message += "CookedPCConsole";
                else
                    message += "CookedPC";
                message += " directory. Both lists should be identical.\n\n" +
                    "  If you are not sure what you installed it is recommended that you revert your game to vanilla\n" +
                    "  and run this check again, to make sure the files are indeed vanilla.\n" +
                    "  If it passes then you can install content mods (PCC, DLC mods),\n" +
                    "  and continue installation.\n\n";
                MessageBox.Show(message, "Warning !");
            }
            else
            {
                labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
                labelPreVanilla.Text = "";
                labelFinalStatus.Text = "Ready to go. Press START button!";
            }
            checkBoxPreVanilla.Checked = true;


            buttonPreInstallCheck.Enabled = true;
            buttonsEnable(true);
            buttonSTART.Enabled = true;
        }

        public void applyModules()
        {
            for (int i = 0; i < memFiles.Count; i++)
            {
                log += "Mod: " + (i + 1) + " of " + memFiles.Count + " started: " + Path.GetFileName(memFiles[i]) + Environment.NewLine;
                using (FileStream fs = new FileStream(memFiles[i], FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                    {
                        if (version != TexExplorer.TextureModVersion)
                        {
                            errors += "File " + memFiles[i] + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                            log += "File " + memFiles[i] + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                        }
                        else
                        {
                            errors += "File " + memFiles[i] + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                            log += "File " + memFiles[i] + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                        }
                        continue;
                    }
                    else
                    {
                        uint gameType = 0;
                        fs.JumpTo(fs.ReadInt64());
                        gameType = fs.ReadUInt32();
                        if ((MeType)gameType != GameData.gameType)
                        {
                            errors += "File " + memFiles[i] + " is not a MEM mod valid for this game, skipping..." + Environment.NewLine;
                            log += "File " + memFiles[i] + " is not a MEM mod valid for this game, skipping..." + Environment.NewLine;
                            continue;
                        }
                    }
                    int numFiles = fs.ReadInt32();
                    List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();
                    for (int k = 0; k < numFiles; k++)
                    {
                        MipMaps.FileMod fileMod = new MipMaps.FileMod();
                        fileMod.tag = fs.ReadUInt32();
                        fileMod.name = fs.ReadStringASCIINull();
                        fileMod.offset = fs.ReadInt64();
                        fileMod.size = fs.ReadInt64();
                        modFiles.Add(fileMod);
                    }
                    numFiles = modFiles.Count;
                    for (int l = 0; l < numFiles; l++)
                    {
                        string name = "";
                        uint crc = 0;
                        long size = 0, dstLen = 0;
                        int exportId = -1;
                        string pkgPath = "";
                        byte[] dst = null;
                        fs.JumpTo(modFiles[l].offset);
                        size = modFiles[l].size;
                        if (modFiles[l].tag == MipMaps.FileTextureTag)
                        {
                            name = fs.ReadStringASCIINull();
                            crc = fs.ReadUInt32();
                        }
                        else if (modFiles[l].tag == MipMaps.FileBinaryTag)
                        {
                            name = modFiles[l].name;
                            exportId = fs.ReadInt32();
                            pkgPath = fs.ReadStringASCIINull();
                        }

                        dst = MipMaps.decompressData(fs, size);
                        dstLen = dst.Length;

                        updateStatusTextures("Mod: " + (i + 1) + " of " + memFiles.Count + " - in progress: " + ((l + 1) * 100 / numFiles) + " % ");

                        if (modFiles[l].tag == MipMaps.FileTextureTag)
                        {
                            FoundTexture foundTexture;
                            foundTexture = textures.Find(s => s.crc == crc);
                            if (foundTexture.crc != 0)
                            {
                                Image image = new Image(dst, Image.ImageFormat.DDS);
                                if (!image.checkDDSHaveAllMipmaps())
                                {
                                    errors += "Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps" + Environment.NewLine;
                                    log += "Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps" + Environment.NewLine;
                                    continue;
                                }
                                errors += mipMaps.replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc);
                            }
                            else
                            {
                                log += "Texture skipped. Texture " + name + string.Format("_0x{0:X8}", crc) + " is not present in your game setup" + Environment.NewLine;
                            }
                        }
                        else if (modFiles[l].tag == MipMaps.FileBinaryTag)
                        {
                            string path = GameData.GamePath + pkgPath;
                            if (!File.Exists(path))
                            {
                                log += "Warning: File " + path + " not exists in your game setup." + Environment.NewLine;
                                continue;
                            }
                            Package pkg = cachePackageMgr.OpenPackage(path);
                            pkg.setExportData(exportId, dst);
                        }
                        else
                        {
                            errors += "Unknown tag for file: " + name + Environment.NewLine;
                            log += "Unknown tag for file: " + name + Environment.NewLine;
                        }
                    }
                }
            }
        }

        public void buttonsDefault(int gameId)
        {
            if (gameId != 3)
            {
                checkBoxPreEnablePack.Visible = false;
                labelME3DLCPack.Visible = false;
                checkBoxPackDLC.Visible = false;
                labelStatusPackDLC.Visible = false;

                labelMERepackZlib.Visible = false;
                checkBoxRepackZlib.Visible = false;
                labelStatusRepackZlib.Visible = false;
                buttonUnpackDLC.Visible = false;
            }
            if (gameId == 3)
            {
                checkBoxPreEnableRepack.Visible = false;
                labelMERepackZlib.Visible = false;
                labelME3DLCPack.Visible = false;
                checkBoxRepackZlib.Visible = false;
                labelStatusRepackZlib.Visible = false;

                labelME3DLCPack.Visible = false;
                checkBoxPackDLC.Visible = false;
                labelStatusPackDLC.Visible = false;
            }

            Application.DoEvents();
        }

        public void clearPreCheckStatus()
        {
            labelPreMods.Text = "";
            labelPreGamePath.Text = "";
            labelPrePath.Text = "";
            labelPreAccess.Text = "";
            labelPreSpace.Text = "";
            labelPreVanilla.Text = "";
            labelStatusRepackZlib.Text = "";
            labelStatusPackDLC.Text = "";

            checkBoxPreMods.Checked = false;
            checkBoxPrePath.Checked = false;
            checkBoxPreAccess.Checked = false;
            checkBoxPreSpace.Checked = false;
            checkBoxPreVanilla.Checked = false;

            Application.DoEvents();
        }

        private void buttonChangePath_Click(object sender, EventArgs e)
        {
            gameData = new GameData((MeType)gameId, configIni, true);
            clearPreCheckStatus();
            labelPrePath.Text = GameData.GamePath;
            buttonSTART.Enabled = false;
            buttonUnpackDLC.Enabled = false;
        }

        private void buttonsEnable(bool enabled)
        {
            buttonExit.Enabled = enabled;
            buttonNormal.Enabled = enabled;
            buttonPreChangePath.Enabled = enabled;
            checkBoxPreEnableRepack.Enabled = enabled;
            checkBoxPreEnablePack.Enabled = enabled;
            checkBoxOptionVanilla.Enabled = enabled;
            checkBoxOptionFaster.Enabled = enabled;
            buttonUnpackDLC.Enabled = enabled;
            Application.DoEvents();
        }

        private void buttonSTART_Click(object sender, EventArgs e)
        {
            buttonsEnable(false);
            buttonPreInstallCheck.Enabled = false;
            buttonSTART.Enabled = false;
            labelFinalStatus.Text = "Process in progress...";

            errors = "";
            log = "";
            Misc.startTimer();

            log += "Prepare game data started..." + Environment.NewLine;
            updateStatusPrepare("In progress...");
            if (GameData.gameType == MeType.ME1_TYPE)
                Misc.VerifyME1Exe(gameData, false);

            if (GameData.gameType == MeType.ME3_TYPE)
            {
                ME3DLC.unpackAllDLC(null, this);
                gameData.getPackages(true, true);
            }

            if (GameData.gameType != MeType.ME1_TYPE)
                gameData.getTfcTextures();

            checkBoxPrepare.Checked = true;
            updateStatusPrepare("");
            log += "Prepare game data finished" + Environment.NewLine + Environment.NewLine;

            if (Directory.Exists(GameData.DLCData))
            {
                List<string> dirs = Directory.EnumerateDirectories(GameData.DLCData).ToList();
                log += "Detected folowing folders in DLC path:" + Environment.NewLine;
                for (int dl = 0; dl < dirs.Count; dl++)
                {
                    log += Path.GetFileName(dirs[dl]) + Environment.NewLine;
                }
            }
            else
            {
                log += "Not detected folders in DLC path" + Environment.NewLine;
            }
            log += Environment.NewLine;

            log += "Scan textures started..." + Environment.NewLine;
            updateStatusScan("In progress...");
            if (checkBoxOptionFaster.Checked)
                errors += treeScan.PrepareListOfTextures(null, cachePackageMgr, null, this, ref log, true);
            else
                errors += treeScan.PrepareListOfTextures(null, null, null, this, ref log, true);
            textures = treeScan.treeScan;
            checkBoxScan.Checked = true;
            updateStatusScan("");
            log += "Scan textures finished" + Environment.NewLine + Environment.NewLine;


            if (checkBoxOptionFaster.Checked)
            {
                if (GameData.gameType == MeType.ME1_TYPE)
                {
                    log += "Remove mipmaps started..." + Environment.NewLine;
                    updateStatusMipMaps("In progress...");
                    errors += mipMaps.removeMipMapsME1(1, textures, cachePackageMgr, null, this, checkBoxPreEnableRepack.Checked);
                    errors += mipMaps.removeMipMapsME1(2, textures, cachePackageMgr, null, this, checkBoxPreEnableRepack.Checked);
                    checkBoxMipMaps.Checked = true;
                    updateStatusMipMaps("");
                    log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
                }
                else
                {
                    log += "Remove mipmaps started..." + Environment.NewLine;
                    updateStatusMipMaps("In progress...");
                    errors += mipMaps.removeMipMapsME2ME3(textures, cachePackageMgr, null, this, checkBoxPreEnableRepack.Checked);
                    checkBoxMipMaps.Checked = true;
                    updateStatusMipMaps("");
                    log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
                }
            }


            log += "Process textures started..." + Environment.NewLine;
            updateStatusTextures("In progress...");
            applyModules();
            checkBoxTextures.Checked = true;
            updateStatusTextures("");
            log += "Process textures finished" + Environment.NewLine + Environment.NewLine;


            updateStatusStore("Progress...");
            cachePackageMgr.CloseAllWithSave(checkBoxPreEnableRepack.Checked);
            checkBoxStore.Checked = true;
            updateStatusStore("");


            if (!checkBoxOptionFaster.Checked)
            {
                if (GameData.gameType == MeType.ME1_TYPE)
                {
                    log += "Remove mipmaps started..." + Environment.NewLine;
                    updateStatusMipMaps("In progress...");
                    errors += mipMaps.removeMipMapsME1(1, textures, null, null, this, checkBoxPreEnableRepack.Checked);
                    errors += mipMaps.removeMipMapsME1(2, textures, null, null, this, checkBoxPreEnableRepack.Checked);
                    checkBoxMipMaps.Checked = true;
                    updateStatusMipMaps("");
                    log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
                }
                else
                {
                    log += "Remove mipmaps started..." + Environment.NewLine;
                    updateStatusMipMaps("In progress...");
                    errors += mipMaps.removeMipMapsME2ME3(textures, null, null, this, checkBoxPreEnableRepack.Checked);
                    checkBoxMipMaps.Checked = true;
                    updateStatusMipMaps("");
                    log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
                }
            }

            log += "Updating LODs and other settings started..." + Environment.NewLine;
            updateStatusLOD("In progress...");
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD((MeType)gameId, engineConf);
            LODSettings.updateGFXSettings((MeType)gameId, engineConf);
            checkBoxLOD.Checked = true;
            updateStatusLOD("");
            log += "Updating LODs and other settings finished" + Environment.NewLine + Environment.NewLine;


            if (checkBoxPreEnableRepack.Checked)
            {
                log += "Repack started..." + Environment.NewLine;
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    updateStatusRepackZlib("Repacking PCC files... " + ((i + 1) * 100 / GameData.packageFiles.Count) + " %");
                    Package package = new Package(GameData.packageFiles[i], true, true);
                    if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                    {
                        package.Dispose();
                        package = new Package(GameData.packageFiles[i]);
                        package.SaveToFile(true);
                    }
                }
                checkBoxRepackZlib.Checked = true;
                updateStatusRepackZlib("");
                log += "Repack finished" + Environment.NewLine + Environment.NewLine;
            }


            if (checkBoxPreEnablePack.Checked)
            {
                if (Directory.Exists(GameData.DLCData))
                {
                    TOCBinFile.UpdateAllTOCBinFiles();

                    log += "Repack started..." + Environment.NewLine;
                    updateStatusPackDLC("In progress...");
                    List<string> DLCs = Directory.GetDirectories(GameData.DLCData).ToList();
                    for (int i = 0; i < DLCs.Count; i++)
                    {
                        List<string> files = Directory.GetFiles(DLCs[i], "Mount.dlc", SearchOption.AllDirectories).ToList();
                        if (files.Count == 0)
                            DLCs.RemoveAt(i--);
                    }

                    string tmpDlcDir = Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp");
                    for (int i = 0; i < DLCs.Count; i++)
                    {
                        string DLCname = Path.GetFileName(DLCs[i]);
                        string outPath = Path.Combine(tmpDlcDir, DLCname, "CookedPCConsole", "Default.sfar");
                        ME3DLC dlc = new ME3DLC(null);
                        dlc.fullRePack(DLCs[i], outPath, DLCname, null, this);
                    }

                    Directory.Delete(GameData.DLCData, true);
                    Directory.Move(tmpDlcDir, GameData.DLCData);

                    updateStatusPackDLC("");
                    log += "Repack started finished" + Environment.NewLine + Environment.NewLine;
                }
                checkBoxPackDLC.Checked = true;
            }

            if (!applyModTag(gameId))
                errors += "Failed applying stamp for installation!\n";

            var time = Misc.stopTimer();
            labelFinalStatus.Text = "Process finished. Process total time: " + Misc.getTimerFormat(time);
            buttonExit.Enabled = true;
            buttonNormal.Enabled = true;

            log += "==========================================" + Environment.NewLine;
            log += "LOD settings:" + Environment.NewLine;
            LODSettings.readLOD((MeType)gameId, engineConf, ref log);
            log += "==========================================" + Environment.NewLine;

            string filename = "install-log.txt";
            if (File.Exists(filename))
                File.Delete(filename);
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
            {
                fs.WriteStringASCII(log);
            }

            filename = "errors-install.txt";
            if (File.Exists(filename))
                File.Delete(filename);
            if (errors != "")
            {
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                MessageBox.Show("WARNING: Some errors have occured!");
                Process.Start(filename);
            }
        }

        public void updateLabelPreVanilla(string text)
        {
            labelPreVanilla.Text = text;
            Application.DoEvents();
        }

        public void updateStatusPrepare(string text)
        {
            labelStatusPrepare.Text = text;
            Application.DoEvents();
        }

        public void updateStatusScan(string text)
        {
            labelStatusScan.Text = text;
            Application.DoEvents();
        }

        public void updateStatusMipMaps(string text)
        {
            labelStatusMipMaps.Text = text;
            Application.DoEvents();
        }

        public void updateStatusTextures(string text)
        {
            labelStatusTextures.Text = text;
            Application.DoEvents();
        }

        public void updateStatusStore(string text)
        {
            labelStatusStore.Text = text;
            Application.DoEvents();
        }

        public void updateStatusLOD(string text)
        {
            labelStatusLOD.Text = text;
            Application.DoEvents();
        }

        public void updateStatusRepackZlib(string text)
        {
            labelStatusRepackZlib.Text = text;
            Application.DoEvents();
        }

        public void updateStatusPackDLC(string text)
        {
            labelStatusPackDLC.Text = text;
            Application.DoEvents();
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonNormal_Click(object sender, EventArgs e)
        {
            exitToModder = true;
            Close();
        }

        private void checkBoxPreEnableRepack_CheckedChanged(object sender, EventArgs e)
        {
            labelMERepackZlib.Visible = checkBoxPreEnableRepack.Checked;
            checkBoxRepackZlib.Visible = checkBoxPreEnableRepack.Checked;
            labelStatusRepackZlib.Visible = checkBoxPreEnableRepack.Checked;
        }

        private void checkBoxPreEnablePack_CheckedChanged(object sender, EventArgs e)
        {
            labelME3DLCPack.Visible = false;//checkBoxPreEnablePack.Checked;
            checkBoxPackDLC.Visible = checkBoxPreEnablePack.Checked;
            labelStatusPackDLC.Visible = checkBoxPreEnablePack.Checked;
        }

        private void buttonUnpackDLC_Click(object sender, EventArgs e)
        {
            buttonsEnable(false);
            ME3DLC.unpackAllDLC(null, this);
            updateStatusPackDLC("");
            buttonsEnable(true);
        }

        private void checkBoxOptionVanilla_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOptionVanilla.Checked)
            {
                if (MessageBox.Show("This option is for advanced users !\n\n" +
                    "Disabling the check of the game files prevents the detection of various potential issues.\n\n" +
                    "If you are not sure press 'Cancel'", "Warning !", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    checkBoxOptionVanilla.CheckedChanged -= checkBoxOptionVanilla_CheckedChanged;
                    checkBoxOptionVanilla.Checked = false;
                    checkBoxOptionVanilla.CheckedChanged += checkBoxOptionVanilla_CheckedChanged;
                }
            }
            else
            {
                checkBoxOptionVanilla.CheckedChanged -= checkBoxOptionVanilla_CheckedChanged;
                checkBoxOptionVanilla.Checked = false;
                checkBoxOptionVanilla.CheckedChanged += checkBoxOptionVanilla_CheckedChanged;
            }
        }
    }
}
