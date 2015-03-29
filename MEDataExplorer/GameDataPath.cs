/*
 * MEDataExplorer
 *
 * Copyright (C) 2014-2015 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;

namespace MEDataExplorer
{
    public class GameData
    {
        private string _path = null;
        private MeType _gameType;
        private ConfIni _configIni;

        public bool DLCDataCacheDone = false;

        public GameData(MeType type, ConfIni configIni)
        {
            _gameType = type;
            _configIni = configIni;

            var key = "ME" + (int)_gameType;
            var path = configIni.Read(key, "GameDataPath");
            if (path != null && path != "")
            {
                _path = path;
                return;
            }

            string softwareKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string key64 = @"\Wow6432Node\";
            string gameKey = @"BioWare\Mass Effect";

            if (type == MeType.ME2_TYPE)
                gameKey += @" 2";
            else if (type == MeType.ME3_TYPE)
                gameKey += @" 3";

            path = (string)Registry.GetValue(softwareKey + gameKey, "Path", null);
            if (path == null)
                path = (string)Registry.GetValue(softwareKey + key64 + gameKey, "Path", null);
            if (path != null)
            {
                _path = path + @"\";
                configIni.Write(key, path, "GameDataPath");
                return;
            }

            OpenFileDialog selectExe = new OpenFileDialog();
            selectExe.Title = "Please select the Mass Effect " + (int)_gameType + " executable file";
            switch (_gameType)
            {
                case MeType.ME1_TYPE:
                    selectExe.Filter = "ME1 exe file|MassEffect.exe";
                    selectExe.FileName = "MassEffect.exe";
                    break;
                case MeType.ME2_TYPE:
                    selectExe.Filter = "ME2 exe file|MassEffect2.exe";
                    selectExe.FileName = "MassEffect2.exe";
                    break;
                case MeType.ME3_TYPE:
                    selectExe.Filter = "ME3 exe file|MassEffect3.exe";
                    selectExe.FileName = "MassEffect3.exe";
                    break;
            }
            if (selectExe.ShowDialog() == DialogResult.OK)
            {
                if (_gameType == MeType.ME3_TYPE)
                    _path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(selectExe.FileName))) + @"\";
                else
                    _path = Path.GetDirectoryName(Path.GetDirectoryName(selectExe.FileName)) + @"\";
            }
            if (_path != null)
                _configIni.Write(key, _path, "GameDataPath");
        }

        public string GamePath
        {
            get
            {
                return _path;
            }
        }

        public string MainData
        {
            get
            {
                if (_path != null)
                {
                    switch (_gameType)
                    {
                        case MeType.ME1_TYPE:
                        case MeType.ME2_TYPE:
                            return _path + @"\BioGame\CookedPC\";
                        case MeType.ME3_TYPE:
                            return _path + @"\BioGame\CookedPCConsole\";
                        default:
                            return null;
                    }
                } 
                else
                    return null;
            }
        }

        public string DLCData
        {
            get
            {
                if (_path != null)
                {
                    switch (_gameType)
                    {
                        case MeType.ME1_TYPE:
                            return _path + @"\DLC\";
                        case MeType.ME2_TYPE:
                        case MeType.ME3_TYPE:
                            return _path + @"\BioGame\DLC\";
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }

        public string DLCDataCache
        {
            get
            {
                if (_gameType == MeType.ME3_TYPE)
                    return _path + @"\BioGame\DLCCache\";
                else
                    return null;
            }
        }

        public string GameExePath
        {
            get
            {
                if (_gameType == MeType.ME1_TYPE)
                    return _path + @"\Binaries\MassEffect.exe";
                else if (_gameType == MeType.ME2_TYPE)
                    return _path + @"\Binaries\MassEffect2.exe";
                else if (_gameType == MeType.ME3_TYPE)
                    return _path + @"\Binaries\Win32\MassEffect3.exe";
                else
                    return null;
            }
        }

        public string GameUserPath
        {
            get
            {
                string dir = @"\BioWare\Mass Effect";

                if (_gameType == MeType.ME2_TYPE)
                    dir += @" 2\";
                else if (_gameType == MeType.ME3_TYPE)
                    dir += @" 3\";

                return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + dir;
            }
        }

        public string EngineConfigIniPath
        {
            get
            {
                if (_gameType == MeType.ME1_TYPE)
                    return GameUserPath + @"\Config\BIOEngine.ini";
                else
                    return null;
            }
        }
    }
}
