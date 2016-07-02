/*
 * MassEffectModder
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
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using StreamHelpers;

namespace MassEffectModder
{
    class ME3DLC
    {
        const uint SfarTag = 0x53464152; // 'SFAR'
        const uint SfarVersion = 0x00010000;
        const uint LZMATag = 0x6c7a6d61; // 'lzma'
        const uint HeaderSize = 0x20;
        const uint EntryHeaderSize = 0x1e;
        byte[] FileListHash = new byte[] { 0xb5, 0x50, 0x19, 0xcb, 0xf9, 0xd3, 0xda, 0x65, 0xd5, 0x5b, 0x32, 0x1c, 0x00, 0x19, 0x69, 0x7c };
        const long MaxBlockSize = 0x00010000;

        public struct FileEntry
        {
            public byte[] filenameHash;
            public long uncomprSize;
            public int compressedBlockSizesIndex;
            public int numBlocks;
            public long dataOffset;
        }

        class FileArrayComparer : IComparer<FileEntry>
        {
            public int Compare(FileEntry x, FileEntry y)
            {
                return StructuralComparisons.StructuralComparer.Compare(x.filenameHash, y.filenameHash);
            }
        }

        public void extract(string filename, string outPath, string DLCName)
        {
            if (!File.Exists(filename))
                throw new Exception("File not found: " + filename);

            FileStream sfarFile = new FileStream(filename, FileMode.Open, FileAccess.Read);
            uint tag = sfarFile.ReadUInt32();
            if (tag != SfarTag)
                throw new Exception("Wrong SFAR tag");
            uint sfarVersion = sfarFile.ReadUInt32();
            if (sfarVersion != SfarVersion)
                throw new Exception("Wrong SFAR version");

            var dataOffset = sfarFile.ReadUInt32();
            var entriesOffset = sfarFile.ReadUInt32();
            var filesCount = sfarFile.ReadUInt32();
            var sizesArrayOffset = sfarFile.ReadUInt32();
            var maxBlockSize = sfarFile.ReadUInt32();
            var compressionTag = sfarFile.ReadUInt32();
            if (compressionTag != LZMATag)
                throw new Exception("Not LZMA compression for SFAR file");

            List<FileEntry> filesList = new List<FileEntry>();
            for (int i = 0; i < filesCount; i++)
            {
                FileEntry file = new FileEntry();
                file.filenameHash = sfarFile.ReadToBuffer(16);
                file.compressedBlockSizesIndex = sfarFile.ReadInt32();
                file.uncomprSize = sfarFile.ReadUInt32();
                file.uncomprSize |= (long)sfarFile.ReadByte() << 32;
                file.dataOffset = sfarFile.ReadUInt32();
                file.dataOffset |= (long)sfarFile.ReadByte() << 32;
                file.numBlocks = (int)((file.uncomprSize + maxBlockSize - 1) / maxBlockSize);
                filesList.Add(file);
            }
            if (sfarFile.Position != sizesArrayOffset)
                throw new Exception("not expected file position");

            var numBlockSizes = (dataOffset - sizesArrayOffset) / sizeof(ushort);
            ushort[] blockSizes = new ushort[numBlockSizes];
            for (int i = 0; i < numBlockSizes; i++)
            {
                blockSizes[i] = sfarFile.ReadUInt16();
            }
            if (sfarFile.Position != dataOffset)
                throw new Exception("not expected file position");

            int filenamesIndex = -1;
            string[] filenamesArray = new string[filesCount];
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
                    using (FileStream outputFile = new FileStream(outPath + @"\TOC", FileMode.Create, FileAccess.Write))
                    {
                        var filenamesStream = new StreamReader(new MemoryStream(outBuf));
                        while (filenamesStream.EndOfStream == false)
                        {
                            string name = filenamesStream.ReadLine();
                            var hash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant()));
                            for (int l = 0; l < filesCount; l++)
                            {
                                if (StructuralComparisons.StructuralEqualityComparer.Equals(filesList[l].filenameHash, hash))
                                {
                                    filenamesArray[l] = name.Replace('/', '\\');
                                }
                            }
                            outputFile.WriteStringASCII(name + Environment.NewLine);
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
                    sfarFile.JumpTo(filesList[i].dataOffset);
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
        }

        public void pack(string inPath, string outPath, string DLCName, bool noCompress = false)
        {
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
                        file.numBlocks = (int)((fileLen + MaxBlockSize - 1) / MaxBlockSize);
                        for (int k = 0; k < file.numBlocks; k++, curBlockSizesIndex++)
                        {
                            long uncompressedBlockSize = MaxBlockSize;
                            if (k == (file.numBlocks - 1)) // last block
                                uncompressedBlockSize = fileLen - (MaxBlockSize * k);
                            byte[] inBuf = inputFile.ReadToBuffer((int)uncompressedBlockSize);
                            byte[] outBuf = SevenZipHelper.LZMA.Compress(inBuf, noCompress ? 0 : 9);
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

                outputFile.Begin();
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

    class ME2DLC
    {
        public void updateChecksums(GameData gameData)
        {
            StringWriter cacheStream = new StringWriter();
            string user = "user"; // it's EA login
            cacheStream.WriteLine("[Global]");
            cacheStream.WriteLine("LastNucleusID=" + user);
            cacheStream.WriteLine();
            cacheStream.WriteLine("[KeyValuePair]");
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_NRX_1=TRUE"); // shadow broker
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_4=TRUE"); // kasumi
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_5=TRUE"); // overlord
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_6=TRUE"); // arrival
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_7=TRUE"); // alt pack 1
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_8=TRUE"); // aegis pack
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_9=TRUE"); // firepower
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_10=TRUE"); // equalizer
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_11=TRUE"); // alt pack 2
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ME2_PRC_CP_12=TRUE"); // genesis
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.ONLINE_ACCESS=TRUE"); // normandy, zaeed, cerberus, arc, firewalker
            cacheStream.WriteLine(user + ".Entitlement.ME2PCOffers.PC_CERBERUS_NETWORK=TRUE");
            cacheStream.WriteLine(user + ".Entitlement.ME2GenOffers.ME2_PRC_PROMO_C1=TRUE");
            cacheStream.WriteLine(user + ".Numeric.DaysSinceReg=0");
            cacheStream.WriteLine();
            cacheStream.WriteLine("[Hash]");
            foreach (string DLC in Directory.GetDirectories(gameData.DLCData))
            {
                uint DLCId;
                using (FileStream mountFile = new FileStream(Path.Combine(DLC, @"CookedPC\Mount.dlc"), FileMode.Open, FileAccess.Read))
                {
                    mountFile.Seek(12, SeekOrigin.Begin);
                    DLCId = mountFile.ReadUInt32();
                }
                List<string> dlcFiles = Directory.GetFiles(DLC, "*.pcc", SearchOption.AllDirectories).ToList();
                dlcFiles.AddRange(Directory.GetFiles(DLC, "*.ini", SearchOption.AllDirectories));
                dlcFiles = dlcFiles.OrderBy(s => s.ToUpperInvariant()).ToList();
                using (SHA1 sha1 = SHA1.Create())
                {
                    sha1.Initialize();
                    byte[] SHA1Buffer = new byte[0x1000];
                    foreach (string DLCFile in dlcFiles)
                    {
                        // filter localized files
                        if (DLCFile[DLCFile.Length - 8] == '_')
                        {
                            if (!System.Text.RegularExpressions.Regex.IsMatch(DLCFile, ".*_[0-9][0-9][0-9].pcc",
                                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                                continue;
                        }

                        using (FileStream fileToHash = new FileStream(DLCFile, FileMode.Open, FileAccess.Read))
                        {
                            int numBytesToHash = fileToHash.Read(SHA1Buffer, 0, 0x1000);
                            sha1.TransformBlock(SHA1Buffer, 0, numBytesToHash, null, 0);
                        }
                    }
                    sha1.TransformFinalBlock(SHA1Buffer, 0, 0);
                    string hash = BitConverter.ToString(sha1.Hash).Replace("-", "").ToLower();
                    cacheStream.WriteLine(user + ".Mount" + DLCId + "=" + hash);
                }
            }
            byte[] buffer = Encoding.Unicode.GetBytes(cacheStream.GetStringBuilder().ToString());
            encryptCacheDLCFile(gameData, buffer);
        }

        public byte[] decryptCacheDLCFile(GameData gameData)
        {
            byte[] output = null;
            byte[] mac = getNetworkCardAddress();
            byte[] entropy = getEntropyFromMAC(mac);
            if (!File.Exists(gameData.EntitlementCacheIniPath))
                return null;
            using (FileStream cacheDLCFile = new FileStream(gameData.EntitlementCacheIniPath, FileMode.Open, FileAccess.Read))
            {
                int fileLength = (int)new FileInfo(gameData.EntitlementCacheIniPath).Length;
                byte[] buffer = cacheDLCFile.ReadToBuffer(fileLength);
                output = ProtectedData.Unprotect(buffer, entropy, DataProtectionScope.CurrentUser);
                return output;
            }
        }

        public void encryptCacheDLCFile(GameData gameData, byte[] buffer)
        {
            byte[] output = null;
            byte[] mac = getNetworkCardAddress();
            if (mac == null)
                throw new Exception("not network cards");
            byte[] entropy = getEntropyFromMAC(mac);
            if (!Directory.Exists(gameData.ConfigIniPath))
                Directory.CreateDirectory(gameData.ConfigIniPath);
            using (FileStream cacheDLCFile = new FileStream(gameData.EntitlementCacheIniPath, FileMode.Create, FileAccess.Write))
            {
                output = ProtectedData.Protect(buffer, entropy, DataProtectionScope.CurrentUser);
                cacheDLCFile.WriteFromBuffer(output);
            }
        }

        byte[] getNetworkCardAddress()
        {
            NetworkInterface[] cards = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface foundCard = null;
            foreach (NetworkInterface card in cards)
            {
                if (card.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foundCard = card;
                }
            }
            if (foundCard != null)
                return foundCard.GetPhysicalAddress().GetAddressBytes();
            else
                return null;
        }

        byte[] getEntropyFromMAC(byte[] mac)
        {
            byte[] key = new byte[] { 0x65, 0x6f, 0x4a, 0x00, 0x66, 0x61, 0x72, 0x47 };
            for (int i = 0; i < 6; i++)
            {
                key[i] ^= mac[i];
            }
            return key;
        }
    }
}
