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
        public UInt32 crc;
        public int mipmapOffset;
        public string packageName;
        public string displayName;
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
        List<FoundTexture> _textures;

        public class PackageTreeNode : TreeNode
        {
            public List<FoundTexture> textures;

            public PackageTreeNode()
                : base()
            {
                textures = new List<FoundTexture>();
            }
            public PackageTreeNode(string name)
                : base()
            {
                this.Name = this.Text = name;
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

        public void Run()
        {
            _mainWindow.updateStatusLabel("");
            sTARTModdingToolStripMenuItem.Enabled = false;
            eNDModdingToolStripMenuItem.Enabled = false;

            if (_gameSelected == MeType.ME1_TYPE)
                _mainWindow.VerifyME1Exe(gameData);

            if (!_mainWindow.GetPackages(gameData))
            {
                Close();
                return;
            }
            else
            {
                _packageFiles = GameData.packageFiles;
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
                            Close();
                            return;
                        }

                        UInt32 countTexture = fs.ReadUInt32();
                        for (int i = 0; i < countTexture; i++)
                        {
                            FoundTexture texture = new FoundTexture();
                            texture.name = fs.ReadStringASCIINull();
                            texture.crc = fs.ReadUInt32();
                            texture.packageName = fs.ReadStringASCIINull();
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
                    {
                        Close();
                        return;
                    }

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

            listViewTextures.Clear();
            richTextBoxInfo.Clear();
            if (pictureBoxPreview.Image != null)
                pictureBoxPreview.Image.Dispose();

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
                            if (nodeList[i].textures[j].name == nodeList[i].Name)
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

            sTARTModdingToolStripMenuItem.Enabled = true;
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

        {

        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mainWindow.enableGameDataMenu(true);
        }
    }
}
