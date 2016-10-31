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

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using StreamHelpers;
using AmaroK86.ImageFormat;
using System.Linq;

namespace MassEffectModder
{
    public enum MeType
    {
        ME1_TYPE = 1,
        ME2_TYPE,
        ME3_TYPE
    }

    struct TFCTexture
    {
        public byte[] guid;
        public string name;
    }

    public struct MatchedTexture
    {
        public string path;
        public int exportID;
    }

    public struct FoundTexture
    {
        public string name;
        public uint crc;
        public uint mipmapOffset;
        public string packageName;
        public string displayName;
        public List<MatchedTexture> list;
    }

    public partial class TexExplorer : Form
    {
        const uint textureMapBinTag = 0x5054454D;
        const uint textureMapBinVersion = 1;
        const string TempModFileName = "TextureMod.tmp";
        const uint TextureModTag = 0x444F4D54;
        const uint TextureModVersion = 1;
        const uint TextureModHeaderLength = 16;

        MeType _gameSelected;
        MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<FoundTexture> _textures;
        bool previewShow = true;
        bool moddingEnable = false;
        FileStream fileStreamMod;
        uint numberOfTexturesMod;
        CachePackageMgr cachePackageMgr;

        public class PackageTreeNode : TreeNode
        {
            public List<FoundTexture> textures;

            public PackageTreeNode(string name)
                : base()
            {
                Name = Text = name;
                textures = new List<FoundTexture>();
            }
        };
        List<PackageTreeNode> nodeList;

        public TexExplorer(MainWindow main, MeType gameType)
        {
            InitializeComponent();
            _mainWindow = main;
            _gameSelected = gameType;
            _configIni = main._configIni;
            gameData = new GameData(_gameSelected, _configIni);
            cachePackageMgr = new CachePackageMgr(main);
        }

        public void EnableMenuOptions(bool enable)
        {
            MODsToolStripMenuItem.Enabled = enable;
            searchToolStripMenuItem.Enabled = enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = enable;
            treeViewPackages.Enabled = enable;
            listViewResults.Enabled = enable;
            listViewTextures.Enabled = enable;
            listViewMods.Enabled = enable;
            Application.DoEvents();
        }

        public void Run()
        {
            _mainWindow.updateStatusLabel("");
            _mainWindow.updateStatusLabel2("");
            EnableMenuOptions(false);
            eNDModdingToolStripMenuItem.Enabled = false;
            clearMODsToolStripMenuItem.Enabled = false;
            listViewResults.Hide();
            listViewMods.Hide();
            richTextBoxInfo.Hide();
            listViewTextures.Clear();
            richTextBoxInfo.Clear();

            if (_gameSelected == MeType.ME1_TYPE)
                _mainWindow.VerifyME1Exe(gameData);

            if (!_mainWindow.GetPackages(gameData))
            {
                Close();
                return;
            }
            else
            {
                _mainWindow.updateStatusLabel("");
                _mainWindow.updateStatusLabel("Preparing tree...");
                PrepareListOfTextures();
                _mainWindow.updateStatusLabel("Done.");
                _mainWindow.updateStatusLabel("");
            }

            PrepareTreeList();

            EnableMenuOptions(true);
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (GameData.packageFiles != null)
                GameData.packageFiles.Clear();
            cachePackageMgr.CloseAllWithoutSave();
            _mainWindow.enableGameDataMenu(true);
        }

        private void treeViewPackages_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageIndex = 0;
        }

        private void treeViewPackages_AfterExpand(object sender, TreeViewEventArgs e)
        {
            e.Node.ImageIndex = 1;
        }

        private void updateListViewTextures(PackageTreeNode node)
        {
            listViewTextures.BeginUpdate();
            listViewTextures.Clear();
            listViewTextures.Sort();
            for (int i = 0; i < node.textures.Count; i++)
            {
                FoundTexture texture = node.textures[i];
                ListViewItem item = new ListViewItem();
                item.Name = i.ToString();
                item.Text = texture.displayName;
                listViewTextures.Items.Add(item);
            }
            listViewTextures.EndUpdate();
            listViewTextures.Refresh();
        }

        private void treeViewPackages_AfterSelect(object sender, TreeViewEventArgs e)
        {
            updateListViewTextures((PackageTreeNode)e.Node);
            updateViewFromListView();
        }

        private void clearPreview()
        {
            richTextBoxInfo.Clear();
            if (pictureBoxPreview.Image != null)
            {
                pictureBoxPreview.Image.Dispose();
                pictureBoxPreview.Image = null;
            }
        }

        private void updateViewFromListView()
        {
            if (listViewTextures.SelectedItems.Count == 0)
            {
                clearPreview();
                return;
            }
            if (listViewMods.Items.Count > 0)
            {
                previewShow = true;
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                previewTextureMod(listViewMods.SelectedItems[0].Name, index);
                _mainWindow.updateStatusLabel("Done.");
                pictureBoxPreview.Show();
                richTextBoxInfo.Hide();
            }
            else
            {
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                MatchedTexture nodeTexture = node.textures[index].list[0];
                Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                if (previewShow)
                {
                    byte[] textureData = texture.getImageData();
                    int width = texture.getTopMipmap().width;
                    int height = texture.getTopMipmap().height;
                    DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
                    pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, format, width, height);
                    pictureBoxPreview.Show();
                    richTextBoxInfo.Hide();
                }
                else
                {
                    string text = "";

                    text += "Texture name:  " + node.textures[index].name + "\n";
                    text += "Texture original CRC:  " + string.Format("0x{0:X8}", node.textures[index].crc) + "\n";
                    text += "Node name:     " + node.textures[index].displayName + "\n";
                    text += "Package name:  " + node.textures[index].packageName + "\n";
                    text += "Packages:\n";
                    for (int l = 0; l < node.textures[index].list.Count; l++)
                    {
                        text += "  Export Id:     " + node.textures[index].list[l].exportID + "\n";
                        text += "  Package path:  " + node.textures[index].list[l].path + "\n";
                    }
                    text += "Texture properties:\n";
                    for (int l = 0; l < texture.properties.texPropertyList.Count; l++)
                    {
                        text += texture.properties.getDisplayString(l);
                    }
                    for (int l = 0; l < texture.mipMapsList.Count; l++)
                    {
                        text += "MipMap: " + l + ", " + texture.mipMapsList[l].width + "x" + texture.mipMapsList[l].height + "\n";
                        text += "  StorageType: " + texture.mipMapsList[l].storageType + "\n";
                        text += "  DataOffset:  " + (int)texture.mipMapsList[l].dataOffset + "\n";
                        text += "  CompSize:    " + texture.mipMapsList[l].compressedSize + "\n";
                        text += "  UnCompSize:  " + texture.mipMapsList[l].uncompressedSize + "\n";
                    }
                    richTextBoxInfo.Text = text;
                    pictureBoxPreview.Hide();
                    richTextBoxInfo.Show();
                }
            }
        }

        private void searchTexture(string name, uint crc)
        {
            listViewResults.Clear();

            for (int l = 0; l < _textures.Count; l++)
            {
                FoundTexture foundTexture = _textures[l];
                if ((name != null && foundTexture.name == name) ||
                    (crc != 0 && foundTexture.crc == crc))
                {
                    ListViewItem item = new ListViewItem(foundTexture.displayName + " (" + foundTexture.packageName + ")");
                    item.Name = l.ToString();
                    listViewResults.Items.Add(item);
                }
            }
            if (listViewResults.Items.Count > 1)
            {
                listViewResults.Show();
                listViewTextures.Clear();
                clearPreview();
            }
            if (listViewResults.Items.Count == 1)
            {
                listViewTextures.Focus();
                selectFoundTexture(listViewResults.Items[0]);
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxInfo.Show();
            pictureBoxPreview.Hide();
            previewShow = false;
            updateViewFromListView();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxInfo.Hide();
            pictureBoxPreview.Show();
            previewShow = true;
            updateViewFromListView();
        }

        private void listViewTextures_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateViewFromListView();
        }

        private void listViewTextures_DoubleClick(object sender, EventArgs e)
        {
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void listViewTextures_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r')
                return;
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void selectFoundTexture(ListViewItem item)
        {
            int pos1 = item.Text.IndexOf('(');
            int pos2 = item.Text.IndexOf(')');
            string packageName = item.Text.Substring(pos1 + 1, pos2 - pos1 - 1);
            listViewResults.Hide();
            for (int l = 0; l < treeViewPackages.Nodes[0].Nodes.Count; l++)
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.Nodes[0].Nodes[l];
                if (node.Name == packageName)
                {
                    treeViewPackages.SelectedNode = node;
                    updateListViewTextures(node);
                    for (int i = 0; i < node.textures.Count; i++)
                    {
                        if (node.textures[i].name == item.Text.Split(' ')[0])
                        {
                            listViewTextures.FocusedItem = listViewTextures.Items[i];
                            listViewTextures.Items[i].Selected = true;
                            return;
                        }
                    }
                }
            }
        }

        private void listViewResults_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewResults.Items.Count == 0)
                return;
            selectFoundTexture(listViewResults.SelectedItems[0]);
        }

        private void replaceTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture();
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void switchModMode(bool enable)
        {
            sTARTModdingToolStripMenuItem.Enabled = !enable;
            eNDModdingToolStripMenuItem.Enabled = enable;
            loadMODsToolStripMenuItem.Enabled = !enable;
            clearMODsToolStripMenuItem.Enabled = false;
            packMODToolStripMenuItem.Enabled = !enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void sTARTModdingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switchModMode(true);
            numberOfTexturesMod = 0;
            moddingEnable = true;
            fileStreamMod = File.Create(TempModFileName);
            fileStreamMod.Seek(TextureModHeaderLength, SeekOrigin.Begin);
        }

        private void eNDModdingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileStreamMod.SeekBegin();
            fileStreamMod.WriteUInt32(TextureModTag);
            fileStreamMod.WriteUInt32(TextureModVersion);
            fileStreamMod.WriteUInt32((uint)_gameSelected);
            fileStreamMod.WriteUInt32(numberOfTexturesMod);
            fileStreamMod.Close();

            if (numberOfTexturesMod > 0)
            {
                using (SaveFileDialog modFile = new SaveFileDialog())
                {
                    modFile.Title = "Please select new name for Mod file";
                    modFile.Filter = "MOD file|*.mod";
                    if (modFile.ShowDialog() == DialogResult.OK)
                    {
                        File.Move(TempModFileName, modFile.FileName);
                    }
                }
            }
            if (File.Exists(TempModFileName))
                File.Delete(TempModFileName);

            cachePackageMgr.CloseAllWithSave();

            moddingEnable = false;
            switchModMode(false);
        }

        private void switchModsMode(bool enable)
        {
            sTARTModdingToolStripMenuItem.Enabled = !enable;
            eNDModdingToolStripMenuItem.Enabled = !enable;
            loadMODsToolStripMenuItem.Enabled = enable;
            clearMODsToolStripMenuItem.Enabled = enable;
            packMODToolStripMenuItem.Enabled = true;
            searchToolStripMenuItem.Enabled = !enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void loadMODsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select Mod file";
                modFile.Filter = "MOD file | *.mod; *.tpf";
                modFile.Multiselect = true;
                modFile.InitialDirectory = gameData.lastLoadMODPath;
                if (modFile.ShowDialog() != DialogResult.OK)
                    return;
                gameData.lastLoadMODPath = Path.GetDirectoryName(modFile.FileNames[0]);

                EnableMenuOptions(false);

                listViewMods.Show();

                listViewTextures.Clear();
                clearPreview();

                string[] files = modFile.FileNames;
                foreach (string file in files)
                {
                    bool legacy = false;
                    bool tpf = false;
                    _mainWindow.updateStatusLabel("MOD: " + Path.GetFileNameWithoutExtension(file) + " loading...");
                    if (Path.GetExtension(file).ToLower() == ".tpf")
                    {
                        tpf = true;
                    }
                    else
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open))
                        {
                            uint tag = fs.ReadUInt32();
                            uint version = fs.ReadUInt32();
                            if (tag != TextureModTag || version != TextureModVersion)
                            {
                                fs.SeekBegin();
                                richTextBoxInfo.Text = "";
                                if (!checkTextureMod(fs))
                                {
                                    MessageBox.Show("File " + file + " is not MOD, omitting...");
                                    continue;
                                }
                                if (richTextBoxInfo.Text != "")
                                {
                                    richTextBoxInfo.Show();
                                    pictureBoxPreview.Hide();
                                    MessageBox.Show("There were some errors while process.");
                                }
                                legacy = true;
                            }
                        }
                    }
                    string desc;
                    if (legacy)
                        desc = Path.GetFileNameWithoutExtension(file) + " (Legacy MOD)";
                    else if (tpf)
                        desc = Path.GetFileNameWithoutExtension(file) + " (TPF - View Only)";
                    else
                        desc = Path.GetFileNameWithoutExtension(file);
                    ListViewItem item = new ListViewItem(desc);
                    item.Name = file;
                    listViewMods.Items.Add(item);
                }
            }
            _mainWindow.updateStatusLabel("MODs loaded.");
            _mainWindow.updateStatusLabel2("");
            EnableMenuOptions(true);
            if (listViewMods.Items.Count == 0)
                clearMODsView();
            switchModsMode(true);
        }

        private void clearMODsView()
        {
            listViewMods.Items.Clear();
            listViewMods.Hide();

            listViewTextures.Clear();
            clearPreview();

            switchModMode(false);
            EnableMenuOptions(true);
            clearMODsToolStripMenuItem.Enabled = false;
        }

        private void clearMODsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            clearMODsView();
        }

        private void applyModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);
            foreach (ListViewItem item in listViewMods.SelectedItems)
            {
                replaceTextureMod(item.Name);
                _mainWindow.updateStatusLabel("MOD: " + item.Text + " applying...");
                listViewMods.Items.Remove(item);
            }
            _mainWindow.updateStatusLabel("");
            cachePackageMgr.CloseAllWithSave();
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MODs applied.");
            _mainWindow.updateStatusLabel2("");
            if (listViewMods.Items.Count == 0)
                clearMODsView();
        }

        private void deleteModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);
            foreach (ListViewItem item in listViewMods.SelectedItems)
                listViewMods.Items.Remove(item);

            listViewTextures.Clear();
            clearPreview();

            EnableMenuOptions(true);
            if (listViewMods.Items.Count == 0)
                clearMODsView();
        }

        private void listViewMods_SelectedIndexChanged(object sender, EventArgs e)
        {
            listViewTextures.Clear();
            clearPreview();

            if (listViewMods.SelectedItems.Count != 1)
                return;

            listTextureMod(listViewMods.SelectedItems[0].Name);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void saveModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.SelectedPath = gameData.lastSaveMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    gameData.lastSaveMODPath = modFile.SelectedPath;
                    foreach (ListViewItem item in listViewMods.SelectedItems)
                    {
                        _mainWindow.updateStatusLabel("MOD: " + item.Text + "saving...");
                        saveTextureMod(item.Name, modFile.SelectedPath);
                        _mainWindow.updateStatusLabel2("");
                    }
                }
            }
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MODs saved.");
            _mainWindow.updateStatusLabel2("");
        }

        private void extractModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
                return;

            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.SelectedPath = gameData.lastExtractMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    gameData.lastExtractMODPath = modFile.SelectedPath;
                    foreach (ListViewItem item in listViewMods.SelectedItems)
                    {
                        string outDir = Path.Combine(modFile.SelectedPath, Path.GetFileNameWithoutExtension(item.Name));
                        Directory.CreateDirectory(outDir);
                        extractTextureMod(item.Name, outDir);
                        _mainWindow.updateStatusLabel("MOD: " + item.Text + "extracting...");
                        _mainWindow.updateStatusLabel2("");
                    }
                }
            }
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MODs extracted.");
            _mainWindow.updateStatusLabel2("");
        }

        private void packMODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.SelectedPath = gameData.lastCreateMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    gameData.lastCreateMODPath = modFile.SelectedPath;
                    _mainWindow.updateStatusLabel("MOD packing...");
                    _mainWindow.updateStatusLabel2("");
                    richTextBoxInfo.Text = "";
                    packTextureMod(modFile.SelectedPath, Path.Combine(Path.GetDirectoryName(modFile.SelectedPath), Path.GetFileName(modFile.SelectedPath)) + ".mod");
                    if (richTextBoxInfo.Text != "")
                    {
                        richTextBoxInfo.Show();
                        pictureBoxPreview.Hide();
                        MessageBox.Show("There were some errors while process.");
                    }
                }
            }

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("MOD packed.");
            _mainWindow.updateStatusLabel2("");
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture name or CRC", "", "", 0, 0);
            if (string.IsNullOrEmpty(name))
                return;

            string crcStr = name;
            if (crcStr.Contains("_0x"))
            {
                crcStr = name.Split('_').Last().Substring(2, 8); // in case filename contain CRC 
            }
            else
            {
                crcStr = name.Split('-').Last(); // in case filename contain CRC
                if (crcStr == "")
                {
                    crcStr = name;
                }
            }

            uint crc = 0;
            try
            {
                if (crcStr.Substring(0, 2).ToLower() == "0x")
                    crcStr = crcStr.Substring(2, 8);
                else
                    crcStr = crcStr.Substring(0, 8);
                crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                searchTexture("", crc);
            }
            catch
            {
                name = name.Split('.')[0]; // in case filename
                searchTexture(name, 0);
            }
        }
    }
}
