/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Reflection;

namespace MassEffectModder
{
    public partial class Installer : Form
    {
        struct ModSelection
        {
            public List<string> files;
            public List<string> descriptions;
        }
        List<ModSelection> modsSelection;
        List<string> allMemMods;
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
        bool updateMode;
        string errors = "";
        string log = "";
        string sourceDir;
        int AlotVer;
        int MeuitmVer;
        bool allowToSkipScan;
        string softShadowsModPath;
        string splashDemiurge;
        string splashBitmapPath;
        bool meuitmMode = false;
        bool OptionVanillaVisible;
        bool OptionSkipScanVisible;
        bool OptionRepackVisible;
        bool OptionLimit2KVisible;
        bool mute = false;
        int stage = 1;
        int totalStages = 7;
        System.Media.SoundPlayer musicPlayer;
        CustomLabel customLabelDesc;
        CustomLabel customLabelCurrentStatus;
        CustomLabel customLabelFinalStatus;

        public Installer()
        {
            InitializeComponent();
            Text = "MEM Installer v" + Application.ProductVersion + " for ALOT";
            mipMaps = new MipMaps();
            treeScan = new TreeScan();
            cachePackageMgr = new CachePackageMgr(null, this);

            // 
            // customLabelDesc
            // 
            customLabelDesc = new CustomLabel();
            customLabelDesc.Anchor = labelDesc.Anchor;
            customLabelDesc.BackColor = labelDesc.BackColor;
            customLabelDesc.FlatStyle = labelDesc.FlatStyle;
            customLabelDesc.Font = labelDesc.Font;
            customLabelDesc.ForeColor = labelDesc.ForeColor;
            customLabelDesc.Location = labelDesc.Location;
            customLabelDesc.Name = "customLabelDesc";
            customLabelDesc.Size = labelDesc.Size;
            customLabelDesc.TextAlign = labelDesc.TextAlign;
            customLabelDesc.Visible = true;
            Controls.Add(customLabelDesc);
            // 
            // customLabelCurrentStatus
            // 
            customLabelCurrentStatus = new CustomLabel();
            customLabelCurrentStatus.Anchor = labelCurrentStatus.Anchor;
            customLabelCurrentStatus.BackColor = labelCurrentStatus.BackColor;
            customLabelCurrentStatus.FlatStyle = labelCurrentStatus.FlatStyle;
            customLabelCurrentStatus.Font = labelCurrentStatus.Font;
            customLabelCurrentStatus.ForeColor = labelCurrentStatus.ForeColor;
            customLabelCurrentStatus.Location = labelCurrentStatus.Location;
            customLabelCurrentStatus.Name = "customLabelCurrentStatus";
            customLabelCurrentStatus.Size = labelCurrentStatus.Size;
            customLabelCurrentStatus.TextAlign = labelCurrentStatus.TextAlign;
            customLabelCurrentStatus.Visible = true;
            Controls.Add(customLabelCurrentStatus);
            // 
            // customLabelDesc
            // 
            customLabelFinalStatus = new CustomLabel();
            customLabelFinalStatus.Anchor = labelFinalStatus.Anchor;
            customLabelFinalStatus.BackColor = labelFinalStatus.BackColor;
            customLabelFinalStatus.FlatStyle = labelFinalStatus.FlatStyle;
            customLabelFinalStatus.Font = labelFinalStatus.Font;
            customLabelFinalStatus.ForeColor = labelFinalStatus.ForeColor;
            customLabelFinalStatus.Location = labelFinalStatus.Location;
            customLabelFinalStatus.Name = "customLabelFinalStatus";
            customLabelFinalStatus.Size = labelFinalStatus.Size;
            customLabelFinalStatus.TextAlign = labelFinalStatus.TextAlign;
            customLabelFinalStatus.Visible = true;
            Controls.Add(customLabelFinalStatus);
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
            {
                Text = "MEM Installer v" + Application.ProductVersion + " for " + baseModNameStr;
                if (gameId == 1 && baseModNameStr.Contains("MEUITM"))
                    meuitmMode = true;
            }
            else
                Text += " for ALOT ME" + gameId;

            if (runAsAdmin)
                Text += " (run as Administrator)";

            sourceDir = installerIni.Read("SourceDir", "Main");
            if (sourceDir == "")
                sourceDir = ".";

            try
            {
                AlotVer = int.Parse(installerIni.Read("AlotVersion", "Main"));
            }
            catch (Exception)
            {
                AlotVer = 0;
            }

            try
            {
                MeuitmVer = int.Parse(installerIni.Read("MeuitmVersion", "Main"));
            }
            catch (Exception)
            {
                MeuitmVer = 0;
            }

            bool allowToSkip = false;
            string skip = installerIni.Read("AllowSkipCheck", "Main").ToLowerInvariant();
            if (skip == "true")
                allowToSkip = true;

            skip = installerIni.Read("AllowSkipScan", "Main").ToLowerInvariant();
            if (skip == "true")
                allowToSkipScan = true;

            string meuitm = installerIni.Read("MEUITM", "Main").ToLowerInvariant();
            if (gameId == 1 && (meuitm == "true" || MeuitmVer != 0))
                meuitmMode = true;
            if (meuitmMode && MeuitmVer == 0)
                MeuitmVer = 1;

            comboBoxMod0.Visible = comboBoxMod1.Visible = comboBoxMod2.Visible = comboBoxMod3.Visible = false;
            comboBoxMod4.Visible = comboBoxMod5.Visible = comboBoxMod6.Visible = comboBoxMod7.Visible = false;
            comboBoxMod8.Visible = comboBoxMod9.Visible = false;

            allMemMods = new List<string>();
            modsSelection = new List<ModSelection>();
            for (int i = 1; i <= 10; i++)
            {
                ModSelection modSelect = new ModSelection();
                modSelect.files = new List<string>();
                modSelect.descriptions = new List<string>();
                for (int l = 1; l <= 10; l++)
                {
                    string file = installerIni.Read("File" + l, "Mod" + i).ToLowerInvariant();
                    string description = installerIni.Read("Label" + l, "Mod" + i);
                    if (file == "" || description == "")
                        continue;
                    modSelect.files.Add(file);
                    modSelect.descriptions.Add(description);
                }
                if (modSelect.files.Count < 2)
                {
                    modSelect.files.Clear();
                    modSelect.descriptions.Clear();
                }
                modsSelection.Add(modSelect);
            }
            for (int i = 1; i <= modsSelection.Count; i++)
            {
                ModSelection modSelect = modsSelection[i - 1];
                for (int l = 1; l <= modSelect.files.Count; l++)
                {
                    allMemMods.Add(modSelect.files[l - 1]);
                    string description = modSelect.descriptions[l - 1];
                    switch (i)
                    {
                        case 1:
                            comboBoxMod0.Items.Add(description);
                            comboBoxMod0.Visible = true;
                            comboBoxMod0.SelectedIndex = 0;
                            break;
                        case 2:
                            comboBoxMod1.Items.Add(description);
                            comboBoxMod1.Visible = true;
                            comboBoxMod1.SelectedIndex = 0;
                            break;
                        case 3:
                            comboBoxMod2.Items.Add(description);
                            comboBoxMod2.Visible = true;
                            comboBoxMod2.SelectedIndex = 0;
                            break;
                        case 4:
                            comboBoxMod3.Items.Add(description);
                            comboBoxMod3.Visible = true;
                            comboBoxMod3.SelectedIndex = 0;
                            break;
                        case 5:
                            comboBoxMod4.Items.Add(description);
                            comboBoxMod4.Visible = true;
                            comboBoxMod4.SelectedIndex = 0;
                            break;
                        case 6:
                            comboBoxMod5.Items.Add(description);
                            comboBoxMod5.Visible = true;
                            comboBoxMod5.SelectedIndex = 0;
                            break;
                        case 7:
                            comboBoxMod6.Items.Add(description);
                            comboBoxMod6.Visible = true;
                            comboBoxMod6.SelectedIndex = 0;
                            break;
                        case 8:
                            comboBoxMod7.Items.Add(description);
                            comboBoxMod7.Visible = true;
                            comboBoxMod7.SelectedIndex = 0;
                            break;
                        case 9:
                            comboBoxMod8.Items.Add(description);
                            comboBoxMod8.Visible = true;
                            comboBoxMod8.SelectedIndex = 0;
                            break;
                        case 10:
                            comboBoxMod9.Items.Add(description);
                            comboBoxMod9.Visible = true;
                            comboBoxMod9.SelectedIndex = 0;
                            break;
                    }
                }
            }

            if (modsSelection.Count == 0)
                labelModsSelection.Visible = false;

            configIni = new ConfIni();

            customLabelDesc.Text = customLabelCurrentStatus.Text = customLabelFinalStatus.Text = "";


            if (gameId == 3)
            {
                OptionRepackVisible = checkBoxOptionRepack.Visible = labelOptionRepack.Visible = false;
            }
            else
            {
                OptionRepackVisible = checkBoxOptionRepack.Visible = labelOptionRepack.Visible = true;
            }
            if (gameId == 1)
            {
                OptionLimit2KVisible = checkBoxOptionLimit2K.Visible = labelOptionLimit2K.Visible = true;
            }
            else
            {
                OptionLimit2KVisible = checkBoxOptionLimit2K.Visible = labelOptionLimit2K.Visible = false;
            }

            if (allowToSkip)
            {
                OptionVanillaVisible = checkBoxOptionVanilla.Visible = labelOptionVanilla.Visible = true;
            }
            else
            {
                OptionVanillaVisible = checkBoxOptionVanilla.Visible = labelOptionVanilla.Visible = false;
            }
            if (allowToSkipScan)
            {
                OptionSkipScanVisible = checkBoxOptionSkipScan.Visible = labelOptionSkipScan.Visible = true;
            }
            else
            {
                OptionSkipScanVisible = checkBoxOptionSkipScan.Visible = labelOptionSkipScan.Visible = false;
            }
            checkBoxOptionVanilla.Checked = false;
            checkBoxOptionLimit2K.Checked = false;
            checkBoxOptionSkipScan.Checked = false;

            buttonSTART.Visible = true;
            buttonNormal.Visible = true;

            customLabelDesc.Parent = pictureBoxBG;
            customLabelFinalStatus.Parent = pictureBoxBG;
            customLabelCurrentStatus.Parent = pictureBoxBG;
            labelOptions.Parent = pictureBoxBG;
            labelOptionLimit2K.Parent = pictureBoxBG;
            labelOptionRepack.Parent = pictureBoxBG;
            labelOptionSkipScan.Parent = pictureBoxBG;
            labelOptionVanilla.Parent = pictureBoxBG;
            checkBoxOptionRepack.Parent = pictureBoxBG;
            checkBoxOptionLimit2K.Parent = pictureBoxBG;
            checkBoxOptionSkipScan.Parent = pictureBoxBG;
            checkBoxOptionVanilla.Parent = pictureBoxBG;
            buttonMute.Parent = pictureBoxBG;

            labelOptions.Visible = OptionVanillaVisible || OptionSkipScanVisible ||
                OptionRepackVisible || OptionLimit2KVisible;

            string bgFile = installerIni.Read("BackgroundImage", "Main").ToLowerInvariant();
            if (bgFile != "")
            {
                if (File.Exists(bgFile))
                {
                    try
                    {
                        pictureBoxBG.Image = new Bitmap(bgFile);
                    }
                    catch
                    {
                        pictureBoxBG.Image = null;
                    }
                }
            }
            if (pictureBoxBG.Image == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string res = assembly.GetName().Name + ".Resources.me" + gameId + "_bg.jpg";
                pictureBoxBG.Image = new Bitmap(assembly.GetManifestResourceStream(res));
            }

            softShadowsModPath = installerIni.Read("SoftShadowsMod", "Main").ToLowerInvariant();
            if (softShadowsModPath != "")
            {
                if (!File.Exists(softShadowsModPath) || Path.GetExtension(softShadowsModPath).ToLowerInvariant() != ".zip")
                {
                    softShadowsModPath = "";
                }
            }

            splashDemiurge = installerIni.Read("DemiurgeSplashVideo", "Main").ToLowerInvariant();
            if (splashDemiurge != "")
            {
                if (!File.Exists(splashDemiurge) || Path.GetExtension(splashDemiurge).ToLowerInvariant() != ".bik")
                {
                    splashDemiurge = "";
                }
            }

            splashBitmapPath = installerIni.Read("SplashBitmap", "Main").ToLowerInvariant();
            if (splashBitmapPath != "")
            {
                if (!File.Exists(splashBitmapPath) || Path.GetExtension(splashBitmapPath).ToLowerInvariant() != ".bmp")
                {
                    splashBitmapPath = "";
                }
            }

            MessageBox.Show("Before starting the installation,\nmake sure real time scanning is turned off.\n" +
                "Antivirus software can interfere with the install process.", "Warning !");

            string musicFile = installerIni.Read("MusicSource", "Main").ToLowerInvariant();
            if (musicFile != "" && File.Exists(musicFile))
            {
                try
                {
                    if (Path.GetExtension(musicFile).ToLowerInvariant() == ".mp3")
                    {
                        new System.Threading.Thread(delegate () {
                            try
                            {
                                byte[] srcBuffer = File.ReadAllBytes(musicFile);
                                byte[] wavBuffer = LibMadHelper.LibMad.Decompress(srcBuffer);
                                if (wavBuffer.Length != 0)
                                {
                                    MemoryStream wavStream = new MemoryStream(wavBuffer);
                                    musicPlayer = new System.Media.SoundPlayer(wavStream);
                                    musicPlayer.PlayLooping();
                                    Invoke(new Action(() => { buttonMute.Visible = true; }));
                                }
                            }
                            catch
                            {
                            }
                        }).Start();
                    }
                    else if (Path.GetExtension(musicFile).ToLowerInvariant() == ".wav")
                    {
                        musicPlayer = new System.Media.SoundPlayer(musicFile);
                        musicPlayer.PlayLooping();
                        buttonMute.Visible = true;
                    }
                }
                catch
                {
                }
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

        static public bool applyModTag(int gameId, int MeuitmV, int AlotV)
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
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Seek(-16, SeekOrigin.End);
                    int prevMeuitmV = fs.ReadInt32();
                    int prevAlotV = fs.ReadInt32();
                    int prevProductV = fs.ReadInt32();
                    uint memiTag = fs.ReadUInt32();
                    if (memiTag == MEMI_TAG)
                    {
                        if (prevProductV < 10 || prevProductV == 4352 || prevProductV == 16777472) // default before MEM v178
                            prevProductV = prevAlotV = prevMeuitmV = 0;
                    }
                    else
                        prevProductV = prevAlotV = prevMeuitmV = 0;
                    if (MeuitmV != 0)
                        prevMeuitmV = MeuitmV;
                    if (AlotV != 0)
                        prevAlotV = AlotV;
                    fs.WriteInt32(prevMeuitmV);
                    fs.WriteInt32(prevAlotV);
                    fs.WriteInt32((int)(prevProductV & 0xffff0000) | int.Parse(Application.ProductVersion));
                    fs.WriteUInt32(MEMI_TAG);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool installSoftShadowsMod(GameData gameData, string path)
        {
            IntPtr handle = IntPtr.Zero;
            int result;
            ulong numEntries = 0;
            string fileName = "";
            ulong dstLen = 0;
            ZlibHelper.Zip zip = new ZlibHelper.Zip();
            try
            {
                handle = zip.Open(path, ref numEntries, 0);
                if (handle == IntPtr.Zero)
                    throw new Exception();
                for (uint i = 0; i < numEntries; i++)
                {
                    result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                    if (result != 0)
                        throw new Exception();

                    byte[] data = new byte[dstLen];
                    result = zip.ReadCurrentFile(handle, data, dstLen);
                    if (result != 0)
                    {
                        throw new Exception();
                    }

                    string filePath = GameData.GamePath + "\\Engine\\Shaders\\" + fileName;
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    using (FileStream fs = new FileStream(filePath, FileMode.CreateNew))
                    {
                        fs.WriteFromBuffer(data);
                    }

                    zip.GoToNextFile(handle);
                }
            }
            catch
            {
                return false;
            }

            try
            {
                string cachePath = gameData.GameUserPath + "\\Published\\CookedPC\\LocalShaderCache-PC-D3D-SM3.upk";
                if (File.Exists(cachePath))
                    File.Delete(cachePath);
                cachePath = GameData.MainData + "\\LocalShaderCache-PC-D3D-SM3.upk";
                if (File.Exists(cachePath))
                    File.Delete(cachePath);
            }
            catch
            {
                return false;
            }


            return true;
        }

        private bool installSplashScreen(string path)
        {
            string filePath = GameData.bioGamePath + "\\Splash\\Splash.bmp";
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.Copy(path, filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool installSplashVideo(string path)
        {
            string filePath = GameData.MainData + "\\Movies\\db_standard.bik";
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.Copy(path, filePath);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool PreInstallCheck()
        {
            buttonsEnable(false);
            customLabelFinalStatus.Text = "Checking game setup...";
            Application.DoEvents();

            string filename = "errors-precheck.txt";
            if (File.Exists(filename))
                File.Delete(filename);

            ulong memorySize = ((new ComputerInfo().TotalPhysicalMemory / 1024 / 1024) + 1023) / 1024;
            if (memorySize < 4 && gameId == 3)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take many hours or fail.", "Installer");
            }
            else if (memorySize < 8 && gameId == 3)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take several hours.", "Installer");
            }
            else if (memorySize <= 4 && gameId != 3)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take a long time.", "Installer");
            }

            memFiles = Directory.GetFiles(sourceDir, "*.mem", SearchOption.AllDirectories).Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
            memFiles.Sort();
            if (memFiles.Count == 0)
            {
                customLabelFinalStatus.Text = "No MEM file mods found!, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);
                return false;
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

            if (errors != "")
            {
                customLabelFinalStatus.Text = "There are some errors while detecting MEM mods, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);

                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                Process.Start(filename);
                return false;
            }


            gameData = new GameData((MeType)gameId, configIni);
            if (!Directory.Exists(GameData.GamePath))
            {
                customLabelFinalStatus.Text = "Game path is wrong, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);
                return false;
            }
            if (!gameData.getPackages(true, true))
            {
                customLabelFinalStatus.Text = "Missing game data, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);
                return false;
            }
            if (gameId == (int)MeType.ME1_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Startup_int.upk"))
                {
                    customLabelFinalStatus.Text = "ME1 game not found, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    buttonsEnable(true);
                    return false;
                }
            }
            if (gameId == (int)MeType.ME2_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Textures.tfc"))
                {
                    customLabelFinalStatus.Text = "ME2 game not found, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    buttonsEnable(true);
                    return false;
                }
            }
            if (gameId == (int)MeType.ME3_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BIOGame\\PCConsoleTOC.bin"))
                {
                    customLabelFinalStatus.Text = "ME3 game not found, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    buttonsEnable(true);
                    return false;
                }
            }

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
                customLabelFinalStatus.Text = "Write access denied to game folders, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);
                return false;
            }


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
                customLabelFinalStatus.Text = "You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);
                return false;
            }


            List<string> mods = Misc.detectBrokenMod((MeType)gameId);
            if (mods.Count != 0)
            {
                errors = Environment.NewLine + "------- Detected not compatible mods --------" + Environment.NewLine + Environment.NewLine;
                for (int l = 0; l < mods.Count; l++)
                {
                    errors += mods[l] + Environment.NewLine;
                }
                errors += "---------------------------------------------" + Environment.NewLine + Environment.NewLine;
                errors += Environment.NewLine + Environment.NewLine;

                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                Process.Start(filename);

                customLabelFinalStatus.Text = "Detected not compatible mod, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                buttonsEnable(true);
                return false;
            }

            errors = "";
            if (detectMod(gameId))
            {
                updateMode = true;
                checkBoxOptionVanilla.CheckedChanged -= checkBoxOptionVanilla_CheckedChanged;
                checkBoxOptionVanilla.Checked = true;
                checkBoxOptionVanilla.CheckedChanged += checkBoxOptionVanilla_CheckedChanged;
                checkBoxOptionRepack.Checked = false;
                OptionVanillaVisible = false;
                OptionSkipScanVisible = false;
                OptionRepackVisible = false;
            }

            // unpack DLC
            if (gameId != 3)
                totalStages -= 1;
            // check game files
            if ((updateMode || checkBoxOptionVanilla.Checked) || checkBoxOptionSkipScan.Checked)
                totalStages -= 1;

            // scan textures, remove mipmaps
            if (updateMode || checkBoxOptionSkipScan.Checked)
                totalStages -= 2;
            // recompress game files
            if (!checkBoxOptionRepack.Checked || updateMode || checkBoxOptionSkipScan.Checked)
                totalStages -= 1;

            if (GameData.gameType == MeType.ME3_TYPE)
            {
                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                ME3DLC.unpackAllDLC(null, this);
                gameData.getPackages(true, true);
            }

            if (updateMode || checkBoxOptionSkipScan.Checked)
            {
                checkBoxOptionVanilla.CheckedChanged -= checkBoxOptionVanilla_CheckedChanged;
                checkBoxOptionVanilla.Checked = true;
                checkBoxOptionVanilla.CheckedChanged += checkBoxOptionVanilla_CheckedChanged;
                string mapPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name);
                string mapFile = Path.Combine(mapPath, "me" + gameId + "map.bin");

                if (!File.Exists(mapFile))
                {
                    customLabelFinalStatus.Text = "Game was not scanned for textures, can not continue, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    buttonsEnable(true);
                    return false;
                }

                if (!loadTexturesMap(mapFile))
                {
                    customLabelFinalStatus.Text = "Game inconsistent from previous scan! Reinstall ME" + gameId + " and restart.";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    buttonsEnable(true);
                    return false;
                }

                if (!MipMaps.verifyGameDataEmptyMipMapsRemoval())
                {
                    customLabelFinalStatus.Text = "Game doesn't have empty mips removed, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    buttonsEnable(true);
                    return false;
                }
            }
            else
            {
                if (!checkBoxOptionVanilla.Checked)
                {
                    customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                    List<string> modList = new List<string>();
                    bool vanilla = Misc.checkGameFiles((MeType)gameId, ref errors, ref modList, null, this, Misc.generateModsMd5Entries);
                    updateLabelPreVanilla("");
                    if (modList.Count != 0)
                    {
                        FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                        fs.SeekEnd();
                        fs.WriteStringASCII(Environment.NewLine + "------- Detected mods --------" + Environment.NewLine);
                        for (int l = 0; l < modList.Count; l++)
                        {
                            fs.WriteStringASCII(modList[l] + Environment.NewLine);
                        }
                        fs.WriteStringASCII("------------------------------" + Environment.NewLine + Environment.NewLine);
                        fs.Close();
                    }

                    if (!vanilla)
                    {
                        FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                        fs.SeekEnd();
                        fs.WriteStringASCII("===========================================================================" + Environment.NewLine);
                        fs.WriteStringASCII("WARNING: looks like the following file(s) are not vanilla or not recognized" + Environment.NewLine);
                        fs.WriteStringASCII("===========================================================================" + Environment.NewLine + Environment.NewLine);
                        fs.WriteStringASCII(errors);
                        fs.Close();
                        Process.Start(filename);
                        customLabelFinalStatus.Text = "Game files are not vanilla or not recognized";
                        customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                        if (gameId == 3)
                        {
                            string message = "The installer detected that the following game files\n" +
                                "are modded (not vanilla) or not recognized by the installer.\n" +
                                "You can find the list of files in the window that just opened.\n\n" +
                                "The correct installation order is as follows:\n" +
                                "1. Content mods (PCC, DLC mods)\n";
                            message += "2. ALOT & ALOT Addon\n";
                            message += "3. Texture and meshes mods (TPF, DDS, MOD)\n\n" +
                                "- If you have properly installed content mods before this mod,\n" +
                                "  this result is normal and you can continue the installation.\n" +
                                "  It's advised to verify if all items in the list are supposed to be modded.\n" +
                                "  To verify: compare the list of files that failed the check against\n" +
                                "  the list of files you copied\n" +
                                "  from your content mods to the CookedPCConsole directory.\n" +
                                "  Both lists should be identical.\n\n" +
                                "- If you are not sure what you installed,\n" +
                                "  it is recommended that you revert your game to vanilla\n" +
                                "  and optionally install the content mods (PCC, DLC mods) you want,\n" +
                                "  then restart the installation of this mod.\n\n\n" +
                                "However you can ignore the warning and continue the installation at your own risk!\n";
                            MessageBox.Show(message, "Warning !");
                        }
                        else
                        {
                            string message = "The installer detected that the following game files\n" +
                                "are modded (not vanilla) or not recognized by the installer.\n" +
                                "You can find the list of files in the window that just opened.\n\n" +
                                "- If you are not sure what you installed,\n" +
                                "  it is recommended that you revert your game to vanilla\n" +
                                "  and optionally install the content mods (PCC, DLC mods) you want,\n" +
                                "  then restart the installation of this mod.\n\n" +
                                "- If the installer still reports this issue,\n" +
                                "  do not install unrecognized mod files\n" +
                                "  and submit a report to add those files to the list of supported mods.\n\n\n" +
                                "However you can ignore the warning and continue the installation at your own risk!\n";
                            MessageBox.Show(message, "Warning !");
                        }
                        DialogResult resp = MessageBox.Show("Press Cancel to abort or press Ok button to continue.", "Warning !", MessageBoxButtons.OKCancel);
                        if (resp == DialogResult.Cancel)
                            return false;
                    }
                }
            }

            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist && gameId == 1)
            {
                MessageBox.Show("Missing game configuration file.\nYou need atleast once launch the game first.");
                return false;
            }

            return true;
        }

        private bool loadTexturesMap(string mapPath)
        {
            textures = new List<FoundTexture>();

            if (!File.Exists(mapPath))
                return false;

            using (FileStream fs = new FileStream(mapPath, FileMode.Open, FileAccess.Read))
            {
                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != TexExplorer.textureMapBinTag || version != TexExplorer.textureMapBinVersion)
                {
                    errors += "Detected wrong or old version of textures scan file!" + Environment.NewLine;
                    log += "Detected wrong or old version of textures scan file!" + Environment.NewLine;
                    return false;
                }

                uint countTexture = fs.ReadUInt32();
                for (int i = 0; i < countTexture; i++)
                {
                    FoundTexture texture = new FoundTexture();
                    int len = fs.ReadInt32();
                    texture.name = fs.ReadStringASCII(len);
                    texture.crc = fs.ReadUInt32();
                    uint countPackages = fs.ReadUInt32();
                    texture.list = new List<MatchedTexture>();
                    for (int k = 0; k < countPackages; k++)
                    {
                        MatchedTexture matched = new MatchedTexture();
                        matched.exportID = fs.ReadInt32();
                        matched.linkToMaster = fs.ReadInt32();
                        len = fs.ReadInt32();
                        matched.path = fs.ReadStringASCII(len);
                        texture.list.Add(matched);
                    }
                    textures.Add(texture);
                }

                List<string> packages = new List<string>();
                int numPackages = fs.ReadInt32();
                for (int i = 0; i < numPackages; i++)
                {
                    int len = fs.ReadInt32();
                    string pkgPath = fs.ReadStringASCII(len);
                    pkgPath = GameData.GamePath + pkgPath;
                    packages.Add(pkgPath);
                }
                for (int i = 0; i < packages.Count; i++)
                {
                    if (GameData.packageFiles.Find(s => s.Equals(packages[i], StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        errors += "Detected removal of game files since last game data scan." + Environment.NewLine + Environment.NewLine;
                        log += "Detected removal of game files since last game data scan." + Environment.NewLine + Environment.NewLine;
                        return false;
                    }
                }
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    if (packages.Find(s => s.Equals(GameData.packageFiles[i], StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        errors += "Detected additional game files not present in latest game data scan." + Environment.NewLine + Environment.NewLine;
                        log += "Detected additional game files not present in latest game data scan." + Environment.NewLine + Environment.NewLine;
                        return false;
                    }
                }
            }

            return true;
        }

        public void applyModules()
        {
            int totalNumberOfMods = 0;
            int currentNumberOfTotalMods = 1;

            for (int i = 0; i < memFiles.Count; i++)
            {
                if (memFiles[i].EndsWith(".mem", StringComparison.OrdinalIgnoreCase))
                {
                    using (FileStream fs = new FileStream(memFiles[i], FileMode.Open, FileAccess.Read))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                            continue;
                        fs.JumpTo(fs.ReadInt64());
                        fs.SkipInt32();
                        totalNumberOfMods += fs.ReadInt32();
                    }
                }
                else
                    throw new Exception();
            }

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
                    for (int l = 0; l < numFiles; l++, currentNumberOfTotalMods++)
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

                        updateStatusTextures("Installing textures " + (currentNumberOfTotalMods * 100 / totalNumberOfMods) + "%");

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
                                errors += mipMaps.replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, false);
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
            if (gameId == 3)
            {
                checkBoxOptionRepack.Visible = labelOptionRepack.Visible = false;
            }
            if (gameId == 1)
            {
                checkBoxOptionLimit2K.Visible = labelOptionLimit2K.Visible = true;
            }
            else
            {
                checkBoxOptionLimit2K.Visible = labelOptionLimit2K.Visible = false;
            }

            Application.DoEvents();
        }

        private void buttonsEnable(bool enabled)
        {
            buttonNormal.Visible = false;
            checkBoxOptionRepack.Enabled = OptionRepackVisible;
            if (updateMode)
            {
                checkBoxOptionVanilla.Enabled = false;
                checkBoxOptionSkipScan.Enabled = false;
            }
            else
            {
                checkBoxOptionVanilla.Enabled = OptionVanillaVisible;
                checkBoxOptionSkipScan.Enabled = OptionSkipScanVisible;
            }
            checkBoxOptionLimit2K.Enabled = OptionLimit2KVisible;
            Application.DoEvents();
        }

        private void buttonSTART_Click(object sender, EventArgs e)
        {
            List<string> selectedFileMods = new List<string>();
            for (int i = 1; i <= modsSelection.Count; i++)
            {
                ModSelection modSelect = modsSelection[i - 1];
                string file = "";
                switch (i)
                {
                    case 1:
                        file = modSelect.files[comboBoxMod0.SelectedIndex];
                        break;
                    case 2:
                        file = modSelect.files[comboBoxMod1.SelectedIndex];
                        break;
                    case 3:
                        file = modSelect.files[comboBoxMod2.SelectedIndex];
                        break;
                    case 4:
                        file = modSelect.files[comboBoxMod3.SelectedIndex];
                        break;
                    case 5:
                        file = modSelect.files[comboBoxMod4.SelectedIndex];
                        break;
                    case 6:
                        file = modSelect.files[comboBoxMod5.SelectedIndex];
                        break;
                    case 7:
                        file = modSelect.files[comboBoxMod6.SelectedIndex];
                        break;
                    case 8:
                        file = modSelect.files[comboBoxMod7.SelectedIndex];
                        break;
                    case 9:
                        file = modSelect.files[comboBoxMod8.SelectedIndex];
                        break;
                    case 10:
                        file = modSelect.files[comboBoxMod9.SelectedIndex];
                        break;
                }
                selectedFileMods.Add(file);
            }

            buttonsEnable(false);
            buttonSTART.Visible = false;
            checkBoxOptionVanilla.Visible = labelOptionVanilla.Visible = false;
            checkBoxOptionSkipScan.Visible = labelOptionSkipScan.Visible = false;
            checkBoxOptionRepack.Visible = labelOptionRepack.Visible = false;
            checkBoxOptionLimit2K.Visible = labelOptionLimit2K.Visible = false;
            labelOptions.Visible = false;
            if (meuitmMode)
                customLabelDesc.Text = "Installing MEUITM for Mass Effect";
            else
                customLabelDesc.Text = "Installing ALOT for Mass Effect " + gameId;

            comboBoxMod0.Visible = comboBoxMod1.Visible = comboBoxMod2.Visible = comboBoxMod3.Visible = false;
            comboBoxMod4.Visible = comboBoxMod5.Visible = comboBoxMod6.Visible = comboBoxMod7.Visible = false;
            comboBoxMod8.Visible = comboBoxMod9.Visible = false;
            labelModsSelection.Visible = false;

            if (!PreInstallCheck())
            {
                labelOptionVanilla.Visible = checkBoxOptionVanilla.Visible = OptionVanillaVisible;
                labelOptionSkipScan.Visible = checkBoxOptionSkipScan.Visible = OptionSkipScanVisible;
                labelOptionRepack.Visible = checkBoxOptionRepack.Visible = OptionRepackVisible;
                labelOptionLimit2K.Visible = checkBoxOptionLimit2K.Visible = OptionLimit2KVisible;
                labelOptions.Visible = checkBoxOptionVanilla.Visible || checkBoxOptionSkipScan.Visible ||
                    checkBoxOptionRepack.Visible || checkBoxOptionLimit2K.Visible;
                customLabelDesc.Text = "";

                for (int i = 1; i <= modsSelection.Count; i++)
                {
                    ModSelection modSelect = modsSelection[i - 1];
                    string file = "";
                    switch (i)
                    {
                        case 1:
                            comboBoxMod0.Visible = true;
                            break;
                        case 2:
                            comboBoxMod1.Visible = true;
                            break;
                        case 3:
                            comboBoxMod2.Visible = true;
                            break;
                        case 4:
                            comboBoxMod3.Visible = true;
                            break;
                        case 5:
                            comboBoxMod4.Visible = true;
                            break;
                        case 6:
                            comboBoxMod5.Visible = true;
                            break;
                        case 7:
                            comboBoxMod6.Visible = true;
                            break;
                        case 8:
                            comboBoxMod7.Visible = true;
                            break;
                        case 9:
                            comboBoxMod8.Visible = true;
                            break;
                        case 10:
                            comboBoxMod9.Visible = true;
                            break;
                    }
                    selectedFileMods.Add(file);
                }

                if (modsSelection.Count != 0)
                    labelModsSelection.Visible = true;

                return;
            }

            for (int i = 0; i < selectedFileMods.Count; i++)
            {
                allMemMods.Remove(selectedFileMods[i]);
            }
            for (int i = 0; i < allMemMods.Count; i++)
            {
                memFiles.Remove(Path.GetFileName(allMemMods[i]).ToLowerInvariant());
            }

            customLabelFinalStatus.Text = "";
            customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.White);

            errors = "";
            log = "";
            Misc.startTimer();

            if (!updateMode && !checkBoxOptionSkipScan.Checked)
            {
                log += "Prepare game data started..." + Environment.NewLine;
                if (GameData.gameType == MeType.ME1_TYPE)
                    Misc.VerifyME1Exe(gameData, false);

                if (GameData.gameType != MeType.ME1_TYPE)
                    gameData.getTfcTextures();

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

                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;

                log += "Scan textures started..." + Environment.NewLine;
                errors += treeScan.PrepareListOfTextures(null, null, null, this, ref log, true);
                textures = treeScan.treeScan;
                log += "Scan textures finished" + Environment.NewLine + Environment.NewLine;
            }
            else
            {
                if (GameData.gameType != MeType.ME1_TYPE)
                    gameData.getTfcTextures();

                log += "Prepare game data skipped" + Environment.NewLine + Environment.NewLine;
                log += "Scan textures skipped" + Environment.NewLine + Environment.NewLine;
            }

            customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
            log += "Process textures started..." + Environment.NewLine;
            applyModules();
            log += "Process textures finished" + Environment.NewLine + Environment.NewLine;


            customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
            cachePackageMgr.CloseAllWithSave(checkBoxOptionRepack.Checked);


            if (!updateMode && !checkBoxOptionSkipScan.Checked)
            {
                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                if (GameData.gameType == MeType.ME1_TYPE)
                {
                    log += "Remove mipmaps started..." + Environment.NewLine;
                    errors += mipMaps.removeMipMapsME1(1, textures, null, null, this, checkBoxOptionRepack.Checked);
                    errors += mipMaps.removeMipMapsME1(2, textures, null, null, this, checkBoxOptionRepack.Checked);
                    log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
                }
                else
                {
                    log += "Remove mipmaps started..." + Environment.NewLine;
                    errors += mipMaps.removeMipMapsME2ME3(textures, null, null, this, checkBoxOptionRepack.Checked);
                    log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
                }
            }
            else
            {
                log += "Remove mipmaps skipped" + Environment.NewLine + Environment.NewLine;
            }

            log += "Updating LODs and other settings started..." + Environment.NewLine;
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD((MeType)gameId, engineConf, checkBoxOptionLimit2K.Checked);
            LODSettings.updateGFXSettings((MeType)gameId, engineConf, softShadowsModPath != "", meuitmMode);
            log += "Updating LODs and other settings finished" + Environment.NewLine + Environment.NewLine;


            if (checkBoxOptionRepack.Checked)
            {
                if (!updateMode && !checkBoxOptionSkipScan.Checked)
                {
                    customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                    log += "Repack started..." + Environment.NewLine;
                    for (int i = 0; i < GameData.packageFiles.Count; i++)
                    {
                        updateStatusRepackZlib("Recompress game files " + ((i + 1) * 100 / GameData.packageFiles.Count) + "%");
                        Package package = new Package(GameData.packageFiles[i], true, true);
                        if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                        {
                            package.Dispose();
                            package = new Package(GameData.packageFiles[i]);
                            package.SaveToFile(true);
                        }
                    }
                    log += "Repack finished" + Environment.NewLine + Environment.NewLine;
                }
                else
                {
                    log += "Repack skipped" + Environment.NewLine + Environment.NewLine;
                }
            }


            if (!applyModTag(gameId, MeuitmVer, AlotVer))
                errors += "Failed applying stamp for installation!\n";

            if (meuitmMode && softShadowsModPath != "")
            {
                if (installSoftShadowsMod(gameData, softShadowsModPath))
                    log += "Soft Shadows mod installed.";
                else
                {
                    log += "Soft Shadows mod failed to install!";
                    errors += "Soft Shadows mod failed to install!";
                }
            }

            if (meuitmMode && splashBitmapPath != "")
            {
                if (installSplashScreen(splashBitmapPath))
                    log += "Splash screen mod installed.";
                else
                {
                    log += "Splash mod failed to install!";
                    errors += "Splash mod failed to install!";
                }
            }

            if (meuitmMode && splashDemiurge != "")
            {
                if (installSplashVideo(splashDemiurge))
                    log += "Splash video mod installed.";
                else
                {
                    log += "Splash video mod failed to install!";
                    errors += "Splash video mod failed to install!";
                }
            }

            var time = Misc.stopTimer();
            log += "Installation finished. Process total time: " + Misc.getTimerFormat(time) + Environment.NewLine;
            customLabelFinalStatus.Text = "Installation finished.";
            customLabelCurrentStatus.Text = "";
            customLabelCurrentStatus.ForeColor = Color.FromKnownColor(KnownColor.White);
            customLabelDesc.Text = "";
            buttonNormal.Visible = true;

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
                    fs.WriteStringASCII("================================================================================" + Environment.NewLine);
                    fs.WriteStringASCII("WARNING: Some textures couldn't be installed." + Environment.NewLine);
                    fs.WriteStringASCII("This is not necessarily a problem, as it will happen if you do not own all DLCs." + Environment.NewLine);
                    fs.WriteStringASCII("If that is your case, you can safely ignore the errors below. " + Environment.NewLine);
                    fs.WriteStringASCII("If you have all DLCs but still see these errors, " + Environment.NewLine);
                    fs.WriteStringASCII("you should redownload your game and install your mods again." + Environment.NewLine);
                    fs.WriteStringASCII("================================================================================" + Environment.NewLine + Environment.NewLine);
                    fs.WriteStringASCII(errors);
                }
                customLabelFinalStatus.Text = "WARNING: Some errors have occured!";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                Process.Start(filename);
            }
        }

        public void updateLabelPreVanilla(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateStatusPrepare(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateStatusScan(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateStatusMipMaps(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateStatusTextures(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateStatusStore(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateStatusRepackZlib(string text)
        {
            customLabelCurrentStatus.Text = text;
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

        private void checkBoxOptionSkipScan_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOptionSkipScan.Checked)
            {
                if (MessageBox.Show("This option is for advanced users !\n\n" +
                    "Disabling the scan of the game files may leads to various potential issues.\n\n" +
                    "If you are not sure press 'Cancel'", "Warning !", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                {
                    checkBoxOptionSkipScan.CheckedChanged -= checkBoxOptionSkipScan_CheckedChanged;
                    checkBoxOptionSkipScan.Checked = false;
                    checkBoxOptionSkipScan.CheckedChanged += checkBoxOptionSkipScan_CheckedChanged;
                }
                else
                {
                    checkBoxOptionVanilla.CheckedChanged -= checkBoxOptionVanilla_CheckedChanged;
                    checkBoxOptionVanilla.Checked = true;
                    checkBoxOptionVanilla.CheckedChanged += checkBoxOptionVanilla_CheckedChanged;
                }
            }
            else
            {
                checkBoxOptionSkipScan.CheckedChanged -= checkBoxOptionSkipScan_CheckedChanged;
                checkBoxOptionSkipScan.Checked = false;
                checkBoxOptionSkipScan.CheckedChanged += checkBoxOptionSkipScan_CheckedChanged;
                checkBoxOptionVanilla.CheckedChanged -= checkBoxOptionVanilla_CheckedChanged;
                checkBoxOptionVanilla.Checked = false;
                checkBoxOptionVanilla.CheckedChanged += checkBoxOptionVanilla_CheckedChanged;
            }
        }

        private void buttonMute_Click(object sender, EventArgs e)
        {
            if (musicPlayer != null)
            {
                buttonMute.Visible = true;
                if (mute)
                {
                    buttonMute.ImageIndex = 0;
                    mute = false;
                    musicPlayer.PlayLooping();
                }
                else
                {
                    buttonMute.ImageIndex = 1;
                    mute = true;
                    musicPlayer.Stop();
                }
            }
        }
    }
}
