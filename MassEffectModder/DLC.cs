/*
 * MassEffectModder
 *
 * Copyright (C) 2015-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StreamHelpers;

namespace MassEffectModder
{
    class ME3DLC : IDisposable
    {
        const uint SfarTag = 0x53464152; // 'SFAR'
        const uint SfarVersion = 0x00010000;
        const uint LZMATag = 0x6c7a6d61; // 'lzma'
        const uint HeaderSize = 0x20;
        const uint EntryHeaderSize = 0x1e;
        byte[] FileListHash = new byte[] { 0xb5, 0x50, 0x19, 0xcb, 0xf9, 0xd3, 0xda, 0x65, 0xd5, 0x5b, 0x32, 0x1c, 0x00, 0x19, 0x69, 0x7c };
        const long MaxBlockSize = 0x00010000;
        MainWindow mainWindow;
        FileStream sfarFile;
        int filenamesIndex;
        int TOCFileIndex;
        MemoryStream TOCcontent;
        TOCBinFile tocFile;
        uint filesCount;
        List<FileEntry> filesList;
        uint maxBlockSize;
        List<ushort> blockSizes;

        public struct FileEntry
        {
            public byte[] filenameHash;
            public string filenamePath;
            public long uncomprSize;
            public int compressedBlockSizesIndex;
            public uint numBlocks;
            public long dataOffset;
        }

        class FileArrayComparer : IComparer<FileEntry>
        {
            public int Compare(FileEntry x, FileEntry y)
            {
                return StructuralComparisons.StructuralComparer.Compare(x.filenameHash, y.filenameHash);
            }
        }

        public ME3DLC(MainWindow main)
        {
            mainWindow = main;
        }

        public void Dispose()
        {
            TOCcontent.Dispose();
            sfarFile.Close();
            sfarFile.Dispose();
            sfarFile = null;
        }

        private void loadHeader(string filename)
        {
            if (!File.Exists(filename))
                throw new Exception("File not found: " + filename);

            if (sfarFile != null)
                return;

            sfarFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            uint tag = sfarFile.ReadUInt32();
            if (tag != SfarTag)
                throw new Exception("Wrong SFAR tag");
            uint sfarVersion = sfarFile.ReadUInt32();
            if (sfarVersion != SfarVersion)
                throw new Exception("Wrong SFAR version");

            uint dataOffset = sfarFile.ReadUInt32();
            uint entriesOffset = sfarFile.ReadUInt32();
            filesCount = sfarFile.ReadUInt32();
            uint sizesArrayOffset = sfarFile.ReadUInt32();
            maxBlockSize = sfarFile.ReadUInt32();
            uint compressionTag = sfarFile.ReadUInt32();
            if (compressionTag != LZMATag)
                throw new Exception("Not LZMA compression for SFAR file");

            uint numBlockSizes = 0;
            sfarFile.JumpTo(entriesOffset);
            filesList = new List<FileEntry>();
            for (int i = 0; i < filesCount; i++)
            {
                FileEntry file = new FileEntry();
                file.filenameHash = sfarFile.ReadToBuffer(16);
                file.compressedBlockSizesIndex = sfarFile.ReadInt32();
                file.uncomprSize = sfarFile.ReadUInt32();
                file.uncomprSize |= (long)sfarFile.ReadByte() << 32;
                file.dataOffset = sfarFile.ReadUInt32();
                file.dataOffset |= (long)sfarFile.ReadByte() << 32;
                file.numBlocks = (uint)((file.uncomprSize + maxBlockSize - 1) / maxBlockSize);
                filesList.Add(file);
                numBlockSizes += file.numBlocks;
            }

            sfarFile.JumpTo(sizesArrayOffset);
            blockSizes = new List<ushort>();
            for (int i = 0; i < numBlockSizes; i++)
            {
                blockSizes.Add(sfarFile.ReadUInt16());
            }

            filenamesIndex = -1;
            for (int i = 0; i < filesCount; i++)
            {
                if (StructuralComparisons.StructuralEqualityComparer.Equals(filesList[i].filenameHash, FileListHash))
                {
                    sfarFile.JumpTo(filesList[i].dataOffset);
                    int compressedBlockSize = blockSizes[filesList[i].compressedBlockSizesIndex];
                    byte[] inBuf = sfarFile.ReadToBuffer(compressedBlockSize);
                    byte[] outBuf = SevenZipHelper.LZMA.Decompress(inBuf, (uint)filesList[i].uncomprSize);
                    if (outBuf.Length == 0)
                        throw new Exception();
                    TOCcontent = new MemoryStream();
                    StreamReader filenamesStream = new StreamReader(new MemoryStream(outBuf));
                    while (filenamesStream.EndOfStream == false)
                    {
                        string name = filenamesStream.ReadLine();
                        byte[] hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant()));
                        for (int l = 0; l < filesCount; l++)
                        {
                            if (StructuralComparisons.StructuralEqualityComparer.Equals(filesList[l].filenameHash, hash))
                            {
                                FileEntry f = filesList[l];
                                f.filenamePath = name;
                                filesList[l] = f;
                            }
                        }
                        TOCcontent.WriteStringASCII(name + Environment.NewLine);
                    }
                    filenamesIndex = i;
                    break;
                }
            }
            if (filenamesIndex == -1)
                throw new Exception("filenames entry not found");
            TOCcontent.SeekBegin();

            TOCFileIndex = -1;
            for (int i = 0; i < filesCount; i++)
            {
                if (filesList[i].filenamePath != null && filesList[i].filenamePath.EndsWith("PCConsoleTOC.bin"))
                {
                    tocFile = new TOCBinFile(unpackFileEntry(filesList[i].filenamePath));
                    TOCFileIndex = i;
                }
            }
        }

        public byte[] unpackFileEntry(string filename)
        {
            if (sfarFile == null)
                throw new Exception();

            string name = filename.Replace('\\', '/');
            byte[] fileHash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant()));
            int index = filesList.FindIndex(s => StructuralComparisons.StructuralEqualityComparer.Equals(s.filenameHash, fileHash));
            if (index == -1)
                throw new Exception();
            using (MemoryStream outputFile = new MemoryStream())
            {
                sfarFile.JumpTo(filesList[index].dataOffset);
                if (filesList[index].compressedBlockSizesIndex == -1)
                {
                    outputFile.WriteFromStream(sfarFile, filesList[index].uncomprSize);
                }
                else
                {
                    long bytesLeft = filesList[index].uncomprSize;
                    for (int j = 0; j < filesList[index].numBlocks; j++)
                    {
                        int compressedBlockSize = blockSizes[filesList[index].compressedBlockSizesIndex + j];
                        int uncompressedBlockSize = (int)Math.Min(bytesLeft, maxBlockSize);
                        if (compressedBlockSize == 0)
                        {
                            compressedBlockSize = (int)maxBlockSize;
                            outputFile.WriteFromStream(sfarFile, compressedBlockSize);
                        }
                        else if (compressedBlockSize == bytesLeft)
                        {
                            outputFile.WriteFromStream(sfarFile, compressedBlockSize);
                        }
                        else
                        {
                            byte[] inBuf = sfarFile.ReadToBuffer(compressedBlockSize);
                            byte[] outBuf = SevenZipHelper.LZMA.Decompress(inBuf, (uint)uncompressedBlockSize);
                            if (outBuf.Length == 0)
                                throw new Exception();
                            outputFile.WriteFromBuffer(outBuf);
                        }
                        bytesLeft -= uncompressedBlockSize;
                    }
                }
                return outputFile.ToArray();
            }
        }

        public void extract(string SFARfilename, string outPath)
        {
            loadHeader(SFARfilename);

            using (FileStream outputFile = new FileStream(outPath + @"\TOC", FileMode.Create, FileAccess.Write))
            {
                outputFile.WriteFromStream(TOCcontent, TOCcontent.Length);
            }

            for (int i = 0; i < filesCount; i++)
            {
                if (filenamesIndex == i)
                    continue;
                if (filesList[i].filenamePath == null)
                    throw new Exception("filename missing");

                mainWindow.updateStatusLabel2("File " + (i + 1) + " of " + filesList.Count() + " - " + Path.GetFileName(filesList[i].filenamePath));

                string dir = Path.GetDirectoryName(filesList[i].filenamePath);
                Directory.CreateDirectory(outPath + dir);
                using (FileStream outputFile = new FileStream(outPath + filesList[i].filenamePath, FileMode.Create, FileAccess.Write))
                {
                    sfarFile.JumpTo(filesList[i].dataOffset);
                    if (filesList[i].compressedBlockSizesIndex == -1)
                    {
                        outputFile.WriteFromStream(sfarFile, filesList[i].uncomprSize);
                    }
                    else
                    {
                        long bytesLeft = filesList[i].uncomprSize;
                        for (int j = 0; j < filesList[i].numBlocks; j++)
                        {
                            int compressedBlockSize = blockSizes[filesList[i].compressedBlockSizesIndex + j];
                            int uncompressedBlockSize = (int)Math.Min(bytesLeft, maxBlockSize);
                            if (compressedBlockSize == 0)
                            {
                                compressedBlockSize = (int)maxBlockSize;
                                outputFile.WriteFromStream(sfarFile, compressedBlockSize);
                            }
                            else if (compressedBlockSize == bytesLeft)
                            {
                                outputFile.WriteFromStream(sfarFile, compressedBlockSize);
                            }
                            else
                            {
                                byte[] inBuf = sfarFile.ReadToBuffer(compressedBlockSize);
                                byte[] outBuf = SevenZipHelper.LZMA.Decompress(inBuf, (uint)uncompressedBlockSize);
                                if (outBuf.Length == 0)
                                    throw new Exception();
                                outputFile.WriteFromBuffer(outBuf);
                            }
                            bytesLeft -= uncompressedBlockSize;
                        }
                    }
                }
            }
            sfarFile.Close();
            sfarFile.Dispose();
            sfarFile = null;
        }

        public void update(string filename, List<string> modifiedFiles)
        {
            loadHeader(filename);

            List<byte[]> hashList = new List<byte[]>();
            FileEntry e;
            for (int k = 0; k < modifiedFiles.Count(); k++)
            {
                int pos = modifiedFiles[k].IndexOf("CookedPCConsole");
                if (pos == -1)
                    throw new Exception();
                string TOCBinfilePath = modifiedFiles[k].Substring(pos);
                pos = modifiedFiles[k].IndexOf("\\BIOGame\\DLC\\");
                if (pos == -1)
                    throw new Exception();
                string fileName = modifiedFiles[k].Substring(pos).Replace('\\', '/').ToLowerInvariant();
                byte[] fileHash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(fileName));
                hashList.Add(fileHash);
                int index = filesList.FindIndex(s => StructuralComparisons.StructuralEqualityComparer.Equals(s.filenameHash, fileHash));
                if (index == -1)
                    throw new Exception("File not found in SFAR file");
                e = filesList[index];
                e.uncomprSize = (uint)new FileInfo(GameData.GamePath + modifiedFiles[k]).Length;
                filesList[index] = e;
                tocFile.updateFile(TOCBinfilePath, GameData.GamePath + modifiedFiles[k]);
            }
            byte[] TOCFileMem = tocFile.saveToBuffer();

            modifiedFiles.Add("PCConsoleTOC.bin");
            hashList.Add(filesList[TOCFileIndex].filenameHash);
            e = filesList[TOCFileIndex];
            e.uncomprSize = TOCFileMem.Length;
            filesList[TOCFileIndex] = e;

            using (FileStream outputFile = new FileStream(filename + ".tmp", FileMode.Create, FileAccess.Write))
            {
                long numBlockSizes = 0;
                int curBlockSizesIndex = 0;
                long dataOffset = HeaderSize + EntryHeaderSize * (filesList.Count());
                long sizesArrayOffset = dataOffset;
                for (int i = 0; i < filesList.Count(); i++)
                {
                    e = filesList[i];
                    if (e.filenamePath != null && (e.filenamePath.EndsWith(".bik") || e.filenamePath.EndsWith(".afc")))
                        continue;
                    long numBlocks = (e.uncomprSize + MaxBlockSize - 1) / MaxBlockSize;
                    dataOffset += numBlocks * sizeof(ushort);
                    numBlockSizes += numBlocks;
                }

                ushort[] newBlockSizes = new ushort[numBlockSizes];
                long curDataOffset = dataOffset;
                outputFile.JumpTo(dataOffset);
                for (int i = 0; i < filesList.Count(); i++)
                {
                    Stream inputFile = null;
                    FileEntry file = filesList[i];
                    mainWindow.updateStatusLabel2("File " + (i + 1) + " of " + filesList.Count() + " - " + Path.GetFileName(file.filenamePath));

                    if (file.filenamePath != null && file.filenamePath.EndsWith("PCConsoleTOC.bin"))
                    {
                        inputFile = new MemoryStream(TOCFileMem);
                    }
                    else if (hashList.Exists(a => StructuralComparisons.StructuralEqualityComparer.Equals(a, file.filenameHash)))
                    {
                        int index = hashList.FindIndex(a => StructuralComparisons.StructuralEqualityComparer.Equals(a, file.filenameHash));
                        inputFile = new FileStream(GameData.GamePath + modifiedFiles[index], FileMode.Open, FileAccess.Read);
                    }
                    file.dataOffset = curDataOffset;

                    if (file.filenamePath != null && (file.filenamePath.EndsWith(".bik") || file.filenamePath.EndsWith(".afc")))
                    {
                        if (!hashList.Exists(a => StructuralComparisons.StructuralEqualityComparer.Equals(a, file.filenameHash)))
                        {
                            sfarFile.JumpTo(filesList[i].dataOffset);
                            outputFile.WriteFromStream(sfarFile, file.uncomprSize);
                        }
                        else
                        {
                            outputFile.WriteFromStream(inputFile, file.uncomprSize);
                        }
                    }
                    else
                    {
                        file.compressedBlockSizesIndex = curBlockSizesIndex;
                        if (!hashList.Exists(a => StructuralComparisons.StructuralEqualityComparer.Equals(a, file.filenameHash)))
                        {
                            int compressedSize = 0;
                            for (int k = 0; k < file.numBlocks; k++, curBlockSizesIndex++)
                            {
                                newBlockSizes[curBlockSizesIndex] = blockSizes[filesList[i].compressedBlockSizesIndex + k];
                                compressedSize += newBlockSizes[curBlockSizesIndex];
                            }
                            sfarFile.JumpTo(filesList[i].dataOffset);
                            outputFile.WriteFromStream(sfarFile, compressedSize);
                        }
                        else
                        {
                            file.numBlocks = (uint)((file.uncomprSize + MaxBlockSize - 1) / MaxBlockSize);
                            for (int k = 0; k < file.numBlocks; k++, curBlockSizesIndex++)
                            {
                                long uncompressedBlockSize = MaxBlockSize;
                                if (k == (file.numBlocks - 1)) // last block
                                    uncompressedBlockSize = file.uncomprSize - (MaxBlockSize * k);
                                byte[] inBuf = inputFile.ReadToBuffer((int)uncompressedBlockSize);
                                byte[] outBuf = SevenZipHelper.LZMA.Compress(inBuf, 9);
                                if (outBuf.Length == 0)
                                    throw new Exception();
                                if (outBuf.Length >= (int)MaxBlockSize)
                                {
                                    outputFile.WriteFromBuffer(inBuf);
                                    newBlockSizes[curBlockSizesIndex] = 0;
                                }
                                else if (outBuf.Length >= inBuf.Length)
                                {
                                    outputFile.WriteFromBuffer(inBuf);
                                    newBlockSizes[curBlockSizesIndex] = (ushort)inBuf.Length;
                                }
                                else
                                {
                                    outputFile.WriteFromBuffer(outBuf);
                                    newBlockSizes[curBlockSizesIndex] = (ushort)outBuf.Length;
                                }
                            }
                        }
                    }
                    filesList[i] = file;
                    curDataOffset = outputFile.Position;
                }

                outputFile.SeekBegin();
                outputFile.WriteUInt32(SfarTag);
                outputFile.WriteUInt32(SfarVersion);
                outputFile.WriteUInt32((uint)dataOffset);
                outputFile.WriteUInt32(HeaderSize);
                outputFile.WriteUInt32((uint)filesList.Count);
                outputFile.WriteUInt32((uint)sizesArrayOffset);
                outputFile.WriteUInt32((uint)MaxBlockSize);
                outputFile.WriteUInt32(LZMATag);

                filesList.Sort(new FileArrayComparer());
                for (int i = 0; i < filesList.Count; i++)
                {
                    outputFile.WriteFromBuffer(filesList[i].filenameHash);
                    outputFile.WriteInt32(filesList[i].compressedBlockSizesIndex);
                    outputFile.WriteUInt32((uint)filesList[i].uncomprSize);
                    outputFile.WriteByte((byte)(filesList[i].uncomprSize >> 32));
                    outputFile.WriteUInt32((uint)filesList[i].dataOffset);
                    outputFile.WriteByte((byte)(filesList[i].dataOffset >> 32));
                }

                if (outputFile.Position != sizesArrayOffset)
                    throw new Exception();

                for (int i = 0; i < newBlockSizes.Count(); i++)
                {
                    outputFile.WriteUInt16(newBlockSizes[i]);
                }

                if (outputFile.Position != dataOffset)
                    throw new Exception();
            }
            sfarFile.Close();
            sfarFile.Dispose();
            sfarFile = null;
            File.Delete(filename);
            File.Move(filename + ".tmp", filename);
        }

        public void fullRePack(string inPath, string outPath, string DLCName)
        {
            if (sfarFile != null)
                throw new Exception();

            if (!Directory.Exists(inPath))
                throw new Exception("Directory not found: " + inPath);

            int indexTOC = -1;
            List<byte[]> hashList = new List<byte[]>();
            List<string> srcFilesList = new List<string>();
            string TOCFilePath = inPath + @"\TOC";
            string[] srcFilesOrg = File.ReadAllLines(TOCFilePath);
            for (int i = 0; i < srcFilesOrg.Count(); i++)
            {
                if (srcFilesOrg[i].EndsWith("PCConsoleTOC.bin"))
                {
                    indexTOC = i;
                }
                string filename = srcFilesOrg[i].ToLowerInvariant();
                hashList.Add(MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(filename)));
                srcFilesList.Add(inPath + srcFilesOrg[i].Replace('/', '\\'));
            }
            hashList.Add(FileListHash);
            srcFilesList.Add(TOCFilePath);

            TOCBinFile tocFile = new TOCBinFile(srcFilesList[indexTOC]);
            int pos = (@"\BIOGame\DLC\" + DLCName + @"\").Length;
            for (int i = 0; i < srcFilesOrg.Count(); i++)
            {
                string filename = srcFilesOrg[i].Substring(pos).Replace('/', '\\');
                tocFile.updateFile(filename, srcFilesList[i]);
            }
            tocFile.saveToFile(srcFilesList[indexTOC]);

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            using (FileStream outputFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                long numBlockSizes = 0;
                int curBlockSizesIndex = 0;
                long dataOffset = HeaderSize + EntryHeaderSize * (srcFilesList.Count());
                long sizesArrayOffset = dataOffset;
                for (int i = 0; i < srcFilesList.Count(); i++)
                {
                    if (srcFilesList[i].EndsWith(".bik") || srcFilesList[i].EndsWith(".afc"))
                        continue;
                    long fileLen = new FileInfo(srcFilesList[i]).Length;
                    long numBlocks = (fileLen + MaxBlockSize - 1) / MaxBlockSize;
                    dataOffset += numBlocks * sizeof(ushort);
                    numBlockSizes += numBlocks;
                }

                List<FileEntry> filesList = new List<FileEntry>();
                ushort[] blockSizes = new ushort[numBlockSizes];
                long curDataOffset = dataOffset;
                outputFile.JumpTo(dataOffset);
                for (int i = 0; i < srcFilesList.Count(); i++)
                {
                    mainWindow.updateStatusLabel2("File " + (i + 1) + " of " + srcFilesList.Count() + " - " + Path.GetFileName(srcFilesList[i]));
                    FileEntry file = new FileEntry();
                    Stream inputFile = new FileStream(srcFilesList[i], FileMode.Open, FileAccess.Read);
                    long fileLen = new FileInfo(srcFilesList[i]).Length;
                    file.dataOffset = curDataOffset;
                    file.uncomprSize = fileLen;
                    file.filenameHash = hashList[i];
                    if (srcFilesList[i].EndsWith(".bik") || srcFilesList[i].EndsWith(".afc"))
                    {
                        outputFile.WriteFromStream(inputFile, fileLen);
                        file.compressedBlockSizesIndex = -1;
                    }
                    else
                    {
                        file.compressedBlockSizesIndex = curBlockSizesIndex;
                        file.numBlocks = (uint)((fileLen + MaxBlockSize - 1) / MaxBlockSize);
                        for (int k = 0; k < file.numBlocks; k++, curBlockSizesIndex++)
                        {
                            long uncompressedBlockSize = MaxBlockSize;
                            if (k == (file.numBlocks - 1)) // last block
                                uncompressedBlockSize = fileLen - (MaxBlockSize * k);
                            byte[] inBuf = inputFile.ReadToBuffer((int)uncompressedBlockSize);
                            byte[] outBuf = SevenZipHelper.LZMA.Compress(inBuf, 9);
                            if (outBuf.Length == 0)
                                throw new Exception();
                            if (outBuf.Length >= (int)MaxBlockSize)
                            {
                                outputFile.WriteFromBuffer(inBuf);
                                blockSizes[curBlockSizesIndex] = 0;
                            }
                            else if (outBuf.Length >= inBuf.Length)
                            {
                                outputFile.WriteFromBuffer(inBuf);
                                blockSizes[curBlockSizesIndex] = (ushort)inBuf.Length;
                            }
                            else
                            {
                                outputFile.WriteFromBuffer(outBuf);
                                blockSizes[curBlockSizesIndex] = (ushort)outBuf.Length;
                            }
                        }
                    }
                    curDataOffset = outputFile.Position;
                    filesList.Add(file);
                }

                if (blockSizes.Count() != curBlockSizesIndex)
                    throw new Exception();

                outputFile.SeekBegin();
                outputFile.WriteUInt32(SfarTag);
                outputFile.WriteUInt32(SfarVersion);
                outputFile.WriteUInt32((uint)dataOffset);
                outputFile.WriteUInt32(HeaderSize);
                outputFile.WriteUInt32((uint)filesList.Count);
                outputFile.WriteUInt32((uint)sizesArrayOffset);
                outputFile.WriteUInt32((uint)MaxBlockSize);
                outputFile.WriteUInt32(LZMATag);

                filesList.Sort(new FileArrayComparer());
                for (int i = 0; i < filesList.Count; i++)
                {
                    outputFile.WriteFromBuffer(filesList[i].filenameHash);
                    outputFile.WriteInt32(filesList[i].compressedBlockSizesIndex);
                    outputFile.WriteUInt32((uint)filesList[i].uncomprSize);
                    outputFile.WriteByte((byte)(filesList[i].uncomprSize >> 32));
                    outputFile.WriteUInt32((uint)filesList[i].dataOffset);
                    outputFile.WriteByte((byte)(filesList[i].dataOffset >> 32));
                }

                if (outputFile.Position != sizesArrayOffset)
                    throw new Exception();

                for (int i = 0; i < blockSizes.Count(); i++)
                {
                    outputFile.WriteUInt16(blockSizes[i]);
                }

                if (outputFile.Position != dataOffset)
                    throw new Exception();
            }
        }
    }
}
