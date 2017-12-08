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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MassEffectModder
{
    static public class CmdLineConverter
    {
        static List<FoundTexture> textures;

        static private void loadTexturesMap(MeType gameId)
        {
            Stream fs;
            textures = new List<FoundTexture>();
            byte[] buffer = null;

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
                int len = fs.ReadInt32();
                texture.name = fs.ReadStringASCII(len);
                texture.crc = fs.ReadUInt32();
                texture.width = fs.ReadInt32();
                texture.height = fs.ReadInt32();
                texture.pixfmt = (PixelFormat)fs.ReadInt32();
                texture.alphadxt1 = fs.ReadInt32() != 0;
                texture.numMips = fs.ReadInt32();
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
        }

        static private bool loadTexturesMapFile(string path)
        {
            List<FoundTexture> textures = new List<FoundTexture>();

            if (!File.Exists(path))
                return false;

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != TexExplorer.textureMapBinTag || version != TexExplorer.textureMapBinVersion)
                {
                    Console.WriteLine("Detected wrong or old version of textures scan file!" + Environment.NewLine);
                    return false;
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
                        Console.WriteLine("Detected removal of game files since last game data scan." + Environment.NewLine + Environment.NewLine);
                        return false;
                    }
                }
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    if (packages.Find(s => s.Equals(GameData.packageFiles[i], StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        Console.WriteLine("Detected additional game files not present in latest game data scan." + Environment.NewLine + Environment.NewLine);
                        return false;
                    }
                }
            }

            return true;
        }

        static public bool convertDataModtoMem(string inputDir, string memFilePath,
            MeType gameId, MainWindow mainWindow, ref string errors, bool onlyIndividual = false, bool ipc = false)
        {
            string[] files = null;

            loadTexturesMap(gameId);

            Console.WriteLine("Mods conversion started...");

            List<string> list;
            List<string> list2;
            if (!onlyIndividual)
            {
                list = Directory.GetFiles(inputDir, "*.mem").Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
                list.Sort();
                list2 = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
                list2.AddRange(Directory.GetFiles(inputDir, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                list = new List<string>();
                list2 = new List<string>();
            }
            list2.AddRange(Directory.GetFiles(inputDir, "*.bin").Where(item => item.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.dds").Where(item => item.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.png").Where(item => item.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.bmp").Where(item => item.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.tga").Where(item => item.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.jpg").Where(item => item.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.jpeg").Where(item => item.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)));
            list2.Sort();
            list.AddRange(list2);
            files = list.ToArray();

            int result;
            string fileName = "";
            ulong dstLen = 0;
            string[] ddsList = null;
            ulong numEntries = 0;
            FileStream outFs;

            List<TexExplorer.BinaryMod> mods = new List<TexExplorer.BinaryMod>();
            List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();

            if (File.Exists(memFilePath))
                File.Delete(memFilePath);
            outFs = new FileStream(memFilePath, FileMode.Create, FileAccess.Write);
            outFs.WriteUInt32(TexExplorer.TextureModTag);
            outFs.WriteUInt32(TexExplorer.TextureModVersion);
            outFs.WriteInt64(0); // filled later

            for (int n = 0; n < files.Count(); n++)
            {
                string file = files[n];
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Creating MEM: " + Path.GetFileName(memFilePath));
                    mainWindow.updateStatusLabel2("File " + (n + 1) + " of " + files.Count() + ", " + Path.GetFileName(file));
                }
                Console.WriteLine("File: " + Path.GetFileName(file));
                if (ipc)
                {
                    Console.WriteLine("[IPC]PROCESSING_FILE " + Path.GetFileName(file));
                    Console.WriteLine("[IPC]OVERALL_PROGRESS " + (n * 100) / files.Count());
                    Console.Out.Flush();
                }

                if (file.EndsWith(".mem", StringComparison.OrdinalIgnoreCase))
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                        {
                            if (version != TexExplorer.TextureModVersion)
                            {
                                errors += "File " + file + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                                Console.WriteLine("File " + file + " was made with an older version of MEM, skipping...");
                            }
                            else
                            {
                                errors += "File " + file + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                                Console.WriteLine("File " + file + " is not a valid MEM mod, skipping...");
                            }
                            continue;
                        }
                        else
                        {
                            uint gameType = 0;
                            fs.JumpTo(fs.ReadInt64());
                            gameType = fs.ReadUInt32();
                            if ((MeType)gameType != gameId)
                            {
                                errors += "File " + file + " is not a MEM mod valid for this game" + Environment.NewLine;
                                Console.WriteLine("File " + file + " is not a MEM mod valid for this game");
                                continue;
                            }
                        }
                        int numFiles = fs.ReadInt32();
                        for (int l = 0; l < numFiles; l++)
                        {
                            MipMaps.FileMod fileMod = new MipMaps.FileMod();
                            fileMod.tag = fs.ReadUInt32();
                            fileMod.name = fs.ReadStringASCIINull();
                            fileMod.offset = fs.ReadInt64();
                            fileMod.size = fs.ReadInt64();
                            long prevPos = fs.Position;
                            fs.JumpTo(fileMod.offset);
                            fileMod.offset = outFs.Position;
                            if (fileMod.tag == MipMaps.FileTextureTag)
                            {
                                outFs.WriteStringASCIINull(fs.ReadStringASCIINull());
                                outFs.WriteUInt32(fs.ReadUInt32());
                            }
                            else if (fileMod.tag == MipMaps.FileBinaryTag)
                            {
                                outFs.WriteInt32(fs.ReadInt32());
                                outFs.WriteStringASCIINull(fs.ReadStringASCIINull());
                            }
                            outFs.WriteFromStream(fs, fileMod.size);
                            fs.JumpTo(prevPos);
                            modFiles.Add(fileMod);
                        }
                    }
                }
                else if (file.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            string package = "";
                            int len = fs.ReadInt32();
                            string version = fs.ReadStringASCIINull();
                            if (version.Length < 5) // legacy .mod
                                fs.SeekBegin();
                            else
                            {
                                fs.SeekBegin();
                                len = fs.ReadInt32();
                                version = fs.ReadStringASCII(len); // version
                            }
                            numEntries = fs.ReadUInt32();
                            for (uint i = 0; i < numEntries; i++)
                            {
                                TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                                len = fs.ReadInt32();
                                string desc = fs.ReadStringASCII(len); // description
                                len = fs.ReadInt32();
                                string scriptLegacy = fs.ReadStringASCII(len);
                                string path = "";
                                if (desc.Contains("Binary Replacement"))
                                {
                                    try
                                    {
                                        Misc.ParseME3xBinaryScriptMod(scriptLegacy, ref package, ref mod.exportId, ref path);
                                        if (mod.exportId == -1 || package == "" || path == "")
                                            throw new Exception();
                                    }
                                    catch
                                    {
                                        len = fs.ReadInt32();
                                        fs.Skip(len);
                                        errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        Console.WriteLine("Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file);
                                        continue;
                                    }
                                    mod.packagePath = Path.Combine(path, package);
                                    mod.binaryMod = true;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);
                                }
                                else
                                {
                                    string textureName = desc.Split(' ').Last();
                                    FoundTexture f;
                                    try
                                    {
                                        f = Misc.ParseLegacyMe3xScriptMod(textures, scriptLegacy, textureName);
                                        mod.textureCrc = f.crc;
                                        if (mod.textureCrc == 0)
                                            throw new Exception();
                                    }
                                    catch
                                    {
                                        len = fs.ReadInt32();
                                        fs.Skip(len);
                                        errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        Console.WriteLine("Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file);
                                        continue;
                                    }
                                    textureName = f.name;
                                    mod.textureName = textureName;
                                    mod.binaryMod = false;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);

                                    PixelFormat pixelFormat = f.pixfmt;
                                    Image image = new Image(mod.data, Image.ImageFormat.DDS);

                                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                        f.width / f.height)
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        Console.WriteLine("Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file);
                                        continue;
                                    }

                                    if (!image.checkDDSHaveAllMipmaps() ||
                                        (f.numMips != 1 && image.mipMaps.Count() == 1) ||
                                        image.pixelFormat != pixelFormat)
                                    {
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]PROCESSING_FILE Converting " + textureName);
                                            Console.Out.Flush();
                                        }
                                        Console.WriteLine("Converting/correcting texture: " + textureName);
                                        bool dxt1HasAlpha = false;
                                        byte dxt1Threshold = 128;
                                        if (f.alphadxt1)
                                        {
                                            dxt1HasAlpha = true;
                                            if (image.pixelFormat == PixelFormat.ARGB ||
                                                image.pixelFormat == PixelFormat.DXT3 ||
                                                image.pixelFormat == PixelFormat.DXT5)
                                            {
                                                Console.WriteLine("Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha.");
                                            }
                                        }
                                        image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
                                        mod.data = image.StoreImageToDDS();
                                    }
                                }
                                mods.Add(mod);
                            }
                        }
                    }
                    catch
                    {
                        errors += "Mod is not compatible: " + file + Environment.NewLine;
                        Console.WriteLine("Mod is not compatible: " + file);
                        continue;
                    }
                }
                else if (file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                    try
                    {
                        string filename = Path.GetFileNameWithoutExtension(file);
                        string dlcName = "";
                        int posStr = 0;
                        if (filename.ToUpperInvariant()[0] == 'D')
                        {
                            string tmpDLC = filename.Split('-')[0];
                            int lenDLC = int.Parse(tmpDLC.Substring(1));
                            dlcName = filename.Substring(tmpDLC.Length + 1, lenDLC);
                            posStr += tmpDLC.Length + lenDLC + 1;
                            if (filename[posStr++] != '-')
                                throw new Exception();
                        }
                        else if (filename.ToUpperInvariant()[0] == 'B')
                        {
                            posStr += 1;
                        }
                        else
                            throw new Exception();
                        string tmpPkg = filename.Substring(posStr).Split('-')[0];
                        posStr += tmpPkg.Length + 1;
                        int lenPkg = int.Parse(tmpPkg.Substring(0));
                        string pkgName = filename.Substring(posStr, lenPkg);
                        posStr += lenPkg;
                        if (filename[posStr++] != '-')
                            throw new Exception();
                        if (filename.ToUpperInvariant()[posStr++] != 'E')
                            throw new Exception();
                        string tmpExp = filename.Substring(posStr);
                        mod.exportId = int.Parse(tmpExp.Substring(0));
                        if (dlcName != "")
                        {
                            if (gameId == MeType.ME1_TYPE)
                                mod.packagePath = @"\DLC\" + dlcName + @"\CookedPC\" + pkgName;
                            else if (gameId == MeType.ME2_TYPE)
                                mod.packagePath = @"\BioGame\DLC\" + dlcName + @"\CookedPC\" + pkgName;
                            else
                                mod.packagePath = @"\BIOGame\DLC\" + dlcName + @"\CookedPCConsole\" + pkgName;
                        }
                        else
                        {
                            if (gameId == MeType.ME1_TYPE || gameId == MeType.ME2_TYPE)
                                mod.packagePath = @"\BioGame\CookedPC\" + pkgName;
                            else
                                mod.packagePath = @"\BIOGame\CookedPCConsole\" + pkgName;
                        }
                        mod.binaryMod = true;
                        mod.data = File.ReadAllBytes(file);
                        mods.Add(mod);
                    }
                    catch
                    {
                        errors += "Filename not valid: " + file + Environment.NewLine;
                        Console.WriteLine("Filename not valid: " + file);
                        continue;
                    }
                }
                else if (file.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
                {
                    int indexTpf = -1;
                    IntPtr handle = IntPtr.Zero;
                    ZlibHelper.Zip zip = new ZlibHelper.Zip();
                    try
                    {
                        byte[] buffer = File.ReadAllBytes(file);
                        handle = zip.Open(buffer, ref numEntries, 1);
                        for (ulong i = 0; i < numEntries; i++)
                        {
                            result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            fileName = fileName.Trim();
                            if (Path.GetExtension(fileName).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(fileName).ToLowerInvariant() == ".log")
                            {
                                indexTpf = (int)i;
                                break;
                            }
                            result = zip.GoToNextFile(handle);
                            if (result != 0)
                                throw new Exception();
                        }
                        byte[] listText = new byte[dstLen];
                        result = zip.ReadCurrentFile(handle, listText, dstLen);
                        if (result != 0)
                            throw new Exception();
                        ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                        result = zip.GoToFirstFile(handle);
                        if (result != 0)
                            throw new Exception();

                        for (uint i = 0; i < numEntries; i++)
                        {
                            if (i == indexTpf)
                            {
                                result = zip.GoToNextFile(handle);
                                continue;
                            }
                            TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                            try
                            {
                                uint crc = 0;
                                result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                                if (result != 0)
                                    throw new Exception();
                                string filename = Path.GetFileName(fileName).Trim();
                                foreach (string dds in ddsList)
                                {
                                    string ddsFile = dds.Split('|')[1];
                                    if (ddsFile.ToLowerInvariant().Trim() != filename.ToLowerInvariant())
                                        continue;
                                    crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }
                                if (crc == 0)
                                {
                                    if (Path.GetExtension(filename).ToLowerInvariant() != ".def" &&
                                        Path.GetExtension(filename).ToLowerInvariant() != ".log")
                                    {
                                        errors += "Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        Console.WriteLine("Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + file);
                                    }
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                                if (foundCrcList.Count == 0)
                                {
                                    Console.WriteLine("Texture skipped. File " + filename + string.Format(" - 0x{0:X8}", crc) + " is not present in your game setup - mod: " + file);
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                string textureName = foundCrcList[0].name;
                                mod.textureName = textureName;
                                mod.binaryMod = false;
                                mod.textureCrc = crc;
                                mod.data = new byte[dstLen];
                                result = zip.ReadCurrentFile(handle, mod.data, dstLen);
                                if (result != 0)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    Console.WriteLine("Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + file);
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                PixelFormat pixelFormat = foundCrcList[0].pixfmt;
                                Image image = new Image(mod.data, Path.GetExtension(filename));

                                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                    foundCrcList[0].width / foundCrcList[0].height)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    Console.WriteLine("Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file);
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                if (!image.checkDDSHaveAllMipmaps() ||
                                   (foundCrcList[0].numMips != 1 && image.mipMaps.Count() == 1) ||
                                    image.pixelFormat != pixelFormat)
                                {
                                    bool dxt1HasAlpha = false;
                                    byte dxt1Threshold = 128;
                                    if (foundCrcList[0].alphadxt1)
                                    {
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]PROCESSING_FILE Converting " + textureName);
                                            Console.Out.Flush();
                                        }
                                        Console.WriteLine("Converting/correcting texture: " + textureName);
                                        dxt1HasAlpha = true;
                                        if (image.pixelFormat == PixelFormat.ARGB ||
                                            image.pixelFormat == PixelFormat.DXT3 ||
                                            image.pixelFormat == PixelFormat.DXT5)
                                        {
                                            Console.WriteLine("Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha.");
                                        }
                                    }
                                    image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
                                    mod.data = image.StoreImageToDDS();
                                }
                                mods.Add(mod);
                            }
                            catch
                            {
                                errors += "Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                                Console.WriteLine("Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file);
                            }
                            zip.GoToNextFile(handle);
                        }
                        zip.Close(handle);
                        handle = IntPtr.Zero;
                    }
                    catch
                    {
                        errors += "Mod is not compatible: " + file + Environment.NewLine;
                        Console.WriteLine("Mod is not compatible: " + file + Environment.NewLine);
                        if (handle != IntPtr.Zero)
                            zip.Close(handle);
                        handle = IntPtr.Zero;
                        continue;
                    }
                }
                else if (file.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                    string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!filename.Contains("0x"))
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                        continue;
                    }
                    uint crc;
                    string crcStr = filename.Substring(idx + 2, 8);
                    try
                    {
                        crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        Console.WriteLine("Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup.");
                        continue;
                    }

                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        PixelFormat pixelFormat = foundCrcList[0].pixfmt;

                        mod.data = fs.ReadToBuffer((int)fs.Length);
                        Image image = new Image(mod.data, Image.ImageFormat.DDS);

                        if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                            foundCrcList[0].width / foundCrcList[0].height)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                            Console.WriteLine("Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture...");
                            continue;
                        }

                        if (!image.checkDDSHaveAllMipmaps() ||
                           (foundCrcList[0].numMips != 1 && image.mipMaps.Count() == 1) ||
                            image.pixelFormat != pixelFormat)
                        {
                            if (ipc)
                            {
                                Console.WriteLine("[IPC]PROCESSING_FILE Converting " + Path.GetFileName(file));
                                Console.Out.Flush();
                            }
                            Console.WriteLine("Converting/correcting texture: " + Path.GetFileName(file));
                            bool dxt1HasAlpha = false;
                            byte dxt1Threshold = 128;
                            if (foundCrcList[0].alphadxt1)
                            {
                                dxt1HasAlpha = true;
                                if (image.pixelFormat == PixelFormat.ARGB ||
                                    image.pixelFormat == PixelFormat.DXT3 ||
                                    image.pixelFormat == PixelFormat.DXT5)
                                {
                                    Console.WriteLine("Warning for texture: " + Path.GetFileName(file) + ". This texture converted from full alpha to binary alpha.");
                                }
                            }
                            image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
                            mod.data = image.StoreImageToDDS();
                        }

                        mod.textureName = foundCrcList[0].name;
                        mod.binaryMod = false;
                        mod.textureCrc = crc;
                        mods.Add(mod);
                    }
                }
                else if (
                    file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                    string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!filename.Contains("0x"))
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                        continue;
                    }
                    uint crc;
                    string crcStr = filename.Substring(idx + 2, 8);
                    try
                    {
                        crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        Console.WriteLine("Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup.");
                        continue;
                    }

                    PixelFormat pixelFormat = foundCrcList[0].pixfmt;
                    Image image = new Image(file, Image.ImageFormat.Unknown).convertToARGB();

                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                        foundCrcList[0].width / foundCrcList[0].height)
                    {
                        errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                        Console.WriteLine("Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture...");
                        continue;
                    }

                    bool dxt1HasAlpha = false;
                    byte dxt1Threshold = 128;
                    if (foundCrcList[0].alphadxt1)
                    {
                        dxt1HasAlpha = true;
                        if (image.pixelFormat == PixelFormat.ARGB ||
                            image.pixelFormat == PixelFormat.DXT3 ||
                            image.pixelFormat == PixelFormat.DXT5)
                        {
                            Console.WriteLine("Warning for texture: " + Path.GetFileName(file) + ". This texture converted from full alpha to binary alpha.");
                        }
                    }
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE Converting " + Path.GetFileName(file));
                        Console.Out.Flush();
                    }
                    Console.WriteLine("Converting/correcting texture: " + Path.GetFileName(file));
                    image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
                    mod.data = image.StoreImageToDDS();
                    mod.textureName = foundCrcList[0].name;
                    mod.binaryMod = false;
                    mod.textureCrc = crc;
                    mods.Add(mod);
                }

                for (int l = 0; l < mods.Count; l++)
                {
                    MipMaps.FileMod fileMod = new MipMaps.FileMod();
                    Stream dst = MipMaps.compressData(mods[l].data);
                    dst.SeekBegin();
                    fileMod.offset = outFs.Position;
                    fileMod.size = dst.Length;

                    if (mods[l].binaryMod)
                    {
                        fileMod.tag = MipMaps.FileBinaryTag;
                        if (mods[l].packagePath.Contains("\\DLC\\"))
                        {
                            string dlcName = mods[l].packagePath.Split('\\')[3];
                            fileMod.name = "D" + dlcName.Length + "-" + dlcName + "-";
                        }
                        else
                        {
                            fileMod.name = "B";
                        }
                        fileMod.name += Path.GetFileName(mods[l].packagePath).Length + "-" + Path.GetFileName(mods[l].packagePath) + "-E" + mods[l].exportId + ".bin";

                        outFs.WriteInt32(mods[l].exportId);
                        outFs.WriteStringASCIINull(mods[l].packagePath);
                    }
                    else
                    {
                        fileMod.tag = MipMaps.FileTextureTag;
                        fileMod.name = mods[l].textureName + string.Format("_0x{0:X8}", mods[l].textureCrc) + ".dds";
                        outFs.WriteStringASCIINull(mods[l].textureName);
                        outFs.WriteUInt32(mods[l].textureCrc);
                    }
                    outFs.WriteFromStream(dst, dst.Length);
                    modFiles.Add(fileMod);
                }
                mods.Clear();
            }

            if (modFiles.Count == 0)
            {
                outFs.Close();
                if (File.Exists(memFilePath))
                    File.Delete(memFilePath);
                if (mainWindow == null)
                    Console.WriteLine("Mods conversion failed - nothing converted!");
                return false;
            }

            long pos = outFs.Position;
            outFs.SeekBegin();
            outFs.WriteUInt32(TexExplorer.TextureModTag);
            outFs.WriteUInt32(TexExplorer.TextureModVersion);
            outFs.WriteInt64(pos);
            outFs.JumpTo(pos);
            outFs.WriteUInt32((uint)gameId);
            outFs.WriteInt32(modFiles.Count);
            for (int i = 0; i < modFiles.Count; i++)
            {
                outFs.WriteUInt32(modFiles[i].tag);
                outFs.WriteStringASCIINull(modFiles[i].name);
                outFs.WriteInt64(modFiles[i].offset);
                outFs.WriteInt64(modFiles[i].size);
            }

            outFs.Close();
            if (mainWindow == null)
                Console.WriteLine("Mods conversion process completed");

            return true;
        }

        static public bool ConvertToMEM(MeType gameId, string inputDir, string memFile, bool ipc)
        {
            string errors = "";
            bool status = convertDataModtoMem(inputDir, memFile, gameId, null, ref errors, false, ipc);
            if (errors != "")
                Console.WriteLine("Error: Some errors have occured");

            return status;
        }

        static public bool convertGameTexture(string inputFile, string outputFile)
        {
            string filename = Path.GetFileNameWithoutExtension(inputFile).ToLowerInvariant();
            if (!filename.Contains("0x"))
            {
                Console.WriteLine("Texture filename not valid: " + Path.GetFileName(inputFile) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                return false;
            }
            int idx = filename.IndexOf("0x");
            if (filename.Length - idx < 10)
            {
                Console.WriteLine("Texture filename not valid: " + Path.GetFileName(inputFile) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                return false;
            }
            uint crc;
            string crcStr = filename.Substring(idx + 2, 8);
            try
            {
                crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                Console.WriteLine("Texture filename not valid: " + Path.GetFileName(inputFile) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture...");
                return false;
            }

            List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
            if (foundCrcList.Count == 0)
            {
                Console.WriteLine("Texture skipped. Texture " + Path.GetFileName(inputFile) + " is not present in your game setup.");
                return false;
            }

            PixelFormat pixelFormat = foundCrcList[0].pixfmt;
            Image image = new Image(inputFile);

            if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                foundCrcList[0].width / foundCrcList[0].height)
            {
                Console.WriteLine("Error in texture: " + Path.GetFileName(inputFile) + " This texture has wrong aspect ratio, skipping texture...");
                return false;
            }

            bool dxt1HasAlpha = false;
            byte dxt1Threshold = 128;
            if (foundCrcList[0].alphadxt1)
            {
                dxt1HasAlpha = true;
                if (image.pixelFormat == PixelFormat.ARGB ||
                    image.pixelFormat == PixelFormat.DXT3 ||
                    image.pixelFormat == PixelFormat.DXT5)
                {
                    Console.WriteLine("Warning for texture: " + Path.GetFileName(inputFile) + ". This texture converted from full alpha to binary alpha.");
                }
            }
            image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                fs.WriteFromBuffer(image.StoreImageToDDS());
            }

            return true;
        }

        static public bool convertGameImage(MeType gameId, string inputFile, string outputFile)
        {
            loadTexturesMap(gameId);

            bool status = convertGameTexture(inputFile, outputFile);
            if (!status)
            {
                Console.WriteLine("Error: Some errors have occured.");
            }

            return status;
        }

        static public bool convertGameImages(MeType gameId, string inputDir, string outputDir)
        {
            loadTexturesMap(gameId);

            List<string> list = Directory.GetFiles(inputDir, "*.dds").Where(item => item.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)).ToList();
            list.AddRange(Directory.GetFiles(inputDir, "*.png").Where(item => item.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.bmp").Where(item => item.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.tga").Where(item => item.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.jpg").Where(item => item.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.jpeg").Where(item => item.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)));
            list.Sort();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            bool status = true;
            foreach (string file in list)
            {
                if (!convertGameTexture(file, Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".dds")))
                    status = false;
            }

            return status;
        }

        static public bool convertImage(string inputFile, string outputFile, string format, string threshold)
        {
            format = format.ToLowerInvariant();
            PixelFormat pixFmt;
            bool dxt1HasAlpha = false;
            byte dxt1Threshold = 128;
            try
            {
                dxt1Threshold = byte.Parse(threshold);
            }
            catch
            {
                Console.WriteLine("Error: wrong threshold for dxt1: " + threshold);
                return false;
            }

            switch (format)
            {
                case "dxt1":
                    pixFmt = PixelFormat.DXT1;
                    break;
                case "dxt1a":
                    pixFmt = PixelFormat.DXT1;
                    dxt1HasAlpha = true;
                    break;
                case "dxt3":
                    pixFmt = PixelFormat.DXT3;
                    break;
                case "dxt5":
                    pixFmt = PixelFormat.DXT5;
                    break;
                case "ati2":
                    pixFmt = PixelFormat.ATI2;
                    break;
                case "v8u8":
                    pixFmt = PixelFormat.V8U8;
                    break;
                case "argb":
                    pixFmt = PixelFormat.ARGB;
                    break;
                case "rgb":
                    pixFmt = PixelFormat.RGB;
                    break;
                case "g8":
                    pixFmt = PixelFormat.G8;
                    break;
                default:
                    Console.WriteLine("Error: not supported format: " + format);
                    return false;

            }
            Image image = new Image(inputFile);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            image.correctMips(pixFmt, dxt1HasAlpha, dxt1Threshold);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                fs.WriteFromBuffer(image.StoreImageToDDS());
            }

            return true;
        }

        static public bool extractTPF(string inputDir, string outputDir)
        {
            Console.WriteLine("Extract TPF files started...");

            bool status = true;
            string[] files = null;
            int result;
            string fileName = "";
            ulong dstLen = 0;
            ulong numEntries = 0;
            List<string> list = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
            list.Sort();
            files = list.ToArray();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            foreach (string file in files)
            {
                Console.WriteLine("Extract TPF: " + file);
                string outputTPFdir = outputDir + "\\" + Path.GetFileNameWithoutExtension(file);
                if (!Directory.Exists(outputTPFdir))
                    Directory.CreateDirectory(outputTPFdir);

                IntPtr handle = IntPtr.Zero;
                ZlibHelper.Zip zip = new ZlibHelper.Zip();
                try
                {
                    byte[] buffer = File.ReadAllBytes(file);
                    handle = zip.Open(buffer, ref numEntries, 1);
                    if (handle == IntPtr.Zero)
                        throw new Exception();

                    for (uint i = 0; i < numEntries; i++)
                    {
                        try
                        {
                            result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            string filename = Path.GetFileName(fileName).Trim();
                            if (Path.GetExtension(filename).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(filename).ToLowerInvariant() == ".log")
                            {
                                zip.GoToNextFile(handle);
                                continue;
                            }

                            byte[] data = new byte[dstLen];
                            result = zip.ReadCurrentFile(handle, data, dstLen);
                            if (result != 0)
                            {
                                throw new Exception();
                            }
                            using (FileStream fs = new FileStream(Path.Combine(outputTPFdir, filename), FileMode.CreateNew))
                            {
                                fs.WriteFromBuffer(data);
                            }
                            Console.WriteLine(Path.Combine(outputTPFdir, filename));
                        }
                        catch
                        {
                            Console.WriteLine("Skipping damaged content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file);
                            status = false;
                        }
                        zip.GoToNextFile(handle);
                    }
                    zip.Close(handle);
                    handle = IntPtr.Zero;
                }
                catch
                {
                    Console.WriteLine("TPF file is damaged: " + file);
                    if (handle != IntPtr.Zero)
                        zip.Close(handle);
                    handle = IntPtr.Zero;
                    continue;
                }
            }

            Console.WriteLine("Extract TPF files completed.");
            return status;
        }

        static public bool extractMOD(MeType gameId, string inputDir, string outputDir)
        {
            loadTexturesMap(gameId);

            Console.WriteLine("Extract MOD files started...");

            bool status = true;
            string[] files = null;
            ulong numEntries = 0;
            List<string> list = Directory.GetFiles(inputDir, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)).ToList();
            list.Sort();
            files = list.ToArray();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            foreach (string file in files)
            {
                Console.WriteLine("Extract MOD: " + file);
                string outputMODdir = outputDir + "\\" + Path.GetFileNameWithoutExtension(file);
                if (!Directory.Exists(outputMODdir))
                    Directory.CreateDirectory(outputMODdir);

                try
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        uint textureCrc;
                        int len = fs.ReadInt32();
                        string version = fs.ReadStringASCIINull();
                        if (version.Length < 5) // legacy .mod
                            fs.SeekBegin();
                        else
                        {
                            fs.SeekBegin();
                            len = fs.ReadInt32();
                            version = fs.ReadStringASCII(len); // version
                        }
                        numEntries = fs.ReadUInt32();
                        for (uint i = 0; i < numEntries; i++)
                        {
                            len = fs.ReadInt32();
                            string desc = fs.ReadStringASCII(len); // description
                            len = fs.ReadInt32();
                            string scriptLegacy = fs.ReadStringASCII(len);
                            if (desc.Contains("Binary Replacement"))
                            {
                                int exportId = -1;
                                string path = "";
                                string package = "";
                                try
                                {
                                    Misc.ParseME3xBinaryScriptMod(scriptLegacy, ref package, ref exportId, ref path);
                                    if (exportId == -1 || package == "" || path == "")
                                        throw new Exception();
                                }
                                catch
                                {
                                    len = fs.ReadInt32();
                                    fs.Skip(len);
                                    Console.WriteLine("Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file);
                                    status = false;
                                    continue;
                                }
                                path = Path.Combine(path, package);
                                len = fs.ReadInt32();
                                string newFilename;
                                if (path.Contains("\\DLC\\"))
                                {
                                    string dlcName = path.Split('\\')[3];
                                    newFilename = "D" + dlcName.Length + "-" + dlcName + "-";
                                }
                                else
                                {
                                    newFilename = "B";
                                }
                                newFilename += Path.GetFileName(path).Length + "-" + Path.GetFileName(path) + "-E" + exportId + ".bin";
                                if (File.Exists(Path.Combine(outputMODdir, newFilename)))
                                    File.Delete(Path.Combine(outputMODdir, newFilename));
                                using (FileStream fs2 = new FileStream(Path.Combine(outputMODdir, newFilename), FileMode.CreateNew))
                                {
                                    fs2.WriteFromStream(fs, len);
                                }
                                Console.WriteLine(Path.Combine(outputMODdir, newFilename));
                            }
                            else
                            {
                                string textureName = desc.Split(' ').Last();
                                FoundTexture f;
                                try
                                {
                                    f = Misc.ParseLegacyMe3xScriptMod(textures, scriptLegacy, textureName);
                                    textureCrc = f.crc;
                                    if (textureCrc == 0)
                                        throw new Exception();
                                }
                                catch
                                {
                                    len = fs.ReadInt32();
                                    fs.Skip(len);
                                    Console.WriteLine("Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file);
                                    status = false;
                                    continue;
                                }
                                len = fs.ReadInt32();
                                string newFile = textureName + string.Format("_0x{0:X8}", f.crc) + ".dds";
                                if (File.Exists(Path.Combine(outputMODdir, newFile)))
                                    File.Delete(Path.Combine(outputMODdir, newFile));
                                using (FileStream fs2 = new FileStream(Path.Combine(outputMODdir, newFile), FileMode.CreateNew))
                                {
                                    fs2.WriteFromStream(fs, len);
                                }
                                Console.WriteLine(Path.Combine(outputMODdir, newFile));
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("MOD is not compatible: " + file);
                    status = false;
                    continue;
                }
            }

            Console.WriteLine("Extract MOD files completed.");
            return status;
        }

        static public bool extractAllTextures(MeType gameId, string outputDir, bool png, string textureTfcFilter)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                    return false;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + gameId + "map.bin");
            if (!loadTexturesMapFile(mapFile))
                return false;

            Console.WriteLine("Extracting textures started...");

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            for (int i = 0; i < textures.Count; i++)
            {
                if (png)
                {
                    new MipMaps().extractTextureToPng(Path.Combine(outputDir, textures[i].name +
                        string.Format("_0x{0:X8}", textures[i].crc) + ".png"), GameData.GamePath +
                        textures[i].list[0].path, textures[i].list[0].exportID);
                }
                else
                {
                    string outputFile = Path.Combine(outputDir, textures[i].name +
                        string.Format("_0x{0:X8}", textures[i].crc) + ".dds");
                    string packagePath = GameData.GamePath + textures[i].list[0].path;
                    int exportID = textures[i].list[0].exportID;
                    Package package = new Package(packagePath);
                    Texture texture = new Texture(package, exportID, package.getExportData(exportID));
                    if (textureTfcFilter != "" && texture.properties.exists("TextureFileCacheName"))
                    {
                        string archive = texture.properties.getProperty("TextureFileCacheName").valueName;
                        if (archive != textureTfcFilter)
                            continue;
                    }
                    while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                    {
                        texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                    }
                    List<MipMap> mipmaps = new List<MipMap>();
                    PixelFormat pixelFormat = Image.getEngineFormatType(texture.properties.getProperty("Format").valueName);
                    for (int k = 0; k < texture.mipMapsList.Count; k++)
                    {
                        byte[] data = texture.getMipMapDataByIndex(k);
                        if (data == null)
                        {
                            continue;
                        }
                        mipmaps.Add(new MipMap(data, texture.mipMapsList[k].width, texture.mipMapsList[k].height, pixelFormat));
                    }
                    Image image = new Image(mipmaps, pixelFormat);
                    if (File.Exists(outputFile))
                        File.Delete(outputFile);
                    using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
                    {
                        image.StoreImageToDDS(fs);
                    }
                }
            }

            Console.WriteLine("Extracting textures completed.");
            return true;
        }

        public static bool ApplyModTag(MeType gameId, int meuitmV, int alotV)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            return Installer.applyModTag((int)gameId, meuitmV, alotV);
        }

        public static bool ApplyME1LAAPatch(MeType gameId)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            return Misc.VerifyME1Exe(gameData, false);
        }

        public static bool ApplyLODAndGfxSettings(MeType gameId, bool Limit2K = false)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.updateLOD((MeType)gameId, engineConf, Limit2K);
            LODSettings.updateGFXSettings(gameId, engineConf);

            return true;
        }

        public static bool RemoveLODSettings(MeType gameId, bool Limit2K = false)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            LODSettings.removeLOD(gameId, engineConf);

            return true;
        }

        public static bool PrintLODSettings(MeType gameId, bool Limit2K = false)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = gameData.EngineConfigIniPath;
            bool exist = File.Exists(path);
            if (!exist)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            ConfIni engineConf = new ConfIni(path);
            string log = "";
            LODSettings.readLOD(gameId, engineConf, ref log);
            Console.WriteLine(log);

            return true;
        }

        public static bool ScanAndMipMapsRemoval(MeType gameId, bool repack = false)
        {
            string errors = "";
            string log = "";

            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            gameData.getPackages(true, true);
            if (gameId != MeType.ME1_TYPE)
                gameData.getTfcTextures();

            TreeScan treeScan = new TreeScan();
            errors += treeScan.PrepareListOfTextures(null, null, null, null, ref log, true);
            textures = treeScan.treeScan;

            MipMaps mipMaps = new MipMaps();
            Console.WriteLine("Remove mipmaps started..." + Environment.NewLine);
            if (GameData.gameType == MeType.ME1_TYPE)
            {
                errors += mipMaps.removeMipMapsME1(1, textures, null, null, null, repack);
                errors += mipMaps.removeMipMapsME1(2, textures, null, null, null, repack);
            }
            else
            {
                errors += mipMaps.removeMipMapsME2ME3(textures, null, null, null, repack);
            }
            Console.WriteLine("Remove mipmaps finished" + Environment.NewLine + Environment.NewLine);

            return true;
        }

        public static bool UnpackDLCs(MeType gameId)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            ME3DLC.unpackAllDLC(null, null);

            return true;
        }

        public static bool RepackGameData(MeType gameId)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            gameData.getPackages(true, true);

            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                //updateStatusRepackZlib("Repacking PCC files... " + ((i + 1) * 100 / GameData.packageFiles.Count) + " %");
                Package package = new Package(GameData.packageFiles[i], true, true);
                if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                {
                    package.Dispose();
                    package = new Package(GameData.packageFiles[i]);
                    package.SaveToFile(true);
                }
            }

            return true;
        }

        public static bool VerifyGameDataEmptyMipMapsRemoval(MeType gameId)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            return MipMaps.verifyGameDataEmptyMipMapsRemoval();
        }

        public static List<string> getStandardDLCFolders()
        {
            List<string> foldernames = new List<string>();
            foldernames.Add("DLC_CON_MP1");
            foldernames.Add("DLC_CON_MP2");
            foldernames.Add("DLC_CON_MP3");
            foldernames.Add("DLC_CON_MP4");
            foldernames.Add("DLC_CON_MP5");
            foldernames.Add("DLC_UPD_Patch01");
            foldernames.Add("DLC_UPD_Patch02");
            foldernames.Add("DLC_HEN_PR");
            foldernames.Add("DLC_CON_END");
            foldernames.Add("DLC_EXP_Pack001");
            foldernames.Add("DLC_EXP_Pack002");
            foldernames.Add("DLC_EXP_Pack003");
            foldernames.Add("DLC_EXP_Pack003_Base");
            foldernames.Add("DLC_CON_APP01");
            foldernames.Add("DLC_CON_GUN01");
            foldernames.Add("DLC_CON_GUN02");
            foldernames.Add("DLC_CON_DH1");
            foldernames.Add("DLC_OnlinePassHidCE");
            foldernames.Add("__metadata"); //don't delete
            return foldernames;
        }

        public static bool CheckGameData(MeType gameId, bool wihtoutSfars, bool onlyVanilla, bool backupMode, bool ipc)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }
            string errors = "";
            List<string> modList = new List<string>();
            bool vanilla = Misc.checkGameFiles(gameId, ref errors, ref modList, null, null, ipc,
                wihtoutSfars, onlyVanilla, backupMode, Misc.generateModsMd5Entries);
            Console.WriteLine(errors);
            if (modList.Count != 0)
            {
                Console.WriteLine(Environment.NewLine + "------- Detected mods --------" + Environment.NewLine);
                for (int l = 0; l < modList.Count; l++)
                {
                    Console.WriteLine(modList[l] + Environment.NewLine);
                }
                Console.WriteLine("------------------------------" + Environment.NewLine + Environment.NewLine);
            }

            if (!vanilla)
            {
                Console.WriteLine("===========================================================================" + Environment.NewLine);
                Console.WriteLine("WARNING: looks like the following file(s) are not vanilla or not recognized" + Environment.NewLine);
                Console.WriteLine("===========================================================================" + Environment.NewLine + Environment.NewLine);
                Console.WriteLine(errors);
            }

            return vanilla;
        }

        public static bool DetectBadMods(MeType gameId)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            List<string> badMods = Misc.detectBrokenMod(gameId);
            if (badMods.Count != 0)
            {
                Console.WriteLine("Error: Detected not compatible mods: \n\n");
                for (int l = 0; l < badMods.Count; l++)
                {
                    Console.WriteLine(badMods[l] + Environment.NewLine);
                }
                return true;
            }

            return true;
        }

        public static bool InstallMEMs(MeType gameId, string inputDir, bool repack = false)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + gameId + "map.bin");
            if (!loadTexturesMapFile(mapFile))
                return false;

            gameData.getPackages(true, true);
            if (gameId != MeType.ME1_TYPE)
                gameData.getTfcTextures();

            List<string> memFiles = Directory.GetFiles(inputDir, "*.mem").Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
            bool status = applyMEMs(memFiles, repack);
            return status;
        }

        static public bool applyMEMSpecialModME3(string memFile, string tfcName, byte[] guid)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(MeType.ME3_TYPE, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + 3 + "map.bin");
            if (!loadTexturesMapFile(mapFile))
                return false;

            gameData.getPackages(true, true);
            gameData.getTfcTextures();

            List<string> memFiles = new List<string>();
            memFiles.Add(memFile);

            applyMEMs(memFiles, false, true, tfcName, guid);

            return true;
        }

        static public bool applyMEMs(List<string> memFiles, bool repack = false, bool special = false, string tfcName = "", byte[] guid = null)
        {
            bool status = true;
            CachePackageMgr cachePackageMgr = new CachePackageMgr(null, null);

            for (int i = 0; i < memFiles.Count; i++)
            {
                Console.WriteLine("Mod: " + (i + 1) + " of " + memFiles.Count + " started: " + Path.GetFileName(memFiles[i]) + Environment.NewLine);
                using (FileStream fs = new FileStream(memFiles[i], FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                    {
                        if (version != TexExplorer.TextureModVersion)
                        {
                            Console.WriteLine("File " + memFiles[i] + " was made with an older version of MEM, skipping..." + Environment.NewLine);
                        }
                        else
                        {
                            Console.WriteLine("File " + memFiles[i] + " is not a valid MEM mod, skipping..." + Environment.NewLine);
                        }
                        continue;
                    }
                    else
                    {
                        uint gameType = 0;
                        fs.JumpTo(fs.ReadInt64());
                        gameType = fs.ReadUInt32();
                        if ((MeType)gameType != GameData.gameType)
                        {
                            Console.WriteLine("File " + memFiles[i] + " is not a MEM mod valid for this game, skipping..." + Environment.NewLine);
                            continue;
                        }
                    }
                    int numFiles = fs.ReadInt32();
                    List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();
                    for (int k = 0; k < numFiles; k++)
                    {
                        MipMaps.FileMod fileMod = new MipMaps.FileMod();
                        fileMod.tag = fs.ReadUInt32();
                        fileMod.name = fs.ReadStringASCIINull();
                        fileMod.offset = fs.ReadInt64();
                        fileMod.size = fs.ReadInt64();
                        modFiles.Add(fileMod);
                    }
                    numFiles = modFiles.Count;
                    for (int l = 0; l < numFiles; l++)
                    {
                        string name = "";
                        uint crc = 0;
                        long size = 0, dstLen = 0;
                        int exportId = -1;
                        string pkgPath = "";
                        byte[] dst = null;
                        fs.JumpTo(modFiles[l].offset);
                        size = modFiles[l].size;
                        if (modFiles[l].tag == MipMaps.FileTextureTag)
                        {
                            name = fs.ReadStringASCIINull();
                            crc = fs.ReadUInt32();
                        }
                        else if (modFiles[l].tag == MipMaps.FileBinaryTag)
                        {
                            name = modFiles[l].name;
                            exportId = fs.ReadInt32();
                            pkgPath = fs.ReadStringASCIINull();
                        }

                        dst = MipMaps.decompressData(fs, size);
                        dstLen = dst.Length;

                        if (modFiles[l].tag == MipMaps.FileTextureTag)
                        {
                            FoundTexture foundTexture;
                            foundTexture = textures.Find(s => s.crc == crc);
                            if (foundTexture.crc != 0)
                            {
                                Image image = new Image(dst, Image.ImageFormat.DDS);
                                if (!image.checkDDSHaveAllMipmaps())
                                {
                                    Console.WriteLine("Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps" + Environment.NewLine);
                                    continue;
                                }
                                string errors = "";
                                if (special)
                                    errors = replaceTextureSpecialME3Mod(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, tfcName, guid);
                                else
                                    errors = new MipMaps().replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, false);
                                if (errors != "")
                                    Console.WriteLine(errors);
                            }
                            else
                            {
                                Console.WriteLine("Texture skipped. Texture " + name + string.Format("_0x{0:X8}", crc) + " is not present in your game setup" + Environment.NewLine);
                            }
                        }
                        else if (modFiles[l].tag == MipMaps.FileBinaryTag)
                        {
                            string path = GameData.GamePath + pkgPath;
                            if (!File.Exists(path))
                            {
                                Console.WriteLine("Warning: File " + path + " not exists in your game setup." + Environment.NewLine);
                                continue;
                            }
                            Package pkg = cachePackageMgr.OpenPackage(path);
                            pkg.setExportData(exportId, dst);
                        }
                        else
                        {
                            Console.WriteLine("Unknown tag for file: " + name + Environment.NewLine);
                        }
                    }
                }
            }

            cachePackageMgr.CloseAllWithSave(repack);

            return status;
        }

        static public string replaceTextureSpecialME3Mod(Image image, List<MatchedTexture> list, CachePackageMgr cachePackageMgr, string textureName, uint crc, string tfcName, byte[] guid)
        {
            Texture arcTexture = null, cprTexture = null;
            string errors = "";

            for (int n = 0; n < list.Count; n++)
            {
                MatchedTexture nodeTexture = list[n];
                Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                string fmt = texture.properties.getProperty("Format").valueName;
                PixelFormat pixelFormat = Image.getEngineFormatType(fmt);

                while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                {
                    texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                }

                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                    texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                {
                    errors += "Error in texture: " + textureName + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                    break;
                }

                if (!image.checkDDSHaveAllMipmaps() ||
                    (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1) ||
                    image.pixelFormat != pixelFormat)
                {
                    bool dxt1HasAlpha = false;
                    byte dxt1Threshold = 128;
                    if (pixelFormat == PixelFormat.DXT1 && texture.properties.exists("CompressionSettings"))
                    {
                        if (texture.properties.exists("CompressionSettings") &&
                            texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
                        {
                            dxt1HasAlpha = true;
                            if (image.pixelFormat == PixelFormat.ARGB ||
                                image.pixelFormat == PixelFormat.DXT3 ||
                                image.pixelFormat == PixelFormat.DXT5)
                            {
                                errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                            }
                        }
                    }
                    image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
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
                            MipMap mipmap = new MipMap(data, texture.mipMapsList[t].width, texture.mipMapsList[t].height, pixelFormat);
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
                    TFCTexture tfc = new TFCTexture
                    {
                        guid = guid,
                        name = tfcName
                    };

                    Array.Copy(texture.properties.getProperty("TFCFileGuid").valueStruct, origGuid, 16);
                    archiveFile = Path.Combine(Path.GetDirectoryName(GameData.GamePath + nodeTexture.path), tfc.name + ".tfc");
                    texture.properties.setNameValue("TextureFileCacheName", tfc.name);
                    texture.properties.setStructValue("TFCFileGuid", "Guid", tfc.guid);
                    if (!File.Exists(archiveFile))
                    {
                        using (FileStream fs = new FileStream(archiveFile, FileMode.CreateNew, FileAccess.Write))
                        {
                            fs.WriteFromBuffer(tfc.guid);
                        }
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
                        if (texture.mipMapsList.Count() > 1)
                        {
                            if (texture.properties.exists("TextureFileCacheName"))
                            {
                                if (texture.mipMapsList.Count < 6)
                                {
                                    mipmap.storageType = Texture.StorageTypes.pccUnc;
                                    if (!texture.properties.exists("NeverStream"))
                                    {
                                        if (package.existsNameId("NeverStream"))
                                            texture.properties.addBoolValue("NeverStream", true);
                                        else
                                            goto skip;
                                    }
                                }
                                else
                                {
                                    mipmap.storageType = Texture.StorageTypes.extZlib;
                                }
                            }
                        }
                    }

                    mipmap.uncompressedSize = image.mipMaps[m].data.Length;
                    if (mipmap.storageType == Texture.StorageTypes.extZlib ||
                        mipmap.storageType == Texture.StorageTypes.extLZO)
                    {
                        if (cprTexture == null || (cprTexture != null && mipmap.storageType != cprTexture.mipMapsList[m].storageType))
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

                if (triggerCacheCpr)
                    cprTexture = texture;
                if (triggerCacheArc)
                    arcTexture = texture;
                skip:
                package = null;
            }
            arcTexture = cprTexture = null;

            return errors;
        }
    }
}
