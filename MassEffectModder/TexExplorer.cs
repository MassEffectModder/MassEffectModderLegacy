/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Text.RegularExpressions;
using System.Diagnostics;

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
        public string packageName;
        public List<MatchedTexture> list;
    }

    public partial class TexExplorer : Form
    {
        public const uint textureMapBinTag = 0x5054454D;
        public const uint textureMapBinVersion = 1;
        public const uint TextureModTag = 0x444F4D54;
        public const uint FileTextureTag = 0x53444446;
        public const uint FileBinTag = 0x4E494246;
        public const uint TextureModVersion = 2;

        MeType _gameSelected;
        public MainWindow _mainWindow;
        ConfIni _configIni;
        public static GameData gameData;
        List<FoundTexture> _textures;
        bool previewShow = true;
        bool detailedInfo = false;
        CachePackageMgr cachePackageMgr;
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

        public TexExplorer(MainWindow main, MeType gameType)
        {
            InitializeComponent();
            _mainWindow = main;
            _gameSelected = gameType;
            _configIni = main._configIni;
            gameData = new GameData(_gameSelected, _configIni);
            cachePackageMgr = new CachePackageMgr(main, null);
            treeScan = new TreeScan();
            mipMaps = new MipMaps();
        }

        public void EnableMenuOptions(bool enable)
        {
            MODsToolStripMenuItem.Enabled = enable;
            searchToolStripMenuItem.Enabled = enable;
            miscToolStripMenuItem.Enabled = enable;
            treeViewPackages.Enabled = enable;
            listViewResults.Enabled = enable;
            listViewTextures.Enabled = enable;
            listViewMods.Enabled = enable;
            replaceTextureToolStripMenuItem.Enabled = enable;
            viewToolStripMenuItem.Enabled = enable;
            extractToDDSFileToolStripMenuItem.Enabled = enable;
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
            richTextBoxInfo.Hide();
            listViewTextures.Clear();
            richTextBoxInfo.Clear();

            if (!Directory.Exists(GameData.GamePath))
            {
                MessageBox.Show("Game path is wrong!");
                Close();
                return;
            }

            bool writeAccess = false;
            if (Misc.checkWriteAccessDir(GameData.MainData))
                writeAccess = true;
            if (_gameSelected == MeType.ME1_TYPE)
            {
                if (Misc.checkWriteAccessFile(GameData.GamePath + @"\BioGame\CookedPC\Packages\GameObjects\Characters\Humanoids\HumanMale\BIOG_HMM_HED_PROMorph.upk"))
                    writeAccess = true;
                else
                    writeAccess = false;
            }
            if (_gameSelected == MeType.ME2_TYPE)
            {
                if (Misc.checkWriteAccessFile(GameData.GamePath + @"\BioGame\CookedPC\BioD_CitAsL.pcc"))
                    writeAccess = true;
                else
                    writeAccess = false;
            }
            if (_gameSelected == MeType.ME3_TYPE)
            {
                if (Misc.checkWriteAccessFile(GameData.GamePath + @"\BioGame\CookedPCConsole\BioA_CitSam_000LevelTrans.pcc"))
                    writeAccess = true;
                else
                    writeAccess = false;
            }
            if (!writeAccess)
            {
                MessageBox.Show("Write access denied to game folders!\n\n" + GameData.GamePath);
                Close();
                return;
            }

            if (_gameSelected == MeType.ME1_TYPE)
                Misc.VerifyME1Exe(gameData);

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
                errors += treeScan.PrepareListOfTextures(this, null, _mainWindow, null, ref log);
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

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (GameData.packageFiles != null)
                GameData.packageFiles.Clear();
            cachePackageMgr.CloseAllWithoutSave();
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
            if (listViewTextures.SelectedItems.Count == 0)
            {
                clearPreview();
                return;
            }
            if (listViewMods.Items.Count > 0)
            {
                previewShow = true;
                string log = "";
                int index = Convert.ToInt32(listViewTextures.FocusedItem.Name);
                mipMaps.previewTextureMod(listViewMods.SelectedItems[0].Name, index, _textures, cachePackageMgr, this, ref log);
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
                    MatchedTexture nodeTexture = node.textures[index].list[0];
                    Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                    Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                    byte[] textureData = texture.getTopImageData();
                    if (textureData != null)
                    {
                        int width = texture.getTopMipmap().width;
                        int height = texture.getTopMipmap().height;
                        DDSFormat format = DDSImage.convertFormat(texture.properties.getProperty("Format").valueName);
                        pictureBoxPreview.Image = DDSImage.ToBitmap(textureData, format, width, height);
                        pictureBoxPreview.Show();
                        richTextBoxInfo.Hide();
                    }
                }
                else
                {
                    string text = "";
                    text += "Texture original CRC:  " + string.Format("0x{0:X8}", node.textures[index].crc) + "\n";
                    text += "Node name:     " + node.textures[index].name + "\n";
                    text += "Package name:  " + node.textures[index].packageName + "\n";
                    for (int index2 = 0; index2 < (detailedInfo ? node.textures[index].list.Count : 1); index2++)
                    {
                        MatchedTexture nodeTexture = node.textures[index].list[index2];
                        Package package = cachePackageMgr.OpenPackage(GameData.GamePath + nodeTexture.path);
                        Texture texture = new Texture(package, nodeTexture.exportID, package.getExportData(nodeTexture.exportID));
                        text += "\nTexture instance: " + (index2 + 1) + "\n";
                        text += "  Texture name:  " + package.exportsTable[nodeTexture.exportID].objectName + "\n";
                        text += "  Export Id:     " + node.textures[index].list[index2].exportID + "\n";
                        text += "  Package path:  " + node.textures[index].list[index2].path + "\n";
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
                    else if (foundTexture.name == name)
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
                    ListViewItem item = new ListViewItem(foundTexture.name + " (" + foundTexture.packageName + ")");
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
            detailedInfo = false;
            richTextBoxInfo.Show();
            pictureBoxPreview.Hide();
            previewShow = false;
            updateViewFromListView();
        }

        private void info2TextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            detailedInfo = true;
            richTextBoxInfo.Show();
            pictureBoxPreview.Hide();
            previewShow = false;
            updateViewFromListView();
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            detailedInfo = false;
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
            if (!replaceTextureToolStripMenuItem.Enabled)
                return;
            _mainWindow.updateStatusLabel("Replacing texture...");
            _mainWindow.updateStatusLabel2("");
            Misc.startTimer();
            replaceTexture();
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
            replaceTexture();
            var time = Misc.stopTimer();
            _mainWindow.updateStatusLabel("Done. Process total time: " + Misc.getTimerFormat(time));
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
            loadMODsToolStripMenuItem.Enabled = !enable;
            clearMODsToolStripMenuItem.Enabled = false;
            packMODToolStripMenuItem.Enabled = !enable;
            miscToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void switchModsMode(bool enable)
        {
            loadMODsToolStripMenuItem.Enabled = enable;
            clearMODsToolStripMenuItem.Enabled = enable;
            packMODToolStripMenuItem.Enabled = true;
            searchToolStripMenuItem.Enabled = !enable;
            miscToolStripMenuItem.Enabled = !enable;
            replaceTextureToolStripMenuItem.Enabled = !enable;
            viewToolStripMenuItem.Enabled = !enable;
            extractToDDSFileToolStripMenuItem.Enabled = !enable;
            Application.DoEvents();
        }

        private void loadMODsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog modFile = new OpenFileDialog())
            {
                modFile.Title = "Please select Mod file";
                modFile.Filter = "MOD file | *.mem";
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
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != TextureModTag || version != TextureModVersion)
                        {
                            if (version != TextureModVersion)
                                MessageBox.Show("File " + file + "  was made with an older version of MEM, skipping...");
                            else
                                MessageBox.Show("File " + file + " is not a valid MEM mod, skipping...");
                            continue;
                        }
                    }
                    string desc = Path.GetFileNameWithoutExtension(file);
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
            richTextBoxInfo.Text = "";
            string log = "";

            long diskFreeSpace = Misc.getDiskFreeSpace(GameData.GamePath);
            long diskUsage = 0;
            foreach (ListViewItem item in listViewMods.SelectedItems)
            {
                diskUsage += new FileInfo(item.Name).Length;
            }
            diskUsage = (long)(diskUsage * 2.5);
            if (diskUsage < diskFreeSpace)
            {
                string errors = "";
                Misc.startTimer();
                foreach (ListViewItem item in listViewMods.SelectedItems)
                {
                    errors += mipMaps.replaceTextureMod(item.Name, _textures, cachePackageMgr, this, ref log);
                    _mainWindow.updateStatusLabel("MOD: " + item.Text + " applying...");
                    listViewMods.Items.Remove(item);
                }
                cachePackageMgr.CloseAllWithSave();
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
            }
            else
            {
                MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
            }
            EnableMenuOptions(true);
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

            string log = "";
            richTextBoxInfo.Text = mipMaps.listTextureMod(listViewMods.SelectedItems[0].Name, _textures, cachePackageMgr, this, ref log);
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
                return;

            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
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
                    if (diskUsage < diskFreeSpace)
                    {
                        Misc.startTimer();
                        richTextBoxInfo.Text = "";
                        string log = "";
                        foreach (ListViewItem item in listViewMods.SelectedItems)
                        {
                            string outDir = Path.Combine(modFile.SelectedPath, Path.GetFileNameWithoutExtension(item.Name));
                            Directory.CreateDirectory(outDir);
                            _mainWindow.updateStatusLabel("MOD: " + item.Text + "extracting...");
                            _mainWindow.updateStatusLabel2("");
                            richTextBoxInfo.Text += mipMaps.extractTextureMod(item.Name, outDir, _textures, cachePackageMgr, this, ref log);
                        }
                        var time = Misc.stopTimer();
                        _mainWindow.updateStatusLabel("MODs extracted. Process total time: " + Misc.getTimerFormat(time));
                        _mainWindow.updateStatusLabel2("");
                        if (richTextBoxInfo.Text != "")
                        {
                            richTextBoxInfo.Show();
                            pictureBoxPreview.Hide();
                            MessageBox.Show("WARNING: Some errors have occured!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("You have not enough disk space remaining. You need about " + Misc.getBytesFormat(diskUsage) + " free.");
                    }
                }
            }
            EnableMenuOptions(true);
            _mainWindow.updateStatusLabel("");
            _mainWindow.updateStatusLabel2("");
        }

        private void createMODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);

            using (FolderBrowserDialog modFile = new FolderBrowserDialog())
            {
                modFile.SelectedPath = gameData.lastCreateMODPath;
                if (modFile.ShowDialog() == DialogResult.OK)
                {
                    gameData.lastCreateMODPath = modFile.SelectedPath;
                    long diskUsage = new DirectoryInfo(modFile.SelectedPath).GetFiles("*.dds").ToList().Sum(file => file.Length);
                    long diskFreeSpace = Misc.getDiskFreeSpace(modFile.SelectedPath);
                    diskUsage = (long)(diskUsage / 1.5);
                    if (diskUsage < diskFreeSpace)
                    {
                        Misc.startTimer();
                        _mainWindow.updateStatusLabel("MOD packing...");
                        _mainWindow.updateStatusLabel2("");
                        string log = "";
                        richTextBoxInfo.Text = mipMaps.createTextureMod(modFile.SelectedPath,
                            Path.Combine(Path.GetDirectoryName(modFile.SelectedPath), Path.GetFileName(modFile.SelectedPath)) + ".mem",
                            _textures, _mainWindow, ref log);
                        var time = Misc.stopTimer();
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
                    }
                    diskUsage = (long)(diskUsage / 1.5);

                    if (diskUsage < diskFreeSpace)
                    {
                        Misc.startTimer();
                        _mainWindow.updateStatusLabel("MODs packing...");
                        _mainWindow.updateStatusLabel2("");
                        string log = "";
                        richTextBoxInfo.Text = "";
                        richTextBoxInfo.Show();
                        pictureBoxPreview.Hide();
                        for (int i = 0; i < listDirs.Count; i++)
                        {
                            richTextBoxInfo.Text += mipMaps.createTextureMod(listDirs[i], Path.Combine(Path.GetDirectoryName(listDirs[i]), Path.GetFileName(listDirs[i])) + ".mem",
                                _textures, _mainWindow, ref log);
                        }
                        var time = Misc.stopTimer();
                        if (richTextBoxInfo.Text != "")
                        {
                            MessageBox.Show("WARNING: Some errors have occured!");
                        }
                        _mainWindow.updateStatusLabel("MODs packed. Process total time: " + Misc.getTimerFormat(time));
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

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Please enter texture name or CRC.\n\nYou can use * as wilcards.", "Search Texture", "", 0, 0);
            if (string.IsNullOrEmpty(name))
                return;

            uint crc = 0;
            string crcStr = name.ToLower();
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

        private void dumpAllTexturesMipmapsInfoTXTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog logFile = new SaveFileDialog())
            {
                logFile.Title = "Please select output TXT file";
                logFile.Filter = "TXT file | *.txt";
                if (logFile.ShowDialog() != DialogResult.OK)
                    return;
                string filename = logFile.FileName;
                if (File.Exists(filename))
                    File.Delete(filename);

                EnableMenuOptions(false);
                _mainWindow.updateStatusLabel("Generating textures information...");
                _mainWindow.updateStatusLabel2("");

                GameData.packageFiles.Sort();

                using (FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
                {
                    for (int l = 0; l < GameData.packageFiles.Count; l++)
                    {
                        _mainWindow.updateStatusLabel("Generating textures information from package " + (l + 1) + " of " + GameData.packageFiles.Count);
                        _mainWindow.updateStatusLabel2("");
                        Package package = new Package(GameData.packageFiles[l]);
                        fs.WriteStringASCII("--- Package: " + Path.GetFileName(GameData.packageFiles[l]) + " ---\n");
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

                                fs.WriteStringASCII("Texture: " + package.exportsTable[i].objectName);
                                for (int m = 0; m < texture.mipMapsList.Count; m++)
                                {
                                    fs.WriteStringASCII(", " + texture.mipMapsList[m].width + " x " + texture.mipMapsList[m].height +
                                        " CRC: " + string.Format("0x{0:X8}", texture.getCrcMipmap(texture.mipMapsList[m])));
                                }
                                fs.WriteStringASCII("\n");
                            }
                        }
                        package.Dispose();
                    }
                }
                _mainWindow.updateStatusLabel("Generating textures information completed.");
                _mainWindow.updateStatusLabel2("");
                EnableMenuOptions(true);
            }
        }

        private void repackTexturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            DialogResult result = MessageBox.Show("Repacking textures process can take a lot of time.\n\n" +
                "Are you sure you want to proceed?", "Textures repacking (WIP feature!)", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                new Repack().RepackTexturesTFC(_textures, _mainWindow, cachePackageMgr);
            }
            EnableMenuOptions(true);
        }

        private void extractToDDSFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EnableMenuOptions(false);
            if (listViewTextures.SelectedItems.Count == 0)
                return;

            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                ListViewItem item = listViewTextures.FocusedItem;
                int index = Convert.ToInt32(item.Name);
                MatchedTexture nodeTexture = node.textures[index].list[0];
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

            using (SaveFileDialog saveFile = new SaveFileDialog())
            {
                PackageTreeNode node = (PackageTreeNode)treeViewPackages.SelectedNode;
                ListViewItem item = listViewTextures.FocusedItem;
                int index = Convert.ToInt32(item.Name);
                MatchedTexture nodeTexture = node.textures[index].list[0];
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
    }
}
