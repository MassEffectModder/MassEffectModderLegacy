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
        struct DDS_PF
        {
            public uint flags;
            public uint fourCC;
            public uint bits;
            public uint Rmask;
            public uint Gmask;
            public uint Bmask;
            public uint Amask;
        }

        public const int DDS_TAG = 0x20534444;
        private const int DDS_HEADER_dwSize = 124;
        private const int DDS_PIXELFORMAT_dwSize = 32;

        private const int DDPF_ALPHAPIXELS = 0x1;
        private const int DDPF_FOURCC = 0x4;
        private const int DDPF_RGB = 0x40;
        private const int DDPF_LUMINANCE = 0x20000;

        private const int FOURCC_DX10_TAG = 0x30315844;
        private const int FOURCC_DXT1_TAG = 0x31545844;
        private const int FOURCC_DXT3_TAG = 0x33545844;
        private const int FOURCC_DXT5_TAG = 0x35545844;
        private const int FOURCC_ATI2_TAG = 0x32495441;

        private const int DDSD_CAPS = 0x1;
        private const int DDSD_HEIGHT = 0x2;
        private const int DDSD_WIDTH = 0x4;
        private const int DDSD_PIXELFORMAT = 0x1000;
        private const int DDSD_MIPMAPCOUNT = 0x20000;
        private const int DDSD_LINEARSIZE = 0x80000;

        private const int DDSCAPS_COMPLEX = 0x8;
        private const int DDSCAPS_TEXTURE = 0x1000;
        private const int DDSCAPS_MIPMAP = 0x400000;

        private DDS_PF ddsPixelFormat = new DDS_PF();
        private uint DDSflags;

        private void LoadImageDDS(MemoryStream stream, ImageFormat format)
        {
            if (stream.ReadUInt32() != DDS_TAG)
                throw new Exception("not DDS tag");

            if (stream.ReadInt32() != DDS_HEADER_dwSize)
                throw new Exception("wrong DDS header dwSize");

            DDSflags = stream.ReadUInt32();

            int dwHeight = stream.ReadInt32();
            int dwWidth = stream.ReadInt32();
            if (!checkPowerOfTwo(dwWidth) ||
                !checkPowerOfTwo(dwHeight))
                throw new Exception("dimensions not power of two");

            stream.Skip(8); // dwPitchOrLinearSize, dwDepth

            int dwMipMapCount = 1;
            if ((DDSflags & DDSD_MIPMAPCOUNT) != 0)
                dwMipMapCount = stream.ReadInt32();
            if (dwMipMapCount == 0)
                dwMipMapCount = 1;

            stream.Skip(11 * 4); // dwReserved1
            stream.SkipInt32(); // ppf.dwSize

            ddsPixelFormat.flags = stream.ReadUInt32();
            ddsPixelFormat.fourCC = stream.ReadUInt32();
            if ((ddsPixelFormat.flags & DDPF_FOURCC) != 0 && ddsPixelFormat.fourCC == FOURCC_DX10_TAG)
                throw new Exception("DX10 DDS format not supported");

            ddsPixelFormat.bits = stream.ReadUInt32();
            ddsPixelFormat.Rmask = stream.ReadUInt32();
            ddsPixelFormat.Gmask = stream.ReadUInt32();
            ddsPixelFormat.Bmask = stream.ReadUInt32();
            ddsPixelFormat.Amask = stream.ReadUInt32();

            switch (ddsPixelFormat.fourCC)
            {
                case 0:
                    if (ddsPixelFormat.bits == 32 &&
                        (ddsPixelFormat.flags & DDPF_ALPHAPIXELS) != 0 &&
                           ddsPixelFormat.Rmask == 0xFF0000 &&
                           ddsPixelFormat.Gmask == 0xFF00 &&
                           ddsPixelFormat.Bmask == 0xFF &&
                           ddsPixelFormat.Amask == 0xFF000000)
                    {
                        hasAlpha = true;
                        pixelFormat = PixelFormat.ARGB;
                        break;
                    }
                    if (ddsPixelFormat.bits == 24 &&
                           ddsPixelFormat.Rmask == 0xFF0000 &&
                           ddsPixelFormat.Gmask == 0xFF00 &&
                           ddsPixelFormat.Bmask == 0xFF)
                    {
                        pixelFormat = PixelFormat.RGB;
                        break;
                    }
                    if (ddsPixelFormat.bits == 16 &&
                           ddsPixelFormat.Rmask == 0xFF &&
                           ddsPixelFormat.Gmask == 0xFF00 &&
                           ddsPixelFormat.Bmask == 0x00)
                    {
                        pixelFormat = PixelFormat.V8U8;
                        break;
                    }
                    if (ddsPixelFormat.bits == 0x8 &&
                           ddsPixelFormat.Rmask == 0xFF &&
                           ddsPixelFormat.Gmask == 0x00 &&
                           ddsPixelFormat.Bmask == 0x00)
                    {
                        pixelFormat = PixelFormat.G8;
                        break;
                    }
                    throw new Exception("Not supported DDS format");

                case 21:
                    hasAlpha = true;
                    pixelFormat = PixelFormat.ARGB;
                    break;

                case 20:
                    pixelFormat = PixelFormat.RGB;
                    break;

                case 60:
                    pixelFormat = PixelFormat.V8U8;
                    break;

                case 50:
                    pixelFormat = PixelFormat.G8;
                    break;

                case FOURCC_DXT1_TAG:
                    if ((ddsPixelFormat.flags & DDPF_ALPHAPIXELS) != 0)
                        hasAlpha = true;
                    pixelFormat = PixelFormat.DXT1;
                    break;

                case FOURCC_DXT3_TAG:
                    hasAlpha = true;
                    pixelFormat = PixelFormat.DXT3;
                    break;

                case FOURCC_DXT5_TAG:
                    hasAlpha = true;
                    pixelFormat = PixelFormat.DXT5;
                    break;

                default:
                    throw new Exception("Not supported DDS format");
            }
            stream.Skip(5 * 4); // dwCaps, dwCaps2, dwCaps3, dwCaps4, dwReserved2

            byte[] tempData;
            for (int i = 0; i < dwMipMapCount; i++)
            {
                int w = dwWidth >> i;
                int h = dwHeight >> i;
                int origW = w;
                int origH = h;
                if (origW == 0 && origH != 0)
                    origW = 1;
                if (origH == 0 && origW != 0)
                    origH = 1;
                w = origW;
                h = origH;

                if (pixelFormat == PixelFormat.DXT1 ||
                    pixelFormat == PixelFormat.DXT3 ||
                    pixelFormat == PixelFormat.DXT5)
                {
                    if (w < 4)
                        w = 4;
                    if (h < 4)
                        h = 4;
                }

                try
                {
                    tempData = stream.ReadToBuffer(MipMap.getBufferSize(origW, origH, pixelFormat));
                }
                catch
                {
                    throw new Exception("not enough data in stream");
                }

                mipMaps.Add(new MipMap(tempData, w, h, pixelFormat));
            }
        }

        public bool checkDDSHaveAllMipmaps()
        {
            if ((DDSflags & DDSD_MIPMAPCOUNT) != 0 && mipMaps.Count > 1)
            {
                int width = mipMaps[0].origWidth;
                int height = mipMaps[0].origHeight;
                for (int i = 0; i < mipMaps.Count; i++)
                {
                    if (mipMaps[i].origWidth < 4 || mipMaps[i].origHeight < 4)
                        return true;
                    if (mipMaps[i].origWidth != width && mipMaps[i].origHeight != height)
                        return false;
                    width /= 2;
                    height /= 2;
                }
                return true;
            }
            else
            {
                return true;
            }
        }

        public Image convertToARGB()
        {
            for (int i = 0; i < mipMaps.Count; i++)
            {
                mipMaps[i] = new MipMap(convertRawToARGB(mipMaps[i].data, mipMaps[i].width, mipMaps[i].height, pixelFormat),
                    mipMaps[i].width, mipMaps[i].height, PixelFormat.ARGB);
            }
            pixelFormat = PixelFormat.ARGB;
            hasAlpha = true;

            return this;
        }

        public Image convertToRGB()
        {
            for (int i = 0; i < mipMaps.Count; i++)
            {
                mipMaps[i] = new MipMap(convertRawToRGB(mipMaps[i].data, mipMaps[i].width, mipMaps[i].height, pixelFormat),
                    mipMaps[i].width, mipMaps[i].height, PixelFormat.RGB);
            }
            pixelFormat = PixelFormat.RGB;
            hasAlpha = false;

            return this;
        }

        private DDS_PF getDDSPixelFormat(PixelFormat format)
        {
            DDS_PF pixelFormat = new DDS_PF();
            switch (format)
            {
                case PixelFormat.DXT1:
                    pixelFormat.flags = DDPF_FOURCC;
                    if (hasAlpha)
                        pixelFormat.flags |= DDPF_ALPHAPIXELS;
                    pixelFormat.fourCC = FOURCC_DXT1_TAG;
                    break;

                case PixelFormat.DXT3:
                    pixelFormat.flags = DDPF_FOURCC | DDPF_ALPHAPIXELS;
                    pixelFormat.fourCC = FOURCC_DXT3_TAG;
                    break;

                case PixelFormat.DXT5:
                    pixelFormat.flags = DDPF_FOURCC | DDPF_ALPHAPIXELS;
                    pixelFormat.fourCC = FOURCC_DXT5_TAG;
                    break;

                case PixelFormat.ATI2:
                    pixelFormat.flags = DDPF_FOURCC;
                    pixelFormat.fourCC = FOURCC_ATI2_TAG;
                    break;

                case PixelFormat.ARGB:
                    pixelFormat.flags = DDPF_ALPHAPIXELS | DDPF_RGB;
                    pixelFormat.bits = 0x20;
                    pixelFormat.Rmask = 0xFF0000;
                    pixelFormat.Gmask = 0xFF00;
                    pixelFormat.Bmask = 0xFF;
                    pixelFormat.Amask = 0xFF000000;
                    break;

                case PixelFormat.RGB:
                    pixelFormat.flags = DDPF_RGB;
                    pixelFormat.bits = 0x18;
                    pixelFormat.Rmask = 0xFF0000;
                    pixelFormat.Gmask = 0xFF00;
                    pixelFormat.Bmask = 0xFF;
                    break;

                case PixelFormat.V8U8:
                    pixelFormat.flags = 0x80000;
                    pixelFormat.bits = 0x10;
                    pixelFormat.Rmask = 0xFF;
                    pixelFormat.Gmask = 0xFF00;
                    pixelFormat.Bmask = 0x00;
                    break;

                case PixelFormat.G8:
                    pixelFormat.flags = DDPF_LUMINANCE;
                    pixelFormat.bits = 0x08;
                    pixelFormat.Rmask = 0xFF;
                    pixelFormat.Gmask = 0x00;
                    pixelFormat.Bmask = 0x00;
                    break;

                default:
                    throw new Exception("invalid texture format " + this.pixelFormat);
            }
            return pixelFormat;
        }

        public void StoreImageToDDS(Stream stream, PixelFormat format = PixelFormat.Unknown)
        {
            stream.WriteUInt32(DDS_TAG);
            stream.WriteInt32(DDS_HEADER_dwSize);
            stream.WriteUInt32(DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_MIPMAPCOUNT | DDSD_PIXELFORMAT | DDSD_LINEARSIZE);
            stream.WriteInt32(mipMaps[0].height);
            stream.WriteInt32(mipMaps[0].width);

            int dataSize = 0;
            for (int i = 0; i < mipMaps.Count; i++)
                dataSize += MipMap.getBufferSize(mipMaps[i].width, mipMaps[i].height, format == PixelFormat.Unknown ? pixelFormat : format);
            stream.WriteInt32(dataSize);

            stream.WriteUInt32(0); // dwDepth
            stream.WriteInt32(mipMaps.Count);
            stream.WriteZeros(44); // dwReserved1

            stream.WriteInt32(DDS_PIXELFORMAT_dwSize);
            DDS_PF pixfmt = getDDSPixelFormat(format == PixelFormat.Unknown ? pixelFormat: format);
            stream.WriteUInt32(pixfmt.flags);
            stream.WriteUInt32(pixfmt.fourCC);
            stream.WriteUInt32(pixfmt.bits);
            stream.WriteUInt32(pixfmt.Rmask);
            stream.WriteUInt32(pixfmt.Gmask);
            stream.WriteUInt32(pixfmt.Bmask);
            stream.WriteUInt32(pixfmt.Amask);

            stream.WriteInt32(DDSCAPS_COMPLEX | DDSCAPS_MIPMAP | DDSCAPS_TEXTURE);
            stream.WriteUInt32(0); // dwCaps2
            stream.WriteUInt32(0); // dwCaps3
            stream.WriteUInt32(0); // dwCaps4
            stream.WriteUInt32(0); // dwReserved2
            for (int i = 0; i < mipMaps.Count; i++)
            {
                stream.WriteFromBuffer(mipMaps[i].data);
            }
        }

        public byte[] StoreImageToDDS()
        {
            MemoryStream stream = new MemoryStream();
            StoreImageToDDS(stream);
            return stream.ToArray();
        }

    }
}
