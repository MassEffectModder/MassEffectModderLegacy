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

using AmaroK86.ImageFormat;
using StreamHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class MipMaps
    {
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

        public string replaceTexture(DDSImage image, List<MatchedTexture> list, CachePackageMgr cachePackageMgr, string textureName)
        {
            List<Texture> masterTextures = new List<Texture>();
            Texture arcTexture = null, cprTexture = null;
            string errors = "";

            for (int n = 0; n < list.Count; n++)
            {
                MatchedTexture nodeTexture = list[n];
                Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                {
                    texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                }
                bool master = true;
                if (GameData.gameType == MeType.ME1_TYPE)
                {
                    if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extLZO) ||
                        texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extZlib) ||
                        texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extUnc))
                    {
                        master = false;
                        if (!masterTextures.Exists(s => s.packageName == texture.packageName))
                        {
                            errors += "Error in texture: " + textureName + " Broken game file: " + nodeTexture.path + ", skipping texture..." + Environment.NewLine;
                            continue;
                        }
                    }
                }

                if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                {
                    errors += "Error in texture: " + textureName + " This texture has not all the required mipmaps, skipping texture..." + Environment.NewLine;
                    break;
                }

                string fmt = texture.properties.getProperty("Format").valueName;
                DDSFormat ddsFormat = DDSImage.convertFormat(fmt);
                if (image.ddsFormat != ddsFormat)
                {
                    errors += "Error in texture: " + textureName + " This texture has wrong texture format, should be: " + ddsFormat + ", skipping texture..." + Environment.NewLine;
                    break;
                }

                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                    texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                {
                    errors += "Error in texture: " + textureName + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                    break;
                }

                // remove lower mipmaps from source image which not exist in game data
                for (int t = 0; t < image.mipMaps.Count(); t++)
                {
                    if (image.mipMaps[t].origWidth <= texture.mipMapsList[0].width &&
                        image.mipMaps[t].origHeight <= texture.mipMapsList[0].height &&
                        texture.mipMapsList.Count > 1)
                    {
                        if (!texture.mipMapsList.Exists(m => m.width == image.mipMaps[t].origWidth && m.height == image.mipMaps[t].origHeight))
                        {
                            image.mipMaps.RemoveAt(t--);
                        }
                    }
                }

                bool skip = false;
                // reuse lower mipmaps from game data which not exist in source image
                for (int t = 0; t < texture.mipMapsList.Count; t++)
                {
                    if (texture.mipMapsList[t].width <= image.mipMaps[0].origWidth &&
                        texture.mipMapsList[t].height <= image.mipMaps[0].origHeight)
                    {
                        if (!image.mipMaps.Exists(m => m.origWidth == texture.mipMapsList[t].width && m.origHeight == texture.mipMapsList[t].height))
                        {
                            byte[] data = texture.getMipMapData(texture.mipMapsList[t]);
                            if (data == null)
                            {
                                errors += "Error in game data: " + nodeTexture.path + ", skipping texture..." + Environment.NewLine;
                                skip = true;
                                break;
                            }
                            DDSImage.MipMap mipmap = new DDSImage.MipMap(data, ddsFormat, texture.mipMapsList[t].width, texture.mipMapsList[t].height);
                            image.mipMaps.Add(mipmap);
                        }
                    }
                }
                if (skip)
                    continue;

                package.DisposeCache();

                bool triggerCacheArc = false, triggerCacheCpr = false;
                string archiveFile = "";
                byte[] origGuid = new byte[16];
                if (texture.properties.exists("TextureFileCacheName"))
                {
                    Array.Copy(texture.properties.getProperty("TFCFileGuid").valueStruct, origGuid, 16);
                    string archive = texture.properties.getProperty("TextureFileCacheName").valueName;
                    archiveFile = Path.Combine(GameData.MainData, archive + ".tfc");
                    if (nodeTexture.path.Contains("\\DLC"))
                    {
                        string DLCArchiveFile = Path.Combine(Path.GetDirectoryName(GameData.GamePath + nodeTexture.path), archive + ".tfc");
                        if (File.Exists(DLCArchiveFile))
                            archiveFile = DLCArchiveFile;
                        else if (GameData.gameType == MeType.ME2_TYPE)
                            archiveFile = Path.Combine(GameData.MainData, "Textures.tfc");
                        else if (GameData.gameType == MeType.ME3_TYPE)
                        {
                            if (!File.Exists(archiveFile))
                                archiveFile = Directory.GetFiles(GameData.bioGamePath, archive + ".tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList()[0];
                        }
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

                List<Texture.MipMap> mipmaps = new List<Texture.MipMap>();
                for (int m = 0; m < image.mipMaps.Count(); m++)
                {
                    Texture.MipMap mipmap = new Texture.MipMap();
                    mipmap.width = image.mipMaps[m].origWidth;
                    mipmap.height = image.mipMaps[m].origHeight;
                    if (texture.existMipmap(mipmap.width, mipmap.height))
                        mipmap.storageType = texture.getMipmap(mipmap.width, mipmap.height).storageType;
                    else
                    {
                        mipmap.storageType = texture.getTopMipmap().storageType;
                        if (GameData.gameType == MeType.ME1_TYPE && master == false)
                        {
                            if (mipmap.storageType == Texture.StorageTypes.pccUnc ||
                                mipmap.storageType == Texture.StorageTypes.pccLZO ||
                                mipmap.storageType == Texture.StorageTypes.pccZlib)
                            {
                                mipmap.storageType = Texture.StorageTypes.extLZO;
                            }
                        }
                        else if (GameData.gameType == MeType.ME2_TYPE)
                        {
                            if (texture.properties.exists("TextureFileCacheName") && texture.mipMapsList.Count > 1)
                            {
                                if (texture.mipMapsList.Count < 6)
                                {
                                    mipmap.storageType = Texture.StorageTypes.pccUnc;
                                    if (!texture.properties.exists("NeverStream"))
                                        texture.properties.addBoolValue("NeverStream", true);
                                }
                                else
                                {
                                    mipmap.storageType = Texture.StorageTypes.extLZO;
                                }
                            }
                        }
                        else if (GameData.gameType == MeType.ME3_TYPE)
                        {
                            if (texture.properties.exists("TextureFileCacheName") && texture.mipMapsList.Count > 1)
                            {
                                if (archiveFile.Contains("\\DLC"))
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
                    if (GameData.gameType == MeType.ME1_TYPE)
                    {
                        if (mipmap.storageType == Texture.StorageTypes.pccLZO ||
                            mipmap.storageType == Texture.StorageTypes.pccZlib)
                        {
                            if (master)
                                mipmap.newData = texture.compressTexture(image.mipMaps[m].data, mipmap.storageType);
                            else
                                mipmap.newData = masterTextures.Find(s => s.packageName == texture.packageName).mipMapsList[m].newData;
                            mipmap.compressedSize = mipmap.newData.Length;
                        }
                        if (mipmap.storageType == Texture.StorageTypes.pccUnc)
                        {
                            mipmap.compressedSize = mipmap.uncompressedSize;
                            mipmap.newData = image.mipMaps[m].data;
                        }
                        if ((mipmap.storageType == Texture.StorageTypes.extLZO ||
                            mipmap.storageType == Texture.StorageTypes.extZlib) && master == false)
                        {
                            mipmap.compressedSize = masterTextures.Find(s => s.packageName == texture.packageName).mipMapsList[m].compressedSize;
                            mipmap.dataOffset = masterTextures.Find(s => s.packageName == texture.packageName).mipMapsList[m].dataOffset;
                        }
                    }
                    else
                    {
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
                                texture.properties.getProperty("TFCFileGuid").valueStruct))
                            {
                                triggerCacheArc = true;
                                Texture.MipMap oldMipmap = texture.getMipmap(mipmap.width, mipmap.height);
                                if (StructuralComparisons.StructuralEqualityComparer.Equals(origGuid,
                                    texture.properties.getProperty("TFCFileGuid").valueStruct) &&
                                    oldMipmap.width != 0 && mipmap.newData.Length <= oldMipmap.compressedSize)
                                {
                                    try
                                    {
                                        using (FileStream fs = new FileStream(archiveFile, FileMode.Open, FileAccess.Write))
                                        {
                                            fs.JumpTo(oldMipmap.dataOffset);
                                            mipmap.dataOffset = oldMipmap.dataOffset;
                                            fs.WriteFromBuffer(mipmap.newData);
                                        }
                                    }
                                    catch
                                    {
                                        throw new Exception("Problem with access to TFC file: " + archiveFile);
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        using (FileStream fs = new FileStream(archiveFile, FileMode.Open, FileAccess.Write))
                                        {
                                            fs.SeekEnd();
                                            mipmap.dataOffset = (uint)fs.Position;
                                            fs.WriteFromBuffer(mipmap.newData);
                                        }
                                    }
                                    catch
                                    {
                                        throw new Exception("Problem with access to TFC file: " + archiveFile);
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

                    mipmap.width = image.mipMaps[m].width;
                    mipmap.height = image.mipMaps[m].height;
                    mipmaps.Add(mipmap);
                    if (texture.mipMapsList.Count() == 1)
                        break;
                }
                texture.replaceMipMaps(mipmaps);
                texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                if (texture.properties.exists("MipTailBaseIdx"))
                    texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                using (MemoryStream newData = new MemoryStream())
                {
                    newData.WriteFromBuffer(texture.properties.toArray());
                    newData.WriteFromBuffer(texture.toArray(0)); // filled later
                    package.setExportData(nodeTexture.exportID, newData.ToArray());
                }

                using (MemoryStream newData = new MemoryStream())
                {
                    newData.WriteFromBuffer(texture.properties.toArray());
                    newData.WriteFromBuffer(texture.toArray(package.exportsTable[nodeTexture.exportID].dataOffset + (uint)newData.Position));
                    package.setExportData(nodeTexture.exportID, newData.ToArray());
                }

                if (GameData.gameType == MeType.ME1_TYPE)
                {
                    if (master)
                        masterTextures.Add(texture);
                }
                else
                {
                    if (triggerCacheCpr)
                        cprTexture = texture;
                    if (triggerCacheArc)
                        arcTexture = texture;
                }
                package = null;
            }
            masterTextures = null;
            arcTexture = cprTexture = null;

            return errors;
        }
    }

    public partial class TexExplorer : Form
    {
        private void replaceTexture()
        {
            if (listViewTextures.SelectedItems.Count == 0)
                return;

            using (OpenFileDialog selectDDS = new OpenFileDialog())
            {
                selectDDS.Title = "Please select DDS file";
                selectDDS.Filter = "DDS file|*.dds";
                if (selectDDS.ShowDialog() != DialogResult.OK)
                    return;

                DDSImage image = new DDSImage(selectDDS.FileName);
                if (!image.checkExistAllMipmaps())
                {
                    MessageBox.Show("This texture has not all the required mipmaps, canceling...");
                    return;
                }

                bool loadMod = loadMODsToolStripMenuItem.Enabled;
                bool clearMod = clearMODsToolStripMenuItem.Enabled;
                bool packMod = packMODToolStripMenuItem.Enabled;
                EnableMenuOptions(false);

                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                ListViewItem item = listViewTextures.FocusedItem;
                int index = Convert.ToInt32(item.Name);

                MipMaps mipMaps = new MipMaps();
                richTextBoxInfo.Text = mipMaps.replaceTexture(image, node.textures[index].list, cachePackageMgr,  node.textures[index].name);
                if (richTextBoxInfo.Text != "")
                {
                    richTextBoxInfo.Show();
                    pictureBoxPreview.Hide();
                    MessageBox.Show("WARNING: Some errors have occured!");
                }

                cachePackageMgr.CloseAllWithSave();

                EnableMenuOptions(true);
                loadMODsToolStripMenuItem.Enabled = loadMod;
                clearMODsToolStripMenuItem.Enabled = clearMod;
                packMODToolStripMenuItem.Enabled = packMod;
                listViewTextures.Focus();
                item.Selected = false;
                item.Selected = true;
                item.Focused = true;
            }
        }
    }
}
