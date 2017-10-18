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
            return processTextureMod(filenameMod, -1, true, false, false, outDir, textures, cachePackageMgr, texExplorer, ref log);
        }

        public string previewTextureMod(string filenameMod, int previewIndex, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, previewIndex, false, false, false, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string replaceTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, bool verify, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, true, verify, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        public string listTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
        {
            return processTextureMod(filenameMod, -1, false, false, false, "", textures, cachePackageMgr, texExplorer, ref log);
        }

        private string processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, bool verify, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer, ref string log)
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
                                ListViewItem item = new ListViewItem(foundTexture.name + " (" + Path.GetFileNameWithoutExtension(foundTexture.list[0].path).ToUpperInvariant() + ")");
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
                            Image image = new Image(dst, Image.ImageFormat.DDS);
                            texExplorer.pictureBoxPreview.Image = image.getBitmapARGB();
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
                                Image image = new Image(dst, Image.ImageFormat.DDS);
                                errors += replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, crc, verify);
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
            while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
            {
                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
            }
            List<MipMap> mipmaps = new List<MipMap>();
            PixelFormat pixelFormat = Image.getEngineFormatType(texture.properties.getProperty("Format").valueName);
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
            PixelFormat format = Image.getEngineFormatType(texture.properties.getProperty("Format").valueName);
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
