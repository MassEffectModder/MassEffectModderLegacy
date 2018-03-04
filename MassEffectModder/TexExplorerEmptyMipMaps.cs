/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

namespace MassEffectModder
{
    public partial class MipMaps
    {
        struct EmptyMipMaps
        {
            public MeType gameType;
            public string packagePath;
            public int exportId;
            public uint crc;
        }

        public struct RemoveMipsEntry
        {
            public string pkgPath;
            public List<int> exportIDs;
        }

        public void AddMarkerToPackages(string packagePath)
        {
            string path = "";
            if (GameData.gameType == MeType.ME1_TYPE)
            {
                path = @"\BioGame\CookedPC\testVolumeLight_VFX.upk".ToLowerInvariant();
            }
            if (GameData.gameType == MeType.ME2_TYPE)
            {
                path = @"\BioGame\CookedPC\BIOC_Materials.pcc".ToLowerInvariant();
            }
            if (path != "" && packagePath.ToLowerInvariant().Contains(path))
                return;
            try
            {
                using (FileStream fs = new FileStream(packagePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.SeekEnd();
                    fs.Seek(-Package.MEMendFileMarker.Length, SeekOrigin.Current);
                    string marker = fs.ReadStringASCII(Package.MEMendFileMarker.Length);
                    if (marker != Package.MEMendFileMarker)
                    {
                        fs.SeekEnd();
                        fs.WriteStringASCII(Package.MEMendFileMarker);
                    }
                }
            }
            catch
            {
            }
        }

        public List<RemoveMipsEntry> prepareListToRemove(List<FoundTexture> textures)
        {
            List<RemoveMipsEntry> list = new List<RemoveMipsEntry>();

            for (int k = 0; k < textures.Count; k++)
            {
                for (int t = 0; t < textures[k].list.Count; t++)
                {
                    if (textures[k].list[t].removeEmptyMips)
                    {
                        bool found = false;
                        for (int e = 0; e < list.Count; e++)
                        {
                            if (list[e].pkgPath == textures[k].list[t].path)
                            {
                                list[e].exportIDs.Add(textures[k].list[t].exportID);
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            break;
                        RemoveMipsEntry entry = new RemoveMipsEntry();
                        entry.pkgPath = textures[k].list[t].path;
                        entry.exportIDs = new List<int>();
                        entry.exportIDs.Add(textures[k].list[t].exportID);
                        list.Add(entry);
                    }
                }
            }

            return list;
        }

        public string removeMipMapsME1Installer(int phase, List<FoundTexture> textures, Installer installer)
        {
            string errors = "";

            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                installer.updateStatusMipMaps("Removing empty mipmaps " + ((GameData.packageFiles.Count * (phase - 1) + i + 1) * 100 / (GameData.packageFiles.Count * 2)) + "% ");
                Package package = null;

                try
                {
                    package = new Package(GameData.packageFiles[i], true);
                }
                catch (Exception e)
                {
                    string err = "";
                    err += "---- Start --------------------------------------------" + Environment.NewLine;
                    err += "Issue with open package file: " + GameData.packageFiles[i] + Environment.NewLine;
                    err += e.Message + Environment.NewLine + Environment.NewLine;
                    err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                    err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    errors += err;
                    continue;
                }

                for (int l = 0; l < package.exportsTable.Count; l++)
                {
                    int id = package.getClassNameId(package.exportsTable[l].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        using (Texture texture = new Texture(package, l, package.getExportData(l), false))
                        {
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

                            FoundTexture foundTexture = new FoundTexture();
                            int foundListEntry = -1;
                            string pkgName = GameData.RelativeGameData(package.packagePath).ToLowerInvariant();
                            for (int k = 0; k < textures.Count; k++)
                            {
                                for (int t = 0; t < textures[k].list.Count; t++)
                                {
                                    if (textures[k].list[t].exportID == l &&
                                        textures[k].list[t].path.ToLowerInvariant() == pkgName)
                                    {
                                        foundTexture = textures[k];
                                        foundListEntry = t;
                                        break;
                                    }
                                }
                            }
                            if (foundListEntry == -1)
                            {
                                errors += "Error: Texture " + package.exportsTable[l].objectName + " not found in package: " + GameData.packageFiles[i] + ", skipping..." + Environment.NewLine;
                                goto skip;
                            }

                            if (foundTexture.list[foundListEntry].linkToMaster != -1)
                            {
                                if (phase == 1)
                                    continue;

                                MatchedTexture foundMasterTex = foundTexture.list[foundTexture.list[foundListEntry].linkToMaster];
                                Package masterPkg = null;
                                masterPkg = new Package(GameData.GamePath + foundMasterTex.path);
                                int masterExportId = foundMasterTex.exportID;
                                byte[] masterData = masterPkg.getExportData(masterExportId);
                                masterPkg.DisposeCache();
                                using (Texture masterTexture = new Texture(masterPkg, masterExportId, masterData, false))
                                {
                                    if (texture.mipMapsList.Count != masterTexture.mipMapsList.Count)
                                    {
                                        errors += "Error: Texture " + package.exportsTable[l].objectName + " in package: " + GameData.packageFiles[i] + " has wrong reference, skipping..." + Environment.NewLine;
                                        goto skip;
                                    }
                                    for (int t = 0; t < texture.mipMapsList.Count; t++)
                                    {
                                        Texture.MipMap mipmap = texture.mipMapsList[t];
                                        if (mipmap.storageType == Texture.StorageTypes.extLZO ||
                                            mipmap.storageType == Texture.StorageTypes.extZlib ||
                                            mipmap.storageType == Texture.StorageTypes.extUnc)
                                        {
                                            mipmap.dataOffset = masterPkg.exportsTable[masterExportId].dataOffset + (uint)masterTexture.properties.propertyEndOffset + masterTexture.mipMapsList[t].internalOffset;
                                            texture.mipMapsList[t] = mipmap;
                                        }
                                    }
                                }
                                masterPkg.Dispose();
                            }
                            skip:
                            using (MemoryStream newData = new MemoryStream())
                            {
                                newData.WriteFromBuffer(texture.properties.toArray());
                                newData.WriteFromBuffer(texture.toArray(package.exportsTable[l].dataOffset + (uint)newData.Position));
                                package.setExportData(l, newData.ToArray());
                            }
                            modified = true;
                        }
                    }
                }
                if (modified)
                {
                    package.SaveToFile();
                }
                else
                {
                    package.Dispose();
                    AddMarkerToPackages(GameData.packageFiles[i]);
                }
                package.Dispose();
            }
            return errors;
        }

        public string removeMipMapsME1(int phase, List<FoundTexture> textures, MainWindow mainWindow, bool forceZlib = false)
        {
            string errors = "";

            List<RemoveMipsEntry> list = prepareListToRemove(textures);
            for (int i = 0; i < list.Count; i++)
            {
                bool modified = false;
                mainWindow.updateStatusLabel("Removing empty mipmaps (" + phase + ") - package " + (i + 1) + " of " + list.Count + " - " + list[i].pkgPath);
                mainWindow.updateStatusLabel2("");
                Package package = null;

                try
                {
                    package = new Package(GameData.GamePath + list[i].pkgPath, true);
                }
                catch (Exception e)
                {
                    string err = "";
                    err += "---- Start --------------------------------------------" + Environment.NewLine;
                    err += "Issue with open package file: " + GameData.GamePath + list[i].pkgPath + Environment.NewLine;
                    err += e.Message + Environment.NewLine + Environment.NewLine;
                    err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                    err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    errors += err;
                    continue;
                }

                for (int l = 0; l < list[i].exportIDs.Count; l++)
                {
                    int exportID = list[i].exportIDs[l];
                    int id = package.getClassNameId(package.exportsTable[exportID].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        using (Texture texture = new Texture(package, exportID, package.getExportData(exportID), false))
                        {
                            do
                            {
                                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                            } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));
                            texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                            texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                            texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                            FoundTexture foundTexture = new FoundTexture();
                            int foundListEntry = -1;
                            string pkgName = GameData.RelativeGameData(package.packagePath).ToLowerInvariant();
                            for (int k = 0; k < textures.Count; k++)
                            {
                                for (int t = 0; t < textures[k].list.Count; t++)
                                {
                                    if (textures[k].list[t].exportID == exportID &&
                                        textures[k].list[t].path.ToLowerInvariant() == pkgName)
                                    {
                                        foundTexture = textures[k];
                                        foundListEntry = t;
                                        break;
                                    }
                                }
                            }
                            if (foundListEntry == -1)
                            {
                                errors += "Error: Texture " + package.exportsTable[exportID].objectName + " not found in package: " + list[i].pkgPath + ", skipping..." + Environment.NewLine;
                                goto skip;
                            }

                            if (foundTexture.list[foundListEntry].linkToMaster != -1)
                            {
                                if (phase == 1)
                                    continue;

                                MatchedTexture foundMasterTex = foundTexture.list[foundTexture.list[foundListEntry].linkToMaster];
                                Package masterPkg = null;
                                masterPkg = new Package(GameData.GamePath + foundMasterTex.path);
                                int masterExportId = foundMasterTex.exportID;
                                byte[] masterData = masterPkg.getExportData(masterExportId);
                                masterPkg.DisposeCache();
                                using (Texture masterTexture = new Texture(masterPkg, masterExportId, masterData, false))
                                {
                                    if (texture.mipMapsList.Count != masterTexture.mipMapsList.Count)
                                    {
                                        errors += "Error: Texture " + package.exportsTable[exportID].objectName + " in package: " + foundMasterTex.path + " has wrong reference, skipping..." + Environment.NewLine;
                                        goto skip;
                                    }
                                    for (int t = 0; t < texture.mipMapsList.Count; t++)
                                    {
                                        Texture.MipMap mipmap = texture.mipMapsList[t];
                                        if (mipmap.storageType == Texture.StorageTypes.extLZO ||
                                            mipmap.storageType == Texture.StorageTypes.extZlib ||
                                            mipmap.storageType == Texture.StorageTypes.extUnc)
                                        {
                                            mipmap.dataOffset = masterPkg.exportsTable[masterExportId].dataOffset + (uint)masterTexture.properties.propertyEndOffset + masterTexture.mipMapsList[t].internalOffset;
                                            texture.mipMapsList[t] = mipmap;
                                        }
                                    }
                                }
                                masterPkg.Dispose();
                            }
skip:
                            using (MemoryStream newData = new MemoryStream())
                            {
                                newData.WriteFromBuffer(texture.properties.toArray());
                                newData.WriteFromBuffer(texture.toArray(package.exportsTable[exportID].dataOffset + (uint)newData.Position));
                                package.setExportData(exportID, newData.ToArray());
                            }
                            modified = true;
                        }
                    }
                }
                if (modified)
                {
                    if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                        package.SaveToFile(forceZlib, false, false, null, false);
                    else
                        package.SaveToFile(false, false, false, null, false);
                }
                else
                {
                    package.Dispose();
                }
                package.Dispose();
            }
            return errors;
        }

        public string removeMipMapsME2ME3(List<FoundTexture> textures, MainWindow mainWindow, bool forceZlib = false)
        {
            string errors = "";

            List<RemoveMipsEntry> list = prepareListToRemove(textures);
            for (int i = 0; i < list.Count; i++)
            {
                bool modified = false;
                mainWindow.updateStatusLabel("Removing empty mipmaps - package " + (i + 1) + " of " + list.Count + " - " + list[i].pkgPath);
                mainWindow.updateStatusLabel2("");
                Package package = null;

                try
                {
                    package = new Package(GameData.GamePath + list[i].pkgPath, true);
                }
                catch (Exception e)
                {
                    string err = "";
                    err += "---- Start --------------------------------------------" + Environment.NewLine;
                    err += "Issue with open package file: " + GameData.GamePath + list[i].pkgPath + Environment.NewLine;
                    err += e.Message + Environment.NewLine + Environment.NewLine;
                    err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                    err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    errors += err;
                    continue;
                }

                for (int l = 0; l < list[i].exportIDs.Count; l++)
                {
                    int exportID = list[i].exportIDs[l];
                    int id = package.getClassNameId(package.exportsTable[exportID].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        using (Texture texture = new Texture(package, exportID, package.getExportData(exportID), false))
                        {
                            do
                            {
                                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                            } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));
                            texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                            texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                            texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                            using (MemoryStream newData = new MemoryStream())
                            {
                                newData.WriteFromBuffer(texture.properties.toArray());
                                newData.WriteFromBuffer(texture.toArray(package.exportsTable[exportID].dataOffset + (uint)newData.Position));
                                package.setExportData(exportID, newData.ToArray());
                            }
                            modified = true;
                        }
                    }
                }
                if (modified)
                {
                    if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                        package.SaveToFile(forceZlib, false, false, null, false);
                    else
                        package.SaveToFile(false, false, false, null, false);
                }
                else
                {
                    package.Dispose();
                }
                package.Dispose();
            }
            if (GameData.gameType == MeType.ME3_TYPE)
            {
                TOCBinFile.UpdateAllTOCBinFiles();
            }
            return errors;
        }

        static public bool verifyGameDataEmptyMipMapsRemoval()
        {
            EmptyMipMaps[] entries = new EmptyMipMaps[]
            {
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = "\\BioGame\\CookedPC\\Packages\\VFX_Prototype\\v_Explosion_PrototypeTest_01.upk",
                    exportId = 4888,
                    crc = 0x9C074E3B,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = "\\BioGame\\CookedPC\\BioA_CitHub_500Udina.pcc",
                    exportId = 3655,
                    crc = 0xE18544C0,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = "\\BioGame\\CookedPCConsole\\BIOG_UIWorld.pcc",
                    exportId = 464,
                    crc = 0x85EFF558,
                },
            };

            for (int i = 0; i < entries.Count(); i++)
            {
                if (GameData.gameType == entries[i].gameType)
                {
                    Package package = new Package(GameData.GamePath + entries[i].packagePath);
                    Texture texture = new Texture(package, entries[i].exportId, package.getExportData(entries[i].exportId));
                    package.Dispose();
                    if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                        return false;
                }
            }

            return true;
        }
    }
}
