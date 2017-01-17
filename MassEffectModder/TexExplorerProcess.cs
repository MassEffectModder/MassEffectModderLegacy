/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

        public struct FileMod
        {
            public uint tag;
            public string name;
            public long offset;
            public long size;
        }

        public string extractTextureMod(string filenameMod, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer)
        {
            return processTextureMod(filenameMod, -1, true, false, outDir, textures, cachePackageMgr, texExplorer);
        }

        public string previewTextureMod(string filenameMod, int previewIndex, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer)
        {
            return processTextureMod(filenameMod, previewIndex, false, false, "", textures, cachePackageMgr, texExplorer);
        }

        public string replaceTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer)
        {
            return processTextureMod(filenameMod, -1, false, true, "", textures, cachePackageMgr, texExplorer);
        }

        public string listTextureMod(string filenameMod, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer)
        {
            return processTextureMod(filenameMod, -1, false, false, "", textures, cachePackageMgr, texExplorer);
        }

        private string processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, string outDir, List<FoundTexture> textures, CachePackageMgr cachePackageMgr, TexExplorer texExplorer)
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
                if (tag != TexExplorer.TextureModTag || (version != TexExplorer.TextureModVersion1 && version != TexExplorer.TextureModVersion))
                {
                    errors += "File " + filenameMod + " is not a valid MEM mod" + Environment.NewLine;
                    if (previewIndex == -1 && !extract && !replace)
                        texExplorer.listViewTextures.EndUpdate();
                    return errors;
                }
                else
                {
                    uint gameType = 0;
                    if (version == TexExplorer.TextureModVersion)
                        fs.JumpTo(fs.ReadInt64());
                    gameType = fs.ReadUInt32();
                    if ((MeType)gameType != GameData.gameType)
                    {
                        errors += "File " + filenameMod + " is not a MEM mod valid for this game" + Environment.NewLine;
                        if (previewIndex == -1 && !extract && !replace)
                            texExplorer.listViewTextures.EndUpdate();
                        return errors;
                    }
                }
                int numFiles = fs.ReadInt32();
                List<FileMod> modFiles = new List<FileMod>();
                if (version == TexExplorer.TextureModVersion)
                {
                    for (int i = 0; i < numFiles; i++)
                    {
                        FileMod fileMod = new FileMod();
                        fileMod.tag = fs.ReadUInt32();
                        fileMod.name = fs.ReadStringASCIINull();
                        fileMod.offset = fs.ReadInt64();
                        fileMod.size = fs.ReadInt64();
                        if (fileMod.tag == FileTextureTag)
                            modFiles.Add(fileMod);
                    }
                    numFiles = modFiles.Count;
                }

                for (int i = 0; i < numFiles; i++)
                {
                    string name;
                    uint crc;
                    long size = 0, dstLen = 0, decSize = 0;
                    byte[] dst = null;
                    byte[] src = null;
                    if (version == TexExplorer.TextureModVersion)
                    {
                        if (previewIndex != -1)
                            i = previewIndex;
                        fs.JumpTo(modFiles[i].offset);
                        size = modFiles[i].size;
                    }
                    name = fs.ReadStringASCIINull();
                    crc = fs.ReadUInt32();
                    if (version == TexExplorer.TextureModVersion1)
                    {
                        decSize = fs.ReadUInt32();
                        size = fs.ReadUInt32();
                    }

                    if (texExplorer != null)
                    {
                        texExplorer._mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileName(filenameMod) +
                            " - Texture " + (i + 1) + " of " + numFiles + " - " + name);
                    }

                    if (version == TexExplorer.TextureModVersion1)
                    {
                        if (previewIndex != -1 && i != previewIndex)
                        {
                            fs.Skip(size);
                            continue;
                        }
                    }

                    if (previewIndex == -1 && !extract && !replace)
                    {
                        if (version == TexExplorer.TextureModVersion1)
                            fs.Skip(size);
                        FoundTexture foundTexture;
                        if (GameData.gameType == MeType.ME1_TYPE)
                            foundTexture = textures.Find(s => s.crc == crc && s.name == name);
                        else
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
                            errors += "Texture skipped. Texture " + name + string.Format("_0x{0:X8}", crc) + " is not present in your game setup" + Environment.NewLine;
                        }
                        continue;
                    }

                    if (version == TexExplorer.TextureModVersion1)
                    {
                        src = fs.ReadToBuffer(size);
                        dst = new byte[decSize];
                        dstLen = ZlibHelper.Zlib.Decompress(src, (uint)size, dst);
                    }
                    else
                    {
                        dst = decompressData(fs, size);
                        dstLen = dst.Length;
                    }

                    if (extract)
                    {
                        string filename = name + "_" + string.Format("0x{0:X8}", crc) + ".dds";
                        using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                        {
                            output.Write(dst, 0, (int)dstLen);
                        }
                        continue;
                    }
                    if (previewIndex != -1)
                    {
                        DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                        texExplorer.pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                        break;
                    }
                    else
                    {
                        FoundTexture foundTexture;
                        if (GameData.gameType == MeType.ME1_TYPE)
                            foundTexture = textures.Find(s => s.crc == crc && s.name == name);
                        else
                            foundTexture = textures.Find(s => s.crc == crc);
                        if (foundTexture.crc != 0)
                        {
                            if (replace)
                            {
                                DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                                if (!image.checkExistAllMipmaps())
                                {
                                    errors += "Error in texture: " + name + string.Format("_0x{0:X8}", crc) + " Texture skipped. This texture has not all the required mipmaps" + Environment.NewLine;
                                    continue;
                                }
                                errors += replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name);
                            }
                        }
                        else
                        {
                            errors += "Texture " + name + string.Format("_0x{0:X8}", crc) + "is not present in your game setup" + Environment.NewLine;
                        }
                    }
                }
                if (previewIndex == -1 && !extract && !replace)
                    texExplorer.listViewTextures.EndUpdate();
            }
            return errors;
        }

        public string createTextureMod(string inDir, string outFile, List<FoundTexture> textures, MainWindow mainWindow)
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
                        mainWindow.updateStatusLabel("Ctreating MOD: " + Path.GetFileName(outFile));
                    string crcStr = Path.GetFileNameWithoutExtension(file);
                    if (crcStr.Contains("_0x"))
                    {
                        crcStr = Path.GetFileNameWithoutExtension(file).Split('_').Last().Substring(2, 8); // in case filename contain CRC 
                    }
                    else
                    {
                        crcStr = Path.GetFileNameWithoutExtension(file).Split('-').Last().Substring(2, 8); // in case filename contain CRC 
                    }
                    uint crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    if (crc == 0)
                    {
                        errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC. Skipping texture..." + Environment.NewLine;
                        continue;
                    }

                    string filename = Path.GetFileNameWithoutExtension(file);
                    int idx = filename.IndexOf(crcStr);
                    string name = filename.Substring(0, idx - "_0x".Length);

                    string textureName = "";
                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    FoundTexture foundTexture = textures.Find(s => s.crc == crc && s.name == name);
                    if (foundCrcList.Count == 0)
                    {
                        errors += "Texture skipped. Texture " + Path.GetFileName(file) + " is not present in your game setup" + Environment.NewLine;
                        continue;
                    }

                    int savedCount = count;
                    for (int l = 0; l < foundCrcList.Count; l++)
                    {
                        if (foundTexture.crc == crc)
                        {
                            if (l > 0)
                                break;
                            textureName = foundTexture.name;
                        }
                        else
                        {
                            textureName = foundCrcList[l].name;
                        }
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            byte[] src = fs.ReadToBuffer((int)fs.Length);
                            DDSImage image = new DDSImage(new MemoryStream(src));
                            if (!image.checkExistAllMipmaps())
                            {
                                errors += "Error in texture: " + Path.GetFileName(file) + " Texture has not all the required mipmaps" + Environment.NewLine;
                                continue;
                            }

                            Package pkg = new Package(GameData.GamePath + foundCrcList[l].list[0].path);
                            Texture texture = new Texture(pkg, foundCrcList[l].list[0].exportID, pkg.getExportData(foundCrcList[l].list[0].exportID));

                            if (texture.mipMapsList.Count > 1 && image.mipMaps.Count() <= 1)
                            {
                                errors += "Error in texture: " + Path.GetFileName(file) + "This texture must have mipmaps, skipping texture..." + Environment.NewLine;
                                continue;
                            }

                            string fmt = texture.properties.getProperty("Format").valueName;
                            DDSFormat ddsFormat = DDSImage.convertFormat(fmt);
                            if (image.ddsFormat != ddsFormat)
                            {
                                errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong texture format: " + fmt + ", skipping..." + Environment.NewLine;
                                continue;
                            }

                            if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                texture.mipMapsList[0].width / texture.mipMapsList[0].height)
                            {
                                errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                                continue;
                            }

                            if (mainWindow != null)
                                mainWindow.updateStatusLabel2("Texture " + (n + 1) + " of " + files.Count() + ", Name: " + textureName);

                            Stream dst = compressData(src);
                            dst.SeekBegin();

                            FileMod fileMod = new FileMod();
                            fileMod.tag = FileTextureTag;
                            fileMod.name = Path.GetFileName(file);
                            fileMod.offset = outFs.Position;
                            fileMod.size = dst.Length;
                            count++;
                            modFiles.Add(fileMod);

                            outFs.WriteStringASCIINull(textureName);
                            outFs.WriteUInt32(crc);
                            outFs.WriteFromStream(dst, dst.Length);
                        }
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
                errors += "There are no texture files in " + inDir + " Mod not generated." + Environment.NewLine;
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
                mipmaps.Add(new DDSImage.MipMap(texture.getMipMapDataByIndex(i), format, texture.mipMapsList[i].width, texture.mipMapsList[i].height));
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
            PngBitmapEncoder image = DDSImage.ToPng(texture.getTopImageData(), format, mipmap.width, mipmap.height);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                image.Save(fs);
            }
        }
    }
}
