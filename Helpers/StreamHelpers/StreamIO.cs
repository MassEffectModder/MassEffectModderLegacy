/*
 * C# Stream Helpers
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

using System;
using System.IO;
using System.Text;

namespace StreamHelpers
{
    public static class StreamHelpers
    {
        public static byte[] ReadToBuffer(this Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            if (stream.Read(buffer, 0, count) != count)
                throw new Exception();
            return buffer;
        }

        public static byte[] ReadToBuffer(this Stream stream, uint count)
        {
            return stream.ReadToBuffer((int)count);
        }

        public static byte[] ReadToBuffer(this Stream stream, long count)
        {
            return stream.ReadToBuffer((int)count);
        }

        public static void WriteFromBuffer(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteFromStream(this Stream stream, Stream inputStream, int count)
        {
            var buffer = ReadToBuffer(inputStream, count);
            stream.Write(buffer, 0, buffer.Length);
        }

        public static void WriteFromStream(this Stream stream, Stream inputStream, uint count)
        {
            WriteFromStream(stream, inputStream, (int)count);
        }

        public static void WriteFromStream(this Stream stream, Stream inputStream, long count)
        {
            WriteFromStream(stream, inputStream, (int)count);
        }

        public static string ReadStringASCII(this Stream stream, int count)
        {
            var buffer = stream.ReadToBuffer(count);
            return Encoding.ASCII.GetString(buffer);
        }

        public static string ReadStringASCIINull(this Stream stream)
        {
            string str = "";
            for (;;)
            {
                char c = (char)stream.ReadByte();
                if (c == 0)
                    break;
                str += c;
            }
            return str;
        }

        public static string ReadStringUnicode(this Stream stream, int count)
        {
            var buffer = stream.ReadToBuffer(count);
            return Encoding.ASCII.GetString(buffer);
        }

        public static string ReadStringUnicodeNull(this Stream stream, int count)
        {
            return stream.ReadStringASCII(count).Trim('\0');
        }

        public static void WriteStringASCII(this Stream stream, string str)
        {
            stream.Write(Encoding.ASCII.GetBytes(str), 0, Encoding.ASCII.GetByteCount(str));
        }

        public static void WriteStringASCIINull(this Stream stream, string str)
        {
            stream.WriteStringASCII(str + "\0");
        }

        public static void WriteStringUnicode(this Stream stream, string str)
        {
            stream.Write(Encoding.Unicode.GetBytes(str), 0, Encoding.Unicode.GetByteCount(str));
        }

        public static void WriteStringUnicodeNull(this Stream stream, string str)
        {
            stream.WriteStringUnicode(str + "\0");
        }

        public static UInt64 ReadUInt64(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(UInt64)];
            if (stream.Read(buffer, 0, sizeof(UInt64)) != sizeof(UInt64))
                throw new Exception();
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static Int64 ReadInt64(this Stream stream)
        {
            return (Int64)stream.ReadInt64();
        }

        public static void WriteUInt64(this Stream stream, UInt64 data)
        {
            stream.Write(BitConverter.GetBytes(data), 0, sizeof(UInt64));
        }

        public static void WriteInt64(this Stream stream, Int64 data)
        {
            stream.Write(BitConverter.GetBytes(data), 0, sizeof(Int64));
        }

        public static UInt32 ReadUInt32(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(UInt32)];
            if (stream.Read(buffer, 0, sizeof(UInt32)) != sizeof(UInt32))
                throw new Exception();
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static Int32 ReadInt32(this Stream stream)
        {
            return (Int32)stream.ReadInt32();
        }

        public static void WriteUInt32(this Stream stream, UInt32 data)
        {
            stream.Write(BitConverter.GetBytes(data), 0, sizeof(UInt32));
        }

        public static void WriteInt32(this Stream stream, Int32 data)
        {
            stream.Write(BitConverter.GetBytes(data), 0, sizeof(Int32));
        }

        public static UInt16 ReadUInt16(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(UInt16)];
            if (stream.Read(buffer, 0, sizeof(UInt16)) != sizeof(UInt16))
                throw new Exception();
            return BitConverter.ToUInt16(buffer, 0);
        }

        public static Int16 ReadInt16(this Stream stream)
        {
            return (Int16)stream.ReadInt16();
        }

        public static void WriteUInt16(this Stream stream, UInt16 data)
        {
            stream.Write(BitConverter.GetBytes(data), 0, sizeof(UInt16));
        }

        public static void WriteInt16(this Stream stream, Int16 data)
        {
            stream.Write(BitConverter.GetBytes(data), 0, sizeof(Int16));
        }
    }
}
