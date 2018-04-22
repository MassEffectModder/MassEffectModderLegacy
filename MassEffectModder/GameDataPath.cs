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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public class GameData
    {
        static private string _path = null;
        static public MeType gameType;
        static private ConfIni _configIni;
        static public List<string> packageFiles;
        static public List<string> tfcFiles;
        static public bool FullScanME1Game = false;

        public bool DLCDataCacheDone = false;

        public GameData(MeType type, ConfIni configIni, bool force = false, bool installerMode = false)
        {
            gameType = type;
            _configIni = configIni;

            string key = "ME" + (int)gameType;
            string path = configIni.Read(key, "GameDataPath");
            if (path != null && path != "" && !force)
            {
                _path = path.TrimEnd(Path.DirectorySeparatorChar);
                if (File.Exists(GameExePath))
                    return;
                else
                    _path = null;
            }

            string registryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\BioWare\Mass Effect";
            string entry = "Path";

            if (type == MeType.ME2_TYPE)
                registryKey += " 2";
            else if (type == MeType.ME3_TYPE)
            {
                registryKey += " 3";
                entry = "Install Dir";
            }

            path = (string)Registry.GetValue(registryKey, entry, null);
            if (path != null && !force)
            {
                _path = path.TrimEnd(Path.DirectorySeparatorChar);
                if (File.Exists(GameExePath))
                {
                    configIni.Write(key, _path, "GameDataPath");
                    return;
                }
                else
                    _path = null;
            }

            if (!installerMode)
            {
                OpenFileDialog selectExe = new OpenFileDialog();
                selectExe.Title = "Please select the Mass Effect " + (int)gameType + " executable file";
                if (_path != null)
                    selectExe.FileName = _path;
                switch (gameType)
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
                    if (gameType == MeType.ME3_TYPE)
                        _path = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(selectExe.FileName)));
                    else
                        _path = Path.GetDirectoryName(Path.GetDirectoryName(selectExe.FileName));
                }
            }
            if (_path != null)
                _configIni.Write(key, _path, "GameDataPath");
        }

        static public string GamePath
        {
            get
            {
                return _path;
            }
        }

        static public string MainData
        {
            get
            {
                if (_path != null)
                {
                    switch (gameType)
                    {
                        case MeType.ME1_TYPE:
                        case MeType.ME2_TYPE:
                            return Path.Combine(_path, @"BioGame\CookedPC");
                        case MeType.ME3_TYPE:
                            return Path.Combine(_path, @"BioGame\CookedPCConsole");
                        default:
                            return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        static public string bioGamePath
        {
            get
            {
                if (_path != null)
                {
                    switch (gameType)
                    {
                        case MeType.ME1_TYPE:
                        case MeType.ME2_TYPE:
                            return Path.Combine(_path, @"BioGame");
                        case MeType.ME3_TYPE:
                            return Path.Combine(_path, @"BIOGame");
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }

        static public string RelativeGameData(string path)
        {
            if (_path == null || !path.ToLowerInvariant().Contains(_path.ToLowerInvariant()))
                return null;
            else
                return path.Substring(_path.Length);
        }

        static public string DLCData
        {
            get
            {
                if (_path != null)
                {
                    switch (gameType)
                    {
                        case MeType.ME1_TYPE:
                            return Path.Combine(_path, @"DLC");
                        case MeType.ME2_TYPE:
                            return Path.Combine(_path, @"BioGame\DLC");
                        case MeType.ME3_TYPE:
                            return Path.Combine(_path, @"BIOGame\DLC");
                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }

        static public string GameExePath
        {
            get
            {
                if (gameType == MeType.ME1_TYPE)
                    return Path.Combine(_path, @"Binaries\MassEffect.exe");
                else if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(_path, @"Binaries\MassEffect2.exe");
                else if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(_path, @"Binaries\Win32\MassEffect3.exe");
                else
                    return null;
            }
        }

        public string GameUserPath
        {
            get
            {
                string dir;

                if (gameType == MeType.ME1_TYPE)
                    dir = @"BioWare\Mass Effect";
                else if (gameType == MeType.ME2_TYPE)
                    dir = @"BioWare\Mass Effect 2";
                else if (gameType == MeType.ME3_TYPE)
                    dir = @"BioWare\Mass Effect 3";
                else
                    return null;

                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dir);
            }
        }

        public string ConfigIniPath
        {
            get
            {
                if (gameType == MeType.ME1_TYPE)
                    return Path.Combine(GameUserPath, @"Config");
                else if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(GameUserPath, @"BioGame\Config");
                else if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(GameUserPath, @"BioGame\Config");
                else
                    return null;
            }
        }

        public string EngineConfigIniPath
        {
            get
            {
                if (gameType == MeType.ME1_TYPE)
                    return Path.Combine(ConfigIniPath, @"BIOEngine.ini");
                else if (gameType == MeType.ME2_TYPE)
                    return Path.Combine(ConfigIniPath, @"GamerSettings.ini");
                else if (gameType == MeType.ME3_TYPE)
                    return Path.Combine(ConfigIniPath, @"GamerSettings.ini");
                else
                    return null;
            }
        }

        public bool getTfcTextures()
        {
            if (tfcFiles != null && tfcFiles.Count != 0)
                return true;

            tfcFiles = Directory.GetFiles(GamePath, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList();
            return true;
        }

        static public string lastLoadMODPath
        {
            get
            {
                return _configIni.Read("LastLoadMODPath", "Paths");
            }
            set
            {
                _configIni.Write("LastLoadMODPath", value, "Paths");
            }
        }

        public string lastSaveMODPath
        {
            get
            {
                return _configIni.Read("LastSaveMODPath", "Paths");
            }
            set
            {
                _configIni.Write("LastSaveMODPath", value, "Paths");
            }
        }

        public string lastCreateMODPath
        {
            get
            {
                return _configIni.Read("LastCreateMODPath", "Paths");
            }
            set
            {
                _configIni.Write("LastCreateMODPath", value, "Paths");
            }
        }

        static public string lastExtractMODPath
        {
            get
            {
                return _configIni.Read("LastExtractMODPath", "Paths");
            }
            set
            {
                _configIni.Write("LastExtractMODPath", value, "Paths");
            }
        }

        public bool getPackages(bool force = false, bool installerMode = false)
        {
            if (packageFiles != null && (packageFiles.Count != 0 && !force))
                return true;

            FullScanME1Game = false;

            if (gameType == MeType.ME1_TYPE)
            {
                packageFiles = Directory.GetFiles(MainData, "*.*",
                SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                if (packageFiles.FindAll(s => s.Contains("_PLPC.")).Count > 5)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("_RA.")).Count > 5)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("_RU.")).Count > 5)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("GlobalTlk_PLPC.upk")).Count > 0)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("GlobalTlk_CS.upk")).Count > 0)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("GlobalTlk_HU.upk")).Count > 0)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("GlobalTlk_RA.upk")).Count > 0)
                    FullScanME1Game = true;
                else if (packageFiles.FindAll(s => s.Contains("GlobalTlk_RU.upk")).Count > 0)
                    FullScanME1Game = true;

                if (Directory.Exists(DLCData))
                {
                    packageFiles.AddRange(Directory.GetFiles(DLCData, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                        StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)));
                }
                packageFiles.RemoveAll(s => s.ToLowerInvariant().Contains("localshadercache-pc-d3d-sm3.upk"));
                packageFiles.RemoveAll(s => s.ToLowerInvariant().Contains("refshadercache-pc-d3d-sm3.upk"));
            }
            else if (gameType == MeType.ME2_TYPE)
            {
                packageFiles = Directory.GetFiles(MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(DLCData))
                    packageFiles.AddRange(Directory.GetFiles(DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)));
            }
            else if (gameType == MeType.ME3_TYPE)
            {
                List<string> pccs = null;
                if (Directory.Exists(DLCData))
                {
                    pccs = Directory.GetFiles(DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (pccs.Count() == 0)
                    {
                        if (!installerMode)
                        {
                            MessageBox.Show("You need to extract the DLC files first.");
                            return false;
                        }
                    }
                    List<string> DLCs = Directory.GetDirectories(DLCData).ToList();
                    for (int i = 0; i < DLCs.Count; i++)
                    {
                        List<string> sfars = Directory.GetFiles(DLCs[i], "Default.sfar", SearchOption.AllDirectories).ToList();
                        if (sfars.Count == 0)
                            continue;
                        List<string> dlcs = Directory.GetFiles(DLCs[i], "Mount.dlc", SearchOption.AllDirectories).ToList();
                        if (dlcs.Count() == 0)
                        {
                            if (!installerMode)
                            {
                                MessageBox.Show("Detected compressed DLCs in DLC folder." +
                                "\nYou need to extract the DLC files first.");
                                return false;
                            }
                        }
                    }
                }

                packageFiles = Directory.GetFiles(MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (pccs != null)
                    packageFiles.AddRange(pccs);
                packageFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
            }
            return true;
        }

        void ClosePackagesList()
        {
            packageFiles.Clear();
            tfcFiles.Clear();
        }
    }
}
