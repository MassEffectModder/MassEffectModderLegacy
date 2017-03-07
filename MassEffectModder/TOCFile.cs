/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using StreamHelpers;
using System.Linq;

namespace MassEffectModder
{
    public class TOCBinFile
    {
        const uint TOCTag = 0x3AB70C13; // TOC tag

        struct FileEntry
        {
            public uint size;
            public string path;
        }

        static public void UpdateAllTOCBinFiles()
        {
            GenerateMainTocBinFile();
            GenerateDLCsTocBinFiles();
        }

        static private void GenerateMainTocBinFile()
        {
            List<string> files = Directory.GetFiles(GameData.MainData, "*.*",
            SearchOption.AllDirectories).Where(s => s.EndsWith(".pcc",
                StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".upk", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".tlk", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".afc", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".cnd", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)).ToList();
            files.AddRange(Directory.GetFiles(Path.Combine(GameData.bioGamePath, "Movies"), "*.*",
            SearchOption.AllDirectories).Where(s => s.EndsWith(".bik",
                StringComparison.OrdinalIgnoreCase)));

            string tocFile = Path.Combine(GameData.bioGamePath, "PCConsoleTOC.bin");

            List<FileEntry> filesList = new List<FileEntry>();
            for (int f = 0; f < files.Count; f++)
            {
                FileEntry file = new FileEntry();
                if (files[f].ToLower() != "pcconsoletoc.bin")
                    file.size = (uint)new FileInfo(files[f]).Length;
                file.path = files[f].Substring(GameData.GamePath.Length + 1);
                filesList.Add(file);
            }
            CreateTocBinFile(tocFile, filesList);
        }

        static private void GenerateDLCsTocBinFiles()
        {
            if (Directory.Exists(GameData.DLCData))
            {
                List<string> DLCs = Directory.GetDirectories(GameData.DLCData).ToList();
                for (int i = 0; i < DLCs.Count; i++)
                {
                    List<string> dlcs = Directory.GetFiles(DLCs[i], "Mount.dlc", SearchOption.AllDirectories).ToList();
                    if (dlcs.Count == 0)
                        DLCs.RemoveAt(i--);
                }

                for (int i = 0; i < DLCs.Count; i++)
                {
                    List<string> files = Directory.GetFiles(DLCs[i], "*.*",
                        SearchOption.AllDirectories).Where(s => s.EndsWith(".pcc",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".upk", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".tlk", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".afc", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".cnd", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".bik", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)).ToList();

                    string DLCname = Path.GetFileName(DLCs[i]);
                    string tocFile = Path.Combine(GameData.DLCData, DLCname, "PCConsoleTOC.bin");

                    List<FileEntry> filesList = new List<FileEntry>();
                    for (int f = 0; f < files.Count; f++)
                    {
                        FileEntry file = new FileEntry();
                        if (files[f].ToLower() != "pcconsoletoc.bin")
                            file.size = (uint)new FileInfo(files[f]).Length;
                        file.path = files[f].Substring((GameData.DLCData + "\\" + DLCname).Length + 1);
                        filesList.Add(file);
                    }
                    CreateTocBinFile(tocFile, filesList);
                }
            }
        }

        static private void CreateTocBinFile(string path, List<FileEntry> filesList)
        {
            if (File.Exists(path))
                File.Delete(path);
            using (FileStream tocFile = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                tocFile.WriteUInt32(TOCTag);
                tocFile.WriteUInt32(0);
                tocFile.WriteUInt32(1);
                tocFile.WriteUInt32(8);
                tocFile.WriteInt32(filesList.Count);

                long lastOffset = 0;
                for (int f = 0; f < filesList.Count; f++)
                {
                    long fileOffset = lastOffset = tocFile.Position;
                    int blockSize = ((28 + (filesList[f].path.Length + 1) + 3) / 4) * 4; // align to 4
                    tocFile.WriteUInt16((ushort)blockSize);
                    tocFile.WriteUInt16(0);
                    tocFile.WriteUInt32(filesList[f].size);
                    tocFile.WriteZeros(20);
                    tocFile.WriteStringASCIINull(filesList[f].path);
                    tocFile.JumpTo(fileOffset + blockSize - 1);
                    tocFile.WriteByte(0); // make sure all bytes are written after seek
                }
                if (lastOffset != 0)
                {
                    tocFile.JumpTo(lastOffset);
                    tocFile.WriteUInt16(0);
                }
            }
        }
    }
}
