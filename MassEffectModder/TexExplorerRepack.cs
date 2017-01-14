/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
    public class Repack
    {
        public void RepackTexturesTFC(List<FoundTexture> textures, MainWindow mainWindow, CachePackageMgr cachePackageMgr)
        {
            if (GameData.gameType == MeType.ME2_TYPE)
            {
                for (int i = 0; i < textures.Count; i++)
                {
                    FoundTexture foundTexture = textures[i];
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("Texture: " + (i + 1) + " of " + textures.Count);
                        mainWindow.updateStatusLabel2("");
                    }
                    string TFCfilename = "", TFCfilenameTemp, prevTFCfilename = "";
                    Texture prevTexture = null;
                    for (int index2 = 0; index2 < foundTexture.list.Count; index2++)
                    {
                        MatchedTexture matchedTexture = foundTexture.list[index2];
                        string packagePath = GameData.GamePath + matchedTexture.path;
                        Package package = cachePackageMgr.OpenPackage(packagePath);
                        Texture texture = new Texture(package, matchedTexture.exportID, package.getExportData(matchedTexture.exportID));
                        if (!texture.mipMapsList.Exists(b => b.storageType == Texture.StorageTypes.extLZO || b.storageType == Texture.StorageTypes.extUnc || b.storageType == Texture.StorageTypes.extZlib))
                            continue;

                        bool modified = false;
                        for (int l = 0; l < texture.mipMapsList.Count; l++)
                        {
                            Texture.MipMap mipmap = texture.mipMapsList[l];
                            if (((int)mipmap.storageType & (int)Texture.StorageFlags.externalFile) != 0)
                            {
                                string archive = texture.properties.getProperty("TextureFileCacheName").valueName;
                                TFCfilename = Path.Combine(GameData.MainData, archive + ".tfc");
                                if (packagePath.Contains("\\DLC"))
                                {
                                    string DLCArchiveFile = Path.Combine(Path.GetDirectoryName(packagePath), archive + ".tfc");
                                    if (File.Exists(DLCArchiveFile))
                                        TFCfilename = DLCArchiveFile;
                                    else if (GameData.gameType == MeType.ME2_TYPE)
                                        TFCfilename = Path.Combine(GameData.MainData, "Textures.tfc");
                                }

                                TFCfilenameTemp = TFCfilename + ".TempTFC";
                                if (!File.Exists(TFCfilenameTemp))
                                {
                                    using (FileStream fs = new FileStream(TFCfilenameTemp, FileMode.CreateNew, FileAccess.Write))
                                    {
                                        byte[] guid = new byte[16];
                                        Array.Copy(texture.properties.getProperty("TFCFileGuid").valueStruct, guid, 16);
                                        fs.WriteFromBuffer(guid);
                                    }
                                }

                                if (TFCfilename != prevTFCfilename)
                                {
                                    using (FileStream fs = new FileStream(TFCfilename, FileMode.Open, FileAccess.Read))
                                    {
                                        fs.JumpTo(mipmap.dataOffset);
                                        using (FileStream fsNew = new FileStream(TFCfilenameTemp, FileMode.Append, FileAccess.Write))
                                        {
                                            fsNew.SeekEnd();
                                            mipmap.dataOffset = (uint)fsNew.Position;
                                            if (mipmap.storageType == Texture.StorageTypes.extLZO ||
                                                mipmap.storageType == Texture.StorageTypes.extZlib)
                                            {
                                                fsNew.WriteFromStream(fs, mipmap.compressedSize);
                                            }
                                            else
                                            {
                                                fsNew.WriteFromStream(fs, mipmap.uncompressedSize);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Texture.MipMap tmpMipmap = prevTexture.getMipmap(mipmap.width, mipmap.height);
                                    if (mipmap.storageType != tmpMipmap.storageType)
                                        throw new Exception("");
                                    mipmap.dataOffset = tmpMipmap.dataOffset;
                                }
                                texture.mipMapsList[l] = mipmap;
                                modified = true;
                            }
                        }

                        if (modified)
                        {
                            prevTFCfilename = TFCfilename;
                            prevTexture = texture;
                            using (MemoryStream newData = new MemoryStream())
                            {
                                newData.WriteFromBuffer(texture.properties.toArray());
                                newData.WriteFromBuffer(texture.toArray(package.exportsTable[matchedTexture.exportID].dataOffset + (uint)newData.Position));
                                //package.setExportData(matchedTexture.exportID, newData.ToArray());
                            }
                        }
                    }
                }
            }
            if (mainWindow != null)
            {
                mainWindow.updateStatusLabel("");
            }

            List<string> tfcFles = Directory.GetFiles(GameData.GamePath, "*.TempTFC", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < tfcFles.Count; i++)
            {

                string newname = tfcFles[i].Substring(0, tfcFles[i].IndexOf(".TempTFC"));
                File.Delete(newname);
                File.Move(tfcFles[i], newname);
            }
            //cachePackageMgr.CloseAllWithSave();

            if (mainWindow != null)
            {
                mainWindow.updateStatusLabel("Done.");
                mainWindow.updateStatusLabel("");
            }
        }
    }
}
