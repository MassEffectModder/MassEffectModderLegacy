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
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class TexExplorer : Form
    {
        public class CachePackageMgr
        {
            public List<Package> packages;
            MainWindow mainWindow;

            public CachePackageMgr(MainWindow main)
            {
                packages = new List<Package>();
                mainWindow = main;
            }

            public Package OpenPackage(string path, bool headerOnly = false)
            {
                if (!packages.Exists(p => p.packagePath == path))
                {
                    Package pkg = new Package(path, headerOnly);
                    packages.Add(pkg);
                    return pkg;
                }
                else
                {
                    return packages.Find(p => p.packagePath == path);
                }
            }

            public void ClosePackageWithoutSave(Package package)
            {
                int index = packages.IndexOf(package);
                packages[index].Dispose();
                packages.RemoveAt(index);
            }

            public void CloseAllWithoutSave()
            {
                foreach (Package pkg in packages)
                {
                    pkg.Dispose();
                }
                packages.Clear();
            }

            public void CloseAllWithSave()
            {
                for (int i = 0; i < packages.Count; i++)
                {
                    Package pkg = packages[i];
                    mainWindow.updateStatusLabel2("Saving package " + (i + 1) + " of " + packages.Count + " - " + pkg.packagePath);
                    pkg.SaveToFile();
                    pkg.Dispose();
                }

                if (GameData.gameType == MeType.ME3_TYPE)
                {
                    updateMainTOC();
                    updateDLCsTOC();
                }

                mainWindow.updateStatusLabel2("");
                packages.Clear();
            }

            public void updateMainTOC()
            {
                List<string> mainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).ToList();
                mainFiles.AddRange(Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories).ToList());
                TOCBinFile tocFile = new TOCBinFile(Path.Combine(GameData.bioGamePath, "PCConsoleTOC.bin"));
                for (int i = 0; i < mainFiles.Count; i++)
                {
                    int pos = mainFiles[i].IndexOf("BioGame", StringComparison.OrdinalIgnoreCase);
                    string filename = mainFiles[i].Substring(pos);
                    tocFile.updateFile(filename, mainFiles[i]);
                }
                tocFile.saveToFile(Path.Combine(GameData.bioGamePath, @"PCConsoleTOC.bin"));
            }

            public void updateDLCsTOC()
            {
                List<string> DLCs = Directory.GetDirectories(GameData.DLCData, "DLC_*").ToList();
                for (int i = 0; i < DLCs.Count; i++)
                {
                    List<string> dlcFiles = Directory.GetFiles(DLCs[i], "*.pcc", SearchOption.AllDirectories).ToList();
                    if (dlcFiles.Count == 0)
                        continue;
                    dlcFiles.AddRange(Directory.GetFiles(DLCs[i], "*.tfc", SearchOption.AllDirectories).ToList());
                    string DLCname = Path.GetFileName(DLCs[i]);
                    TOCBinFile tocDLC = new TOCBinFile(Path.Combine(GameData.DLCData, DLCname, "PCConsoleTOC.bin"));
                    for (int f = 0; f < dlcFiles.Count; f++)
                    {
                        int pos = dlcFiles[f].IndexOf(DLCname + "\\", StringComparison.OrdinalIgnoreCase);
                        string filename = dlcFiles[f].Substring(pos + DLCname.Length + 1);
                        tocDLC.updateFile(filename, dlcFiles[f]);
                    }
                    tocDLC.saveToFile(Path.Combine(GameData.DLCData, DLCname, "PCConsoleTOC.bin"));
                }
            }
        }
    }
}
