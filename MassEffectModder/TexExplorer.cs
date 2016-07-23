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

namespace MassEffectModder
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

        public TexExplorer(MainWindow main, MeType gameType)
        {
            InitializeComponent();
            _mainWindow = main;
            _gameSelected = gameType;
            _configIni = main._configIni;
            gameData = new GameData(_gameSelected, _configIni);
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
                _mainWindow.updateStatusLabel("Preparing tree...");
                PrepareListOfTextures();
                _mainWindow.updateStatusLabel("Done.");
            }

            PrepareTreeList();

            EnableMenuOptions(true);
        }

        void sortPackagesME1()
        {
            _mainWindow.updateStatusLabel("Sorting packages...");
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
                            mipmap.storageType == Texture.StorageTypes.pccCpr)
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

        private uint ParseLegacyScriptMod(string script, string textureName)
        {
            Regex parts = new Regex("pccs.Add[(]\"[A-z,0-9/,..]*\"");
            Match match = parts.Match(script);
            if (match.Success)
            {
                string packageName = match.ToString().Split('\"')[1].Split('/').Last().Split('.')[0];
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
                                            return _textures[i].crc;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return 0;
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
                    uint crc = ParseLegacyScriptMod(script, textureName);
                    if (crc == 0)
                    {
                        MessageBox.Show("Not able match texture: " + textureName + " in MOD");
                    }
                    len = fs.ReadInt32();
                    if (len == 0)
                        return false;
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
                                    Path.GetFileNameWithoutExtension(filenameMod) + ", Texture: " + name);

                            if (store)
                            {
                                outFile.WriteStringASCIINull(name);
                                outFile.WriteUInt32(crc);
                                outFile.WriteUInt32((uint)entry.Size);
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
                            else if (store || extract)
                            {
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outFile.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                if (extract)
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
                    }
                    int numTextures = fs.ReadInt32();
                    if (store)
                        outFile.WriteInt32(numTextures);
                    for (int i = 0; i < numTextures; i++)
                    {
                        string name;
                        uint crc, size;
                        if (legacy)
                        {
                            int len = fs.ReadInt32();
                            name = fs.ReadStringASCII(len);
                            name = name.Split(' ').Last();
                            len = fs.ReadInt32();
                            string scriptLegacy = fs.ReadStringASCII(len);
                            crc = ParseLegacyScriptMod(scriptLegacy, name);
                        }
                        else
                        {
                            name = fs.ReadStringASCIINull();
                            crc = fs.ReadUInt32();
                        }
                        size = fs.ReadUInt32();
                        _mainWindow.updateStatusLabel("Processing MOD: " +
                            Path.GetFileNameWithoutExtension(filenameMod) + ", Texture: " + name);
                        if (extract)
                        {
                            string filename = name + "-" + string.Format("0x{0:X8}", crc) + ".dds";
                            using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                            {
                                output.WriteFromStream(fs, size);
                            }
                            continue;
                        }
                        if (store)
                        {
                            outFile.WriteStringASCIINull(name);
                            outFile.WriteUInt32(crc);
                            outFile.WriteUInt32(size);
                            outFile.WriteFromStream(fs, size);
                            continue;
                        }
                        if (previewIndex != -1)
                        {
                            if (i != previewIndex)
                            {
                                fs.Skip(size);
                                continue;
                            }
                            DDSImage image = new DDSImage(new MemoryStream(fs.ReadToBuffer(size)));
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
                                    DDSImage image = new DDSImage(new MemoryStream(fs.ReadToBuffer(size)));
                                    replaceTexture(image, foundTexture.list);
                                }
                                else
                                {
                                    fs.Skip(size);
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
            }
        }

        void packTextureMod(string inDir, string outFile)
        {
            string[] files = Directory.GetFiles(inDir, "*.dds");

            using (FileStream outFs = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                outFs.WriteUInt32(TextureModTag);
                outFs.WriteUInt32(TextureModVersion);
                outFs.WriteInt32(files.Count());
                foreach (string file in files)
                {
                    _mainWindow.updateStatusLabel("Processing MOD: " + Path.GetFileNameWithoutExtension(outFile));
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        string textureName = Path.GetFileNameWithoutExtension(file).Split('-').First();
                        uint crc = uint.Parse(Path.GetFileNameWithoutExtension(file).Split('-').Last().Substring(2), System.Globalization.NumberStyles.HexNumber);
                        if (textureName == "" || crc == 0)
                        {
                            MessageBox.Show("Wrong format of texture filename: " + file);
                            File.Delete(outFile);
                            return;
                        }
                        outFs.WriteStringASCIINull(textureName);
                        outFs.WriteUInt32(crc);
                        outFs.WriteUInt32((uint)fs.Length);
                        outFs.WriteFromStream(fs, fs.Length);
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
                Package package = new Package(GameData.GamePath + nodeTexture.path);
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
                package.Dispose();
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
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
        }

        private void listViewTextures_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r')
                return;
            _mainWindow.updateStatusLabel("Replacing texture...");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
        }

        private void byNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture name", "", "", 0, 0);
            if (string.IsNullOrEmpty(name))
                return;

            name = name.Split('.')[0]; // in case filename
            searchTexture(name, 0);
        }

        private void byCRCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string crc = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture CRC", "", "", 0, 0);
            if (string.IsNullOrEmpty(crc))
                return;

            if (crc.Substring(0, 2).ToLower() == "0x")
                crc = crc.Substring(2);
            searchTexture(null, uint.Parse(crc, System.Globalization.NumberStyles.HexNumber));
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
            Texture firstTexture = null;

            for (int n = 0; n < list.Count; n++)
            {
                MatchedTexture nodeTexture = list[n];
                Package package = new Package(GameData.GamePath + nodeTexture.path);
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
                    MessageBox.Show("DDS file not match texture format!");
                    break;
                }

                List<Texture.MipMap> mipmaps = new List<Texture.MipMap>();
                for (int m = 0; m < image.mipMaps.Count(); m++)
                {
                    Texture.MipMap mipmap = new Texture.MipMap();
                    mipmap.storageType = texture.getStorageType(image.mipMaps[m].width, image.mipMaps[m].height);
                    mipmap.uncompressedSize = image.mipMaps[m].data.Length;
                    mipmap.width = image.mipMaps[m].width;
                    mipmap.height = image.mipMaps[m].height;
                    if (mipmap.storageType == Texture.StorageTypes.pccCpr ||
                        mipmap.storageType == Texture.StorageTypes.arcCpr ||
                        (mipmap.storageType == Texture.StorageTypes.extCpr && _gameSelected != MeType.ME1_TYPE))
                    {
                        if (mipmap.width < 4 && mipmap.height < 4)
                            continue;
                        if (n == 0)
                            mipmap.newData = texture.compressTexture(image.mipMaps[m].data);
                        else
                            mipmap.newData = firstTexture.mipMapsList[m].newData;
                        mipmap.compressedSize = mipmap.newData.Length;
                    }
                    if (mipmap.storageType == Texture.StorageTypes.pccUnc ||
                        mipmap.storageType == Texture.StorageTypes.extUnc)
                    {
                        mipmap.compressedSize = mipmap.uncompressedSize;
                        mipmap.newData = image.mipMaps[m].data;
                    }
                    if (mipmap.storageType == Texture.StorageTypes.extCpr && _gameSelected == MeType.ME1_TYPE)
                    {
                        if (mipmap.width < 4 && mipmap.height < 4)
                            continue;
                        mipmap.compressedSize = firstTexture.mipMapsList[m].compressedSize;
                    }
                    if ((_gameSelected == MeType.ME2_TYPE ||
                         _gameSelected == MeType.ME3_TYPE) &&
                        (mipmap.storageType == Texture.StorageTypes.arcCpr ||
                         mipmap.storageType == Texture.StorageTypes.extCpr ||
                         mipmap.storageType == Texture.StorageTypes.extUnc))
                    {
                        if (n == 0)
                        {
                            string archive = texture.properties.getProperty("TextureFileCacheName").valueName + ".tfc";
                            string filename = GameData.tfcFiles.Find(s => Path.GetFileName(s).Equals(archive, StringComparison.OrdinalIgnoreCase));
                            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Write))
                            {
                                Texture.MipMap oldMipmap = texture.getMipmap(mipmap.width, mipmap.height);
                                if (oldMipmap.width == 0 || mipmap.newData.Length > oldMipmap.compressedSize)
                                {
                                    if (oldMipmap.width != 0)
                                    {
                                        fs.JumpTo(oldMipmap.dataOffset);
                                        fs.WriteZeros(oldMipmap.compressedSize);
                                    }
                                    fs.SeekEnd();
                                    mipmap.dataOffset = (uint)fs.Position;
                                    fs.WriteFromBuffer(mipmap.newData);
                                }
                                else
                                {
                                    fs.JumpTo(oldMipmap.dataOffset);
                                    mipmap.dataOffset = (uint)fs.Position;
                                    fs.WriteFromBuffer(mipmap.newData);
                                    fs.WriteZeros(oldMipmap.compressedSize - mipmap.newData.Length);
                                }
                            }
                        }
                        else
                        {
                            mipmap.dataOffset = firstTexture.mipMapsList[m].dataOffset;
                        }
                    }
                    if (_gameSelected == MeType.ME1_TYPE && n > 0 && mipmap.storageType == Texture.StorageTypes.extCpr)
                    {
                        mipmap.dataOffset = firstTexture.mipMapsList[m].dataOffset;
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

                MemoryStream newData = new MemoryStream();
                newData.WriteFromBuffer(texture.properties.toArray());
                newData.WriteFromBuffer(texture.toArray(0)); // filled later
                package.setExportData(nodeTexture.exportID, newData.ToArray());

                newData = new MemoryStream();
                newData.WriteFromBuffer(texture.properties.toArray());
                newData.WriteFromBuffer(texture.toArray(package.exportsTable[nodeTexture.exportID].dataOffset + (uint)newData.Position));
                package.setExportData(nodeTexture.exportID, newData.ToArray());

                if (n == 0)
                    firstTexture = texture;

                _mainWindow.updateStatusLabel("Saving package: " + nodeTexture.path);
                package.SaveToFile();
                package.Dispose();
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
                    fileStreamMod.WriteUInt32((uint)fs.Length);
                    fileStreamMod.WriteFromStream(fs, fs.Length);
                }
                numberOfTexturesMod++;
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
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
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
                Package package = new Package(GameData.packageFiles[i]);
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
                            if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extCpr) ||
                                texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extUnc))
                            {
                                string textureName = package.exportsTable[l].objectName;
                                FoundTexture foundTexName = _textures.Find(s => s.name == textureName && s.packageName == texture.packageName);
                                Package refPkg = new Package(GameData.GamePath + foundTexName.list[0].path);
                                int refExportId = foundTexName.list[0].exportID;
                                byte[] refData = refPkg.getExportData(refExportId);
                                Texture refTexture = new Texture(refPkg, refExportId, refData);

                                if (texture.mipMapsList.Count != refTexture.mipMapsList.Count)
                                    throw new Exception("");
                                for (int t = 0; t < texture.mipMapsList.Count; t++)
                                {
                                    Texture.MipMap mipmap = texture.mipMapsList[t];
                                    if (mipmap.storageType == Texture.StorageTypes.extCpr ||
                                        mipmap.storageType == Texture.StorageTypes.extUnc)
                                    {
                                        mipmap.dataOffset = refPkg.exportsTable[refExportId].dataOffset + (uint)refTexture.properties.propertyEndOffset + refTexture.mipMapsList[t].internalOffset;
                                        texture.mipMapsList[t] = mipmap;
                                    }
                                }
                                refPkg.Dispose();
                            }
                        }

                        MemoryStream newData = new MemoryStream();
                        newData.WriteFromBuffer(texture.properties.toArray());
                        newData.WriteFromBuffer(texture.toArray(package.exportsTable[l].dataOffset + (uint)newData.Position));
                        package.setExportData(l, newData.ToArray());
                        modified = true;
                    }
                }
                if (modified)
                    package.SaveToFile();

                package.Dispose();
            }

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("Done.");
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
                _mainWindow.updateStatusLabel("Done.");
                listViewMods.Items.Remove(item);
            }
            EnableMenuOptions(true);
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
                    saveTextureMod(item.Name, modFile.SelectedPath);
                    _mainWindow.updateStatusLabel("Done.");
                }
            }
            EnableMenuOptions(true);
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
                    _mainWindow.updateStatusLabel("Done.");
                }
            }
            EnableMenuOptions(true);
        }

        private void packMODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);

            FolderBrowserDialog modFile = new FolderBrowserDialog();
            if (modFile.ShowDialog() == DialogResult.OK)
            {
                packTextureMod(modFile.SelectedPath, Path.Combine(Path.GetDirectoryName(modFile.SelectedPath), Path.GetFileName(modFile.SelectedPath)) + ".mod");
            }
            _mainWindow.updateStatusLabel("Done.");

            EnableMenuOptions(true);
        }
    }
}
