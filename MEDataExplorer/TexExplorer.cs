/*
 * MEDataExplorer
 *
 * Copyright (C) 2014 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MEDataExplorer
{
    public partial class TexExplorer : Form
    {
        MeType _gameSelected;
        MainWindow _mainWindow;
        ConfIni _configIni;
        GameData _gameData;

        public TexExplorer(MainWindow main)
        {
            InitializeComponent();
            _mainWindow = main;
        }

        public void Run(MeType gameType)
        {
            _gameSelected = gameType;
            _configIni = _mainWindow._configIni;
            _gameData = new GameData(gameType, _configIni);
            if (_gameSelected == MeType.ME1_TYPE)
            {
                var path = _gameData.EngineConfigIniPath;
                var exist = File.Exists(path);
                if (!exist)
                    return;
                ConfIni engineConf = new ConfIni(path);
                var str = engineConf.Read("TEXTUREGROUP_Character_Diff", "TextureLODSettings");
                if (str != "(MinLODSize=512,MaxLODSize=4096,LODBias=0)")
                {
                    engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                }

                if (!File.Exists(_gameData.GameExePath))
                    throw new FileNotFoundException("Game exe not found: " + _gameData.GameExePath);

                using (FileStream fs = new FileStream(_gameData.GameExePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(0x146, SeekOrigin.Begin); // offset to byte with LAA flag
                    var flag = fs.ReadByte();
                    if (flag == 0x02)
                        MessageBox.Show("Warning: Large Aware Address flag is not enabled on MassEffect.exe file.");
                    else if (flag == 0x22)
                        ; // LAA flag enabled
                    else
                        throw new Exception("Not expected flags in exe file");
                }

            }
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mainWindow.enableGameDataMenu(true);
        }
    }
}
