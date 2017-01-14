/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
    public partial class TreeScan
    {
        public List<FoundTexture> PrepareListOfTextures(TexExplorer texEplorer, MainWindow mainWindow, Installer installer, bool force = false)
        {
            List<FoundTexture> textures = new List<FoundTexture>();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filename = Path.Combine(path, "me" + (int)GameData.gameType + "map.bin");
            if (force && File.Exists(filename))
                File.Delete(filename);

            if (File.Exists(filename))
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.textureMapBinTag || version != TexExplorer.textureMapBinVersion)
                    {
                        if (mainWindow != null)
                        {
                            MessageBox.Show("Wrong " + filename + " file!");
                            mainWindow.updateStatusLabel("");
                            mainWindow.updateStatusLabel2("");
                            texEplorer.Close();
                        }
                        return null;
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
                        textures.Add(texture);
                    }
                    if (fs.Position < new FileInfo(filename).Length)
                    {
                        List<string> packages = new List<string>();
                        int numPackages = fs.ReadInt32();
                        for (int i = 0; i < numPackages; i++)
                        {
                            string pkgPath = fs.ReadStringASCIINull();
                            packages.Add(Path.Combine(GameData.GamePath, pkgPath));
                        }
                    }
                    else
                    {
                        fs.SeekEnd();
                        fs.WriteInt32(GameData.packageFiles.Count);
                        for (int i = 0; i < GameData.packageFiles.Count; i++)
                        {
                            fs.WriteStringASCIINull(GameData.RelativeGameData(GameData.packageFiles[i]));
                        }
                    }
                }
            }
            else
            {
                if (mainWindow != null)
                {
                    DialogResult result = MessageBox.Show("Replacing textures and creating mods require textures mapping.\n" +
                    "It's one time only process.\n\n" +
                    "IMPORTANT! Make sure game data is not modified.\n\n" +
                    "Are you sure to proceed?", "Textures mapping", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No)
                    {
                        texEplorer.Close();
                        return null;
                    }
                }

                GameData.packageFiles.Sort();
                if (GameData.gameType == MeType.ME1_TYPE)
                    sortPackagesME1(mainWindow, installer);
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("Find textures in package " + (i + 1) + " of " + GameData.packageFiles.Count + " - " + GameData.packageFiles[i]);
                    }
                    if (installer != null)
                    {
                        installer.updateStatusScan("Progress... " + (i * 100 / GameData.packageFiles.Count) + " % ");
                    }
                    FindTextures(textures, GameData.packageFiles[i]);
                }

                using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    fs.WriteUInt32(TexExplorer.textureMapBinTag);
                    fs.WriteUInt32(TexExplorer.textureMapBinVersion);
                    fs.WriteInt32(textures.Count);
                    for (int i = 0; i < textures.Count; i++)
                    {
                        fs.WriteStringASCIINull(textures[i].name);
                        fs.WriteUInt32(textures[i].crc);
                        fs.WriteStringASCIINull(textures[i].packageName);
                        fs.WriteInt32(textures[i].list.Count);
                        for (int k = 0; k < textures[i].list.Count; k++)
                        {
                            fs.WriteInt32(textures[i].list[k].exportID);
                            fs.WriteStringASCIINull(textures[i].list[k].path);
                        }
                    }
                    fs.WriteInt32(GameData.packageFiles.Count);
                    for (int i = 0; i < GameData.packageFiles.Count; i++)
                    {
                        fs.WriteStringASCIINull(GameData.RelativeGameData(GameData.packageFiles[i]));
                    }
                }
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Done.");
                    mainWindow.updateStatusLabel("");
                }
            }
            return textures;
        }

        public void sortPackagesME1(MainWindow mainWindow, Installer installer)
        {
            if (mainWindow != null)
            {
                mainWindow.updateStatusLabel("Sorting packages...");
                mainWindow.updateStatusLabel("");
            }
            List<string> sortedList = new List<string>();
            List<string> restList = new List<string>();
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                Package package = new Package(GameData.packageFiles[i], false, true);
                if (!package.compressed)
                    sortedList.Add(GameData.packageFiles[i]);
                else
                    restList.Add(GameData.packageFiles[i]);
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Sorting packages... " + (i + 1) + " of " + GameData.packageFiles.Count);
                }
                if (installer != null)
                {
                    installer.updateStatusScan("Sorting packages... " + (i * 100 / GameData.packageFiles.Count) + " %");
                }
            }
            sortedList.AddRange(restList);
            GameData.packageFiles = sortedList;
            if (mainWindow != null)
            {
                mainWindow.updateStatusLabel("Done.");
                mainWindow.updateStatusLabel("");
            }
        }

        private void FindTextures(List<FoundTexture> textures, string packagePath)
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

                    if (GameData.gameType == MeType.ME1_TYPE)
                    {
                        if (mipmap.storageType == Texture.StorageTypes.pccUnc ||
                            mipmap.storageType == Texture.StorageTypes.pccLZO ||
                            mipmap.storageType == Texture.StorageTypes.pccZlib)
                        {
                            uint crc = texture.getCrcTopMipmap();
                            FoundTexture foundTexName = textures.Find(s => s.crc == crc);
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
                                textures.Add(foundTex);
                            }
                        }
                        else
                        {
                            FoundTexture foundTexName;
                            List<FoundTexture> foundList = textures.FindAll(s => s.name == name && s.packageName == texture.packageName);
                            if (foundList.Count > 1)
                                foundTexName = textures.Find(s => s.name == name && s.packageName == texture.packageName && s.crc == texture.getCrcTopMipmap());
                            else
                                foundTexName = foundList[0];
                            foundTexName.list.Add(matchTexture);
                        }
                    }
                    else
                    {
                        uint crc = texture.getCrcTopMipmap();
                        FoundTexture foundTexName = textures.Find(s => s.crc == crc);
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
                            textures.Add(foundTex);
                        }
                    }
                }
            }
            package.Dispose();
        }
    }

    public partial class TexExplorer : Form
    {
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
    }
}
