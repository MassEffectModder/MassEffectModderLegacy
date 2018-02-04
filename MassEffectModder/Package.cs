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

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using StreamHelpers;
using System.Linq;
using System.Threading.Tasks;

namespace MassEffectModder
{
    public class Package : IDisposable
    {
        const uint packageTag = 0x9E2A83C1;
        const ushort packageFileVersionME1 = 491;
        const ushort packageFileVersionME2 = 512;
        const ushort packageFileVersionME3 = 684;
        const uint maxBlockSize = 0x20000; // 128KB
        const uint maxChunkSize = 0x100000; // 1MB
        const uint packageHeaderSizeME1 = 121;
        const uint packageHeaderSizeME2 = 117;
        const uint packageHeaderSizeME3 = 126;
        const int sizeOfGeneration = 12;

        const int packageHeaderTagOffset = 0;
        const int packageHeaderVersionOffset = 4;
        const int packageHeaderFirstChunkSizeOffset = 8;
        const int packageHeaderNameSizeOffset = 12;

        const int packageHeaderNamesCountTableOffset = 0;
        const int packageHeaderNamesOffsetTabletsOffset = 4;
        const int packageHeaderExportsCountTableOffset = 8;
        const int packageHeaderExportsOffsetTableOffset = 12;
        const int packageHeaderImportsCountTableOffset = 16;
        const int packageHeaderImportsOffsetTableOffset = 20;
        const int packageHeaderDependsOffsetTableOffset = 24;
        const int packageHeaderGuidsOffsetTableOffset = 28;
        const int packageHeaderGuidsCountTableOffset = 36;
        public const string MEMendFileMarker = "ThisIsMEMEndOfFileMarker";

        public enum CompressionType
        {
            None = 0,
            Zlib,
            LZO
        }
        public enum PackageFlags
        {
            compressed = 0x02000000,
        }

        byte[] packageHeader;
        uint packageHeaderSize;
        uint packageFileVersion;
        uint numChunks;
        uint someTag;
        long dataOffset;
        uint exportsEndOffset = 0;
        public CompressionType compressionType;
        public FileStream packageFile;
        public string packagePath;
        MemoryStream packageData;
        List<Chunk> chunks;
        List<NameEntry> namesTable;
        uint namesTableEnd;
        List<ImportEntry> importsTable;
        uint importsTableEnd;
        public List<ExportEntry> exportsTable;
        List<int> dependsTable;
        List<GuidEntry> guidsTable;
        List<ExtraNameEntry> extraNamesTable;
        int currentChunk = -1;
        MemoryStream chunkCache;
        bool modified;
        bool memoryMode;
        public int nameIdTexture2D = -1;
        public int nameIdLightMapTexture2D = -1;
        public int nameIdShadowMapTexture2D = -1;
        public int nameIdTextureFlipBook = -1;

        const int SizeOfChunkBlock = 8;
        public struct ChunkBlock
        {
            public uint comprSize;
            public uint uncomprSize;
            public byte[] compressedBuffer;
            public byte[] uncompressedBuffer;
        }

        const int SizeOfChunk = 16;
        public struct Chunk
        {
            public uint uncomprOffset;
            public uint uncomprSize;
            public uint comprOffset;
            public uint comprSize;
            public List<ChunkBlock> blocks;
        }

        public struct NameEntry
        {
            public string name;
            public ulong flags;
        }

        public struct ImportEntry
        {
            //public int packageFileId;
            //public string packageFile; // not used - save RAM
            public int classId;
            //public string className; // not used - save RAM
            public int linkId;
            public int objectNameId;
            public string objectName;
            public byte[] raw;
        }

        public struct ExportEntry
        {
            const int ClassIdOffset = 0;
            const int LinkIdOffset = 8;
            const int ObjectNameIdOffset = 12;
            const int DataSizeOffset = 32;
            public const int DataOffsetOffset = 36;
            public int classId
            {
                get
                {
                    return BitConverter.ToInt32(raw, ClassIdOffset);
                }
            }
            //public string className; // not used - save RAM
            //public int classParentId; // not used - save RAM
            public int linkId
            {
                get
                {
                    return BitConverter.ToInt32(raw, LinkIdOffset);
                }
            }
            public int objectNameId
            {
                get
                {
                    return BitConverter.ToInt32(raw, ObjectNameIdOffset);
                }
            }
            public string objectName;
            //public int suffixNameId; // not used - save RAM
            public uint dataSize
            {
                get
                {
                    return BitConverter.ToUInt32(raw, DataSizeOffset);
                }
                set
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, raw, DataSizeOffset, sizeof(uint));
                }
            }
            public uint dataOffset
            {
                get
                {
                    return BitConverter.ToUInt32(raw, DataOffsetOffset);
                }
                set
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, raw, DataOffsetOffset, sizeof(uint));
                }
            }
            //public ulong objectFlags; // not used - save RAM
            public byte[] raw;
            public bool updatedData;
            public byte[] newData;
            public uint id;
        }
        public struct GuidEntry
        {
            public byte[] guid;
            public int index;
        }

        public struct ExtraNameEntry
        {
            public string name;
            public byte[] raw;
        }

        private uint tag
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, packageHeaderTagOffset);
            }
        }

        private ushort version
        {
            get
            {
                return BitConverter.ToUInt16(packageHeader, packageHeaderVersionOffset);
            }
        }

        private uint endOfTablesOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, packageHeaderFirstChunkSizeOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, packageHeaderFirstChunkSizeOffset, sizeof(uint));
            }
        }

        private int packageHeaderFlagsOffset
        {
            get
            {
                int len = BitConverter.ToInt32(packageHeader, packageHeaderNameSizeOffset);
                if (len < 0)
                    return (len * -2) + packageHeaderNameSizeOffset + sizeof(uint); // Unicode name
                else
                    return len + packageHeaderNameSizeOffset + sizeof(uint); // Ascii name
            }
        }

        private uint flags
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, packageHeaderFlagsOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, packageHeaderFlagsOffset, sizeof(uint));
            }
        }

        public bool compressed
        {
            get
            {
                return (flags & (uint)PackageFlags.compressed) != 0;
            }
            set
            {
                if (value)
                    flags |= (uint)PackageFlags.compressed;
                else
                    flags &= ~(uint)PackageFlags.compressed;
                Buffer.BlockCopy(BitConverter.GetBytes(flags), 0, packageHeader, packageHeaderFlagsOffset, sizeof(uint));
            }
        }

        private int tablesOffset
        {
            get
            {
                if (version == packageFileVersionME3)
                    return packageHeaderFlagsOffset + sizeof(uint) + sizeof(uint); // additional entry in header
                else
                    return packageHeaderFlagsOffset + sizeof(uint);
            }
        }

        private uint namesCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderNamesCountTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderNamesCountTableOffset, sizeof(uint));
            }
        }

        private uint namesOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderNamesOffsetTabletsOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderNamesOffsetTabletsOffset, sizeof(uint));
            }
        }

        private uint exportsCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderExportsCountTableOffset);
            }
        }

        private uint exportsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderExportsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderExportsOffsetTableOffset, sizeof(uint));
            }
        }

        private uint importsCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderImportsCountTableOffset);
            }
        }

        private uint importsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderImportsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderImportsOffsetTableOffset, sizeof(uint));
            }
        }

        private uint dependsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderDependsOffsetTableOffset);
            }
        }

        private uint guidsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderGuidsOffsetTableOffset);
            }
        }

        private uint guidsCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderGuidsCountTableOffset);
            }
        }

        public bool isName(int id)
        {
            return id >= 0 && id < namesTable.Count;
        }

        public string getClassName(int id)
        {
            if (id > 0 && id < exportsTable.Count)
                return exportsTable[id - 1].objectName;
            if (id < 0 && -id < importsTable.Count)
                return importsTable[-id - 1].objectName;
            return "Class";
        }

        public int getClassNameId(int id)
        {
            if (id > 0 && id < exportsTable.Count)
                return exportsTable[id - 1].objectNameId;
            if (id < 0 && -id < importsTable.Count)
                return importsTable[-id - 1].objectNameId;
            return 0;
        }

        public string resolvePackagePath(int id)
        {
            string s = "";
            if (id > 0 && id < exportsTable.Count)
            {
                s += resolvePackagePath(exportsTable[id - 1].linkId);
                if (s != "")
                    s += ".";
                s += exportsTable[id - 1].objectName;
            }
            if (id < 0 && -id < importsTable.Count)
            {
                s += resolvePackagePath(importsTable[-id - 1].linkId);
                if (s != "")
                    s += ".";
                s += importsTable[-id - 1].objectName;
            }
            return s;
        }

        public Package(string filename, bool memMode = false, bool headerOnly = false)
        {
            packagePath = filename;
            memoryMode = memMode;

            if (!File.Exists(filename))
                throw new Exception("File not found: " + filename);

            packageFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            try
            {
                if (packageFile.ReadUInt32() != packageTag)
                    throw new Exception("Wrong PCC tag: " + filename);

                ushort ver = packageFile.ReadUInt16();
                if (ver == packageFileVersionME1)
                {
                    packageHeaderSize = packageHeaderSizeME1;
                    packageFileVersion = packageFileVersionME1;
                }
                else if (ver == packageFileVersionME2)
                {
                    packageHeaderSize = packageHeaderSizeME2;
                    packageFileVersion = packageFileVersionME2;
                }
                else if (ver == packageFileVersionME3)
                {
                    packageHeaderSize = packageHeaderSizeME3;
                    packageFileVersion = packageFileVersionME3;
                }
                else
                    throw new Exception("Wrong PCC version: " + filename);

                packageFile.SeekBegin();
                packageHeader = packageFile.ReadToBuffer(packageHeaderSize);
            }
            catch
            {
                if (new FileInfo(filename).Length == 0)
                    throw new Exception("PCC file has 0 length: " + filename);
                throw new Exception("Problem with PCC file header: " + filename);
            }

            compressionType = (CompressionType)packageFile.ReadUInt32();

            if (headerOnly)
                return;

            numChunks = packageFile.ReadUInt32();

            dataOffset = packageFile.Position;

            if (compressed)
            {
                chunks = new List<Chunk>();
                for (int i = 0; i < numChunks; i++)
                {
                    Chunk chunk = new Chunk();
                    chunk.uncomprOffset = packageFile.ReadUInt32();
                    chunk.uncomprSize = packageFile.ReadUInt32();
                    chunk.comprOffset = packageFile.ReadUInt32();
                    chunk.comprSize = packageFile.ReadUInt32();
                    chunks.Add(chunk);
                }
            }
            long filePos = packageFile.Position;
            someTag = packageFile.ReadUInt32();
            if (version == packageFileVersionME2)
                packageFile.SkipInt32(); // const 0

            loadExtraNames(packageFile);

            if (compressed && packageFile.Position != chunks[0].comprOffset)
                throw new Exception();

            dataOffset += packageFile.Position - filePos;

            if (compressed && dataOffset != chunks[0].uncomprOffset)
                throw new Exception();

            if (compressed)
            {
                packageData = new MemoryStream();
                uint length = endOfTablesOffset - (uint)dataOffset;
                packageData.JumpTo(dataOffset);
                getData((uint)dataOffset, length, packageData);
            }

            if (compressed)
                loadNames(packageData);
            else
                loadNames(packageFile);

            if (endOfTablesOffset < namesOffset)
            {
                if (compressed) // allowed only uncompressed
                    throw new Exception();
            }

            if (compressed)
                loadImports(packageData);
            else
                loadImports(packageFile);

            if (endOfTablesOffset < importsOffset)
            {
                if (compressed) // allowed only uncompressed
                    throw new Exception();
            }

            if (compressed)
                loadExports(packageData);
            else
                loadExports(packageFile);

            //loadImportsNames(); // not used by tool
            //loadExportsNames(); // not used by tool
        }

        private void getData(uint offset, uint length, Stream output)
        {
            if (compressed)
            {
                uint bytesLeft = length;
                for (int c = 0; c < chunks.Count; c++)
                {
                    Chunk chunk = chunks[c];
                    if (chunk.uncomprOffset + chunk.uncomprSize <= offset)
                        continue;
                    uint startInChunk;
                    if (offset < chunk.uncomprOffset)
                        startInChunk = 0;
                    else
                        startInChunk = offset - chunk.uncomprOffset;

                    uint bytesLeftInChunk = Math.Min(chunk.uncomprSize - startInChunk, bytesLeft);
                    if (currentChunk != c)
                    {
                        if (chunkCache != null)
                        {
                            chunkCache.Close();
                            chunkCache.Dispose();
                        }
                        chunkCache = new MemoryStream();
                        currentChunk = c;
                        packageFile.JumpTo(chunk.comprOffset);
                        uint blockTag = packageFile.ReadUInt32(); // block tag
                        if (blockTag != packageTag)
                            throw new Exception("not match");
                        uint blockSize = packageFile.ReadUInt32(); // max block size
                        if (blockSize != maxBlockSize)
                            throw new Exception("not match");
                        uint compressedChunkSize = packageFile.ReadUInt32(); // compressed chunk size
                        uint uncompressedChunkSize = packageFile.ReadUInt32();
                        if (uncompressedChunkSize != chunk.uncomprSize)
                            throw new Exception("not match");

                        uint blocksCount = (uncompressedChunkSize + maxBlockSize - 1) / maxBlockSize;
                        if ((compressedChunkSize + SizeOfChunk + SizeOfChunkBlock * blocksCount) != chunk.comprSize)
                            throw new Exception("not match");

                        List<ChunkBlock> blocks = new List<ChunkBlock>();
                        for (uint b = 0; b < blocksCount; b++)
                        {
                            ChunkBlock block = new ChunkBlock();
                            block.comprSize = packageFile.ReadUInt32();
                            block.uncomprSize = packageFile.ReadUInt32();
                            blocks.Add(block);
                        }
                        chunk.blocks = blocks;

                        for (int b = 0; b < blocks.Count; b++)
                        {
                            ChunkBlock block = blocks[b];
                            block.compressedBuffer = packageFile.ReadToBuffer(block.comprSize);
                            block.uncompressedBuffer = new byte[maxBlockSize * 2];
                            blocks[b] = block;
                        }

                        if (compressionType == CompressionType.LZO)
                        {
                            for (int b = 0; b < blocks.Count; b++)
                            {
                                uint dstLen;
                                ChunkBlock block = blocks[b];
                                dstLen = new LZO2Helper.LZO2().Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
                                if (dstLen != block.uncomprSize)
                                    throw new Exception("Decompressed data size not expected!");
                            }
                        }
                        else if (compressionType == CompressionType.Zlib)
                        {
                            Parallel.For(0, blocks.Count, b =>
                            {
                                uint dstLen = 0;
                                ChunkBlock block = blocks[b];
                                dstLen = new ZlibHelper.Zlib().Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
                                if (dstLen != block.uncomprSize)
                                    throw new Exception("Decompressed data size not expected!");
                            });
                        }
                        else
                            throw new Exception("Compression type not expected!");

                        for (int b = 0; b < blocks.Count; b++)
                        {
                            chunkCache.Write(blocks[b].uncompressedBuffer, 0, (int)blocks[b].uncomprSize);
                        }
                    }
                    chunkCache.JumpTo(startInChunk);
                    output.WriteFromStream(chunkCache, bytesLeftInChunk);
                    bytesLeft -= bytesLeftInChunk;
                    if (bytesLeft == 0)
                        break;
                }
            }
            else
            {
                packageFile.JumpTo(offset);
                output.WriteFromStream(packageFile, length);
            }
        }

        public byte[] getExportData(int id)
        {
            if (exportsTable[id].updatedData)
            {
                string exportFile = packagePath + "-exports\\exportId-" + id;
                using (FileStream fs = new FileStream(exportFile, FileMode.Open, FileAccess.Read))
                {
                    return fs.ReadToBuffer(fs.Length);
                }
            }
            if (exportsTable[id].newData != null)
            {
                return exportsTable[id].newData;
            }
            using (MemoryStream data = new MemoryStream())
            {
                getData(exportsTable[id].dataOffset, exportsTable[id].dataSize, data);
                return data.ToArray();
            }
        }

        public void setExportData(int id, byte[] data)
        {
            ExportEntry export = exportsTable[id];
            if (data.Length > export.dataSize)
            {
                export.dataOffset = exportsEndOffset;
                exportsEndOffset = export.dataOffset + (uint)data.Length;
            }
            export.dataSize = (uint)data.Length;

            if (memoryMode)
            {
                export.newData = new byte[data.Length];
                Array.Copy(data, export.newData, data.Length);
            }
            else
            {
                string packageDir = Path.GetDirectoryName(packagePath);
                Directory.CreateDirectory(packagePath + "-exports");
                string exportFile = packagePath + "-exports\\exportId-" + id;
                if (File.Exists(exportFile))
                    File.Delete(exportFile);
                using (FileStream fs = new FileStream(exportFile, FileMode.Create, FileAccess.Write))
                {
                    fs.WriteFromBuffer(data);
                }
                export.updatedData = true;
            }

            exportsTable[id] = export;
            modified = true;
        }

        public int getNameId(string name)
        {
            int i = namesTable.FindIndex(s => s.name == name);
            if (i == -1)
                throw new Exception();
            else
                return i;
        }

        public bool existsNameId(string name)
        {
            return namesTable.Exists(s => s.name == name);
        }

        public string getName(int id)
        {
            if (id >= namesTable.Count)
                throw new Exception();
            return namesTable[id].name;
        }

        private void loadNames(Stream input)
        {
            namesTable = new List<NameEntry>();
            input.JumpTo(namesOffset);
            for (int i = 0; i < namesCount; i++)
            {
                NameEntry entry = new NameEntry();
                int len = input.ReadInt32();
                if (len < 0) // unicode
                {
                    byte[] str = input.ReadToBuffer(-len * 2);
                    if (version == packageFileVersionME3)
                    {
                        entry.name = Encoding.Unicode.GetString(str);
                    }
                    else
                    {
                        for (int c = 0; c < -len; c++)
                        {
                            entry.name += (char)str[c * 2];
                        }
                    }
                }
                else
                {
                    entry.name = input.ReadStringASCII(len);
                }
                entry.name = entry.name.Trim('\0');

                if (nameIdTexture2D == -1 && entry.name == "Texture2D")
                    nameIdTexture2D = i;
                else if (nameIdLightMapTexture2D == -1 && entry.name == "LightMapTexture2D")
                    nameIdLightMapTexture2D = i;
                else if (nameIdShadowMapTexture2D == -1 && entry.name == "ShadowMapTexture2D")
                    nameIdShadowMapTexture2D = i;
                else if (nameIdTextureFlipBook == -1 && entry.name == "TextureFlipBook")
                    nameIdTextureFlipBook = i;

                if (version == packageFileVersionME1)
                    entry.flags = input.ReadUInt64();
                if (version == packageFileVersionME2)
                    entry.flags = input.ReadUInt32();

                namesTable.Add(entry);
            }
            namesTableEnd = (uint)input.Position;
        }

        private void saveNames(Stream output, bool mod = false)
        {
            if (!mod)
            {
                packageFile.JumpTo(namesOffset);
                output.WriteFromStream(packageFile, namesTableEnd - namesOffset);
            }
            else
            {
                for (int i = 0; i < namesTable.Count; i++)
                {
                    NameEntry entry = namesTable[i];
                    if (packageFileVersion == packageFileVersionME3)
                    {
                        output.WriteInt32(-(entry.name.Length + 1));
                        output.WriteStringUnicodeNull(entry.name);
                    }
                    else
                    {
                        output.WriteInt32(entry.name.Length + 1);
                        output.WriteStringASCIINull(entry.name);
                    }
                    if (version == packageFileVersionME1)
                        output.WriteUInt64(entry.flags);
                    if (version == packageFileVersionME2)
                        output.WriteUInt32((uint)entry.flags);
                }
            }
        }

        private void loadExtraNames(Stream input, bool rawMode = true)
        {
            extraNamesTable = new List<ExtraNameEntry>();
            uint extraNamesCount = input.ReadUInt32();
            for (int c = 0; c < extraNamesCount; c++)
            {
                ExtraNameEntry entry = new ExtraNameEntry();
                int len = input.ReadInt32();
                if (rawMode)
                {
                    if (len < 0)
                    {
                        entry.raw = input.ReadToBuffer(-len * 2);
                    }
                    else
                    {
                        entry.raw = input.ReadToBuffer(len);
                    }
                }
                else
                {
                    string name;
                    if (len < 0)
                    {
                        name = input.ReadStringUnicode(-len * 2);
                    }
                    else
                    {
                        name = input.ReadStringASCII(len);
                    }
                    name = name.Trim('\0');
                    entry.name = name;
                }
                extraNamesTable.Add(entry);
            }
        }

        private void saveExtraNames(Stream output, bool rawMode = true)
        {
            output.WriteInt32(extraNamesTable.Count);
            for (int c = 0; c < extraNamesTable.Count; c++)
            {
                if (rawMode)
                {
                    if (packageFileVersion == packageFileVersionME3)
                        output.WriteInt32(-(extraNamesTable[c].raw.Length / 2));
                    else
                        output.WriteInt32(extraNamesTable[c].raw.Length);
                    output.WriteFromBuffer(extraNamesTable[c].raw);
                }
                else
                {
                    if (packageFileVersion == packageFileVersionME3)
                    {
                        output.WriteInt32(-(extraNamesTable[c].name.Length + 1));
                        output.WriteStringUnicodeNull(extraNamesTable[c].name);
                    }
                    else
                    {
                        output.WriteInt32(extraNamesTable[c].name.Length + 1);
                        output.WriteStringASCIINull(extraNamesTable[c].name);
                    }
                }
            }
        }

        private void loadImports(Stream input)
        {
            importsTable = new List<ImportEntry>();
            input.JumpTo(importsOffset);
            for (int i = 0; i < importsCount; i++)
            {
                ImportEntry entry = new ImportEntry();

                long start = input.Position;
                input.SkipInt32(); // entry.packageFileId = input.ReadInt32(); // not used, save RAM
                //entry.packageFile = namesTable[packageFileId].name; // not used, save RAM
                input.SkipInt32(); // const 0
                entry.classId = input.ReadInt32();
                input.SkipInt32(); // const 0
                entry.linkId = input.ReadInt32();
                entry.objectNameId = input.ReadInt32();
                entry.objectName = namesTable[entry.objectNameId].name;
                input.SkipInt32();

                long len = input.Position - start;
                input.JumpTo(start);
                entry.raw = input.ReadToBuffer((int)len);

                importsTable.Add(entry);
            }
            importsTableEnd = (uint)input.Position;
        }

        private void loadImportsNames()
        {
            for (int i = 0; i < importsCount; i++)
            {
                ImportEntry entry = importsTable[i];
                //entry.className = getClassName(entry.classId); // disabled for now - save RAM
                importsTable[i] = entry;
            }
        }

        private void saveImports(Stream output, bool mod = false)
        {
            if (!mod)
            {
                packageFile.JumpTo(importsOffset);
                output.WriteFromStream(packageFile, importsTableEnd - importsOffset);
            }
            else
            {
                for (int i = 0; i < importsTable.Count; i++)
                {
                    output.WriteFromBuffer(importsTable[i].raw);
                }
            }
        }

        private void loadExports(Stream input)
        {
            exportsTable = new List<ExportEntry>();
            input.JumpTo(exportsOffset);
            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry entry = new ExportEntry();

                long start = input.Position;
                input.Skip(ExportEntry.DataOffsetOffset + 4);
                if (version != packageFileVersionME3)
                {
                    input.Skip(input.ReadUInt32() * 12); // skip entries
                }
                input.SkipInt32();
                input.Skip(input.ReadUInt32() * 4 + 16 + 4); // skip entries + skip guid + some

                long len = input.Position - start;
                input.JumpTo(start);
                entry.raw = input.ReadToBuffer((int)len);

                if ((entry.dataOffset + entry.dataSize) > exportsEndOffset)
                    exportsEndOffset = entry.dataOffset + entry.dataSize;

                entry.objectName = namesTable[entry.objectNameId].name;
                entry.id = (uint)i;
                exportsTable.Add(entry);
            }
        }

        private void loadExportsNames()
        {
            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry entry = exportsTable[i];
                //entry.className = getClassName(entry.classId); // disabled for now - save RAM
                exportsTable[i] = entry;
            }
        }

        private void saveExports(Stream output)
        {
            for (int i = 0; i < exportsTable.Count; i++)
            {
                output.WriteFromBuffer(exportsTable[i].raw);
            }
        }

        private void loadDepends(Stream input)
        {
            dependsTable = new List<int>();
            input.JumpTo(dependsOffset);
            for (int i = 0; i < exportsCount; i++)
                dependsTable.Add(input.ReadInt32());
        }

        private void saveDepends(Stream output)
        {
            for (int i = 0; i < exportsTable.Count; i++)
                output.WriteInt32(dependsTable[i]);
        }

        private void loadGuids(Stream input)
        {
            guidsTable = new List<GuidEntry>();
            input.JumpTo(guidsOffset);
            for (int i = 0; i < guidsCount; i++)
            {
                GuidEntry entry = new GuidEntry();
                entry.guid = input.ReadToBuffer(16);
                entry.index = input.ReadInt32();
                guidsTable.Add(entry);
            }
        }

        private void saveGuids(Stream output)
        {
            for (int i = 0; i < guidsTable.Count; i++)
            {
                GuidEntry entry = guidsTable[i];
                output.WriteFromBuffer(entry.guid);
                output.WriteInt32(entry.index);
            }
        }

        public bool SaveToFile(bool forceZlib = false, bool forceCompressed = false, bool forceDecompressed = false, string filename = null)
        {
            if (forceZlib && compressionType != CompressionType.Zlib)
                modified = true;

            // WA Allow force back to LZO2
            if (forceZlib && packageFileVersion == packageFileVersionME1)
                modified = true;

            if (packageFile.Length == 0 || !modified && !forceDecompressed && !forceCompressed)
                return false;

            if (forceCompressed && forceDecompressed)
                throw new Exception("force de/compression can't be both enabled!");

            MemoryStream tempOutput = new MemoryStream();

            List<ExportEntry> sortedExports = exportsTable.OrderBy(s => s.dataOffset).ToList();

            if (!compressed)
            {
                packageFile.SeekBegin();
                tempOutput.WriteFromStream(packageFile, sortedExports[0].dataOffset);
            }
            else
            {
                packageData.SeekBegin();
                tempOutput.WriteFromStream(packageData, packageData.Length);
            }

            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry export = sortedExports[i];
                uint dataLeft;
                tempOutput.JumpTo(export.dataOffset);
                if (i + 1 == exportsCount)
                    dataLeft = exportsEndOffset - export.dataOffset - export.dataSize;
                else
                    dataLeft = sortedExports[i + 1].dataOffset - export.dataOffset - export.dataSize;
                if (export.updatedData)
                {
                    string exportFile = packagePath + "-exports\\exportId-" + export.id;
                    using (FileStream fs = new FileStream(exportFile, FileMode.Open, FileAccess.Read))
                    {
                        tempOutput.WriteFromStream(fs, fs.Length);
                    }
                    export.updatedData = false;
                }
                else if (export.newData != null)
                {
                    tempOutput.WriteFromBuffer(export.newData);
                }
                else
                {
                    getData(export.dataOffset, export.dataSize, tempOutput);
                }
                tempOutput.WriteZeros(dataLeft);
            }

            if (exportsOffset > sortedExports[0].dataOffset)
            {
                if (compressed) // allowed only uncompressed
                    throw new Exception();
                exportsOffset = (uint)tempOutput.Position;
                saveExports(tempOutput);
                exportsEndOffset = (uint)tempOutput.Position;
            }
            else
            {
                tempOutput.JumpTo(exportsOffset);
                saveExports(tempOutput);
            }

            tempOutput.JumpTo(exportsEndOffset);
            if (namesOffset > sortedExports[0].dataOffset)
            {
                if (compressed) // allowed only uncompressed
                    throw new Exception();
                namesOffset = (uint)tempOutput.Position;
                saveNames(tempOutput, true);
            }
            else
            {
                saveNames(tempOutput);
            }

            if (importsOffset > sortedExports[0].dataOffset)
            {
                if (compressed) // allowed only uncompressed
                    throw new Exception();
                importsOffset = (uint)tempOutput.Position;
                saveImports(tempOutput, true);
            }
            else
            {
                saveImports(tempOutput);
            }

            if (forceDecompressed && compressed)
                compressed = false;
            else
                forceDecompressed = false;

            if (forceCompressed && !compressed)
            {
                if (namesOffset < sortedExports[0].dataOffset &&
                    importsOffset < sortedExports[0].dataOffset &&
                    exportsOffset < sortedExports[0].dataOffset &&
                    endOfTablesOffset <= sortedExports[0].dataOffset)
                {
                    if (compressionType == CompressionType.None)
                    {
                        if (packageFileVersion == packageFileVersionME3)
                            compressionType = CompressionType.Zlib;
                        else
                            compressionType = CompressionType.LZO;
                    }
                    compressed = true;
                }
                else
                    forceCompressed = false;
            }

            // WA Force back to LZO2 compression
            if (packageFileVersion == packageFileVersionME1 && compressionType == CompressionType.Zlib)
            {
                compressionType = CompressionType.LZO;
                forceCompressed = true;
            }

            if (namesOffset > sortedExports[0].dataOffset ||
                importsOffset > sortedExports[0].dataOffset ||
                exportsOffset > sortedExports[0].dataOffset ||
                forceDecompressed || forceCompressed)
            {
                tempOutput.SeekBegin();
                tempOutput.Write(packageHeader, 0, packageHeader.Length);
            }
            packageFile.Close();
            if (!memoryMode && Directory.Exists(packagePath + "-exports"))
                Directory.Delete(packagePath + "-exports", true);

            if (filename == null)
                filename = packageFile.Name;
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                if (fs == null)
                    throw new Exception("Failed to write to file: " + filename);

                if (!compressed)
                {
                    tempOutput.SeekBegin();
                    fs.WriteFromStream(tempOutput, tempOutput.Length);
                }
                else
                {
                    if (chunks == null)
                        chunks = new List<Chunk>();
                    chunks.Clear();
                    Chunk chunk = new Chunk();
                    chunk.uncomprSize = sortedExports[0].dataOffset - (uint)dataOffset;
                    chunk.uncomprOffset = (uint)dataOffset;
                    for (int i = 0; i < exportsCount; i++)
                    {
                        ExportEntry export = sortedExports[i];
                        uint dataSize;
                        if (i + 1 == exportsCount)
                            dataSize = exportsEndOffset - export.dataOffset;
                        else
                            dataSize = sortedExports[i + 1].dataOffset - export.dataOffset;
                        if (chunk.uncomprSize + dataSize > maxChunkSize)
                        {
                            uint offset = chunk.uncomprOffset + chunk.uncomprSize;
                            chunks.Add(chunk);
                            chunk = new Chunk();
                            chunk.uncomprSize = dataSize;
                            chunk.uncomprOffset = offset;
                        }
                        else
                        {
                            chunk.uncomprSize += dataSize;
                        }
                    }
                    chunks.Add(chunk);

                    if (forceZlib)
                        compressionType = CompressionType.Zlib; // override compression type to Zlib
                    fs.Write(packageHeader, 0, packageHeader.Length);
                    fs.WriteUInt32((uint)compressionType);
                    fs.WriteUInt32((uint)chunks.Count);
                    uint chunksTableOffset = (uint)fs.Position;
                    fs.Skip(SizeOfChunk * chunks.Count); // skip chunks table - filled later
                    fs.WriteUInt32(someTag);
                    if (version == packageFileVersionME2)
                        fs.WriteUInt32(0); // const 0
                    saveExtraNames(fs);

                    for (int c = 0; c < chunks.Count; c++)
                    {
                        chunk = chunks[c];
                        chunk.comprOffset = (uint)fs.Position;
                        chunk.comprSize = 0; // filled later

                        uint dataBlockLeft = chunk.uncomprSize;
                        uint newNumBlocks = (chunk.uncomprSize + maxBlockSize - 1) / maxBlockSize;
                        // skip blocks header and table - filled later
                        fs.Seek(SizeOfChunk + SizeOfChunkBlock * newNumBlocks, SeekOrigin.Current);

                        tempOutput.JumpTo(chunk.uncomprOffset);

                        chunk.blocks = new List<ChunkBlock>();
                        for (int b = 0; b < newNumBlocks; b++)
                        {
                            ChunkBlock block = new ChunkBlock();
                            block.uncomprSize = Math.Min(maxBlockSize, dataBlockLeft);
                            dataBlockLeft -= block.uncomprSize;
                            block.uncompressedBuffer = tempOutput.ReadToBuffer(block.uncomprSize);
                            chunk.blocks.Add(block);
                        }

                        if (compressionType == CompressionType.LZO)
                        {
                            for (int b = 0; b < newNumBlocks; b++)
                            {
                                ChunkBlock block = chunk.blocks[b];
                                block.compressedBuffer = new LZO2Helper.LZO2().Compress(block.uncompressedBuffer);
                                if (block.compressedBuffer.Length == 0)
                                    throw new Exception("Compression failed!");
                                block.comprSize = (uint)block.compressedBuffer.Length;
                                chunk.blocks[b] = block;
                            }
                        }
                        else if (compressionType == CompressionType.Zlib)
                        {
                            Parallel.For(0, chunk.blocks.Count, b =>
                            {
                                ChunkBlock block = chunk.blocks[b];
                                block.compressedBuffer = new ZlibHelper.Zlib().Compress(block.uncompressedBuffer);
                                if (block.compressedBuffer.Length == 0)
                                    throw new Exception("Compression failed!");
                                block.comprSize = (uint)block.compressedBuffer.Length;
                                chunk.blocks[b] = block;
                            });
                        }
                        else
                            throw new Exception("Compression type not expected!");

                        for (int b = 0; b < newNumBlocks; b++)
                        {
                            ChunkBlock block = chunk.blocks[b];
                            fs.Write(block.compressedBuffer, 0, (int)block.comprSize);
                            chunk.comprSize += block.comprSize;
                        }
                        chunks[c] = chunk;
                    }

                    for (int c = 0; c < chunks.Count; c++)
                    {
                        chunk = chunks[c];
                        fs.JumpTo(chunksTableOffset + c * SizeOfChunk); // jump to chunks table
                        fs.WriteUInt32(chunk.uncomprOffset);
                        fs.WriteUInt32(chunk.uncomprSize);
                        fs.WriteUInt32(chunk.comprOffset);
                        fs.WriteUInt32(chunk.comprSize + SizeOfChunk + SizeOfChunkBlock * (uint)chunk.blocks.Count);
                        fs.JumpTo(chunk.comprOffset); // jump to blocks header
                        fs.WriteUInt32(packageTag);
                        fs.WriteUInt32(maxBlockSize);
                        fs.WriteUInt32(chunk.comprSize);
                        fs.WriteUInt32(chunk.uncomprSize);
                        foreach (ChunkBlock block in chunk.blocks)
                        {
                            fs.WriteUInt32(block.comprSize);
                            fs.WriteUInt32(block.uncomprSize);
                        }
                    }
                    for (int c = 0; c < chunks.Count; c++)
                    {
                        chunk = chunks[c];
                        chunk.blocks.Clear();
                        chunk.blocks = null;
                    }
                    chunks.Clear();
                    chunks = null;
                }

                fs.SeekEnd();
                fs.WriteStringASCII(MEMendFileMarker);
            }

            tempOutput.Close();
            tempOutput.Dispose();

            return true;
        }

        public void DisposeCache()
        {
            if (chunkCache != null)
                chunkCache.Dispose();
            chunkCache = null;
            currentChunk = -1;
        }

        public void Dispose()
        {
            if (chunkCache != null)
                chunkCache.Dispose();
            if (packageData != null)
            {
                packageData.Close();
                packageData.Dispose();
            }
            packageFile.Close();
            packageFile.Dispose();
            if (!memoryMode && modified && Directory.Exists(packagePath + "-exports"))
                Directory.Delete(packagePath + "-exports", true);
        }
    }
}
