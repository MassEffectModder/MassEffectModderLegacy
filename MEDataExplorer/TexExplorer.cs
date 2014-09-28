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
        IniConf _configIni;
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
                var path = _gameData.GamerSettingsIniPath;
                var exist = File.Exists(path);
                // TODO: check/update for texture max size
            }
        }

        private void TexExplorer_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mainWindow.enableGameDataMenu(true);
        }
    }
}
