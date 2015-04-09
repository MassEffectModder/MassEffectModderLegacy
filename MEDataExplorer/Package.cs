/*
 * MEDataExplorer
 *
 * Copyright (C) 2014-2015 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using Gibbed.IO;

namespace MEDataExplorer
{
    public class Package : IDisposable
    {
        const UInt32 packageTag = 0x9E2A83C1;
        const UInt16 packageFileVersionME1 = 491;
        const UInt16 packageFileVersionME2 = 512;
        const UInt16 packageFileVersionME3 = 684;
        const UInt32 maxBlockSize = 0x20000; // 128KB
        const UInt32 maxChunkSize = 0x100000; // 1MB
        const UInt32 packageHeaderSizeME1 = 121;
        const UInt32 packageHeaderSizeME2 = 117;
        const UInt32 packageHeaderSizeME3 = 126;
        const Int32 sizeOfGeneration = 12;

        const Int32 packageHeaderTagOffset = 0;
        const Int32 packageHeaderVersionOffset = 4;
        const Int32 packageHeaderFirstChunkSizeOffset = 8;
        const Int32 packageHeaderNameSizeOffset = 12;

        const Int32 packageHeaderNamesCountTableOffset = 0;
        const Int32 packageHeaderNamesOffsetTabletsOffset = 4;
        const Int32 packageHeaderExportsCountTableOffset = 8;
        const Int32 packageHeaderExportsOffsetTableOffset = 12;
        const Int32 packageHeaderImportsCountTableOffset = 16;
        const Int32 packageHeaderImportsOffsetTableOffset = 20;
        const Int32 packageHeaderDependsOffsetTableOffset = 24;
        const Int32 packageHeaderGuidsOffsetTableOffset = 28;
        const Int32 packageHeaderGuidsCountTableOffset = 36;

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
        UInt32 packageHeaderSize;
        UInt32 packageFileVersion;
        UInt32 numChunks;
        UInt32 someTag;
        long dataOffset;
        CompressionType compressionType;
        FileStream packageFile;
        MemoryTributary packageData;
        List<Chunk> chunks;
        List<NameEntry> namesTable;
        List<ImportEntry> importsTable;
        List<ExportEntry> exportsTable;
        List<Int32> dependsTable;
        List<GuidEntry> guidsTable;
        List<string> extraNamesTable;
        Int32 currentChunk = -1;
        MemoryStream chunkCache;

        const int SizeOfChunkBlock = 8;
        public struct ChunkBlock
        {
            public UInt32 comprSize;
            public UInt32 uncomprSize;
        }

        const int SizeOfChunk = 16;
        public struct Chunk
        {
            public UInt32 uncomprOffset;
            public UInt32 uncomprSize;
            public UInt32 comprOffset;
            public UInt32 comprSize;
            public List<ChunkBlock> blocks;
        }

        public struct NameEntry
        {
            public string name;
            public UInt64 flags;
        }
        public struct ImportEntry
        {
            public Int32 packageFileId;
            public Int32 classNameId;
            public Int32 linkId;
            public Int32 objectNameId;
            public byte[] raw;
        }
        public struct ExportEntry
        {
            const int DataOffsetSize = 32;
            const int DataOffsetOffset = 36;
            public Int32 classNameId;
            public Int32 classParentId;
            public Int32 linkId;
            public Int32 objectNameId;
            public Int32 suffixNameId;
            public Int32 archTypeNameId;
            public UInt32 dataSize
            {
                get
                {
                    return BitConverter.ToUInt32(raw, DataOffsetSize);
                }
                set
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, raw, DataOffsetSize, sizeof(UInt32));
                }
            }
            public UInt32 dataOffset
            {
                get
                {
                    return BitConverter.ToUInt32(raw, DataOffsetOffset);
                }
                set
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, raw, DataOffsetOffset, sizeof(UInt32));
                }
            }
            public UInt64 objectFlags;
            public UInt32 exportflags;
            public UInt32 packageflags;
            public byte[] raw;
            public byte[] newData;
        }
        public struct GuidEntry
        {
            public byte[] guid;
            public Int32 index;
        }

        private UInt32 tag
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, packageHeaderTagOffset);
            }
        }

        private UInt16 version
        {
            get
            {
                return BitConverter.ToUInt16(packageHeader, packageHeaderVersionOffset);
            }
        }

        private UInt32 endOfTablesOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, packageHeaderFirstChunkSizeOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, packageHeaderFirstChunkSizeOffset, sizeof(UInt32));
            }
        }

        private Int32 packageHeaderFlagsOffset
        {
            get
            {
                Int32 len = BitConverter.ToInt32(packageHeader, packageHeaderNameSizeOffset);
                if (len < 0)
                    return (len * -2) + packageHeaderNameSizeOffset + sizeof(UInt32); // Unicode name
                else
                    return len + packageHeaderNameSizeOffset + sizeof(UInt32); // Ansi name
            }
        }

        private UInt32 flags
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, packageHeaderFlagsOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, packageHeaderFlagsOffset, sizeof(UInt32));
            }
        }

        private bool compressed
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
                Buffer.BlockCopy(BitConverter.GetBytes(flags), 0, packageHeader, packageHeaderFlagsOffset, sizeof(UInt32));
            }
        }

        private Int32 tablesOffset
        {
            get
            {
                if (version == packageFileVersionME3)
                    return packageHeaderFlagsOffset + sizeof(UInt32) + sizeof(UInt32); // additional entry in header
                else
                    return packageHeaderFlagsOffset + sizeof(UInt32);
            }
        }

        private UInt32 namesCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderNamesCountTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderNamesCountTableOffset, sizeof(UInt32));
            }
        }

        private UInt32 namesOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderNamesOffsetTabletsOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderNamesOffsetTabletsOffset, sizeof(UInt32));
            }
        }

        private UInt32 exportsCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderExportsCountTableOffset);
            }
        }

        private UInt32 exportsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderExportsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderExportsOffsetTableOffset, sizeof(UInt32));
            }
        }

        private UInt32 importsCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderImportsCountTableOffset);
            }
        }

        private UInt32 importsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderImportsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderImportsOffsetTableOffset, sizeof(UInt32));
            }
        }

        private UInt32 dependsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderDependsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderDependsOffsetTableOffset, sizeof(UInt32));
            }
        }

        private UInt32 guidsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderGuidsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderGuidsOffsetTableOffset, sizeof(UInt32));
            }
        }

        private UInt32 guidsCount
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderGuidsCountTableOffset);
            }
        }

        public Package(MeType gameType, string filename)
        {
            if (gameType == MeType.ME1_TYPE)
            {
                packageHeaderSize = packageHeaderSizeME1;
                packageFileVersion = packageFileVersionME1;
            }
            else if (gameType == MeType.ME2_TYPE)
            {
                packageHeaderSize = packageHeaderSizeME2;
                packageFileVersion = packageFileVersionME2;
            }
            else if (gameType == MeType.ME3_TYPE)
            {
                packageHeaderSize = packageHeaderSizeME3;
                packageFileVersion = packageFileVersionME3;
            }

            if (!File.Exists(filename))
                throw new Exception("File not found: " + filename);

            packageData = new MemoryTributary();
            packageFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            packageHeader = packageFile.ReadBytes(packageHeaderSize);
            if (tag != packageTag)
                throw new Exception("Wrong PCC tag");

            if (version != packageFileVersion)
                throw new Exception("Wrong PCC version");

            compressionType = (CompressionType)packageFile.ReadValueU32();
            numChunks = packageFile.ReadValueU32();

            dataOffset = packageFile.Position;

            if (compressed)
            {
                chunks = new List<Chunk>();
                for (int i = 0; i < numChunks; i++)
                {
                    Chunk chunk = new Chunk();
                    chunk.uncomprOffset = packageFile.ReadValueU32();
                    chunk.uncomprSize = packageFile.ReadValueU32();
                    chunk.comprOffset = packageFile.ReadValueU32();
                    chunk.comprSize = packageFile.ReadValueU32();
                    chunks.Add(chunk);
                }
            }

            var filePos = packageFile.Position;
            someTag = packageFile.ReadValueU32();
            if (version == packageFileVersionME2)
                packageFile.ReadValueU32(); // const 0

            loadExtraNames(packageFile);
            
            if (compressed && packageFile.Position != chunks[0].comprOffset)
                throw new Exception("wrong");

            dataOffset += packageFile.Position - filePos;

            if (compressed && dataOffset != chunks[0].uncomprOffset)
                throw new Exception("wrong");

            uint length = endOfTablesOffset - (uint)dataOffset;
            packageData.Seek(dataOffset, SeekOrigin.Begin);
            MemoryStream data = new MemoryStream();
            getData((uint)dataOffset, length, data);
            data.Seek(0, SeekOrigin.Begin);
            packageData.WriteFromStream(data, data.Length);

            if (endOfTablesOffset < namesOffset)
            {
                if (compressed) // allowed only uncompressed
                    throw new Exception("wrong");
                loadNames(packageFile);
            }
            else
            {
                loadNames(packageData);
            }
            loadImports(packageData);
            loadExports(packageData);
            loadDepends(packageData);
            if (version == packageFileVersionME3)
                loadGuids(packageData);
        }

        private void getData(UInt32 offset, UInt32 length, MemoryStream output)
        {
            if (compressed)
            {
                UInt32 bytesLeft = length;
                for (int c = 0; c < chunks.Count; c++)
                {
                    Chunk chunk = chunks[c];
                    if (chunk.uncomprOffset + chunk.uncomprSize <= offset)
                        continue;
                    UInt32 startInChunk;
                    if (offset < chunk.uncomprOffset)
                        startInChunk = 0;
                    else
                        startInChunk = offset - chunk.uncomprOffset;

                    UInt32 bytesLeftInChunk = Math.Min(chunk.uncomprSize - startInChunk, bytesLeft);
                    if (currentChunk != c)
                    {
                        if (chunkCache != null)
                        {
                            chunkCache.Close();
                            chunkCache.Dispose();
                        }
                        chunkCache = new MemoryStream();
                        currentChunk = c;
                        packageFile.Seek(chunk.comprOffset, SeekOrigin.Begin);
                        UInt32 blockTag = packageFile.ReadValueU32(); // block tag
                        if (blockTag != packageTag)
                            throw new Exception("not match");
                        UInt32 blockSize = packageFile.ReadValueU32(); // max block size
                        if (blockSize != maxBlockSize)
                            throw new Exception("not match");
                        UInt32 compressedChunkSize = packageFile.ReadValueU32(); // compressed chunk size
                        UInt32 uncompressedChunkSize = packageFile.ReadValueU32();
                        if (uncompressedChunkSize != chunk.uncomprSize)
                            throw new Exception("not match");

                        UInt32 blocksCount = (uncompressedChunkSize + maxBlockSize - 1) / maxBlockSize;
                        if ((compressedChunkSize + SizeOfChunk + SizeOfChunkBlock * blocksCount) != chunk.comprSize)
                            throw new Exception("not match");

                        List<ChunkBlock> blocks = new List<ChunkBlock>();
                        for (uint b = 0; b < blocksCount; b++)
                        {
                            ChunkBlock block = new ChunkBlock();
                            block.comprSize = packageFile.ReadValueU32();
                            block.uncomprSize = packageFile.ReadValueU32();
                            blocks.Add(block);
                        }

                        for (int b = 0; b < blocks.Count; b++)
                        {
                            ChunkBlock block = blocks[b];
                            byte[] dst = new byte[block.uncomprSize];
                            byte[] src = packageFile.ReadBytes(block.comprSize);
                            uint dstLen;
                            if (compressionType == CompressionType.LZO)
                                dstLen = LZO2Helper.LZO2.Decompress(src, block.comprSize, dst);
                            else if (compressionType == CompressionType.Zlib)
                                dstLen = ZlibHelper.Zlib.Decompress(src, block.comprSize, dst);
                            else
                                throw new Exception("Compression type not expected!");

                            if (dstLen != block.uncomprSize)
                                throw new Exception("Decompressed data size not expected!");

                            chunkCache.WriteBytes(dst);
                        }
                    }
                    chunkCache.Seek(startInChunk, SeekOrigin.Begin);
                    output.WriteFromStream(chunkCache, bytesLeftInChunk);
                    bytesLeft -= bytesLeftInChunk;
                    if (bytesLeft == 0)
                        break;
                }
            }
            else
            {
                packageFile.Seek(offset, SeekOrigin.Begin);
                output.WriteFromStream(packageFile, length);
            }
        }
        private byte[] getExportData(Int32 id)
        {
            if (exportsTable[id].newData != null)
                return exportsTable[id].newData;
            MemoryStream data = new MemoryStream();
            getData(exportsTable[id].dataOffset, exportsTable[id].dataOffset, data);
            return data.ToArray();
        }
        private void setExportData(Int32 id, byte[] data)
        {
            ExportEntry export = exportsTable[id];
            export.newData = data;
            export.dataSize = (UInt32)data.Length;
            export.dataOffset = 0;
            exportsTable[id] = export;
        }

        private void loadNames(Stream input)
        {
            namesTable = new List<NameEntry>();
            input.Seek(namesOffset, SeekOrigin.Begin);
            for (int i = 0; i < namesCount; i++)
            {
                NameEntry entry = new NameEntry();
                int len = input.ReadValueS32();
                if (len < 0) // unicode
                {
                    var str = input.ReadBytes(-len * 2);
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
                    entry.name = input.ReadString((uint)len);
                }
                entry.name = entry.name.Trim('\0');

                if (version == packageFileVersionME1)
                    entry.flags = input.ReadValueU64(); // 0x0007001000000000 will be default for new
                if (version == packageFileVersionME2)
                    entry.flags = input.ReadValueU32(); // 0xfffffff2 will be default for new

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
                    output.WriteValueS32((entry.name.Length + 1) * -2);
                    output.WriteString(entry.name + '\0', Encoding.Unicode);
                }
                else
                {
                    output.WriteValueS32(entry.name.Length + 1);
                    output.WriteString(entry.name + '\0');
                }
                if (version == packageFileVersionME1)
                    output.WriteValueU64(entry.flags);
                if (version == packageFileVersionME2)
                    output.WriteValueU32((uint)entry.flags);
            }
        }
        private void loadExtraNames(Stream input)
        {
            extraNamesTable = new List<string>();
            UInt32 extraNamesCount = input.ReadValueU32();
            for (int c = 0; c < extraNamesCount; c++)
            {
                int len = input.ReadValueS32();
                string name;
                if (len < 0)
                {
                    name = input.ReadString(-len * 2, Encoding.Unicode);
                }
                else
                {
                    name = input.ReadString((uint)len);
                }
                name = name.Trim('\0');
                extraNamesTable.Add(name);
            }
        }
        private void saveExtraNames(Stream output)
        {
            output.WriteValueS32(extraNamesTable.Count);
            for (int c = 0; c < extraNamesTable.Count; c++)
            {
                output.WriteValueS32(extraNamesTable[c].Length + 1);
                output.WriteString(extraNamesTable[c] + '\0', (uint)(extraNamesTable[c].Length + 1), Encoding.Unicode);
            }
        }
        private void loadImports(Stream input)
        {
            importsTable = new List<ImportEntry>();
            input.Seek(importsOffset, SeekOrigin.Begin);
            for (int i = 0; i < importsCount; i++)
            {
                ImportEntry entry = new ImportEntry();

                var start = input.Position;
                entry.packageFileId = input.ReadValueS32();
                input.ReadValueS32(); // const 0
                entry.classNameId = input.ReadValueS32();
                input.ReadValueS32(); // const 0
                entry.linkId = input.ReadValueS32();
                entry.objectNameId = input.ReadValueS32();
                input.ReadValueS32();

                var len = input.Position - start;
                input.Seek(start, SeekOrigin.Begin);
                entry.raw = input.ReadBytes((int)len);

                importsTable.Add(entry);
            }
        }
        private void saveImports(Stream output)
        {
            for (int i = 0; i < importsTable.Count; i++)
                output.WriteBytes(importsTable[i].raw);
        }
        private void loadExports(Stream input)
        {
            exportsTable = new List<ExportEntry>();
            input.Seek(exportsOffset, SeekOrigin.Begin);
            for (int i = 0; i < exportsCount; i++)
            {
                uint count;
                ExportEntry entry = new ExportEntry();

                var start = input.Position;
                entry.classNameId = input.ReadValueS32();
                entry.classParentId = input.ReadValueS32();
                entry.linkId = input.ReadValueS32();
                entry.objectNameId = input.ReadValueS32();
                entry.suffixNameId = input.ReadValueS32();
                input.ReadValueS32();
                entry.objectFlags = input.ReadValueU64();
                var dataSize = input.ReadValueU32(); // dataSize
                var dataOffset = input.ReadValueU32(); // dataOffset
                if (version != packageFileVersionME3)
                {
                    count = input.ReadValueU32();
                    input.Seek(count * 12, SeekOrigin.Current); // skip entries
                }
                input.ReadValueU32();
                count = input.ReadValueU32();
                input.Seek(count * 4, SeekOrigin.Current); // skip entries
                input.Seek(16, SeekOrigin.Current); // skip guid
                input.ReadValueU32();

                var len = input.Position - start;
                input.Seek(start, SeekOrigin.Begin);
                entry.raw = input.ReadBytes((int)len);

                exportsTable.Add(entry);
            }
        }
        private void saveExports(Stream output)
        {
            for (int i = 0; i < exportsTable.Count; i++)
                output.WriteBytes(exportsTable[i].raw);
        }
        private void loadDepends(Stream input)
        {
            dependsTable = new List<Int32>();
            input.Seek(dependsOffset, SeekOrigin.Begin);
            for (int i = 0; i < exportsCount; i++)
                dependsTable.Add(input.ReadValueS32());
        }
        private void saveDepends(Stream output)
        {
            for (int i = 0; i < exportsTable.Count; i++)
                output.WriteValueS32(dependsTable[i]);
        }
        private void loadGuids(Stream input)
        {
            guidsTable = new List<GuidEntry>();
            input.Seek(guidsOffset, SeekOrigin.Begin);
            for (int i = 0; i < guidsCount; i++)
            {
                GuidEntry entry = new GuidEntry();
                entry.guid = input.ReadBytes(16);
                entry.index = input.ReadValueS32();
                guidsTable.Add(entry);
            }
        }
        private void saveGuids(Stream output)
        {
            for (int i = 0; i < guidsTable.Count; i++)
            {
                GuidEntry entry = guidsTable[i];
                output.WriteBytes(entry.guid);
                output.WriteValueS32(entry.index);
            }
        }

        public bool SaveToFile()
        {
            if (packageFile.Length == 0)
                return false;

            MemoryStream tempOutput = new MemoryStream();
            tempOutput.Write(packageHeader, 0, packageHeader.Length); // updated later
            tempOutput.WriteValueU32((uint)compressionType);
            tempOutput.WriteValueU32(0); // number of chunks
            tempOutput.WriteValueU32(someTag);
            if (version == packageFileVersionME2)
                tempOutput.WriteValueU32(0); // const 0
            saveExtraNames(tempOutput);
            if (dataOffset != tempOutput.Position)
                throw new Exception("wrong");
            namesOffset = (UInt32)tempOutput.Position;
            saveNames(tempOutput);
            importsOffset = (UInt32)tempOutput.Position;
            saveImports(tempOutput);
            exportsOffset = (UInt32)tempOutput.Position;
            saveExports(tempOutput);
            dependsOffset = (UInt32)tempOutput.Position;
            saveDepends(tempOutput);
            if (version == packageFileVersionME3)
            {
                guidsOffset = (UInt32)tempOutput.Position;
                saveGuids(tempOutput);
            }
            endOfTablesOffset = (UInt32)tempOutput.Position;

            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry export = exportsTable[i];
                UInt32 newDataOffset = (UInt32)tempOutput.Position;
                if (export.newData == null)
                    getData(export.dataOffset, export.dataSize, tempOutput);
                else
                    tempOutput.WriteBytes(export.newData);
                export.dataOffset = newDataOffset; // update
                exportsTable[i] = export;
            }
            tempOutput.Seek(0, SeekOrigin.Begin);
            tempOutput.Write(packageHeader, 0, packageHeader.Length);
            packageFile.Close();

            string filename = packageFile.Name;
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                if (fs == null)
                    throw new Exception("Failed to write to file: " + filename);

                if (!compressed)
                {
                    tempOutput.Seek(0, SeekOrigin.Begin);
                    fs.WriteFromStream(tempOutput, tempOutput.Length);
                }
                else
                {
                    chunks.Clear();
                    Chunk chunk = new Chunk();
                    chunk.uncomprSize = endOfTablesOffset - (uint)dataOffset;
                    chunk.uncomprOffset = (uint)dataOffset;
                    for (int i = 0; i < exportsCount; i++)
                    {
                        ExportEntry export = exportsTable[i];
                        if (chunk.uncomprSize + export.dataSize > maxChunkSize)
                        {
                            UInt32 offset = chunk.uncomprOffset + chunk.uncomprSize;
                            chunks.Add(chunk);
                            chunk = new Chunk();
                            chunk.uncomprSize = export.dataSize;
                            chunk.uncomprOffset = offset;
                        }
                        else
                        {
                            chunk.uncomprSize += export.dataSize;
                        }
                    }
                    chunks.Add(chunk);

                    fs.Write(packageHeader, 0, packageHeader.Length);
                    fs.WriteValueU32((uint)compressionType);
                    fs.WriteValueU32((uint)chunks.Count);
                    var chunksTableOffset = (UInt32)fs.Position;
                    fs.Seek(SizeOfChunk * chunks.Count, SeekOrigin.Current); // skip chunks table - filled later
                    fs.WriteValueU32(someTag);
                    if (version == packageFileVersionME2)
                        fs.WriteValueU32(0); // const 0
                    saveExtraNames(fs);

                    for (int c = 0; c < chunks.Count; c++)
                    {
                        chunk = chunks[c];
                        chunk.comprOffset = (UInt32)fs.Position;
                        chunk.comprSize = 0; // filled later

                        UInt32 dataBlockLeft = chunk.uncomprSize;
                        UInt32 newNumBlocks = (chunk.uncomprSize + maxBlockSize - 1) / maxBlockSize;
                        // skip blocks header and table - filled later
                        fs.Seek(SizeOfChunk + SizeOfChunkBlock * newNumBlocks, SeekOrigin.Current);

                        tempOutput.Seek(chunk.uncomprOffset, SeekOrigin.Begin);

                        chunk.blocks = new List<ChunkBlock>();
                        for (int b = 0; b < newNumBlocks; b++)
                        {
                            ChunkBlock block = new ChunkBlock();
                            UInt32 newBlockSize = Math.Min(maxBlockSize, dataBlockLeft);

                            byte[] dst;
                            byte[] src = tempOutput.ReadBytes(newBlockSize);
                            if (compressionType == CompressionType.LZO)
                                dst = LZO2Helper.LZO2.Compress(src);
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
                        fs.Seek(chunksTableOffset + c * SizeOfChunk, SeekOrigin.Begin); // seek to chunks table
                        fs.WriteValueU32(chunk.uncomprOffset);
                        fs.WriteValueU32(chunk.uncomprSize);
                        fs.WriteValueU32(chunk.comprOffset);
                        fs.WriteValueU32(chunk.comprSize + SizeOfChunk + SizeOfChunkBlock * (uint)chunk.blocks.Count);
                        fs.Seek(chunk.comprOffset, SeekOrigin.Begin); // seek to blocks header
                        fs.WriteValueU32(packageTag);
                        fs.WriteValueU32(maxBlockSize);
                        fs.WriteValueU32(chunk.comprSize);
                        fs.WriteValueU32(chunk.uncomprSize);
                        foreach (ChunkBlock block in chunk.blocks)
                        {
                            fs.WriteValueU32(block.comprSize);
                            fs.WriteValueU32(block.uncomprSize);
                        }
                    }
                }
            }

            return true;
        }

        public void Dispose()
        {
            chunkCache.Dispose();
            packageData.Dispose();
            packageFile.Close();
        }

    }
}
