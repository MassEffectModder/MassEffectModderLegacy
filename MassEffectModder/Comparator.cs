/*
 * MassEffectModder
 *
 * Copyright (C) 2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class Comparator : Form
    {
        MainWindow _main;
        ConfIni _configIni;
        List<List<FoundTexture>> _textures;
        List<string> ddsFilesME1;
        List<string> ddsFilesME2;
        List<string> ddsFilesME3;
        string[] lines;
        bool zoom = false;
        int mode;

        public Comparator(MainWindow main)
        {
            InitializeComponent();
            _configIni = main._configIni;
            _main = main;
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog csvFile = new OpenFileDialog())
            {
                csvFile.Title = "Please select CSV file";
                csvFile.Filter = "CSV file | *.csv";
                if (csvFile.ShowDialog() != DialogResult.OK)
                    return;
                lines = File.ReadAllLines(csvFile.FileName);
            }
            if (lines[0] == "GroupId;Game;CRC;Ok")
            {
                mode = 1;
            }
            else if (lines[0] == "GroupId;Game;CRC;Modded;Ok")
            {
                mode = 2;
                if (!Directory.Exists("ME1") || !Directory.Exists("ME2") || !Directory.Exists("ME3"))
                {
                    MessageBox.Show("Missing MEx directories!");
                    return;
                }
                ddsFilesME1 = Directory.GetFiles("ME1", "*.dds", SearchOption.AllDirectories).ToList();
                ddsFilesME2 = Directory.GetFiles("ME2", "*.dds", SearchOption.AllDirectories).ToList();
                ddsFilesME3 = Directory.GetFiles("ME3", "*.dds", SearchOption.AllDirectories).ToList();
                if (ddsFilesME1.Count == 0 || ddsFilesME2.Count == 0 || ddsFilesME3.Count == 0)
                {
                    MessageBox.Show("Missing DDS files in MEx directories!");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Wrong CSV format!");
                return;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name);
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Texture maps not exists!");
                return;
            }

            _textures = new List<List<FoundTexture>>();
            for (int i = 0; i < 3; i++)
            {
                List<FoundTexture> textures = new List<FoundTexture>();
                string mapFile = Path.Combine(path, "me" + (i + 1) + "map.bin");
                if (!loadTexturesMap(mapFile, textures))
                    return;
                _textures.Add(textures);
            }

            for (int i = 0; i < 3; i++)
            {
                new GameData((MeType)i, _configIni);
            }

            checkedListBox.Items.Clear();
            checkedListBox.BeginUpdate();
            if (mode == 1)
                checkedListBox.Items.Add("Ok | GroupId |  Game  | CRC", false);
            else
                checkedListBox.Items.Add("Ok | GroupId |  Game  | CRC        | Modded", false);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] str = lines[i].Split(';');
                string groupId = str[0];
                uint gameId;
                if (str[1] == "ME1")
                    gameId = 1;
                else if (str[1] == "ME2")
                    gameId = 2;
                else if (str[1] == "ME3")
                    gameId = 3;
                else
                    throw new Exception("Not expected game id, must be: ME1, ME2, ME3");
                if (str[2].Contains("0x"))
                {
                    str[2] = str[2].Substring(2, 8);
                }
                uint crc = uint.Parse(str[2], System.Globalization.NumberStyles.HexNumber);
                if (mode == 1)
                {
                    bool ok = uint.Parse(str[3]) == 1;
                    checkedListBox.Items.Add("   | " + string.Format("{0,7}", groupId) + " |   " + "ME" + gameId + "  | " + string.Format("0x{0:X8}", crc), ok);
                }
                else
                {
                    int modded = int.Parse(str[3]);
                    bool ok = uint.Parse(str[4]) == 1;
                    checkedListBox.Items.Add("   | " + string.Format("{0,7}", groupId) + " |   " + "ME" + gameId + "  | " + string.Format("0x{0:X8} | ", crc) + ((modded == 1) ? "Modded" : "Vanilla"), ok);
                }
            }
            checkedListBox.EndUpdate();
        }

        private bool loadTexturesMap(string path, List<FoundTexture> textures)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("File " + path + " not exists!");
                return false;
            }
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != TexExplorer.textureMapBinTag || version != TexExplorer.textureMapBinVersion)
                {
                    MessageBox.Show("Wrong " + path + " file!");
                    return false;
                }

                uint countTexture = fs.ReadUInt32();
                for (int i = 0; i < countTexture; i++)
                {
                    FoundTexture texture = new FoundTexture();
                    texture.name = fs.ReadStringASCIINull();
                    texture.crc = fs.ReadUInt32();
                    texture.packageName = fs.ReadStringASCIINull();
                    uint countPackages = fs.ReadUInt32();
                    texture.list = new List<MatchedTexture>();
                    for (int k = 0; k < countPackages; k++)
                    {
                        MatchedTexture matched = new MatchedTexture();
                        matched.exportID = fs.ReadInt32();
                        matched.path = fs.ReadStringASCIINull();
                        texture.list.Add(matched);
                    }
                    textures.Add(texture);
                }
            }
            return true;
        }

        private void Comparator_FormClosed(object sender, FormClosedEventArgs e)
        {
           _main.enableGameDataMenu(true);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog csvFile = new SaveFileDialog())
            {
                csvFile.Title = "Please select CSV file";
                csvFile.Filter = "CSV file | *.csv";
                if (csvFile.ShowDialog() != DialogResult.OK)
                    return;
                File.WriteAllLines(csvFile.FileName, lines);
            }
        }

        private void checkedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == 0)
                return;

            string[] str = lines[e.Index].Split(';');
            if (mode == 1)
            {
                if (e.NewValue == CheckState.Checked)
                    str[3] = "1";
                else
                    str[3] = "0";
                lines[e.Index] = str[0] + ";" + str[1] + ";" + str[2] + ";" + str[3];
            }
            else
            {
                if (e.NewValue == CheckState.Checked)
                    str[4] = "1";
                else
                    str[4] = "0";
                lines[e.Index] = str[0] + ";" + str[1] + ";" + str[2] + ";" + str[3] + ";" + str[4];
            }
        }

        private void checkedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = checkedListBox.Items.IndexOf(checkedListBox.SelectedItem);
            if (index == 0)
            {
                pictureBox.Image = null;
                return;
            }

            string[] str = lines[index].Split(';');
            string groupId = str[0];
            int gameId;
            if (str[1] == "ME1")
                gameId = 1;
            else if (str[1] == "ME2")
                gameId = 2;
            else if (str[1] == "ME3")
                gameId = 3;
            else
                throw new Exception("Not expected game id, must be: ME1, ME2, ME3");
            if (str[2].Contains("0x"))
            {
                str[2] = str[2].Substring(2, 8);
            }
            uint crc = uint.Parse(str[2], System.Globalization.NumberStyles.HexNumber);

            int modded = 0;
            if (mode == 2)
            {
                modded = int.Parse(str[3]);
            }

            if (modded == 0)
            {
                new GameData((MeType)gameId, _configIni);
                List<FoundTexture> foundCrcList = _textures[gameId - 1].FindAll(s => s.crc == crc);
                if (foundCrcList.Count == 0)
                {
                    MessageBox.Show("Texture not exist in game data");
                    return;
                }
                MatchedTexture nodeTexture = foundCrcList[0].list[0];
                Package package = new Package(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                byte[] textureData = texture.getTopImageData();
                int width = texture.getTopMipmap().width;
                int height = texture.getTopMipmap().height;
                textBoxResolution.Text = width + " x " + height;
                DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
                pictureBox.Image = DDSImage.ToBitmap(textureData, format, width, height);
            }
            else
            {
                string ddsFileName;
                if (gameId == 1)
                    ddsFileName = ddsFilesME1.Find(s => s.Contains(string.Format("0x{0:X8}", crc)));
                else if (gameId == 2)
                    ddsFileName = ddsFilesME2.Find(s => s.Contains(string.Format("0x{0:X8}", crc)));
                else
                    ddsFileName = ddsFilesME3.Find(s => s.Contains(string.Format("0x{0:X8}", crc)));
                if (ddsFileName != null)
                {
                    DDSImage image = new DDSImage(ddsFileName, true);
                    int width = image.mipMaps[0].width;
                    int height = image.mipMaps[0].height;
                    textBoxResolution.Text = width + " x " + height;
                    pictureBox.Image = new DDSImage(ddsFileName, true).mipMaps[0].bitmap;
                }
                else
                {
                    textBoxResolution.Text = "Missing DDS file!";
                    pictureBox.Image = null;
                }
            }

        }

        private void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox.Image == null)
                return;

            if (zoom)
            {
                pictureBox.Width = 512;
                pictureBox.Height = 512;
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                zoom = false;
            }
            else
            {
                pictureBox.Width = Math.Max(512, pictureBox.Image.Width);
                pictureBox.Height = Math.Max(512, pictureBox.Image.Height);
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                zoom = true;
            }
        }
    }
}
