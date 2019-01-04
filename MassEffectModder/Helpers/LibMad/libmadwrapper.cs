/*
 * LibMad Helper
 *
 * Copyright (C) 2018-2019 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Runtime.InteropServices;

namespace LibMadHelper
{
    static public class LibMad
    {
        [DllImport("libmadwrapper.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int LibMadDecompress([In] byte[] srcBuf, uint srcLen, ref IntPtr dstBuf, ref uint dstLen);

        [DllImport("libmadwrapper.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern void LibMadFreeBuffer(IntPtr buffer);


        static public byte[] Decompress(byte[] src)
        {
            uint len = 0;
            IntPtr buffer = IntPtr.Zero;

            int status = LibMadDecompress(src, (uint)src.Length, ref buffer, ref len);
            if (status != 0)
                return new byte[0];

            byte[] dst = new byte[len];
            Marshal.Copy(buffer, dst, 0, (int)len);
            LibMadFreeBuffer(buffer);

            return dst;
        }
    }
}
