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

            List<string> list = Directory.GetFiles(inputDir, "*.mem").Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
            list.Sort();
            List<string> list2 = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
            list2.AddRange(Directory.GetFiles(inputDir, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)));
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
            uint dstLen = 0;
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

            foreach (string file in files)
            {
                if (file.EndsWith(".mem", StringComparison.OrdinalIgnoreCase))
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                        {
                            if (version != TexExplorer.TextureModVersion)
                                errors += "File " + file + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                            else
                                errors += "File " + file + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                            return errors;
                        }
                        else
                        {
                            uint gameType = 0;
                            fs.JumpTo(fs.ReadInt64());
                            gameType = fs.ReadUInt32();
                            if ((MeType)gameType != GameData.gameType)
                            {
                                errors += "File " + file + " is not a MEM mod valid for this game" + Environment.NewLine;
                                return errors;
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
                                    Package pkg = null;
                                    try
                                    {
                                        pkg = new Package(GameData.GamePath + f.list[0].path);
                                    }
                                    catch
                                    {
                                        errors += "Missing package file: " + f.list[0].path + " - Skipping, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }
                                    Texture texture = new Texture(pkg, f.list[0].exportID, pkg.getExportData(f.list[0].exportID));
                                    string fmt = texture.properties.getProperty("Format").valueName;
                                    PixelFormat pixelFormat = Image.getEngineFormatType(fmt);
                                    Image image = new Image(mod.data, Image.ImageFormat.DDS);
                                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                        texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }

                                    if (!image.checkDDSHaveAllMipmaps() ||
                                        (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1) ||
                                        image.pixelFormat != pixelFormat)
                                    {
                                        bool dxt1HasAlpha = false;
                                        byte dxt1Threshold = 128;
                                        if (texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
                                        {
                                            dxt1HasAlpha = true;
                                            if (image.pixelFormat == PixelFormat.ARGB ||
                                                image.pixelFormat == PixelFormat.DXT3 ||
                                                image.pixelFormat == PixelFormat.DXT5)
                                            {
                                                errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
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
                        for (ulong i = 0; i < numEntries; i++)
                        {
                            result = ZlibHelper.Zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            if (Path.GetExtension(fileName).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(fileName).ToLowerInvariant() == ".log")
                            {
                                break;
                            }
                            ZlibHelper.Zip.GoToNextFile(handle);
                        }
                        if (Path.GetExtension(fileName).ToLowerInvariant() != ".def" &&
                            Path.GetExtension(fileName).ToLowerInvariant() != ".log")
                        {
                            throw new Exception();
                        }
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
                                    if (ddsFile.ToLowerInvariant() != filename.ToLowerInvariant())
                                        continue;
                                    crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }
                                if (crc == 0)
                                {
                                    if (Path.GetExtension(filename).ToLowerInvariant() != ".def" &&
                                        Path.GetExtension(filename).ToLowerInvariant() != ".log")
                                        errors += "Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
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

                                Package pkg = null;
                                try
                                {
                                    pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
                                }
                                catch
                                {
                                    errors += "Missing package file: " + foundCrcList[0].list[0].path + " - Skipping, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                                    continue;
                                }
                                Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));
                                string fmt = texture.properties.getProperty("Format").valueName;
                                PixelFormat pixelFormat = Image.getEngineFormatType(fmt);

                                Image image = new Image(mod.data, Path.GetExtension(filename));
                                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                    texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    ZlibHelper.Zip.GoToNextFile(handle);
                                    continue;
                                }

                                if (!image.checkDDSHaveAllMipmaps() ||
                                    (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1) ||
                                    image.pixelFormat != pixelFormat)
                                {
                                    bool dxt1HasAlpha = false;
                                    byte dxt1Threshold = 128;
                                    if (texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
                                    {
                                        dxt1HasAlpha = true;
                                        if (image.pixelFormat == PixelFormat.ARGB ||
                                            image.pixelFormat == PixelFormat.DXT3 ||
                                            image.pixelFormat == PixelFormat.DXT5)
                                        {
                                            errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
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
                    string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!filename.Contains("0x"))
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
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
                        Package pkg = null;
                        try
                        {
                            pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
                        }
                        catch
                        {
                            errors += "Missing package file: " + foundCrcList[0].list[0].path + " - Skipping, file: " + Path.GetFileName(file) + Environment.NewLine;
                            continue;
                        }
                        Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));
                        string fmt = texture.properties.getProperty("Format").valueName;
                        PixelFormat pixelFormat = Image.getEngineFormatType(fmt);
                        mod.data = fs.ReadToBuffer((int)fs.Length);
                        Image image = new Image(mod.data, Image.ImageFormat.DDS);

                        if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                            texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        if (!image.checkDDSHaveAllMipmaps() ||
                            (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1) ||
                            image.pixelFormat != pixelFormat)
                        {
                            bool dxt1HasAlpha = false;
                            byte dxt1Threshold = 128;
                            if (texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
                            {
                                dxt1HasAlpha = true;
                                if (image.pixelFormat == PixelFormat.ARGB ||
                                    image.pixelFormat == PixelFormat.DXT3 ||
                                    image.pixelFormat == PixelFormat.DXT5)
                                {
                                    errors += "Warning for texture: " + Path.GetFileName(file) + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
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
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
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
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        errors += "Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup." + Environment.NewLine;
                        continue;
                    }

                    Package pkg = null;
                    try
                    {
                        pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
                    }
                    catch
                    {
                        errors += "Missing package file: " + foundCrcList[0].list[0].path + " - Skipping, file: " + Path.GetFileName(file) + Environment.NewLine;
                        continue;
                    }
                    Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));
                    string fmt = texture.properties.getProperty("Format").valueName;
                    PixelFormat pixelFormat = Image.getEngineFormatType(fmt);

                    Image image = new Image(file, Image.ImageFormat.Unknown).convertToARGB();
                    if (image.mipMaps[0].width / image.mipMaps[0].height !=
                        texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                    {
                        errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                        continue;
                    }

                    bool dxt1HasAlpha = false;
                    byte dxt1Threshold = 128;
                    if (texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
                    {
                        dxt1HasAlpha = true;
                        if (image.pixelFormat == PixelFormat.ARGB ||
                            image.pixelFormat == PixelFormat.DXT3 ||
                            image.pixelFormat == PixelFormat.DXT5)
                        {
                            errors += "Warning for texture: " + Path.GetFileName(file) + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                        }
                    }
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
                mods.Clear();
            }

            if (modFiles.Count == 0)
            {
                outFs.Close();
                if (File.Exists(memFilePath))
                    File.Delete(memFilePath);
                Console.WriteLine("Mods conversion failed - nothing converted!");
                return errors;
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

            outFs.Close();
            Console.WriteLine("Mods conversion process completed");
            return errors;
        }

        static public bool ConvertToMEM(int gameId, string inputDir, string memFile)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData((MeType)gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

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

        static public string convertGameTexture(string inputFile, string outputFile)
        {
            string errors = "";
            string filename = Path.GetFileNameWithoutExtension(inputFile).ToLowerInvariant();
            if (!filename.Contains("0x"))
            {
                errors += "Texture filename not valid: " + Path.GetFileName(inputFile) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                return errors;
            }
            int idx = filename.IndexOf("0x");
            if (filename.Length - idx < 10)
            {
                errors += "Texture filename not valid: " + Path.GetFileName(inputFile) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                return errors;
            }
            uint crc;
            string crcStr = filename.Substring(idx + 2, 8);
            try
            {
                crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                errors += "Texture filename not valid: " + Path.GetFileName(inputFile) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                return errors;
            }

            List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
            if (foundCrcList.Count == 0)
            {
                errors += "Texture skipped. Texture " + Path.GetFileName(inputFile) + " is not present in your game setup." + Environment.NewLine;
                return errors;
            }

            Package pkg = null;
            try
            {
                pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
            }
            catch
            {
                errors += "Missing package file: " + foundCrcList[0].list[0].path + " - Skipping, file: " + Path.GetFileName(inputFile) + Environment.NewLine;
                return errors;
            }
            Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));
            string fmt = texture.properties.getProperty("Format").valueName;
            PixelFormat pixelFormat = Image.getEngineFormatType(fmt);
            Image image = new Image(inputFile);

            if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                texture.mipMapsList[0].width / texture.mipMapsList[0].height)
            {
                errors += "Error in texture: " + Path.GetFileName(inputFile) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                return errors;
            }

            bool dxt1HasAlpha = false;
            byte dxt1Threshold = 128;
            if (texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
            {
                dxt1HasAlpha = true;
                if (image.pixelFormat == PixelFormat.ARGB ||
                    image.pixelFormat == PixelFormat.DXT3 ||
                    image.pixelFormat == PixelFormat.DXT5)
                {
                    errors += "Warning for texture: " + Path.GetFileName(inputFile) + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                }
            }
            image.correctMips(pixelFormat, dxt1HasAlpha, dxt1Threshold);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                fs.WriteFromBuffer(image.StoreImageToDDS());
            }

            return errors;
        }

        static public bool convertGameImage(int gameId, string inputFile, string outputFile)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData((MeType)gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + gameId + "map.bin");
            if (!loadTexturesMap(mapFile, textures))
                return false;

            string errors = convertGameTexture(inputFile, outputFile);

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

        static public bool convertGameImages(int gameId, string inputDir, string outputDir)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData((MeType)gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + gameId + "map.bin");
            if (!loadTexturesMap(mapFile, textures))
                return false;

            List<string> list = Directory.GetFiles(inputDir, "*.dds").Where(item => item.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)).ToList();
            list.AddRange(Directory.GetFiles(inputDir, "*.png").Where(item => item.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.bmp").Where(item => item.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.tga").Where(item => item.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.jpg").Where(item => item.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)));
            list.AddRange(Directory.GetFiles(inputDir, "*.jpeg").Where(item => item.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)));
            list.Sort();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string errors = "";
            foreach (string file in list)
            {
                errors += convertGameTexture(file, Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".dds"));
            }

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

            string errors = "";
            string[] files = null;
            int result;
            string fileName = "";
            uint dstLen = 0;
            string[] ddsList = null;
            ulong numEntries = 0;
            List<string> list = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
            list.Sort();
            files = list.ToArray();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            foreach (string file in files)
            {
                string outputTPFdir = outputDir + "\\" + Path.GetFileNameWithoutExtension(file);
                if (!Directory.Exists(outputTPFdir))
                    Directory.CreateDirectory(outputTPFdir);

                IntPtr handle = IntPtr.Zero;
                try
                {
                    byte[] buffer = File.ReadAllBytes(file);
                    handle = ZlibHelper.Zip.Open(buffer, ref numEntries, 1);
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
                        try
                        {
                            result = ZlibHelper.Zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            string filename = Path.GetFileName(fileName);
                            if (Path.GetExtension(filename).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(filename).ToLowerInvariant() == ".log")
                            {
                                ZlibHelper.Zip.GoToNextFile(handle);
                                continue;
                            }

                            byte[] data = new byte[dstLen];
                            result = ZlibHelper.Zip.ReadCurrentFile(handle, data, dstLen);
                            if (result != 0)
                            {
                                throw new Exception();
                            }
                            using (FileStream fs = new FileStream(Path.Combine(outputTPFdir, filename), FileMode.CreateNew))
                            {
                                fs.WriteFromBuffer(data);
                            }
                        }
                        catch
                        {
                            errors += "Skipping damaged content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                        }
                        ZlibHelper.Zip.GoToNextFile(handle);
                    }
                    ZlibHelper.Zip.Close(handle);
                    handle = IntPtr.Zero;
                }
                catch
                {
                    errors += "TPF file is damaged: " + file + Environment.NewLine;
                    if (handle != IntPtr.Zero)
                        ZlibHelper.Zip.Close(handle);
                    handle = IntPtr.Zero;
                    continue;
                }
            }

            Console.WriteLine("Extract TPF files completed.");
            return true;
        }

        static public bool extractMOD(int gameId, string inputDir, string outputDir)
        {
            textures = new List<FoundTexture>();
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData((MeType)gameId, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            string mapFile = Path.Combine(path, "me" + gameId + "map.bin");
            if (!loadTexturesMap(mapFile, textures))
                return false;

            Console.WriteLine("Extract MOD files started...");

            string errors = "";
            string[] files = null;
            ulong numEntries = 0;
            List<string> list = Directory.GetFiles(inputDir, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)).ToList();
            list.Sort();
            files = list.ToArray();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            foreach (string file in files)
            {
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
                                len = fs.ReadInt32();
                                fs.Skip(len);
                                errors += "Skipping non texture entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
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
                                    errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    continue;
                                }
                                len = fs.ReadInt32();
                                using (FileStream fs2 = new FileStream(Path.Combine(outputMODdir, textureName) + ".dds", FileMode.CreateNew))
                                {
                                    fs2.WriteFromStream(fs, len);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    errors += "Mod is not compatible: " + file + Environment.NewLine;
                    continue;
                }
            }

            Console.WriteLine("Extract MOD files completed.");
            return true;
        }
    }
}
