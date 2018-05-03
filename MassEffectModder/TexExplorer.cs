/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace MassEffectModder
{
    public partial class TexExplorer : Form
    {
        MeType _gameSelected;
        public MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<FoundTexture> _textures;
        bool previewShow = true;
        bool detailedInfo = false;
        bool singlePackageMode = false;
        TreeScan treeScan;
        MipMaps mipMaps;

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
        public List<FoundTexture> texturesPreMap;

        public TexExplorer(MainWindow main, MeType gameType)
        {
            InitializeComponent();
            _mainWindow = main;
            _gameSelected = gameType;
            _configIni = main._configIni;
            gameData = new GameData(_gameSelected, _configIni);
            treeScan = new TreeScan();
            mipMaps = new MipMaps();
            texturesPreMap = new List<FoundTexture>();
            MipMaps.modsToReplace = new List<ModEntry>();
            new TreeScan().loadTexturesMap(GameData.gameType, texturesPreMap);
        }

        public void EnableMenuOptions(bool enable)
        {
            MODsToolStripMenuItem.Enabled = enable;
            searchToolStripMenuItem.Enabled = enable;
            treeViewPackages.Enabled = enable;
            listViewResults.Enabled = enable;
            listViewTextures.Enabled = enable;
            listViewMods.Enabled = enable;
            listViewPackages.Enabled = enable;
            replaceTextureToolStripMenuItem.Enabled = enable;
            viewToolStripMenuItem.Enabled = enable;
            extractToDDSFileToolStripMenuItem.Enabled = enable;
            extractToPNGFileToolStripMenuItem.Enabled = enable;
            _mainWindow.helpToolStripMenuItem.Enabled = enable;
            Application.DoEvents();
        }

        public void Run()
        {
            _mainWindow.updateStatusLabel("");
            _mainWindow.updateStatusLabel2("");
            EnableMenuOptions(false);
            clearMODsToolStripMenuItem.Enabled = false;
            listViewResults.Hide();
            listViewMods.Hide();
            listViewPackages.Hide();
            richTextBoxInfo.Hide();
            listViewTextures.Clear();
            listViewPackages.Clear();
            richTextBoxInfo.Clear();
            if (GameData.gameType == MeType.ME1_TYPE)
            {
                singleMultiPackageToolStripMenuItem.Enabled = false;
            }

            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                Close();
                return;
            }

            if (!Misc.CheckAndCorrectAccessToGame(_gameSelected))
            {
                Close();
                return;
            }

            if (!_mainWindow.GetPackages(gameData))
            {
                Close();
                return;
            }
            else
            {
                string errors = "";
                string log = "";
                _mainWindow.updateStatusLabel("");
                _mainWindow.updateStatusLabel("Preparing tree...");
                errors += treeScan.PrepareListOfTextures(GameData.gameType, this, _mainWindow, null, ref log, false);
                _textures = treeScan.treeScan;
                if (errors != "")
                {
                    MessageBox.Show("WARNING: Some errors have occured!");
                    string errorFile = "errors-scan.txt";
                    if (File.Exists(errorFile))
                        File.Delete(errorFile);
                    using (FileStream fs = new FileStream(errorFile, FileMode.CreateNew))
                    {
                        fs.WriteStringASCII(errors);
                    }
                    Process.Start(errorFile);
                }
            }

            if (_textures == null)
            {
                EnableMenuOptions(true);
                Close();
                return;
            }

            PrepareTreeList();
            EnableMenuOptions(true);
        }

        struct EmptyMipMaps
        {
            public MeType gameType;
            public string packagePath;
            public int exportId;
            public uint crc;
        }

        public bool verifyGameDataEmptyMipMapsRemoval()
        {
            EmptyMipMaps[] entries = new EmptyMipMaps[]
            {
                new EmptyMipMaps
                {
                    gameType = MeType.ME1_TYPE,
                    packagePath = "\\BioGame\\CookedPC\\Packages\\VFX_Prototype\\v_Explosion_PrototypeTest_01.upk",
                    exportId = 4888,
                    crc = 0x9C074E3B,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME2_TYPE,
                    packagePath = "\\BioGame\\CookedPC\\BioA_CitHub_500Udina.pcc",
                    exportId = 3655,
                    crc = 0xE18544C0,
                },
                new EmptyMipMaps
                {
                    gameType = MeType.ME3_TYPE,
                    packagePath = "\\BioGame\\CookedPCConsole\\BIOG_UIWorld.pcc",
                    exportId = 464,
                    crc = 0x85EFF558,
                },
            };

            for (int i = 0; i < entries.Count(); i++)
            {
                if (GameData.gameType == entries[i].gameType)
                {
                    Package package = new Package(GameData.GamePath + entries[i].packagePath);
                    Texture texture = new Texture(package, entries[i].exportId, package.getExportData(entries[i].exportId));
                    if (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                        return false;
                }
            }

            return true;
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (GameData.packageFiles != null)
                GameData.packageFiles.Clear();
            _mainWindow.updateStatusLabel("");
            _mainWindow.updateStatusLabel2("");
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
                item.Text = texture.name;
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
            clearPreview();
            if (listViewTextures.SelectedItems.Count == 0)
            {
                return;
            }
            if (listViewMods.Items.Count > 0)
            {
                previewShow = true;
                string log = "";
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                mipMaps.previewTextureMod(listViewMods.SelectedItems[0].Name, index, _textures, this, ref log);
                _mainWindow.updateStatusLabel("Done.");
                pictureBoxPreview.Show();
                richTextBoxInfo.Hide();
            }
            else
            {
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                if (previewShow)
                {
                    MatchedTexture nodeTexture = node.textures[index].list.Find(s => s.path != "");
                    Package package = new Package(GameData.GamePath + nodeTexture.path);
                    Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                    byte[] textureData = texture.getTopImageData();
                    if (textureData != null)
                    {
                        int width = texture.getTopMipmap().width;
                        int height = texture.getTopMipmap().height;
                        PixelFormat pixelFormat = Image.getPixelFormatType(texture.properties.getProperty("Format").valueName);
                        pictureBoxPreview.Image = Image.convertRawToBitmapARGB(textureData, width, height, pixelFormat);
                        pictureBoxPreview.Show();
                        richTextBoxInfo.Hide();
                    }
                    package.Dispose();
                }
                else
                {
                    string text = "";
                    text += "Texture original CRC:  " + string.Format("0x{0:X8}", node.textures[index].crc) + "\n";
                    text += "Node name:     " + node.textures[index].name + "\n";
                    for (int index2 = 0; index2 < (detailedInfo ? node.textures[index].list.Count : 1); index2++)
                    {
                        MatchedTexture nodeTexture = node.textures[index].list[index2];
                        Package package = new Package(GameData.GamePath + nodeTexture.path);
                        Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                        text += "\nTexture instance: " + (index2 + 1) + "\n";
                        text += "  Texture name:  " + package.exportsTable[nodeTexture.exportID].objectName + "\n";
                        text += "  Export Id:     " + nodeTexture.exportID + "\n";
                        if (GameData.gameType == MeType.ME1_TYPE)
                        {
                            if (nodeTexture.linkToMaster == -1)
                                text += "  Package name:  " + texture.packageName + "\n";
                            else
                                text += "  Package name:  " + texture.basePackageName + "\n";
                        }
                        text += "  Package path:  " + nodeTexture.path + "\n";
                        text += "  Texture properties:\n";
                        for (int l = 0; l < texture.properties.texPropertyList.Count; l++)
                        {
                            text += "  " + texture.properties.getDisplayString(l);
                        }
                        text += "\n";
                        for (int l = 0; l < texture.mipMapsList.Count; l++)
                        {
                            text += "  MipMap: " + l + ", " + texture.mipMapsList[l].width + "x" + texture.mipMapsList[l].height + "\n";
                            text += "    StorageType: " + texture.mipMapsList[l].storageType + "\n";
                            text += "    DataOffset:  " + (int)texture.mipMapsList[l].dataOffset + "\n";
                            text += "    CompSize:    " + texture.mipMapsList[l].compressedSize + "\n";
                            text += "    UnCompSize:  " + texture.mipMapsList[l].uncompressedSize + "\n";
                        }
                        package.Dispose();
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
                bool found = false;
                if (name != "")
                {
                    if (name.Contains("*"))
                    {
                        Regex regex = new Regex("^" + Regex.Escape(name).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase);
                        if (regex.IsMatch(foundTexture.name))
                        {
                            found = true;
                        }
                    }
                    else if (foundTexture.name.ToLowerInvariant() == name.ToLowerInvariant())
                    {
                        found = true;
                    }
                }
                else if (crc != 0 && foundTexture.crc == crc)
                {
                    found = true;
                }
                if (found)
                {
                    ListViewItem item = new ListViewItem(foundTexture.name + " (" + Path.GetFileNameWithoutExtension(foundTexture.list.Find(s => s.path != "").path).ToUpperInvariant() + ")");
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
            if (listViewResults.Items.Count == 0)
            {
                MessageBox.Show("Texture not found.");
            }
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            singlePackageMode = false;
            listViewPackages.Hide();
            listViewPackages.Clear();
            detailedInfo = false;
            richTextBoxInfo.Show();
            pictureBoxPreview.Hide();
            previewShow = false;
            updateViewFromListView();
        }

        private void info2TextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            singlePackageMode = false;
            listViewPackages.Hide();
            listViewPackages.Clear();
            detailedInfo = true;
            richTextBoxInfo.Show();
            pictureBoxPreview.Hide();
            previewShow = false;
            updateViewFromListView();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            singlePackageMode = false;
            listViewPackages.Hide();
            listViewPackages.Clear();
            detailedInfo = false;
            richTextBoxInfo.Hide();
            pictureBoxPreview.Show();
            previewShow = true;
            updateViewFromListView();
        }

        private void updateListOfPackagesView()
        {
            listViewPackages.Clear();
            if (listViewTextures.SelectedItems.Count == 0)
            {
                return;
            }

            int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
            PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;

            for (int index2 = 0; index2 < node.textures[index].list.Count; index2++)
            {
                if (node.textures[index].list[index2].path == "")
                    continue;
                ListViewItem item = new ListViewItem();
                item.Name = index2.ToString();
                item.Text = (index2 + 1) + ": " + Path.GetFileNameWithoutExtension(node.textures[index].list[index2].path);
                listViewPackages.Items.Add(item);
            }
        }

        private void singleMultiPackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (singlePackageMode)
            {
                singlePackageMode = false;
                listViewPackages.Hide();
                listViewPackages.Clear();
            }
            else
            {
                listViewPackages.Show();
                singlePackageMode = true;
                updateListOfPackagesView();
            }
        }

        private void listViewTextures_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateViewFromListView();
            updateListOfPackagesView();
        }

        private void listViewTextures_DoubleClick(object sender, EventArgs e)
        {
            if (!replaceTextureToolStripMenuItem.Enabled)
                return;
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            Misc.startTimer();
            replaceTexture(false);
            var time = Misc.stopTimer();
            _mainWindow.updateStatusLabel("Done. Process total time: " + Misc.getTimerFormat(time));
            _mainWindow.updateStatusLabel2("");
        }

        private void listViewTextures_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\r')
                return;
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            Misc.startTimer();
            replaceTexture(false);
            var time = Misc.stopTimer();
            _mainWindow.updateStatusLabel("Done. Process total time: " + Misc.getTimerFormat(time));
            _mainWindow.updateStatusLabel2("");
        }

        private void selectFoundTexture(ListViewItem item)
        {
            int pos1 = item.Text.IndexOf('(');
            int pos2 = item.Text.IndexOf(')');
            string packageName = item.Text.Substring(pos1 + 1, pos2 - pos1 - 1).ToLowerInvariant();
            listViewResults.Hide();
            for (int l = 0; l < treeViewPackages.Nodes[0].Nodes.Count; l++)
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.Nodes[0].Nodes[l];
                if (node.Name.ToLowerInvariant() == packageName)
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
            replaceTexture(false);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void replaceTextureToolStripMenuItemConvert_Click(object sender, EventArgs e)
        {
            _mainWindow.updateStatusLabel("Replacing texture (convert mode) ...");
            _mainWindow.updateStatusLabel2("");
            replaceTexture(false);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
        }

        private void switchModMode(bool enable)
        {
            loadMODsToolStripMenuItem.Enabled = !enable;
            clearMODsToolStripMenuItem.Enabled = false;
            packMODToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void switchModsMode(bool enable)
        {
            loadMODsToolStripMenuItem.Enabled = enable;
            clearMODsToolStripMenuItem.Enabled = enable;
            packMODToolStripMenuItem.Enabled = true;
            searchToolStripMenuItem.Enabled = !enable;
            replaceTextureToolStripMenuItem.Enabled = !enable;
            viewToolStripMenuItem.Enabled = !enable;
            extractToDDSFileToolStripMenuItem.Enabled = !enable;
            extractToPNGFileToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void loadMODsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select Mod file";
                modFile.Filter = "MOD file | *.mem;*.tpf;*.mod";
                modFile.Multiselect = true;
                modFile.InitialDirectory = GameData.lastLoadMODPath;
                if (modFile.ShowDialog() != DialogResult.OK)
                    return;
                GameData.lastLoadMODPath = Path.GetDirectoryName(modFile.FileNames[0]);

                EnableMenuOptions(false);

                listViewMods.Show();

                listViewTextures.Clear();
                clearPreview();

                string[] files = modFile.FileNames;
                foreach (string file in files)
                {
                    _mainWindow.updateStatusLabel("MOD: " + Path.GetFileNameWithoutExtension(file) + " loading...");
                    if (file.EndsWith(".mem", StringComparison.OrdinalIgnoreCase))
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            uint tag = fs.ReadUInt32();
                            uint version = fs.ReadUInt32();
                            if (tag != TreeScan.TextureModTag || version != TreeScan.TextureModVersion)
                            {
                                if (version != TreeScan.TextureModVersion)
                                    MessageBox.Show("File " + file + " was made with an older version of MEM, skipping...");
                                else
                                    MessageBox.Show("File " + file + " is not a valid MEM mod, skipping...");
                                continue;
                            }
                        }
                    }
                    else if (file.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                            {
                                string package = "";
                                int len = fs.ReadInt32();
                                string version = fs.ReadStringASCIINull();
                                if (version.Length < 5) // legacy .mod
                                    fs.SeekBegin();
                                else
                                {
                                    fs.SeekBegin();
                                    len = fs.ReadInt32();
                                    version = fs.ReadStringASCII(len); // version
                                }
                                uint numEntries = fs.ReadUInt32();
                                for (uint i = 0; i < numEntries; i++)
                                {
                                    BinaryMod mod = new BinaryMod();
                                    len = fs.ReadInt32();
                                    string desc2 = fs.ReadStringASCII(len); // description
                                    len = fs.ReadInt32();
                                    string scriptLegacy = fs.ReadStringASCII(len);
                                    string path = "";
                                    if (desc2.Contains("Binary Replacement"))
                                    {
                                        try
                                        {
                                            Misc.ParseME3xBinaryScriptMod(scriptLegacy, ref package, ref mod.exportId, ref path);
                                            if (mod.exportId == -1 || package == "" || path == "")
                                                throw new Exception();
                                        }
                                        catch
                                        {
                                            len = fs.ReadInt32();
                                            fs.Skip(len);
                                            MessageBox.Show("Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        string textureName = desc2.Split(' ').Last();
                                        try
                                        {
                                            int index = Misc.ParseLegacyMe3xScriptMod(_textures, scriptLegacy, textureName);
                                            if (index == -1)
                                                throw new Exception();
                                        }
                                        catch
                                        {
                                            len = fs.ReadInt32();
                                            fs.Skip(len);
                                            MessageBox.Show("Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file);
                                            continue;
                                        }
                                    }
                                    len = fs.ReadInt32();
                                    fs.Skip(len);
                                }
                            }
                        }
                        catch
                        {
                            MessageBox.Show("File " + file + " is not a valid Mod file, skipping...");
                        }
                    }
                    else if (file.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
                    {
                        int result;
                        string fileName = "";
                        ulong dstLen = 0;
                        string[] ddsList = null;
                        ulong numEntries = 0;

                        IntPtr handle = IntPtr.Zero;
                        ZlibHelper.Zip zip = new ZlibHelper.Zip();
                        try
                        {
                            int indexTpf = -1;
                            handle = zip.Open(file, ref numEntries, 1);
                            for (ulong i = 0; i < numEntries; i++)
                            {
                                result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                                fileName = fileName.Trim();
                                if (result != 0)
                                    throw new Exception();
                                if (Path.GetExtension(fileName).ToLowerInvariant() == ".def" ||
                                    Path.GetExtension(fileName).ToLowerInvariant() == ".log")
                                {
                                    indexTpf = (int)i;
                                    break;
                                }
                                result = zip.GoToNextFile(handle);
                                if (result != 0)
                                    throw new Exception();
                            }
                            byte[] listText = new byte[dstLen];
                            result = zip.ReadCurrentFile(handle, listText, dstLen);
                            if (result != 0)
                                throw new Exception();
                            ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                            result = zip.GoToFirstFile(handle);
                            if (result != 0)
                                throw new Exception();

                            for (uint i = 0; i < numEntries; i++)
                            {
                                if (i == indexTpf)
                                {
                                    result = zip.GoToNextFile(handle);
                                    continue;
                                }
                                uint crc = 0;
                                result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                                if (result != 0)
                                    throw new Exception();
                                fileName = fileName.Trim();
                                foreach (string dds in ddsList)
                                {
                                    string ddsFile = dds.Split('|')[1];
                                    if (ddsFile.ToLowerInvariant().Trim() != fileName.ToLowerInvariant())
                                        continue;
                                    crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }
                                string filename = Path.GetFileName(fileName);
                                if (crc == 0)
                                {
                                    zip.GoToNextFile(handle);
                                    continue;
                                }
                                result = zip.GoToNextFile(handle);
                            }
                        }
                        catch
                        {
                            MessageBox.Show("File " + file + " is not a valid TPF mod, skipping...");
                        }
                        if (handle != IntPtr.Zero)
                            zip.Close(handle);
                        handle = IntPtr.Zero;
                    }
                    string desc = Path.GetFileNameWithoutExtension(file);
                    if (file.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
                        desc = Path.GetFileNameWithoutExtension(file) + " (TPF)";
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
            applyModToolStripMenuItem_Click(false);
        }

        private void applyModsWithVerificationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            applyModToolStripMenuItem_Click(true);
        }

        private void applyModToolStripMenuItem_Click(bool verify)
        {
            if (listViewMods.SelectedItems.Count == 0)
            {
                foreach (ListViewItem item in listViewMods.Items)
                    item.Selected = true;
                if (listViewMods.SelectedItems.Count == 0)
                    return;
            }

            EnableMenuOptions(false);
            richTextBoxInfo.Text = "";
            string log = "";
            string errors = "";

            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;
            foreach (ListViewItem item in listViewMods.SelectedItems)
            {
                diskUsage += new FileInfo(item.Name).Length;
            }
            diskUsage = (long)(diskUsage * 2.5);
            if (diskUsage > diskFreeSpace)
            {
                MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                EnableMenuOptions(true);
                return;
            }

            Misc.startTimer();
            MipMaps.modsToReplace.Clear();
            foreach (ListViewItem item in listViewMods.SelectedItems)
            {
                errors += mipMaps.replaceTextureMod(item.Name, _textures, this, false, ref log);
                _mainWindow.updateStatusLabel("MOD: " + item.Text + " preparing...");
                listViewMods.Items.Remove(item);
            }
            errors += mipMaps.replaceModsFromList(_textures, _mainWindow, null, false, false, verify, false, false);
            if (GameData.gameType == MeType.ME3_TYPE)
                TOCBinFile.UpdateAllTOCBinFiles();
            _mainWindow.updateStatusLabel("");

            if (verify)
            {
                errors = "";
                _mainWindow.updateStatusLabel2("Verification...");
                for (int k = 0; k < _textures.Count; k++)
                {
                    FoundTexture foundTexture = _textures[k];
                    for (int t = 0; t < foundTexture.list.Count; t++)
                    {
                        if (foundTexture.list[t].path == "")
                            continue;
                        MatchedTexture matchedTexture = foundTexture.list[t];
                        if (matchedTexture.crcs != null)
                        {
                            _mainWindow.updateStatusLabel("Texture: " + foundTexture.name + " in " + matchedTexture.path);
                            Package pkg = new Package(GameData.GamePath + matchedTexture.path);
                            Texture texture = new Texture(pkg, matchedTexture.exportID, pkg.getExportData(matchedTexture.exportID));
                            for (int m = 0; m < matchedTexture.crcs.Count(); m++)
                            {
                                if (matchedTexture.crcs[m] != texture.getCrcData(texture.getMipMapDataByIndex(m)))
                                {
                                    errors += "CRC does not match: Texture: " + foundTexture.name + ", instance: " + t + ", mipmap: " +
                                        m + Environment.NewLine;
                                }
                            }
                            matchedTexture.crcs = null;
                            foundTexture.list[t] = matchedTexture;
                            pkg.Dispose();
                        }
                    }
                }
            }

            var time = Misc.stopTimer();
            if (listViewMods.Items.Count == 0)
                clearMODsView();
            _mainWindow.updateStatusLabel("MODs applied. Process total time: " + Misc.getTimerFormat(time));
            _mainWindow.updateStatusLabel2("");
            if (errors != "")
            {
                richTextBoxInfo.Text = errors;
                richTextBoxInfo.Show();
                pictureBoxPreview.Hide();
                MessageBox.Show("WARNING: Some errors have occured!");
            }

            EnableMenuOptions(true);
        }

        private void deleteModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
            {
                foreach (ListViewItem item in listViewMods.Items)
                    item.Selected = true;
                if (listViewMods.SelectedItems.Count == 0)
                    return;
            }

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

            string log = "";
            if (listViewMods.SelectedItems[0].Name.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
                return;
            if (listViewMods.SelectedItems[0].Name.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                return;
            _mainWindow.updateStatusLabel("Processing MOD...");
            richTextBoxInfo.Text = mipMaps.listTextureMod(listViewMods.SelectedItems[0].Name, _textures, this, ref log);
            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel2("");
            if (richTextBoxInfo.Text != "")
            {
                richTextBoxInfo.Show();
                pictureBoxPreview.Hide();
                MessageBox.Show("WARNING: Some errors have occured!");
            }
        }

        private void extractModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewMods.SelectedItems.Count == 0)
            {
                foreach (ListViewItem item in listViewMods.Items)
                    item.Selected = true;
                if (listViewMods.SelectedItems.Count == 0)
                    return;
            }

            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.Description = "Please select destination directory for MEM extraction";
                modFile.SelectedPath = GameData.lastExtractMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    GameData.lastExtractMODPath = modFile.SelectedPath;
                    long diskFreeSpace = Misc.getDiskFreeSpace(modFile.SelectedPath);
                    long diskUsage = 0;
                    foreach (ListViewItem item in listViewMods.SelectedItems)
                    {
                        diskUsage += new FileInfo(item.Name).Length;
                    }
                    diskUsage = (long)(diskUsage * 2.5);
                    if (diskUsage > diskFreeSpace)
                    {
                        MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                        EnableMenuOptions(true);
                        _mainWindow.updateStatusLabel("");
                        _mainWindow.updateStatusLabel2("");
                    }

                    Misc.startTimer();
                    richTextBoxInfo.Text = "";
                    string log = "";
                    foreach (ListViewItem item in listViewMods.SelectedItems)
                    {
                        string outDir = Path.Combine(modFile.SelectedPath, Path.GetFileNameWithoutExtension(item.Name));
                        Directory.CreateDirectory(outDir);
                        _mainWindow.updateStatusLabel("MEM: " + item.Text + "extracting...");
                        _mainWindow.updateStatusLabel2("");
                        richTextBoxInfo.Text += mipMaps.extractTextureMod(item.Name, outDir, texturesPreMap, this, ref log);
                    }
                    var time = Misc.stopTimer();
                    _mainWindow.updateStatusLabel("MEMs extracted. Process total time: " + Misc.getTimerFormat(time));
                    _mainWindow.updateStatusLabel2("");
                    if (richTextBoxInfo.Text != "")
                    {
                        richTextBoxInfo.Show();
                        pictureBoxPreview.Hide();
                        MessageBox.Show("WARNING: Some errors have occured!");
                    }
                }
            }
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("");
            _mainWindow.updateStatusLabel2("");
        }

        private void createMODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            createMODToolStripMenuItem(false);
        }

        private void packMODToolStripMenuItemConvert_Click(object sender, EventArgs e)
        {
            createMODToolStripMenuItem(true);
        }

        private void createMODToolStripMenuItem(bool markConvert)
        {
            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.Description = "Please select source directory for MEM creation";
                modFile.SelectedPath = gameData.lastCreateMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    gameData.lastCreateMODPath = modFile.SelectedPath;
                    long diskUsage = new DirectoryInfo(modFile.SelectedPath).GetFiles("*.dds").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.bmp").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.png").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.tga").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.jpg").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.jpeg").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.bin").ToList().Sum(file => file.Length);
                    diskUsage += new DirectoryInfo(modFile.SelectedPath).GetFiles("*.xdelta").ToList().Sum(file => file.Length);
                    long diskFreeSpace = Misc.getDiskFreeSpace(modFile.SelectedPath);
                    diskUsage = (long)(diskUsage / 1.5);
                    if (diskUsage < diskFreeSpace)
                    {
                        Misc.startTimer();
                        _mainWindow.updateStatusLabel("MEM packing...");
                        _mainWindow.updateStatusLabel2("");
                        string errors = "";
                        Misc.convertDataModtoMem(modFile.SelectedPath,
                            Path.Combine(Path.GetDirectoryName(modFile.SelectedPath), Path.GetFileName(modFile.SelectedPath)) + ".mem",
                            texturesPreMap, GameData.gameType, _mainWindow, ref errors, markConvert, true, false);
                        var time = Misc.stopTimer();
                        richTextBoxInfo.Text = errors;
                        if (richTextBoxInfo.Text != "")
                        {
                            richTextBoxInfo.Show();
                            pictureBoxPreview.Hide();
                            MessageBox.Show("WARNING: Some errors have occured!");
                        }
                        _mainWindow.updateStatusLabel("MOD packed. Process total time: " + Misc.getTimerFormat(time));
                    }
                    else
                    {
                        MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                    }
                }
            }

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel2("");
        }

        private void createMODBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.Description = "Please select source directory for MEM creation";
                modFile.SelectedPath = gameData.lastCreateMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    gameData.lastCreateMODPath = modFile.SelectedPath;
                    List<string> listDirs = Directory.GetDirectories(modFile.SelectedPath).ToList();
                    if (listDirs.Count == 0)
                    {
                        MessageBox.Show("There are no subfolders required for mod batch creation. Use mod creation instead.");
                        EnableMenuOptions(true);
                        return;
                    }

                    long diskFreeSpace = Misc.getDiskFreeSpace(modFile.SelectedPath);
                    long diskUsage = 0;
                    for (int i = 0; i < listDirs.Count; i++)
                    {
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.dds").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.bmp").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.png").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.tga").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.jpg").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.jpeg").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.bin").ToList().Sum(file => file.Length);
                        diskUsage += new DirectoryInfo(listDirs[i]).GetFiles("*.xdelta").ToList().Sum(file => file.Length);
                    }
                    diskUsage = (long)(diskUsage / 1.5);

                    if (diskUsage > diskFreeSpace)
                    {
                        MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                        EnableMenuOptions(true);
                        _mainWindow.updateStatusLabel2("");
                    }

                    Misc.startTimer();
                    _mainWindow.updateStatusLabel("MEMs packing...");
                    _mainWindow.updateStatusLabel2("");
                    richTextBoxInfo.Text = "";
                    richTextBoxInfo.Show();
                    pictureBoxPreview.Hide();
                    string errors = "";
                    for (int i = 0; i < listDirs.Count; i++)
                    {
                        Misc.convertDataModtoMem(listDirs[i], Path.Combine(Path.GetDirectoryName(listDirs[i]), Path.GetFileName(listDirs[i])) + ".mem",
                            texturesPreMap, GameData.gameType, _mainWindow, ref errors, true, false, false);
                    }
                    var time = Misc.stopTimer();
                    richTextBoxInfo.Text = errors;
                    if (richTextBoxInfo.Text != "")
                    {
                        MessageBox.Show("WARNING: Some errors have occured!");
                    }
                    _mainWindow.updateStatusLabel("MEMs packed. Process total time: " + Misc.getTimerFormat(time));
                }
            }

            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel2("");
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture name or CRC.\n\nYou can use * as wilcards.", "Search Texture", "", 0, 0);
            if (string.IsNullOrEmpty(name))
                return;

            uint crc = 0;
            string crcStr = name.ToLowerInvariant();
            try
            {
                crcStr = crcStr.Substring(crcStr.IndexOf("0x") + 2, 8);
                crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                searchTexture("", crc);
            }
            catch
            {
                name = name.Split('.')[0]; // in case filename
                searchTexture(name, 0);
            }
        }

        private void extractToDDSFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            if (listViewTextures.SelectedItems.Count == 0)
                return;

            if (singlePackageMode && listViewPackages.SelectedItems.Count == 0)
                return;

            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                ListViewItem item = listViewTextures.FocusedItem;
                int index = Convert.ToInt32(item.Name);
                MatchedTexture nodeTexture;
                if (singlePackageMode)
                {
                    ListViewItem itemPkg = listViewPackages.FocusedItem;
                    int indexPackage = Convert.ToInt32(itemPkg.Name);
                    nodeTexture = node.textures[index].list[indexPackage];
                }
                else
                {
                    nodeTexture = node.textures[index].list.Find(s => s.path != "");
                }
                saveFile.Title = "Please select output DDS file";
                saveFile.Filter = "DDS file | *.dds";
                saveFile.FileName = node.textures[index].name + string.Format("_0x{0:X8}", node.textures[index].crc) + ".dds";
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    mipMaps.extractTextureToDDS(saveFile.FileName, GameData.GamePath + nodeTexture.path, nodeTexture.exportID);
                }
            }
            EnableMenuOptions(true);
        }

        private void extractToPNGFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            if (listViewTextures.SelectedItems.Count == 0)
                return;

            if (singlePackageMode && listViewPackages.SelectedItems.Count == 0)
                return;

            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                ListViewItem item = listViewTextures.FocusedItem;
                int index = Convert.ToInt32(item.Name);
                MatchedTexture nodeTexture;
                if (singlePackageMode)
                {
                    ListViewItem itemPkg = listViewPackages.FocusedItem;
                    int indexPackage = Convert.ToInt32(itemPkg.Name);
                    nodeTexture = node.textures[index].list[indexPackage];
                }
                else
                {
                    nodeTexture = node.textures[index].list.Find(s => s.path != "");
                }
                saveFile.Title = "Please select output PNG file";
                saveFile.Filter = "PNG file | *.png";
                saveFile.FileName = node.textures[index].name + string.Format("_0x{0:X8}", node.textures[index].crc) + ".png";
                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    mipMaps.extractTextureToPng(saveFile.FileName, GameData.GamePath + nodeTexture.path, nodeTexture.exportID);
                }
            }
            EnableMenuOptions(true);
        }

        private string convertDataModtoMem(List<FoundTexture> textures, bool markConvert, bool batch = false)
        {
            string errors = "";
            string[] files = null;
            string memFilename = "";
            string memDir = "";

            if (batch)
            {
                using (FolderBrowserDialog modDir = new FolderBrowserDialog())
                {
                    modDir.Description = "Please select source directory of .mod, .tpf files to convert";
                    if (modDir.ShowDialog() != DialogResult.OK)
                        return "";
                    List<string> list = Directory.GetFiles(modDir.SelectedPath, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
                    list.AddRange(Directory.GetFiles(modDir.SelectedPath, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)));
                    list.Sort();
                    files = list.ToArray();
                }
                using (FolderBrowserDialog modDir = new FolderBrowserDialog())
                {
                    modDir.Description = "Please select destination directory of MEM files";
                    if (modDir.ShowDialog() != DialogResult.OK)
                        return "";
                    memDir = modDir.SelectedPath;
                }
            }
            else
            {
                using (OpenFileDialog modFile = new OpenFileDialog())
                {
                    modFile.Title = "Please select Texmod .tpf, ME3Explorer .mod";
                    modFile.Filter = "TFP,MOD files | *.mod;*.tpf";
                    modFile.Multiselect = true;
                    if (modFile.ShowDialog() != DialogResult.OK)
                        return "";
                    files = modFile.FileNames;
                }
                using (SaveFileDialog memFile = new SaveFileDialog())
                {
                    memFile.Title = "Select a MEM .mem file, or create a new one";
                    memFile.Filter = "MEM file | *.mem";
                    if (memFile.ShowDialog() != DialogResult.OK)
                    {
                        _mainWindow.updateStatusLabel("");
                        return "";
                    }
                    memFilename = memFile.FileName;
                }
            }

            int result;
            string fileName = "";
            ulong dstLen = 0;
            string[] ddsList = null;
            ulong numEntries = 0;
            List<BinaryMod> mods = new List<BinaryMod>();
            foreach (string file in files)
            {
                _mainWindow.updateStatusLabel("Processing mod: " + file);
                if (file.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            string package = "";
                            int len = fs.ReadInt32();
                            string version = fs.ReadStringASCIINull();
                            if (version.Length < 5) // legacy .mod
                                fs.SeekBegin();
                            else
                            {
                                fs.SeekBegin();
                                len = fs.ReadInt32();
                                version = fs.ReadStringASCII(len); // version
                            }
                            numEntries = fs.ReadUInt32();
                            for (uint i = 0; i < numEntries; i++)
                            {
                                _mainWindow.updateStatusLabel2("Mod entry " + (i + 1) + " of " + numEntries);
                                BinaryMod mod = new BinaryMod();
                                len = fs.ReadInt32();
                                string desc = fs.ReadStringASCII(len); // description
                                len = fs.ReadInt32();
                                string scriptLegacy = fs.ReadStringASCII(len);
                                string path = "";
                                if (desc.Contains("Binary Replacement"))
                                {
                                    try
                                    {
                                        Misc.ParseME3xBinaryScriptMod(scriptLegacy, ref package, ref mod.exportId, ref path);
                                        if (mod.exportId == -1 || package == "" || path == "")
                                            throw new Exception();
                                    }
                                    catch
                                    {
                                        len = fs.ReadInt32();
                                        fs.Skip(len);
                                        errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }
                                    mod.packagePath = Path.Combine(path, package);
                                    mod.binaryModType = 1;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);
                                }
                                else
                                {
                                    string textureName = desc.Split(' ').Last();
                                    FoundTexture f;
                                    int index = -1;
                                    try
                                    {
                                        index = Misc.ParseLegacyMe3xScriptMod(textures, scriptLegacy, textureName);
                                        if (index == -1)
                                            throw new Exception();
                                        f = textures[index];
                                    }
                                    catch
                                    {
                                        len = fs.ReadInt32();
                                        fs.Skip(len);
                                        errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }
                                    mod.textureCrc = f.crc;
                                    mod.textureName = f.name;
                                    mod.binaryModType = 0;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);
                                    PixelFormat pixelFormat = f.pixfmt;
                                    Image image = new Image(mod.data, Image.ImageFormat.DDS);

                                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                        f.width / f.height)
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        continue;
                                    }

                                    PixelFormat newPixelFormat = pixelFormat;
                                    if (markConvert)
                                        newPixelFormat = Misc.changeTextureType(pixelFormat, image.pixelFormat, f.flags);
                                    if (!image.checkDDSHaveAllMipmaps() ||
                                       (f.list.Find(s => s.path != "").numMips > 1 && image.mipMaps.Count() <= 1) ||
                                       (markConvert && image.pixelFormat != newPixelFormat) ||
                                       (!markConvert && image.pixelFormat != pixelFormat))
                                    {
                                        bool dxt1HasAlpha = false;
                                        byte dxt1Threshold = 128;
                                        if (f.flags == TexProperty.TextureTypes.OneBitAlpha)
                                        {
                                            dxt1HasAlpha = true;
                                            if (image.pixelFormat == PixelFormat.ARGB ||
                                                image.pixelFormat == PixelFormat.DXT3 ||
                                                image.pixelFormat == PixelFormat.DXT5)
                                            {
                                                errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                                            }
                                        }
                                        image.correctMips(newPixelFormat, dxt1HasAlpha, dxt1Threshold);
                                        mod.data = image.StoreImageToDDS();
                                    }
                                }
                                mod.markConvert = markConvert;
                                mods.Add(mod);
                            }
                        }
                    }
                    catch
                    {
                        errors += "Mod is not compatible: " + file + Environment.NewLine;
                        continue;
                    }
                }
                else
                {
                    IntPtr handle = IntPtr.Zero;
                    ZlibHelper.Zip zip = new ZlibHelper.Zip();
                    try
                    {
                        int indexTpf = -1;
                        handle = zip.Open(file, ref numEntries, 1);
                        for (ulong i = 0; i < numEntries; i++)
                        {
                            result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            fileName = fileName.Trim();
                            if (result != 0)
                                throw new Exception();
                            if (Path.GetExtension(fileName).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(fileName).ToLowerInvariant() == ".log")
                            {
                                indexTpf = (int)i;
                                break;
                            }
                            result = zip.GoToNextFile(handle);
                            if (result != 0)
                                throw new Exception();
                        }
                        byte[] listText = new byte[dstLen];
                        result = zip.ReadCurrentFile(handle, listText, dstLen);
                        if (result != 0)
                            throw new Exception();
                        ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                        result = zip.GoToFirstFile(handle);
                        if (result != 0)
                            throw new Exception();

                        for (uint i = 0; i < numEntries; i++)
                        {
                            if (i == indexTpf)
                            {
                                result = zip.GoToNextFile(handle);
                                continue;
                            }
                            BinaryMod mod = new BinaryMod();
                            try
                            {
                                uint crc = 0;
                                result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                                if (result != 0)
                                    throw new Exception();
                                fileName = fileName.Trim();
                                foreach (string dds in ddsList)
                                {
                                    string ddsFile = dds.Split('|')[1];
                                    if (ddsFile.ToLowerInvariant().Trim() != fileName.ToLowerInvariant())
                                        continue;
                                    crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }
                                string filename = Path.GetFileName(fileName);
                                if (crc == 0)
                                {
                                    errors += "Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                                if (foundCrcList.Count == 0)
                                {
                                    errors += "Texture skipped. File " + filename + string.Format(" - 0x{0:X8}", crc) + " is not present in your game setup - mod: " + file + Environment.NewLine;
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                _mainWindow.updateStatusLabel2("Processing texture: " + (i + 1) + " of " + (numEntries - 1) + " - " + foundCrcList[0].name + string.Format("_0x{0:X8}", crc));

                                string textureName = foundCrcList[0].name;
                                mod.textureName = textureName;
                                mod.binaryModType = 0;
                                mod.textureCrc = crc;
                                mod.data = new byte[dstLen];
                                result = zip.ReadCurrentFile(handle, mod.data, dstLen);
                                if (result != 0)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    zip.GoToNextFile(handle);
                                    continue;
                                }
                                PixelFormat pixelFormat = foundCrcList[0].pixfmt;
                                Image image = new Image(mod.data, Path.GetExtension(filename));
                                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                    foundCrcList[0].width / foundCrcList[0].height)
                                {
                                    errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                PixelFormat newPixelFormat = pixelFormat;
                                if (markConvert)
                                    newPixelFormat = Misc.changeTextureType(pixelFormat, image.pixelFormat, foundCrcList[0].flags);

                                if (!image.checkDDSHaveAllMipmaps() ||
                                   (foundCrcList[0].list.Find(s => s.path != "").numMips > 1 && image.mipMaps.Count() <= 1) ||
                                   (markConvert && image.pixelFormat != newPixelFormat) ||
                                   (!markConvert && image.pixelFormat != pixelFormat))
                                {
                                    bool dxt1HasAlpha = false;
                                    byte dxt1Threshold = 128;
                                    if (foundCrcList[0].flags == TexProperty.TextureTypes.OneBitAlpha)
                                    {
                                        dxt1HasAlpha = true;
                                        if (image.pixelFormat == PixelFormat.ARGB ||
                                            image.pixelFormat == PixelFormat.DXT3 ||
                                            image.pixelFormat == PixelFormat.DXT5)
                                        {
                                            errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                                        }
                                    }
                                    image.correctMips(newPixelFormat, dxt1HasAlpha, dxt1Threshold);
                                    mod.data = image.StoreImageToDDS();
                                }

                                mod.markConvert = markConvert;
                                mods.Add(mod);
                            }
                            catch
                            {
                                errors += "Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                            }
                            result = zip.GoToNextFile(handle);
                        }
                        zip.Close(handle);
                        handle = IntPtr.Zero;
                    }
                    catch
                    {
                        errors += "Mod is not compatible: " + file + Environment.NewLine;
                        if (handle != IntPtr.Zero)
                            zip.Close(handle);
                        handle = IntPtr.Zero;
                        continue;
                    }
                }

                if (!batch && file != files.Last())
                    continue;

                if (mods.Count == 0 && file == files.Last() && !batch)
                {
                    _mainWindow.updateStatusLabel("Mods conversion failed");
                    _mainWindow.updateStatusLabel2("");
                    return errors;
                }

                if (batch)
                {
                    memFilename = Path.Combine(memDir, Path.GetFileNameWithoutExtension(file) + ".mem");
                    if (!Directory.Exists(Path.GetDirectoryName(memFilename)))
                        Directory.CreateDirectory(Path.GetDirectoryName(memFilename));
                    if (File.Exists(memFilename))
                        File.Delete(memFilename);
                    if (mods.Count == 0)
                    {
                        errors += "This mod is not compatible: " + file + Environment.NewLine;
                        continue;
                    }
                }

                _mainWindow.updateStatusLabel("Saving to MEM file... " + memFilename);
                List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();
                FileStream outFs;
                if (File.Exists(memFilename))
                {
                    outFs = new FileStream(memFilename, FileMode.Open, FileAccess.ReadWrite);
                    uint tag = outFs.ReadUInt32();
                    uint version = outFs.ReadUInt32();
                    if (tag != TreeScan.TextureModTag || version != TreeScan.TextureModVersion)
                    {
                        if (version != TreeScan.TextureModVersion)
                            errors += "File " + memFilename + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                        else
                            errors += "File " + memFilename + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                        _mainWindow.updateStatusLabel("Mod conversion failed");
                        _mainWindow.updateStatusLabel2("");
                        return errors;
                    }
                    else
                    {
                        uint gameType = 0;
                        outFs.JumpTo(outFs.ReadInt64());
                        gameType = outFs.ReadUInt32();
                        if ((MeType)gameType != GameData.gameType)
                        {
                            errors += "File " + memFilename + " is not a MEM mod valid for this game" + Environment.NewLine;
                            _mainWindow.updateStatusLabel("Mod conversion failed");
                            _mainWindow.updateStatusLabel2("");
                            return errors;
                        }
                    }
                    int numFiles = outFs.ReadInt32();
                    for (int l = 0; l < numFiles; l++)
                    {
                        MipMaps.FileMod fileMod = new MipMaps.FileMod();
                        fileMod.tag = outFs.ReadUInt32();
                        fileMod.name = outFs.ReadStringASCIINull();
                        fileMod.offset = outFs.ReadInt64();
                        fileMod.size = outFs.ReadInt64();
                        modFiles.Add(fileMod);
                    }
                    outFs.SeekEnd();
                }
                else
                {
                    outFs = new FileStream(memFilename, FileMode.Create, FileAccess.Write);
                    outFs.WriteUInt32(TreeScan.TextureModTag);
                    outFs.WriteUInt32(TreeScan.TextureModVersion);
                    outFs.WriteInt64(0); // filled later
                }

                for (int l = 0; l < mods.Count; l++)
                {
                    MipMaps.FileMod fileMod = new MipMaps.FileMod();
                    Stream dst = MipMaps.compressData(mods[l].data);
                    dst.SeekBegin();
                    fileMod.offset = outFs.Position;
                    fileMod.size = dst.Length;

                    if (mods[l].binaryModType == 1)
                    {
                        fileMod.tag = MipMaps.FileBinaryTag;
                        fileMod.name = Path.GetFileNameWithoutExtension(mods[l].packagePath) + "_" + mods[l].exportId + ".bin";

                        outFs.WriteInt32(mods[l].exportId);
                        outFs.WriteStringASCIINull(mods[l].packagePath);
                    }
                    else if (mods[l].binaryModType == 2)
                    {
                        fileMod.tag = MipMaps.FileXdeltaTag;
                        fileMod.name = Path.GetFileNameWithoutExtension(mods[l].packagePath) + "_" + mods[l].exportId + ".xdelta";

                        outFs.WriteInt32(mods[l].exportId);
                        outFs.WriteStringASCIINull(mods[l].packagePath);
                    }
                    else
                    {
                        if (mods[l].markConvert)
                            fileMod.tag = MipMaps.FileTextureTag2;
                        else
                            fileMod.tag = MipMaps.FileTextureTag;
                        fileMod.name = mods[l].textureName + string.Format("_0x{0:X8}", mods[l].textureCrc) + ".dds";
                        outFs.WriteStringASCIINull(mods[l].textureName);
                        outFs.WriteUInt32(mods[l].textureCrc);
                    }
                    _mainWindow.updateStatusLabel2("Mod entry " + (l + 1) + " of " + mods.Count);
                    outFs.WriteFromStream(dst, dst.Length);
                    modFiles.Add(fileMod);
                }

                long pos = outFs.Position;
                outFs.SeekBegin();
                outFs.WriteUInt32(TreeScan.TextureModTag);
                outFs.WriteUInt32(TreeScan.TextureModVersion);
                outFs.WriteInt64(pos);
                outFs.JumpTo(pos);
                outFs.WriteUInt32((uint)GameData.gameType);
                outFs.WriteInt32(modFiles.Count);
                for (int i = 0; i < modFiles.Count; i++)
                {
                    outFs.WriteUInt32(modFiles[i].tag);
                    outFs.WriteStringASCIINull(modFiles[i].name);
                    outFs.WriteInt64(modFiles[i].offset);
                    outFs.WriteInt64(modFiles[i].size);
                }
                outFs.Close();
                if (batch)
                    mods.Clear();
                else
                    break;
                _mainWindow.updateStatusLabel("");
            }
            _mainWindow.updateStatusLabel("Mods conversion process completed");
            _mainWindow.updateStatusLabel2("");
            return errors;
        }

        private void convertME3ExplorermodForMEMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            string errors = convertDataModtoMem(texturesPreMap, false);
            if (errors != "")
            {
                string filename = "mod-errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                MessageBox.Show("WARNING: Some errors have occured!");
                Process.Start(filename);
            }
            EnableMenuOptions(true);
        }

        private void convertME3ExplorermodForMEMToolStripMenuItemConvert_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            string errors = convertDataModtoMem(texturesPreMap, true);
            if (errors != "")
            {
                string filename = "mod-errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                MessageBox.Show("WARNING: Some errors have occured!");
                Process.Start(filename);
            }
            EnableMenuOptions(true);
        }

        private void batchConvertME3ExplorermodForMEMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            string errors = convertDataModtoMem(texturesPreMap, true);
            if (errors != "")
            {
                string filename = "mod-errors.txt";
                if (File.Exists(filename))
                    File.Delete(filename);
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                MessageBox.Show("WARNING: Some errors have occured!");
                Process.Start(filename);
            }
            EnableMenuOptions(true);
        }
    }
}
