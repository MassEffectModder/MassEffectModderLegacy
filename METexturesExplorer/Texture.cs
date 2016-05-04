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

namespace METexturesExplorer
{
    class Texture
    {
        enum StorageTypes
        {
            pccUnc = 0x0,
            pccCpr = 0x10,
            extUnc = 0x1,
            extCpr = 0x11,
            arcCpr = 0x3,
        }

        int numMipMaps;
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

            MemoryStream mem = new MemoryStream(data, properties.propertyEndOffset, data.Length - properties.propertyEndOffset);
            if (GameData.gameType != MeType.ME3_TYPE)
            {
                mem.Skip(12); // 12 zeros
                mem.SkipInt32(); // position in the package
            }

            mipMapsList = new List<Bitmap>();
            numMipMaps = mem.ReadInt32();
            for (int l = 0; l < numMipMaps; l++)
            {
                Bitmap bmp = new Bitmap();
                bmp.storageType = (StorageTypes)mem.ReadInt32();
                bmp.uncompressedSize = mem.ReadInt32();
                bmp.compressedSize = mem.ReadInt32();
                bmp.dataOffset = mem.ReadInt32();
                if (bmp.storageType == StorageTypes.pccUnc)
                {
                    bmp.dataOffset = (int)mem.Position;
                    mem.Skip(bmp.uncompressedSize);
                }
                if (bmp.storageType == StorageTypes.pccCpr)
                {
                    bmp.dataOffset = (int)mem.Position;
                    mem.Skip(bmp.compressedSize);
                }
                bmp.width = mem.ReadInt32();
                bmp.height = mem.ReadInt32();
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
    }
}
