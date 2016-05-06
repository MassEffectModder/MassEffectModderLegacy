/*
 * METexturesExplorer
 *
 * Copyright (C) 2015 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

namespace METexturesExplorer
{
    class Texture : IDisposable
    {
        const uint textureTag = 0x9E2A83C1;
        const uint maxBlockSize = 0x20000; // 128KB
        const int SizeOfChunkBlock = 8;
        const int SizeOfChunk = 16;

        enum StorageTypes
        {
            pccUnc = 0x0,
            pccCpr = 0x10,
            extUnc = 0x1,
            extCpr = 0x11,
            arcCpr = 0x3,
            empty = 0x21,
        }

        struct Bitmap
        {
            public StorageTypes storageType;
            public int uncompressedSize;
            public int compressedSize;
            public int dataOffset;
            public int width;
            public int height;
        }
        List<Bitmap> mipMapsList;
        MemoryStream textureData;

        public Texture(Package package, int exportId, byte[] data, TexExplorer tex)
        {
            TexProperty properties = new TexProperty(package, data);
#if false // dump properties info
            using (FileStream file = new FileStream("Textures.txt", FileMode.Append))
            {
                file.WriteStringASCII("---Package---\n");
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
                    if (!tex.textures.Contains(package.exportsTable[exportId].objectName))
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

            textureData = new MemoryStream(data, properties.propertyEndOffset, data.Length - properties.propertyEndOffset);
            if (GameData.gameType != MeType.ME3_TYPE)
            {
                textureData.Skip(12); // 12 zeros
                textureData.SkipInt32(); // position in the package
            }

            mipMapsList = new List<Bitmap>();
            int numMipMaps = textureData.ReadInt32();
            for (int l = 0; l < numMipMaps; l++)
            {
                Bitmap bmp = new Bitmap();
                bmp.storageType = (StorageTypes)textureData.ReadInt32();
                bmp.uncompressedSize = textureData.ReadInt32();
                bmp.compressedSize = textureData.ReadInt32();
                bmp.dataOffset = textureData.ReadInt32();
                if (bmp.storageType == StorageTypes.pccUnc)
                {
                    bmp.dataOffset = (int)textureData.Position;
                    textureData.Skip(bmp.uncompressedSize);
                }
                if (bmp.storageType == StorageTypes.pccCpr)
                {
                    bmp.dataOffset = (int)textureData.Position;
                    textureData.Skip(bmp.compressedSize);
                }
                bmp.width = textureData.ReadInt32();
                bmp.height = textureData.ReadInt32();
                mipMapsList.Add(bmp);
            }
#if false // dump mipmaps info
            using (FileStream file = new FileStream("Textures.txt", FileMode.Append))
            {
                for (int l = 0; l < numMipMaps; l++)
                {
                    file.WriteStringASCII("MipMap: " + l + ", Width: " + mipMapsList[l].width + ", Height: " + mipMapsList[l].height);
                    file.WriteStringASCII("StorageType: " + mipMapsList[l].storageType + "\n");
                }
            }
#endif
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

        public byte[] getImageData()
        {
            if (textureData == null || mipMapsList.Count == 0)
                return null;

            Bitmap bmp = mipMapsList.First(b => b.storageType != StorageTypes.empty);
            byte[] bmpData = null;

            switch (bmp.storageType)
            {
                case StorageTypes.pccUnc:
                    {
                        textureData.JumpTo(bmp.dataOffset);
                        bmpData = textureData.ReadToBuffer(bmp.uncompressedSize);
                        break;
                    }
                case StorageTypes.pccCpr:
                    {
                        textureData.JumpTo(bmp.dataOffset);
                        bmpData = decompressTexture(textureData, bmp.uncompressedSize, bmp.compressedSize);
                        break;
                    }
            }
            return bmpData;
        }

        public void Dispose()
        {
            textureData.Dispose();
        }
    }
}
