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
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class TexExplorer : Form
    {
        private void PrepareListOfTextures()
        {
            _textures = new List<FoundTexture>();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filename = Path.Combine(path, "me" + (int)_gameSelected + "map.bin");
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

                GameData.packageFiles.Sort();
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
                _mainWindow.updateStatusLabel("");
            }
        }

        private void PrepareTreeList()
        {
            nodeList = new List<PackageTreeNode>();
            PackageTreeNode rootNode = new PackageTreeNode("All Packages");
            for (int l = 0; l < _textures.Count; l++)
            {
                bool found = false;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].Name == _textures[l].packageName)
                    {
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

        void sortPackagesME1()
        {
            _mainWindow.updateStatusLabel("Sorting packages...");
            _mainWindow.updateStatusLabel("");
            List<string> sortedList = new List<string>();
            List<string> restList = new List<string>();
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                Package package = new Package(GameData.packageFiles[i], false, true);
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
                            uint crc = texture.getCrcTopMipmap();
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
                                _textures.Add(foundTex);
                            }
                        }
                        else
                        {
                            FoundTexture foundTexName;
                            List<FoundTexture> foundList = _textures.FindAll(s => s.name == name && s.packageName == texture.packageName);
                            if (foundList.Count > 1)
                                foundTexName = _textures.Find(s => s.name == name && s.packageName == texture.packageName && s.crc == texture.getCrcTopMipmap());
                            else
                                foundTexName = foundList[0];
                            foundTexName.list.Add(matchTexture);
                        }
                    }
                    else
                    {
                        uint crc = texture.getCrcTopMipmap();
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
                            _textures.Add(foundTex);
                        }
                    }
                }
            }
            package.Dispose();
        }

    }
}
