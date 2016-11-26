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
using System.Text.RegularExpressions;
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
                return;
            ConfIni engineConf = new ConfIni(path);
            engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_GUI", "(MinLODSize=64,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            enableGameDataMenu(true);
        }

        public void VerifyME1Exe(GameData gameData)
        {
            if (!File.Exists(gameData.GameExePath))
                throw new FileNotFoundException("Game exe not found: " + gameData.GameExePath);

            using (FileStream fs = new FileStream(gameData.GameExePath, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.JumpTo(0x3C); // jump to offset of COFF header
                uint offset = fs.ReadUInt32() + 4; // skip PE signature too
                fs.JumpTo(offset + 0x12); // jump to flags entry
                ushort flag = fs.ReadUInt16(); // read flags
                if ((flag & 0x20) != 0x20) // check for LAA flag
                {
                    MessageBox.Show("Large Aware Address flag is not enabled in MassEffect.exe file. Correcting...");
                    flag |= 0x20;
                    fs.Skip(-2);
                    fs.WriteUInt16(flag); // write LAA flag
                }
            }
        }

        public bool GetPackages(GameData gameData)
        {
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

        private void ParseLegacyScriptMod(string script, ref string package, ref int expId)
        {
            Regex parts = new Regex("int objidx = [0-9]*");
            Match match = parts.Match(script);
            if (match.Success)
            {
                expId = int.Parse(match.ToString().Split(' ').Last());
                parts = new Regex("string filename = \"[A-z,0-9,.]*\";");
                match = parts.Match(script);
                if (match.Success)
                {
                    package = match.ToString().Split('\"')[1];
                    return;
                }
            }
            MessageBox.Show("Wrong Legacy Mod");
        }

        private void replaceExportDataMod(MeType gameType)
        {
            GameData gameData = new GameData(gameType, _configIni);
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
                        fs.SeekBegin();
                        int len = fs.ReadInt32();
                        fs.ReadStringASCII(len); // version
                        int numEntries = fs.ReadInt32();
                        len = fs.ReadInt32();
                        fs.ReadStringASCII(len); // description
                        len = fs.ReadInt32();
                        string scriptLegacy = fs.ReadStringASCII(len);
                        int expId = -1;
                        string package = "";
                        ParseLegacyScriptMod(scriptLegacy, ref package, ref expId);
                        if (expId != -1 && package != "")
                        {
                            string[] packages = Directory.GetFiles(GameData.MainData, package, SearchOption.AllDirectories);
                            if (packages.Count() != 0)
                            {
                                len = fs.ReadInt32();
                                byte[] data = fs.ReadToBuffer(len);
                                Package pkg = new Package(packages[0]);
                                pkg.setExportData(expId, data);
                                pkg.SaveToFile();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Legacy Mod not compatible!");
                            return;
                        }
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
                return;
            ConfIni engineConf = new ConfIni(path);
            engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_RenderTarget", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
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
            string tmpDlcDir = Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp");
            List<string> dlcs = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).ToList();
            if (dlcs.Count() == 0)
            {
                if (Directory.Exists(tmpDlcDir))
                    Directory.Delete(tmpDlcDir, true);
                Directory.CreateDirectory(tmpDlcDir);
                List<string> sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                for (int i = 0; i < sfarFiles.Count; i++)
                {
                    string DLCname = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(sfarFiles[i])));
                    string outPath = Path.Combine(tmpDlcDir, DLCname);
                    Directory.CreateDirectory(outPath);
                    ME3DLC dlc = new ME3DLC(this);
                    updateStatusLabel("SFAR unpacking - DLC " + (i + 1) + " of " + sfarFiles.Count);
                    dlc.extract(sfarFiles[i], outPath);
                }
                Directory.Delete(GameData.DLCData, true);
                Directory.Move(tmpDlcDir, GameData.DLCData);
            }
            updateStatusLabel("Done");
            updateStatusLabel2("");
            enableGameDataMenu(true);
        }

        private void PackME3DLC(string inPath, string DLCname)
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            string outPath = Path.Combine(Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp"), DLCname, "CookedPCConsole", "Default.sfar");
            ME3DLC dlc = new ME3DLC(this);
            dlc.fullRePack(inPath, outPath, DLCname);
        }

        private void PackAllME3DLC()
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
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
                return;
            ConfIni engineConf = new ConfIni(path);
            engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            enableGameDataMenu(true);
        }

        private void modME3ExportDataToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            replaceExportDataMod(MeType.ME3_TYPE);
            enableGameDataMenu(true);
        }
    }
}
