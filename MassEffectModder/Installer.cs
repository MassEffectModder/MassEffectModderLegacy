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

using AmaroK86.ImageFormat;
using StreamHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class Installer : Form
    {
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

        public Installer()
        {
            InitializeComponent();
            mipMaps = new MipMaps();
            treeScan = new TreeScan();
            cachePackageMgr = new CachePackageMgr(null, this);
        }

        public bool Run()
        {
            installerIni = new ConfIni(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "installer.ini"));
            string gameIdStr = installerIni.Read("GameId", "Main");
            if (gameIdStr == "ME1")
                gameId = 1;
            else if (gameIdStr == "ME2")
                gameId = 2;
            else if (gameIdStr == "ME3")
                gameId = 3;
            else
            {
                MessageBox.Show("Game ID not recognized in installer.ini, exiting...", "Installer");
                return false;
            }
            configIni = new ConfIni();

            labelStatusPrepare.Text = "";
            labelStatusScan.Text = "";
            labelStatusTextures.Text = "";
            labelStatusStore.Text = "";
            labelStatusMipMaps.Text = "";
            labelStatusLOD.Text = "";
            labelFinalStatus.Text = "Before beginning, press the CHECK button.";

            if (gameId != 3)
            {
                checkBoxPreEnablePack.Visible = false;
                labelME3DLCPack.Visible = false;
                checkBoxPackDLC.Visible = false;
                labelStatusPackDLC.Visible = false;

                labelMERepackZlib.Visible = false;
                checkBoxRepackZlib.Visible = false;
                labelStatusRepackZlib.Visible = false;
            }
            if (gameId == 3)
            {
                checkBoxPreEnableRepack.Visible = false;
                labelMERepackZlib.Visible = false;
                checkBoxRepackZlib.Visible = false;
                labelStatusRepackZlib.Visible = false;

                labelME3DLCPack.Visible = false;
                checkBoxPackDLC.Visible = false;
                labelStatusPackDLC.Visible = false;
            }

            clearPreCheckStatus();

            buttonSTART.Enabled = false;

            return true;
        }

        private void buttonPreInstallCheck_Click(object sender, EventArgs e)
        {
            clearPreCheckStatus();

            buttonPreInstallCheck.Enabled = false;
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
                buttonPreInstallCheck.Enabled = true;
                return;
            }
            errors = "";
            for (int i = 0; i < memFiles.Count; i++)
            {
                using (FileStream fs = new FileStream(memFiles[i], FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.TextureModTag || (version != TexExplorer.TextureModVersion1 && version != TexExplorer.TextureModVersion))
                    {
                        errors += "File " + memFiles[i] + " is not a valid MEM mod" + Environment.NewLine;
                        continue;
                    }
                    else
                    {
                        uint gameType = 0;
                        if (version == TexExplorer.TextureModVersion)
                            fs.JumpTo(fs.ReadInt64());
                        gameType = fs.ReadUInt32();
                        if (gameType != gameId)
                        {
                            errors += "File " + memFiles[i] + " is not a MEM mod valid for this game" + Environment.NewLine;
                            continue;
                        }
                    }
                }
            }
            if (errors != "")
            {
                labelPreMods.Text = "There are some errors while detecting MEM mods!";
                labelPreMods.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonPreInstallCheck.Enabled = true;

                string filename = "errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                if (errors != "")
                {
                    using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                    {
                        fs.WriteStringASCII(errors);
                    }
                    Process.Start(filename);
                }

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
                buttonPreInstallCheck.Enabled = true;
                return;
            }
            if (!gameData.getPackages(false))
            {
                labelPreGamePath.Text = "Missing game data!";
                labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonPreInstallCheck.Enabled = true;
                return;
            }
            if (gameId == (int)MeType.ME1_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Startup_int.upk"))
                {
                    labelPreGamePath.Text = "ME1 game not found!";
                    labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.Red);
                    labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                    buttonPreInstallCheck.Enabled = true;
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
                    buttonPreInstallCheck.Enabled = true;
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
                    buttonPreInstallCheck.Enabled = true;
                    return;
                }
            }
            labelPreGamePath.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreGamePath.Text = "";
            checkBoxPrePath.Checked = true;

            labelPreAccess.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreAccess.Text = "Checking...";
            Application.DoEvents();
            if (!Misc.checkWriteAccess(GameData.GamePath))
            {
                labelPreAccess.Text = "Write access denied to game folders!";
                labelPreAccess.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonPreInstallCheck.Enabled = true;
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
                List<string> sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                for (int i = 0; i < sfarFiles.Count; i++)
                {
                    if (new FileInfo(sfarFiles[i]).Length <= 32)
                        sfarFiles.RemoveAt(i--);
                }
                for (int i = 0; i < sfarFiles.Count; i++)
                {
                    diskUsage += new FileInfo(sfarFiles[i]).Length;
                }
                diskUsage = (long)(diskUsage * 2.5);
            }

            if (diskUsage > diskFreeSpace)
            {
                labelPreSpace.Text = "You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.";
                labelPreSpace.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonPreInstallCheck.Enabled = true;
                return;
            }
            labelPreSpace.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreSpace.Text = "";
            checkBoxPreSpace.Checked = true;


            labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreVanilla.Text = "Checking...";
            Application.DoEvents();
            if (MipMaps.checkGameDataModded())
            {
                labelPreVanilla.Text = "Game is already modded!";
                labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.Red);
                labelFinalStatus.Text = "Preliminary checking failed. Issue detected...";
                buttonPreInstallCheck.Enabled = true;
                return;
            }
            labelPreVanilla.ForeColor = Color.FromKnownColor(KnownColor.LimeGreen);
            labelPreVanilla.Text = "";
            checkBoxPreVanilla.Checked = true;


            labelFinalStatus.Text = "Ready to go. Press START button!";
            buttonPreInstallCheck.Enabled = true;
            buttonSTART.Enabled = true;
        }

        public void applyModules()
        {
            for (int i = 0; i < memFiles.Count; i++)
            {
                using (FileStream fs = new FileStream(memFiles[i], FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.TextureModTag || (version != TexExplorer.TextureModVersion1 && version != TexExplorer.TextureModVersion))
                    {
                        errors += "File " + memFiles[i] + " is not a valid MEM mod" + Environment.NewLine;
                        continue;
                    }
                    else
                    {
                        uint gameType = 0;
                        if (version == TexExplorer.TextureModVersion)
                            fs.JumpTo(fs.ReadInt64());
                        gameType = fs.ReadUInt32();
                        if ((MeType)gameType != GameData.gameType)
                        {
                            errors += "File " + memFiles[i] + " is not a MEM mod valid for this game" + Environment.NewLine;
                            continue;
                        }
                    }
                    int numFiles = fs.ReadInt32();
                    List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();
                    if (version == TexExplorer.TextureModVersion)
                    {
                        for (int k = 0; k < numFiles; k++)
                        {
                            MipMaps.FileMod fileMod = new MipMaps.FileMod();
                            fileMod.tag = fs.ReadUInt32();
                            fileMod.name = fs.ReadStringASCIINull();
                            fileMod.offset = fs.ReadInt64();
                            fileMod.size = fs.ReadInt64();
                            if (fileMod.tag == MipMaps.FileTextureTag)
                                modFiles.Add(fileMod);
                        }
                        numFiles = modFiles.Count;
                    }
                    for (int l = 0; l < numFiles; l++)
                    {
                        string name;
                        uint crc;
                        long size = 0, dstLen = 0, decSize = 0;
                        byte[] dst = null;
                        if (version == TexExplorer.TextureModVersion)
                        {
                            fs.JumpTo(modFiles[i].offset);
                            size = modFiles[i].size;
                        }
                        name = fs.ReadStringASCIINull();
                        crc = fs.ReadUInt32();
                        if (version == TexExplorer.TextureModVersion1)
                        {
                            decSize = fs.ReadUInt32();
                            size = fs.ReadUInt32();
                            byte[] src = fs.ReadToBuffer(size);
                            dst = new byte[decSize];
                            dstLen = ZlibHelper.Zlib.Decompress(src, (uint)size, dst);
                        }
                        else
                        {
                            dst = MipMaps.decompressData(fs, size);
                            dstLen = dst.Length;
                        }

                        updateStatusTextures("Mod: " + (i + 1) + " of " + modFiles.Count + " - in progress: " + ((l + 1) * 100 / numFiles) + " % ");

                        FoundTexture foundTexture;
                        if (GameData.gameType == MeType.ME1_TYPE)
                            foundTexture = textures.Find(s => s.crc == crc && s.name == name);
                        else
                            foundTexture = textures.Find(s => s.crc == crc);
                        if (foundTexture.crc != 0)
                        {
                            DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                            if (!image.checkExistAllMipmaps())
                            {
                                errors += "Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps" +  Environment.NewLine;
                                continue;
                            }
                            errors += mipMaps.replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name);
                        }
                        else
                        {
                            errors += "Texture skipped. Texture " + name + string.Format("_0x{0:X8}", crc) + " is not present in your game setup" + Environment.NewLine;
                        }
                    }
                }
            }
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

            checkBoxPreEnableRepack.Checked = false;
            checkBoxPreEnablePack.Checked = false;
            Application.DoEvents();
        }

        private void buttonChangePath_Click(object sender, EventArgs e)
        {
            gameData = new GameData((MeType)gameId, configIni, true);
            clearPreCheckStatus();
            labelPrePath.Text = GameData.GamePath;
            buttonSTART.Enabled = false;
        }

        private void buttonsEnable(bool enabled)
        {
            buttonExit.Enabled = enabled;
            buttonNormal.Enabled = enabled;
            Application.DoEvents();
        }

        private void buttonSTART_Click(object sender, EventArgs e)
        {
            buttonPreInstallCheck.Enabled = false;
            buttonSTART.Enabled = false;
            buttonPreChangePath.Enabled = false;
            checkBoxPreEnableRepack.Enabled = false;
            checkBoxPreEnablePack.Enabled = false;
            buttonsEnable(false);
            labelFinalStatus.Text = "Process in progress...";

            Misc.startTimer();

            updateStatusPrepare("In progress...");
            if (GameData.gameType == MeType.ME1_TYPE)
                Misc.VerifyME1Exe(gameData, false);

            if (GameData.gameType == MeType.ME3_TYPE)
                ME3DLC.unpackAllDLC(null, this);

            if (GameData.gameType != MeType.ME1_TYPE)
                gameData.getTfcTextures();

            checkBoxPrepare.Checked = true;
            updateStatusPrepare("");


            updateStatusScan("In progress...");
            textures = treeScan.PrepareListOfTextures(null, null, this, true);
            checkBoxScan.Checked = true;
            updateStatusScan("");


            updateStatusTextures("In progress...");
            applyModules();
            checkBoxTextures.Checked = true;
            updateStatusTextures("");


            updateStatusStore("Progress...");
            cachePackageMgr.CloseAllWithSave();
            checkBoxStore.Checked = true;
            updateStatusStore("");


            updateStatusMipMaps("In progress...");
            mipMaps.removeMipMaps(textures, cachePackageMgr, null, this, checkBoxPreEnableRepack.Checked);
            checkBoxMipMaps.Checked = true;
            updateStatusMipMaps("");


            updateStatusLOD("In progress...");
            LODSettings.updateLOD((MeType)gameId, configIni);
            checkBoxLOD.Checked = true;
            updateStatusLOD("");


            if (checkBoxPreEnableRepack.Checked)
            {
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    updateStatusRepackZlib("Repacking PCC files... " + ((i + 1) * 100 / GameData.packageFiles.Count) + " %");
                    Package package = new Package(GameData.packageFiles[i]);
                    if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                        package.SaveToFile(true);
                }
                checkBoxRepackZlib.Checked = true;
                updateStatusRepackZlib("");
            }


            if (checkBoxPreEnablePack.Checked)
            {
                if (Directory.Exists(GameData.DLCData))
                {
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
                }
                checkBoxPackDLC.Checked = true;
            }


            var time = Misc.stopTimer();
            labelFinalStatus.Text = "Process finished. Process total time: " + Misc.getTimerFormat(time);
            buttonsEnable(true);

            string filename = "errors.txt";
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
            labelME3DLCPack.Visible = checkBoxPreEnablePack.Checked;
            checkBoxPackDLC.Visible = checkBoxPreEnablePack.Checked;
            labelStatusPackDLC.Visible = checkBoxPreEnablePack.Checked;
        }
    }
}
