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

using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Windows.Forms;

namespace MassEffectModder
{
    public class CachePackageMgr
    {
        public List<Package> packages;
        MainWindow mainWindow;
        static Installer _installer;

        public CachePackageMgr(MainWindow main, Installer installer)
        {
            packages = new List<Package>();
            mainWindow = main;
            _installer = installer;
        }

        public Package OpenPackage(string path)
        {
            if (!packages.Exists(p => p.packagePath.ToLowerInvariant() == path.ToLowerInvariant()))
            {
                Package pkg = new Package(path);
                packages.Add(pkg);
                return pkg;
            }
            else
            {
                return packages.Find(p => p.packagePath.ToLowerInvariant() == path.ToLowerInvariant());
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

        public void CloseAllWithSave(bool repack = false, bool appendMarker = true)
        {
            int skipCounter = 0;
            bool lowMemoryMode = false;
            ulong memorySize = ((new ComputerInfo().TotalPhysicalMemory / 1024 / 1024) + 1023) / 1024;
            if (memorySize <= 12)
                lowMemoryMode = true;

            for (int i = 0; i < packages.Count; i++)
            {
                Package pkg = packages[i];
                if (mainWindow != null)
                    mainWindow.updateStatusLabel2("Saving package " + (i + 1) + " of " + packages.Count);
                if (_installer != null)
                    _installer.updateProgressStatus("Saving packages " + ((i + 1) * 100 / packages.Count) + "%");
                if (skipCounter > 10 && lowMemoryMode)
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                    skipCounter = 0;
                }
                skipCounter++;

                if (pkg.SaveToFile(repack, false, appendMarker))
                {
                    if (repack && Installer.pkgsToRepack != null)
                        Installer.pkgsToRepack.Remove(pkg.packagePath);
                    if (appendMarker && Installer.pkgsToMarker != null)
                        Installer.pkgsToMarker.Remove(pkg.packagePath);
                }
                pkg.Dispose();
            }

            if (mainWindow != null)
                mainWindow.updateStatusLabel2("");
            packages.Clear();
        }
    }
}
