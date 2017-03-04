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
        const int TOCHeaderSize = 12;

        struct FileEntry
        {
            public ushort type;
            public uint size;
            public byte[] sha1;
            public string path;
        }

        struct Block
        {
            public uint filesOffset;
            public uint numFiles;
            public List<FileEntry> filesList;
        }

        List<Block> blockList;

        public TOCBinFile(string filename)
        {
            using (FileStream tocFile = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                upload(tocFile);
            }
        }

        public TOCBinFile(byte[] content)
        {
            upload(new MemoryStream(content));
        }

        private void upload(Stream tocFile)
        {
            uint tag = tocFile.ReadUInt32();
            if (tag != TOCTag)
                throw new Exception("Wrong TOCTag tag");
            tocFile.SkipInt32();

            blockList = new List<Block>();
            uint numBlocks = tocFile.ReadUInt32();
            for (int b = 0; b < numBlocks; b++)
            {
                Block block = new Block();
                block.filesOffset = tocFile.ReadUInt32();
                block.numFiles = tocFile.ReadUInt32();
                block.filesList = new List<FileEntry>();
                blockList.Add(block);
            }

            tocFile.JumpTo(TOCHeaderSize + (numBlocks * 8));
            for (int b = 0; b < numBlocks; b++)
            {
                Block block = blockList[b];
                FileEntry file = new FileEntry();
                for (int f = 0; f < block.numFiles; f++)
                {
                    long curPos = tocFile.Position;
                    ushort blockSize = tocFile.ReadUInt16();
                    file.type = tocFile.ReadUInt16();
                    if (file.type != 9 && file.type != 1 && file.type != 0)
                        throw new Exception();
                    file.size = tocFile.ReadUInt32();
                    file.sha1 = tocFile.ReadToBuffer(20);
                    file.path = tocFile.ReadStringASCIINull();
                    block.filesList.Add(file);
                    tocFile.JumpTo(curPos + blockSize);
                }
                blockList[b] = block;
            }
        }

        public void updateFile(string filename, string filePath, bool updateSHA1 = false)
        {
            string searchFile = filename.ToLower();
            for (int b = 0; b < blockList.Count; b++)
            {
                for (int f = 0; f < blockList[b].numFiles; f++)
                {
                    FileEntry file = blockList[b].filesList[f];
                    if (file.path.ToLower() == searchFile)
                    {
                        file.size = (uint)new FileInfo(filePath).Length;
                        if (updateSHA1)
                            file.sha1 = Misc.calculateSHA1(filePath);
                        blockList[b].filesList[f] = file;
                        return;
                    }
                }
            }
            Block block = blockList[blockList.Count - 1];
            FileEntry e = new FileEntry();
            e.path = filename;
            e.size = (uint)new FileInfo(filePath).Length;
            e.type = 1;
            if (updateSHA1)
                e.sha1 = Misc.calculateSHA1(filePath);
            else
                e.sha1 = new byte[20];
            block.filesList.Add(e);
            block.numFiles++;
            blockList[blockList.Count - 1] = block;
        }

        public byte[] saveToBuffer()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                saveToStream(stream);
                return stream.ToArray();
            }
        }

        public void saveToFile(string outPath)
        {
            if (File.Exists(outPath))
                File.Delete(outPath);
            using (FileStream stream = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                saveToStream(stream);
            }
        }

        public void saveToStream(Stream tocFile, bool updateOffsets = false)
        {
            tocFile.WriteUInt32(TOCTag);
            tocFile.WriteUInt32(0);
            tocFile.WriteUInt32((uint)blockList.Count);
            tocFile.Skip(8 * blockList.Count); // filled later

            long lastOffset = 0;
            for (int b = 0; b < blockList.Count; b++)
            {
                Block block = blockList[b];
                if (updateOffsets)
                    block.filesOffset = (uint)(tocFile.Position - TOCHeaderSize - (8 * b));
                for (int f = 0; f < blockList[b].numFiles; f++)
                {
                    long fileOffset = lastOffset = tocFile.Position;
                    FileEntry file = blockList[b].filesList[f];
                    int blockSize = ((28 + (file.path.Length + 1) + 3) / 4) * 4; // align to 4
                    tocFile.WriteUInt16((ushort)blockSize);
                    tocFile.WriteUInt16(file.type);
                    tocFile.WriteUInt32(file.size);
                    tocFile.WriteFromBuffer(file.sha1);
                    tocFile.WriteStringASCIINull(file.path);
                    tocFile.JumpTo(fileOffset + blockSize - 1);
                    tocFile.WriteByte(0); // make sure all bytes are written after seek
                }
                blockList[b] = block;
            }
            if (lastOffset != 0)
            {
                tocFile.JumpTo(lastOffset);
                tocFile.WriteUInt16(0);
            }

            tocFile.JumpTo(TOCHeaderSize);
            for (int b = 0; b < blockList.Count; b++)
            {
                tocFile.WriteUInt32(blockList[b].filesOffset);
                tocFile.WriteUInt32(blockList[b].numFiles);
            }
        }
    }
}
