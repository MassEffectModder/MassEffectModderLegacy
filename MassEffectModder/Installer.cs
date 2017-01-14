/*
 * MassEffectModder
 *
 * Copyright (C) 2016-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class Installer : Form
    {
        ConfIni configIni;
        ConfIni installerIni;
        int gameId;
        GameData gameData;
        public List<string> modFiles;
        CachePackageMgr cachePackageMgr;
        List<FoundTexture> textures;
        MipMaps mipMaps;
        TreeScan treeScan;

        public Installer()
        {
            InitializeComponent();
            mipMaps = new MipMaps();
            treeScan = new TreeScan();
            cachePackageMgr = new CachePackageMgr(null, this);
        }

        public bool Run()
        {
            installerIni = new ConfIni(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "installer.ini"));
            string gameIdStr = installerIni.Read("GameId", "Main");
            if (gameIdStr == "ME1")
                gameId = 1;
            else if (gameIdStr == "ME2")
                gameId = 2;
            else if (gameIdStr == "ME3")
                gameId = 3;
            else
            {
                MessageBox.Show("Not recognized game id in installer.ini, exiting...", "Installer");
                return false;
            }
            modFiles = Directory.GetFiles(".", "*.mem", SearchOption.AllDirectories).ToList();
            modFiles.Sort();
            if (modFiles.Count == 0)
            {
                MessageBox.Show("No mods included, exiting", "Installer");
                return false;
            }

            labelStatusDetected.Text = "";
            labelStatusLOD.Text = "";
            labelStatusPrepare.Text = "";
            labelStatusScan.Text = "";
            labelStatusMipMaps.Text = "";
            labelStatusTextures.Text = "";
            labelStatusStore.Text = "";

            return true;
        }

        private void buttonSTART_Click(object sender, EventArgs e)
        {
            buttonSTART.Enabled = false;


            configIni = new ConfIni();
            gameData = new GameData((MeType)gameId, configIni);
            if (!Directory.Exists(GameData.GamePath))
                return;

            labelStatusDetected.Text = "In progress...";
            checkBoxDetected.Checked = true;
            labelStatusDetected.Text = "";


            labelStatusLOD.Text = "In progress...";
            LODSettings.updateLOD((MeType)gameId, configIni);
            checkBoxLOD.Checked = true;
            labelStatusLOD.Text = "";


            updateStatusPrepare("In progress...");
            if (GameData.gameType == MeType.ME1_TYPE)
                Misc.VerifyME1Exe(gameData, false);

            if (GameData.gameType == MeType.ME3_TYPE)
                ME3DLC.unpackAllDLC(null, this);

            if (!gameData.getPackages(false))
            {
                labelStatusPrepare.Text = "Failed!";
                return;
            }
            if (GameData.gameType != MeType.ME1_TYPE)
            {
                if (!gameData.getTfcTextures())
                {
                    labelStatusPrepare.Text = "Failed!";
                    return;
                }
            }
            checkBoxPrepare.Checked = true;
            updateStatusPrepare("");


            updateStatusScan("In progress...");
            textures = treeScan.PrepareListOfTextures(null, null, this);
            checkBoxScan.Checked = true;
            updateStatusScan("");


            updateStatusMipMaps("In progress...");
            mipMaps.removeMipMaps(textures, cachePackageMgr, null, this);
            checkBoxMipMaps.Checked = true;
            updateStatusMipMaps("");


            updateStatusTextures("In progress...");
            string errors = "";
            for (int i = 0; i < modFiles.Count; i++)
            {
                using (FileStream fs = new FileStream(modFiles[i], FileMode.Open, FileAccess.Read))
                {
                    uint tag = fs.ReadUInt32();
                    uint version = fs.ReadUInt32();
                    if (tag != TexExplorer.TextureModTag || version != TexExplorer.TextureModVersion)
                    {
                        continue;
                    }
                    else
                    {
                        uint gameType = fs.ReadUInt32();
                        if ((MeType)gameType != GameData.gameType)
                        {
                            return;
                        }
                    }
                    int numTextures = fs.ReadInt32();
                    for (int l = 0; l < numTextures; l++)
                    {
                        string name;
                        uint crc, size, dstLen = 0, decSize = 0;
                        byte[] dst = null;
                        name = fs.ReadStringASCIINull();
                        crc = fs.ReadUInt32();
                        decSize = fs.ReadUInt32();
                        size = fs.ReadUInt32();
                        byte[] src = fs.ReadToBuffer(size);
                        dst = new byte[decSize];
                        dstLen = ZlibHelper.Zlib.Decompress(src, size, dst);

                        updateStatusTextures("Mod: " + (i + 1) + " of " + modFiles.Count + " - in progress: " + ((l + 1) * 100 / numTextures) + " % ");

                        FoundTexture foundTexture;
                        if (GameData.gameType == MeType.ME1_TYPE)
                            foundTexture = textures.Find(s => s.crc == crc && s.name == name);
                        else
                            foundTexture = textures.Find(s => s.crc == crc);
                        if (foundTexture.crc != 0)
                        {
                            DDSImage image = new DDSImage(new MemoryStream(dst, 0, (int)dstLen));
                            if (!image.checkExistAllMipmaps())
                            {
                                errors += "Not all mipmaps exists in texture: " + name + string.Format("_0x{0:X8}", crc) + Environment.NewLine;
                                continue;
                            }
                            mipMaps.replaceTexture(image, foundTexture.list, cachePackageMgr, foundTexture.name, errors);
                        }
                        else
                        {
                            errors += "Not matched texture: " + name + string.Format("_0x{0:X8}", crc) + Environment.NewLine;
                        }
                    }
                }
            }
            checkBoxTextures.Checked = true;
            updateStatusTextures("");


            updateStatusStore("Progress...");
            cachePackageMgr.CloseAllWithSave();
            checkBoxStore.Checked = true;
            updateStatusStore("");

            string filename = "errors.txt";
            File.Delete(filename);
            if (errors != "")
            {
                using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
                {
                    fs.WriteStringASCII(errors);
                }
                MessageBox.Show("There were some errors while process.");
                Process.Start(filename);
            }
        }

        public void updateStatusPrepare(string text)
        {
            labelStatusPrepare.Text = text;
            Application.DoEvents();
        }

        public void updateStatusScan(string text)
        {
            labelStatusScan.Text = text;
            Application.DoEvents();
        }

        public void updateStatusMipMaps(string text)
        {
            labelStatusMipMaps.Text = text;
            Application.DoEvents();
        }

        public void updateStatusTextures(string text)
        {
            labelStatusTextures.Text = text;
            Application.DoEvents();
        }

        public void updateStatusStore(string text)
        {
            labelStatusStore.Text = text;
            Application.DoEvents();
        }

    }
}
