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
using System.Security.Cryptography;

namespace MEDataExplorer
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
        CompressionType compressionType;
        FileStream packageFile;
        MemoryTributary packageData;
        List<Chunk> chunks;
        List<NameEntry> namesTable;
        List<ImportEntry> importsTable;
        public List<ExportEntry> exportsTable;
        List<int> dependsTable;
        List<GuidEntry> guidsTable;
        List<string> extraNamesTable;
        int currentChunk = -1;
        MemoryStream chunkCache;

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
        public struct GuidEntry
        {
            public byte[] guid;
            public int index;
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
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderDependsOffsetTableOffset, sizeof(uint));
            }
        }

        private uint guidsOffset
        {
            get
            {
                return BitConverter.ToUInt32(packageHeader, tablesOffset + packageHeaderGuidsOffsetTableOffset);
            }
            set
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value), 0, packageHeader, tablesOffset + packageHeaderGuidsOffsetTableOffset, sizeof(uint));
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

        public Package(string filename)
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
                        packageFile.Seek(chunk.comprOffset, SeekOrigin.Begin);
                        uint blockTag = packageFile.ReadValueU32(); // block tag
                        if (blockTag != packageTag)
                            throw new Exception("not match");
                        uint blockSize = packageFile.ReadValueU32(); // max block size
                        if (blockSize != maxBlockSize)
                            throw new Exception("not match");
                        uint compressedChunkSize = packageFile.ReadValueU32(); // compressed chunk size
                        uint uncompressedChunkSize = packageFile.ReadValueU32();
                        if (uncompressedChunkSize != chunk.uncomprSize)
                            throw new Exception("not match");

                        uint blocksCount = (uncompressedChunkSize + maxBlockSize - 1) / maxBlockSize;
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
                        chunk.blocks = blocks;
                        chunks[c] = chunk;

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
            export.newData = data;
            export.dataSize = (uint)data.Length;
            export.dataOffset = 0;
            exportsTable[id] = export;
        }

        public int getNameId(string name)
        {
            for (int i = 0; i < namesCount; i++)
            {
                if (namesTable[i].name == name)
                    return i;
            }
            return addName(name);
        }

        public int addName(string name)
        {
            NameEntry entry = new NameEntry();
            entry.name = name;
            if (version == packageFileVersionME1)
                entry.flags = 0x0007001000000000;
            if (version == packageFileVersionME2)
                entry.flags = 0xfffffff2;
            namesTable.Add(entry);
            namesCount = (uint)namesTable.Count;
            return namesTable.Count - 1;
        }

        public string getName(int id)
        {
            if (id >= namesTable.Count)
                throw new Exception("wrong");
            return namesTable[id].name;
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
                    entry.name = input.ReadString(len, Encoding.ASCII);
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
                    output.WriteValueS32(-(entry.name.Length + 1));
                    output.WriteStringZ(entry.name, Encoding.Unicode);
                }
                else
                {
                    output.WriteValueS32(entry.name.Length + 1);
                    output.WriteStringZ(entry.name, Encoding.ASCII);
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
            uint extraNamesCount = input.ReadValueU32();
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
                    name = input.ReadString(len, Encoding.ASCII);
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
                if (packageFileVersion == packageFileVersionME3)
                {
                    output.WriteValueS32(-(extraNamesTable[c].Length + 1));
                    output.WriteStringZ(extraNamesTable[c], Encoding.Unicode);
                }
                else
                {
                    output.WriteValueS32(extraNamesTable[c].Length + 1);
                    output.WriteStringZ(extraNamesTable[c], Encoding.ASCII);
                }
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
                entry.packageFile = namesTable[entry.packageFileId].name;
                input.ReadValueS32(); // const 0
                entry.classId = input.ReadValueS32();
                input.ReadValueS32(); // const 0
                entry.linkId = input.ReadValueS32();
                entry.objectNameId = input.ReadValueS32();
                entry.objectName = namesTable[entry.objectNameId].name;
                input.ReadValueS32();

                var len = input.Position - start;
                input.Seek(start, SeekOrigin.Begin);
                entry.raw = input.ReadBytes((int)len);

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

        private void saveImports(Stream output)
        {
            for (int i = 0; i < importsTable.Count; i++)
            {
                output.WriteBytes(importsTable[i].raw);
            }
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
                entry.classId = input.ReadValueS32();
                entry.classParentId = input.ReadValueS32();
                entry.linkId = input.ReadValueS32();
                entry.objectNameId = input.ReadValueS32();
                entry.objectName = namesTable[entry.objectNameId].name;
                entry.suffixNameId = input.ReadValueS32();
                input.ReadValueS32();
                entry.objectFlags = input.ReadValueU64();
                input.ReadValueU32(); // dataSize
                input.ReadValueU32(); // dataOffset
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
                output.WriteBytes(exportsTable[i].raw);
            }
        }
        private void loadDepends(Stream input)
        {
            dependsTable = new List<int>();
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

        public bool SaveToFile(bool forceCompress = false, int compressionLevel = -1)
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
            namesOffset = (uint)tempOutput.Position;
            saveNames(tempOutput);
            importsOffset = (uint)tempOutput.Position;
            saveImports(tempOutput);
            exportsOffset = (uint)tempOutput.Position;
            saveExports(tempOutput);
            dependsOffset = (uint)tempOutput.Position;
            saveDepends(tempOutput);
            if (version == packageFileVersionME3)
            {
                guidsOffset = (uint)tempOutput.Position;
                saveGuids(tempOutput);
            }
            endOfTablesOffset = (uint)tempOutput.Position;

            for (int i = 0; i < exportsCount; i++)
            {
                ExportEntry export = exportsTable[i];
                uint newDataOffset = (uint)tempOutput.Position;
                if (export.newData != null)
                    tempOutput.WriteBytes(export.newData);
                else
                    getData(export.dataOffset, export.dataSize, tempOutput);
                export.dataOffset = newDataOffset; // update
                exportsTable[i] = export;
            }
            if (forceCompress) // override to compression
                compressed = true;
            compressionType = CompressionType.Zlib; // override compression type to Zlib
            tempOutput.Seek(0, SeekOrigin.Begin);
            tempOutput.Write(packageHeader, 0, packageHeader.Length);
            tempOutput.WriteValueU32((uint)compressionType);
            tempOutput.Seek(exportsOffset, SeekOrigin.Begin);
            saveExports(tempOutput);
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
                    if (chunks == null)
                        chunks = new List<Chunk>();
                    chunks.Clear();
                    Chunk chunk = new Chunk();
                    chunk.uncomprSize = endOfTablesOffset - (uint)dataOffset;
                    chunk.uncomprOffset = (uint)dataOffset;
                    for (int i = 0; i < exportsCount; i++)
                    {
                        ExportEntry export = exportsTable[i];
                        if (chunk.uncomprSize + export.dataSize > maxChunkSize)
                        {
                            uint offset = chunk.uncomprOffset + chunk.uncomprSize;
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
                    var chunksTableOffset = (uint)fs.Position;
                    fs.Seek(SizeOfChunk * chunks.Count, SeekOrigin.Current); // skip chunks table - filled later
                    fs.WriteValueU32(someTag);
                    if (version == packageFileVersionME2)
                        fs.WriteValueU32(0); // const 0
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

                        tempOutput.Seek(chunk.uncomprOffset, SeekOrigin.Begin);

                        chunk.blocks = new List<ChunkBlock>();
                        for (int b = 0; b < newNumBlocks; b++)
                        {
                            ChunkBlock block = new ChunkBlock();
                            uint newBlockSize = Math.Min(maxBlockSize, dataBlockLeft);

                            byte[] dst;
                            byte[] src = tempOutput.ReadBytes(newBlockSize);
                            if (compressionType == CompressionType.LZO)
                                dst = LZO2Helper.LZO2.Compress(src);
                            else if (compressionType == CompressionType.Zlib)
                                dst = ZlibHelper.Zlib.Compress(src, compressionLevel);
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
                uint tag = tocFile.ReadValueU32();
                if (tag != TOCTag)
                    throw new Exception("Wrong TOCTag tag");
                tocFile.ReadValueU32();

                blockList = new List<Block>();
                uint numBlocks = tocFile.ReadValueU32();
                for (int b = 0; b < numBlocks; b++)
                {
                    Block block = new Block();
                    block.filesOffset = tocFile.ReadValueU32();
                    block.numFiles = tocFile.ReadValueU32();
                    block.filesList = new List<File>();
                    blockList.Add(block);
                }

                tocFile.Seek(TOCHeaderSize + (numBlocks * 8), SeekOrigin.Begin);
                for (int b = 0; b < numBlocks; b++)
                {
                    Block block = blockList[b];
                    File file = new File();
                    for (int f = 0; f < block.numFiles; f++)
                    {
                        long curPos = tocFile.Position;
                        ushort blockSize = tocFile.ReadValueU16();
                        file.type = tocFile.ReadValueU16();
                        if (file.type != 9 && file.type != 1)
                            throw new Exception("wrong");
                        file.size = tocFile.ReadValueU32();
                        file.sha1 = tocFile.ReadBytes(20);
                        file.path = tocFile.ReadStringZ(Encoding.ASCII);
                        block.filesList.Add(file);
                        tocFile.Seek(curPos + blockSize, SeekOrigin.Begin);
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
                tocFile.WriteValueU32(TOCTag);
                tocFile.WriteValueU32(0);
                tocFile.WriteValueU32((uint)blockList.Count);
                tocFile.Seek(8 * blockList.Count, SeekOrigin.Current); // filled later

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
                        tocFile.WriteValueU16((ushort)blockSize);
                        tocFile.WriteValueU16(file.type);
                        tocFile.WriteValueU32(file.size);
                        tocFile.WriteBytes(file.sha1);
                        tocFile.WriteStringZ(file.path, Encoding.ASCII);
                        tocFile.Seek(fileOffset + blockSize - 1, SeekOrigin.Begin);
                        tocFile.WriteByte(0); // make sure all bytes are written after seek
                    }
                    blockList[b] = block;
                }
                if (lastOffset != 0)
                {
                    tocFile.Seek(lastOffset, SeekOrigin.Begin);
                    tocFile.WriteValueU16(0);
                }

                tocFile.Seek(TOCHeaderSize, SeekOrigin.Begin);
                for (int b = 0; b < blockList.Count; b++)
                {
                    tocFile.WriteValueU32(blockList[b].filesOffset);
                    tocFile.WriteValueU32(blockList[b].numFiles);
                }
            }
        }

    }

}
