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

using StreamHelpers;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MassEffectModder
{
    static class Program
    {
        [DllImport("kernel32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        static void loadEmbeddedDlls()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            for (int l = 0; l < resources.Length; l++)
            {
                if (resources[l].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    string dllName = resources[l].Split('.')[2] + ".dll";
                    string dllPath = Path.Combine(Path.GetTempPath(), dllName);

                    using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                    {
                        byte[] buf = s.ReadToBuffer(s.Length);
                        if (File.Exists(dllPath))
                            File.Delete(dllPath);
                        File.WriteAllBytes(dllPath, buf);
                    }

                    if (LoadLibrary(dllPath) == IntPtr.Zero)
                        throw new Exception();
                }
            }
        }

        [STAThread]

        static void Main()
        {
            loadEmbeddedDlls();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool runAsAdmin = false;

            if (Misc.isRunAsAdministrator())
            {
                runAsAdmin = true;
            }

            string iniPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "installer.ini");
            if (File.Exists(iniPath))
            {
                Installer installer = new Installer();
                if (installer.Run(runAsAdmin))
                    Application.Run(installer);
                if (installer.exitToModder)
                    Application.Run(new MainWindow(runAsAdmin));
            }
            else
                Application.Run(new MainWindow(runAsAdmin));
        }
    }
}
