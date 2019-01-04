/*
 * C# xdelta 3 Helper for wrapper
 *
 * Copyright (C) 2018-2019 Pawel Kolodziejski
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Xdelta3Helper
{
    public class Xdelta3
    {
        [DllImport("xdelta.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int XDelta3Decompress([In] byte[] srcBuf, uint srcLen, [In] byte[] delta, uint deltaLen,
            [Out] byte[] dstBuf, ref uint dstLen);

        [DllImport("xdelta.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int XDelta3Compress([In] byte[] src1Buf, [In] byte[] src2Buf, uint srcLen,
            [Out] byte[] delta, ref uint deltaLen);

        public byte[] Decompress(byte[] src, byte[] delta)
        {
            byte[] tmpbuf = new byte[src.Length];
            uint dstLen = 0;

            int status = XDelta3Decompress(src, (uint)src.Length, delta, (uint)delta.Length, tmpbuf, ref dstLen);
            if (status != 0)
                return new byte[0];

            byte[] dst = new byte[dstLen];
            Array.Copy(tmpbuf, dst, (int)dstLen);

            return dst;
        }

        public byte[] Compress(byte[] src1, byte[] src2)
        {
            byte[] tmpbuf = new byte[src1.Length];
            uint deltaLen = 0;

            int status = XDelta3Compress(src1, src2, (uint)src1.Length, tmpbuf, ref deltaLen);
            if (status != 0)
                return new byte[0];

            byte[] dst = new byte[deltaLen];
            Array.Copy(tmpbuf, dst, (int)deltaLen);

            return dst;
        }
    }
}
