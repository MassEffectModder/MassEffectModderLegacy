/*  Copyright (C) 2013 AmaroK86 (marcidm 'at' hotmail 'dot' com)
 *  Copyright (C) 2013 saltisgood
 *  Copyright (C) 2013-2014 KFreon
 *  Copyright (C) 2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.

 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.

 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

/*
 * This is a heavy modification of
 * original project took from:
 * http://code.google.com/p/kprojects/
 *
 * Kons 2012-12-03 Version .1
 * Supported features:
 * - DXT1
 * - DXT5
 * - V8U8 (by AmaroK86)
 *
 * contact: kons.snok<at>gmail.com
 */

using System;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using StreamHelpers;

namespace AmaroK86.ImageFormat
{
    public enum DDSFormat
    {
        DXT1, DXT3, DXT5, V8U8, ATI2, G8, ARGB, RGB
    }

    public enum ATI2BitCodes : ulong
    {
        color0 = 0xFFFFFFFFFFFFFFFFUL, // checks 000
        color1 = 0xDFFFFFFFFFFFFFFFUL, // 001
        interpColor0 = 0xBFFFFFFFFFFFFFFFUL, // 010
        interpColor1 = 0x9FFFFFFFFFFFFFFFUL, // 011
        interpColor2 = 0x7FFFFFFFFFFFFFFFUL, // 100
        interpColor3 = 0x5FFFFFFFFFFFFFFFUL, // 101
        interpColor4 = 0x3FFFFFFFFFFFFFFFUL, // 110
        interpColor5 = 0x1FFFFFFFFFFFFFFFUL, // 111
        result = 0xE000000000000000UL // The expected value on a successful test
    }

    public class DDSImage
    {
        public class MipMap
        {
            public int width;
            public int height;
            public int origWidth;
            public int origHeight;
            DDSFormat ddsFormat;
            private byte[] _data;
            public byte[] data
            {
                get
                {
                    return _data;
                }
                set
                {
                    _data = value;
                    _bitmap = null;
                }
            }
            private Bitmap _bitmap;
            public Bitmap bitmap
            {
                get
                {
                    if (_bitmap == null)
                    {
                        _bitmap = ToBitmap(data, ddsFormat, width, height);
                    }
                    return _bitmap;
                }
            }

            public MipMap(byte[] data, DDSFormat format, int w, int h, int origW , int origH)
            {
                long requiredSize = (long)(w * h * getBytesPerPixel(format));
                if (data.Length != requiredSize)
                    throw new InvalidDataException("Data size is not valid for selected format.\nActual: " + data.Length + " bytes\nRequired: " + requiredSize + " bytes");

                this.data = data;
                ddsFormat = format;
                width = w;
                height = h;
                origWidth = origW;
                origHeight = origH;
            }
        }

        private const int DDPF_ALPHAPIXELS = 0x00000001;
        private const int DDPF_ALPHA = 0x00000002;
        private const int DDPF_FOURCC = 0x00000004;
        private const int DDPF_RGB = 0x00000040;
        private const int DDPF_YUV = 0x00000200;
        private const int DDPF_LUMINANCE = 0x00020000;
        private const int DDSD_MIPMAPCOUNT = 0x00020000;
        private const int FOURCC_DXT1 = 0x31545844;
        private const int FOURCC_DX10 = 0x30315844;
        private const int FOURCC_DXT5 = 0x35545844;
        private const int FOURCC_ATI2 = 0x32495441;

        public int dwMagic;
        private DDS_HEADER header = new DDS_HEADER();
        public DDSFormat ddsFormat { get; private set; }

        public MipMap[] mipMaps;

        public DDSImage(string ddsFileName, bool bypassCheck = false)
        {
            using (FileStream ddsStream = File.OpenRead(ddsFileName))
            {
                LoadDDSImage(new MemoryStream(ddsStream.ReadToBuffer(ddsStream.Length)), bypassCheck);
            }
        }

        public DDSImage(MemoryStream ddsStream, bool bypassCheck = false)
        {
            LoadDDSImage(ddsStream, bypassCheck);
        }

        private void LoadDDSImage(MemoryStream ddsStream, bool bypassCheck = false)
        {
            using (BinaryReader r = new BinaryReader(ddsStream))
            {
                dwMagic = r.ReadInt32();
                if (dwMagic != 0x20534444)
                {
                    throw new Exception("This is not a DDS!");
                }

                Read_DDS_HEADER(header, r);

                if (((header.ddspf.dwFlags & DDPF_FOURCC) != 0) && (header.ddspf.dwFourCC == FOURCC_DX10 /*DX10*/))
                {
                    throw new Exception("DX10 not supported yet!");
                }

                int mipMapCount = 1;
                if ((header.dwFlags & DDSD_MIPMAPCOUNT) != 0)
                    mipMapCount = header.dwMipMapCount;

                mipMaps = new MipMap[mipMapCount];

                ddsFormat = getFormat();

                double bytePerPixel = getBytesPerPixel(ddsFormat);

                for (int i = 0; i < mipMapCount; i++)
                {
                    int w = (int)(header.dwWidth / Math.Pow(2, i));
                    int h = (int)(header.dwHeight / Math.Pow(2, i));

                    int origW = w;
                    int origH = h;
                    if (origW == 0 && origH != 0)
                        origW = 1;
                    if (origH == 0 && origW != 0)
                        origH = 1;
                    if (ddsFormat == DDSFormat.DXT1 || ddsFormat == DDSFormat.DXT5)
                    {
                        w = (w < 4) ? 4 : w;
                        h = (h < 4) ? 4 : h;
                    }

                    int mipMapBytes = (int)(w * h * bytePerPixel);
                    byte[] data = r.ReadBytes(mipMapBytes);
                    mipMaps[i] = new MipMap(data, ddsFormat, w, h, origW, origH);
                }
                if (!bypassCheck && !checkExistAllMipmaps())
                    throw new Exception("DDS doesn't have all mipmaps!");
            }
        }

        public bool checkExistAllMipmaps()
        {
            if ((header.dwFlags & DDSD_MIPMAPCOUNT) != 0 && header.dwMipMapCount > 1)
            {
                int val = Math.Max(mipMaps[0].origWidth, mipMaps[0].origHeight);
                int count = 0;
                do
                {
                    val /= 2;
                    count++;
                } while (val != 0);
                if (count == mipMaps.Length)
                    return true;
                else
                    return false;
            }
            else
            {
                return true;
            }
        }

        private DDSFormat getFormat()
        {
            switch (header.ddspf.dwFourCC)
            {
                case FOURCC_DXT1: return DDSFormat.DXT1;
                case FOURCC_DXT5: return DDSFormat.DXT5;
                case FOURCC_ATI2: return DDSFormat.ATI2;
                case 0:
                    if (header.ddspf.dwRGBBitCount == 0x10 &&
                           header.ddspf.dwRBitMask == 0xFF &&
                           header.ddspf.dwGBitMask == 0xFF00 &&
                           header.ddspf.dwBBitMask == 0x00 &&
                           header.ddspf.dwABitMask == 0x00)
                        return DDSFormat.V8U8;
                    if (header.ddspf.dwRGBBitCount == 0x8 &&
                           header.ddspf.dwRBitMask == 0xFF &&
                           header.ddspf.dwGBitMask == 0x00 &&
                           header.ddspf.dwBBitMask == 0x00 &&
                           header.ddspf.dwABitMask == 0x00)
                        return DDSFormat.G8;
                    if (header.ddspf.dwRGBBitCount == 0x20 &&
                           header.ddspf.dwRBitMask == 0xFF0000 &&
                           header.ddspf.dwGBitMask == 0xFF00 &&
                           header.ddspf.dwBBitMask == 0xFF &&
                           header.ddspf.dwABitMask == 0xFF000000)
                        return DDSFormat.ARGB;
                    if (header.ddspf.dwRGBBitCount == 0x18 &&
                           header.ddspf.dwRBitMask == 0xFF0000 &&
                           header.ddspf.dwGBitMask == 0xFF00 &&
                           header.ddspf.dwBBitMask == 0xFF &&
                           header.ddspf.dwABitMask == 0x00)
                        return DDSFormat.RGB;
                    break;
                default: break;
            }
            throw new Exception("invalid texture format");
        }

        public static double getBytesPerPixel(DDSFormat ddsFormat)
        {
            switch (ddsFormat)
            {
                case DDSFormat.DXT1: return 0.5;
                case DDSFormat.DXT5:
                case DDSFormat.ATI2:
                case DDSFormat.G8: return 1;
                case DDSFormat.V8U8: return 2;
                case DDSFormat.ARGB: return 4;
                case DDSFormat.RGB: return 3;
            }
            throw new Exception("invalid texture format");
        }

        public static DDSFormat convertFormat(string format)
        {
            switch (format)
            {
                case "PF_DXT1":
                    return DDSFormat.DXT1;
                case "PF_DXT5":
                    return DDSFormat.DXT5;
                case "PF_NormalMap_HQ":
                    return DDSFormat.ATI2;
                case "PF_V8U8":
                    return DDSFormat.V8U8;
                case "PF_A8R8G8B8":
                    return DDSFormat.ARGB;
                case "PF_R8G8B8":
                    return DDSFormat.RGB;
                case "PF_G8":
                    return DDSFormat.G8;
                default:
                    throw new Exception("invalid texture format");
            }
        }

        public static Bitmap ToBitmap(byte[] imgData, DDSFormat ddsFormat, int w, int h)
        {
            switch (ddsFormat)
            {
                case DDSFormat.DXT1: return UncompressDXT1(imgData, w, h);
                case DDSFormat.DXT5: return UncompressDXT5(imgData, w, h);
                case DDSFormat.ATI2: return UncompressATI2(imgData, w, h);
                case DDSFormat.V8U8: return UncompressV8U8(imgData, w, h);
                case DDSFormat.G8: return ViewG8(imgData, w, h);
                case DDSFormat.ARGB: return View32Bit(imgData, w, h);
                case DDSFormat.RGB: return View24Bit(imgData, w, h);
            }
            throw new Exception("invalid texture format " + ddsFormat);
        }

        #region DXT1
        private static Bitmap UncompressDXT1(byte[] imgData, int w, int h)
        {
            const int bufferSize = 8;
            byte[] blockStorage = new byte[bufferSize];
            MemoryStream bitmapStream = new MemoryStream(w * h * 2);
            BinaryWriter bitmapBW = new BinaryWriter(bitmapStream);

            int readPtr = 0;
            for (int s = 0; s < h; s += 4)
            {
                for (int t = 0; t < w; t += 4)
                {
                    Buffer.BlockCopy(imgData, readPtr, blockStorage, 0, bufferSize);
                    //DecompressBlockDXT1(i, j, buffer, res);
                    readPtr += bufferSize;
                    {
                        int color0 = blockStorage[0] | blockStorage[1] << 8;
                        int color1 = blockStorage[2] | blockStorage[3] << 8;

                        int temp;

                        temp = (color0 >> 11) * 255 + 16;
                        int r0 = ((temp >> 5) + temp) >> 5;
                        temp = ((color0 & 0x07E0) >> 5) * 255 + 32;
                        int g0 = ((temp >> 6) + temp) >> 6;
                        temp = (color0 & 0x001F) * 255 + 16;
                        int b0 = ((temp >> 5) + temp) >> 5;

                        temp = (color1 >> 11) * 255 + 16;
                        int r1 = ((temp >> 5) + temp) >> 5;
                        temp = ((color1 & 0x07E0) >> 5) * 255 + 32;
                        int g1 = ((temp >> 6) + temp) >> 6;
                        temp = (color1 & 0x001F) * 255 + 16;
                        int b1 = ((temp >> 5) + temp) >> 5;

                        int code = blockStorage[4] | blockStorage[5] << 8 | blockStorage[6] << 16 | blockStorage[7] << 24;

                        for (int j = 0; j < 4; j++)
                        {
                            bitmapStream.Seek(((s + j) * w * 4) + (t * 4), SeekOrigin.Begin);
                            for (int i = 0; i < 4; i++)
                            {
                                int fCol = 0;
                                int positionCode = ((code >> 2 * (4 * j + i)) & 0x03);

                                if (color0 > color1)
                                {
                                    switch (positionCode)
                                    {
                                        case 0:
                                            fCol = b0 | (g0 << 8) | (r0 << 16) | 0xFF << 24;
                                            break;
                                        case 1:
                                            fCol = b1 | (g1 << 8) | (r1 << 16) | 0xFF << 24;
                                            break;
                                        case 2:
                                            fCol = ((2 * b0 + b1) / 3) | (((2 * g0 + g1) / 3) << 8) | (((2 * r0 + r1) / 3) << 16) | (0xFF << 24);
                                            break;
                                        case 3:
                                            fCol = ((b0 + 2 * b1) / 3) | ((g0 + 2 * g1) / 3) << 8 | ((r0 + 2 * r1) / 3) << 16 | 0xFF << 24;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (positionCode)
                                    {
                                        case 0:
                                            fCol = b0 | g0 << 8 | r0 << 16 | 0xFF << 24;
                                            break;
                                        case 1:
                                            fCol = b1 | g1 << 8 | r1 << 16 | 0xFF << 24;
                                            break;
                                        case 2:
                                            fCol = ((b0 + b1) / 2) | ((g0 + g1) / 2) << 8 | ((r0 + r1) / 2) << 16 | 0xFF << 24;
                                            break;
                                        case 3:
                                            fCol = 0xFF << 24;
                                            break;
                                    }
                                }

                                bitmapBW.Write(fCol);
                            }
                        }
                    }
                }
            }

            byte[] imageData = bitmapStream.ToArray();
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0,
                                                    bmp.Width,
                                                    bmp.Height),
                                      ImageLockMode.WriteOnly,
                                      bmp.PixelFormat);
                //MessageBox.Show("empty bitmap stride: " + bmpData.Stride + "\n total bytes: " + bmpData.Stride * bmpData.Height + "\n imageData size : " + imageData.Length);

                Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }
        #endregion
        #region DXT5
        private static Bitmap UncompressDXT5(byte[] imgData, int w, int h)
        {
            const int bufferSize = 16;
            byte[] blockStorage = new byte[bufferSize];
            MemoryStream bitmapStream = new MemoryStream(w * h * 2);
            BinaryWriter bitmapBW = new BinaryWriter(bitmapStream);

            int ptr = 0;
            for (int s = 0; s < h; s += 4)
            {
                for (int t = 0; t < w; t += 4)
                {
                    Buffer.BlockCopy(imgData, ptr, blockStorage, 0, bufferSize);
                    ptr += bufferSize;
                    {
                        int color0 = blockStorage[8] | blockStorage[9] << 8;
                        int color1 = blockStorage[10] | blockStorage[11] << 8;

                        int temp;

                        temp = (color0 >> 11) * 255 + 16;
                        int r0 = ((temp >> 5) + temp) >> 5;
                        temp = ((color0 & 0x07E0) >> 5) * 255 + 32;
                        int g0 = ((temp >> 6) + temp) >> 6;
                        temp = (color0 & 0x001F) * 255 + 16;
                        int b0 = ((temp >> 5) + temp) >> 5;

                        temp = (color1 >> 11) * 255 + 16;
                        int r1 = ((temp >> 5) + temp) >> 5;
                        temp = ((color1 & 0x07E0) >> 5) * 255 + 32;
                        int g1 = ((temp >> 6) + temp) >> 6;
                        temp = (color1 & 0x001F) * 255 + 16;
                        int b1 = ((temp >> 5) + temp) >> 5;

                        int code = blockStorage[12] | blockStorage[13] << 8 | blockStorage[14] << 16 | blockStorage[15] << 24;

                        for (int j = 0; j < 4; j++)
                        {
                            bitmapStream.Seek(((s + j) * w * 4) + (t * 4), SeekOrigin.Begin);
                            for (int i = 0; i < 4; i++)
                            {
                                int fCol = 0;
                                int colorCode = (code >> 2 * (4 * j + i)) & 0x03;

                                switch (colorCode)
                                {
                                    case 0:
                                        fCol = b0 | g0 << 8 | r0 << 16 | 0xFF << 24;
                                        break;
                                    case 1:
                                        fCol = b1 | g1 << 8 | r1 << 16 | 0xFF << 24;
                                        break;
                                    case 2:
                                        fCol = (2 * b0 + b1) / 3 | (2 * g0 + g1) / 3 << 8 | (2 * r0 + r1) / 3 << 16 | 0xFF << 24;
                                        break;
                                    case 3:
                                        fCol = (b0 + 2 * b1) / 3 | (g0 + 2 * g1) / 3 << 8 | (r0 + 2 * r1) / 3 << 16 | 0xFF << 24;
                                        break;
                                }

                                bitmapBW.Write(fCol);
                            }
                        }
                    }
                }
            }

            byte[] imageData = bitmapStream.ToArray();
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0,
                                                    bmp.Width,
                                                    bmp.Height),
                                      ImageLockMode.WriteOnly,
                                      bmp.PixelFormat);
                //MessageBox.Show("empty bitmap stride: " + bmpData.Stride + "\n total bytes: " + bmpData.Stride * bmpData.Height + "\n imageData size : " + imageData.Length);

                Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }
        #endregion
        #region V8U8
        private static Bitmap UncompressV8U8(byte[] imgData, int w, int h)
        {
            MemoryStream bitmapStream = new MemoryStream(w * h * 2);
            BinaryWriter bitmapBW = new BinaryWriter(bitmapStream);
            int ptr = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    sbyte red = (sbyte)Buffer.GetByte(imgData, ptr++);
                    sbyte green = (sbyte)Buffer.GetByte(imgData, ptr++);
                    byte blue = 0xFF;

                    int fCol = blue | (0x7F + green) << 8 | (0x7F + red) << 16 | 0xFF << 24;
                    bitmapBW.Write(fCol);
                }
            }

            byte[] imageData = bitmapStream.ToArray();
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0,
                                                    bmp.Width,
                                                    bmp.Height),
                                      ImageLockMode.WriteOnly,
                                      bmp.PixelFormat);

                Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }
        #endregion
        #region ATI2
        private static Bitmap UncompressATI2(byte[] imgData, int w, int h)
        {
            const int bufferSize = 16;
            const int bytesPerPixel = 3;
            byte[] blockStorage = new byte[bufferSize];
            MemoryStream bitmapStream = new MemoryStream(w * h * 2);

            int ptr = 0;
            for (int s = 0; s < h; s += 4)
            {
                for (int t = 0; t < w; t += 4)
                {
                    Buffer.BlockCopy(imgData, ptr, blockStorage, 0, bufferSize);
                    ptr += bufferSize;
                    #region Block Decompression Loop
                    byte[][] rgbVals = new byte[3][];
                    byte[] blueVals = new byte[bufferSize];
                    for (int j = 1; j >= 0; j--)
                    {
                        byte colour0 = blockStorage[j * 8]; // First 2 bytes are the min and max vals to be interpolated between
                        byte colour1 = blockStorage[1 + (j * 8)];
                        ulong longRep = BitConverter.ToUInt64(blockStorage, j * 8);
                        byte[] colVals = new byte[bufferSize];

                        //for (int k = 0; k < bufferSize; k++)
                        for (int k = bufferSize - 1; k >= 0; k--)
                        {
                            ulong tempLong = longRep | (ulong)ATI2BitCodes.interpColor5; // Set all trailing bits to 1

                            if ((tempLong ^ (ulong)ATI2BitCodes.color0) == (ulong)ATI2BitCodes.result) // First 2 values mean to use the specified min or max values
                            {
                                colVals[k] = colour0;
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.color1) == (ulong)ATI2BitCodes.result)
                            {
                                colVals[k] = colour1;
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.interpColor0) == (ulong)ATI2BitCodes.result) // Remaining values interpolate the min/max
                            {
                                if (colour0 > colour1)
                                    colVals[k] = (byte)((6 * colour0 + colour1) / 7);
                                else
                                    colVals[k] = (byte)((4 * colour0 + colour1) / 5);
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.interpColor1) == (ulong)ATI2BitCodes.result)
                            {
                                if (colour0 > colour1)
                                    colVals[k] = (byte)((5 * colour0 + 2 * colour1) / 7);
                                else
                                    colVals[k] = (byte)((3 * colour0 + 2 * colour1) / 5);
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.interpColor2) == (ulong)ATI2BitCodes.result)
                            {
                                if (colour0 > colour1)
                                    colVals[k] = (byte)((4 * colour0 + 3 * colour1) / 7);
                                else
                                    colVals[k] = (byte)((2 * colour0 + 3 * colour1) / 5);
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.interpColor3) == (ulong)ATI2BitCodes.result)
                            {
                                if (colour0 > colour1)
                                    colVals[k] = (byte)((3 * colour0 + 4 * colour1) / 7);
                                else
                                    colVals[k] = (byte)((colour0 + 4 * colour1) / 5);
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.interpColor4) == (ulong)ATI2BitCodes.result)
                            {
                                if (colour0 > colour1)
                                    colVals[k] = (byte)((2 * colour0 + 5 * colour1) / 7);
                                else
                                    colVals[k] = (byte)0;
                            }
                            else if ((tempLong ^ (ulong)ATI2BitCodes.interpColor5) == (ulong)ATI2BitCodes.result)
                            {
                                if (colour0 > colour1)
                                    colVals[k] = (byte)((colour0 + 6 * colour1) / 7);
                                else
                                    colVals[k] = (byte)255;
                            }
                            else
                            {
                                //MessageBox.Show("Error. Bitwise value not found."); // Safety catch. Shouldn't ever be encountered
                                throw new FormatException("Unknown bit value found. This shouldn't be possible...");
                            }
                            longRep <<= 3;
                        }
                        int index = (j == 0) ? 0 : 1;
                        rgbVals[index] = colVals;
                    }
                    for (int j = 0; j < bufferSize; j++)
                    {
                        if (rgbVals[0][j] <= 20 && rgbVals[1][j] <= 20)
                            blueVals[j] = 128;
                        else
                            blueVals[j] = 255;
                    }
                    rgbVals[2] = blueVals;
                    #endregion

                    for (int i = 0; i < 4; i++)
                    {
                        bitmapStream.Seek(((s + i) * w * bytesPerPixel) + (t * bytesPerPixel), SeekOrigin.Begin);
                        for (int j = 0; j < 4; j++)
                        {
                            //for (int k = 0; k < 3; k++)
                            for (int k = 2; k >= 0; k--)
                                bitmapStream.WriteByte(rgbVals[k][(i * 4) + j]);
                        }
                    }
                }
            }

            byte[] imageData = bitmapStream.ToArray();
            if (imageData.Length != (w * h * bytesPerPixel))
                throw new FormatException("Incorect length of generated data array");
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(imageData, 0, bmpData.Scan0, imageData.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
        #endregion
        #region A8R8G8B8
        public static Bitmap View32Bit(byte[] imgData, int w, int h)
        {
            if (imgData.Length != (w * h * 4))
                throw new ArgumentException("Input array is not correct size");
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(imgData, 0, bmpData.Scan0, imgData.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        #endregion
        #region R8G8B8
        public static Bitmap View24Bit(byte[] imgData, int w, int h)
        {
            if (imgData.Length != (w * h * 3))
                throw new ArgumentException("Input array is not correct size");
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(imgData, 0, bmpData.Scan0, imgData.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        #endregion
        #region G8
        public static Bitmap ViewG8(byte[] imgData, int w, int h)
        {
            if (imgData.Length != (w * h))
                throw new ArgumentException("Input array is not correct size");
            byte[] buff = new byte[w * h * 3];
            for (int i = 0; i < (w * h); i++)
            {
                for (int j = 0; j < 3; j++)
                    buff[(3 * i) + j] = imgData[i];
            }
            
            var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(buff, 0, bmpData.Scan0, buff.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        #endregion

        private void Read_DDS_HEADER(DDS_HEADER h, BinaryReader r)
        {
            h.dwSize = r.ReadInt32();
            h.dwFlags = r.ReadInt32();
            h.dwHeight = r.ReadInt32();
            h.dwWidth = r.ReadInt32();
            h.dwPitchOrLinearSize = r.ReadInt32();
            h.dwDepth = r.ReadInt32();
            h.dwMipMapCount = r.ReadInt32();
            for (int i = 0; i < 11; ++i)
            {
                h.dwReserved1[i] = r.ReadInt32();
            }
            Read_DDS_PIXELFORMAT(h.ddspf, r);
            h.dwCaps = r.ReadInt32();
            h.dwCaps2 = r.ReadInt32();
            h.dwCaps3 = r.ReadInt32();
            h.dwCaps4 = r.ReadInt32();
            h.dwReserved2 = r.ReadInt32();
        }

        private void Read_DDS_PIXELFORMAT(DDS_PIXELFORMAT p, BinaryReader r)
        {
            p.dwSize = r.ReadUInt32();
            p.dwFlags = r.ReadUInt32();
            p.dwFourCC = r.ReadUInt32();
            p.dwRGBBitCount = r.ReadUInt32();
            p.dwRBitMask = r.ReadUInt32();
            p.dwGBitMask = r.ReadUInt32();
            p.dwBBitMask = r.ReadUInt32();
            p.dwABitMask = r.ReadUInt32();
        }
    }

    public class DDS_HEADER
    {
        public int dwSize;
        public int dwFlags;
        public int dwHeight;
        public int dwWidth;
        public int dwPitchOrLinearSize;
        public int dwDepth;
        public int dwMipMapCount;
        public int[] dwReserved1 = new int[11];
        public DDS_PIXELFORMAT ddspf = new DDS_PIXELFORMAT();
        public int dwCaps;
        public int dwCaps2;
        public int dwCaps3;
        public int dwCaps4;
        public int dwReserved2;
    }

    public class DDS_PIXELFORMAT
    {
        public uint dwSize;
        public uint dwFlags;
        public uint dwFourCC;
        public uint dwRGBBitCount;
        public uint dwRBitMask;
        public uint dwGBitMask;
        public uint dwBBitMask;
        public uint dwABitMask;
    }
}
