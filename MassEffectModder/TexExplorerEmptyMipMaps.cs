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

        public string removeMipMapsME1(int phase, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, MainWindow mainWindow, Installer installer, bool forceZlib = false)
        {
            string errors = "";

            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Removing empty mipmaps (" + phase + ") - package " + (i + 1) + " of " + GameData.packageFiles.Count + " - " + GameData.packageFiles[i]);
                    mainWindow.updateStatusLabel2("");
                }
                if (installer != null)
                {
                    installer.updateStatusMipMaps("Progress (" + phase + ") ... " + (i * 100 / GameData.packageFiles.Count) + " % ");
                }
                Package package = null;

                try
                {
                    if (cachePackageMgr != null)
                        package = cachePackageMgr.OpenPackage(GameData.packageFiles[i]);
                    else
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

                            if (texture.slave)
                            {
                                if (phase == 1)
                                    continue;
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

                                MatchedTexture foundMasterTex = foundTexture.list[foundTexture.list[foundListEntry].linkToMaster];
                                Package masterPkg = null;
                                if (cachePackageMgr != null)
                                    masterPkg = cachePackageMgr.OpenPackage(GameData.GamePath + foundMasterTex.path);
                                else
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
                                if (cachePackageMgr == null)
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
                if (cachePackageMgr == null)
                {
                    if (modified)
                    {
                        if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                            package.SaveToFile(forceZlib);
                        else
                            package.SaveToFile();
                    }
                    package.Dispose();
                }
                else
                {
                    package.DisposeCache();
                }
            }
            return errors;
        }

        public string removeMipMapsME2ME3(List<FoundTexture> textures, CachePackageMgr cachePackageMgr, MainWindow mainWindow, Installer installer, bool forceZlib = false)
        {
            string errors = "";

            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Removing empty mipmaps - package " + (i + 1) + " of " + GameData.packageFiles.Count + " - " + GameData.packageFiles[i]);
                    mainWindow.updateStatusLabel2("");
                }
                if (installer != null)
                {
                    installer.updateStatusMipMaps("Progress ... " + (i * 100 / GameData.packageFiles.Count) + " % ");
                }
                Package package = null;

                try
                {
                    if (cachePackageMgr != null)
                        package = cachePackageMgr.OpenPackage(GameData.packageFiles[i]);
                    else
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
                if (cachePackageMgr == null)
                {
                    if (modified)
                    {
                        if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                            package.SaveToFile(forceZlib);
                        else
                            package.SaveToFile();
                    }
                    package.Dispose();
                }
                else
                {
                    package.DisposeCache();
                }
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
                    if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                        return false;
                }
            }

            return true;
        }

        static public bool checkGameDataModded(CachePackageMgr cachePackageMgr)
        {
            EmptyMipMaps[] entries = new EmptyMipMaps[]
            {
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk",
                    exportId = 304,
                    crc = 0x80B2CBCF,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanFemale\BIOG_HMF_HED_PROMorph_R.upk",
                    exportId = 279,
                    crc = 0x422AAA0D,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\BIOG_ASA_ARM_MRC_R.upk",
                    exportId = 71,
                    crc = 0x077202BD,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanFemale\BIOG_HMF_HED_PROMorph_R.upk",
                    exportId = 259,
                    crc = 0x8F331825,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanFemale\BIOG_HMF_HED_PROMorph_R.upk",
                    exportId = 262,
                    crc = 0x2CC6F67C,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\Turian\BIOG_TUR_HED_PROMorph_R.upk",
                    exportId = 115,
                    crc = 0x39A26907,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk",
                    exportId = 356,
                    crc = 0xCD4AD3A5,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk",
                    exportId = 360,
                    crc = 0x0898E4C4,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanFemale\BIOG_HMF_ARM_CTH_R.upk",
                    exportId = 243,
                    crc = 0x8D20F3EB,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\Turian\BIOG_TUR_HED_SAR.upk",
                    exportId = 51,
                    crc = 0x4F24BAAF,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk",
                    exportId = 394,
                    crc = 0x947A74A8,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk",
                    exportId = 341,
                    crc = 0x72D6575F,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\Krogan\BIOG_KRO_HED_PROMorph.upk",
                    exportId = 61,
                    crc = 0x822EEFB1,
                },


                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioA_N7Spdr2_100.pcc",
                    exportId = 13796,
                    crc = 0x80B2CBCF,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_JunCvL_100Landing.pcc",
                    exportId = 6105,
                    crc = 0x72D6575F,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_EndGm2_440Normandy.pcc",
                    exportId = 2569,
                    crc = 0xCD4AD3A5,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_HorCr1_303AshKaidan.pcc",
                    exportId = 3781,
                    crc = 0x0898E4C4,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BIOG_HMM_HED_PROMorph.pcc",
                    exportId = 2495,
                    crc = 0x947A74A8,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_JnkKgA_300Labs.pcc",
                    exportId = 5785,
                    crc = 0xECA7DA8F,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_CitAsL_210FirstStop.pcc",
                    exportId = 4117,
                    crc = 0x822EEFB1,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_EndGm1_110ROMGarrus.pcc",
                    exportId = 5076,
                    crc = 0x6E3C2E30,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_OmgGrA_113AlamoAssault.pcc",
                    exportId = 4864,
                    crc = 0x6CD2B8F2,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioH_END_Professor_00.pcc",
                    exportId = 5894,
                    crc = 0x9A987362,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_EndGm1_110ROMMirranda.pcc",
                    exportId = 8808,
                    crc = 0xB389BFE6,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BIOG_HMF_HED_PROMorph_R.pcc",
                    exportId = 3073,
                    crc = 0x5674B1E3,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_HorCr1_303AshKaidan.pcc",
                    exportId = 3757,
                    crc = 0x422AAA0D,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_BchLmL_102BeachFight.pcc",
                    exportId = 5869,
                    crc = 0x9CA124E8,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioA_N7Mmnt7.pcc",
                    exportId = 10440,
                    crc = 0x2A01319D,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_ProNor.pcc",
                    exportId = 21224,
                    crc = 0x40197218,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_Nor_231Morinth.pcc",
                    exportId = 1922,
                    crc = 0x27539E1B,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_CitAsL.pcc",
                    exportId = 8798,
                    crc = 0x42EE7CBF,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = @"\BioGame\CookedPC\BioD_Nor_110DebriefLeadVixen.pcc",
                    exportId = 4433,
                    crc = 0xD30672BD,
                },


                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_CitSam_000LevelTrans.pcc",
                    exportId = 4607,
                    crc = 0x0F4E701E,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_CitHub_EmbassyP3.pcc",
                    exportId = 8182,
                    crc = 0x27539E1B,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_Cat003_380DesksConvos.pcc",
                    exportId = 9460,
                    crc = 0x42EE7CBF,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_GthLeg_000LevelTrans.pcc",
                    exportId = 3500,
                    crc = 0xD30672BD,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_CitHub_000Docking.pcc",
                    exportId = 3625,
                    crc = 0xF51672AC,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_CerJcb_150MainDoor.pcc",
                    exportId = 7722,
                    crc = 0xEDC5BFE7,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_CitHub_WardsFluxP3.pcc",
                    exportId = 11443,
                    crc = 0xF95D3472,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_CitHub_Dock.pcc",
                    exportId = 11485,
                    crc = 0xE8271883,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_CerMir_375Lab_vid.pcc",
                    exportId = 4630,
                    crc = 0x4D73F4F6,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BIOG_HMF_HED_PROMorph_R.pcc",
                    exportId = 7552,
                    crc = 0xF6BFD7B5,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_MPCer_000Translevel.pcc",
                    exportId = 6294,
                    crc = 0x3451A823,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_Cat002_050Shuttle.pcc",
                    exportId = 2995,
                    crc = 0xF394E97A,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_CitHub_000ProCit.pcc",
                    exportId = 6451,
                    crc = 0x2DCFDEA9,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BIOG_HMM_HED_PROMorph.pcc",
                    exportId = 8818,
                    crc = 0xB2900BF5,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_Cat003_780FinalConvos.pcc",
                    exportId = 9938,
                    crc = 0x6574CE07,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_Gth001_560Gethries.pcc",
                    exportId = 7227,
                    crc = 0x36DACF3F,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_Kro001_000LevelTrans.pcc",
                    exportId = 7156,
                    crc = 0x1198BA9D,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_End001_436CRGrunt.pcc",
                    exportId = 1568,
                    crc = 0xECA7DA8F,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioD_End001_436CRMordin.pcc",
                    exportId = 1456,
                    crc = 0x9A987362,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = @"\BioGame\CookedPCConsole\BioA_CitHub_Council.pcc",
                    exportId = 6653,
                    crc = 0x8645A85C,
                },
            };

            for (int i = 0; i < entries.Count(); i++)
            {
                if (GameData.gameType == entries[i].gameType)
                {
                    Package package = null;
                    try
                    {
                        if (cachePackageMgr != null)
                            package = cachePackageMgr.OpenPackage(GameData.GamePath + entries[i].packagePath);
                        else
                        {
                            package = new Package(GameData.GamePath + entries[i].packagePath);
                        }
                    }
                    catch
                    {
                        return false;
                    }
                    Texture texture = new Texture(package, entries[i].exportId, package.getExportData(entries[i].exportId));
                    if (texture.getCrcTopMipmap() != entries[i].crc)
                        return true;
                }
            }

            return false;
        }
    }
}
