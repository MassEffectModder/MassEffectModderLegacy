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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace MassEffectModder
{
    public partial class MipMaps
    {
        const uint maxBlockSize = 0x20000; // 128KB
        const int SizeOfChunkBlock = 8;
        const int SizeOfChunk = 8;
        public const uint FileTextureTag = 0x53444446;
        public const uint FileTextureTag2 = 0x53444443;
        public const uint FileBinaryTag = 0x4E494246;
        public const uint FileXdeltaTag = 0x4E494258;

        public struct FileMod
        {
            public uint tag;
            public string name;
            public long offset;
            public long size;
        }

        public string extractTextureMod(string filenameMod, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, -1, true, false, false, false, outDir, textures, cachePackageMgr, texExplorer, ref log);
        }

        public string previewTextureMod(string filenameMod, int previewIndex, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, previewIndex, false, false, false, false, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string replaceTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, bool verify, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, true, false, verify, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string newReplaceTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, bool verify, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, false, true, verify, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string listTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, false, false, false, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        private string processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, bool newReplace, bool verify, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            string errors = "";

            if (filenameMod.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
            {
                if (!replace && !extract && !newReplace)
                    throw new Exception();

                int result;
                string fileName = "";
                ulong dstLen = 0;
                string[] ddsList = null;
                ulong numEntries = 0;
                IntPtr handle = IntPtr.Zero;
                ZlibHelper.Zip zip = new ZlibHelper.Zip();
                try
                {
                    int indexTpf = -1;
                    handle = zip.Open(filenameMod, ref numEntries, 1);
                    for (ulong i = 0; i < numEntries; i++)
                    {
                        result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                        fileName = fileName.Trim();
                        if (result != 0)
                            throw new Exception();
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
                        try
                        {
                            uint crc = 0;
                            result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            fileName = fileName.Trim();
                            foreach (string dds in ddsList)
                            {
                                string ddsFile = dds.Split('|')[1];
                                if (ddsFile.ToLowerInvariant().Trim() != fileName.ToLowerInvariant())
                                    continue;
                                crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                break;
                            }
                            string filename = Path.GetFileName(fileName);
                            if (crc == 0)
                            {
                                log += "Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + filenameMod + Environment.NewLine;
                                errors += "Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + filenameMod + Environment.NewLine;
                                zip.GoToNextFile(handle);
                                continue;
                            }

                            int index = -1;
                            for (int t = 0; t < textures.Count; t++)
                            {
                                if (textures[t].crc == crc)
                                {
                                    index = t;
                                    break;
                                }
                            }
                            if (index != -1)
                            {
                                FoundTexture foundTexture = textures[index];
                                byte[] data = new byte[dstLen];
                                result = zip.ReadCurrentFile(handle, data, dstLen);
                                if (result != 0)
                                {
                                    log += "Error in texture: " + foundTexture.name + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + filenameMod + Environment.NewLine;
                                    errors += "Error in texture: " + foundTexture.name + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + filenameMod + Environment.NewLine;
                                    zip.GoToNextFile(handle);
                                    continue;
                                }
                                if (texExplorer != null)
                                {
                                    texExplorer._mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileName(filenameMod) +
                                        " - File " + (i + 1) + " of " + numEntries + " - " + foundTexture.name);
                                }
                                if (extract)
                                {
                                    string name = foundTexture.name + "_" + string.Format("0x{0:X8}", crc) + Path.GetExtension(fileName);
                                    using (FileStream output = new FileStream(Path.Combine(outDir, name), FileMode.Create, FileAccess.Write))
                                    {
                                        output.Write(data, 0, (int)dstLen);
                                    }
                                }
                                else if (replace)
                                {
                                    Image image = new Image(data, Path.GetExtension(filename));
                                    errors += replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, verify, false);
                                    textures[index] = foundTexture;
                                }
                                else
                                {
                                    // TODO
                                }
                            }
                            else
                            {
                                log += "Texture skipped. File " + filename + string.Format(" - 0x{0:X8}", crc) + " is not present in your game setup - mod: " + filenameMod + Environment.NewLine;
                                zip.GoToNextFile(handle);
                                continue;
                            }
                        }
                        catch
                        {
                            log += "Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + filenameMod + Environment.NewLine;
                            errors += "Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + filenameMod + Environment.NewLine;
                        }
                        result = zip.GoToNextFile(handle);
                    }
                    zip.Close(handle);
                    handle = IntPtr.Zero;
                }
                catch
                {
                    log += "Mod is not compatible: " + filenameMod + Environment.NewLine;
                    errors += "Mod is not compatible: " + filenameMod + Environment.NewLine;
                    if (handle != IntPtr.Zero)
                        zip.Close(handle);
                    handle = IntPtr.Zero;
                }
                return errors;
            }
            else if (filenameMod.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
            {
                if (!replace && !extract && !newReplace)
                    throw new Exception();

                try
                {
                    using (FileStream fs = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
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
                        uint numEntries = fs.ReadUInt32();
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
                                    errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + filenameMod + Environment.NewLine;
                                    continue;
                                }
                                mod.packagePath = Path.Combine(GameData.GamePath + path, package);
                                len = fs.ReadInt32();
                                mod.data = fs.ReadToBuffer(len);

                                if (!File.Exists(mod.packagePath))
                                {
                                    errors += "Warning: File " + mod.packagePath + " not exists in your game setup." + Environment.NewLine;
                                    log += "Warning: File " + mod.packagePath + " not exists in your game setup." + Environment.NewLine;
                                    continue;
                                }
                                if (replace)
                                {
                                    Package pkg = cachePackageMgr.OpenPackage(mod.packagePath);
                                    pkg.setExportData(mod.exportId, mod.data);
                                }
                                else
                                {
                                    // TODO
                                }
                            }
                            else
                            {
                                string textureName = desc.Split(' ').Last();
                                FoundTexture f;
                                int index = -1;
                                try
                                {
                                    index = Misc.ParseLegacyMe3xScriptMod(textures, scriptLegacy, textureName);
                                    if (index == -1)
                                        throw new Exception();
                                    f = textures[index];
                                }
                                catch
                                {
                                    len = fs.ReadInt32();
                                    fs.Skip(len);
                                    errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + filenameMod + Environment.NewLine;
                                    continue;
                                }
                                mod.textureCrc = textures[index].crc;
                                mod.textureName = f.name;
                                len = fs.ReadInt32();
                                mod.data = fs.ReadToBuffer(len);

                                if (replace)
                                {
                                    PixelFormat pixelFormat = f.pixfmt;
                                    Image image = new Image(mod.data, Image.ImageFormat.DDS);
                                    errors += replaceTexture(image, f.list, cachePackageMgr, f.name, f.crc, verify, false);
                                    textures[index] = f;
                                }
                                else
                                {
                                    // TODO
                                }
                            }
                        }
                    }
                }
                catch
                {
                    errors += "Mod is not compatible: " + filenameMod + Environment.NewLine;
                }
                return errors;
            }

            using (FileStream fs = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
            {
                if (previewIndex == -1 && !extract && !replace && !newReplace)
                {
                    texExplorer.listViewTextures.BeginUpdate();
                }
                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                {
                    if (version != TexExplorer.TextureModVersion)
                    {
                        errors += "File " + filenameMod + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                        log += "File " + filenameMod + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                    }
                    else
                    {
                        errors += "File " + filenameMod + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                        log += "File " + filenameMod + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                    }
                    if (previewIndex == -1 && !extract && !replace)
                        texExplorer.listViewTextures.EndUpdate();
                    return errors;
                }
                else
                {
                    uint gameType = 0;
                    fs.JumpTo(fs.ReadInt64());
                    gameType = fs.ReadUInt32();
                    if (textures != null && (MeType)gameType != GameData.gameType)
                    {
                        errors += "File " + filenameMod + " is not a MEM mod valid for this game" + Environment.NewLine;
                        log += "File " + filenameMod + " is not a MEM mod valid for this game" + Environment.NewLine;
                        if (previewIndex == -1 && !extract && !replace)
                            texExplorer.listViewTextures.EndUpdate();
                        return errors;
                    }
                }
                int numFiles = fs.ReadInt32();
                List<FileMod> modFiles = new List<FileMod>();
                for (int i = 0; i < numFiles; i++)
                {
                    FileMod fileMod = new FileMod();
                    fileMod.tag = fs.ReadUInt32();
                    fileMod.name = fs.ReadStringASCIINull();
                    fileMod.offset = fs.ReadInt64();
                    fileMod.size = fs.ReadInt64();
                    modFiles.Add(fileMod);
                }
                numFiles = modFiles.Count;

                for (int i = 0; i < numFiles; i++)
                {
                    string name = "";
                    uint crc = 0;
                    long size = 0, dstLen = 0;
                    int exportId = -1;
                    string pkgPath = "";
                    byte[] dst = null;
                    if (previewIndex != -1)
                        i = previewIndex;
                    fs.JumpTo(modFiles[i].offset);
                    size = modFiles[i].size;
                    if (modFiles[i].tag == FileTextureTag || modFiles[i].tag == FileTextureTag2)
                    {
                        name = fs.ReadStringASCIINull();
                        crc = fs.ReadUInt32();
                    }
                    else if (modFiles[i].tag == FileBinaryTag)
                    {
                        name = modFiles[i].name;
                        exportId = fs.ReadInt32();
                        pkgPath = fs.ReadStringASCIINull();
                    }
                    else if (modFiles[i].tag == FileXdeltaTag)
                    {
                        name = modFiles[i].name;
                        exportId = fs.ReadInt32();
                        pkgPath = fs.ReadStringASCIINull();
                    }

                    if (texExplorer != null && (extract || replace || newReplace))
                    {
                        texExplorer._mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileName(filenameMod) +
                            " - File " + (i + 1) + " of " + numFiles + " - " + name);
                    }

                    if (previewIndex == -1 && !extract && !replace && !newReplace)
                    {
                        if (modFiles[i].tag == FileTextureTag || modFiles[i].tag == FileTextureTag2)
                        {
                            FoundTexture foundTexture;
                            foundTexture = textures.Find(s => s.crc == crc);
                            if (foundTexture.crc != 0)
                            {
                                ListViewItem item = new ListViewItem(foundTexture.name + " (" + Path.GetFileNameWithoutExtension(foundTexture.list.Find(s => s.path != "").path).ToUpperInvariant() + ")");
                                item.Name = i.ToString();
                                texExplorer.listViewTextures.Items.Add(item);
                            }
                            else
                            {
                                ListViewItem item = new ListViewItem(name + " (Texture not found: " + name + string.Format("_0x{0:X8}", crc) + ")");
                                item.Name = i.ToString();
                                texExplorer.listViewTextures.Items.Add(item);
                                log += "Texture skipped. Texture " + name + string.Format("_0x{0:X8}", crc) + " is not present in your game setup" + Environment.NewLine;
                            }
                        }
                        else if (modFiles[i].tag == FileBinaryTag)
                        {
                            ListViewItem item = new ListViewItem(name + " (Raw Binary Mod)");
                            item.Name = i.ToString();
                            texExplorer.listViewTextures.Items.Add(item);
                        }
                        else if (modFiles[i].tag == FileXdeltaTag)
                        {
                            ListViewItem item = new ListViewItem(name + " (Xdelta Binary Mod)");
                            item.Name = i.ToString();
                            texExplorer.listViewTextures.Items.Add(item);
                        }
                        else
                        {
                            ListViewItem item = new ListViewItem(name + " (Unknown)");
                            item.Name = i.ToString();
                            errors += "Unknown tag for file: " + name + Environment.NewLine;
                            log += "Unknown tag for file: " + name + Environment.NewLine;
                        }
                        continue;
                    }

                    dst = decompressData(fs, size);
                    dstLen = dst.Length;

                    if (extract)
                    {
                        if (modFiles[i].tag == FileTextureTag)
                        {
                            string filename = name + "_" + string.Format("0x{0:X8}", crc) + ".dds";
                            using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                            {
                                output.Write(dst, 0, (int)dstLen);
                            }
                        }
                        else if (modFiles[i].tag == FileTextureTag2)
                        {
                            string filename = name + "_" + string.Format("0x{0:X8}", crc) + "-memconvert.dds";
                            using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                            {
                                output.Write(dst, 0, (int)dstLen);
                            }
                        }
                        else if (modFiles[i].tag == FileBinaryTag)
                        {
                            string path = pkgPath;
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
                            using (FileStream output = new FileStream(Path.Combine(outDir, newFilename), FileMode.Create, FileAccess.Write))
                            {
                                output.Write(dst, 0, (int)dstLen);
                            }
                        }
                        else if (modFiles[i].tag == FileXdeltaTag)
                        {
                            string path = pkgPath;
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
                            newFilename += Path.GetFileName(path).Length + "-" + Path.GetFileName(path) + "-E" + exportId + ".xdelta";
                            using (FileStream output = new FileStream(Path.Combine(outDir, newFilename), FileMode.Create, FileAccess.Write))
                            {
                                output.Write(dst, 0, (int)dstLen);
                            }
                        }
                        else
                        {
                            errors += "Unknown tag for file: " + name + Environment.NewLine;
                            log += "Unknown tag for file: " + name + Environment.NewLine;
                        }
                        continue;
                    }

                    if (previewIndex != -1)
                    {
                        if (modFiles[i].tag == FileTextureTag || modFiles[i].tag == FileTextureTag2)
                        {
                            Image image = new Image(dst, Image.ImageFormat.DDS);
                            texExplorer.pictureBoxPreview.Image = image.getBitmapARGB();
                        }
                        else
                        {
                            texExplorer.pictureBoxPreview.Image = null;
                        }
                        break;
                    }
                    else if (replace || newReplace)
                    {
                        if (modFiles[i].tag == FileTextureTag || modFiles[i].tag == FileTextureTag2)
                        {
                            int index = -1;
                            for (int t = 0; t < textures.Count; t++)
                            {
                                if (textures[t].crc == crc)
                                {
                                    index = t;
                                    break;
                                }
                            }
                            if (index != -1)
                            {
                                if (replace)
                                {
                                    FoundTexture foundTexture = textures[index];
                                    Image image = new Image(dst, Image.ImageFormat.DDS);
                                    errors += replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, verify, modFiles[i].tag == FileTextureTag2);
                                    textures[index] = foundTexture;
                                }
                                else
                                {
                                    // TODO
                                }
                            }
                            else
                            {
                                log += "Error: Texture " + name + string.Format("_0x{0:X8}", crc) + "is not present in your game setup. Texture skipped." + Environment.NewLine;
                            }
                        }
                        else if (modFiles[i].tag == FileBinaryTag)
                        {
                            string path = GameData.GamePath + pkgPath;
                            if (!File.Exists(path))
                            {
                                errors += "Warning: File " + path + " not exists in your game setup." + Environment.NewLine;
                                log += "Warning: File " + path + " not exists in your game setup." + Environment.NewLine;
                                continue;
                            }
                            if (replace)
                            {
                                Package pkg = cachePackageMgr.OpenPackage(path);
                                pkg.setExportData(exportId, dst);
                            }
                            else
                            {
                                // TODO
                            }
                        }
                        else if (modFiles[i].tag == FileXdeltaTag)
                        {
                            string path = GameData.GamePath + pkgPath;
                            if (!File.Exists(path))
                            {
                                errors += "Warning: File " + path + " not exists in your game setup." + Environment.NewLine;
                                log += "Warning: File " + path + " not exists in your game setup." + Environment.NewLine;
                                continue;
                            }
                            if (replace)
                            {
                                Package pkg = cachePackageMgr.OpenPackage(path);
                                byte[] buffer = new Xdelta3Helper.Xdelta3().Decompress(pkg.getExportData(exportId), dst);
                                pkg.setExportData(exportId, buffer);
                            }
                            else
                            {
                                // TODO
                            }
                        }
                        else
                        {
                            errors += "Error: Unknown tag for file: " + name + Environment.NewLine;
                            log += "Error: Unknown tag for file: " + name + Environment.NewLine;
                        }
                    }
                    else
                        throw new Exception();
                }
                if (previewIndex == -1 && !extract && !replace && !newReplace)
                    texExplorer.listViewTextures.EndUpdate();
            }
            return errors;
        }

        static public Stream compressData(byte[] inputData)
        {
            MemoryStream ouputStream = new MemoryStream();
            uint compressedSize = 0;
            uint dataBlockLeft = (uint)inputData.Length;
            uint newNumBlocks = ((uint)inputData.Length + maxBlockSize - 1) / maxBlockSize;
            List<Package.ChunkBlock> blocks = new List<Package.ChunkBlock>();
            using (MemoryStream inputStream = new MemoryStream(inputData))
            {
                // skip blocks header and table - filled later
                ouputStream.Seek(SizeOfChunk + SizeOfChunkBlock * newNumBlocks, SeekOrigin.Begin);

                for (int b = 0; b < newNumBlocks; b++)
                {
                    Package.ChunkBlock block = new Package.ChunkBlock();
                    block.uncomprSize = Math.Min(maxBlockSize, dataBlockLeft);
                    dataBlockLeft -= block.uncomprSize;
                    block.uncompressedBuffer = inputStream.ReadToBuffer(block.uncomprSize);
                    blocks.Add(block);
                }
            }

            Parallel.For(0, blocks.Count, b =>
            {
                Package.ChunkBlock block = blocks[b];
                block.compressedBuffer = new ZlibHelper.Zlib().Compress(block.uncompressedBuffer);
                if (block.compressedBuffer.Length == 0)
                    throw new Exception("Compression failed!");
                block.comprSize = (uint)block.compressedBuffer.Length;
                blocks[b] = block;
            });

            for (int b = 0; b < blocks.Count; b++)
            {
                Package.ChunkBlock block = blocks[b];
                ouputStream.Write(block.compressedBuffer, 0, (int)block.comprSize);
                compressedSize += block.comprSize;
            }

            ouputStream.SeekBegin();
            ouputStream.WriteUInt32(compressedSize);
            ouputStream.WriteInt32(inputData.Length);
            foreach (Package.ChunkBlock block in blocks)
            {
                ouputStream.WriteUInt32(block.comprSize);
                ouputStream.WriteUInt32(block.uncomprSize);
            }

            return ouputStream;
        }

        static public byte[] decompressData(Stream stream, long compressedSize)
        {
            uint compressedChunkSize = stream.ReadUInt32();
            uint uncompressedChunkSize = stream.ReadUInt32();
            byte[] data = new byte[uncompressedChunkSize];
            uint blocksCount = (uncompressedChunkSize + maxBlockSize - 1) / maxBlockSize;
            if ((compressedChunkSize + SizeOfChunk + SizeOfChunkBlock * blocksCount) != compressedSize)
                throw new Exception("not match");

            List<Package.ChunkBlock> blocks = new List<Package.ChunkBlock>();
            for (uint b = 0; b < blocksCount; b++)
            {
                Package.ChunkBlock block = new Package.ChunkBlock();
                block.comprSize = stream.ReadUInt32();
                block.uncomprSize = stream.ReadUInt32();
                blocks.Add(block);
            }

            for (int b = 0; b < blocks.Count; b++)
            {
                Package.ChunkBlock block = blocks[b];
                block.compressedBuffer = stream.ReadToBuffer(blocks[b].comprSize);
                block.uncompressedBuffer = new byte[maxBlockSize * 2];
                blocks[b] = block;
            }

            Parallel.For(0, blocks.Count, b =>
            {
                uint dstLen = 0;
                Package.ChunkBlock block = blocks[b];
                dstLen = new ZlibHelper.Zlib().Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
                if (dstLen != block.uncomprSize)
                    throw new Exception("Decompressed data size not expected!");
            });

            int dstPos = 0;
            for (int b = 0; b < blocks.Count; b++)
            {
                Buffer.BlockCopy(blocks[b].uncompressedBuffer, 0, data, dstPos, (int)blocks[b].uncomprSize);
                dstPos += (int)blocks[b].uncomprSize;
            }

            return data;
        }

        public void extractTextureToDDS(string outputFile, string packagePath, int exportID)
        {
            Package package = new Package(packagePath);
            Texture texture = new Texture(package, exportID, package.getExportData(exportID));
            package.Dispose();
            while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
            {
                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
            }
            List<MipMap> mipmaps = new List<MipMap>();
            PixelFormat pixelFormat = Image.getPixelFormatType(texture.properties.getProperty("Format").valueName);
            for (int i = 0; i < texture.mipMapsList.Count; i++)
            {
                byte[] data = texture.getMipMapDataByIndex(i);
                if (data == null)
                {
                    MessageBox.Show("Failed to extract to DDS file. Broken game files!");
                    return;
                }
                mipmaps.Add(new MipMap(data, texture.mipMapsList[i].width, texture.mipMapsList[i].height, pixelFormat));
            }
            Image image = new Image(mipmaps, pixelFormat);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                image.StoreImageToDDS(fs);
            }
        }

        public void extractTextureToPng(string outputFile, string packagePath, int exportID)
        {
            Package package = new Package(packagePath);
            Texture texture = new Texture(package, exportID, package.getExportData(exportID));
            package.Dispose();
            PixelFormat format = Image.getPixelFormatType(texture.properties.getProperty("Format").valueName);
            Texture.MipMap mipmap = texture.getTopMipmap();
            byte[] data = texture.getTopImageData();
            if (data == null)
            {
                MessageBox.Show("Failed to extract to PNG file. Broken game files!");
                return;
            }
            PngBitmapEncoder image = Image.convertToPng(data, mipmap.width, mipmap.height, format);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                image.Save(fs);
            }
        }

    }
}
