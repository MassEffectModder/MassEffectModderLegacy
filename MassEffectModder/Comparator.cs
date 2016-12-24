using AmaroK86.ImageFormat;
using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class Comparator : Form
    {
        MainWindow _main;
        ConfIni _configIni;
        List<List<FoundTexture>> _textures;
        string[] lines;

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
            if (lines[0] != "GroupId;Game;CRC;Ok")
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
            checkedListBox.Items.Add("Ok | GroupId | Game | CRC", false);
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
                bool ok = uint.Parse(str[3]) == 1;
                checkedListBox.Items.Add("   | " + string.Format("{0,7}", groupId) + " |   " + gameId + "  | " + string.Format("0x{0:X8}", crc), ok);
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
            if (e.NewValue == CheckState.Checked)
                str[3] = "1";
            else
                str[3] = "0";
            lines[e.Index] = str[0] + ";" + str[1] + ";" + str[2] + ";" + str[3];
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

            new GameData((MeType)gameId, _configIni);
            List<FoundTexture> foundCrcList = _textures[gameId - 1].FindAll(s => s.crc == crc);
            MatchedTexture nodeTexture = foundCrcList[0].list[0];
            Package package = new Package(GameData.GamePath + nodeTexture.path);
            Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
            byte[] textureData = texture.getTopImageData();
            int width = texture.getTopMipmap().width;
            int height = texture.getTopMipmap().height;
            DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
            pictureBox.Image = DDSImage.ToBitmap(textureData, format, width, height);
        }
    }
}
