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
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public struct MatchedTexture
    {
        public int exportID;
        public string packageName; // only used while texture scan for ME1
        public string basePackageName; // only used while texture scan for ME1
        public bool weakSlave;
        public bool slave;
        public string path;
        public int linkToMaster;
        public uint mipmapOffset;
        public List<uint> crcs;
        public List<uint> masterDataOffset;
        public bool removeEmptyMips;
        public int numMips;
    }

    public struct FoundTexture
    {
        public string name;
        public uint crc;
        public List<MatchedTexture> list;
        public PixelFormat pixfmt;
        public TexProperty.TextureTypes flags;
        public int width, height;
    }

    public partial class TreeScan
    {
        public const uint textureMapBinTag = 0x5054454D;
        public const uint textureMapBinVersion = 2;
        public const uint TextureModTag = 0x444F4D54;
        public const uint TextureModVersion = 2;
        public const uint FileTextureTag = 0x53444446;
        public const uint FileBinTag = 0x4E494246;
        public const uint MEMI_TAG = 0x494D454D;

        private bool generateBuiltinMapFiles = false; // change to true to enable map files generation
        public List<FoundTexture> treeScan = null;
        List<string> pkgs;

        public void loadTexturesMap(MeType gameId, List<FoundTexture> textures)
        {
            Stream fs;
            byte[] buffer = null;
            if (gameId == MeType.ME1_TYPE)
                pkgs = Program.tablePkgsME1;
            else if (gameId == MeType.ME2_TYPE)
                pkgs = Program.tablePkgsME2;
            else
                pkgs = Program.tablePkgsME3;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            for (int l = 0; l < resources.Length; l++)
            {
                if (resources[l].Contains("me" + (int)gameId + "map.bin"))
                {
                    using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                    {
                        buffer = s.ReadToBuffer(s.Length);
                        break;
                    }
                }
            }
            if (buffer == null)
                throw new Exception();
            MemoryStream tmp = new MemoryStream(buffer);
            if (tmp.ReadUInt32() != 0x504D5443)
                throw new Exception();
            byte[] decompressed = new byte[tmp.ReadUInt32()];
            byte[] compressed = tmp.ReadToBuffer(tmp.ReadUInt32());
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            fs = new MemoryStream(decompressed);

            fs.Skip(8);
            uint countTexture = fs.ReadUInt32();
            for (int i = 0; i < countTexture; i++)
            {
                FoundTexture texture = new FoundTexture();
                int len = fs.ReadByte();
                texture.name = fs.ReadStringASCII(len);
                texture.crc = fs.ReadUInt32();
                texture.width = fs.ReadInt16();
                texture.height = fs.ReadInt16();
                texture.pixfmt = (PixelFormat)fs.ReadByte();
                texture.flags = (TexProperty.TextureTypes)fs.ReadByte();
                int countPackages = fs.ReadInt16();
                texture.list = new List<MatchedTexture>();
                for (int k = 0; k < countPackages; k++)
                {
                    MatchedTexture matched = new MatchedTexture();
                    matched.exportID = fs.ReadInt32();
                    if (gameId == MeType.ME1_TYPE)
                    {
                        matched.linkToMaster = fs.ReadInt16();
                        if (matched.linkToMaster != -1)
                        {
                            matched.slave = true;
                            matched.basePackageName = fs.ReadStringASCIINull();
                        }
                    }
                    matched.removeEmptyMips = fs.ReadByte() != 0;
                    matched.numMips = fs.ReadByte();
                    matched.path = pkgs[fs.ReadInt16()];
                    matched.packageName = Path.GetFileNameWithoutExtension(matched.path).ToUpper();
                    texture.list.Add(matched);
                }
                textures.Add(texture);
            }
        }

        public string PrepareListOfTextures(MeType gameId, TexExplorer texEplorer, MainWindow mainWindow, Installer installer, ref string log, bool ipc)
        {
            string errors = "";
            treeScan = null;
            Misc.MD5FileEntry[] md5Entries;
            if (gameId == MeType.ME1_TYPE)
                md5Entries = Program.entriesME1;
            else if (gameId == MeType.ME2_TYPE)
                md5Entries = Program.entriesME2;
            else
                md5Entries = Program.entriesME3;

            List<FoundTexture> textures = new List<FoundTexture>();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filename = Path.Combine(path, "me" + (int)gameId + "map.bin");

            if (mainWindow != null)
            {
                if (File.Exists(filename))
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != textureMapBinTag || version != textureMapBinVersion)
                        {
                            MessageBox.Show("Detected wrong or old version of textures scan file!" +
                                "\n\nYou need to restore the game to vanilla state then reinstall optional DLC/PCC mods." +
                                "\n\nThen from the main menu, select 'Remove Textures Scan File' and start Texture Manager again.");
                            mainWindow.updateStatusLabel("");
                            mainWindow.updateStatusLabel2("");
                            texEplorer.Close();
                            fs.Close();
                            log += "Detected wrong or old version of textures scan file!" + Environment.NewLine;
                            log += "You need to restore the game to vanilla state then reinstall optional DLC/PCC mods." + Environment.NewLine;
                            log += "Then from the main menu, select 'Remove Textures Scan File' and start Texture Manager again." + Environment.NewLine;
                            return "Detected wrong or old version of textures scan file!" + Environment.NewLine +
                                "You need to restore the game to vanilla state then reinstall optional DLC/PCC mods." + Environment.NewLine +
                                "Then from the main menu, select 'Remove Textures Scan File' and start Texture Manager again." + Environment.NewLine;
                        }

                        uint countTexture = fs.ReadUInt32();
                        for (int i = 0; i < countTexture; i++)
                        {
                            FoundTexture texture = new FoundTexture();
                            int len = fs.ReadInt32();
                            texture.name = fs.ReadStringASCII(len);
                            texture.crc = fs.ReadUInt32();
                            uint countPackages = fs.ReadUInt32();
                            texture.list = new List<MatchedTexture>();
                            for (int k = 0; k < countPackages; k++)
                            {
                                MatchedTexture matched = new MatchedTexture();
                                matched.exportID = fs.ReadInt32();
                                matched.linkToMaster = fs.ReadInt32();
                                len = fs.ReadInt32();
                                matched.path = fs.ReadStringASCII(len);
                                texture.list.Add(matched);
                            }
                            textures.Add(texture);
                        }

                        List<string> packages = new List<string>();
                        int numPackages = fs.ReadInt32();
                        for (int i = 0; i < numPackages; i++)
                        {
                            int len = fs.ReadInt32();
                            string pkgPath = fs.ReadStringASCII(len);
                            pkgPath = GameData.GamePath + pkgPath;
                            packages.Add(pkgPath);
                        }
                        for (int i = 0; i < packages.Count; i++)
                        {
                            if (GameData.packageFiles.Find(s => s.Equals(packages[i], StringComparison.OrdinalIgnoreCase)) == null)
                            {
                                MessageBox.Show("Detected removal of game files since last game data scan." +
                                "\n\nYou need to restore the game to vanilla state then reinstall optional DLC/PCC mods." +
                                "\n\nThen from the main menu, select 'Remove Textures Scan File' and start Texture Manager again.");
                                return "";
                            }
                        }
                        for (int i = 0; i < GameData.packageFiles.Count; i++)
                        {
                            if (packages.Find(s => s.Equals(GameData.packageFiles[i], StringComparison.OrdinalIgnoreCase)) == null)
                            {
                                MessageBox.Show("Detected additional game files not present in latest game data scan." +
                                "\n\nYou need to restore the game to vanilla state then reinstall optional DLC/PCC mods." +
                                "\n\nThen from the main menu, select 'Remove Textures Scan File' and start Texture Manager again.");
                                return "";
                            }
                        }

                        treeScan = textures;
                        mainWindow.updateStatusLabel("");
                        mainWindow.updateStatusLabel2("");
                    }
                    if (!texEplorer.verifyGameDataEmptyMipMapsRemoval())
                    {
                        MessageBox.Show("Detected empty mips in game files." +
                        "\n\nYou need the game in vanilla state and optional DLC/PCC mods." +
                        "\n\nThen from the main menu, select 'Remove Textures Scan File' and start Texture Manager again.");
                        return "";
                    }
                    return errors;
                }

                if (mainWindow != null)
                {
                    List<string> badMods = Misc.detectBrokenMod(GameData.gameType);
                    if (badMods.Count != 0)
                    {
                        errors = "";
                        for (int l = 0; l < badMods.Count; l++)
                        {
                            errors += badMods[l] + Environment.NewLine;
                        }
                        MessageBox.Show("Detected not compatible mods: \n\n" + errors);
                        return "";
                    }

                    List<string> mods = Misc.detectMods(GameData.gameType);
                    if (mods.Count != 0 && GameData.gameType == MeType.ME1_TYPE && GameData.FullScanME1Game)
                    {
                        errors = "";
                        for (int l = 0; l < mods.Count; l++)
                        {
                            errors += mods[l] + Environment.NewLine;
                        }
                        DialogResult resp = MessageBox.Show("Detected NOT compatible/supported mods with this version of game: \n\n" + errors +
                            "\n\nPress Cancel to abort or press Ok button to continue.", "Warning !", MessageBoxButtons.OKCancel);
                        if (resp == DialogResult.Cancel)
                            return "";
                    }
                }

                DialogResult result = MessageBox.Show("Replacing textures and creating mods requires generating a map of the game's textures.\n" +
                "You only need to do it once.\n\n" +
                "IMPORTANT! Your game needs to be in vanilla state and have optional DLC/PCC mods installed.\n\n" +
                "Are you sure you want to proceed?", "Textures mapping", MessageBoxButtons.YesNo);
                if (result == DialogResult.No)
                {
                    texEplorer.Close();
                    return "";
                }

                Misc.startTimer();
            }

            if (!GameData.FullScanME1Game)
            {
                GameData.packageFiles.Sort();
                int count = GameData.packageFiles.Count;
                for (int i = 0; i < count; i++)
                {
                    if (GameData.packageFiles[i].Contains("_IT.") ||
                        GameData.packageFiles[i].Contains("_FR.") ||
                        GameData.packageFiles[i].Contains("_ES.") ||
                        GameData.packageFiles[i].Contains("_DE.") ||
                        GameData.packageFiles[i].Contains("_RA.") ||
                        GameData.packageFiles[i].Contains("_RU.") ||
                        GameData.packageFiles[i].Contains("_PLPC.") ||
                        GameData.packageFiles[i].Contains("_DEU.") ||
                        GameData.packageFiles[i].Contains("_FRA.") ||
                        GameData.packageFiles[i].Contains("_ITA.") ||
                        GameData.packageFiles[i].Contains("_POL."))
                    {
                        GameData.packageFiles.Add(GameData.packageFiles[i]);
                        GameData.packageFiles.RemoveAt(i--);
                        count--;
                    }
                }
            }

            if (!generateBuiltinMapFiles && !GameData.FullScanME1Game)
            {
                List<string> addedFiles = new List<string>();
                List<string> modifiedFiles = new List<string>();

                loadTexturesMap(gameId, textures);

                List<string> sortedFiles = new List<string>();
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    sortedFiles.Add(GameData.RelativeGameData(GameData.packageFiles[i]).ToLowerInvariant());
                }
                sortedFiles.Sort();

                for (int k = 0; k < textures.Count; k++)
                {
                    for (int t = 0; t < textures[k].list.Count; t++)
                    {
                        string pkgPath = textures[k].list[t].path.ToLowerInvariant();
                        if (sortedFiles.BinarySearch(pkgPath) >= 0)
                            continue;
                        MatchedTexture f = textures[k].list[t];
                        f.path = "";
                        textures[k].list[t] = f;
                    }
                }

                if (installer != null)
                    installer.updateProgressStatus("Scanning packages");
                if (mainWindow != null)
                    mainWindow.updateStatusLabel("Scanning packages...");
                if (ipc)
                {
                    Console.WriteLine("[IPC]STAGE_CONTEXT STAGE_SCAN");
                    Console.Out.Flush();
                }
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    int index = -1;
                    bool modified = true;
                    bool foundPkg = false;
                    string package = GameData.RelativeGameData(GameData.packageFiles[i].ToLowerInvariant());
                    long packageSize = new FileInfo(GameData.packageFiles[i]).Length;
                    for (int p = 0; p < md5Entries.Length; p++)
                    {
                        if (package == md5Entries[p].path.ToLowerInvariant())
                        {
                            foundPkg = true;
                            if (packageSize == md5Entries[p].size)
                            {
                                modified = false;
                                break;
                            }
                            index = p;
                        }
                    }
                    if (foundPkg && modified)
                        modifiedFiles.Add(md5Entries[index].path);
                    else if (!foundPkg)
                        addedFiles.Add(GameData.RelativeGameData(GameData.packageFiles[i]));
                }

                int lastProgress = -1;
                int totalPackages = modifiedFiles.Count + addedFiles.Count;
                int currentPackage = 0;
                if (ipc)
                {
                    Console.WriteLine("[IPC]STAGE_WEIGHT STAGE_SCAN " +
                        string.Format("{0:0.000000}", ((float)totalPackages / GameData.packageFiles.Count)));
                    Console.Out.Flush();
                }
                for (int i = 0; i < modifiedFiles.Count; i++, currentPackage++)
                {
                    if (installer != null)
                        installer.updateProgressStatus("Scanning textures " + ((currentPackage + 1) * 100) / totalPackages + "% ");
                    if (mainWindow != null)
                        mainWindow.updateStatusLabel("Finding textures in package " + (currentPackage + 1) + " of " + totalPackages + " - " + modifiedFiles[i]);
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE " + modifiedFiles[i]);
                        int newProgress = currentPackage * 100 / totalPackages;
                        if (lastProgress != newProgress)
                        {
                            Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                            lastProgress = newProgress;
                        }
                        Console.Out.Flush();
                    }
                    errors += FindTextures(gameId, textures, modifiedFiles[i], true, ref log);
                }

                for (int i = 0; i < addedFiles.Count; i++, currentPackage++)
                {
                    if (installer != null)
                        installer.updateProgressStatus("Scanning textures " + ((currentPackage + 1) * 100) / totalPackages + "% ");
                    if (mainWindow != null)
                        mainWindow.updateStatusLabel("Finding textures in package " + (currentPackage + 1) + " of " + totalPackages + " - " + addedFiles[i]);
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE " + addedFiles[i]);
                        int newProgress = currentPackage * 100 / totalPackages;
                        if (lastProgress != newProgress)
                        {
                            Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                            lastProgress = newProgress;
                        }
                        Console.Out.Flush();
                    }
                    errors += FindTextures(gameId, textures, addedFiles[i], false, ref log);
                }

                for (int k = 0; k < textures.Count; k++)
                {
                    bool found = false;
                    for (int t = 0; t < textures[k].list.Count; t++)
                    {
                        if (textures[k].list[t].path != "")
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        textures[k].list.Clear();
                        textures.Remove(textures[k]);
                        k--;
                    }
                }
            }
            else
            {
                int lastProgress = -1;
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    if (installer != null)
                        installer.updateProgressStatus("Scanning textures " + ((i + 1) * 100) / GameData.packageFiles.Count + "% ");
                    if (mainWindow != null)
                        mainWindow.updateStatusLabel("Finding textures in package " + (i + 1) + " of " + GameData.packageFiles.Count + " - " + GameData.packageFiles[i]);
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE " + GameData.packageFiles[i]);
                        int newProgress = i * 100 / GameData.packageFiles.Count;
                        if (lastProgress != newProgress)
                        {
                            Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                            lastProgress = newProgress;
                        }
                        Console.Out.Flush();
                    }
                    FindTextures(gameId, textures, GameData.RelativeGameData(GameData.packageFiles[i]), false, ref log);
                }
            }

            if (gameId == MeType.ME1_TYPE)
            {
                for (int k = 0; k < textures.Count; k++)
                {
                    for (int t = 0; t < textures[k].list.Count; t++)
                    {
                        uint mipmapOffset = textures[k].list[t].mipmapOffset;
                        if (textures[k].list[t].slave)
                        {
                            MatchedTexture slaveTexture = textures[k].list[t];
                            string basePkgName = slaveTexture.basePackageName;
                            if (basePkgName == Path.GetFileNameWithoutExtension(slaveTexture.path).ToUpperInvariant())
                                throw new Exception();
                            for (int j = 0; j < textures[k].list.Count; j++)
                            {
                                if (!textures[k].list[j].slave &&
                                   textures[k].list[j].mipmapOffset == mipmapOffset &&
                                   textures[k].list[j].packageName == basePkgName)
                                {
                                    slaveTexture.linkToMaster = j;
                                    slaveTexture.slave = true;
                                    textures[k].list[t] = slaveTexture;
                                    break;
                                }
                            }
                        }
                    }
                    if (!textures[k].list.Exists(s => s.slave) &&
                        textures[k].list.Exists(s => s.weakSlave))
                    {
                        List<MatchedTexture> texList = new List<MatchedTexture>();
                        for (int t = 0; t < textures[k].list.Count; t++)
                        {
                            MatchedTexture tex = textures[k].list[t];
                            if (tex.weakSlave)
                                texList.Add(tex);
                            else
                                texList.Insert(0, tex);
                        }
                        FoundTexture f = textures[k];
                        f.list = texList;
                        textures[k] = f;
                        if (textures[k].list[0].weakSlave)
                            continue;

                        for (int t = 0; t < textures[k].list.Count; t++)
                        {
                            if (textures[k].list[t].weakSlave)
                            {
                                MatchedTexture slaveTexture = textures[k].list[t];
                                string basePkgName = slaveTexture.basePackageName;
                                if (basePkgName == Path.GetFileNameWithoutExtension(slaveTexture.path).ToUpperInvariant())
                                    throw new Exception();
                                for (int j = 0; j < textures[k].list.Count; j++)
                                {
                                    if (!textures[k].list[j].weakSlave &&
                                       textures[k].list[j].packageName == basePkgName)
                                    {
                                        slaveTexture.linkToMaster = j;
                                        slaveTexture.slave = true;
                                        textures[k].list[t] = slaveTexture;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            if (File.Exists(filename))
                File.Delete(filename);

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                MemoryStream mem = new MemoryStream();
                mem.WriteUInt32(textureMapBinTag);
                mem.WriteUInt32(textureMapBinVersion);
                mem.WriteInt32(textures.Count);

                for (int i = 0; i < textures.Count; i++)
                {
                    if (generateBuiltinMapFiles)
                        mem.WriteByte((byte)textures[i].name.Length);
                    else
                        mem.WriteInt32(textures[i].name.Length);
                    mem.WriteStringASCII(textures[i].name);
                    mem.WriteUInt32(textures[i].crc);
                    if (generateBuiltinMapFiles)
                    {
                        mem.WriteInt16((short)textures[i].width);
                        mem.WriteInt16((short)textures[i].height);
                        mem.WriteByte((byte)textures[i].pixfmt);
                        mem.WriteByte((byte)textures[i].flags);

                        mem.WriteInt16((short)textures[i].list.Count);
                    }
                    else
                    {
                        mem.WriteInt32(textures[i].list.Count);
                    }
                    for (int k = 0; k < textures[i].list.Count; k++)
                    {
                        mem.WriteInt32(textures[i].list[k].exportID);
                        if (generateBuiltinMapFiles)
                        {
                            if (GameData.gameType == MeType.ME1_TYPE)
                            {
                                mem.WriteInt16((short)textures[i].list[k].linkToMaster);
                                if (textures[i].list[k].linkToMaster != -1)
                                    mem.WriteStringASCIINull(textures[i].list[k].basePackageName);
                            }
                            mem.WriteByte(textures[i].list[k].removeEmptyMips ? (byte)1 : (byte)0);
                            mem.WriteByte((byte)textures[i].list[k].numMips);
                            mem.WriteInt16((short)pkgs.IndexOf(textures[i].list[k].path));
                        }
                        else
                        {
                            mem.WriteInt32(textures[i].list[k].linkToMaster);
                            mem.WriteInt32(textures[i].list[k].path.Length);
                            mem.WriteStringASCII(textures[i].list[k].path);
                        }
                    }
                }
                if (!generateBuiltinMapFiles)
                {
                    mem.WriteInt32(GameData.packageFiles.Count);
                    for (int i = 0; i < GameData.packageFiles.Count; i++)
                    {
                        string s = GameData.RelativeGameData(GameData.packageFiles[i]);
                        mem.WriteInt32(s.Length);
                        mem.WriteStringASCII(s);
                    }
                }
                mem.SeekBegin();

                if (generateBuiltinMapFiles)
                {
                    fs.WriteUInt32(0x504D5443);
                    fs.WriteUInt32((uint)mem.Length);
                    byte[] compressed = new ZlibHelper.Zlib().Compress(mem.ToArray(), 9);
                    fs.WriteUInt32((uint)compressed.Length);
                    fs.WriteFromBuffer(compressed);
                }
                else
                {
                    fs.WriteFromStream(mem, mem.Length);
                }
            }

            if (mainWindow != null)
            {
                if (!generateBuiltinMapFiles)
                {
                    MipMaps mipmaps = new MipMaps();
                    if (GameData.gameType == MeType.ME1_TYPE)
                    {
                        errors += mipmaps.removeMipMapsME1(1, textures, mainWindow, null, false);
                        errors += mipmaps.removeMipMapsME1(2, textures, mainWindow, null, false);
                    }
                    else
                    {
                        errors += mipmaps.removeMipMapsME2ME3(textures, mainWindow, null, false, false);
                    }
                    if (GameData.gameType == MeType.ME3_TYPE)
                    {
                        TOCBinFile.UpdateAllTOCBinFiles();
                    }
                }
            }

            treeScan = textures;

            if (mainWindow != null)
            {
                var time = Misc.stopTimer();
                mainWindow.updateStatusLabel("Done. Process total time: " + Misc.getTimerFormat(time));
                mainWindow.updateStatusLabel2("");
            }

            return errors;
        }

        private string FindTextures(MeType gameId, List<FoundTexture> textures, string packagePath, bool modified, ref string log)
        {
            string errors = "";
            Package package = null;

            try
            {
                package = new Package(GameData.GamePath + packagePath);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Problem with PCC file header:"))
                    return errors;
                string err = "";
                err += "---- Start --------------------------------------------" + Environment.NewLine;
                err += "Issue opening package file: " + packagePath + Environment.NewLine;
                err += e.Message + Environment.NewLine + Environment.NewLine;
                err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                errors += err;
                log += err;
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
                    matchTexture.exportID = i;
                    matchTexture.path = packagePath;
                    matchTexture.packageName = texture.packageName;
                    matchTexture.removeEmptyMips = texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty);
                    matchTexture.numMips = texture.mipMapsList.FindAll(s => s.storageType != Texture.StorageTypes.empty).Count;
                    if (gameId == MeType.ME1_TYPE)
                    {
                        matchTexture.basePackageName = texture.basePackageName;
                        matchTexture.slave = texture.slave;
                        matchTexture.weakSlave = texture.weakSlave;
                        matchTexture.linkToMaster = -1;
                        if (matchTexture.slave)
                            matchTexture.mipmapOffset = mipmap.dataOffset;
                        else
                            matchTexture.mipmapOffset = package.exportsTable[i].dataOffset + (uint)texture.properties.propertyEndOffset + mipmap.internalOffset;
                    }

                    uint crc = 0;
                    try
                    {
                        crc = texture.getCrcTopMipmap();
                    }
                    catch
                    {
                    }
                    if (crc == 0)
                    {
                        errors += "Error: Texture " + package.exportsTable[i].objectName + " is broken in package: " + packagePath + ", skipping..." + Environment.NewLine;
                        log += "Error: Texture " + package.exportsTable[i].objectName + " is broken in package: " + packagePath + ", skipping..." + Environment.NewLine;
                        continue;
                    }

                    FoundTexture foundTexName = textures.Find(s => s.crc == crc);
                    if (foundTexName.crc != 0)
                    {
                        if (modified && foundTexName.list.Exists(s => (s.exportID == i && s.path.ToLowerInvariant() == packagePath.ToLowerInvariant())))
                            continue;
                        if (matchTexture.slave || gameId != MeType.ME1_TYPE)
                            foundTexName.list.Add(matchTexture);
                        else
                            foundTexName.list.Insert(0, matchTexture);
                    }
                    else
                    {
                        if (modified)
                        {
                            for (int k = 0; k < textures.Count; k++)
                            {
                                bool found = false;
                                for (int t = 0; t < textures[k].list.Count; t++)
                                {
                                    if (textures[k].list[t].exportID == i &&
                                        textures[k].list[t].path.ToLowerInvariant() == packagePath.ToLowerInvariant())
                                    {
                                        MatchedTexture f = textures[k].list[t];
                                        f.path = "";
                                        textures[k].list[t] = f;
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                    break;
                            }
                        }
                        FoundTexture foundTex = new FoundTexture();
                        foundTex.list = new List<MatchedTexture>();
                        foundTex.list.Add(matchTexture);
                        foundTex.name = name;
                        foundTex.crc = crc;
                        if (generateBuiltinMapFiles)
                        {
                            foundTex.width = texture.getTopMipmap().width;
                            foundTex.height = texture.getTopMipmap().height;
                            foundTex.pixfmt = Image.getPixelFormatType(texture.properties.getProperty("Format").valueName);
                            if (texture.properties.exists("CompressionSettings"))
                            {
                                string cmp = texture.properties.getProperty("CompressionSettings").valueName;
                                if (cmp == "TC_OneBitAlpha")
                                    foundTex.flags = TexProperty.TextureTypes.OneBitAlpha;
                                else if (cmp == "TC_Displacementmap")
                                    foundTex.flags = TexProperty.TextureTypes.Displacementmap;
                                else if (cmp == "TC_Grayscale")
                                    foundTex.flags = TexProperty.TextureTypes.GreyScale;
                                else if (cmp == "TC_Normalmap" ||
                                    cmp == "TC_NormalmapHQ" ||
                                    cmp == "TC_NormalmapAlpha" ||
                                    cmp == "TC_NormalmapUncompressed")
                                {
                                    foundTex.flags = TexProperty.TextureTypes.Normalmap;
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                            else
                            {
                                foundTex.flags = TexProperty.TextureTypes.Normal;
                            }
                        }
                        textures.Add(foundTex);
                    }
                }
            }

            package.Dispose();

            return errors;
        }
    }

    public partial class TexExplorer : Form
    {
        private void PrepareTreeList()
        {
            nodeList = new List<PackageTreeNode>();
            PackageTreeNode rootNode = new PackageTreeNode("All Packages");
            for (int t = 0; t < _textures.Count; t++)
            {
                for (int l = 0; l < _textures[t].list.Count; l++)
                {
                    if (_textures[t].list[l].path == "")
                        continue;

                    string packageName = Path.GetFileNameWithoutExtension(_textures[t].list[l].path).ToUpperInvariant();
                    bool found = false;
                    for (int n = 0; n < nodeList.Count; n++)
                    {
                        if (nodeList[n].Name == packageName)
                        {
                            nodeList[n].textures.Add(_textures[t]);
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        PackageTreeNode treeNode = new PackageTreeNode(packageName);
                        treeNode.textures.Add(_textures[t]);
                        rootNode.Nodes.Add(treeNode);
                        nodeList.Add(treeNode);
                    }
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
