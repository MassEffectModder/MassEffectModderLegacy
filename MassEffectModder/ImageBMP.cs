/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using StreamHelpers;

namespace MassEffectModder
{
    public partial class Image
    {
        private const int BMP_TAG = 0x4D42;

        private void LoadImageBMP(MemoryStream stream, ImageFormat format)
        {
            ushort tag = stream.ReadUInt16();
            if (tag != BMP_TAG)
                throw new Exception("not BMP header");

            stream.Skip(8);

            uint offsetData = stream.ReadUInt32();
            int headerSize = stream.ReadInt32();

            int imageWidth = stream.ReadInt32();
            int imageHeight = stream.ReadInt32();
            if (imageHeight < 0)
                throw new Exception("down to top not supported in BMP!");
            if (!checkPowerOfTwo(imageWidth) ||
                !checkPowerOfTwo(imageHeight))
                throw new Exception("dimensions not power of two");

            stream.Skip(2);

            int bits = stream.ReadUInt16();
            if (bits != 32 && bits != 24)
                throw new Exception("only 24 and 32 bits BMP supported!");

            uint Rmask = 0, Gmask = 0, Bmask = 0, Amask = 0;
            if (headerSize >= 40)
            {
                int compression = stream.ReadInt32();
                if (compression == 1 || compression == 2)
                    throw new Exception("compression not supported in BMP!");

                if (compression == 3)
                {
                    stream.Skip(20);
                    Rmask = stream.ReadUInt32();
                    Gmask = stream.ReadUInt32();
                    Bmask = stream.ReadUInt32();
                    if (headerSize >= 56)
                        Amask = stream.ReadUInt32();
                }

                stream.JumpTo(headerSize + 14);
            }

            byte[] buffer = new byte[imageWidth * imageHeight * 4];
            int pos = 0;
            for (int h = 0; h < imageHeight; h++)
            {
                for (int i = 0; i < imageWidth; i++)
                {
                    if (bits == 24)
                    {
                        buffer[pos++] = (byte)stream.ReadByte();
                        buffer[pos++] = (byte)stream.ReadByte();
                        buffer[pos++] = (byte)stream.ReadByte();
                        buffer[pos++] = 0xff;
                    }
                    else if (bits == 32)
                    {
                        uint p1 = (uint)stream.ReadByte();
                        uint p2 = (uint)stream.ReadByte();
                        uint p3 = (uint)stream.ReadByte();
                        uint p4 = (uint)stream.ReadByte();
                        uint pixel = p4 << 24 | p3 << 16 | p2 << 8 | p1;
                        buffer[pos++] = (byte)((pixel & Gmask) >> 16);
                        buffer[pos++] = (byte)((pixel & Bmask) >> 8);
                        buffer[pos++] = (byte)((pixel & Rmask) >> 24);
                        buffer[pos++] = (byte)((pixel & Amask) >> 0);
                    }
                }
                if (imageWidth % 4 != 0)
                    stream.Skip(4 - (imageWidth % 4));
            }

            MipMap mipmap = new MipMap(buffer, imageWidth, imageHeight, PixelFormat.ARGB);
            mipMaps.Add(mipmap);
        }
    }
}
