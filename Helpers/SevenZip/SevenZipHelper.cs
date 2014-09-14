/*
 * SevenZip Helper
 *
 * Copyright (C) 2014 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenZipHelper
{
    public static class LZMA
    {
        public static byte[] Decompress(byte[] src, int dstSize)
        {
            if (src.Length < 5)
                throw new Exception("LZMA data is too short");

            MemoryStream input = new MemoryStream(src);
            MemoryStream output = new MemoryStream();

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            decoder.SetDecoderProperties(src);
            decoder.Code(input, output, input.Length - 5, dstSize, null);

            return output.ToArray();
        }

        public static byte[] Compress(byte[] src)
        {
            SevenZip.CoderPropID[] propIDs = 
			{
				SevenZip.CoderPropID.DictionarySize,
				SevenZip.CoderPropID.PosStateBits,
				SevenZip.CoderPropID.LitContextBits,
				SevenZip.CoderPropID.LitPosBits,
				SevenZip.CoderPropID.Algorithm,
				SevenZip.CoderPropID.NumFastBytes,
				SevenZip.CoderPropID.MatchFinder,
				SevenZip.CoderPropID.EndMarker
            };

            object[] properties = 
			{
				(Int32)(1 << 16), // DictionarySize
				(Int32)(2), // PosStateBits
				(Int32)(3), // LitContextBits
				(Int32)(0), // LitPosBits
				(Int32)(2), // Algorithm
				(Int32)(128), // NumFastBytes
				"bt4", // MatchFinder
				false // EndMarker
			};

            MemoryStream input = new MemoryStream(src);
            MemoryStream output = new MemoryStream();

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(output);
            encoder.Code(input, output, -1, -1, null);

            return output.ToArray();
        }
    }
}
