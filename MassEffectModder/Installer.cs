using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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


            labelStatusDetected.Text = "Progress...";
            configIni = new ConfIni();
            gameData = new GameData((MeType)gameId, configIni);
            checkBoxDetected.Checked = true;
            labelStatusDetected.Text = "";


            labelStatusLOD.Text = "Progress...";
            LODSettings.updateLOD((MeType)gameId, configIni);
            checkBoxLOD.Checked = true;
            labelStatusLOD.Text = "";


            updateStatusPrepare("Progress...");
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


            updateStatusScan("Progress...");
            textures = treeScan.PrepareListOfTextures(null, null, this);
            checkBoxScan.Checked = true;
            updateStatusScan("");


            updateStatusMipMaps("Progress...");
//            mipMaps.removeMipMaps(textures, cachePackageMgr, null, this);
            checkBoxMipMaps.Checked = true;
            updateStatusMipMaps("");


            updateStatusTextures("Progress...");
            string errors = "";
            for (int i = 0; i < modFiles.Count; i++)
            {
                mipMaps.replaceTextureMod(modFiles[i], errors, textures, cachePackageMgr, null);
                updateStatusTextures("Progress... " + (i * 100 / GameData.packageFiles.Count) + " % ");
            }
            checkBoxTextures.Checked = true;
            updateStatusTextures("");


            updateStatusStore("Progress...");
            cachePackageMgr.CloseAllWithSave();
            checkBoxStore.Checked = true;
            updateStatusStore("");
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
