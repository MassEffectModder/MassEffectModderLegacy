/*
 * MEDataExplorer
 *
 * Copyright (C) 2014-2015 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MEDataExplorer
{
    public partial class TexExplorer : Form
    {
        MeType _gameSelected;
        MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<string> _packageFiles;

        public TexExplorer(MainWindow main, MeType gameType)
        {
            InitializeComponent();
            _mainWindow = main;
            _gameSelected = gameType;
            _configIni = main._configIni;
            gameData = new GameData(_gameSelected, _configIni);
        }

        public void Run()
        {
            _mainWindow.updateStatusLabel("");
            if (_gameSelected == MeType.ME1_TYPE)
            {
                UpdateME1Config();
                VerifyME1Exe();
            }

            GetPackages(_gameSelected);

        }

        public bool GetPackages(MeType gameType)
        {
            if (_gameSelected == MeType.ME1_TYPE)
            {
                _packageFiles = Directory.GetFiles(gameData.MainData, "*.*",
                SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                _packageFiles.AddRange(Directory.GetFiles(gameData.DLCData, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                        StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)));
                _packageFiles.RemoveAll(s => s.Contains("RefShaderCache-PC-D3D-SM3.upk"));
            }
            else if (_gameSelected == MeType.ME2_TYPE)
            {
                _packageFiles = Directory.GetFiles(gameData.MainData, "*.pcc", SearchOption.AllDirectories).ToList();
                _packageFiles.AddRange(Directory.GetFiles(gameData.DLCData, "*.pcc", SearchOption.AllDirectories));
            }
            else if (_gameSelected == MeType.ME3_TYPE)
            {
                if (!Directory.Exists(gameData.DLCDataCache))
                {
                    MessageBox.Show("DLCCache directory is missing, you need exract DLC packages first.");
                    return false;
                }
                _packageFiles = Directory.GetFiles(gameData.MainData, "*.pcc", SearchOption.AllDirectories).ToList();
                if (Directory.Exists(gameData.DLCDataCache))
                    _packageFiles.AddRange(Directory.GetFiles(gameData.DLCDataCache, "*.pcc", SearchOption.AllDirectories));
                _packageFiles.RemoveAll(s => s.Contains("GuidCache"));
            }
            return true;
        }

        public void UpdateME1Config()
        {
            var path = gameData.EngineConfigIniPath;
            var exist = File.Exists(path);
            if (!exist)
                return;
            ConfIni engineConf = new ConfIni(path);
            var str = engineConf.Read("TEXTUREGROUP_Character_Diff", "TextureLODSettings");
            if (str != "(MinLODSize=512,MaxLODSize=4096,LODBias=0)")
            {
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            }
        }

        void VerifyME1Exe()
        {
            if (!File.Exists(gameData.GameExePath))
                throw new FileNotFoundException("Game exe not found: " + gameData.GameExePath);

            using (FileStream fs = new FileStream(gameData.GameExePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0x146, SeekOrigin.Begin); // offset to byte with LAA flag
                var flag = fs.ReadByte();
                if (flag == 0x02)
                    MessageBox.Show("Warning: Large Aware Address flag is not enabled in MassEffect.exe file.");
                else if (flag == 0x22)
                {
                    ; // LAA flag enabled
                }
                else
                    throw new Exception("Not expected flags in exe file");
            }
        }

        public void RepackME12()
        {
            GetPackages(_gameSelected);
            for (int i = 0; i < _packageFiles.Count; i++)
            {
                _mainWindow.updateStatusLabel("Repack file " + (i + 1) + " of " + _packageFiles.Count);
                Application.DoEvents();
                var package = new Package(_packageFiles[i]);
                package.SaveToFile(true);
            }
            _mainWindow.updateStatusLabel("Done");
        }

        public void UpdateME2DLC()
        {
            ME2DLC dlc = new ME2DLC();
            dlc.updateChecksums(gameData);
            _mainWindow.updateStatusLabel("Done");
        }

        public void RepackME3()
        {
            List<string> packageFiles = Directory.GetFiles(gameData.MainData, "*.pcc", SearchOption.AllDirectories).ToList();
            packageFiles.RemoveAll(s => s.Contains("GuidCache"));
            string TOCFilePath = Path.Combine(gameData.bioGamePath, "PCConsoleTOC.bin");
            TOCBinFile tocFile = new TOCBinFile(TOCFilePath);
            for (int i = 0; i < packageFiles.Count; i++)
            {
                _mainWindow.updateStatusLabel("Repack file " + (i + 1) + " of " + packageFiles.Count);
                Application.DoEvents();
                var package = new Package(packageFiles[i]);
                package.SaveToFile();

                int pos = packageFiles[i].IndexOf(@"\BioGame\", StringComparison.CurrentCultureIgnoreCase);
                string filename = packageFiles[i].Substring(pos + 1);
                tocFile.updateFile(filename, packageFiles[i]);
            }
            tocFile.saveToFile(TOCFilePath);
            _mainWindow.updateStatusLabel("Done");
        }

        public void RepackDLCME3(bool forceCompress)
        {
            if (!Directory.Exists(gameData.DLCDataCache))
            {
                MessageBox.Show("DLCCache directory is missing, you need exract DLC packages first.");
                return;
            }
            List<string> packageFiles = Directory.GetFiles(gameData.DLCDataCache, "*.pcc", SearchOption.AllDirectories).ToList();
            packageFiles.RemoveAll(s => s.Contains("GuidCache"));
            for (int i = 0; i < packageFiles.Count; i++)
            {
                _mainWindow.updateStatusLabel("File " + (i + 1) + " of " + packageFiles.Count);
                Application.DoEvents();
                var package = new Package(packageFiles[i]);
                package.SaveToFile(forceCompress);
            }
            _mainWindow.updateStatusLabel("Done");
        }

        public void ExtractME3DLC()
        {
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
                _mainWindow.updateStatusLabel("DLC extracting " + (i + 1) + " of " + sfarFiles.Count);
                Application.DoEvents();
                dlc.extract(sfarFiles[i], outPath, DLCname);
            }
            _mainWindow.updateStatusLabel("Done");
        }

        private void PackME3DLC(string inPath, string DLCname, bool compressed)
        {
            string outPath = Path.Combine(gameData.DLCData, DLCname, "CookedPCConsole", "Default.sfar");
            ME3DLC dlc = new ME3DLC();
            dlc.pack(inPath, outPath, DLCname, !compressed);
        }

        public void PackAllME3DLC(bool compressed)
        {
            if (!Directory.Exists(gameData.DLCDataCache))
            {
                MessageBox.Show("DLCCache directory is missing, you need exract DLC packages first.");
                return;
            }
            List<string> DLCs = Directory.GetDirectories(gameData.DLCDataCache).ToList();
            for (int i = 0; i < DLCs.Count; i++)
            {
                string DLCname = Path.GetFileName(DLCs[i]);
                _mainWindow.updateStatusLabel("DLC packing " + (i + 1) + " of " + DLCs.Count);
                Application.DoEvents();
                PackME3DLC(DLCs[i], DLCname, compressed);
            }
            _mainWindow.updateStatusLabel("Done");
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mainWindow.enableGameDataMenu(true);
        }
    }
}
