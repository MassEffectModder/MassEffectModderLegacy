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
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class TreeScan
    {
        public List<FoundTexture> treeScan = null;

        public string PrepareListOfTextures(TexExplorer texEplorer, CachePackageMgr cachePackageMgr, MainWindow mainWindow, Installer installer, ref string log, bool force = false)
        {
            string errors = "";
            treeScan = null;

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
                        fs.Close();
                        log += "Wrong " + filename + " file!" + Environment.NewLine;
                        return "Wrong " + filename + " file!" + Environment.NewLine;
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
                            pkgPath = GameData.GamePath + pkgPath;
                            packages.Add(pkgPath);
                        }
                        for (int i = 0; i < packages.Count; i++)
                        {
                            if (GameData.packageFiles.Find(s => s.Equals(packages[i], StringComparison.OrdinalIgnoreCase)) == null)
                            {
                                if (mainWindow != null)
                                {
                                    MessageBox.Show("Detected removal of game files since last game data scan." +
                                    "\n\nYou need to restore the game to vanilla state and reinstall vanilla DLCs and DLC mods." +
                                    "\n\nThen from the main menu, select 'Remove Textures Scan File' and start Texture Manager again.");
                                    return "";
                                }
                                else if (!force)
                                {
                                    errors += "Detected removal of game files since last game data scan." + Environment.NewLine + Environment.NewLine +
                                    "You need to restore the game to vanilla state and reinstall vanilla DLCs and DLC mods.";
                                    return "";
                                }
                            }
                        }
                        for (int i = 0; i < GameData.packageFiles.Count; i++)
                        {
                            if (packages.Find(s => s.Equals(GameData.packageFiles[i], StringComparison.OrdinalIgnoreCase)) == null)
                            {
                                if (mainWindow != null)
                                {
                                    MessageBox.Show("Detected additional game files not present in latest game data scan." +
                                    "\n\nYou need to restore the game to vanilla state and reinstall vanilla DLCs and DLC mods." +
                                    "\n\nThen from the main menu, select 'Remove Textures Scan File' and start Texture Manager again.");
                                    return "";
                                }
                                else if (!force)
                                {
                                    errors += "Detected additional game files not present in latest game data scan." + Environment.NewLine + Environment.NewLine +
                                    "You need to restore the game to vanilla state and reinstall vanilla DLCs and DLC mods.";
                                    return "";
                                }
                            }
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
                    treeScan = textures;
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("");
                        mainWindow.updateStatusLabel2("");
                    }
                    return errors;
                }
            }


            if (File.Exists(filename))
                File.Delete(filename);

            if (Misc.detectBrokenMod(GameData.gameType))
            {
                if (mainWindow != null)
                {
                    MessageBox.Show("Detected ME1 Controller or/and Faster Elevators mod!\nMEM will not work properly due broken content in mod.");
                }
                return "";
            }

            if (MipMaps.checkGameDataModded(cachePackageMgr))
            {
                if (mainWindow != null)
                {
                    MessageBox.Show("Detected modded game. Can not continue." +
                    "\n\nYou need to restore the game to vanilla state and reinstall vanilla DLCs and DLC mods." +
                    "\n\nThen start Texture Manager again.");
                    return "";
                }
                else if (!force)
                {
                    errors += "Detected modded game. Can not continue." + Environment.NewLine + Environment.NewLine +
                    "You need to restore the game to vanilla state and reinstall vanilla DLCs and DLC mods.";
                    return "";
                }
            }

            if (mainWindow != null)
            {
                DialogResult result = MessageBox.Show("Replacing textures and creating mods requires generating a map of the game's textures.\n" +
                "You only need to do it once.\n\n" +
                "IMPORTANT! Your game needs to be in vanilla state and have all original DLCs and DLC mods installed.\n\n" +
                "Are you sure you want to proceed?", "Textures mapping", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                {
                    texEplorer.Close();
                    return "";
                }
            }

            GameData.packageFiles.Sort();
            if (mainWindow != null)
                Misc.startTimer();
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Finding textures in package " + (i + 1) + " of " + GameData.packageFiles.Count + " - " + GameData.packageFiles[i]);
                }
                if (installer != null)
                    installer.updateStatusScan("Progress... " + (i * 100 / GameData.packageFiles.Count) + " % ");
                errors += FindTextures(textures, GameData.packageFiles[i], cachePackageMgr, ref log);
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
                MipMaps mipmaps = new MipMaps();
                if (GameData.gameType == MeType.ME1_TYPE)
                {
                    errors += mipmaps.removeMipMapsME1(1, textures, null, mainWindow, null);
                    errors += mipmaps.removeMipMapsME1(2, textures, null, mainWindow, null);
                }
                else
                {
                    errors += mipmaps.removeMipMapsME2ME3(textures, null, mainWindow, null);
                }

                var time = Misc.stopTimer();
                mainWindow.updateStatusLabel("Done. Process total time: " + Misc.getTimerFormat(time));
                mainWindow.updateStatusLabel2("");
            }
            treeScan = textures;
            return errors;
        }

        private string FindTextures(List<FoundTexture> textures, string packagePath, CachePackageMgr cachePackageMgr, ref string log)
        {
            string errors = "";
            Package package = null;

            try
            {
                if (cachePackageMgr != null)
                    package = cachePackageMgr.OpenPackage(packagePath);
                else
                    package = new Package(packagePath);
            }
            catch
            {
                errors += "The file is propably broken, skipped: " + packagePath + Environment.NewLine;
                log += "The file is propably broken, skipped: " + packagePath + Environment.NewLine;
                return errors;
            }
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
                    bool slave = false;
                    matchTexture.exportID = i;
                    matchTexture.path = GameData.RelativeGameData(packagePath);
                    if (GameData.gameType != MeType.ME1_TYPE)
                        slave = true;
                    else
                        if (texture.packageName.ToUpper() != Path.GetFileNameWithoutExtension(package.packageFile.Name).ToUpper())
                            slave = true;

                    uint crc = texture.getCrcTopMipmap();
                    if (crc == 0)
                    {
                        errors += "Error: Texture " + package.exportsTable[i].objectName + " is broken in package: " + packagePath + ", skipping..." + Environment.NewLine;
                        log += "Error: Texture " + package.exportsTable[i].objectName + " is broken in package: " + packagePath + ", skipping..." + Environment.NewLine;
                        continue;
                    }

                    FoundTexture foundTexName = textures.Find(s => s.crc == crc);
                    if (foundTexName.crc != 0)
                    {
                        if (slave)
                            foundTexName.list.Add(matchTexture);
                        else
                            foundTexName.list.Insert(0, matchTexture);
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
            if (cachePackageMgr == null)
            {
                package.Dispose();
            }
            else
            {
                package.DisposeCache();
            }

            return errors;
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
                    if (nodeList[i].Name.ToLower() == _textures[l].packageName.ToLower())
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
