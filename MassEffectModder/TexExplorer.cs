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
        const uint textureMapBinTag = 0x4D455450;
        const uint textureMapBinVersion = 1;

        MeType _gameSelected;
        MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<string> _packageFiles;
        List<FoundTexture> _textures;
        bool previewShow = true;

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
        }

        public void EnableMenuOptions(bool enable)
        {
            sTARTModdingToolStripMenuItem.Enabled = enable;
            eNDModdingToolStripMenuItem.Enabled = enable;
            searchToolStripMenuItem.Enabled = enable;
            removeEmptyMipmapsToolStripMenuItem.Enabled = enable;
            treeViewPackages.Enabled = enable;
            listViewResults.Enabled = enable;
            listViewTextures.Enabled = enable;
        }

        public void Run()
        {
            _mainWindow.updateStatusLabel("");
            EnableMenuOptions(false);
            listViewResults.Hide();
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
                _packageFiles = GameData.packageFiles;
                _textures = new List<FoundTexture>();
                string filename = "me" + (int)_gameSelected + "map.bin";
                if (File.Exists(filename))
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != textureMapBinTag || version != textureMapBinVersion)
                        {
                            MessageBox.Show("Abort! Wrong " + filename + " file!");
                            _mainWindow.updateStatusLabel("");
                            Close();
                            return;
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
                            _textures.Add(texture);
                        }
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show("Replacing textures and creating mods require textures mapping.\n" +
                        "It's one time only process but can be very long.\n\n" +
                        "IMPORTANT! Make sure game data is not modified.\n\n" +
                        "Are you sure to proceed?", "Textures mapping", MessageBoxButtons.YesNo);
                    if (result == DialogResult.No)
                    {
                        Close();
                        return;
                    }

                    if (_gameSelected == MeType.ME1_TYPE)
                        sortPackagesME1();
                    for (int i = 0; i < _packageFiles.Count; i++)
                    {
                        _mainWindow.updateStatusLabel("Find textures in package " + (i + 1) + " of " + _packageFiles.Count);
                        FindTextures(_packageFiles[i]);
                    }

                    using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        fs.WriteUInt32(textureMapBinTag);
                        fs.WriteUInt32(textureMapBinVersion);
                        fs.WriteInt32(_textures.Count);
                        for (int i = 0; i < _textures.Count; i++)
                        {
                            fs.WriteStringASCIINull(_textures[i].name);
                            fs.WriteUInt32(_textures[i].crc);
                            fs.WriteStringASCIINull(_textures[i].packageName);
                            fs.WriteInt32(_textures[i].list.Count);
                            for (int k = 0; k < _textures[i].list.Count; k++)
                            {
                                fs.WriteInt32(_textures[i].list[k].exportID);
                                fs.WriteStringASCIINull(_textures[i].list[k].path);
                            }
                        }
                    }
                    _mainWindow.updateStatusLabel("Done.");
                }
            }

            nodeList = new List<PackageTreeNode>();
            PackageTreeNode rootNode = new PackageTreeNode("All Packages");
            for (int l = 0; l < _textures.Count; l++)
            {
                string displayName = _textures[l].name;
                FoundTexture texture = _textures[l];
                texture.displayName = displayName;
                _textures[l] = texture;
                bool found = false;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].Name == _textures[l].packageName)
                    {
                        for (int j = 0; j < nodeList[i].textures.Count; j++)
                        {
                            if (nodeList[i].textures[j].name == _textures[l].name)
                                displayName = nodeList[i].textures[j].name + "!" + nodeList[i].textures.Count;
                        }
                        texture.displayName = displayName;
                        _textures[l] = texture;
                        nodeList[i].textures.Add(_textures[l]);
                        found = true;
                    }
                }
                if (!found)
                {
                    PackageTreeNode treeNode = new PackageTreeNode(_textures[l].packageName);
                    treeNode.textures.Add(_textures[l]);
                    rootNode.Nodes.Add(treeNode);
                    nodeList.Add(treeNode);
                }
            }

            treeViewPackages.Nodes.Clear();
            treeViewPackages.BeginUpdate();
            treeViewPackages.Sort();
            treeViewPackages.Nodes.Add(rootNode);
            treeViewPackages.EndUpdate();
            treeViewPackages.Nodes[0].Expand();

            EnableMenuOptions(true);
            eNDModdingToolStripMenuItem.Enabled = false;
        }

        void sortPackagesME1()
        {
            _mainWindow.updateStatusLabel("Sorting packages...");
            List<string> sortedList = new List<string>();
            List<string> restList = new List<string>();
            for (int i = 0; i < _packageFiles.Count; i++)
            {
                var package = new Package(_packageFiles[i], true);
                if (!package.compressed)
                    sortedList.Add(_packageFiles[i]);
                else
                    restList.Add(_packageFiles[i]);
                _mainWindow.updateStatusLabel("Sorting packages... " + (i + 1) + " of " + _packageFiles.Count);
            }
            sortedList.AddRange(restList);
            _packageFiles = sortedList;
            _mainWindow.updateStatusLabel("Done.");
        }

        public void FindTextures(string packagePath)
        {
            var package = new Package(packagePath);
            for (int i = 0; i < package.exportsTable.Count; i++)
            {
                int id = package.getClassNameId(package.exportsTable[i].classId);
                if (id == package.nameIdTexture2D ||
                    id == package.nameIdLightMapTexture2D ||
                    id == package.nameIdShadowMapTexture2D ||
                    id == package.nameIdTextureFlipBook)
                {
                    Texture texture = new Texture(package, i, package.getExportData(i));
                    if (!texture.hasImageData())
                        continue;

                    Texture.MipMap mipmap = texture.getTopMipmap();
                    string name = package.exportsTable[i].objectName;
                    MatchedTexture matchTexture = new MatchedTexture();
                    matchTexture.exportID = i;
                    matchTexture.path = GameData.RelativeGameData(packagePath);

                    if (_gameSelected == MeType.ME1_TYPE)
                    {
                        if (mipmap.storageType == Texture.StorageTypes.pccUnc ||
                            mipmap.storageType == Texture.StorageTypes.pccCpr)
                        {
                            uint crc = texture.getCrcMipmap();
                            FoundTexture foundTexName = _textures.Find(s => s.crc == crc);
                            if (foundTexName.name != null &&
                                package.compressed)
                            {
                                foundTexName.list.Add(matchTexture);
                            }
                            else
                            {
                                FoundTexture foundTex = new FoundTexture();
                                foundTex.list = new List<MatchedTexture>();
                                foundTex.list.Add(matchTexture);
                                foundTex.name = name;
                                foundTex.crc = crc;
                                foundTex.packageName = texture.packageName;
                                foundTex.mipmapOffset = mipmap.dataOffset;
                                _textures.Add(foundTex);
                            }
                        }
                        else
                        {
                            FoundTexture foundTexName = _textures.Find(s => s.name == name && s.packageName == texture.packageName);
                            foundTexName.list.Add(matchTexture);
                        }
                    }
                    else
                    {
                        uint crc = texture.getCrcMipmap();
                        FoundTexture foundTexName = _textures.Find(s => s.crc == crc);
                        if (foundTexName.crc != 0)
                        {
                            foundTexName.list.Add(matchTexture);
                        }
                        else
                        {
                            FoundTexture foundTex = new FoundTexture();
                            foundTex.list = new List<MatchedTexture>();
                            foundTex.list.Add(matchTexture);
                            foundTex.name = name;
                            foundTex.crc = crc;
                            foundTex.packageName = texture.packageName;
                            foundTex.mipmapOffset = mipmap.dataOffset;
                            _textures.Add(foundTex);
                        }
                    }
                }
            }
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            _packageFiles.Clear();
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

        private void updateViewFromListView()
        {
            if (listViewTextures.SelectedItems.Count == 0)
            {
                richTextBoxInfo.Clear();
                if (pictureBoxPreview.Image != null)
                {
                    pictureBoxPreview.Image.Dispose();
                    pictureBoxPreview.Image = null;
                }
                return;
            }

            int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
            PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
            MatchedTexture nodeTexture = node.textures[index].list[0];
            Package package = new Package(GameData.GamePath + nodeTexture.path);
            Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
            if (previewShow)
            {
                byte[] textureData = texture.getImageData();
                string format = texture.properties.getProperty("Format").valueName;
                int width = texture.getTopMipmap().width;
                int height = texture.getTopMipmap().height;
                switch (format)
                {
                    case "PF_DXT1":
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, DDSFormat.DXT1, width, height);
                        break;
                    case "PF_DXT5":
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, DDSFormat.DXT5, width, height);
                        break;
                    case "PF_NormalMap_HQ":
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, DDSFormat.ATI2, width, height);
                        break;
                    case "PF_V8U8":
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, DDSFormat.V8U8, width, height);
                        break;
                    case "PF_A8R8G8B8":
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, DDSFormat.ARGB, width, height);
                        break;
                    case "PF_G8":
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, DDSFormat.G8, width, height);
                        break;
                    default:
                        throw new Exception("");
                }
            }
            else
            {
                richTextBoxInfo.Text += "Texture name:  " + node.textures[index].name + "\n";
                richTextBoxInfo.Text += "Node name:     " + node.textures[index].displayName + "\n";
                richTextBoxInfo.Text += "Package name:  " + node.textures[index].packageName + "\n";
                richTextBoxInfo.Text += "Packages:\n";
                for (int l = 0; l < node.textures[index].list.Count; l++)
                {
                    richTextBoxInfo.Text += "  Export Id:     " + node.textures[index].list[l].exportID + "\n";
                    richTextBoxInfo.Text += "  Package path:  " + node.textures[index].list[l].path + "\n";
                }
                richTextBoxInfo.Text += "Texture properties:\n";
                for (int l = 0; l < texture.properties.texPropertyList.Count; l++)
                {
                    richTextBoxInfo.Text += texture.properties.getDisplayString(l);
                }
                for (int l = 0; l < texture.mipMapsList.Count; l++)
                {
                    richTextBoxInfo.Text += "MipMap:        " + l + "\n";
                    richTextBoxInfo.Text += "  StorageType: " + texture.mipMapsList[l].storageType + "\n";
                    richTextBoxInfo.Text += "  CompSize:    " + texture.mipMapsList[l].compressedSize + "\n";
                    richTextBoxInfo.Text += "  UnCompSize:  " + texture.mipMapsList[l].uncompressedSize + "\n";
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
            if (listViewResults.Items.Count > 0)
            {
                listViewResults.BringToFront();
                listViewResults.Show();
                listViewTextures.Clear();
                richTextBoxInfo.Clear();
                if (pictureBoxPreview.Image != null)
                {
                    pictureBoxPreview.Image.Dispose();
                    pictureBoxPreview.Image = null;
                }
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxInfo.BringToFront();
            previewShow = false;
            updateViewFromListView();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBoxPreview.BringToFront();
            previewShow = true;
            updateViewFromListView();
        }

        private void listViewTextures_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateViewFromListView();
        }
        private void byNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture name", "", "", 0, 0);
            if (string.IsNullOrEmpty(name))
                return;

            name = name.Split('.')[0]; // in case filename
            searchTexture(name, 0);
        }

        private void byCRCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string crc = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture CRC", "", "", 0, 0);
            if (string.IsNullOrEmpty(crc))
                return;

            searchTexture(null, uint.Parse(crc, System.Globalization.NumberStyles.AllowHexSpecifier));
        }

        private void listViewResults_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listViewResults.Items.Count == 0)
                return;
            ListViewItem item = listViewResults.SelectedItems[0];
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
                            updateViewFromListView();
                            return;
                        }
                    }
                }
            }
        }

        private void replaceTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewTextures.SelectedItems.Count == 0)
                return;

            OpenFileDialog selectDDS = new OpenFileDialog();
            selectDDS.Title = "Please select DDS file";
            selectDDS.Filter = "DDS file|*.dds";
            if (selectDDS.ShowDialog() != DialogResult.OK)
                return;

            bool startMod = sTARTModdingToolStripMenuItem.Enabled;
            bool endMod = eNDModdingToolStripMenuItem.Enabled;
            EnableMenuOptions(false);

            DDSImage image = new DDSImage(selectDDS.FileName);
            int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
            PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;

            for (int i = 0; i < node.textures[index].list.Count; i++)
            {
                MatchedTexture nodeTexture = node.textures[index].list[i];
                Package package = new Package(GameData.GamePath + nodeTexture.path);
                Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                do
                {
                    texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));

                List<Texture.MipMap> mipmaps = new List<Texture.MipMap>();

                for (int m = 0; m < image.mipMaps.Count(); m++)
                {
                    Texture.MipMap mipmap = new Texture.MipMap();
                    mipmap.width = image.mipMaps[m].width;
                    mipmap.height = image.mipMaps[m].height;
                    mipmap.storageType = texture.mipMapsList[m].storageType;
                    mipmaps.Add(mipmap);
                    if (texture.mipMapsList.Count() == 1) 
                        break;
                }
            }

            EnableMenuOptions(true);
            sTARTModdingToolStripMenuItem.Enabled = startMod;
            eNDModdingToolStripMenuItem.Enabled = endMod;
        }

        private void removeEmptyMipmapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure to proceed?", "Remove empty mipmaps", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
                return;

            EnableMenuOptions(false);

            _mainWindow.GetPackages(gameData);
            if (_gameSelected == MeType.ME1_TYPE)
                sortPackagesME1();

            for (int i = 0; i < _packageFiles.Count; i++)
            {
                bool modified = false;
                _mainWindow.updateStatusLabel("Remove empty mipmaps, package " + (i + 1) + " of " + _packageFiles.Count);
                var package = new Package(_packageFiles[i]);
                for (int l = 0; l < package.exportsTable.Count; l++)
                {
                    int id = package.getClassNameId(package.exportsTable[l].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        Texture texture = new Texture(package, l, package.getExportData(l));
                        if (!texture.hasImageData() ||
                            !texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                        {
                            continue;
                        }
                        do
                        {
                            texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                        } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));
                        texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                        texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                        texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                        if (_gameSelected == MeType.ME1_TYPE && package.compressed)
                        {
                            if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extCpr) ||
                                texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.extUnc))
                            {
                                string textureName = package.exportsTable[l].objectName;
                                FoundTexture foundTexName = _textures.Find(s => s.name == textureName && s.packageName == texture.packageName);
                                Package refPkg = new Package(GameData.GamePath + foundTexName.list[0].path);
                                int refExportId = foundTexName.list[0].exportID;
                                byte[] refData = refPkg.getExportData(refExportId);
                                Texture refTexture = new Texture(refPkg, refExportId, refData);

                                if (texture.mipMapsList.Count != refTexture.mipMapsList.Count)
                                    throw new Exception("");
                                for (int t = 0; t < texture.mipMapsList.Count; t++)
                                {
                                    Texture.MipMap mipmap = texture.mipMapsList[t];
                                    if (mipmap.storageType == Texture.StorageTypes.extCpr ||
                                        mipmap.storageType == Texture.StorageTypes.extUnc)
                                    {
                                        mipmap.dataOffset = refPkg.exportsTable[refExportId].dataOffset + (uint)refTexture.properties.propertyEndOffset + refTexture.mipMapsList[t].internalOffset;
                                        texture.mipMapsList[t] = mipmap;
                                    }
                                }
                            }
                        }

                        MemoryStream newData = new MemoryStream();
                        newData.WriteFromBuffer(texture.properties.toArray());
                        newData.WriteFromBuffer(texture.toArray(package.exportsTable[l].dataOffset + (uint)newData.Position));
                        package.setExportData(l, newData.ToArray());
                        modified = true;
                    }
                }
                if (_gameSelected == MeType.ME3_TYPE && !modified)
                    continue;
                package.SaveToFile();
            }

            EnableMenuOptions(true);
            eNDModdingToolStripMenuItem.Enabled = false;
            _mainWindow.updateStatusLabel("Done");
        }
    }
}
