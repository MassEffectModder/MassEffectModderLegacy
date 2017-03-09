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

using AmaroK86.ImageFormat;
using StreamHelpers;
using System;
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

        static private bool loadTexturesMap(string path, List<FoundTexture> textures)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Texture map not exist: " + path);
                return false;
            }
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != TexExplorer.textureMapBinTag || version != TexExplorer.textureMapBinVersion)
                {
                    Console.WriteLine("Texture map not compatible: " + path);
                    return false;
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
            }

            return true;
        }

        static private string convertDataModtoMem(string inputDir, string memFilePath)
        {
            string errors = "";
            string[] files = null;

            Console.WriteLine("Mods conversion started...");

            List<string> list = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
            list.AddRange(Directory.GetFiles(inputDir, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.dds").Where(item => item.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)));
            list.Sort();
            files = list.ToArray();

            int result;
            string fileName = "";
            uint dstLen = 0;
            string[] ddsList = null;
            ulong numEntries = 0;
            List<TexExplorer.BinaryMod> mods = new List<TexExplorer.BinaryMod>();
            foreach (string file in files)
            {
                if (file.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            string package = "";
                            int len = fs.ReadInt32();
                            string version = fs.ReadStringASCII(len); // version
                            if (version.Length < 5) // legacy .mod
                                fs.SeekBegin();
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
                                        continue;
                                    }
                                    mod.packagePath = GameData.RelativeGameData(Path.Combine(path, package));
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
                                        continue;
                                    }
                                    textureName = f.name;
                                    mod.textureName = textureName;
                                    mod.binaryMod = false;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);
                                    DDSImage image = new DDSImage(new MemoryStream(mod.data));
                                    if (!image.checkExistAllMipmaps())
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has not all the required mipmaps, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }
                                    Package pkg = new Package(GameData.GamePath + f.list[0].path);
                                    Texture texture = new Texture(pkg, f.list[0].exportID, pkg.getExportData(f.list[0].exportID));

                                    if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture must have mipmaps, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }

                                    string fmt = texture.properties.getProperty("Format").valueName;
                                    DDSFormat ddsFormat = DDSImage.convertFormat(fmt);
                                    if (image.ddsFormat != ddsFormat)
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong texture format, should be: " + ddsFormat + ", skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }

                                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                        texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }
                                }
                                mods.Add(mod);
                            }
                        }
                    }
                    catch
                    {
                        errors += "Mod is not compatible: " + file + Environment.NewLine;
                        continue;
                    }
                }
                else if (file.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
                {
                    IntPtr handle = IntPtr.Zero;
                    try
                    {
                        byte[] buffer = File.ReadAllBytes(file);
                        handle = ZlibHelper.Zip.Open(buffer, ref numEntries, 1);
                        if (ZlibHelper.Zip.LocateFile(handle, "texmod.def") != 0)
                            throw new Exception();
                        result = ZlibHelper.Zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                        if (result != 0)
                            throw new Exception();
                        byte[] listText = new byte[dstLen];
                        result = ZlibHelper.Zip.ReadCurrentFile(handle, listText, dstLen);
                        if (result != 0)
                            throw new Exception();
                        ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                        result = ZlibHelper.Zip.GoToFirstFile(handle);
                        if (result != 0)
                            throw new Exception();

                        for (uint i = 0; i < numEntries; i++)
                        {
                            TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                            try
                            {
                                uint crc = 0;
                                result = ZlibHelper.Zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                                if (result != 0)
                                    throw new Exception();
                                string filename = Path.GetFileName(fileName);
                                foreach (string dds in ddsList)
                                {
                                    string ddsFile = dds.Split('|')[1];
                                    if (ddsFile.ToLower() != filename.ToLower())
                                        continue;
                                    crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }
                                if (crc == 0)
                                {
                                    if (filename != "texmod.def")
                                        errors += "Skipping file: " + filename + " not founded in texmod.def, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }

                                List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                                if (foundCrcList.Count == 0)
                                {
                                    errors += "Texture skipped. File " + filename + string.Format(" - 0x{0:X8}", crc) + " is not present in your game setup - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }

                                string textureName = foundCrcList[0].name;
                                mod.textureName = textureName;
                                mod.binaryMod = false;
                                mod.textureCrc = crc;
                                mod.data = new byte[dstLen];
                                result = ZlibHelper.Zip.ReadCurrentFile(handle, mod.data, dstLen);
                                if (result != 0)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }
                                uint tag = BitConverter.ToUInt32(mod.data, 0);
                                if (tag != 0x20534444) // DDS
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture is not in DDS format, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }
                                DDSImage image = new DDSImage(new MemoryStream(mod.data));
                                if (!image.checkExistAllMipmaps())
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has not all the required mipmaps, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }
                                Package pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
                                Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));

                                if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture must have mipmaps, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }

                                string fmt = texture.properties.getProperty("Format").valueName;
                                DDSFormat ddsFormat = DDSImage.convertFormat(fmt);
                                if (image.ddsFormat != ddsFormat)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong texture format, should be: " + ddsFormat + ", skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }

                                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                    texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }
                                mods.Add(mod);
                            }
                            catch
                            {
                                errors += "Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                            }
                            ZlibHelper.Zip.GoToNextFile(handle);
                        }
                        ZlibHelper.Zip.Close(handle);
                        handle = IntPtr.Zero;
                    }
                    catch
                    {
                        errors += "Mod is not compatible: " + file + Environment.NewLine;
                        if (handle != IntPtr.Zero)
                            ZlibHelper.Zip.Close(handle);
                        handle = IntPtr.Zero;
                        continue;
                    }
                }
                else if (file.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    TexExplorer.BinaryMod mod = new TexExplorer.BinaryMod();
                    string filename = Path.GetFileNameWithoutExtension(file).ToLower();
                    if (!filename.Contains("_0x"))
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (_0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        continue;
                    }
                    int idx = filename.IndexOf("_0x");
                    if (filename.Length - idx < 11)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (_0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        continue;
                    }
                    uint crc;
                    string crcStr = filename.Substring(idx + 3, 8);
                    try
                    {
                        crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (_0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        errors += "Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup." + Environment.NewLine;
                        continue;
                    }

                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        mod.data = fs.ReadToBuffer((int)fs.Length);
                        DDSImage image = new DDSImage(new MemoryStream(mod.data));
                        if (!image.checkExistAllMipmaps())
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has not all the required mipmaps, skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        Package pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
                        Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));

                        if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture must have mipmaps, skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        string fmt = texture.properties.getProperty("Format").valueName;
                        DDSFormat ddsFormat = DDSImage.convertFormat(fmt);
                        if (image.ddsFormat != ddsFormat)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong texture format, should be: " + ddsFormat + ", skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                            texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        mod.textureName = filename.Substring(0, filename.IndexOf(crcStr) - "_0x".Length);
                        mod.binaryMod = false;
                        mod.textureCrc = crc;
                        mods.Add(mod);
                    }
                }
            }

            if (mods.Count == 0)
            {
                Console.WriteLine("Mods conversion failed");
                return errors;
            }

            Console.WriteLine("Saving to MEM file... " + memFilePath);
            List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();
            FileStream outFs;
            if (File.Exists(memFilePath))
                File.Delete(memFilePath);

            using (outFs = new FileStream(memFilePath, FileMode.Create, FileAccess.Write))
            {
                outFs.WriteUInt32(TexExplorer.TextureModTag);
                outFs.WriteUInt32(TexExplorer.TextureModVersion);
                outFs.WriteInt64(0); // filled later

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
                        fileMod.name = Path.GetFileNameWithoutExtension(mods[l].packagePath) + "_" + mods[l].exportId + ".bin";

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

                long pos = outFs.Position;
                outFs.SeekBegin();
                outFs.WriteUInt32(TexExplorer.TextureModTag);
                outFs.WriteUInt32(TexExplorer.TextureModVersion);
                outFs.WriteInt64(pos);
                outFs.JumpTo(pos);
                outFs.WriteUInt32((uint)GameData.gameType);
                outFs.WriteInt32(modFiles.Count);
                for (int i = 0; i < modFiles.Count; i++)
                {
                    outFs.WriteUInt32(modFiles[i].tag);
                    outFs.WriteStringASCIINull(modFiles[i].name);
                    outFs.WriteInt64(modFiles[i].offset);
                    outFs.WriteInt64(modFiles[i].size);
                }
            }

            Console.WriteLine("Mods conversion process completed");
            return errors;
        }

        static public bool ConvertToMEM(int gameId, string inputDir, string memFile)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData((MeType)gameId, configIni);

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + gameId + "map.bin");
            if (!loadTexturesMap(mapFile, textures))
                return false;

            string errors = convertDataModtoMem(inputDir, memFile);
            string filename = "convert-errors.txt";
            if (File.Exists(filename))
                File.Delete(filename);
            if (errors != "")
            {
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                Console.WriteLine("Error: Some errors have occured, check log file: " + filename);
                return false;
            }

            return true;
        }
    }
}
