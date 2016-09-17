/*
 * MassEffectModder
 *
 * Copyright (C) 2015-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using System.IO;
using StreamHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using CRC32;
using System.Threading.Tasks;

namespace MassEffectModder
{
    public class Texture : IDisposable
    {
        const uint textureTag = 0x9E2A83C1;
        const uint maxBlockSize = 0x20000; // 128KB
        const int SizeOfChunkBlock = 8;
        const int SizeOfChunk = 16;

        public enum StorageFlags
        {
            noFlags        = 0,
            externalFile   = 1 << 0,
            compressedZlib = 1 << 1,
            compressedLZO  = 1 << 4,
            unused         = 1 << 5,
        }

        public enum StorageTypes
        {
            pccUnc = StorageFlags.noFlags,                                     // ME1 (Compressed PCC), ME2 (Compressed PCC)
            pccLZO = StorageFlags.compressedLZO,                               // ME1 (Uncompressed PCC)
            pccZlib = StorageFlags.compressedZlib,                             // ME1 (Uncompressed PCC)
            extUnc = StorageFlags.externalFile,                                // ME3 (DLC TFC archive)
            extLZO = StorageFlags.externalFile | StorageFlags.compressedLZO,   // ME1 (Reference to PCC), ME2 (TFC archive)
            extZlib = StorageFlags.externalFile | StorageFlags.compressedZlib, // ME3 (non-DLC TFC archive)
            empty = StorageFlags.externalFile | StorageFlags.unused,           // ME1, ME2, ME3
        }

        public struct MipMap
        {
            public StorageTypes storageType;
            public int uncompressedSize;
            public int compressedSize;
            public uint dataOffset;
            public uint internalOffset;
            public int width;
            public int height;
            public byte[] newData;
        }
        public List<MipMap> mipMapsList;
        MemoryStream textureData;
        public TexProperty properties;
        byte[] mipMapData = null;
        public string packageName;
        byte[] restOfData;

        public Texture(Package package, int exportId, byte[] data, bool fixDim = true)
        {
            properties = new TexProperty(package, data);
            if (data.Length == properties.propertyEndOffset)
                return;

            packageName = Path.GetFileNameWithoutExtension(package.packageFile.Name).ToUpper();
            if (GameData.gameType == MeType.ME1_TYPE && package.compressed)
            {
                string basePkg = package.resolvePackagePath(package.exportsTable[exportId].linkId).Split('.')[0].ToUpper();
                if (basePkg != "")
                    packageName = basePkg;
            }

            textureData = new MemoryStream(data, properties.propertyEndOffset, data.Length - properties.propertyEndOffset);
            if (GameData.gameType != MeType.ME3_TYPE)
            {
                textureData.Skip(12); // 12 zeros
                textureData.SkipInt32(); // position in the package
            }

            mipMapsList = new List<MipMap>();
            int numMipMaps = textureData.ReadInt32();
            for (int l = 0; l < numMipMaps; l++)
            {
                MipMap mipmap = new MipMap();
                mipmap.storageType = (StorageTypes)textureData.ReadInt32();
                mipmap.uncompressedSize = textureData.ReadInt32();
                mipmap.compressedSize = textureData.ReadInt32();
                mipmap.dataOffset = textureData.ReadUInt32();
                if (mipmap.storageType == StorageTypes.pccUnc)
                {
                    mipmap.internalOffset = (uint)textureData.Position;
                    textureData.Skip(mipmap.uncompressedSize);
                }
                if (mipmap.storageType == StorageTypes.pccLZO ||
                    mipmap.storageType == StorageTypes.pccZlib)
                {
                    mipmap.internalOffset = (uint)textureData.Position;
                    textureData.Skip(mipmap.compressedSize);
                }

                mipmap.width = textureData.ReadInt32();
                mipmap.height = textureData.ReadInt32();

                if (fixDim)
                {
                    if (mipmap.width == 4 && mipMapsList.Exists(mip => mip.width == mipmap.width))
                        mipmap.width = mipMapsList.Last().width / 2;
                    if (mipmap.height == 4 && mipMapsList.Exists(mip => mip.height == mipmap.height))
                        mipmap.height = mipMapsList.Last().height / 2;

                    if (mipmap.width == 0)
                        mipmap.width = 1;
                    if (mipmap.height == 0)
                        mipmap.height = 1;
                }

                mipMapsList.Add(mipmap);
            }

            restOfData = textureData.ReadToBuffer(textureData.Length - textureData.Position);
        }

        public void replaceMipMaps(List<MipMap> newMipMaps)
        {
            mipMapsList = newMipMaps;
            textureData.Dispose();
            textureData = new MemoryStream();
            if (GameData.gameType != MeType.ME3_TYPE)
            {
                textureData.WriteZeros(12);
                textureData.WriteUInt32(0); // filled later
            }
            textureData.WriteInt32(newMipMaps.Count);
            for (int l = 0; l < newMipMaps.Count; l++)
            {
                MipMap mipmap = mipMapsList[l];
                textureData.WriteUInt32((uint)mipmap.storageType);
                textureData.WriteInt32(mipmap.uncompressedSize);
                textureData.WriteInt32(mipmap.compressedSize);
                textureData.WriteUInt32(mipmap.dataOffset);

                if (mipmap.storageType == StorageTypes.pccUnc ||
                    mipmap.storageType == StorageTypes.pccLZO ||
                    mipmap.storageType == StorageTypes.pccZlib)
                {
                    mipmap.internalOffset = (uint)textureData.Position;
                    textureData.WriteFromBuffer(mipmap.newData);
                }
                mipMapsList[l] = mipmap;
            }
        }

        public byte[] compressTexture(byte[] inputData, StorageTypes type)
        {
            using (MemoryStream ouputStream = new MemoryStream())
            {
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

                if (type == StorageTypes.extLZO || type == StorageTypes.pccLZO)
                {
                    for (int b = 0; b < blocks.Count; b++)
                    {
                        Package.ChunkBlock block = blocks[b];
                        block.compressedBuffer = LZO2Helper.LZO2.Compress(block.uncompressedBuffer);
                        if (block.compressedBuffer.Length == 0)
                            throw new Exception("Compression failed!");
                        block.comprSize = (uint)block.compressedBuffer.Length;
                        blocks[b] = block;
                    }
                }
                else
                {
                    Parallel.For(0, blocks.Count, b =>
                    {
                        Package.ChunkBlock block = blocks[b];
                        if (type == StorageTypes.extZlib || type == StorageTypes.pccZlib)
                            block.compressedBuffer = ZlibHelper.Zlib.Compress(block.uncompressedBuffer);
                        else
                            throw new Exception("Compression type not expected!");
                        if (block.compressedBuffer.Length == 0)
                            throw new Exception("Compression failed!");
                        block.comprSize = (uint)block.compressedBuffer.Length;
                        blocks[b] = block;
                    });
                }

                for (int b = 0; b < blocks.Count; b++)
                {
                    Package.ChunkBlock block = blocks[b];
                    ouputStream.Write(block.compressedBuffer, 0, (int)block.comprSize);
                    compressedSize += block.comprSize;
                }

                ouputStream.SeekBegin();
                ouputStream.WriteUInt32(textureTag);
                ouputStream.WriteUInt32(maxBlockSize);
                ouputStream.WriteUInt32(compressedSize);
                ouputStream.WriteInt32(inputData.Length);
                foreach (Package.ChunkBlock block in blocks)
                {
                    ouputStream.WriteUInt32(block.comprSize);
                    ouputStream.WriteUInt32(block.uncomprSize);
                }

                return ouputStream.ToArray();
            }
        }

        private byte[] decompressTexture(MemoryStream stream, StorageTypes type, int uncompressedSize, int compressedSize)
        {
            byte[] data = new byte[uncompressedSize];
            uint blockTag = stream.ReadUInt32();
            if (blockTag != textureTag)
                throw new Exception("not match");
            uint blockSize = stream.ReadUInt32();
            if (blockSize != maxBlockSize)
                throw new Exception("not match");
            uint compressedChunkSize = stream.ReadUInt32();
            uint uncompressedChunkSize = stream.ReadUInt32();
            if (uncompressedChunkSize != uncompressedSize)
                throw new Exception("not match");

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

            if (type == StorageTypes.extLZO || type == StorageTypes.pccLZO)
            {
                for (int b = 0; b < blocks.Count; b++)
                {
                    uint dstLen = 0;
                    Package.ChunkBlock block = blocks[b];
                    dstLen = LZO2Helper.LZO2.Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
                    if (dstLen != block.uncomprSize)
                        throw new Exception("Decompressed data size not expected!");
                }
            }
            else
            {
                Parallel.For(0, blocks.Count, b =>
                {
                    uint dstLen = 0;
                    Package.ChunkBlock block = blocks[b];
                    if (type == StorageTypes.extZlib || type == StorageTypes.pccZlib)
                        dstLen = ZlibHelper.Zlib.Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
                    if (dstLen != block.uncomprSize)
                        throw new Exception("Decompressed data size not expected!");
                });
            }

            int dstPos = 0;
            for (int b = 0; b < blocks.Count; b++)
            {
                Buffer.BlockCopy(blocks[b].uncompressedBuffer, 0, data, dstPos, (int)blocks[b].uncomprSize);
                dstPos += (int)blocks[b].uncomprSize;
            }

            return data;
        }

        public uint getCrcMipmap()
        {
            byte[] data = getImageData();
            if (properties.getProperty("Format").valueName == "PF_NormalMap_HQ") // only ME1 and ME2
                return (uint)~ParallelCRC.Compute(data, 0, data.Length / 2);
            else
                return (uint)~ParallelCRC.Compute(data);
        }

        public MipMap getTopMipmap()
        {
            MipMap mipmap = mipMapsList.Find(b => b.storageType != StorageTypes.empty);
            if (mipmap.width == 0)
                throw new Exception();

            return mipmap;
        }

        public bool existMipmap(int width, int height)
        {
            return mipMapsList.Exists(b => b.width == width && b.height == height);
        }

        public bool isBiggerOfTopMipmap(int width, int height)
        {
            int topWidth = getTopMipmap().width;
            int topHeight = getTopMipmap().height;
            if (width * height > topWidth * topHeight)
                return true;
            else
                return false;
        }

        public MipMap getMipmap(int width, int height)
        {
            return mipMapsList.Find(b => b.width == width && b.height == height);
        }

        public bool hasImageData()
        {
            if (textureData == null || mipMapsList.Count == 0)
                return false;
            return true;
        }

        public byte[] getImageData()
        {
            if (textureData == null || mipMapsList.Count == 0)
                return null;

            if (mipMapData != null)
                return mipMapData;

            MipMap mipmap = getTopMipmap();
            switch (mipmap.storageType)
            {
                case StorageTypes.pccUnc:
                    {
                        textureData.JumpTo(mipmap.internalOffset);
                        mipMapData = textureData.ReadToBuffer(mipmap.uncompressedSize);
                        break;
                    }
                case StorageTypes.pccLZO:
                case StorageTypes.pccZlib:
                    {
                        textureData.JumpTo(mipmap.internalOffset);
                        mipMapData = decompressTexture(textureData, mipmap.storageType, mipmap.uncompressedSize, mipmap.compressedSize);
                        break;
                    }
                case StorageTypes.extUnc:
                case StorageTypes.extLZO:
                case StorageTypes.extZlib:
                    {
                        string filename;
                        if (GameData.gameType == MeType.ME1_TYPE)
                        {
                            filename = GameData.packageFiles.Find(s => Path.GetFileNameWithoutExtension(s).Equals(packageName, StringComparison.OrdinalIgnoreCase));
                        }
                        else
                        {
                            string archive = properties.getProperty("TextureFileCacheName").valueName + ".tfc";
                            filename = GameData.tfcFiles.Find(s => Path.GetFileName(s).Equals(archive, StringComparison.OrdinalIgnoreCase));
                        }

                        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                        {
                            fs.JumpTo(mipmap.dataOffset);
                            if (mipmap.storageType == StorageTypes.extLZO || mipmap.storageType == StorageTypes.extZlib)
                            {
                                using (MemoryStream tmpStream = new MemoryStream(fs.ReadToBuffer(mipmap.compressedSize)))
                                {
                                    mipMapData = decompressTexture(tmpStream,
                                        mipmap.storageType, mipmap.uncompressedSize, mipmap.compressedSize);
                                }
                            }
                            else
                            {
                                mipMapData = fs.ReadToBuffer(mipmap.uncompressedSize);
                            }
                        }
                        break;
                    }
            }
            return mipMapData;
        }

        public byte[] toArray(uint pccTextureDataOffset)
        {
            using (MemoryStream newData = new MemoryStream())
            {
                if (GameData.gameType != MeType.ME3_TYPE)
                {
                    newData.WriteZeros(12);
                    newData.WriteUInt32(pccTextureDataOffset + 12 + 4);
                }
                newData.WriteInt32(mipMapsList.Count());
                for (int l = 0; l < mipMapsList.Count(); l++)
                {
                    MipMap mipmap = mipMapsList[l];
                    newData.WriteInt32((int)mipmap.storageType);
                    newData.WriteInt32(mipmap.uncompressedSize);
                    newData.WriteInt32(mipmap.compressedSize);
                    if (mipmap.storageType == StorageTypes.pccUnc)
                    {
                        mipmap.dataOffset = (uint)newData.Position + pccTextureDataOffset + 4;
                        newData.WriteUInt32(mipmap.dataOffset);
                        textureData.JumpTo(mipmap.internalOffset);
                        newData.WriteFromBuffer(textureData.ReadToBuffer(mipmap.uncompressedSize));
                    }
                    else if (mipmap.storageType == StorageTypes.pccLZO ||
                        mipmap.storageType == StorageTypes.pccZlib)
                    {
                        mipmap.dataOffset = (uint)newData.Position + pccTextureDataOffset + 4;
                        newData.WriteUInt32(mipmap.dataOffset);
                        textureData.JumpTo(mipmap.internalOffset);
                        newData.WriteFromBuffer(textureData.ReadToBuffer(mipmap.compressedSize));
                    }
                    else
                    {
                        newData.WriteUInt32(mipmap.dataOffset);
                    }
                    newData.WriteInt32(mipmap.width);
                    newData.WriteInt32(mipmap.height);
                    mipMapsList[l] = mipmap;
                }

                newData.WriteFromBuffer(restOfData);

                return newData.ToArray();
            }
        }

        public void Dispose()
        {
            if (textureData != null)
               textureData.Dispose();
        }
    }
}
