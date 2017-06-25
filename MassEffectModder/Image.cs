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
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StreamHelpers;
using AmaroK86.ImageFormat;

namespace MassEffectModder
{
    public class MipMap
    {
        private byte[] data;
        public int width;
        public int height;

        public MipMap(byte[] data, int w, int h)
        {
            width = w;
            height = h;
            if (data.Length != w * h * 4)
                throw new InvalidDataException("Data size is not valid.");
            this.data = data;
        }

        public byte[] getData()
        {
            return data;
        }
    }

    public class Image
    {
        public enum ImageFormat
        {
            Unknown, DDS, PNG, BMP, TGA, JPEG
        }

        public enum DDSFormat
        {
            DXT1, DXT3, DXT5, ATI2, V8U8, ARGB, RGB, G8
        }

        private const int BMP_TAG = 0x4D42;

        public const int DDS_TAG = 0x20534444;
        private const int DDS_HEADER_dwSize = 124;
        private const int DDS_PIXELFORMAT_dwSize = 32;

        private const int DDPF_ALPHAPIXELS = 0x1;
        private const int DDPF_FOURCC = 0x4;
        private const int DDPF_RGB = 0x40;
        private const int DDPF_LUMINANCE = 0x20000;

        private const int DDSD_CAPS = 0x1;
        private const int DDSD_HEIGHT = 0x2;
        private const int DDSD_WIDTH = 0x4;
        private const int DDSD_PIXELFORMAT = 0x1000;
        private const int DDSD_MIPMAPCOUNT = 0x20000;
        private const int DDSD_LINEARSIZE = 0x80000;

        private const int DDSCAPS_COMPLEX = 0x8;
        private const int DDSCAPS_TEXTURE = 0x1000;
        private const int DDSCAPS_MIPMAP = 0x400000;

        public List<MipMap> mipMaps;
        private bool hasAlpha;

        public Image(string fileName, ImageFormat format = ImageFormat.Unknown)
        {
            if (format == ImageFormat.Unknown)
                format = DetectImageByFilename(fileName);

            using (FileStream stream = File.OpenRead(fileName))
            {
                LoadImage(new MemoryStream(stream.ReadToBuffer(stream.Length)), format);
            }
        }

        public Image(MemoryStream stream, ImageFormat format)
        {
            LoadImage(stream, format);
        }

        public Image(MemoryStream stream, string extension)
        {
            LoadImage(stream, DetectImageByExtension(extension));
        }

        public Image(byte[] image, ImageFormat format)
        {
            LoadImage(new MemoryStream(image), format);
        }

        public Image(byte[] image, string extension)
        {
            LoadImage(new MemoryStream(image), DetectImageByExtension(extension));
        }

        private ImageFormat DetectImageByFilename(string fileName)
        {
            return DetectImageByExtension(Path.GetExtension(fileName));
        }

        private ImageFormat DetectImageByExtension(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".dds":
                    return ImageFormat.DDS;
                case ".tga":
                    return ImageFormat.TGA;
                case ".bmp":
                    return ImageFormat.BMP;
                case ".png":
                    return ImageFormat.PNG;
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.JPEG;
                default:
                    return ImageFormat.Unknown;
            }
        }

        private void LoadImage(MemoryStream stream, ImageFormat format)
        {
            mipMaps = new List<MipMap>();
            switch (format)
            {
                case ImageFormat.DDS:
                    {
                        DDSImage image = new DDSImage(stream);
                        hasAlpha = image.hasAlpha;
                        foreach (DDSImage.MipMap ddsMipmap in image.mipMaps)
                        {
                            MipMap mipmap = new MipMap(DDSImage.ToARGB(ddsMipmap), ddsMipmap.width, ddsMipmap.height);
                            mipMaps.Add(mipmap);
                        }
                        break;
                    }
                case ImageFormat.TGA:
                    {
                        int idLength = stream.ReadByte();
                        int colorMapType = stream.ReadByte();
                        if (colorMapType != 0)
                            throw new Exception("indexed TGA not supported!");
                        int imageType = stream.ReadByte();
                        if (imageType != 2 && imageType != 10)
                            throw new Exception("only RGB TGA supported!");
                        bool compressed = false;
                        if (imageType == 10)
                            compressed = true;
                        stream.SkipInt16(); // color map first entry index
                        stream.SkipInt16(); // color map length
                        stream.Skip(1); // color map entry size
                        stream.SkipInt16(); // x origin
                        stream.SkipInt16(); // y origin
                        int imageWidth = stream.ReadInt16();
                        int imageHeight = stream.ReadInt16();
                        int imageDepth = stream.ReadByte();
                        if (imageDepth != 32 && imageDepth != 24)
                            throw new Exception("only 24 and 32 bits TGA supported!");
                        int imageDesc = stream.ReadByte();
                        if ((imageDesc & 0x10) != 0)
                            throw new Exception("origin right not supported in TGA!");
                        bool downToTop = true;
                        if ((imageDesc & 0x20) != 0)
                            downToTop = false;
                        stream.Skip(idLength);

                        byte[] buffer = new byte[imageWidth * imageHeight * 4];
                        int pos = downToTop ? imageWidth * (imageHeight - 1) * 4 : 0;
                        int delta = downToTop ? -imageWidth * 4 * 2 : 0;
                        if (compressed)
                        {
                            int count = 0, repeat = 0, w = 0, h = 0;
                            for (;;)
                            {
                                if (count == 0 && repeat == 0)
                                {
                                    byte code = (byte)stream.ReadByte();
                                    if ((code & 0x80) != 0)
                                        repeat = (code & 0x7F) + 1;
                                    else
                                        count = code + 1;
                                }
                                else
                                {
                                    byte pixelR, pixelG, pixelB, pixelA;
                                    if (repeat != 0)
                                    {
                                        pixelR = (byte)stream.ReadByte();
                                        pixelG = (byte)stream.ReadByte();
                                        pixelB = (byte)stream.ReadByte();
                                        if (imageDepth == 32)
                                            pixelA = (byte)stream.ReadByte();
                                        else
                                            pixelA = 0xFF;
                                        for (; w < imageWidth && repeat > 0; w++, repeat--)
                                        {
                                            buffer[pos++] = pixelR;
                                            buffer[pos++] = pixelG;
                                            buffer[pos++] = pixelB;
                                            buffer[pos++] = pixelA;
                                        }
                                    }
                                    else
                                    {
                                        for (; w < imageWidth && count > 0; w++, count--)
                                        {
                                            buffer[pos++] = (byte)stream.ReadByte();
                                            buffer[pos++] = (byte)stream.ReadByte();
                                            buffer[pos++] = (byte)stream.ReadByte();
                                            if (imageDepth == 32)
                                                buffer[pos++] = (byte)stream.ReadByte();
                                            else
                                                buffer[pos++] = 0xFF;
                                        }
                                    }
                                }

                                if (w == imageWidth)
                                {
                                    w = 0;
                                    pos += delta;
                                    if (++h == imageHeight)
                                        break;
                                }
                            }
                        }
                        else
                        {
                            for (int h = 0; h < imageHeight; h++, pos += delta)
                            {
                                for (int w = 0; w < imageWidth; w++)
                                {
                                    buffer[pos++] = (byte)stream.ReadByte();
                                    buffer[pos++] = (byte)stream.ReadByte();
                                    buffer[pos++] = (byte)stream.ReadByte();
                                    if (imageDepth == 32)
                                        buffer[pos++] = (byte)stream.ReadByte();
                                    else
                                        buffer[pos++] = 0xFF;
                                }
                            }
                        }

                        MipMap mipmap = new MipMap(buffer, imageWidth, imageHeight);
                        mipMaps.Add(mipmap);
                        break;
                    }
                case ImageFormat.BMP:
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

                        MipMap mipmap = new MipMap(buffer, imageWidth, imageHeight);
                        mipMaps.Add(mipmap);
                        break;
                    }
                case ImageFormat.PNG:
                    {
                        PngBitmapDecoder bmp = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                        BitmapSource frame = bmp.Frames[0];
                        FormatConvertedBitmap srcBitmap = new FormatConvertedBitmap();
                        srcBitmap.BeginInit();
                        srcBitmap.Source = bmp.Frames[0];
                        srcBitmap.DestinationFormat = PixelFormats.Bgra32;
                        srcBitmap.EndInit();
                        byte[] pixels = new byte[srcBitmap.PixelWidth * srcBitmap.PixelHeight * 4];
                        frame.CopyPixels(pixels, srcBitmap.PixelWidth * 4, 0);
                        MipMap mipmap = new MipMap(pixels, srcBitmap.PixelWidth, srcBitmap.PixelHeight);
                        mipMaps.Add(mipmap);
                        break;
                    }
                case ImageFormat.JPEG:
                    {
                        JpegBitmapDecoder bmp = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);
                        BitmapSource frame = bmp.Frames[0];
                        FormatConvertedBitmap srcBitmap = new FormatConvertedBitmap();
                        srcBitmap.BeginInit();
                        srcBitmap.Source = bmp.Frames[0];
                        srcBitmap.DestinationFormat = PixelFormats.Bgra32;
                        srcBitmap.EndInit();
                        byte[] pixels = new byte[srcBitmap.PixelWidth * srcBitmap.PixelHeight * 4];
                        frame.CopyPixels(pixels, srcBitmap.PixelWidth * 4, 0);
                        MipMap mipmap = new MipMap(pixels, srcBitmap.PixelWidth, srcBitmap.PixelHeight);
                        mipMaps.Add(mipmap);
                        break;
                    }
                default:
                    throw new Exception();
            }
        }

        public void StoreARGBImageToDDS(Stream stream)
        {
            stream.WriteUInt32(DDS_TAG);
            stream.WriteInt32(DDS_HEADER_dwSize);
            stream.WriteUInt32(DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_MIPMAPCOUNT | DDSD_PIXELFORMAT | DDSD_LINEARSIZE);
            stream.WriteInt32(mipMaps[0].height);
            stream.WriteInt32(mipMaps[0].width);

            int dataSize = 0;
            for (int i = 0; i < mipMaps.Count; i++)
                dataSize += 4 * mipMaps[i].width * mipMaps[i].height;
            stream.WriteInt32(dataSize);

            stream.WriteUInt32(0); // dwDepth
            stream.WriteInt32(mipMaps.Count);
            stream.WriteZeros(44); // dwReserved1

            stream.WriteInt32(DDS_PIXELFORMAT_dwSize);
            stream.WriteUInt32(DDPF_ALPHAPIXELS | DDPF_RGB); // dwFlags
            stream.WriteUInt32(0); // dwFourCC
            stream.WriteUInt32(32); // dwRGBBitCount
            stream.WriteUInt32(0xFF0000); // dwRBitMask
            stream.WriteUInt32(0xFF00); // dwGBitMask
            stream.WriteUInt32(0xFF); // dwBBitMask
            stream.WriteUInt32(0xFF000000); // dwABitMask

            stream.WriteInt32(DDSCAPS_COMPLEX | DDSCAPS_MIPMAP | DDSCAPS_TEXTURE);
            stream.WriteUInt32(0); // dwCaps2
            stream.WriteUInt32(0); // dwCaps3
            stream.WriteUInt32(0); // dwCaps4
            stream.WriteUInt32(0); // dwReserved2
            for (int i = 0; i < mipMaps.Count; i++)
            {
                stream.WriteFromBuffer(mipMaps[i].getData());
            }
        }

        public void SaveARGBImageToDDS(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
            {
                StoreARGBImageToDDS(fs);
            }
        }

        public byte[] StoreARGBImageToDDS()
        {
            MemoryStream stream = new MemoryStream();
            StoreARGBImageToDDS(stream);
            return stream.ToArray();
        }

        public bool checkMipmaps()
        {
            int width = mipMaps[0].width;
            int height = mipMaps[0].height;
            for (int i = 0; i < mipMaps.Count; i++)
            {
                if (mipMaps[i].width < 4 || mipMaps[i].height < 4)
                    return true;
                if (mipMaps[i].width != width && mipMaps[i].height != height)
                    return false;
                width /= 2;
                height /= 2;
            }
            return true;
        }

    }
}
