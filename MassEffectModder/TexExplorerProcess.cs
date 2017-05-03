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
using Microsoft.VisualBasic;
using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public const uint FileBinaryTag = 0x4E494246;

        public struct FileMod
        {
            public uint tag;
            public string name;
            public long offset;
            public long size;
        }

        public string extractTextureMod(string filenameMod, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, -1, true, false, outDir, textures, cachePackageMgr, texExplorer, ref log);
        }

        public string previewTextureMod(string filenameMod, int previewIndex, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, previewIndex, false, false, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string replaceTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, true, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string listTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, false, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        private string processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            string errors = "";

            using (FileStream fs = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
            {
                if (previewIndex == -1 && !extract && !replace)
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
                    if (modFiles[i].tag == FileTextureTag)
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

                    if (texExplorer != null)
                    {
                        texExplorer._mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileName(filenameMod) +
                            " - File " + (i + 1) + " of " + numFiles + " - " + name);
                    }

                    if (previewIndex == -1 && !extract && !replace)
                    {
                        if (modFiles[i].tag == FileTextureTag)
                        {
                            FoundTexture foundTexture;
                            foundTexture = textures.Find(s => s.crc == crc);
                            if (foundTexture.crc != 0)
                            {
                                ListViewItem item = new ListViewItem(foundTexture.name + " (" + foundTexture.packageName + ")");
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
                            ListViewItem item = new ListViewItem(name + " (Binary Mod)");
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
                        else if (modFiles[i].tag == FileBinaryTag)
                        {
                            string filename = name;
                            using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
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
                        if (modFiles[i].tag == FileTextureTag)
                        {
                            DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                            texExplorer.pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                        }
                        else
                        {
                            texExplorer.pictureBoxPreview.Image = null;
                        }
                        break;
                    }
                    else if (replace)
                    {
                        if (modFiles[i].tag == FileTextureTag)
                        {
                            FoundTexture foundTexture;
                            foundTexture = textures.Find(s => s.crc == crc);
                            if (foundTexture.crc != 0)
                            {
                                DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                                if (!image.checkExistAllMipmaps())
                                {
                                    errors += "Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps." + Environment.NewLine;
                                    log += "Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps." + Environment.NewLine;
                                    continue;
                                }
                                errors += replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc);
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
                            Package pkg = cachePackageMgr.OpenPackage(path);
                            pkg.setExportData(exportId, dst);
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
                if (previewIndex == -1 && !extract && !replace)
                    texExplorer.listViewTextures.EndUpdate();
            }
            return errors;
        }

        public string createTextureMod(string inDir, string outFile, List<FoundTexture> textures, MainWindow mainWindow, ref string log)
        {
            string errors = "";
            int count = 0;

            List<string> files = Directory.GetFiles(inDir, "*.dds").Where(item => item.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)).ToList();
            files.Sort();
            List<FileMod> modFiles = new List<FileMod>();
            using (FileStream outFs = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                outFs.WriteUInt32(TexExplorer.TextureModTag);
                outFs.WriteUInt32(TexExplorer.TextureModVersion);
                outFs.WriteInt64(0); // files info offset - filled later

                for (int n = 0; n < files.Count(); n++)
                {
                    string file = files[n];
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("Creating MOD: " + Path.GetFileName(outFile));
                        mainWindow.updateStatusLabel2("Texture " + (n + 1) + " of " + files.Count() + ", File: " + Path.GetFileName(file));
                    }
                    string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!filename.Contains("0x"))
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        log += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        log += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
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
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (_0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        log += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (_0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        errors += "Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup." + Environment.NewLine;
                        log += "Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup." + Environment.NewLine;
                        continue;
                    }

                    int savedCount = count;
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        byte[] src = fs.ReadToBuffer((int)fs.Length);
                        Package pkg = new Package(GameData.GamePath + foundCrcList[0].list[0].path);
                        Texture texture = new Texture(pkg, foundCrcList[0].list[0].exportID, pkg.getExportData(foundCrcList[0].list[0].exportID));
                        string fmt = texture.properties.getProperty("Format").valueName;
                        DDSFormat ddsFormat = DDSImage.convertFormat(fmt);
                        DDSImage image = new DDSImage(new MemoryStream(src));

                        if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                            texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                            log += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        if (!image.checkExistAllMipmaps() ||
                            (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1) ||
                            image.ddsFormat != ddsFormat)
                        {
                            src = convertDDS(ddsFormat, src);
                        }

                        Stream dst = compressData(src);
                        dst.SeekBegin();

                        FileMod fileMod = new FileMod();
                        fileMod.tag = FileTextureTag;
                        fileMod.name = Path.GetFileName(file);
                        fileMod.offset = outFs.Position;
                        fileMod.size = dst.Length;
                        count++;
                        modFiles.Add(fileMod);

                        outFs.WriteStringASCIINull(foundCrcList[0].name);
                        outFs.WriteUInt32(crc);
                        outFs.WriteFromStream(dst, dst.Length);
                    }
                    if (count == savedCount)
                    {
                        continue;
                    }
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
            if (count == 0)
            {
                errors += "There are no texture files in " + inDir + ", mod not generated." + Environment.NewLine;
                log += "There are no texture files in " + inDir + ", mod not generated." + Environment.NewLine;
                File.Delete(outFile);
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
                block.compressedBuffer = ZlibHelper.Zlib.Compress(block.uncompressedBuffer);
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
                dstLen = ZlibHelper.Zlib.Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
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
            while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
            {
                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
            }
            List<DDSImage.MipMap> mipmaps = new List<DDSImage.MipMap>();
            DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
            for (int i = 0; i < texture.mipMapsList.Count; i++)
            {
                byte[] data = texture.getMipMapDataByIndex(i);
                if (data == null)
                {
                    MessageBox.Show("Failed to extract to DDS file. Broken game files!");
                    return;
                }
                mipmaps.Add(new DDSImage.MipMap(data, format, texture.mipMapsList[i].width, texture.mipMapsList[i].height));
            }
            DDSImage dds = new DDSImage(mipmaps);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                dds.SaveDDSImage(fs);
            }
        }

        public void extractTextureToPng(string outputFile, string packagePath, int exportID)
        {
            Package package = new Package(packagePath);
            Texture texture = new Texture(package, exportID, package.getExportData(exportID));
            DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
            Texture.MipMap mipmap = texture.getTopMipmap();
            byte[] data = texture.getTopImageData();
            if (data == null)
            {
                MessageBox.Show("Failed to extract to PNG file. Broken game files!");
                return;
            }
            PngBitmapEncoder image = DDSImage.ToPng(data, format, mipmap.width, mipmap.height);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                image.Save(fs);
            }
        }

        static public byte[] convertDDS(DDSFormat format, byte[] src, string extension = ".dds")
        {
            string fmtParam = "";

            switch (format)
            {
                case DDSFormat.DXT1:
                    fmtParam = "dxt1";
                    break;
                case DDSFormat.DXT3:
                    fmtParam = "dxt3";
                    break;
                case DDSFormat.DXT5:
                    fmtParam = "dxt5";
                    break;
                case DDSFormat.ATI2:
                    fmtParam = "ati2";
                    break;
                case DDSFormat.V8U8:
                    fmtParam = "v8u8";
                    break;
                case DDSFormat.G8:
                    fmtParam = "g8";
                    break;
                case DDSFormat.ARGB:
                    fmtParam = "argb";
                    break;
                case DDSFormat.RGB:
                    fmtParam = "rgb";
                    break;
                default:
                    throw new Exception("invalid texture format " + format);
            }

            string inputFile = Path.Combine(Program.dllPath, "input" + extension);
            string outputFile = Path.Combine(Program.dllPath, "output.dds");
            if (File.Exists(inputFile))
                File.Delete(inputFile);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(inputFile, FileMode.CreateNew))
            {
                fs.WriteFromBuffer(src);
            }
            Interaction.Shell(Path.Combine(Program.dllPath, "ConvertDDS.exe") + " " +
                fmtParam + " " + inputFile + " " + outputFile, AppWinStyle.Hide, true);
            using (FileStream fs = new FileStream(outputFile, FileMode.Open))
            {
                return fs.ReadToBuffer(new FileInfo(outputFile).Length);
            }
        }
    }
}
