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

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using StreamHelpers;
using System.Linq;

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
        MemoryStream packageData;
        List<Chunk> chunks;
        List<NameEntry> namesTable;
        List<ImportEntry> importsTable;
        public List<ExportEntry> exportsTable;
        List<string> extraNamesTable;
        int currentChunk = -1;
        MemoryStream chunkCache;
        public int nameIdTexture2D = -1;
        public int nameIdLightMapTexture2D = -1;
        public int nameIdShadowMapTexture2D = -1;
        public int nameIdTextureFlipBook = -1;

        const int SizeOfChunkBlock = 8;
        public struct ChunkBlock
        {
            public uint comprSize;
            public uint uncomprSize;
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
            public int packageFileId;
            public string packageFile;
            public int classId;
            public string className;
            public int linkId;
            public int objectNameId;
            public string objectName;
            public byte[] raw;
        }
        public struct ExportEntry
        {
            const int DataOffsetSize = 32;
            const int DataOffsetOffset = 36;
            public int classId;
            public string className;
            public int classParentId;
            public int linkId;
            public int objectNameId;
            public string objectName;
            public int suffixNameId;
            public int archTypeNameId;
            public uint dataSize
            {
                get
                {
                    return BitConverter.ToUInt32(raw, DataOffsetSize);
                }
                set
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, raw, DataOffsetSize, sizeof(uint));
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
            public ulong objectFlags;
            public uint exportflags;
            public uint packageflags;
            public byte[] raw;
            public byte[] newData;
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
        }

        public bool compressed
        {
            get
            {
                return (flags & (uint)PackageFlags.compressed) != 0;
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
        }

        private uint namesOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderNamesOffsetTabletsOffset);
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

        private string getClassName(int id)
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

        public Package(string filename, bool headerOnly = false)
        {
            if (GameData.gameType == MeType.ME1_TYPE)
            {
                packageHeaderSize = packageHeaderSizeME1;
                packageFileVersion = packageFileVersionME1;
            }
            else if (GameData.gameType == MeType.ME2_TYPE)
            {
                packageHeaderSize = packageHeaderSizeME2;
                packageFileVersion = packageFileVersionME2;
            }
            else if (GameData.gameType == MeType.ME3_TYPE)
            {
                packageHeaderSize = packageHeaderSizeME3;
                packageFileVersion = packageFileVersionME3;
            }

            if (!File.Exists(filename))
                throw new Exception("File not found: " + filename);

            packageData = new MemoryStream();
            packageFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            packageHeader = packageFile.ReadToBuffer(packageHeaderSize);
            if (tag != packageTag)
                throw new Exception("Wrong PCC tag");

            if (version != packageFileVersion)
                throw new Exception("Wrong PCC version");

            if (headerOnly)
                return;

            compressionType = (CompressionType)packageFile.ReadUInt32();

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
            var filePos = packageFile.Position;
            someTag = packageFile.ReadUInt32();
            if (version == packageFileVersionME2)
                packageFile.SkipInt32(); // const 0

            loadExtraNames(packageFile);

            if (compressed && packageFile.Position != chunks[0].comprOffset)
                throw new Exception();

            dataOffset += packageFile.Position - filePos;

            if (compressed && dataOffset != chunks[0].uncomprOffset)
                throw new Exception();

            uint length = endOfTablesOffset - (uint)dataOffset;
            packageData.JumpTo(dataOffset);
            getData((uint)dataOffset, length, packageData);

            loadNames(packageData);
            loadImports(packageData);
            loadExports(packageData);
            loadImportsNames();
            loadExportsNames();
        }

        private void getData(uint offset, uint length, MemoryStream output)
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
                        chunks[c] = chunk;

                        for (int b = 0; b < blocks.Count; b++)
                        {
                            ChunkBlock block = blocks[b];
                            byte[] dst = new byte[block.uncomprSize];
                            byte[] src = packageFile.ReadToBuffer(block.comprSize);
                            uint dstLen;
                            if (compressionType == CompressionType.LZO)
                                dstLen = LZO2Helper.LZO2.Decompress(src, block.comprSize, dst);
                            else if (compressionType == CompressionType.Zlib)
                                dstLen = ZlibHelper.Zlib.Decompress(src, block.comprSize, dst);
                            else
                                throw new Exception("Compression type not expected!");

                            if (dstLen != block.uncomprSize)
                                throw new Exception("Decompressed data size not expected!");

                            chunkCache.WriteFromBuffer(dst);
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
            if (exportsTable[id].newData != null)
                return exportsTable[id].newData;
            MemoryStream data = new MemoryStream();
            getData(exportsTable[id].dataOffset, exportsTable[id].dataSize, data);
            return data.ToArray();
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
            export.newData = data;
            exportsTable[id] = export;
        }

        public int getNameId(string name)
        {
            for (int i = 0; i < namesTable.Count; i++)
                if (namesTable[i].name == name)
                    return i;

            throw new Exception();
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
                    var str = input.ReadToBuffer(-len * 2);
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
        }

        private void saveNames(Stream output)
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

        private void loadExtraNames(Stream input)
        {
            extraNamesTable = new List<string>();
            uint extraNamesCount = input.ReadUInt32();
            for (int c = 0; c < extraNamesCount; c++)
            {
                int len = input.ReadInt32();
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
                extraNamesTable.Add(name);
            }
        }

        private void saveExtraNames(Stream output)
        {
            output.WriteInt32(extraNamesTable.Count);
            for (int c = 0; c < extraNamesTable.Count; c++)
            {
                if (packageFileVersion == packageFileVersionME3)
                {
                    output.WriteInt32(-(extraNamesTable[c].Length + 1));
                    output.WriteStringUnicodeNull(extraNamesTable[c]);
                }
                else
                {
                    output.WriteInt32(extraNamesTable[c].Length + 1);
                    output.WriteStringASCIINull(extraNamesTable[c]);
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

                var start = input.Position;
                entry.packageFileId = input.ReadInt32();
                entry.packageFile = namesTable[entry.packageFileId].name;
                input.SkipInt32(); // const 0
                entry.classId = input.ReadInt32();
                input.SkipInt32(); // const 0
                entry.linkId = input.ReadInt32();
                entry.objectNameId = input.ReadInt32();
                entry.objectName = namesTable[entry.objectNameId].name;
                input.SkipInt32();

                var len = input.Position - start;
                input.JumpTo(start);
                entry.raw = input.ReadToBuffer((int)len);

                importsTable.Add(entry);
            }
        }

        private void loadImportsNames()
        {
            for (int i = 0; i < importsCount; i++)
            {
                ImportEntry entry = importsTable[i];
                entry.className = getClassName(entry.classId);
                importsTable[i] = entry;
            }
        }

        private void loadExports(Stream input)
        {
            exportsTable = new List<ExportEntry>();
            input.JumpTo(exportsOffset);
            for (int i = 0; i < exportsCount; i++)
            {
                uint count;
                ExportEntry entry = new ExportEntry();

                var start = input.Position;
                entry.classId = input.ReadInt32();
                entry.classParentId = input.ReadInt32();
                entry.linkId = input.ReadInt32();
                entry.objectNameId = input.ReadInt32();
                entry.objectName = namesTable[entry.objectNameId].name;
                entry.suffixNameId = input.ReadInt32();
                input.SkipInt32();
                entry.objectFlags = input.ReadUInt64();
                input.SkipInt32(); // dataSize
                input.SkipInt32(); // dataOffset
                if (version != packageFileVersionME3)
                {
                    count = input.ReadUInt32();
                    input.Skip(count * 12); // skip entries
                }
                input.ReadUInt32();
                count = input.ReadUInt32();
                input.Skip(count * 4); // skip entries
                input.Skip(16); // skip guid
                input.SkipInt32();

                var len = input.Position - start;
                input.JumpTo(start);
                entry.raw = input.ReadToBuffer((int)len);

                if ((entry.dataOffset + entry.dataSize) > exportsEndOffset)
                    exportsEndOffset = entry.dataOffset + entry.dataSize;

                exportsTable.Add(entry);
            }
        }
        private void loadExportsNames()
        {
            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry entry = exportsTable[i];
                entry.className = getClassName(entry.classId);
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

        public bool SaveToFile(bool forceZlib = false)
        {
            if (packageFile.Length == 0)
                return false;

            MemoryStream tempOutput = new MemoryStream();

            if (!compressed)
            {
                packageFile.Begin();
                tempOutput.WriteFromStream(packageFile, endOfTablesOffset);
            }
            else
            {
                packageData.Begin();
                tempOutput.WriteFromStream(packageData, packageData.Length);
            }

            List<ExportEntry> sortedExports = exportsTable.OrderBy(s => s.dataOffset).ToList();

            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry export = sortedExports[i];
                uint dataLeft;
                tempOutput.JumpTo(export.dataOffset);
                if (i + 1 == exportsCount)
                    dataLeft = exportsEndOffset - export.dataOffset - export.dataSize;
                else
                    dataLeft = sortedExports[i + 1].dataOffset - export.dataOffset - export.dataSize;
                if (export.newData != null)
                    tempOutput.WriteFromBuffer(export.newData);
                else
                    getData(export.dataOffset, export.dataSize, tempOutput);
                tempOutput.WriteZeros(dataLeft);
            }

            tempOutput.JumpTo(exportsOffset);
            saveExports(tempOutput);
            packageFile.Close();

            string filename = packageFile.Name;
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                if (fs == null)
                    throw new Exception("Failed to write to file: " + filename);

                if (!compressed)
                {
                    tempOutput.Begin();
                    fs.WriteFromStream(tempOutput, tempOutput.Length);
                }
                else
                {
                    if (chunks == null)
                        chunks = new List<Chunk>();
                    chunks.Clear();
                    Chunk chunk = new Chunk();
                    chunk.uncomprSize = endOfTablesOffset - (uint)dataOffset;
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
                    var chunksTableOffset = (uint)fs.Position;
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
                            uint newBlockSize = Math.Min(maxBlockSize, dataBlockLeft);

                            byte[] dst;
                            byte[] src = tempOutput.ReadToBuffer(newBlockSize);
                            if (compressionType == CompressionType.LZO)
                                dst = LZO2Helper.LZO2.Compress(src, false);
                            else if (compressionType == CompressionType.Zlib)
                                dst = ZlibHelper.Zlib.Compress(src);
                            else
                                throw new Exception("Compression type not expected!");
                            if (dst.Length == 0)
                                throw new Exception("Compression failed!");

                            fs.Write(dst, 0, dst.Length);
                            block.uncomprSize = newBlockSize;
                            block.comprSize = (uint)dst.Length;
                            chunk.comprSize += block.comprSize;
                            chunk.blocks.Add(block);
                            dataBlockLeft -= newBlockSize;
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
                }
            }

            return true;
        }

        public void Dispose()
        {
            if (chunkCache != null)
                chunkCache.Dispose();
            packageData.Dispose();
            packageFile.Close();
        }
    }

    public class TOCBinFile
    {
        const uint TOCTag = 0x3AB70C13; // TOC tag
        const int TOCHeaderSize = 12;

        struct File
        {
            public ushort type;
            public uint size;
            public byte[] sha1;
            public string path;
        }

        struct Block
        {
            public uint filesOffset;
            public uint numFiles;
            public List<File> filesList;
        }
        List<Block> blockList;

        public TOCBinFile(string filename)
        {
            using (FileStream tocFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                uint tag = tocFile.ReadUInt32();
                if (tag != TOCTag)
                    throw new Exception("Wrong TOCTag tag");
                tocFile.SkipInt32();

                blockList = new List<Block>();
                uint numBlocks = tocFile.ReadUInt32();
                for (int b = 0; b < numBlocks; b++)
                {
                    Block block = new Block();
                    block.filesOffset = tocFile.ReadUInt32();
                    block.numFiles = tocFile.ReadUInt32();
                    block.filesList = new List<File>();
                    blockList.Add(block);
                }

                tocFile.JumpTo(TOCHeaderSize + (numBlocks * 8));
                for (int b = 0; b < numBlocks; b++)
                {
                    Block block = blockList[b];
                    File file = new File();
                    for (int f = 0; f < block.numFiles; f++)
                    {
                        long curPos = tocFile.Position;
                        ushort blockSize = tocFile.ReadUInt16();
                        file.type = tocFile.ReadUInt16();
                        if (file.type != 9 && file.type != 1)
                            throw new Exception();
                        file.size = tocFile.ReadUInt32();
                        file.sha1 = tocFile.ReadToBuffer(20);
                        file.path = tocFile.ReadStringASCIINull();
                        block.filesList.Add(file);
                        tocFile.JumpTo(curPos + blockSize);
                    }
                    blockList[b] = block;
                }
            }
        }

        byte[] calculateSHA1(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    sha1.Initialize();
                    return sha1.ComputeHash(fs);
                }
            }
        }

        public void updateFile(string filename, string filePath, bool updateSHA1 = true)
        {
            for (int b = 0; b < blockList.Count; b++)
            {
                for (int f = 0; f < blockList[b].numFiles; f++)
                {
                    File file = blockList[b].filesList[f];
                    if (file.path == filename)
                    {
                        file.size = (uint)new FileInfo(filePath).Length;
                        if (updateSHA1)
                            file.sha1 = calculateSHA1(filePath);
                        return;
                    }
                }
            }
            throw new Exception("not found");
        }

        public void saveToFile(string outPath, bool updateOffsets = false)
        {
            using (FileStream tocFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                tocFile.WriteUInt32(TOCTag);
                tocFile.WriteUInt32(0);
                tocFile.WriteUInt32((uint)blockList.Count);
                tocFile.Skip(8 * blockList.Count); // filled later

                long lastOffset = 0;
                for (int b = 0; b < blockList.Count; b++)
                {
                    Block block = blockList[b];
                    if (updateOffsets)
                        block.filesOffset = (uint)(tocFile.Position - TOCHeaderSize - (8 * b));
                    for (int f = 0; f < blockList[b].numFiles; f++)
                    {
                        long fileOffset = lastOffset = tocFile.Position;
                        File file = blockList[b].filesList[f];
                        int blockSize = ((28 + (file.path.Length + 1) + 3) / 4) * 4; // align to 4
                        tocFile.WriteUInt16((ushort)blockSize);
                        tocFile.WriteUInt16(file.type);
                        tocFile.WriteUInt32(file.size);
                        tocFile.WriteFromBuffer(file.sha1);
                        tocFile.WriteStringASCIINull(file.path);
                        tocFile.JumpTo(fileOffset + blockSize - 1);
                        tocFile.WriteByte(0); // make sure all bytes are written after seek
                    }
                    blockList[b] = block;
                }
                if (lastOffset != 0)
                {
                    tocFile.JumpTo(lastOffset);
                    tocFile.WriteUInt16(0);
                }

                tocFile.JumpTo(TOCHeaderSize);
                for (int b = 0; b < blockList.Count; b++)
                {
                    tocFile.WriteUInt32(blockList[b].filesOffset);
                    tocFile.WriteUInt32(blockList[b].numFiles);
                }
            }
        }

    }

}
