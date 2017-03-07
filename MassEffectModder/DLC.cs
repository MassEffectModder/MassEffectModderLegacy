/*
 * MassEffectModder
 *
 * Copyright (C) 2015-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Threading.Tasks;
using System.Windows.Forms;

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
                    }
                    filenamesIndex = i;
                    break;
                }
            }
            if (filenamesIndex == -1)
                throw new Exception("filenames entry not found");

            TOCFileIndex = -1;
            for (int i = 0; i < filesCount; i++)
            {
                if (filesList[i].filenamePath != null && filesList[i].filenamePath.EndsWith("PCConsoleTOC.bin", StringComparison.OrdinalIgnoreCase))
                {
                    TOCFileIndex = i;
                    break;
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
                    List<byte[]> uncompressedBlockBuffers = new List<byte[]>();
                    List<byte[]> compressedBlockBuffers = new List<byte[]>();
                    List<long> blockBytesLeft = new List<long>();
                    long bytesLeft = filesList[index].uncomprSize;
                    for (int j = 0; j < filesList[index].numBlocks; j++)
                    {
                        blockBytesLeft.Add(bytesLeft);
                        int compressedBlockSize = blockSizes[filesList[index].compressedBlockSizesIndex + j];
                        int uncompressedBlockSize = (int)Math.Min(bytesLeft, maxBlockSize);
                        if (compressedBlockSize == 0)
                        {
                            compressedBlockSize = (int)maxBlockSize;
                        }
                        compressedBlockBuffers.Add(sfarFile.ReadToBuffer(compressedBlockSize));
                        uncompressedBlockBuffers.Add(null);
                        bytesLeft -= uncompressedBlockSize;
                    }

                    Parallel.For(0, filesList[index].numBlocks, j =>
                    {
                        int compressedBlockSize = blockSizes[filesList[index].compressedBlockSizesIndex + (int)j];
                        int uncompressedBlockSize = (int)Math.Min(blockBytesLeft[(int)j], maxBlockSize);
                        if (compressedBlockSize == 0 || compressedBlockSize == blockBytesLeft[(int)j])
                        {
                            uncompressedBlockBuffers[(int)j] = compressedBlockBuffers[(int)j];
                        }
                        else
                        {
                            uncompressedBlockBuffers[(int)j] = SevenZipHelper.LZMA.Decompress(compressedBlockBuffers[(int)j], (uint)uncompressedBlockSize);
                            if (uncompressedBlockBuffers[(int)j].Length == 0)
                                throw new Exception();
                        }
                    });

                    for (int j = 0; j < filesList[index].numBlocks; j++)
                    {
                        outputFile.WriteFromBuffer(uncompressedBlockBuffers[j]);
                    }
                }
                return outputFile.ToArray();
            }
        }

        public void extract(string SFARfilename, string outPath)
        {
            loadHeader(SFARfilename);

            Directory.CreateDirectory(Path.Combine(outPath, "CookedPCConsole"));
            using (FileStream outputFile = new FileStream(Path.Combine(outPath, "CookedPCConsole", "Default.sfar"), FileMode.Create, FileAccess.Write))
            {
                outputFile.WriteUInt32(SfarTag);
                outputFile.WriteUInt32(SfarVersion);
                outputFile.WriteUInt32(HeaderSize);
                outputFile.WriteUInt32(HeaderSize);
                outputFile.WriteUInt32((uint)filesList.Count);
                outputFile.WriteUInt32(HeaderSize);
                outputFile.WriteUInt32((uint)MaxBlockSize);
                outputFile.WriteUInt32(LZMATag);
            }

            for (int i = 0; i < filesCount; i++)
            {
                if (filenamesIndex == i)
                    continue;
                if (filesList[i].filenamePath == null)
                    throw new Exception("filename missing");

                if (mainWindow != null)
                    mainWindow.updateStatusLabel2("File " + (i + 1) + " of " + filesList.Count() + " - " + Path.GetFileName(filesList[i].filenamePath));

                int pos = filesList[i].filenamePath.IndexOf("\\BIOGame\\DLC\\", StringComparison.OrdinalIgnoreCase);
                string filename = filesList[i].filenamePath.Substring(pos + ("\\BIOGame\\DLC\\").Length).Replace('/', '\\');
                string dir = Path.GetDirectoryName(outPath);
                Directory.CreateDirectory(Path.GetDirectoryName(dir + filename));
                using (FileStream outputFile = new FileStream(dir + filename, FileMode.Create, FileAccess.Write))
                {
                    sfarFile.JumpTo(filesList[i].dataOffset);
                    if (filesList[i].compressedBlockSizesIndex == -1)
                    {
                        outputFile.WriteFromStream(sfarFile, filesList[i].uncomprSize);
                    }
                    else
                    {
                        List<byte[]> uncompressedBlockBuffers = new List<byte[]>();
                        List<byte[]> compressedBlockBuffers = new List<byte[]>();
                        List<long> blockBytesLeft = new List<long>();
                        long bytesLeft = filesList[i].uncomprSize;
                        for (int j = 0; j < filesList[i].numBlocks; j++)
                        {
                            blockBytesLeft.Add(bytesLeft);
                            int compressedBlockSize = blockSizes[filesList[i].compressedBlockSizesIndex + j];
                            int uncompressedBlockSize = (int)Math.Min(bytesLeft, maxBlockSize);
                            if (compressedBlockSize == 0)
                            {
                                compressedBlockSize = (int)maxBlockSize;
                            }
                            compressedBlockBuffers.Add(sfarFile.ReadToBuffer(compressedBlockSize));
                            uncompressedBlockBuffers.Add(null);
                            bytesLeft -= uncompressedBlockSize;
                        }

                        Parallel.For(0, filesList[i].numBlocks, j =>
                        {
                            int compressedBlockSize = blockSizes[filesList[i].compressedBlockSizesIndex + (int)j];
                            int uncompressedBlockSize = (int)Math.Min(blockBytesLeft[(int)j], maxBlockSize);
                            if (compressedBlockSize == 0 || compressedBlockSize == blockBytesLeft[(int)j])
                            {
                                uncompressedBlockBuffers[(int)j] = compressedBlockBuffers[(int)j];
                            }
                            else
                            {
                                uncompressedBlockBuffers[(int)j] = SevenZipHelper.LZMA.Decompress(compressedBlockBuffers[(int)j], (uint)uncompressedBlockSize);
                                if (uncompressedBlockBuffers[(int)j].Length == 0)
                                    throw new Exception();
                            }
                        });

                        for (int j = 0; j < filesList[i].numBlocks; j++)
                        {
                            outputFile.WriteFromBuffer(uncompressedBlockBuffers[j]);
                        }
                    }
                }
            }
            sfarFile.Close();
            sfarFile.Dispose();
            sfarFile = null;
        }

        public void fullRePack(string inPath, string outPath, string DLCName, MainWindow mainWindow, Installer installer)
        {
            if (sfarFile != null)
                throw new Exception();

            if (!Directory.Exists(inPath))
                throw new Exception("Directory not found: " + inPath);

            int indexTOC = -1;
            List<byte[]> hashList = new List<byte[]>();
            List<string> srcFilesList = Directory.GetFiles(inPath, "*.*", SearchOption.AllDirectories).ToList();
            srcFilesList.RemoveAll(s => s.ToLower().Contains("default.sfar"));
            using (FileStream outputFile = new FileStream(inPath + @"\TOC", FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < srcFilesList.Count(); i++)
                {
                    int pos = srcFilesList[i].IndexOf("\\BIOGame\\DLC\\", StringComparison.OrdinalIgnoreCase);
                    string filename = srcFilesList[i].Substring(pos).Replace('\\', '/');
                    if (filename.EndsWith("PCConsoleTOC.bin", StringComparison.OrdinalIgnoreCase))
                    {
                        indexTOC = i;
                    }
                    hashList.Add(MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(filename.ToLowerInvariant())));
                    outputFile.WriteStringASCII(filename + Environment.NewLine);
                }
            }

            hashList.Add(FileListHash);
            srcFilesList.Add(inPath + @"\TOC");

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            using (FileStream outputFile = new FileStream(outPath, FileMode.Create, FileAccess.Write))
            {
                long numBlockSizes = 0;
                int curBlockSizesIndex = 0;
                long dataOffset = HeaderSize + EntryHeaderSize * (srcFilesList.Count());
                long sizesArrayOffset = dataOffset;
                for (int i = 0; i < srcFilesList.Count(); i++)
                {
                    if (srcFilesList[i].EndsWith(".bik", StringComparison.OrdinalIgnoreCase) 
                        || srcFilesList[i].EndsWith(".afc", StringComparison.OrdinalIgnoreCase))
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
                    if (mainWindow != null)
                        mainWindow.updateStatusLabel2("File " + (i + 1) + " of " + srcFilesList.Count() + " - " + Path.GetFileName(srcFilesList[i]));
                    if (installer != null)
                        installer.updateStatusPrepare("Compressing DLC... " + (i + 1) + " of " + srcFilesList.Count);
                    FileEntry file = new FileEntry();
                    Stream inputFile = new FileStream(srcFilesList[i], FileMode.Open, FileAccess.Read);
                    long fileLen = new FileInfo(srcFilesList[i]).Length;
                    file.dataOffset = curDataOffset;
                    file.uncomprSize = fileLen;
                    file.filenameHash = hashList[i];
                    if (srcFilesList[i].EndsWith(".bik", StringComparison.OrdinalIgnoreCase)
                        || srcFilesList[i].EndsWith(".afc", StringComparison.OrdinalIgnoreCase))
                    {
                        outputFile.WriteFromStream(inputFile, fileLen);
                        file.compressedBlockSizesIndex = -1;
                    }
                    else
                    {
                        List<byte[]> uncompressedBlockBuffers = new List<byte[]>();
                        List<byte[]> compressedBlockBuffers = new List<byte[]>();
                        file.compressedBlockSizesIndex = curBlockSizesIndex;
                        file.numBlocks = (uint)((file.uncomprSize + MaxBlockSize - 1) / MaxBlockSize);
                        for (int k = 0; k < file.numBlocks; k++)
                        {
                            long uncompressedBlockSize = MaxBlockSize;
                            if (k == (file.numBlocks - 1)) // last block
                                uncompressedBlockSize = file.uncomprSize - (MaxBlockSize * k);
                            uncompressedBlockBuffers.Add(inputFile.ReadToBuffer((int)uncompressedBlockSize));
                            compressedBlockBuffers.Add(null);
                        }

                        Parallel.For(0, file.numBlocks, k =>
                        {
                            compressedBlockBuffers[(int)k] = SevenZipHelper.LZMA.Compress(uncompressedBlockBuffers[(int)k], 9);
                            if (compressedBlockBuffers[(int)k].Length == 0)
                                throw new Exception();
                        });

                        for (int k = 0; k < file.numBlocks; k++, curBlockSizesIndex++)
                        {
                            if (compressedBlockBuffers[k].Length >= (int)MaxBlockSize)
                            {
                                outputFile.WriteFromBuffer(uncompressedBlockBuffers[k]);
                                blockSizes[curBlockSizesIndex] = 0;
                            }
                            else if (compressedBlockBuffers[k].Length >= uncompressedBlockBuffers[k].Length)
                            {
                                outputFile.WriteFromBuffer(uncompressedBlockBuffers[k]);
                                blockSizes[curBlockSizesIndex] = (ushort)uncompressedBlockBuffers[k].Length;
                            }
                            else
                            {
                                outputFile.WriteFromBuffer(compressedBlockBuffers[k]);
                                blockSizes[curBlockSizesIndex] = (ushort)compressedBlockBuffers[k].Length;
                            }
                        }
                    }
                    curDataOffset = outputFile.Position;
                    filesList.Add(file);
                    inputFile.Close();
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
            File.Delete(inPath + @"\TOC");
        }

        static public void unpackAllDLC(MainWindow mainWindow, Installer installer)
        {
            if (!Directory.Exists(GameData.DLCData))
            {
                if (mainWindow != null)
                    MessageBox.Show("No DLCs need to be extracted.");
                return;
            }

            List<string> sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < sfarFiles.Count; i++)
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                    sfarFiles.RemoveAt(i--);
            }
            if (sfarFiles.Count() == 0)
            {
                if (mainWindow != null)
                    MessageBox.Show("No DLCs need to be extracted.");
                return;
            }

            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;
            for (int i = 0; i < sfarFiles.Count; i++)
            {
                diskUsage += new FileInfo(sfarFiles[i]).Length;
            }
            diskUsage = (long)(diskUsage * 2.5);
            if (diskUsage > diskFreeSpace)
            {
                if (mainWindow != null)
                    MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                return;
            }

            Misc.startTimer();

            string tmpDlcDir = Path.Combine(GameData.GamePath, "BIOGame", "DLCTemp");
            if (Directory.Exists(tmpDlcDir))
                Directory.Delete(tmpDlcDir, true);
            Directory.CreateDirectory(tmpDlcDir);
            string originInstallFiles = Path.Combine(GameData.DLCData, "__metadata");
            if (Directory.Exists(originInstallFiles))
                Directory.Move(originInstallFiles, tmpDlcDir + "\\__metadata");
            for (int i = 0; i < sfarFiles.Count; i++)
            {
                string DLCname = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(sfarFiles[i])));
                string outPath = Path.Combine(tmpDlcDir, DLCname);
                Directory.CreateDirectory(outPath);
                ME3DLC dlc = new ME3DLC(mainWindow);
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("SFAR extracting - DLC " + (i + 1) + " of " + sfarFiles.Count);
                }
                if (installer != null)
                {
                    installer.updateStatusPrepare("Extracting DLC ... " + (i + 1) + " of " + sfarFiles.Count);
                }
                dlc.extract(sfarFiles[i], outPath);
            }

            sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
            for (int i = 0; i < sfarFiles.Count; i++)
            {
                if (File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                {
                    string source = Path.GetDirectoryName(Path.GetDirectoryName(sfarFiles[i]));
                    Directory.Move(source, tmpDlcDir + "\\" + Path.GetFileName(source));
                }
            }

            Directory.Delete(GameData.DLCData, true);
            Directory.Move(tmpDlcDir, GameData.DLCData);

            var time = Misc.stopTimer();
            if (mainWindow != null)
                mainWindow.updateStatusLabel("DLCs extracted. Process total time: " + Misc.getTimerFormat(time));
        }
    }
}
