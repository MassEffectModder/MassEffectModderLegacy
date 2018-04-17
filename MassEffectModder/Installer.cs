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
        int gameId = 1;
        GameData gameData;
        public List<string> memFiles;
        CachePackageMgr cachePackageMgr;
        List<FoundTexture> textures;
        MipMaps mipMaps;
        TreeScan treeScan;
        bool updateMode;
        bool unpackDLC;
        string errors = "";
        string log = "";
        int MeuitmVer;
        string softShadowsModPath;
        string splashDemiurge;
        string splashBitmapPath;
        string reshadePath;
        string indirectSoundPath;
        bool meuitmMode = false;
        bool OptionRepackVisible;
        bool OptionIndirectSoundVisible;
        bool OptionReshadeVisible;
        bool OptionBikVisible;
        bool mute = false;
        int stage = 1;
        int totalStages = 6;
        System.Media.SoundPlayer musicPlayer;
        CustomLabel customLabelDesc;
        CustomLabel customLabelCurrentStatus;
        CustomLabel customLabelFinalStatus;
        static public List<string> pkgsToRepack = null;
        static public List<string> pkgsToMarker = null;

        public Installer()
        {
            InitializeComponent();
            Text = "MEM Installer v" + Application.ProductVersion;
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
            if (runAsAdmin)
                MessageBox.Show("The Installer should be run as standard user to avoid (user account) issues.\n" +
                    "The installer will ask for administrative rights when necessary.");

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
                Text += " for ME" + gameId;

            if (runAsAdmin)
                Text += " (run as Administrator)";

            try
            {
                MeuitmVer = int.Parse(installerIni.Read("MeuitmVersion", "Main"));
            }
            catch (Exception)
            {
                MeuitmVer = 0;
            }

            string meuitm = installerIni.Read("MEUITM", "Main").ToLowerInvariant();
            if (gameId == 1 && (meuitm == "true" || MeuitmVer != 0))
                meuitmMode = true;
            if (meuitmMode && MeuitmVer == 0)
                MeuitmVer = 1;

            indirectSoundPath = installerIni.Read("IndirectSound", "Main").ToLowerInvariant();
            if (indirectSoundPath != "")
            {
                if (!File.Exists(indirectSoundPath) || Path.GetExtension(indirectSoundPath).ToLowerInvariant() != ".zip")
                {
                    indirectSoundPath = "";
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

            reshadePath = installerIni.Read("ReShade", "Main").ToLowerInvariant();
            if (reshadePath != "")
            {
                if (!File.Exists(reshadePath) || Path.GetExtension(reshadePath).ToLowerInvariant() != ".zip")
                {
                    reshadePath = "";
                }
            }

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
                    continue;
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

            if (gameId == 2 || gameId == 3)
                OptionRepackVisible = checkBoxOptionRepack.Visible = labelOptionRepack.Visible = true;
            else
                OptionRepackVisible = checkBoxOptionRepack.Visible = labelOptionRepack.Visible = false;
            if (gameId == 1 && splashDemiurge != "")
                OptionBikVisible = checkBoxOptionBik.Visible = labelOptionBik.Visible = true;
            else
                OptionBikVisible = checkBoxOptionBik.Visible = labelOptionBik.Visible = false;
            if (gameId == 1 && indirectSoundPath != "")
                OptionIndirectSoundVisible = checkBoxOptionIndirectSound.Visible = labelOptionIndirectSound.Visible = true;
            else
                OptionIndirectSoundVisible = checkBoxOptionIndirectSound.Visible = labelOptionIndirectSound.Visible = false;
            if (gameId == 1 && reshadePath != "")
                OptionReshadeVisible = checkBoxOptionReshade.Visible = labelOptionReshade.Visible = true;
            else
                OptionReshadeVisible = checkBoxOptionReshade.Visible = labelOptionReshade.Visible = false;

            if (gameId == 1)
                checkBoxOptionIndirectSound.Checked = true;
            checkBoxOptionReshade.Checked = false;
            checkBoxOptionBik.Checked = false;

            buttonSTART.Visible = true;
            buttonNormal.Visible = true;

            customLabelDesc.Parent = pictureBoxBG;
            customLabelFinalStatus.Parent = pictureBoxBG;
            customLabelCurrentStatus.Parent = pictureBoxBG;
            labelOptions.Parent = pictureBoxBG;
            labelOptionRepack.Parent = pictureBoxBG;
            labelOptionIndirectSound.Parent = pictureBoxBG;
            labelOptionReshade.Parent = pictureBoxBG;
            labelOptionBik.Parent = pictureBoxBG;
            checkBoxOptionRepack.Parent = pictureBoxBG;
            checkBoxOptionIndirectSound.Parent = pictureBoxBG;
            checkBoxOptionReshade.Parent = pictureBoxBG;
            checkBoxOptionBik.Parent = pictureBoxBG;
            labelModsSelection.Parent = pictureBoxBG;
            comboBoxMod0.Parent = comboBoxMod1.Parent = comboBoxMod2.Parent = comboBoxMod3.Parent = comboBoxMod4.Parent = pictureBoxBG;
            comboBoxMod5.Parent = comboBoxMod6.Parent = comboBoxMod7.Parent = comboBoxMod8.Parent = comboBoxMod9.Parent = pictureBoxBG;
            buttonMute.Parent = pictureBoxBG;

            labelOptions.Visible = OptionRepackVisible || OptionReshadeVisible || OptionIndirectSoundVisible || OptionBikVisible;

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

            string msg = "Before starting the installation,\nmake sure real time scanning is turned off.\n" +
                "Antivirus software can interfere with the install process\n" +
                "and crash the installer.\n\n";
            if (gameId == 1)
                msg += "Antivirus software can also add MassEffect.exe into the blocked list.";
            MessageBox.Show(msg, "Warning !");

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

        bool detectMod(int gameId, ref bool allowInstall)
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
                    fs.Seek(-16, SeekOrigin.End);
                    int prevMeuitmV = fs.ReadInt32();
                    int prevAlotV = fs.ReadInt32();
                    int prevProductV = fs.ReadInt32();
                    uint memiTag = fs.ReadUInt32();
                    if (memiTag == MEMI_TAG)
                    {
                        if (prevProductV < 10 || prevProductV == 4352 || prevProductV == 16777472) // default before MEM v178
                            prevProductV = prevAlotV = prevMeuitmV = 0;
                        return true;
                    }
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

        static public void AddMarkers(MeType gameType)
        {
            for (int i = 0; i < pkgsToMarker.Count; i++)
            {
                try
                {
                    using (FileStream fs = new FileStream(pkgsToMarker[i], FileMode.Open, FileAccess.ReadWrite))
                    {
                        fs.SeekEnd();
                        fs.Seek(-Package.MEMendFileMarker.Length, SeekOrigin.Current);
                        string marker = fs.ReadStringASCII(Package.MEMendFileMarker.Length);
                        if (marker != Package.MEMendFileMarker)
                        {
                            fs.SeekEnd();
                            fs.WriteStringASCII(Package.MEMendFileMarker);
                        }
                    }
                }
                catch
                {
                }
            }
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

        private bool installIndirectSoundPath(string path)
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
                    if (fileName.ToLowerInvariant() != "dsound.dll" &&
                        fileName.ToLowerInvariant() != "dsound.ini")
                    {
                        continue;
                    }
                    byte[] data = new byte[dstLen];
                    result = zip.ReadCurrentFile(handle, data, dstLen);
                    if (result != 0)
                    {
                        throw new Exception();
                    }

                    string filePath = GameData.GamePath + "\\Binaries\\" + fileName;
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

            return true;
        }

        private bool installReshadePath(string path)
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
                    fileName = fileName.Replace('/', '\\');
                    string filePath = GameData.GamePath + "\\Binaries\\" + fileName;
                    if (filePath.EndsWith("\\"))
                    {
                        if (!Directory.Exists(filePath))
                            Directory.CreateDirectory(filePath);
                        zip.GoToNextFile(handle);
                        continue;
                    }
                    if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    byte[] data = new byte[dstLen];
                    result = zip.ReadCurrentFile(handle, data, dstLen);
                    if (result != 0)
                    {
                        throw new Exception();
                    }

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

            if (File.Exists(GameData.GamePath + "\\Binaries\\d3d9.ini"))
            {
                try
                {
                    ConfIni shaderConf = new ConfIni(GameData.GamePath + "\\Binaries\\d3d9.ini");
                    shaderConf.Write("TextureSearchPaths", GameData.GamePath + "\\Binaries\\reshade-shaders\\Textures", "GENERAL");
                    shaderConf.Write("EffectSearchPaths", GameData.GamePath + "\\Binaries\\reshade-shaders\\Shaders", "GENERAL");
                    shaderConf.Write("PresetFiles", GameData.GamePath + "\\Binaries\\MassEffect.ini", "GENERAL");
                }
                catch
                {
                }
            }

            return true;
        }

        private bool PreInstallCheck()
        {
            customLabelFinalStatus.Text = "Checking game setup...";
            Application.DoEvents();

            string filename = "errors-precheck.txt";
            if (File.Exists(filename))
                File.Delete(filename);

            ulong memorySize = ((new ComputerInfo().TotalPhysicalMemory / 1024 / 1024) + 1023) / 1024;
            if (memorySize < 8)
            {
                MessageBox.Show("Detected small amount of physical RAM (8GB is recommended).\nInstallation may take a long time.", "Installer");
            }

            memFiles = Directory.GetFiles(".", "*.mem", SearchOption.AllDirectories).Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
            memFiles.Sort();
            if (memFiles.Count == 0)
            {
                customLabelFinalStatus.Text = "No MEM file mods found!, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
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
                    if (tag != TreeScan.TextureModTag || version != TreeScan.TextureModVersion)
                    {
                        if (version != TreeScan.TextureModVersion)
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
                            errors += "File " + memFiles[i] + " is not a MEM mod valid for ME" + gameId + ", skipping..." + Environment.NewLine;
                            continue;
                        }
                    }
                }
            }

            if (errors != "")
            {
                customLabelFinalStatus.Text = "There are some errors while detecting MEM mods, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);

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
                return false;
            }
            if (!gameData.getPackages(true, true))
            {
                customLabelFinalStatus.Text = "Missing game data, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                return false;
            }
            if (gameId == (int)MeType.ME1_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Startup_int.upk"))
                {
                    customLabelFinalStatus.Text = "ME1 game not found, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
                }
            }
            if (gameId == (int)MeType.ME2_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BioGame\\CookedPC\\Textures.tfc"))
                {
                    customLabelFinalStatus.Text = "ME2 game not found, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
                }
            }
            if (gameId == (int)MeType.ME3_TYPE)
            {
                if (!File.Exists(GameData.GamePath + "\\BIOGame\\PCConsoleTOC.bin"))
                {
                    customLabelFinalStatus.Text = "ME3 game not found, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
                }
            }

            bool writeAccess = Misc.CheckAndCorrectAccessToGame((MeType)gameId);
            if (!writeAccess)
            {
                customLabelFinalStatus.Text = "Write access denied, aborting...";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                return false;
            }


            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;

            for (int i = 0; i < memFiles.Count; i++)
            {
                diskUsage += new FileInfo(memFiles[i]).Length;
            }
            diskUsage = (long)(diskUsage * 2.5);

            unpackDLC = false;
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
                    if (sfarFiles.Count != 0)
                        unpackDLC = true;
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
                return false;
            }


            List<string> mods = Misc.detectMods((MeType)gameId);
            if (mods.Count != 0 && gameId == 1 && GameData.FullScanME1Game)
            {
                errors = Environment.NewLine + "------- Detected NOT supported mods with this version of game --------" + Environment.NewLine + Environment.NewLine;
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

                DialogResult resp = MessageBox.Show("Detected NOT compatible/supported mods with this version of game!" +
                    "\n\nPress Cancel to abort or press Ok button to continue.", "Warning !", MessageBoxButtons.OKCancel);
                if (resp == DialogResult.Cancel)
                {
                    customLabelFinalStatus.Text = "Detected NOT supported mod...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
                }
            }

            List<string> brokenMods = Misc.detectBrokenMod((MeType)gameId);
            if (brokenMods.Count != 0)
            {
                errors = Environment.NewLine + "------- Detected not compatible mods --------" + Environment.NewLine + Environment.NewLine;
                for (int l = 0; l < brokenMods.Count; l++)
                {
                    errors += brokenMods[l] + Environment.NewLine;
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
                return false;
            }

            errors = "";
            bool allowInstall = false;
            if (detectMod(gameId, ref allowInstall))
            {
                if (!allowInstall)
                {
                    customLabelFinalStatus.Text = "Not compatible previous installation, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
                }
                updateMode = true;
            }

            // unpack DLC
            if (gameId != 3 || !unpackDLC || (updateMode && gameId == 3))
                totalStages -= 1;

            // scan textures && remove empty mipmaps
            if (updateMode)
                totalStages -= 2;

            // recompress game files
            if (!checkBoxOptionRepack.Checked)
                totalStages -= 1;

            if (updateMode)
            {
                string mapPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Assembly.GetExecutingAssembly().GetName().Name);
                string mapFile = Path.Combine(mapPath, "me" + gameId + "map.bin");

                if (!File.Exists(mapFile))
                {
                    customLabelFinalStatus.Text = "Game was not scanned for textures, can not continue, aborting...";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
                }

                if (!loadTexturesMap(mapFile))
                {
                    customLabelFinalStatus.Text = "Game inconsistent from previous scan! Reinstall ME" + gameId + " and restart.";
                    customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                    return false;
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
                if (tag != TreeScan.textureMapBinTag || version != TreeScan.textureMapBinVersion)
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
                        if (tag != TreeScan.TextureModTag || version != TreeScan.TextureModVersion)
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
                    if (tag != TreeScan.TextureModTag || version != TreeScan.TextureModVersion)
                    {
                        if (version != TreeScan.TextureModVersion)
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
                        if (modFiles[l].tag == MipMaps.FileTextureTag || modFiles[l].tag == MipMaps.FileTextureTag2)
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
                        else if (modFiles[l].tag == MipMaps.FileXdeltaTag)
                        {
                            name = modFiles[l].name;
                            exportId = fs.ReadInt32();
                            pkgPath = fs.ReadStringASCIINull();
                        }

                        dst = MipMaps.decompressData(fs, size);
                        dstLen = dst.Length;

                        updateProgressStatus("Installing textures " + (currentNumberOfTotalMods * 100 / totalNumberOfMods) + "%");

                        if (modFiles[l].tag == MipMaps.FileTextureTag || modFiles[l].tag == MipMaps.FileTextureTag2)
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
                                errors += mipMaps.replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, false, modFiles[l].tag == MipMaps.FileTextureTag2);
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
                            pkg = null;
                        }
                        else if (modFiles[l].tag == MipMaps.FileXdeltaTag)
                        {
                            string path = GameData.GamePath + pkgPath;
                            if (!File.Exists(path))
                            {
                                log += "Warning: File " + path + " not exists in your game setup." + Environment.NewLine;
                                continue;
                            }
                            Package pkg = cachePackageMgr.OpenPackage(path);
                            byte[] buffer = new Xdelta3Helper.Xdelta3().Decompress(pkg.getExportData(exportId), dst);
                            if (buffer.Length == 0)
                            {
                                errors += "Warning: Xdelta patch for " + path + " failed to apply." + Environment.NewLine;
                                log += "Warning: Xdelta patch for " + path + " failed to apply." + Environment.NewLine;
                                continue;
                            }
                            pkg.setExportData(exportId, buffer);
                            pkg = null;
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

            buttonNormal.Visible = false;
            buttonSTART.Visible = false;
            checkBoxOptionRepack.Visible = labelOptionRepack.Visible = false;
            checkBoxOptionIndirectSound.Visible = labelOptionIndirectSound.Visible = false;
            checkBoxOptionReshade.Visible = labelOptionReshade.Visible = false;
            checkBoxOptionBik.Visible = labelOptionBik.Visible = false;
            labelOptions.Visible = false;
            if (meuitmMode)
                customLabelDesc.Text = "Installing MEUITM for Mass Effect";
            else
                customLabelDesc.Text = "Installing for Mass Effect " + gameId;
            comboBoxMod0.Visible = comboBoxMod1.Visible = comboBoxMod2.Visible = comboBoxMod3.Visible = false;
            comboBoxMod4.Visible = comboBoxMod5.Visible = comboBoxMod6.Visible = comboBoxMod7.Visible = false;
            comboBoxMod8.Visible = comboBoxMod9.Visible = false;
            labelModsSelection.Visible = false;

            if (!PreInstallCheck())
                return;

            for (int i = 0; i < selectedFileMods.Count; i++)
            {
                allMemMods.Remove(selectedFileMods[i]);
            }
            for (int i = 0; i < memFiles.Count; i++)
            {
                string file = Path.GetFileName(memFiles[i]).ToLowerInvariant();
                if (allMemMods.Contains(file))
                    memFiles.RemoveAt(i--);
            }

            customLabelFinalStatus.Text = "";
            customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.White);

            errors = "";
            log = "";
            Misc.startTimer();

            if (!updateMode && gameId == 3 && unpackDLC)
            {
                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                ME3DLC.unpackAllDLC(null, this, false);
                gameData.getPackages(true, true);
            }

            if (checkBoxOptionRepack.Checked)
            {
                pkgsToRepack = new List<string>();
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    pkgsToRepack.Add(GameData.packageFiles[i]);
                }
                if (GameData.gameType == MeType.ME1_TYPE)
                    pkgsToRepack.Remove(GameData.GamePath + @"\BioGame\CookedPC\testVolumeLight_VFX.upk");
                if (GameData.gameType == MeType.ME2_TYPE)
                    pkgsToRepack.Remove(GameData.GamePath + @"\BioGame\CookedPC\BIOC_Materials.pcc");
            }

            if (GameData.gameType != MeType.ME1_TYPE)
                gameData.getTfcTextures();

            if (!updateMode)
            {
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

                pkgsToMarker = new List<string>();
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    pkgsToMarker.Add(GameData.packageFiles[i]);
                }
                if (GameData.gameType == MeType.ME1_TYPE)
                    pkgsToMarker.Remove(GameData.GamePath + @"\BioGame\CookedPC\testVolumeLight_VFX.upk");
                if (GameData.gameType == MeType.ME2_TYPE)
                    pkgsToMarker.Remove(GameData.GamePath + @"\BioGame\CookedPC\BIOC_Materials.pcc");

                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;

                log += "Scan textures started..." + Environment.NewLine;
                errors += treeScan.PrepareListOfTextures(GameData.gameType, null, null, this, ref log, false);
                textures = treeScan.treeScan;
                log += "Scan textures finished" + Environment.NewLine + Environment.NewLine;
            }

            customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
            log += "Process textures started..." + Environment.NewLine;
            applyModules();
            log += "Process textures finished" + Environment.NewLine + Environment.NewLine;


            log += "Saving packages started..." + Environment.NewLine;
            customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
            cachePackageMgr.CloseAllWithSave(checkBoxOptionRepack.Checked, !updateMode, false);
            log += "Saving packages finished" + Environment.NewLine + Environment.NewLine;


            if (!updateMode)
            {
                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                log += "Remove mipmaps started..." + Environment.NewLine;
                if (gameId == 1)
                {
                    errors += mipMaps.removeMipMapsME1(1, textures, null, this, false);
                    errors += mipMaps.removeMipMapsME1(2, textures, null, this, false);
                }
                else
                {
                    errors += mipMaps.removeMipMapsME2ME3(textures, null, this, checkBoxOptionRepack.Checked, false);
                }
                log += "Remove mipmaps finished" + Environment.NewLine + Environment.NewLine;
            }

            if (checkBoxOptionRepack.Checked)
            {
                customLabelFinalStatus.Text = "Stage " + stage++ + " of " + totalStages;
                log += "Repack started..." + Environment.NewLine;
                if (GameData.gameType == MeType.ME2_TYPE)
                    pkgsToRepack.Remove(GameData.GamePath + @"\BioGame\CookedPC\BIOC_Materials.pcc");
                for (int i = 0; i < pkgsToRepack.Count; i++)
                {
                    updateProgressStatus("Repack game files " + ((i + 1) * 100 / GameData.packageFiles.Count) + "%");
                    try
                    {
                        Package package = new Package(pkgsToRepack[i], true, true);
                        if (!package.compressed || package.compressed && package.compressionType != Package.CompressionType.Zlib)
                        {
                            package.Dispose();
                            package = new Package(pkgsToRepack[i]);
                            if (package.SaveToFile(true, false, !updateMode))
                            {
                                if (pkgsToMarker != null)
                                    pkgsToMarker.Remove(package.packagePath);
                            }
                        }
                        package.Dispose();
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("Problem with PCC file header:"))
                            continue;
                    }

                }
                log += "Repack finished" + Environment.NewLine + Environment.NewLine;
            }

            if (!updateMode)
                AddMarkers((MeType)gameId);

            if (!applyModTag(gameId, MeuitmVer, 0))
                errors += "Failed applying stamp for installation!\n";

            if (GameData.gameType == MeType.ME3_TYPE)
                TOCBinFile.UpdateAllTOCBinFiles();

            log += "Updating GFX settings started..." + Environment.NewLine;
            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD((MeType)gameId, engineConf);
            LODSettings.updateGFXSettings((MeType)gameId, engineConf, softShadowsModPath != "", meuitmMode);
            log += "Updating GFX settings finished" + Environment.NewLine + Environment.NewLine;


            if (gameId == 1 && softShadowsModPath != "")
            {
                if (installSoftShadowsMod(gameData, softShadowsModPath))
                    log += "Soft Shadows mod installed." + Environment.NewLine + Environment.NewLine;
                else
                {
                    log += "Soft Shadows mod failed to install!" + Environment.NewLine + Environment.NewLine;
                    errors += "Soft Shadows mod failed to install!\n";
                }
            }

            if (gameId == 1 && splashBitmapPath != "")
            {
                if (installSplashScreen(splashBitmapPath))
                    log += "Splash screen mod installed." + Environment.NewLine + Environment.NewLine;
                else
                {
                    log += "Splash mod failed to install!" + Environment.NewLine + Environment.NewLine;
                    errors += "Splash mod failed to install!\n";
                }
            }

            if (gameId == 1 && splashDemiurge != "" && checkBoxOptionBik.Checked)
            {
                if (installSplashVideo(splashDemiurge))
                    log += "Splash video mod installed." + Environment.NewLine + Environment.NewLine;
                else
                {
                    log += "Splash video mod failed to install!" + Environment.NewLine + Environment.NewLine;
                    errors += "Splash video mod failed to install!\n";
                }
            }

            if (gameId == 1 && indirectSoundPath != "" && checkBoxOptionIndirectSound.Checked)
            {
                if (installIndirectSoundPath(indirectSoundPath))
                    log += "Indirect Sound installed." + Environment.NewLine + Environment.NewLine;
                else
                {
                    log += "Indirect Sound failed to install!" + Environment.NewLine + Environment.NewLine;
                    errors += "Indirect Sound failed to install!\n";
                }
            }
            if (gameId == 1 && File.Exists(GameData.GamePath + "\\Binaries\\dsound.dll"))
                engineConf.Write("DeviceName", "Generic Hardware", "ISACTAudio.ISACTAudioDevice");

            if (gameId == 1 && reshadePath != "" && checkBoxOptionReshade.Checked)
            {
                if (installReshadePath(reshadePath))
                    log += "ReShader installed." + Environment.NewLine + Environment.NewLine;
                else
                {
                    log += "ReShader failed to install!" + Environment.NewLine + Environment.NewLine;
                    errors += "ReShader failed to install!\n";
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
                    fs.WriteStringASCII(errors);
                }
                customLabelFinalStatus.Text = "WARNING: Some errors have occured!";
                customLabelFinalStatus.ForeColor = Color.FromKnownColor(KnownColor.Yellow);
                Process.Start(filename);
            }
        }

        public void updateStatusPrepare(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        public void updateProgressStatus(string text)
        {
            customLabelCurrentStatus.Text = text;
            Application.DoEvents();
        }

        private void buttonNormal_Click(object sender, EventArgs e)
        {
            exitToModder = true;
            Close();
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
