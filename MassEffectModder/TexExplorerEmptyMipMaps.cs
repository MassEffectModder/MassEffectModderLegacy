/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2019 Pawel Kolodziejski
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
        public struct RemoveMipsEntry
        {
            public string pkgPath;
            public List<int> exportIDs;
        }

        public List<RemoveMipsEntry> prepareListToRemove(List<FoundTexture> textures)
        {
            List<RemoveMipsEntry> list = new List<RemoveMipsEntry>();

            for (int k = 0; k < textures.Count; k++)
            {
                for (int t = 0; t < textures[k].list.Count; t++)
                {
                    if (textures[k].list[t].path == "")
                        continue;
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
                            continue;
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

        public string removeMipMapsME1(int phase, List<FoundTexture> textures, MainWindow mainWindow, Installer installer, bool ipc)
        {
            string errors = "";
            int lastProgress = -1;

            List<RemoveMipsEntry> list = prepareListToRemove(textures);
            string path = @"\BioGame\CookedPC\testVolumeLight_VFX.upk";
            for (int i = 0; i < list.Count; i++)
            {
                if (path == list[i].pkgPath)
                    continue;

                if (installer != null)
                    installer.updateProgressStatus("Removing empty mipmaps " + ((list.Count * (phase - 1) + i + 1) * 100 / (list.Count * 2)) + "%");
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Removing empty mipmaps (" + phase + ") - package " + (i + 1) + " of " + list.Count + " - " + list[i].pkgPath);
                    mainWindow.updateStatusLabel2("");
                }
                if (ipc)
                {
                    int newProgress = (list.Count * (phase - 1) + i + 1) * 100 / (list.Count * 2);
                    if (lastProgress != newProgress)
                    {
                        Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                        Console.Out.Flush();
                        lastProgress = newProgress;
                    }
                }

                Package package = null;
                try
                {
                    package = new Package(GameData.GamePath + list[i].pkgPath);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Problem with PCC file header:"))
                        return errors;
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR Issue opening package file: " + list[i].pkgPath);
                        Console.Out.Flush();
                    }
                    else
                    {
                        string err = "";
                        err += "---- Start --------------------------------------------" + Environment.NewLine;
                        err += "Issue opening package file: " + list[i].pkgPath + Environment.NewLine;
                        err += e.Message + Environment.NewLine + Environment.NewLine;
                        err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                        err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                        errors += err;
                    }
                    return errors;
                }

                errors += removeMipMapsME1(phase, textures, package, list[i], mainWindow, installer, ipc);
            }

            return errors;
        }

        public string removeMipMapsME1(int phase, List<FoundTexture> textures, Package package, RemoveMipsEntry removeEntry, MainWindow mainWindow, Installer installer, bool ipc)
        {
            string errors = "";

            for (int l = 0; l < removeEntry.exportIDs.Count; l++)
            {
                int exportID = removeEntry.exportIDs[l];
                using (Texture texture = new Texture(package, exportID, package.getExportData(exportID), false))
                {
                    if (!texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
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
                    int foundTextureEntry = -1;
                    string pkgName = GameData.RelativeGameData(package.packagePath).ToLowerInvariant();
                    for (int k = 0; k < textures.Count; k++)
                    {
                        for (int t = 0; t < textures[k].list.Count; t++)
                        {
                            if (textures[k].list[t].exportID == exportID &&
                                textures[k].list[t].path.ToLowerInvariant() == pkgName)
                            {
                                foundTexture = textures[k];
                                foundTextureEntry = k;
                                foundListEntry = t;
                                break;
                            }
                        }
                    }
                    if (foundListEntry == -1)
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR Texture " + package.exportsTable[exportID].objectName + " not found in tree: " + removeEntry.pkgPath + ", skipping...");
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Error: Texture " + package.exportsTable[exportID].objectName + " not found in tree: " + removeEntry.pkgPath + ", skipping..." + Environment.NewLine;
                        }
                        continue;
                    }

                    MatchedTexture m = foundTexture.list[foundListEntry];
                    if (m.linkToMaster != -1)
                    {
                        if (phase == 1)
                            continue;

                        MatchedTexture foundMasterTex = foundTexture.list[m.linkToMaster];
                        if (texture.mipMapsList.Count != foundMasterTex.masterDataOffset.Count)
                        {
                            if (ipc)
                            {
                                Console.WriteLine("[IPC]ERROR Texture " + package.exportsTable[exportID].objectName + " in package: " + foundMasterTex.path + " has wrong reference, skipping..." + Environment.NewLine);
                                Console.Out.Flush();
                            }
                            else
                            {
                                errors += "Error: Texture " + package.exportsTable[exportID].objectName + " in package: " + foundMasterTex.path + " has wrong reference, skipping..." + Environment.NewLine;
                            }
                            continue;
                        }
                        for (int t = 0; t < texture.mipMapsList.Count; t++)
                        {
                            Texture.MipMap mipmap = texture.mipMapsList[t];
                            if (mipmap.storageType == Texture.StorageTypes.extLZO ||
                                mipmap.storageType == Texture.StorageTypes.extZlib ||
                                mipmap.storageType == Texture.StorageTypes.extUnc)
                            {
                                mipmap.dataOffset = foundMasterTex.masterDataOffset[t];
                                texture.mipMapsList[t] = mipmap;
                            }
                        }
                    }

                    uint packageDataOffset;
                    using (MemoryStream newData = new MemoryStream())
                    {
                        newData.WriteFromBuffer(texture.properties.toArray());
                        packageDataOffset = package.exportsTable[exportID].dataOffset + (uint)newData.Position;
                        newData.WriteFromBuffer(texture.toArray(packageDataOffset));
                        package.setExportData(exportID, newData.ToArray());
                    }

                    if (m.linkToMaster == -1)
                    {
                        if (phase == 2)
                            throw new Exception();
                        m.masterDataOffset = new List<uint>();
                        for (int t = 0; t < texture.mipMapsList.Count; t++)
                        {
                            m.masterDataOffset.Add(packageDataOffset + texture.mipMapsList[t].internalOffset);
                        }
                    }

                    m.removeEmptyMips = false;
                    textures[foundTextureEntry].list[foundListEntry] = m;
                }
            }
            if (package.SaveToFile(false, false, installer != null))
            {
                if (Installer.pkgsToMarker != null)
                    Installer.pkgsToMarker.Remove(GameData.RelativeGameData(package.packagePath));
            }
            package.Dispose();

            return errors;
        }

        public string removeMipMapsME2ME3(List<FoundTexture> textures, MainWindow mainWindow, Installer installer, bool ipc, bool repack)
        {
            int lastProgress = -1;
            string errors = "";
            List<RemoveMipsEntry> list = prepareListToRemove(textures);
            string path = "";
            if (GameData.gameType == MeType.ME2_TYPE)
            {
                path = @"\BioGame\CookedPC\BIOC_Materials.pcc";
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (path == list[i].pkgPath)
                    continue;

                if (installer != null)
                    installer.updateProgressStatus("Removing empty mipmaps " + (i + 1) * 100 / list.Count + "%");
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Removing empty mipmaps - package " + (i + 1) + " of " + list.Count + " - " + list[i].pkgPath);
                    mainWindow.updateStatusLabel2("");
                }
                if (ipc)
                {
                    int newProgress = (i + 1) * 100 / list.Count;
                    if (lastProgress != newProgress)
                    {
                        Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                        Console.Out.Flush();
                        lastProgress = newProgress;
                    }
                }

                Package package = null;
                try
                {
                    package = new Package(GameData.GamePath + list[i].pkgPath);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Problem with PCC file header:"))
                        return errors;
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR Issue opening package file: " + list[i].pkgPath);
                        Console.Out.Flush();
                    }
                    else
                    {
                        string err = "";
                        err += "---- Start --------------------------------------------" + Environment.NewLine;
                        err += "Issue opening package file: " + list[i].pkgPath + Environment.NewLine;
                        err += e.Message + Environment.NewLine + Environment.NewLine;
                        err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                        err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                        errors += err;
                    }
                    return errors;
                }
                errors += removeMipMapsME2ME3(textures, package, list[i], mainWindow, installer, ipc, repack);
            }

            return errors;
        }

        public string removeMipMapsME2ME3(List<FoundTexture> textures, Package package, RemoveMipsEntry removeEntry, MainWindow mainWindow, Installer installer, bool ipc, bool repack)
        {
            string errors = "";

            for (int l = 0; l < removeEntry.exportIDs.Count; l++)
            {
                int exportID = removeEntry.exportIDs[l];
                using (Texture texture = new Texture(package, exportID, package.getExportData(exportID), false))
                {
                    if (!texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
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

                    using (MemoryStream newData = new MemoryStream())
                    {
                        newData.WriteFromBuffer(texture.properties.toArray());
                        newData.WriteFromBuffer(texture.toArray(package.exportsTable[exportID].dataOffset + (uint)newData.Position));
                        package.setExportData(exportID, newData.ToArray());
                    }
                }
            }
            if (package.SaveToFile(repack, false, installer != null))
            {
                if (repack && Installer.pkgsToRepack != null)
                    Installer.pkgsToRepack.Remove(GameData.RelativeGameData(package.packagePath));
                if (Installer.pkgsToMarker != null)
                    Installer.pkgsToMarker.Remove(GameData.RelativeGameData(package.packagePath));
            }
            package.Dispose();

            return errors;
        }

    }
}
