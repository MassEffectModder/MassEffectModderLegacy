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
        public ConfIni _configIni;
        List<string> _packageFiles;
        TOCBinFile _tocFile;

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

        public TexExplorer CreateTextureExplorer(MeType type)
        {
            TexExplorer explorer = new TexExplorer(this, type);
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
            var path = gameData.EngineConfigIniPath;
            var exist = File.Exists(path);
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
                UInt32 offset = fs.ReadUInt32() + 4; // skip PE signature too
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
            if (GameData.gameType == MeType.ME3_TYPE)
                _tocFile = new TOCBinFile(Path.Combine(GameData.bioGamePath, @"PCConsoleTOC.bin"));
            updateStatusLabel("Finding packages in game data...");
            if (!gameData.getPackages())
            {
                MessageBox.Show("Unable get packages from game data.");
                updateStatusLabel("");
                return false;
            }
            _packageFiles = GameData.packageFiles;
            updateStatusLabel("Done.");
            return true;
        }

        public void repackME12(MeType gametype)
        {
            GameData gameData = new GameData(gametype, _configIni);
            GetPackages(gameData);
            for (int i = 0; i < _packageFiles.Count; i++)
            {
                updateStatusLabel("Repack file " + (i + 1) + " of " + _packageFiles.Count);
                var package = new Package(_packageFiles[i]);
                if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                    package.SaveToFile();
            }
            updateStatusLabel("Done");
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
            repackME12(MeType.ME1_TYPE);
            enableGameDataMenu(true);
        }

        private void updateME2DLCCacheToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            ME2DLC dlc = new ME2DLC();
            dlc.updateChecksums(gameData);
            updateStatusLabel("Done");
            enableGameDataMenu(true);
        }

        private void updateME2ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME2_TYPE, _configIni);
            var path = gameData.EngineConfigIniPath;
            var exist = File.Exists(path);
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

        private void extractME3DLCPackagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (Directory.Exists(gameData.DLCDataCache))
            {
                Directory.Delete(gameData.DLCDataCache, true);
            }
            Directory.CreateDirectory(gameData.DLCDataCache);
            List<string> sfarFiles = Directory.GetFiles(gameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < sfarFiles.Count; i++)
            {
                string DLCname = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(sfarFiles[i])));
                string outPath = Path.Combine(gameData.DLCDataCache, DLCname);
                Directory.CreateDirectory(outPath);
                ME3DLC dlc = new ME3DLC();
                updateStatusLabel("DLC extracting " + (i + 1) + " of " + sfarFiles.Count);
                Application.DoEvents();
                dlc.extract(sfarFiles[i], outPath, DLCname);
            }
            updateStatusLabel("Done");
            enableGameDataMenu(true);
        }

        private void PackME3DLC(string inPath, string DLCname, bool compressed)
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            string outPath = Path.Combine(gameData.DLCData, DLCname, "CookedPCConsole", "Default.sfar");
            ME3DLC dlc = new ME3DLC();
            dlc.pack(inPath, outPath, DLCname, !compressed);
        }

        public void updateTOCBinEntry(string filePath)
        {
            if (!filePath.Contains(GameData.MainData))
                return;
            int pos = (Path.Combine(Path.GetDirectoryName(GameData.GamePath + @"\"))).Length;
            string filename = filePath.Substring(pos + 1);
            _tocFile.updateFile(filename, filePath, false);
        }

        public void updateAllTOCBinEntries()
        {
            for (int i = 0; i < _packageFiles.Count; i++)
            {
                updateTOCBinEntry(_packageFiles[i]);
            }
            saveTOCBin();
        }

        public void saveTOCBin()
        {
            _tocFile.saveToFile(Path.Combine(GameData.bioGamePath, @"PCConsoleTOC.bin"));
        }

        private void PackAllME3DLC(bool compressed)
        {
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            if (!Directory.Exists(gameData.DLCDataCache))
            {
                MessageBox.Show("DLCCache directory is missing, you need exract DLC packages first.");
                return;
            }
            List<string> DLCs = Directory.GetDirectories(gameData.DLCDataCache).ToList();
            for (int i = 0; i < DLCs.Count; i++)
            {
                string DLCname = Path.GetFileName(DLCs[i]);
                updateStatusLabel("DLC packing " + (i + 1) + " of " + DLCs.Count);
                Application.DoEvents();
                PackME3DLC(DLCs[i], DLCname, compressed);
            }
            updateStatusLabel("Done");
            enableGameDataMenu(true);
        }

        private void packME3DLCPackagesUncompressedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            PackAllME3DLC(false);
            enableGameDataMenu(true);
        }

        private void packME3DLCPackagesLZMAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            PackAllME3DLC(true);
            enableGameDataMenu(true);
        }

        private void updateME3ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableGameDataMenu(false);
            GameData gameData = new GameData(MeType.ME3_TYPE, _configIni);
            var path = gameData.EngineConfigIniPath;
            var exist = File.Exists(path);
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
    }
}
