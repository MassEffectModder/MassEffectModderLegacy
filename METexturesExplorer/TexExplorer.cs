/*
 * METexturesExplorer
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using StreamHelpers;

namespace METexturesExplorer
{
    public enum MeType
    {
        ME1_TYPE = 1,
        ME2_TYPE,
        ME3_TYPE
    }

    public struct MatchedTexture
    {
        public string path;
        public int exportID;
    }

    public struct FoundTexture
    {
        public string name;
        public UInt32 crc;
        public int mipmapOffset;
        public string packageName;
        public List<MatchedTexture> list;
    }

    public partial class TexExplorer : Form
    {
        const UInt32 textureMapBinTag = 0x4D455450;
        const UInt32 textureMapBinVersion = 1;

        MeType _gameSelected;
        MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<string> _packageFiles;
        TOCBinFile _tocFile;
        List<FoundTexture> _textures;

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
                VerifyME1Exe();

            if (GetPackages(_gameSelected))
            {
                _textures = new List<FoundTexture>();
                string filename = "me" + (int)_gameSelected + "map.bin";
                if (File.Exists(filename))
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        UInt32 tag = fs.ReadUInt32();
                        UInt32 version = fs.ReadUInt32();
                        if (tag != textureMapBinTag || version != textureMapBinVersion)
                        {
                            MessageBox.Show("Abort! Wrong " + filename + " file!");
                            _mainWindow.updateStatusLabel("");
                            return;
                        }

                        UInt32 countTexture = fs.ReadUInt32();
                        for (int i = 0; i < countTexture; i++)
                        {
                            FoundTexture texture = new FoundTexture();
                            texture.name = fs.ReadStringASCIINull();
                            texture.crc = fs.ReadUInt32();
                            UInt32 countPackages = fs.ReadUInt32();
                            for (int k = 0; k < countPackages; k++)
                            {
                                MatchedTexture matched = new MatchedTexture();
                                matched.exportID = fs.ReadInt32();
                                matched.path = fs.ReadStringASCIINull();
                                texture.list = new List<MatchedTexture>();
                                texture.list.Add(matched);
                            }
                            _textures.Add(texture);
                        }
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show("Replacing textures and creating mods require textures mapping.\n" +
                        "It's one time only process but can be very long.\n\n" +
                        "IMPORTANT! Make sure game data is not modified.\n\n" +
                        "Are you sure to proceed?", "Textures mapping", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No)
                        return;

                    if (_gameSelected == MeType.ME1_TYPE)
                        sortPackagesME1();
                    for (int i = 0; i < _packageFiles.Count; i++)
                    {
                        _mainWindow.updateStatusLabel("Find textures in package " + (i + 1) + " of " + _packageFiles.Count);
                        FindTextures(_packageFiles[i]);
                    }

                    using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        fs.WriteUInt32(textureMapBinTag);
                        fs.WriteUInt32(textureMapBinVersion);
                        fs.WriteInt32(_textures.Count);
                        for (int i = 0; i < _textures.Count; i++)
                        {
                            fs.WriteStringASCIINull(_textures[i].name);
                            fs.WriteUInt32(_textures[i].crc);
                            fs.WriteInt32(_textures[i].list.Count);
                            for (int k = 0; k < _textures[i].list.Count; k++)
                            {
                                fs.WriteInt32(_textures[i].list[k].exportID);
                                fs.WriteStringASCIINull(_textures[i].list[k].path);
                            }
                        }
                    }
                    _mainWindow.updateStatusLabel("Done.");
                }
            }
            _packageFiles.Clear();
        }

        void sortPackagesME1()
        {
            _mainWindow.updateStatusLabel("Sorting packages...");
            List<string> sortedList = new List<string>();
            List<string> restList = new List<string>();
            for (int i = 0; i < _packageFiles.Count; i++)
            {
                var package = new Package(_packageFiles[i], true);
                if (!package.compressed)
                    sortedList.Add(_packageFiles[i]);
                else
                    restList.Add(_packageFiles[i]);
                _mainWindow.updateStatusLabel("Sorting packages... " + (i + 1) + " of " + _packageFiles.Count);
            }
            sortedList.AddRange(restList);
            _packageFiles = sortedList;
            _mainWindow.updateStatusLabel("Done.");
        }

        public void FindTextures(string packagePath)
        {
            var package = new Package(packagePath);
            for (int i = 0; i < package.exportsTable.Count; i++)
            {
                int id = package.getClassNameId(package.exportsTable[i].classId);
                if (id == package.nameIdTexture2D || 
                    id == package.nameIdLightMapTexture2D || 
                    id == package.nameIdTextureFlipBook)
                {
                    Texture texture = new Texture(package, i, package.getExportData(i), this);
                    if (!texture.hasImageData())
                        continue;

                    Texture.MipMap mipmap = texture.getTopMipmap();
                    string name = package.exportsTable[i].objectName;
                    MatchedTexture matchTexture = new MatchedTexture();
                    matchTexture.exportID = i;
                    matchTexture.path = GameData.RelativeGameData(packagePath);

                    if (_gameSelected == MeType.ME1_TYPE)
                    {
                        if (mipmap.storageType == Texture.StorageTypes.pccUnc || 
                            mipmap.storageType == Texture.StorageTypes.pccCpr)
                        {
                            uint crc = texture.getCrcMipmap();
                            FoundTexture foundTexName = _textures.Find(s => s.crc == crc);
                            if (foundTexName.name != null && 
                                package.compressed)
                            {
                                foundTexName.list.Add(matchTexture);
                            }
                            else
                            {
                                FoundTexture foundTex = new FoundTexture();
                                foundTex.list = new List<MatchedTexture>();
                                foundTex.list.Add(matchTexture);
                                foundTex.name = name;
                                foundTex.crc = crc;
                                foundTex.packageName = texture.packageName;
                                foundTex.mipmapOffset = mipmap.dataOffset;
                                _textures.Add(foundTex);
                            }
                        }
                        else
                        {
                            FoundTexture foundTexName = _textures.Find(s => s.name == name && s.packageName == texture.packageName);
                            foundTexName.list.Add(matchTexture);
                        }
                    }
                    else
                    {
                        uint crc = texture.getCrcMipmap();
                        FoundTexture foundTexName = _textures.Find(s => s.crc == crc);
                        if (foundTexName.crc != 0)
                        {
                            foundTexName.list.Add(matchTexture);
                        }
                        else
                        {
                            FoundTexture foundTex = new FoundTexture();
                            foundTex.list = new List<MatchedTexture>();
                            foundTex.list.Add(matchTexture);
                            foundTex.name = name;
                            foundTex.crc = crc;
                            foundTex.packageName = texture.packageName;
                            foundTex.mipmapOffset = mipmap.dataOffset;
                            _textures.Add(foundTex);
                        }
                    }
                }
            }
        }

        public bool GetPackages(MeType gameType)
        {
            if (_gameSelected == MeType.ME3_TYPE)
                _tocFile = new TOCBinFile(Path.Combine(gameData.bioGamePath, @"PCConsoleTOC.bin"));
            _mainWindow.updateStatusLabel("Finding packages in game data...");
            if (!gameData.getPackages())
            {
                MessageBox.Show("Unable get packages from game data.");
                _mainWindow.updateStatusLabel("");
                return false;
            }
            _packageFiles = GameData.packageFiles;
            _mainWindow.updateStatusLabel("Done.");
            return true;
        }

        public void updateTOCBinEntry(string filePath)
        {
            if (!filePath.Contains(gameData.MainData))
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
            _tocFile.saveToFile(Path.Combine(gameData.bioGamePath, @"PCConsoleTOC.bin"));
        }

        public void UpdateME1Config()
        {
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
        }

        public void UpdateME2Config()
        {
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
        }

        public void UpdateME3Config()
        {
            var path = gameData.EngineConfigIniPath;
            var exist = File.Exists(path);
            if (!exist)
                return;
            ConfIni engineConf = new ConfIni(path);
            engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
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
        }

        void VerifyME1Exe()
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

        public void RepackME12()
        {
            GetPackages(_gameSelected);
            for (int i = 0; i < _packageFiles.Count; i++)
            {
                _mainWindow.updateStatusLabel("Repack file " + (i + 1) + " of " + _packageFiles.Count);
                Application.DoEvents();
                var package = new Package(_packageFiles[i]);
                if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                    package.SaveToFile();
            }
            _mainWindow.updateStatusLabel("Done");
        }

        public void UpdateME2DLC()
        {
            ME2DLC dlc = new ME2DLC();
            dlc.updateChecksums(gameData);
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
