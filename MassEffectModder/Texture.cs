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

namespace MassEffectModder
{
    public class Texture : IDisposable
    {
        const uint textureTag = 0x9E2A83C1;
        const uint maxBlockSize = 0x20000; // 128KB
        const int SizeOfChunkBlock = 8;
        const int SizeOfChunk = 16;

        public enum StorageTypes
        {
            pccUnc = 0x0, // ME1 (Compressed PCC), ME2 (Compressed PCC)
            pccCpr = 0x10, // ME1 (Uncompressed PCC)
            extUnc = 0x1, // ME3 (DLC TFC archive)
            extCpr = 0x11, // ME1 (Reference to PCC), ME2 (TFC archive)
            arcCpr = 0x3, // ME3 (non-DLC TFC archive)
            empty = 0x21, // ME1, ME2, ME3
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

        public Texture(Package package, int exportId, byte[] data)
        {
            properties = new TexProperty(package, data);
#if false // dump properties info
            using (FileStream file = new FileStream("Textures.txt", FileMode.Append))
            {
                file.WriteStringASCII("---Package---" + package.packageFile.Name + " export data offset: " + package.exportsTable[exportId].dataOffset + "\n");
                for (int i = 0; i < properties.texPropertyList.Count; i++)
                {
                    if (properties.texPropertyList[i].name == "None")
                        continue;
                    properties.fetchValue(properties.texPropertyList[i].name);
                }
                foreach (TexProperty.TexPropertyEntry prop in properties.texPropertyList)
                {
                    if (prop.name == "None")
                        continue;
                    file.WriteStringASCII("Texture: " + package.exportsTable[exportId].objectName + ", Name: " + prop.name + ", Type: " + prop.type + ", ");
                    switch (prop.type)
                    {
                        case "IntProperty":
                            file.WriteStringASCII("Value: " + prop.valueInt + "\n");
                            break;
                        case "ByteProperty":
                            file.WriteStringASCII("ValueName: " + prop.valueName + ", ");
                            file.WriteStringASCII("Value: " + prop.valueInt + "\n");
                            break;
                        case "BoolProperty":
                            file.WriteStringASCII("Value: " + prop.valueInt + "\n");
                            break;
                        case "FloatProperty":
                            file.WriteStringASCII("Value: " + prop.valueFloat + "\n");
                            break;
                        case "NameProperty":
                            file.WriteStringASCII("ValueName: " + prop.valueName + ", ");
                            file.WriteStringASCII("Value: " + prop.valueInt + "\n");
                            break;
                        case "StructProperty":
                            file.WriteStringASCII("ValueName: " + prop.valueName + ", ");
                            file.WriteStringASCII("Value: " + prop.valueInt + "\n");
                            break;
                    }
                }
            }
#endif
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
                if (mipmap.storageType == StorageTypes.pccCpr)
                {
                    mipmap.internalOffset = (uint)textureData.Position;
                    textureData.Skip(mipmap.compressedSize);
                }
                mipmap.width = textureData.ReadInt32();
                mipmap.height = textureData.ReadInt32();
                mipMapsList.Add(mipmap);
            }

            if (GameData.gameType == MeType.ME1_TYPE)
            {
                textureData.Seek(-4, SeekOrigin.End);
                restOfData = textureData.ReadToBuffer(4);
            }
#if false // dump mipmaps info
            using (FileStream file = new FileStream("Textures.txt", FileMode.Append))
            {
                for (int l = 0; l < numMipMaps; l++)
                {
                    file.WriteStringASCII("MipMap: " + l + ", Width: " + mipMapsList[l].width + ", Height: " + mipMapsList[l].height);
                    file.WriteStringASCII(" StorageType: " + mipMapsList[l].storageType);
                    file.WriteStringASCII(" uncompressedSize: " + mipMapsList[l].uncompressedSize);
                    file.WriteStringASCII(" compressedSize: " + mipMapsList[l].compressedSize);
                    if (mipMapsList[l].storageType == StorageTypes.pccCpr ||
                        mipMapsList[l].storageType == StorageTypes.pccUnc)
                    {
                        file.WriteStringASCII(" dataOffset: " + (mipMapsList[l].dataOffset + (int)package.exportsTable[exportId].dataOffset + properties.propertyEndOffset) + "\n");
                    }
                    else
                    {
                        file.WriteStringASCII(" dataOffset: " + mipMapsList[l].dataOffset + "\n");
                    }
                }
            }
#endif
        }

        public void replaceMipMaps(List<Texture.MipMap> newMipMaps)
        {
            mipMapsList = newMipMaps;
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
                    mipmap.storageType == StorageTypes.pccCpr)
                {
                    mipmap.internalOffset = (uint)textureData.Position;
                    textureData.WriteFromBuffer(mipmap.newData);
                }
                mipMapsList[l] = mipmap;
            }
        }

        public byte[] compressTexture(byte[] inputData)
        {
            MemoryStream ouputStream = new MemoryStream();
            MemoryStream inputStream = new MemoryStream(inputData);
            uint compressedSize = 0;
            uint dataBlockLeft = (uint)inputData.Length;
            uint newNumBlocks = ((uint)inputData.Length + maxBlockSize - 1) / maxBlockSize;
            // skip blocks header and table - filled later
            ouputStream.Seek(SizeOfChunk + SizeOfChunkBlock * newNumBlocks, SeekOrigin.Begin);

            List<Package.ChunkBlock> blocks = new List<Package.ChunkBlock>();
            for (int b = 0; b < newNumBlocks; b++)
            {
                Package.ChunkBlock block = new Package.ChunkBlock();
                uint newBlockSize = Math.Min(maxBlockSize, dataBlockLeft);

                byte[] dst;
                byte[] src = inputStream.ReadToBuffer(newBlockSize);
                if (GameData.gameType == MeType.ME3_TYPE)
                    dst = ZlibHelper.Zlib.Compress(src, 9);
                else
                    dst = LZO2Helper.LZO2.Compress(src, false);
                if (dst.Length == 0)
                    throw new Exception("Compression failed!");

                ouputStream.Write(dst, 0, dst.Length);
                block.uncomprSize = newBlockSize;
                block.comprSize = (uint)dst.Length;
                compressedSize += block.comprSize;
                blocks.Add(block);
                dataBlockLeft -= newBlockSize;
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

        private byte[] decompressTexture(MemoryStream stream, int uncompressedSize, int compressedSize)
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

            int dstPos = 0;
            for (int b = 0; b < blocks.Count; b++)
            {
                Package.ChunkBlock block = blocks[b];
                byte[] dst = new byte[block.uncomprSize];
                byte[] src = stream.ReadToBuffer(block.comprSize);
                uint dstLen;
                if (GameData.gameType == MeType.ME3_TYPE)
                    dstLen = ZlibHelper.Zlib.Decompress(src, block.comprSize, dst);
                else
                    dstLen = LZO2Helper.LZO2.Decompress(src, block.comprSize, dst);

                if (dstLen != block.uncomprSize)
                    throw new Exception("Decompressed data size not expected!");

                Buffer.BlockCopy(dst, 0, data, dstPos, (int)dstLen);
                dstPos += (int)dstLen;
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
            return mipMapsList.First(b => b.storageType != StorageTypes.empty);
        }

        public MipMap getMipmap(int width, int height)
        {
            return mipMapsList.Find(b => b.width == width && b.height == height);
        }

        public StorageTypes getStorageType(int width, int height)
        {
            MipMap tmpMipmap = getMipmap(width, height);
            if (tmpMipmap.width == 0) // not found
            {
                tmpMipmap = getTopMipmap();
            }
            return tmpMipmap.storageType;
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
                case StorageTypes.pccCpr:
                    {
                        textureData.JumpTo(mipmap.internalOffset);
                        mipMapData = decompressTexture(textureData, mipmap.uncompressedSize, mipmap.compressedSize);
                        break;
                    }
                case StorageTypes.extUnc:
                case StorageTypes.extCpr:
                case StorageTypes.arcCpr:
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
                            if (mipmap.storageType == StorageTypes.extCpr || mipmap.storageType == StorageTypes.arcCpr)
                            {
                                mipMapData = decompressTexture(new MemoryStream(fs.ReadToBuffer(mipmap.compressedSize)),
                                    mipmap.uncompressedSize, mipmap.compressedSize);
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
            MemoryStream newData = new MemoryStream();
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
                else if (mipmap.storageType == StorageTypes.pccCpr)
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

            if (GameData.gameType == MeType.ME1_TYPE)
            {
                newData.WriteFromBuffer(restOfData);
            }

            return newData.ToArray();
        }

        public void Dispose()
        {
            textureData.Dispose();
        }
    }
}
