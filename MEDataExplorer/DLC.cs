/*
 * MEDataExplorer
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
using Gibbed.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace MEDataExplorer
{
    class ME3DLC
    {
        const uint SfarTag = 0x53464152; // 'SFAR'
        const uint SfarVersion = 0x00010000;
        const uint LZMATag = 0x6c7a6d61; // 'lzma'
        const uint HeaderSize = 0x20;
        const uint EntryHeaderSize = 0x1e;
        byte[] FileListHash = new byte[] { 0xb5, 0x50, 0x19, 0xcb, 0xf9, 0xd3, 0xda, 0x65, 0xd5, 0x5b, 0x32, 0x1c, 0x00, 0x19, 0x69, 0x7c };
        const ulong MaxBlockSize = 0x00010000;

        public struct FileEntry
        {
            public byte[] filenameHash;
            public long uncomprSize;
            public int compressedBlockSizesIndex;
            public int numBlocks;
            public long dataOffset;
        }

        public void extract(string filename, string outPath, string DLCName)
        {
            if (!File.Exists(filename))
                throw new Exception("File not found: " + filename);

            FileStream sfarFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            uint tag = sfarFile.ReadValueU32();
            if (tag != SfarTag)
                throw new Exception("Wrong SFAR tag");
            uint sfarVersion = sfarFile.ReadValueU32();
            if (sfarVersion != SfarVersion)
                throw new Exception("Wrong SFAR version");

            var dataOffset = sfarFile.ReadValueU32();
            var entriesOffset = sfarFile.ReadValueU32();
            var filesCount = sfarFile.ReadValueU32();
            var sizesArrayOffset = sfarFile.ReadValueU32();
            var maxBlockSize = sfarFile.ReadValueU32();
            var compressionTag = sfarFile.ReadValueU32();
            if (compressionTag != LZMATag)
                throw new Exception("Not LZMA compression for SFAR file");

            List<FileEntry> filesList = new List<FileEntry>();
            for (int i = 0; i < filesCount; i++)
            {
                FileEntry file = new FileEntry();
                file.filenameHash = sfarFile.ReadBytes(16);
                file.compressedBlockSizesIndex = sfarFile.ReadValueS32();
                file.uncomprSize = sfarFile.ReadValueU32();
                file.uncomprSize |= (long)sfarFile.ReadValueU8() << 32;
                file.dataOffset = sfarFile.ReadValueU32();
                file.dataOffset |= (long)sfarFile.ReadValueU8() << 32;
                file.numBlocks = (int)((file.uncomprSize + maxBlockSize - 1) / maxBlockSize);
                filesList.Add(file);
            }
            if (sfarFile.Position != sizesArrayOffset)
                throw new Exception("not expected file position");

            var numBlockSizes = (dataOffset - sizesArrayOffset) / sizeof(ushort);
            ushort[] blockSizes = new ushort[numBlockSizes];
            for (int i = 0; i < numBlockSizes; i++)
            {
                blockSizes[i] = sfarFile.ReadValueU16();
            }
            if (sfarFile.Position != dataOffset)
                throw new Exception("not expected file position");

            int filenamesIndex = -1;
            string[] filenamesArray = new string[filesCount];
            for (int i = 0; i < filesCount; i++)
            {
                if (StructuralComparisons.StructuralEqualityComparer.Equals(filesList[i].filenameHash, FileListHash))
                {
                    sfarFile.Seek(filesList[i].dataOffset, SeekOrigin.Begin);
                    int compressedBlockSize = blockSizes[filesList[i].compressedBlockSizesIndex];
                    byte[] inBuf = sfarFile.ReadBytes(compressedBlockSize);
                    byte[] outBuf = SevenZipHelper.LZMA.Decompress(inBuf, (uint)filesList[i].uncomprSize);
                    if (outBuf.Length == 0)
                        throw new Exception("wrong");
                    var filenamesStream = new StreamReader(new MemoryStream(outBuf));
                    while (filenamesStream.EndOfStream == false)
                    {
                        string name = filenamesStream.ReadLine();
                        var bytes = new byte[name.Length];
                        for (int k = 0; k < name.Length; k++)
                        {
                            bytes[k] = (byte)char.ToLowerInvariant(name[k]);
                        }
                        var md5 = System.Security.Cryptography.MD5.Create();
                        var hash = md5.ComputeHash(bytes);
                        for (int l = 0; l < filesCount; l++)
                        {
                            if (StructuralComparisons.StructuralEqualityComparer.Equals(filesList[l].filenameHash, hash))
                            {
                                name = name.Replace('/', '\\');
                                filenamesArray[l] = name;
                            }
                        }
                    }
                    filenamesIndex = i;
                    break;
                }
            }
            if (filenamesIndex == -1)
                throw new Exception("filenames entry not found");

            for (int i = 0; i < filesCount; i++)
            {
                if (filenamesIndex == i)
                    continue;
                if (filenamesArray[i] == null)
                    throw new Exception("filename missing");

                string dir = Path.GetDirectoryName(filenamesArray[i]);
                Directory.CreateDirectory(outPath + dir);
                using (FileStream outputFile = new FileStream(outPath + filenamesArray[i], FileMode.Create, FileAccess.Write))
                {
                    using (FileStream hashFile = new FileStream(outPath + filenamesArray[i] + ".hash", FileMode.Create, FileAccess.Write))
                    {
                        hashFile.WriteBytes(filesList[i].filenameHash);
                    }

                    sfarFile.Seek(filesList[i].dataOffset, SeekOrigin.Begin);
                    if (filesList[i].compressedBlockSizesIndex == -1)
                    {
                        outputFile.WriteFromStream(sfarFile, filesList[i].uncomprSize);
                    }
                    else
                    {
                        var bytesLeft = filesList[i].uncomprSize;
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
                                byte[] inBuf = sfarFile.ReadBytes(compressedBlockSize);
                                byte[] outBuf = SevenZipHelper.LZMA.Decompress(inBuf, (uint)uncompressedBlockSize);
                                if (outBuf.Length == 0)
                                    throw new Exception("wrong");
                                outputFile.WriteBytes(outBuf);
                            }
                            bytesLeft -= uncompressedBlockSize;
                        }
                    }
                }
            }
            sfarFile.Close();
        }

        public void pack(string inPath, string outPath)
        {
            if (!Directory.Exists(inPath))
                throw new Exception("Directory not found: " + inPath);

            List<string> srcFilesList = Directory.GetFiles(inPath, "*.*", SearchOption.AllDirectories).ToList();
            srcFilesList.RemoveAll(s => s.EndsWith(".hash"));

            List<byte []> hashList = new List<byte []>();

            string[] filenamesArray = new string[srcFilesList.Count];
            var filenameMemStream = new MemoryStream();
            for (int i = 0; i < srcFilesList.Count; i++)
            {
                byte[] hash;
                if (!File.Exists(srcFilesList[i] + ".hash"))
                    throw new Exception("hash file not exist");
                using (FileStream hashFile = new FileStream(srcFilesList[i] + ".hash", FileMode.Open, FileAccess.Read))
                {
                    hash = hashFile.ReadBytes(16);
                    if (hash.Length != 16)
                        throw new Exception("wrong hash");
                }
                int pos = srcFilesList[i].IndexOf("\\BIOGame\\DLC\\", StringComparison.CurrentCultureIgnoreCase);
                string filename = srcFilesList[i].Substring(pos).Replace('\\', '/');
                var bytes = new byte[filename.Length];
                for (int k = 0; k < filename.Length; k++)
                {
                    bytes[k] = (byte)char.ToLowerInvariant(filename[k]);
                }
                var md5 = System.Security.Cryptography.MD5.Create();
                var fileHash = md5.ComputeHash(bytes);

                if (!StructuralComparisons.StructuralEqualityComparer.Equals(fileHash, hash))
                    throw new Exception("wrong hash");

                hashList.Add(hash);
                filenamesArray[i] = filename;
                filenameMemStream.WriteString(filename + Environment.NewLine);
            }
            byte[] filenamesEntry = SevenZipHelper.LZMA.Compress(5, filenameMemStream.ToArray());
            if (filenamesEntry.Length == 0)
                throw new Exception("wrong");

            using (FileStream outputFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                long numBlockSizes = 1;
                int curBlockSizesIndex = 0;
                long dataOffset = (long)(HeaderSize + EntryHeaderSize * (srcFilesList.Count + 1));
                long sizesArrayOffset = dataOffset;
                for (int i = 0; i < srcFilesList.Count; i++)
                {
                    if (srcFilesList[i].EndsWith(".bik") || srcFilesList[i].EndsWith(".afc"))
                        continue;
                    ulong fileLen = (ulong)new FileInfo(srcFilesList[i]).Length;
                    long numBlocks = (long)((fileLen + MaxBlockSize - 1) / MaxBlockSize);
                    dataOffset += numBlocks * sizeof(ushort);
                    numBlockSizes += numBlocks;
                }
                dataOffset += sizeof(ushort); // filenames entry

                List<FileEntry> filesList = new List<FileEntry>();
                ushort[] blockSizes = new ushort[numBlockSizes];
                long curDataOffset = dataOffset;
                outputFile.Seek(dataOffset, SeekOrigin.Begin);
                for (int i = 0; i < srcFilesList.Count; i++)
                {
                    FileEntry file = new FileEntry();
                    using (FileStream inputFile = new FileStream(srcFilesList[i], FileMode.Open, FileAccess.Read))
                    {
                        uint fileLen = (uint)new FileInfo(srcFilesList[i]).Length;
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
                            file.numBlocks = (int)((fileLen + MaxBlockSize - 1) / MaxBlockSize);
                            for (int k = 0; k < file.numBlocks; k++, curBlockSizesIndex++)
                            {
                                ulong uncompressedBlockSize = MaxBlockSize;
                                if (k == (file.numBlocks - 1))
                                    uncompressedBlockSize = fileLen - (MaxBlockSize * (ulong)k);
                                byte[] inBuf = inputFile.ReadBytes((int)uncompressedBlockSize);
                                byte[] outBuf = SevenZipHelper.LZMA.Compress(0, inBuf);
                                if (outBuf.Length == 0)
                                    throw new Exception("wrong");
                                if (outBuf.Length >= (int)MaxBlockSize)
                                {
                                    outputFile.WriteBytes(inBuf);
                                    blockSizes[curBlockSizesIndex] = 0;
                                }
                                else
                                {
                                    outputFile.WriteBytes(outBuf);
                                    blockSizes[curBlockSizesIndex] = (ushort)outBuf.Length;
                                }
                            }
                        }
                    }
                    curDataOffset = outputFile.Position;
                    filesList.Add(file);
                }
                if ((blockSizes.Count() - 1) != curBlockSizesIndex)
                    throw new Exception("wrong");

                blockSizes[curBlockSizesIndex] = (ushort)filenamesEntry.Length;
                FileEntry namefile = new FileEntry();
                namefile.compressedBlockSizesIndex = curBlockSizesIndex;
                namefile.dataOffset = curDataOffset;
                namefile.uncomprSize = filenameMemStream.Length;
                namefile.filenameHash = FileListHash;
                namefile.numBlocks = 1;
                filesList.Add(namefile);
                outputFile.WriteBytes(filenamesEntry);

                outputFile.Seek(0, SeekOrigin.Begin);
                outputFile.WriteValueU32(SfarTag);
                outputFile.WriteValueU32(SfarVersion);
                outputFile.WriteValueU32((uint)dataOffset);
                outputFile.WriteValueU32(HeaderSize);
                outputFile.WriteValueU32((uint)filesList.Count);
                outputFile.WriteValueU32((uint)sizesArrayOffset);
                outputFile.WriteValueU32((uint)MaxBlockSize);
                outputFile.WriteValueU32(LZMATag);

                for (int i = 0; i < filesList.Count; i++)
                {
                    outputFile.WriteBytes(filesList[i].filenameHash);
                    outputFile.WriteValueS32(filesList[i].compressedBlockSizesIndex);
                    outputFile.WriteValueU32((uint)filesList[i].uncomprSize);
                    outputFile.WriteValueU8((byte)(filesList[i].uncomprSize >> 32));
                    outputFile.WriteValueU32((uint)filesList[i].dataOffset);
                    outputFile.WriteValueU8((byte)(filesList[i].dataOffset >> 32));
                }

                if (outputFile.Position != sizesArrayOffset)
                    throw new Exception("wrong");

                for (int i = 0; i < blockSizes.Count(); i++)
                {
                    outputFile.WriteValueU16(blockSizes[i]);
                }

                if (outputFile.Position != dataOffset)
                    throw new Exception("wrong");
            }
        }
    }
}
