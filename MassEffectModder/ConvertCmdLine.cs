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

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MassEffectModder
{
    static public class CmdLineConverter
    {
        static public List<FoundTexture> loadTexturesMap(MeType gameId)
        {
            Stream fs;
            List<FoundTexture> textures = new List<FoundTexture>();
            byte[] buffer = null;

            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            for (int l = 0; l < resources.Length; l++)
            {
                if (resources[l].Contains("me" + (int)gameId + "map.bin"))
                {
                    using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                    {
                        buffer = s.ReadToBuffer(s.Length);
                        break;
                    }
                }
            }
            if (buffer == null)
                throw new Exception();
            MemoryStream tmp = new MemoryStream(buffer);
            if (tmp.ReadUInt32() != 0x504D5443)
                throw new Exception();
            byte[] decompressed = new byte[tmp.ReadUInt32()];
            byte[] compressed = tmp.ReadToBuffer(tmp.ReadUInt32());
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            fs = new MemoryStream(decompressed);

            fs.Skip(8);
            uint countTexture = fs.ReadUInt32();
            for (int i = 0; i < countTexture; i++)
            {
                FoundTexture texture = new FoundTexture();
                int len = fs.ReadInt32();
                texture.name = fs.ReadStringASCII(len);
                texture.crc = fs.ReadUInt32();
                texture.width = fs.ReadInt32();
                texture.height = fs.ReadInt32();
                texture.pixfmt = (PixelFormat)fs.ReadInt32();
                texture.alphadxt1 = fs.ReadInt32() != 0;
                texture.numMips = fs.ReadInt32();
                uint countPackages = fs.ReadUInt32();
                texture.list = new List<MatchedTexture>();
                for (int k = 0; k < countPackages; k++)
                {
                    MatchedTexture matched = new MatchedTexture();
                    matched.exportID = fs.ReadInt32();
                    matched.linkToMaster = fs.ReadInt32();
                    len = fs.ReadInt32();
                    matched.path = fs.ReadStringASCII(len);
                    texture.list.Add(matched);
                }
                textures.Add(texture);
            }

            return textures;
        }

        static public bool ConvertToMEM(MeType gameId, string inputDir, string memFile)
        {
            string errors = "";
            bool status = Misc.convertDataModtoMem(inputDir, memFile, gameId, null, null, ref errors, false);
            if (errors != "")
                Console.WriteLine("Error: Some errors have occured");

            return status;
        }

        static public bool extractTPF(string inputDir, string outputDir)
        {
            Console.WriteLine("Extract TPF files started...");

            bool status = true;
            string[] files = null;
            int result;
            string fileName = "";
            ulong dstLen = 0;
            ulong numEntries = 0;
            List<string> list = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
            list.Sort();
            files = list.ToArray();

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            foreach (string file in files)
            {
                Console.WriteLine("Extract TPF: " + file);
                string outputTPFdir = outputDir + "\\" + Path.GetFileNameWithoutExtension(file);
                if (!Directory.Exists(outputTPFdir))
                    Directory.CreateDirectory(outputTPFdir);

                IntPtr handle = IntPtr.Zero;
                ZlibHelper.Zip zip = new ZlibHelper.Zip();
                try
                {
                    byte[] buffer = File.ReadAllBytes(file);
                    handle = zip.Open(buffer, ref numEntries, 1);
                    if (handle == IntPtr.Zero)
                        throw new Exception();

                    for (uint i = 0; i < numEntries; i++)
                    {
                        try
                        {
                            result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            string filename = Path.GetFileName(fileName).Trim();
                            if (Path.GetExtension(filename).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(filename).ToLowerInvariant() == ".log")
                            {
                                zip.GoToNextFile(handle);
                                continue;
                            }

                            byte[] data = new byte[dstLen];
                            result = zip.ReadCurrentFile(handle, data, dstLen);
                            if (result != 0)
                            {
                                throw new Exception();
                            }
                            using (FileStream fs = new FileStream(Path.Combine(outputTPFdir, filename), FileMode.CreateNew))
                            {
                                fs.WriteFromBuffer(data);
                            }
                            Console.WriteLine(Path.Combine(outputTPFdir, filename));
                        }
                        catch
                        {
                            Console.WriteLine("Skipping damaged content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file);
                            status = false;
                        }
                        zip.GoToNextFile(handle);
                    }
                    zip.Close(handle);
                    handle = IntPtr.Zero;
                }
                catch
                {
                    Console.WriteLine("TPF file is damaged: " + file);
                    if (handle != IntPtr.Zero)
                        zip.Close(handle);
                    handle = IntPtr.Zero;
                    continue;
                }
            }

            Console.WriteLine("Extract TPF files completed.");
            return status;
        }

    }
}
