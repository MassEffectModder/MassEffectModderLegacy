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

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using StreamHelpers;
using AmaroK86.ImageFormat;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using System.Text;
using System.Collections;

namespace MassEffectModder
{
    public enum MeType
    {
        ME1_TYPE = 1,
        ME2_TYPE,
        ME3_TYPE
    }

    struct TFCTexture
    {
        public byte[] guid;
        public string name;
    }

    public struct MatchedTexture
    {
        public string path;
        public int exportID;
    }

    public struct FoundTexture
    {
        public string name;
        public uint crc;
        public uint mipmapOffset;
        public string packageName;
        public string displayName;
        public List<MatchedTexture> list;
    }

    public partial class TexExplorer : Form
    {
        const uint textureMapBinTag = 0x5054454D;
        const uint textureMapBinVersion = 1;
        const string TempModFileName = "TextureMod.tmp";
        const uint TextureModTag = 0x444F4D54;
        const uint TextureModVersion = 1;
        const uint TextureModHeaderLength = 12;

        MeType _gameSelected;
        MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<FoundTexture> _textures;
        bool previewShow = true;
        bool moddingEnable = false;
        FileStream fileStreamMod;
        uint numberOfTexturesMod;
        CachePackageMgr cachePackageMgr;
        TFCTexture[] guids = new TFCTexture[]
        {
            new TFCTexture
            {
                guid = new byte[] { 0x11, 0xD3, 0xC3, 0x39, 0xB3, 0x40, 0x44, 0x61, 0xBB, 0x0E, 0x76, 0x75, 0x2D, 0xF7, 0xC3, 0xB1 },
                name = "Texture2D"
            },
            new TFCTexture
            {
                guid = new byte[] { 0x81, 0xCD, 0x12, 0x5C, 0xBB, 0x72, 0x40, 0x2D, 0x99, 0xB1, 0x63, 0x8D, 0xC0, 0xA7, 0x6E, 0x03 },
                name = "IntProperty"
            },
            new TFCTexture
            {
                guid = new byte[] { 0xA5, 0xBE, 0xFF, 0x48, 0xB4, 0x7A, 0x47, 0xB0, 0xB2, 0x07, 0x2B, 0x35, 0x96, 0x39, 0x55, 0xFB },
                name = "ByteProperty"
            },
            new TFCTexture
            {
                guid = new byte[] { 0x2B, 0x7D, 0x2F, 0x16, 0x63, 0x52, 0x4F, 0x3E, 0x97, 0x5B, 0x0E, 0xF2, 0xC1, 0xEB, 0xC6, 0x5D },
                name = "Format"
            },
            new TFCTexture
            {
                guid = new byte[] { 0x59, 0xF2, 0x1B, 0x17, 0xD0, 0xFE, 0x42, 0x3E, 0x94, 0x8A, 0x26, 0xBE, 0x26, 0x3C, 0x46, 0x2E },
                name = "SizeX"
            },
            new TFCTexture
            {
                guid = new byte[] { 0x0C, 0x70, 0x7A, 0x01, 0xA0, 0xC1, 0x49, 0xB4, 0x97, 0x8D, 0x3B, 0xA4, 0x94, 0x71, 0xBE, 0x43 },
                name = "SizeY"
            },
            new TFCTexture
            {
                guid = new byte[] { 0xCC, 0xB9, 0x93, 0xFB, 0xD9, 0x56, 0x49, 0x9B, 0xA7, 0x06, 0x9B, 0xD8, 0x37, 0x69, 0x10, 0x9E },
                name = "None"
            }
        };

        public class PackageTreeNode : TreeNode
        {
            public List<FoundTexture> textures;

            public PackageTreeNode(string name)
                : base()
            {
                Name = Text = name;
                textures = new List<FoundTexture>();
            }
        };
        List<PackageTreeNode> nodeList;

        public class CachePackageMgr
        {
            public List<Package> packages;
            MainWindow mainWindow;

            public CachePackageMgr(MainWindow main)
            {
                packages = new List<Package>();
                mainWindow = main;
            }

            public Package OpenPackage(string path, bool headerOnly = false)
            {
                if (!packages.Exists(p => p.packagePath == path))
                {
                    Package pkg = new Package(path, headerOnly);
                    packages.Add(pkg);
                    return pkg;
                }
                else
                {
                    return packages.Find(p => p.packagePath == path);
                }
            }

            public void ClosePackageWithoutSave(Package package)
            {
                int index = packages.IndexOf(package);
                packages[index].Dispose();
                packages.RemoveAt(index);
            }

            public void CloseAllWithoutSave()
            {
                foreach(Package pkg in packages)
                {
                    pkg.Dispose();
                }
                packages.Clear();
            }

            public void CloseAllWithSave()
            {
                if (GameData.gameType == MeType.ME3_TYPE)
                {
                    List<string> sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                    for (int i = 0; i < sfarFiles.Count; i++)
                    {
                        string DLCname = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(sfarFiles[i])));
                        List<Package> dlcPackageList = packages.FindAll(p => p.packagePath.Contains("DLC\\" + DLCname + "\\CookedPCConsole\\"));
                        List<string> modifiedPkgs = new List<string>();
                        for (int p = 0; p < dlcPackageList.Count; p++)
                        {
                            Package pkg = dlcPackageList[p];
                            string path = pkg.packagePath.Substring(pkg.packagePath.IndexOf("\\BioGame\\DLCCache"));
                            modifiedPkgs.Add(path);
                            mainWindow.updateStatusLabel2("DLC " + (i + 1) + " of " + sfarFiles.Count + " - " + DLCname + " - Saving package " + (p + 1) + " of " + dlcPackageList.Count + " - " + path);
                            pkg.SaveToFile();
                            pkg.Dispose();
                            packages.Remove(pkg);
                        }
                        mainWindow.updateStatusLabel2("");

                        if (dlcPackageList.Count != 0)
                        {
                            mainWindow.updateStatusLabel("Updating DLC " + (i + 1) + " of " + sfarFiles.Count + " - " + DLCname);
                            ME3DLC dlc = new ME3DLC(mainWindow);
                            dlc.update(sfarFiles[i], modifiedPkgs);
                        }
                        mainWindow.updateStatusLabel("");
                    }
                }

                for (int i = 0; i < packages.Count; i++)
                {
                    Package pkg = packages[i];
                    mainWindow.updateStatusLabel2("Saving package " + (i + 1) + " of " + packages.Count + " - " + pkg.packagePath);
                    pkg.SaveToFile();
                    pkg.Dispose();
                }
                if (GameData.gameType == MeType.ME3_TYPE)
                {
                    TOCBinFile tocFile = new TOCBinFile(Path.Combine(GameData.bioGamePath, @"PCConsoleTOC.bin"));
                    for (int i = 0; i < packages.Count; i++)
                    {
                        Package pkg = packages[i];
                        int pos = pkg.packagePath.IndexOf("BioGame", StringComparison.OrdinalIgnoreCase);
                        string filename = pkg.packagePath.Substring(pos);
                        tocFile.updateFile(filename, pkg.packagePath);
                    }
                    string[] tfcFiles = Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories);
                    for (int i = 0; i < tfcFiles.Length; i++)
                    {
                        int pos = tfcFiles[i].IndexOf("BioGame", StringComparison.OrdinalIgnoreCase);
                        string filename = tfcFiles[i].Substring(pos);
                        tocFile.updateFile(filename, tfcFiles[i]);
                    }
                    tocFile.saveToFile(Path.Combine(GameData.bioGamePath, @"PCConsoleTOC.bin"));
                }
                mainWindow.updateStatusLabel2("");
                packages.Clear();
            }
        }

        public TexExplorer(MainWindow main, MeType gameType)
        {
            InitializeComponent();
            _mainWindow = main;
            _gameSelected = gameType;
            _configIni = main._configIni;
            gameData = new GameData(_gameSelected, _configIni);
            cachePackageMgr = new CachePackageMgr(main);
        }

        public void EnableMenuOptions(bool enable)
        {
            MODsToolStripMenuItem.Enabled = enable;
            searchToolStripMenuItem.Enabled = enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = enable;
            treeViewPackages.Enabled = enable;
            listViewResults.Enabled = enable;
            listViewTextures.Enabled = enable;
            listViewMods.Enabled = enable;
            Application.DoEvents();
        }

        private void PrepareListOfTextures()
        {
            _textures = new List<FoundTexture>();
            string filename = "me" + (int)_gameSelected + "map.bin";
            if (File.Exists(filename))
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != textureMapBinTag || version != textureMapBinVersion)
                    {
                        MessageBox.Show("Abort! Wrong " + filename + " file!");
                        _mainWindow.updateStatusLabel("");
                        _mainWindow.updateStatusLabel2("");
                        Close();
                        return;
                    }

                    uint countTexture = fs.ReadUInt32();
                    for (int i = 0; i < countTexture; i++)
                    {
                        FoundTexture texture = new FoundTexture();
                        texture.name = fs.ReadStringASCIINull();
                        texture.crc = fs.ReadUInt32();
                        texture.packageName = fs.ReadStringASCIINull();
                        uint countPackages = fs.ReadUInt32();
                        texture.list = new List<MatchedTexture>();
                        for (int k = 0; k < countPackages; k++)
                        {
                            MatchedTexture matched = new MatchedTexture();
                            matched.exportID = fs.ReadInt32();
                            matched.path = fs.ReadStringASCIINull();
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
                {
                    Close();
                    return;
                }

                if (_gameSelected == MeType.ME1_TYPE)
                    sortPackagesME1();
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    _mainWindow.updateStatusLabel("Find textures in package " + (i + 1) + " of " + GameData.packageFiles.Count);
                    _mainWindow.updateStatusLabel2("");
                    FindTextures(GameData.packageFiles[i]);
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
                        fs.WriteStringASCIINull(_textures[i].packageName);
                        fs.WriteInt32(_textures[i].list.Count);
                        for (int k = 0; k < _textures[i].list.Count; k++)
                        {
                            fs.WriteInt32(_textures[i].list[k].exportID);
                            fs.WriteStringASCIINull(_textures[i].list[k].path);
                        }
                    }
                }
                _mainWindow.updateStatusLabel("Done.");
                _mainWindow.updateStatusLabel("");
            }
        }

        private void PrepareTreeList()
        {
            nodeList = new List<PackageTreeNode>();
            PackageTreeNode rootNode = new PackageTreeNode("All Packages");
            for (int l = 0; l < _textures.Count; l++)
            {
                string displayName = _textures[l].name;
                FoundTexture texture = _textures[l];
                texture.displayName = displayName;
                _textures[l] = texture;
                bool found = false;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].Name == _textures[l].packageName)
                    {
                        for (int j = 0; j < nodeList[i].textures.Count; j++)
                        {
                            if (nodeList[i].textures[j].name == _textures[l].name)
                                displayName = nodeList[i].textures[j].name + "!" + nodeList[i].textures.Count;
                        }
                        texture.displayName = displayName;
                        _textures[l] = texture;
                        nodeList[i].textures.Add(_textures[l]);
                        found = true;
                    }
                }
                if (!found)
                {
                    PackageTreeNode treeNode = new PackageTreeNode(_textures[l].packageName);
                    treeNode.textures.Add(_textures[l]);
                    rootNode.Nodes.Add(treeNode);
                    nodeList.Add(treeNode);
                }
            }

            treeViewPackages.Nodes.Clear();
            treeViewPackages.BeginUpdate();
            treeViewPackages.Sort();
            treeViewPackages.Nodes.Add(rootNode);
            treeViewPackages.EndUpdate();
            treeViewPackages.Nodes[0].Expand();
        }

        public void Run()
        {
            _mainWindow.updateStatusLabel("");
            _mainWindow.updateStatusLabel2("");
            EnableMenuOptions(false);
            eNDModdingToolStripMenuItem.Enabled = false;
            clearMODsToolStripMenuItem.Enabled = false;
            listViewResults.Hide();
            listViewMods.Hide();
            richTextBoxInfo.Hide();
            listViewTextures.Clear();
            richTextBoxInfo.Clear();

            if (_gameSelected == MeType.ME1_TYPE)
                _mainWindow.VerifyME1Exe(gameData);

            if (!_mainWindow.GetPackages(gameData))
            {
                Close();
                return;
            }
            else
            {
                _mainWindow.updateStatusLabel("");
                _mainWindow.updateStatusLabel("Preparing tree...");
                PrepareListOfTextures();
                _mainWindow.updateStatusLabel("Done.");
                _mainWindow.updateStatusLabel("");
            }

            PrepareTreeList();

            EnableMenuOptions(true);
        }

        void sortPackagesME1()
        {
            _mainWindow.updateStatusLabel("Sorting packages...");
            _mainWindow.updateStatusLabel("");
            List<string> sortedList = new List<string>();
            List<string> restList = new List<string>();
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                Package package = new Package(GameData.packageFiles[i], true);
                if (!package.compressed)
                    sortedList.Add(GameData.packageFiles[i]);
                else
                    restList.Add(GameData.packageFiles[i]);
                _mainWindow.updateStatusLabel("Sorting packages... " + (i + 1) + " of " + GameData.packageFiles.Count);
            }
            sortedList.AddRange(restList);
            GameData.packageFiles = sortedList;
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel("");
        }

        public void FindTextures(string packagePath)
        {
            Package package = new Package(packagePath);
            for (int i = 0; i < package.exportsTable.Count; i++)
            {
                int id = package.getClassNameId(package.exportsTable[i].classId);
                if (id == package.nameIdTexture2D ||
                    id == package.nameIdLightMapTexture2D ||
                    id == package.nameIdShadowMapTexture2D ||
                    id == package.nameIdTextureFlipBook)
                {
                    Texture texture = new Texture(package, i, package.getExportData(i));
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
                            mipmap.storageType == Texture.StorageTypes.pccLZO ||
                            mipmap.storageType == Texture.StorageTypes.pccZlib)
                        {
                            uint crc = texture.getCrcMipmap();
                            FoundTexture foundTexName = _textures.Find(s => s.crc == crc);
                            if (foundTexName.name != null && package.compressed)
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
            package.Dispose();
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            GameData.packageFiles.Clear();
            cachePackageMgr.CloseAllWithoutSave();
            _mainWindow.enableGameDataMenu(true);
        }

        private void treeViewPackages_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageIndex = 0;
        }

        private void treeViewPackages_AfterExpand(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageIndex = 1;
        }

        private void updateListViewTextures(PackageTreeNode node)
        {
            listViewTextures.BeginUpdate();
            listViewTextures.Clear();
            listViewTextures.Sort();
            for (int i = 0; i < node.textures.Count; i++)
            {
                FoundTexture texture = node.textures[i];
                ListViewItem item = new ListViewItem();
                item.Name = i.ToString();
                item.Text = texture.displayName;
                listViewTextures.Items.Add(item);
            }
            listViewTextures.EndUpdate();
            listViewTextures.Refresh();
        }

        private void treeViewPackages_AfterSelect(object sender, TreeViewEventArgs e)
        {
            updateListViewTextures((PackageTreeNode)e.Node);
            updateViewFromListView();
        }

        private void clearPreview()
        {
            richTextBoxInfo.Clear();
            if (pictureBoxPreview.Image != null)
            {
                pictureBoxPreview.Image.Dispose();
                pictureBoxPreview.Image = null;
            }
        }

        private FoundTexture ParseLegacyScriptMod(string script, string textureName)
        {
            Regex parts = new Regex("pccs.Add[(]\"[A-z,0-9/,..]*\"");
            Match match = parts.Match(script);
            if (match.Success)
            {
                string packageName;
                if (_gameSelected == MeType.ME3_TYPE)
                    packageName = match.ToString().Split('\"')[1].Split('\\').Last().Split('.')[0];
                else
                    packageName = match.ToString().Split('\"')[1].Split('/').Last().Split('.')[0];
                parts = new Regex("IDs.Add[(][0-9]*[)];");
                match = parts.Match(script);
                if (match.Success)
                {
                    int exportId = int.Parse(match.ToString().Split('(')[1].Split(')')[0]);
                    if (exportId != 0)
                    {
                        for (int i = 0; i < _textures.Count; i++)
                        {
                            if (_textures[i].name == textureName)
                            {
                                for (int l = 0; l < _textures[i].list.Count; l++)
                                {
                                    if (_textures[i].list[l].exportID == exportId)
                                    {
                                        string pkg = _textures[i].list[l].path.Split('\\').Last().Split('.')[0];
                                        if (pkg == packageName)
                                        {
                                            return _textures[i];
                                        }
                                    }
                                }
                            }
                        }
                        // search again but without name match
                        for (int i = 0; i < _textures.Count; i++)
                        {
                            for (int l = 0; l < _textures[i].list.Count; l++)
                            {
                                if (_textures[i].list[l].exportID == exportId)
                                {
                                    string pkg = _textures[i].list[l].path.Split('\\').Last().Split('.')[0];
                                    if (pkg == packageName)
                                    {
                                        return _textures[i];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return new FoundTexture();
        }

        private bool checkTextureMod(FileStream fs)
        {
            uint tag = fs.ReadUInt32();
            uint version = fs.ReadUInt32();
            if (tag == TextureModTag && version == TextureModVersion)
                return true;

            fs.SeekBegin();
            uint numberOfTextures = fs.ReadUInt32();
            if (numberOfTextures == 0)
                return false;

            try
            {
                for (int i = 0; i < numberOfTextures; i++)
                {
                    int len = fs.ReadInt32();
                    string textureName = fs.ReadStringASCII(len);
                    textureName = textureName.Split(' ').Last();
                    if (textureName == "")
                        return false;
                    len = fs.ReadInt32();
                    string script = fs.ReadStringASCII(len);
                    if (script == "")
                        return false;
                    FoundTexture f = ParseLegacyScriptMod(script, textureName);
                    uint crc = f.crc;
                    textureName = f.name;
                    if (crc == 0)
                    {
                        MessageBox.Show("Not able match texture: " + textureName + " in MOD");
                    }
                    len = fs.ReadInt32();
                    if (len == 0)
                        return false;
                    _mainWindow.updateStatusLabel2("Checking texture " + (i + 1) + " of " + numberOfTextures + " - " + textureName);
                    DDSImage image = new DDSImage(new MemoryStream(fs.ReadToBuffer(len)));
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void extractTextureMod(string filenameMod, string outDir)
        {
            processTextureMod(filenameMod, -1, true, false, false, outDir);
        }

        private void saveTextureMod(string filenameMod, string outDir)
        {
            processTextureMod(filenameMod, -1, false, false, true, outDir);
        }

        private void previewTextureMod(string filenameMod, int previewIndex)
        {
            processTextureMod(filenameMod, previewIndex, false, false, false, "");
        }

        private void replaceTextureMod(string filenameMod)
        {
            processTextureMod(filenameMod, -1, false, true, false, "");
        }

        private void listTextureMod(string filenameMod)
        {
            processTextureMod(filenameMod, -1, false, false, false, "");
        }

        private void processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, bool store, string outDir)
        {
            using (FileStream fs = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
            {
                bool legacy = false;

                if (previewIndex == -1 && !store && !extract && !replace && !store)
                {
                    listViewTextures.BeginUpdate();
                }

                if (Path.GetExtension(filenameMod).ToLower() == ".tpf")
                {
                    byte[] tpfXorKey = { 0xA4, 0x3F };
                    ZipEntry entry;
                    byte[] buffer = new byte[10000];
                    byte[] password = {
                            0x73, 0x2A, 0x63, 0x7D, 0x5F, 0x0A, 0xA6, 0xBD,
                            0x7D, 0x65, 0x7E, 0x67, 0x61, 0x2A, 0x7F, 0x7F,
                            0x74, 0x61, 0x67, 0x5B, 0x60, 0x70, 0x45, 0x74,
                            0x5C, 0x22, 0x74, 0x5D, 0x6E, 0x6A, 0x73, 0x41,
                            0x77, 0x6E, 0x46, 0x47, 0x77, 0x49, 0x0C, 0x4B,
                            0x46, 0x6F
                        };

                    byte[] listText;
                    using (FileStream zipList = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
                    {
                        using (ZipInputStream zipFs = new ZipInputStream(zipList, tpfXorKey))
                        {
                            zipFs.Password = password;
                            while ((entry = zipFs.GetNextEntry()) != null)
                            {
                                if (entry.Name.ToLower() == "texmod.def")
                                    break;
                            }
                            if (entry.Name.ToLower() != "texmod.def")
                                throw new Exception("missing texmod.def in TPF file");

                            listText = new byte[entry.Size];
                            zipFs.Read(listText, 0, listText.Length);
                        }
                    }

                    string[] ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                    FileStream outFile = null;
                    if (store)
                    {
                        outFile = new FileStream(Path.Combine(outDir, Path.GetFileNameWithoutExtension(filenameMod)) + ".mod", FileMode.Create, FileAccess.Write);
                        outFile.WriteUInt32(TextureModTag);
                        outFile.WriteUInt32(TextureModVersion);
                        outFile.WriteUInt32((uint)_gameSelected);
                        outFile.WriteInt32(ddsList.Count());
                    }

                    using (ZipInputStream zipFs = new ZipInputStream(fs, tpfXorKey))
                    {
                        zipFs.Password = password;
                        int index = 0;
                        bool unique = true;
                        while ((entry = zipFs.GetNextEntry()) != null)
                        {
                            uint crc = 0;
                            string filename = Path.GetFileName(entry.Name);
                            foreach (string dds in ddsList)
                            {
                                string ddsFile = dds.Split('|')[1];
                                if (ddsFile.ToLower() != filename.ToLower())
                                    continue;
                                crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                break;
                            }
                            if (crc == 0)
                                continue;

                            string name = "";
                            for (int i = 0; i < _textures.Count; i++)
                            {
                                if (_textures[i].crc == crc)
                                {
                                    if (name != "")
                                    {
                                        unique = false;
                                        break;
                                    }
                                    name = _textures[i].name;
                                }
                            }
                            if (name == "")
                                name = "Unknown" + index;

                            _mainWindow.updateStatusLabel("Processing MOD: " +
                                    Path.GetFileNameWithoutExtension(filenameMod) + " - Texture: " + name);

                            if (store)
                            {
                                outFile.WriteStringASCIINull(name);
                                outFile.WriteUInt32(crc);
                            }
                            if (extract)
                            {
                                filename = name + "-" + string.Format("0x{0:X8}", crc) + ".dds";
                                outFile = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write);
                            }
                            if (previewIndex == index)
                            {
                                MemoryStream outMem = new MemoryStream();
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outMem.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                outMem.SeekBegin();
                                DDSImage image = new DDSImage(outMem);
                                pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                                break;
                            }
                            else if (store)
                            {
                                MemoryStream outMem = new MemoryStream();
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outMem.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                outMem.SeekBegin();
                                byte[] src = outMem.ReadToBuffer(outMem.Length);
                                byte[] dst = ZlibHelper.Zlib.Compress(src);
                                outFile.WriteInt32(src.Length);
                                outFile.WriteInt32(dst.Length);
                                outFile.WriteFromBuffer(dst);
                            }
                            else if (extract)
                            {
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outFile.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                outFile.Close();
                            }
                            else if (previewIndex == -1)
                            {
                                FoundTexture foundTexture = _textures.Find(s => s.crc == crc && s.name == name);
                                string display;
                                if (foundTexture.crc != 0)
                                {
                                    if (unique)
                                        display = foundTexture.displayName + " (" + foundTexture.packageName + ")";
                                    else
                                        display = foundTexture.displayName + " - Not unique - (" + foundTexture.packageName + ")";
                                }
                                else
                                {
                                    display = name + " (Not matched - CRC: " + string.Format("0x{0:X8}", crc) + ")";
                                }
                                ListViewItem item = new ListViewItem(display);
                                item.Name = index.ToString();
                                listViewTextures.Items.Add(item);
                            }
                            index++;
                        }
                    }
                    if (store)
                        outFile.Close();
                }
                else
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TextureModTag || version != TextureModVersion)
                    {
                        fs.SeekBegin();
                        legacy = true;
                    }
                    else
                    {
                        uint gameType = fs.ReadUInt32();
                        if ((MeType)gameType != _gameSelected)
                        {
                            MessageBox.Show("Mod for different game!");
                            return;
                        }
                    }
                    FileStream outFile = null;
                    if (store)
                    {
                        if (!legacy)
                        {
                            File.Copy(filenameMod, Path.Combine(outDir, Path.GetFileName(filenameMod)));
                            return;
                        }
                        outFile = new FileStream(Path.Combine(outDir, Path.GetFileName(filenameMod)), FileMode.Create, FileAccess.Write);
                        outFile.WriteUInt32(TextureModTag);
                        outFile.WriteUInt32(TextureModVersion);
                        outFile.WriteUInt32((uint)_gameSelected);
                    }
                    int numTextures = fs.ReadInt32();
                    if (store)
                        outFile.WriteInt32(numTextures);
                    for (int i = 0; i < numTextures; i++)
                    {
                        string name;
                        uint crc, size, dstLen = 0, decSize = 0;
                        byte[] dst = null;
                        if (legacy)
                        {
                            int len = fs.ReadInt32();
                            name = fs.ReadStringASCII(len);
                            name = name.Split(' ').Last();
                            len = fs.ReadInt32();
                            string scriptLegacy = fs.ReadStringASCII(len);
                            FoundTexture f = ParseLegacyScriptMod(scriptLegacy, name);
                            crc = f.crc;
                            name = f.name;
                            decSize = dstLen = size = fs.ReadUInt32();
                            dst = fs.ReadToBuffer(size);
                        }
                        else
                        {
                            name = fs.ReadStringASCIINull();
                            crc = fs.ReadUInt32();
                            decSize = fs.ReadUInt32();
                            size = fs.ReadUInt32();
                            byte[] src = fs.ReadToBuffer(size);
                            dst = new byte[decSize];
                            dstLen = ZlibHelper.Zlib.Decompress(src, size, dst);
                        }

                        _mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileNameWithoutExtension(filenameMod) +
                            " - Texture " + (i + 1) + " of " + numTextures + " - " + name);
                        if (extract)
                        {
                            string filename = name + "-" + string.Format("0x{0:X8}", crc) + ".dds";
                            using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                            {
                                output.Write(dst, 0, (int)dstLen);
                            }
                            continue;
                        }
                        if (store)
                        {
                            outFile.WriteStringASCIINull(name);
                            outFile.WriteUInt32(crc);
                            outFile.WriteUInt32(decSize);
                            byte[] src = fs.ReadToBuffer((int)fs.Length);
                            dst = ZlibHelper.Zlib.Compress(src);
                            outFile.WriteInt32(dst.Length);
                            outFile.WriteFromBuffer(dst);
                            continue;
                        }
                        if (previewIndex != -1)
                        {
                            if (i != previewIndex)
                            {
                                continue;
                            }
                            DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                            pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                            break;
                        }
                        else
                        {
                            FoundTexture foundTexture = _textures.Find(s => s.crc == crc && s.name == name);
                            if (foundTexture.crc != 0)
                            {
                                if (replace)
                                {
                                    DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                                    replaceTexture(image, foundTexture.list);
                                }
                                else
                                {
                                    ListViewItem item = new ListViewItem(foundTexture.displayName + " (" + foundTexture.packageName + ")");
                                    item.Name = i.ToString();
                                    listViewTextures.Items.Add(item);
                                }
                            }
                        }
                    }
                    if (store)
                        outFile.Close();
                }
                if (previewIndex == -1 && !store && !extract && !replace && !store)
                {
                    listViewTextures.EndUpdate();
                }
            }
        }

        void packTextureMod(string inDir, string outFile)
        {
            string[] files = Directory.GetFiles(inDir, "*.dds");

            using (FileStream outFs = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                outFs.WriteUInt32(TextureModTag);
                outFs.WriteUInt32(TextureModVersion);
                outFs.WriteUInt32((uint)_gameSelected);
                outFs.WriteInt32(files.Count());
                for (int n = 0; n < files.Count(); n++)
                {
                    string file = files[n];
                    _mainWindow.updateStatusLabel("Processing MOD: " + Path.GetFileNameWithoutExtension(outFile));
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        string crcStr = Path.GetFileNameWithoutExtension(file);
                        if (crcStr.Contains("_0x"))
                        {
                            crcStr = Path.GetFileNameWithoutExtension(file).Split('_').Last().Substring(2, 8); // in case filename contain CRC 
                        }
                        else
                        {
                            crcStr = Path.GetFileNameWithoutExtension(file).Split('-').Last().Substring(2, 8); // in case filename contain CRC 
                        }
                        uint crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                        if (crc == 0)
                        {
                            MessageBox.Show("Wrong format of texture filename: " + file);
                            return;
                        }

                        string textureName = "";
                        for (int l = 0; l < _textures.Count; l++)
                        {
                            FoundTexture foundTexture = _textures[l];
                            if (crc != 0 && foundTexture.crc == crc)
                            {
                                textureName = foundTexture.name;
                            }
                        }
                        if (textureName == "")
                        {
                            MessageBox.Show("Texture not match: " + file);
                            textureName = "!Unknown";
                        }
                        _mainWindow.updateStatusLabel2("Texture " + (n + 1) + " of " + files.Count() + ", Name: " + textureName);

                        outFs.WriteStringASCIINull(textureName);
                        outFs.WriteUInt32(crc);
                        byte[] src = fs.ReadToBuffer((int)fs.Length);
                        byte[] dst = ZlibHelper.Zlib.Compress(src);
                        outFs.WriteInt32(src.Length);
                        outFs.WriteInt32(dst.Length);
                        outFs.WriteFromBuffer(dst);
                    }
                }
            }
        }

        private void updateViewFromListView()
        {
            if (listViewTextures.SelectedItems.Count == 0)
            {
                clearPreview();
                return;
            }
            if (listViewMods.Items.Count > 0)
            {
                previewShow = true;
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                previewTextureMod(listViewMods.SelectedItems[0].Name, index);
                _mainWindow.updateStatusLabel("Done.");
                pictureBoxPreview.Show();
                richTextBoxInfo.Hide();
            }
            else
            {
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                MatchedTexture nodeTexture = node.textures[index].list[0];
                Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                if (previewShow)
                {
                    byte[] textureData = texture.getImageData();
                    int width = texture.getTopMipmap().width;
                    int height = texture.getTopMipmap().height;
                    DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
                    pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, format, width, height);
                    pictureBoxPreview.Show();
                    richTextBoxInfo.Hide();
                }
                else
                {
                    string text = "";

                    text += "Texture name:  " + node.textures[index].name + "\n";
                    text += "Texture original CRC:  " + string.Format("0x{0:X8}", node.textures[index].crc) + "\n";
                    text += "Node name:     " + node.textures[index].displayName + "\n";
                    text += "Package name:  " + node.textures[index].packageName + "\n";
                    text += "Packages:\n";
                    for (int l = 0; l < node.textures[index].list.Count; l++)
                    {
                        text += "  Export Id:     " + node.textures[index].list[l].exportID + "\n";
                        text += "  Package path:  " + node.textures[index].list[l].path + "\n";
                    }
                    text += "Texture properties:\n";
                    for (int l = 0; l < texture.properties.texPropertyList.Count; l++)
                    {
                        text += texture.properties.getDisplayString(l);
                    }
                    for (int l = 0; l < texture.mipMapsList.Count; l++)
                    {
                        text += "MipMap: " + l + ", " + texture.mipMapsList[l].width + "x" + texture.mipMapsList[l].height + "\n";
                        text += "  StorageType: " + texture.mipMapsList[l].storageType + "\n";
                        text += "  DataOffset:  " + (int)texture.mipMapsList[l].dataOffset + "\n";
                        text += "  CompSize:    " + texture.mipMapsList[l].compressedSize + "\n";
                        text += "  UnCompSize:  " + texture.mipMapsList[l].uncompressedSize + "\n";
                    }
                    richTextBoxInfo.Text = text;
                    pictureBoxPreview.Hide();
                    richTextBoxInfo.Show();
                }
            }
        }

        private void searchTexture(string name, uint crc)
        {
            listViewResults.Clear();

            for (int l = 0; l < _textures.Count; l++)
            {
                FoundTexture foundTexture = _textures[l];
                if ((name != null && foundTexture.name == name) ||
                    (crc != 0 && foundTexture.crc == crc))
                {
                    ListViewItem item = new ListViewItem(foundTexture.displayName + " (" + foundTexture.packageName + ")");
                    item.Name = l.ToString();
                    listViewResults.Items.Add(item);
                }
            }
            if (listViewResults.Items.Count > 1)
            {
                listViewResults.Show();
                listViewTextures.Clear();
                clearPreview();
            }
            if (listViewResults.Items.Count == 1)
            {
                listViewTextures.Focus();
                selectFoundTexture(listViewResults.Items[0]);
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxInfo.Show();
            pictureBoxPreview.Hide();
            previewShow = false;
            updateViewFromListView();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxInfo.Hide();
            pictureBoxPreview.Show();
            previewShow = true;
            updateViewFromListView();
        }

        private void listViewTextures_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateViewFromListView();
        }

        private void listViewTextures_DoubleClick(object sender, EventArgs e)
        {
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void listViewTextures_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r')
                return;
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void selectFoundTexture(ListViewItem item)
        {
            int pos1 = item.Text.IndexOf('(');
            int pos2 = item.Text.IndexOf(')');
            string packageName = item.Text.Substring(pos1 + 1, pos2 - pos1 - 1);
            listViewResults.Hide();
            for (int l = 0; l < treeViewPackages.Nodes[0].Nodes.Count; l++)
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.Nodes[0].Nodes[l];
                if (node.Name == packageName)
                {
                    treeViewPackages.SelectedNode = node;
                    updateListViewTextures(node);
                    for (int i = 0; i < node.textures.Count; i++)
                    {
                        if (node.textures[i].name == item.Text.Split(' ')[0])
                        {
                            listViewTextures.FocusedItem = listViewTextures.Items[i];
                            listViewTextures.Items[i].Selected = true;
                            return;
                        }
                    }
                }
            }
        }

        private void listViewResults_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewResults.Items.Count == 0)
                return;
            selectFoundTexture(listViewResults.SelectedItems[0]);
        }

        private void replaceTexture(DDSImage image, List<MatchedTexture> list)
        {
            Texture firstTexture = null, arcTexture = null, cprTexture = null;

            for (int n = 0; n < list.Count; n++)
            {
                MatchedTexture nodeTexture = list[n];
                Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                {
                    texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                }

                if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                {
                    MessageBox.Show("DDS file must have mipmaps!");
                    break;
                }

                DDSFormat ddsFormat = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
                if (image.ddsFormat != ddsFormat)
                {
                    MessageBox.Show("DDS file not match expected texture format!");
                    break;
                }

                bool triggerCacheArc = false, triggerCacheCpr = false;
                string archiveFile = "";
                byte[] origGuid = null;
                if (texture.properties.exists("TextureFileCacheName"))
                {
                    origGuid = texture.properties.getProperty("TFCFileGuid").valueStruct;
                    string archive = texture.properties.getProperty("TextureFileCacheName").valueName;
                    archiveFile = archiveFile = Path.Combine(GameData.MainData, archive + ".tfc");
                    if (nodeTexture.path.Contains("\\DLC"))
                    {
                        string DLCname = Path.GetDirectoryName(Path.GetDirectoryName(nodeTexture.path)).Split('\\').Last();
                        archiveFile = Path.Combine(Path.GetDirectoryName((GameData.GamePath + nodeTexture.path)), archive + ".tfc");
                        if (!File.Exists(archiveFile))
                            archiveFile = Path.Combine(GameData.MainData, "Textures" + ".tfc");
                    }
                    long fileLength = new FileInfo(archiveFile).Length;
                    if (fileLength + 0x3000000 > 0x80000000)
                    {
                        archiveFile = "";
                        foreach (TFCTexture newGuid in guids)
                        {
                            archiveFile = Path.Combine(GameData.MainData, newGuid.name + ".tfc");
                            if (!File.Exists(archiveFile))
                            {
                                texture.properties.setNameValue("TextureFileCacheName", newGuid.name);
                                texture.properties.setStructValue("TFCFileGuid", "Guid", newGuid.guid);
                                using (FileStream fs = new FileStream(archiveFile, FileMode.CreateNew, FileAccess.Write))
                                {
                                    fs.WriteFromBuffer(newGuid.guid);
                                }
                                break;
                            }
                            else
                            {
                                fileLength = new FileInfo(archiveFile).Length;
                                if (fileLength + 0x3000000 < 0x80000000)
                                {
                                    texture.properties.setNameValue("TextureFileCacheName", newGuid.name);
                                    texture.properties.setStructValue("TFCFileGuid", "Guid", newGuid.guid);
                                    break;
                                }
                            }
                            archiveFile = "";
                        }
                        if (archiveFile == "")
                            throw new Exception("No free TFC texture file!");
                    }
                }

                if (n == 0)
                    _mainWindow.updateStatusLabel2("Preparing texture...");

                List<Texture.MipMap> mipmaps = new List<Texture.MipMap>();
                for (int m = 0; m < image.mipMaps.Count(); m++)
                {
                    Texture.MipMap mipmap = new Texture.MipMap();
                    mipmap.width = image.mipMaps[m].width;
                    mipmap.height = image.mipMaps[m].height;
                    if (texture.existMipmap(mipmap.width, mipmap.height))
                        mipmap.storageType = texture.getMipmap(mipmap.width, mipmap.height).storageType;
                    else
                    {
                        mipmap.storageType = texture.getTopMipmap().storageType;
                        if (_gameSelected == MeType.ME2_TYPE)
                        {
                            if (texture.properties.exists("TextureFileCacheName"))
                                mipmap.storageType = Texture.StorageTypes.extLZO;
                        }
                        else if (_gameSelected == MeType.ME3_TYPE)
                        {
                            if (texture.properties.exists("TextureFileCacheName"))
                            {
                                if (nodeTexture.path.Contains("\\DLC"))
                                    mipmap.storageType = Texture.StorageTypes.extUnc;
                                else
                                    mipmap.storageType = Texture.StorageTypes.extZlib;
                            }
                        }
                    }

                    if (mipmap.storageType == Texture.StorageTypes.extLZO)
                        mipmap.storageType = Texture.StorageTypes.extZlib;
                    if (mipmap.storageType == Texture.StorageTypes.pccLZO)
                        mipmap.storageType = Texture.StorageTypes.pccZlib;

                    mipmap.uncompressedSize = image.mipMaps[m].data.Length;
                    if (_gameSelected == MeType.ME1_TYPE)
                    { 
                        if (texture.properties.getProperty("Format").valueName == "PF_NormalMap_HQ" &&
                            (image.mipMaps[m].width < 4 || image.mipMaps[m].height < 4))
                        {
                            continue;
                        }
                        if (mipmap.storageType == Texture.StorageTypes.pccLZO ||
                            mipmap.storageType == Texture.StorageTypes.pccZlib)
                        {
                            if (n == 0)
                                mipmap.newData = texture.compressTexture(image.mipMaps[m].data, mipmap.storageType);
                            else
                                mipmap.newData = firstTexture.mipMapsList[m].newData;
                            mipmap.compressedSize = mipmap.newData.Length;
                        }
                        if (mipmap.storageType == Texture.StorageTypes.pccUnc)
                        {
                            mipmap.compressedSize = mipmap.uncompressedSize;
                            mipmap.newData = image.mipMaps[m].data;
                        }
                        if ((mipmap.storageType == Texture.StorageTypes.extLZO ||
                            mipmap.storageType == Texture.StorageTypes.extZlib) && n > 0)
                        {
                            mipmap.compressedSize = firstTexture.mipMapsList[m].compressedSize;
                            mipmap.dataOffset = firstTexture.mipMapsList[m].dataOffset;
                        }
                    }
                    else
                    {
                        if (_gameSelected == MeType.ME2_TYPE &&
                            (image.mipMaps[m].origWidth < 4 || image.mipMaps[m].origHeight < 4))
                        {
                            continue;
                        }
                        if (mipmap.storageType == Texture.StorageTypes.extZlib ||
                            mipmap.storageType == Texture.StorageTypes.extLZO)
                        {
                            if (cprTexture == null)
                            {
                                mipmap.newData = texture.compressTexture(image.mipMaps[m].data, mipmap.storageType);
                                triggerCacheCpr = true;
                            }
                            else
                            {
                                if (cprTexture.mipMapsList[m].width != mipmap.width ||
                                    cprTexture.mipMapsList[m].height != mipmap.height)
                                    throw new Exception();
                                mipmap.newData = cprTexture.mipMapsList[m].newData;
                            }
                            mipmap.compressedSize = mipmap.newData.Length;
                        }
                        if (mipmap.storageType == Texture.StorageTypes.pccUnc ||
                            mipmap.storageType == Texture.StorageTypes.extUnc)
                        {
                            mipmap.compressedSize = mipmap.uncompressedSize;
                            mipmap.newData = image.mipMaps[m].data;
                        }
                        if (mipmap.storageType == Texture.StorageTypes.extZlib ||
                            mipmap.storageType == Texture.StorageTypes.extLZO ||
                            mipmap.storageType == Texture.StorageTypes.extUnc)
                        {
                            if (arcTexture == null ||
                                !StructuralComparisons.StructuralEqualityComparer.Equals(
                                arcTexture.properties.getProperty("TFCFileGuid").valueStruct,
                                texture.properties.getProperty("TFCFileGuid").valueStruct) ||
                                !StructuralComparisons.StructuralEqualityComparer.Equals(origGuid,
                                texture.properties.getProperty("TFCFileGuid").valueStruct))
                            {
                                triggerCacheArc = true;
                                Texture.MipMap oldMipmap = texture.getMipmap(mipmap.width, mipmap.height);
                                if (StructuralComparisons.StructuralEqualityComparer.Equals(origGuid,
                                    texture.properties.getProperty("TFCFileGuid").valueStruct) &&
                                    oldMipmap.width != 0 && mipmap.newData.Length <= oldMipmap.compressedSize)
                                {
                                    using (FileStream fs = new FileStream(archiveFile, FileMode.Open, FileAccess.Write))
                                    {
                                        fs.JumpTo(oldMipmap.dataOffset);
                                        mipmap.dataOffset = oldMipmap.dataOffset;
                                        fs.WriteFromBuffer(mipmap.newData);
                                    }
                                }
                                else
                                {
                                    using (FileStream fs = new FileStream(archiveFile, FileMode.Open, FileAccess.Write))
                                    {
                                        fs.SeekEnd();
                                        mipmap.dataOffset = (uint)fs.Position;
                                        fs.WriteFromBuffer(mipmap.newData);
                                    }
                                }
                            }
                            else
                            {
                                if (arcTexture.mipMapsList[m].width != mipmap.width ||
                                    arcTexture.mipMapsList[m].height != mipmap.height)
                                    throw new Exception();
                                mipmap.dataOffset = arcTexture.mipMapsList[m].dataOffset;
                            }
                        }
                    }

                    mipmaps.Add(mipmap);
                    if (texture.mipMapsList.Count() == 1)
                        break;
                }
                texture.replaceMipMaps(mipmaps);
                texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                if (texture.properties.exists("MipTailBaseIdx"))
                    texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                _mainWindow.updateStatusLabel2("Applying package " + (n + 1) + " of " + list.Count + " - " + nodeTexture.path);
                MemoryStream newData = new MemoryStream();
                newData.WriteFromBuffer(texture.properties.toArray());
                newData.WriteFromBuffer(texture.toArray(0)); // filled later
                package.setExportData(nodeTexture.exportID, newData.ToArray());

                newData = new MemoryStream();
                newData.WriteFromBuffer(texture.properties.toArray());
                newData.WriteFromBuffer(texture.toArray(package.exportsTable[nodeTexture.exportID].dataOffset + (uint)newData.Position));
                package.setExportData(nodeTexture.exportID, newData.ToArray());

                if (_gameSelected == MeType.ME1_TYPE)
                {
                    if (n == 0)
                        firstTexture = texture;
                }
                else
                {
                    if (triggerCacheCpr)
                        cprTexture = texture;
                    if (triggerCacheArc)
                        arcTexture = texture;
                }
            }
        }

        private void replaceTexture()
        {
            if (listViewTextures.SelectedItems.Count == 0)
                return;

            OpenFileDialog selectDDS = new OpenFileDialog();
            selectDDS.Title = "Please select DDS file";
            selectDDS.Filter = "DDS file|*.dds";
            if (selectDDS.ShowDialog() != DialogResult.OK)
                return;

            bool startMod = sTARTModdingToolStripMenuItem.Enabled;
            bool endMod = eNDModdingToolStripMenuItem.Enabled;
            bool loadMod = loadMODsToolStripMenuItem.Enabled;
            bool clearMod = clearMODsToolStripMenuItem.Enabled;
            bool packMod = packMODToolStripMenuItem.Enabled;
            EnableMenuOptions(false);

            DDSImage image = new DDSImage(selectDDS.FileName);
            PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
            ListViewItem item = listViewTextures.FocusedItem;
            int index = Convert.ToInt32(item.Name);

            replaceTexture(image, node.textures[index].list);

            if (moddingEnable)
            {
                using (FileStream fs = new FileStream(selectDDS.FileName, FileMode.Open, FileAccess.Read))
                {
                    fileStreamMod.WriteStringASCIINull(node.textures[index].name);
                    fileStreamMod.WriteUInt32(node.textures[index].crc);
                    byte[] src = fs.ReadToBuffer((int)fs.Length);
                    byte[] dst = ZlibHelper.Zlib.Compress(src);
                    fileStreamMod.WriteInt32(src.Length);
                    fileStreamMod.WriteInt32(dst.Length);
                    fileStreamMod.WriteFromBuffer(dst);
                }
                numberOfTexturesMod++;
            }
            else
            {
                cachePackageMgr.CloseAllWithSave();
            }

            EnableMenuOptions(true);
            sTARTModdingToolStripMenuItem.Enabled = startMod;
            eNDModdingToolStripMenuItem.Enabled = endMod;
            loadMODsToolStripMenuItem.Enabled = loadMod;
            clearMODsToolStripMenuItem.Enabled = clearMod;
            packMODToolStripMenuItem.Enabled = packMod;
            if (moddingEnable)
                switchModMode(true);
            listViewTextures.Focus();
            item.Selected = false;
            item.Selected = true;
            item.Focused = true;
        }

        private void replaceTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void removeEmptyMipmapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to proceed?", "Remove empty mipmaps", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return;

            EnableMenuOptions(false);

            _mainWindow.GetPackages(gameData);
            if (_gameSelected == MeType.ME1_TYPE)
                sortPackagesME1();

            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                _mainWindow.updateStatusLabel("Remove empty mipmaps, package " + (i + 1) + " of " + GameData.packageFiles.Count);
                _mainWindow.updateStatusLabel2("");
                Package package = cachePackageMgr.OpenPackage(GameData.packageFiles[i]);
                for (int l = 0; l < package.exportsTable.Count; l++)
                {
                    int id = package.getClassNameId(package.exportsTable[l].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        Texture texture = new Texture(package, l, package.getExportData(l));
                        if (!texture.hasImageData() ||
                            !texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                        {
                            continue;
                        }
                        do
                        {
                            texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                        } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));
                        texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                        texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                        texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                        if (_gameSelected == MeType.ME1_TYPE && package.compressed)
                        {
                            if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extLZO) ||
                                texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extZlib) ||
                                texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extUnc))
                            {
                                string textureName = package.exportsTable[l].objectName;
                                FoundTexture foundTexName = _textures.Find(s => s.name == textureName && s.packageName == texture.packageName);
                                Package refPkg = cachePackageMgr.OpenPackage(GameData.GamePath + foundTexName.list[0].path);
                                int refExportId = foundTexName.list[0].exportID;
                                byte[] refData = refPkg.getExportData(refExportId);
                                Texture refTexture = new Texture(refPkg, refExportId, refData);

                                if (texture.mipMapsList.Count != refTexture.mipMapsList.Count)
                                    throw new Exception("");
                                for (int t = 0; t < texture.mipMapsList.Count; t++)
                                {
                                    Texture.MipMap mipmap = texture.mipMapsList[t];
                                    if (mipmap.storageType == Texture.StorageTypes.extLZO ||
                                        mipmap.storageType == Texture.StorageTypes.extZlib ||
                                        mipmap.storageType == Texture.StorageTypes.extUnc)
                                    {
                                        mipmap.dataOffset = refPkg.exportsTable[refExportId].dataOffset + (uint)refTexture.properties.propertyEndOffset + refTexture.mipMapsList[t].internalOffset;
                                        texture.mipMapsList[t] = mipmap;
                                    }
                                }
                            }
                        }

                        MemoryStream newData = new MemoryStream();
                        newData.WriteFromBuffer(texture.properties.toArray());
                        newData.WriteFromBuffer(texture.toArray(package.exportsTable[l].dataOffset + (uint)newData.Position));
                        package.setExportData(l, newData.ToArray());
                        modified = true;
                    }
                }
                if (!modified)
                    cachePackageMgr.ClosePackageWithoutSave(package);
            }
            cachePackageMgr.CloseAllWithSave();

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void switchModMode(bool enable)
        {
            sTARTModdingToolStripMenuItem.Enabled = !enable;
            eNDModdingToolStripMenuItem.Enabled = enable;
            loadMODsToolStripMenuItem.Enabled = !enable;
            clearMODsToolStripMenuItem.Enabled = false;
            packMODToolStripMenuItem.Enabled = !enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void sTARTModdingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switchModMode(true);
            numberOfTexturesMod = 0;
            moddingEnable = true;
            fileStreamMod = File.Create(TempModFileName);
            fileStreamMod.Seek(TextureModHeaderLength, SeekOrigin.Begin);
        }

        private void eNDModdingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileStreamMod.SeekBegin();
            fileStreamMod.WriteUInt32(TextureModTag);
            fileStreamMod.WriteUInt32(TextureModVersion);
            fileStreamMod.WriteUInt32((uint)_gameSelected);
            fileStreamMod.WriteUInt32(numberOfTexturesMod);
            fileStreamMod.Close();

            if (numberOfTexturesMod > 0)
            {
                SaveFileDialog modFile = new SaveFileDialog();
                modFile.Title = "Please select new name for Mod file";
                modFile.Filter = "MOD file|*.mod";
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    File.Move(TempModFileName, modFile.FileName);
                }
            }
            if (File.Exists(TempModFileName))
                File.Delete(TempModFileName);

            cachePackageMgr.CloseAllWithSave();

            moddingEnable = false;
            switchModMode(false);
        }

        private void switchModsMode(bool enable)
        {
            sTARTModdingToolStripMenuItem.Enabled = !enable;
            eNDModdingToolStripMenuItem.Enabled = !enable;
            loadMODsToolStripMenuItem.Enabled = enable;
            clearMODsToolStripMenuItem.Enabled = enable;
            packMODToolStripMenuItem.Enabled = true;
            searchToolStripMenuItem.Enabled = !enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void loadMODsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog modFile = new OpenFileDialog();
            modFile.Title = "Please select Mod file";
            modFile.Filter = "MOD file | *.mod; *.tpf";
            modFile.Multiselect = true;
            if (modFile.ShowDialog() != DialogResult.OK)
                return;

            EnableMenuOptions(false);

            listViewMods.Show();

            listViewTextures.Clear();
            clearPreview();

            string[] files = modFile.FileNames;
            foreach (string file in files)
            {
                bool legacy = false;
                bool tpf = false;
                _mainWindow.updateStatusLabel("MOD: " + Path.GetFileNameWithoutExtension(file) + " loading...");
                if (Path.GetExtension(file).ToLower() == ".tpf")
                {
                    tpf = true;
                }
                else
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != TextureModTag || version != TextureModVersion)
                        {
                            fs.SeekBegin();
                            if (!checkTextureMod(fs))
                            {
                                MessageBox.Show("File " + file + " is not MOD, omitting...");
                                continue;
                            }
                            legacy = true;
                        }
                    }
                }
                string desc;
                if (legacy)
                    desc = Path.GetFileNameWithoutExtension(file) + " (Legacy MOD)";
                else if (tpf)
                    desc = Path.GetFileNameWithoutExtension(file) + " (TPF - View Only)";
                else
                    desc = Path.GetFileNameWithoutExtension(file);
                ListViewItem item = new ListViewItem(desc);
                item.Name = file;
                listViewMods.Items.Add(item);
            }
            _mainWindow.updateStatusLabel("MODs loaded.");
            _mainWindow.updateStatusLabel2("");
            EnableMenuOptions(true);
            if (listViewMods.Items.Count == 0)
                clearMODsView();
            switchModsMode(true);
        }

        private void clearMODsView()
        {
            listViewMods.Items.Clear();
            listViewMods.Hide();

            listViewTextures.Clear();
            clearPreview();

            switchModMode(false);
            EnableMenuOptions(true);
            clearMODsToolStripMenuItem.Enabled = false;
        }

        private void clearMODsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearMODsView();
        }

        private void applyModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);
            foreach (ListViewItem item in listViewMods.SelectedItems)
            {
                replaceTextureMod(item.Name);
                _mainWindow.updateStatusLabel("MOD: " + item.Text + " applying...");
                listViewMods.Items.Remove(item);
            }
            _mainWindow.updateStatusLabel("");
            cachePackageMgr.CloseAllWithSave();
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MODs applied.");
            _mainWindow.updateStatusLabel2("");
            if (listViewMods.Items.Count == 0)
                clearMODsView();
        }

        private void deleteModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);
            foreach (ListViewItem item in listViewMods.SelectedItems)
                listViewMods.Items.Remove(item);

            listViewTextures.Clear();
            clearPreview();

            EnableMenuOptions(true);
            if (listViewMods.Items.Count == 0)
                clearMODsView();
        }

        private void listViewMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            listViewTextures.Clear();
            clearPreview();

            if (listViewMods.SelectedItems.Count != 1)
                return;

            listTextureMod(listViewMods.SelectedItems[0].Name);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void saveModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);

            FolderBrowserDialog modFile = new FolderBrowserDialog();
            if (modFile.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in listViewMods.SelectedItems)
                {
                    _mainWindow.updateStatusLabel("MOD: " + item.Text + "saving...");
                    saveTextureMod(item.Name, modFile.SelectedPath);
                    _mainWindow.updateStatusLabel2("");
                }
            }
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MODs saved.");
            _mainWindow.updateStatusLabel2("");
        }

        private void extractModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);

            FolderBrowserDialog modFile = new FolderBrowserDialog();
            if (modFile.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in listViewMods.SelectedItems)
                {
                    string outDir = Path.Combine(modFile.SelectedPath, Path.GetFileNameWithoutExtension(item.Name));
                    Directory.CreateDirectory(outDir);
                    extractTextureMod(item.Name, outDir);
                    _mainWindow.updateStatusLabel("MOD: " + item.Text + "extracting...");
                    _mainWindow.updateStatusLabel2("");
                }
            }
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MODs extracted.");
            _mainWindow.updateStatusLabel2("");
        }

        private void packMODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);

            FolderBrowserDialog modFile = new FolderBrowserDialog();
            if (modFile.ShowDialog() == DialogResult.OK)
            {
                _mainWindow.updateStatusLabel("MOD packing...");
                _mainWindow.updateStatusLabel2("");
                packTextureMod(modFile.SelectedPath, Path.Combine(Path.GetDirectoryName(modFile.SelectedPath), Path.GetFileName(modFile.SelectedPath)) + ".mod");
            }

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MOD packed.");
            _mainWindow.updateStatusLabel2("");
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture name or CRC", "", "", 0, 0);
            if (string.IsNullOrEmpty(name))
                return;

            string crcStr = name;
            if (crcStr.Contains("_0x"))
            {
                crcStr = name.Split('_').Last().Substring(2, 8); // in case filename contain CRC 
            }
            else
            {
                crcStr = name.Split('-').Last(); // in case filename contain CRC
                if (crcStr == "")
                {
                    crcStr = name;
                }
            }

            uint crc = 0;
            try
            {
                if (crcStr.Substring(0, 2).ToLower() == "0x")
                    crcStr = crcStr.Substring(2, 8);
                else
                    crcStr = crcStr.Substring(0, 8);
                crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                searchTexture("", crc);
            }
            catch
            {
                name = name.Split('.')[0]; // in case filename
                searchTexture(name, 0);
            }
        }
    }
}
