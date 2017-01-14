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
using System.Windows.Forms;

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

        public void removeMipMaps(List<FoundTexture> textures, CachePackageMgr cachePackageMgr, MainWindow mainWindow, Installer installer)
        {
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Remove empty mipmaps, package " + (i + 1) + " of " + GameData.packageFiles.Count);
                    mainWindow.updateStatusLabel2("");
                }
                if (installer != null)
                {
                    installer.updateStatusMipMaps("Progress... " + (i * 100 / GameData.packageFiles.Count) + " % ");
                }
                Package package = new Package(GameData.packageFiles[i], true);
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

                            if (GameData.gameType == MeType.ME1_TYPE && package.compressed)
                            {
                                if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extLZO) ||
                                    texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extZlib) ||
                                    texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extUnc))
                                {
                                    string textureName = package.exportsTable[l].objectName;
                                    FoundTexture foundTexName;
                                    List<FoundTexture> foundList = textures.FindAll(s => s.name == textureName && s.packageName == texture.packageName);
                                    if (foundList.Count > 1)
                                    {
                                        foundTexName = new FoundTexture();
                                        for (int t = 0; t < foundList.Count; t++)
                                        {
                                            if (foundList[t].list.Exists(s => s.exportID == l))
                                            {
                                                foundTexName = foundList[t];
                                                break;
                                            }
                                        }
                                        if (foundTexName.crc == 0)
                                            throw new Exception();
                                    }
                                    else
                                    {
                                        foundTexName = foundList[0];
                                    }
                                    Package refPkg = new Package(GameData.GamePath + foundTexName.list[0].path, true);
                                    int refExportId = foundTexName.list[0].exportID;
                                    byte[] refData = refPkg.getExportData(refExportId);
                                    refPkg.DisposeCache();
                                    using (Texture refTexture = new Texture(refPkg, refExportId, refData, false))
                                    {
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
                                        refPkg = null;
                                    }
                                }
                            }

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
                    package.SaveToFile();
                package.Dispose();
            }
            if (GameData.gameType == MeType.ME3_TYPE)
            {
                cachePackageMgr.updateMainTOC();
                cachePackageMgr.updateDLCsTOC();
            }
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
                    if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                        return false;
                }
            }

            return true;
        }
    }

    public partial class TexExplorer : Form
    {
        private void removeEmptyMipmapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MipMaps.verifyGameDataEmptyMipMapsRemoval())
            {
                DialogResult result = MessageBox.Show("It seems removal empty mipmaps was already processed.\nAre you sure to proceed?", "Remove empty mipmaps", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                    return;
            }
            else
            {
                DialogResult result = MessageBox.Show("Are you sure to proceed?", "Remove empty mipmaps", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                    return;
            }

            EnableMenuOptions(false);

            _mainWindow.GetPackages(gameData);
            if (_gameSelected == MeType.ME1_TYPE)
                treeScan.sortPackagesME1(_mainWindow, null);

            MipMaps mipmaps = new MipMaps();
            mipmaps.removeMipMaps(_textures, cachePackageMgr, _mainWindow, null);

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }
    }
}
