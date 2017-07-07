/*  Copyright (C) 2013 AmaroK86 (marcidm 'at' hotmail 'dot' com)
 *  Copyright (C) 2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
 *
 * contact: kons.snok<at>gmail.com
 */

using System;
using System.IO;

namespace AmaroK86.ImageFormat
{
    public class DDSImage
    {
        #region DXT1
        public static byte[] UncompressDXT1(byte[] imgData, int w, int h, bool stripAlpha = false)
        {
            const int bufferSize = 8;
            byte[] blockStorage = new byte[bufferSize];
            using (MemoryStream bitmapStream = new MemoryStream(w * h * 2))
            {
                using (BinaryWriter bitmapBW = new BinaryWriter(bitmapStream))
                {
                    int readPtr = 0;
                    for (int s = 0; s < h; s += 4)
                    {
                        for (int t = 0; t < w; t += 4)
                        {
                            Buffer.BlockCopy(imgData, readPtr, blockStorage, 0, bufferSize);
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
                                                    fCol = (stripAlpha ? 0xFF : 0x00) << 24;
                                                    break;
                                            }
                                        }

                                        bitmapBW.Write(fCol);
                                    }
                                }
                            }
                        }
                    }
                }

                return bitmapStream.ToArray();
            }
        }
        #endregion
        #region DXT3
        public static byte[] UncompressDXT3(byte[] imgData, int w, int h, bool stripAlpha = false)
        {
            const int bufferSize = 16;
            byte[] blockStorage = new byte[bufferSize];
            using (MemoryStream bitmapStream = new MemoryStream(w * h * 2))
            {
                using (BinaryWriter bitmapBW = new BinaryWriter(bitmapStream))
                {
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
                                        byte alpha = (byte)((blockStorage[(j * i) < 8 ? 0 : 1] >> (((i * j) % 8) * 4)) & 0xFF);
                                        alpha = (byte)((alpha << 4) | alpha);
                                        if (stripAlpha)
                                            alpha = 0xFF;

                                        int fCol = 0;
                                        int colorCode = (code >> 2 * (4 * j + i)) & 0x03;

                                        switch (colorCode)
                                        {
                                            case 0:
                                                fCol = b0 | g0 << 8 | r0 << 16 | 0xFF << alpha;
                                                break;
                                            case 1:
                                                fCol = b1 | g1 << 8 | r1 << 16 | 0xFF << alpha;
                                                break;
                                            case 2:
                                                fCol = (2 * b0 + b1) / 3 | (2 * g0 + g1) / 3 << 8 | (2 * r0 + r1) / 3 << 16 | 0xFF << alpha;
                                                break;
                                            case 3:
                                                fCol = (b0 + 2 * b1) / 3 | (g0 + 2 * g1) / 3 << 8 | (r0 + 2 * r1) / 3 << 16 | 0xFF << alpha;
                                                break;
                                        }

                                        bitmapBW.Write(fCol);
                                    }
                                }
                            }
                        }
                    }

                    return bitmapStream.ToArray();
                }
            }
        }
        #endregion
        #region DXT5
        public static byte[] UncompressDXT5(byte[] imgData, int w, int h, bool stripAlpha = false)
        {
            const int bufferSize = 16;
            byte[] blockStorage = new byte[bufferSize];
            using (MemoryStream bitmapStream = new MemoryStream(w * h * 2))
            {
                using (BinaryWriter bitmapBW = new BinaryWriter(bitmapStream))
                {
                    int ptr = 0;
                    for (int s = 0; s < h; s += 4)
                    {
                        for (int t = 0; t < w; t += 4)
                        {
                            Buffer.BlockCopy(imgData, ptr, blockStorage, 0, bufferSize);
                            ptr += bufferSize;
                            {
                                byte alpha0 = blockStorage[0];
                                byte alpha1 = blockStorage[1];

                                uint alphaCode1 = (uint)(blockStorage[4] | (blockStorage[5] << 8) | (blockStorage[6] << 16) | (blockStorage[7] << 24));
                                ushort alphaCode2 = (ushort)(blockStorage[2] | (blockStorage[3] << 8));

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
                                        int alphaCodeIndex = 3 * (4 * j + i);
                                        int alphaCode;

                                        if (alphaCodeIndex <= 12)
                                        {
                                            alphaCode = (alphaCode2 >> alphaCodeIndex) & 0x07;
                                        }
                                        else if (alphaCodeIndex == 15)
                                        {
                                            alphaCode = (int)((uint)(alphaCode2 >> 15) | ((alphaCode1 << 1) & 0x06));
                                        }
                                        else
                                        {
                                            alphaCode = (int)((alphaCode1 >> (alphaCodeIndex - 16)) & 0x07);
                                        }

                                        byte alpha;
                                        if (alphaCode == 0)
                                        {
                                            alpha = alpha0;
                                        }
                                        else if (alphaCode == 1)
                                        {
                                            alpha = alpha1;
                                        }
                                        else
                                        {
                                            if (alpha0 > alpha1)
                                            {
                                                alpha = (byte)(((8 - alphaCode) * alpha0 + (alphaCode - 1) * alpha1) / 7);
                                            }
                                            else
                                            {
                                                if (alphaCode == 6)
                                                    alpha = 0;
                                                else if (alphaCode == 7)
                                                    alpha = 255;
                                                else
                                                    alpha = (byte)(((6 - alphaCode) * alpha0 + (alphaCode - 1) * alpha1) / 5);
                                            }
                                        }

                                        if (stripAlpha)
                                            alpha = 0xFF;

                                        int fCol = 0;
                                        int colorCode = (code >> 2 * (4 * j + i)) & 0x03;

                                        switch (colorCode)
                                        {
                                            case 0:
                                                fCol = b0 | g0 << 8 | r0 << 16 | alpha << 24;
                                                break;
                                            case 1:
                                                fCol = b1 | g1 << 8 | r1 << 16 | alpha << 24;
                                                break;
                                            case 2:
                                                fCol = (2 * b0 + b1) / 3 | (2 * g0 + g1) / 3 << 8 | (2 * r0 + r1) / 3 << 16 | alpha << 24;
                                                break;
                                            case 3:
                                                fCol = (b0 + 2 * b1) / 3 | (g0 + 2 * g1) / 3 << 8 | (r0 + 2 * r1) / 3 << 16 | alpha << 24;
                                                break;
                                        }

                                        bitmapBW.Write(fCol);
                                    }
                                }
                            }
                        }
                    }

                    return bitmapStream.ToArray();
                }
            }
        }
        #endregion
        #region ATI2
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

        public static byte[] UncompressATI2(byte[] imgData, int w, int h)
        {
            const int bufferSize = 16;
            const int bytesPerPixel = 4;
            byte[] blockStorage = new byte[bufferSize];
            using (MemoryStream bitmapStream = new MemoryStream(w * h * 2))
            {
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
                                bitmapStream.WriteByte(rgbVals[2][(i * 4) + j]);
                                bitmapStream.WriteByte(rgbVals[0][(i * 4) + j]);
                                bitmapStream.WriteByte(rgbVals[1][(i * 4) + j]);
                                bitmapStream.WriteByte(255);
                            }
                        }
                    }
                }

                return bitmapStream.ToArray();
            }
        }
        #endregion
    }
}
