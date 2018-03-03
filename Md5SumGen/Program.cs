/*
 * MassEffectModder
 *
 * Copyright (C) 2017-2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Collections.Generic;
using StreamHelpers;
using ZlibHelper;

namespace Md5SumGen
{
    struct MD5FileEntry
    {
        public string path;
        public byte[] md5;
        public int size;
    }

    class Tables
    {
        public const uint md5Tag = 0x5435444D;

        public void generateBinFile(int gameId)
        {
            MD5FileEntry[] entries = null;

            switch (gameId)
            {
                case 1:
                    MD5TablesME1 tablesME1 = new MD5TablesME1();
                    entries = tablesME1.entriesME1;
                    break;
                case 2:
                    MD5TablesME2 tablesME2 = new MD5TablesME2();
                    entries = tablesME2.entriesME2;
                    break;
                case 3:
                    MD5TablesME3 tablesME3 = new MD5TablesME3();
                    entries = tablesME3.entriesME3;
                    break;
            }

            MemoryStream stream = new MemoryStream();

            List<string> files = new List<string>();
            for (int p = 0; p < entries.Length; p++)
            {
                if (!files.Exists(s => s == entries[p].path))
                    files.Add(entries[p].path);
            }

            stream.WriteInt32(files.Count);
            for (int p = 0; p < files.Count; p++)
            {
                stream.WriteStringASCIINull(files[p]);
            }

            stream.WriteInt32(entries.Length);
            for (int p = 0; p < entries.Length; p++)
            {
                stream.WriteInt32(files.IndexOf(entries[p].path));
                stream.WriteInt32(entries[p].size);
                stream.WriteFromBuffer(entries[p].md5);
            }
            using (FileStream fs = new FileStream("MD5EntriesME" + gameId + ".bin", FileMode.Create, FileAccess.Write))
            {
                fs.WriteUInt32(md5Tag);
                byte[] tmp = stream.ToArray();
                fs.WriteInt32(tmp.Length);
                fs.WriteFromBuffer(Zlib.Compress(tmp, 9));
            }
        }
    }

    static class Program
    {
        static void Main(string[] args)
        {
            new Tables().generateBinFile(1);
            new Tables().generateBinFile(2);
            new Tables().generateBinFile(3);
        }
    }
}
