/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using AmaroK86.ImageFormat;
using ICSharpCode.SharpZipLib.Zip;
using StreamHelpers;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class TexExplorer : Form
    {
        private FoundTexture ParseLegacyScriptMod(string script, string textureName)
        {
            Regex parts = new Regex("pccs.Add[(]\"[A-z,0-9/,..]*\"");
            Match match = parts.Match(script);
            if (match.Success)
            {
                string packageName;
                if (_gameSelected == MeType.ME3_TYPE)
                    packageName = match.ToString().Split('\"')[1].Split('\\').Last().Split('.')[0];
                else
                    packageName = match.ToString().Split('\"')[1].Split('/').Last().Split('.')[0];
                parts = new Regex("IDs.Add[(][0-9]*[)];");
                match = parts.Match(script);
                if (match.Success)
                {
                    int exportId = int.Parse(match.ToString().Split('(')[1].Split(')')[0]);
                    if (exportId != 0)
                    {
                        for (int i = 0; i < _textures.Count; i++)
                        {
                            if (_textures[i].name == textureName)
                            {
                                for (int l = 0; l < _textures[i].list.Count; l++)
                                {
                                    if (_textures[i].list[l].exportID == exportId)
                                    {
                                        string pkg = _textures[i].list[l].path.Split('\\').Last().Split('.')[0];
                                        if (pkg == packageName)
                                        {
                                            return _textures[i];
                                        }
                                    }
                                }
                            }
                        }
                        // search again but without name match
                        for (int i = 0; i < _textures.Count; i++)
                        {
                            for (int l = 0; l < _textures[i].list.Count; l++)
                            {
                                if (_textures[i].list[l].exportID == exportId)
                                {
                                    string pkg = _textures[i].list[l].path.Split('\\').Last().Split('.')[0];
                                    if (pkg == packageName)
                                    {
                                        return _textures[i];
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return new FoundTexture();
        }

        private bool checkTextureMod(FileStream fs)
        {
            uint tag = fs.ReadUInt32();
            uint version = fs.ReadUInt32();
            if (tag == TextureModTag && version == TextureModVersion)
                return true;

            fs.SeekBegin();
            uint numberOfTextures = fs.ReadUInt32();
            if (numberOfTextures == 0)
                return false;

            try
            {
                for (int i = 0; i < numberOfTextures; i++)
                {
                    int len = fs.ReadInt32();
                    string textureName = fs.ReadStringASCII(len);
                    textureName = textureName.Split(' ').Last();
                    if (textureName == "")
                        return false;
                    len = fs.ReadInt32();
                    string script = fs.ReadStringASCII(len);
                    if (script == "")
                        return false;
                    FoundTexture f = ParseLegacyScriptMod(script, textureName);
                    uint crc = f.crc;
                    textureName = f.name;
                    if (crc == 0)
                    {
                        richTextBoxInfo.Text += "Not able match texture: " + textureName + "\n";
                    }
                    len = fs.ReadInt32();
                    if (len == 0)
                        return false;
                    _mainWindow.updateStatusLabel2("Checking texture " + (i + 1) + " of " + numberOfTextures + " - " + textureName);
                    DDSImage image = new DDSImage(new MemoryStream(fs.ReadToBuffer(len)));
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private void extractTextureMod(string filenameMod, string outDir)
        {
            processTextureMod(filenameMod, -1, true, false, false, outDir);
        }

        private void saveTextureMod(string filenameMod, string outDir)
        {
            processTextureMod(filenameMod, -1, false, false, true, outDir);
        }

        private void previewTextureMod(string filenameMod, int previewIndex)
        {
            processTextureMod(filenameMod, previewIndex, false, false, false, "");
        }

        private void replaceTextureMod(string filenameMod)
        {
            processTextureMod(filenameMod, -1, false, true, false, "");
        }

        private void listTextureMod(string filenameMod)
        {
            processTextureMod(filenameMod, -1, false, false, false, "");
        }

        private void processTextureMod(string filenameMod, int previewIndex, bool extract, bool replace, bool store, string outDir)
        {
            using (FileStream fs = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
            {
                bool legacy = false;

                if (previewIndex == -1 && !store && !extract && !replace && !store)
                {
                    listViewTextures.BeginUpdate();
                }

                if (Path.GetExtension(filenameMod).ToLower() == ".tpf")
                {
                    byte[] tpfXorKey = { 0xA4, 0x3F };
                    ZipEntry entry;
                    byte[] buffer = new byte[10000];
                    byte[] password = {
                            0x73, 0x2A, 0x63, 0x7D, 0x5F, 0x0A, 0xA6, 0xBD,
                            0x7D, 0x65, 0x7E, 0x67, 0x61, 0x2A, 0x7F, 0x7F,
                            0x74, 0x61, 0x67, 0x5B, 0x60, 0x70, 0x45, 0x74,
                            0x5C, 0x22, 0x74, 0x5D, 0x6E, 0x6A, 0x73, 0x41,
                            0x77, 0x6E, 0x46, 0x47, 0x77, 0x49, 0x0C, 0x4B,
                            0x46, 0x6F
                        };

                    byte[] listText;
                    using (FileStream zipList = new FileStream(filenameMod, FileMode.Open, FileAccess.Read))
                    {
                        using (ZipInputStream zipFs = new ZipInputStream(zipList, tpfXorKey))
                        {
                            zipFs.Password = password;
                            while ((entry = zipFs.GetNextEntry()) != null)
                            {
                                if (entry.Name.ToLower() == "texmod.def")
                                    break;
                            }
                            if (entry.Name.ToLower() != "texmod.def")
                                throw new Exception("missing texmod.def in TPF file");

                            listText = new byte[entry.Size];
                            zipFs.Read(listText, 0, listText.Length);
                        }
                    }

                    string[] ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                    FileStream outFile = null;
                    if (store)
                    {
                        outFile = new FileStream(Path.Combine(outDir, Path.GetFileNameWithoutExtension(filenameMod)) + ".mod", FileMode.Create, FileAccess.Write);
                        outFile.WriteUInt32(TextureModTag);
                        outFile.WriteUInt32(TextureModVersion);
                        outFile.WriteUInt32((uint)_gameSelected);
                        outFile.WriteInt32(ddsList.Count());
                    }

                    using (ZipInputStream zipFs = new ZipInputStream(fs, tpfXorKey))
                    {
                        zipFs.Password = password;
                        int index = 0;
                        bool unique = true;
                        while ((entry = zipFs.GetNextEntry()) != null)
                        {
                            uint crc = 0;
                            string filename = Path.GetFileName(entry.Name);
                            foreach (string dds in ddsList)
                            {
                                string ddsFile = dds.Split('|')[1];
                                if (ddsFile.ToLower() != filename.ToLower())
                                    continue;
                                crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                break;
                            }
                            if (crc == 0)
                                continue;

                            string name = "";
                            for (int i = 0; i < _textures.Count; i++)
                            {
                                if (_textures[i].crc == crc)
                                {
                                    if (name != "")
                                    {
                                        unique = false;
                                        break;
                                    }
                                    name = _textures[i].name;
                                }
                            }
                            if (name == "")
                                name = "Unknown" + index;

                            _mainWindow.updateStatusLabel("Processing MOD: " +
                                    Path.GetFileNameWithoutExtension(filenameMod) + " - Texture: " + name);

                            if (store)
                            {
                                outFile.WriteStringASCIINull(name);
                                outFile.WriteUInt32(crc);
                            }
                            if (extract)
                            {
                                filename = name + "-" + string.Format("0x{0:X8}", crc) + ".dds";
                                outFile = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write);
                            }
                            if (previewIndex == index)
                            {
                                MemoryStream outMem = new MemoryStream();
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outMem.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                outMem.SeekBegin();
                                DDSImage image = new DDSImage(outMem);
                                pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                                break;
                            }
                            else if (store)
                            {
                                MemoryStream outMem = new MemoryStream();
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outMem.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                outMem.SeekBegin();
                                byte[] src = outMem.ReadToBuffer(outMem.Length);
                                byte[] dst = ZlibHelper.Zlib.Compress(src);
                                outFile.WriteInt32(src.Length);
                                outFile.WriteInt32(dst.Length);
                                outFile.WriteFromBuffer(dst);
                            }
                            else if (extract)
                            {
                                for (;;)
                                {
                                    int readed = zipFs.Read(buffer, 0, buffer.Length);
                                    if (readed > 0)
                                        outFile.Write(buffer, 0, readed);
                                    else
                                        break;
                                }
                                outFile.Close();
                            }
                            else if (previewIndex == -1)
                            {
                                FoundTexture foundTexture = _textures.Find(s => s.crc == crc && s.name == name);
                                string display;
                                if (foundTexture.crc != 0)
                                {
                                    if (unique)
                                        display = foundTexture.displayName + " (" + foundTexture.packageName + ")";
                                    else
                                        display = foundTexture.displayName + " - Not unique - (" + foundTexture.packageName + ")";
                                }
                                else
                                {
                                    display = name + " (Not matched - CRC: " + string.Format("0x{0:X8}", crc) + ")";
                                }
                                ListViewItem item = new ListViewItem(display);
                                item.Name = index.ToString();
                                listViewTextures.Items.Add(item);
                            }
                            index++;
                        }
                    }
                    if (store)
                        outFile.Close();
                }
                else
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TextureModTag || version != TextureModVersion)
                    {
                        fs.SeekBegin();
                        legacy = true;
                    }
                    else
                    {
                        uint gameType = fs.ReadUInt32();
                        if ((MeType)gameType != _gameSelected)
                        {
                            MessageBox.Show("Mod for different game!");
                            return;
                        }
                    }
                    FileStream outFile = null;
                    if (store)
                    {
                        if (!legacy)
                        {
                            File.Copy(filenameMod, Path.Combine(outDir, Path.GetFileName(filenameMod)));
                            return;
                        }
                        outFile = new FileStream(Path.Combine(outDir, Path.GetFileName(filenameMod)), FileMode.Create, FileAccess.Write);
                        outFile.WriteUInt32(TextureModTag);
                        outFile.WriteUInt32(TextureModVersion);
                        outFile.WriteUInt32((uint)_gameSelected);
                    }
                    int numTextures = fs.ReadInt32();
                    if (store)
                        outFile.WriteInt32(numTextures);
                    for (int i = 0; i < numTextures; i++)
                    {
                        string name;
                        uint crc, size, dstLen = 0, decSize = 0;
                        byte[] dst = null;
                        if (legacy)
                        {
                            int len = fs.ReadInt32();
                            name = fs.ReadStringASCII(len);
                            name = name.Split(' ').Last();
                            len = fs.ReadInt32();
                            string scriptLegacy = fs.ReadStringASCII(len);
                            FoundTexture f = ParseLegacyScriptMod(scriptLegacy, name);
                            crc = f.crc;
                            name = f.name;
                            decSize = dstLen = size = fs.ReadUInt32();
                            dst = fs.ReadToBuffer(size);
                        }
                        else
                        {
                            name = fs.ReadStringASCIINull();
                            crc = fs.ReadUInt32();
                            decSize = fs.ReadUInt32();
                            size = fs.ReadUInt32();
                            byte[] src = fs.ReadToBuffer(size);
                            dst = new byte[decSize];
                            dstLen = ZlibHelper.Zlib.Decompress(src, size, dst);
                        }

                        _mainWindow.updateStatusLabel("Processing MOD " + Path.GetFileNameWithoutExtension(filenameMod) +
                            " - Texture " + (i + 1) + " of " + numTextures + " - " + name);
                        if (extract)
                        {
                            string filename = name + "-" + string.Format("0x{0:X8}", crc) + ".dds";
                            using (FileStream output = new FileStream(Path.Combine(outDir, Path.GetFileName(filename)), FileMode.Create, FileAccess.Write))
                            {
                                output.Write(dst, 0, (int)dstLen);
                            }
                            continue;
                        }
                        if (store)
                        {
                            outFile.WriteStringASCIINull(name);
                            outFile.WriteUInt32(crc);
                            outFile.WriteUInt32(decSize);
                            byte[] src = fs.ReadToBuffer((int)fs.Length);
                            dst = ZlibHelper.Zlib.Compress(src);
                            outFile.WriteInt32(dst.Length);
                            outFile.WriteFromBuffer(dst);
                            continue;
                        }
                        if (previewIndex != -1)
                        {
                            if (i != previewIndex)
                            {
                                continue;
                            }
                            DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                            pictureBoxPreview.Image = image.mipMaps[0].bitmap;
                            break;
                        }
                        else
                        {
                            FoundTexture foundTexture = _textures.Find(s => s.crc == crc && s.name == name);
                            if (foundTexture.crc != 0)
                            {
                                if (replace)
                                {
                                    DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                                    replaceTexture(image, foundTexture.list);
                                }
                                else
                                {
                                    ListViewItem item = new ListViewItem(foundTexture.displayName + " (" + foundTexture.packageName + ")");
                                    item.Name = i.ToString();
                                    listViewTextures.Items.Add(item);
                                }
                            }
                        }
                    }
                    if (store)
                        outFile.Close();
                }
                if (previewIndex == -1 && !store && !extract && !replace && !store)
                {
                    listViewTextures.EndUpdate();
                }
            }
        }

        void packTextureMod(string inDir, string outFile)
        {
            string[] files = Directory.GetFiles(inDir, "*.dds");

            using (FileStream outFs = new FileStream(outFile, FileMode.Create, FileAccess.Write))
            {
                outFs.WriteUInt32(TextureModTag);
                outFs.WriteUInt32(TextureModVersion);
                outFs.WriteUInt32((uint)_gameSelected);
                outFs.WriteInt32(0); // filled later
                int count = 0;
                for (int n = 0; n < files.Count(); n++)
                {
                    string file = files[n];
                    _mainWindow.updateStatusLabel("Processing MOD: " + Path.GetFileNameWithoutExtension(outFile));
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        string crcStr = Path.GetFileNameWithoutExtension(file);
                        if (crcStr.Contains("_0x"))
                        {
                            crcStr = Path.GetFileNameWithoutExtension(file).Split('_').Last().Substring(2, 8); // in case filename contain CRC 
                        }
                        else
                        {
                            crcStr = Path.GetFileNameWithoutExtension(file).Split('-').Last().Substring(2, 8); // in case filename contain CRC 
                        }
                        uint crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                        if (crc == 0)
                        {
                            richTextBoxInfo.Text += "Wrong format of texture filename: " + Path.GetFileName(file) + "\n";
                            continue;
                        }

                        string textureName = "";
                        for (int l = 0; l < _textures.Count; l++)
                        {
                            FoundTexture foundTexture = _textures[l];
                            if (crc != 0 && foundTexture.crc == crc)
                            {
                                textureName = foundTexture.name;
                            }
                        }
                        if (textureName == "")
                        {
                            richTextBoxInfo.Text += "Texture not matched: " + Path.GetFileName(file) + "\n";
                            continue;
                        }
                        _mainWindow.updateStatusLabel2("Texture " + (n + 1) + " of " + files.Count() + ", Name: " + textureName);

                        outFs.WriteStringASCIINull(textureName);
                        outFs.WriteUInt32(crc);
                        byte[] src = fs.ReadToBuffer((int)fs.Length);
                        byte[] dst = ZlibHelper.Zlib.Compress(src);
                        outFs.WriteInt32(src.Length);
                        outFs.WriteInt32(dst.Length);
                        outFs.WriteFromBuffer(dst);
                        count++;
                    }
                }
                outFs.SeekBegin();
                outFs.WriteUInt32(TextureModTag);
                outFs.WriteUInt32(TextureModVersion);
                outFs.WriteUInt32((uint)_gameSelected);
                outFs.WriteInt32(count);
            }
        }
    }
}
