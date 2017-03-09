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
using System.Collections.Generic;
using System.Diagnostics;
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

        [DllImport("kernel32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int FreeLibrary(IntPtr hModule);

        private static List<string> dlls = new List<string>();
        private static string dllPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        static void loadEmbeddedDlls()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            for (int l = 0; l < resources.Length; l++)
            {
                if (resources[l].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    string dllName = resources[l].Split('.')[2] + ".dll";
                    string dllFilePath = Path.Combine(dllPath, dllName);
                    if (!Directory.Exists(dllPath))
                        Directory.CreateDirectory(dllPath);

                    using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                    {
                        byte[] buf = s.ReadToBuffer(s.Length);
                        if (File.Exists(dllFilePath))
                            File.Delete(dllFilePath);
                        File.WriteAllBytes(dllFilePath, buf);
                    }

                    IntPtr handle = LoadLibrary(dllFilePath);
                    if (handle == IntPtr.Zero)
                        throw new Exception();
                    dlls.Add(dllName);
                }
            }
        }

        static void unloadEmbeddedDlls()
        {
            for (int l = 0; l < 10; l++)
            {
                foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
                {
                    if (dlls.Contains(mod.ModuleName))
                    {
                        FreeLibrary(mod.BaseAddress);
                    }
                }
            }

            try
            {
                if (Directory.Exists(dllPath))
                    Directory.Delete(dllPath, true);
            }
            catch
            {
            }
        }

        [STAThread]

        static void Main(string[] args)
        {
            loadEmbeddedDlls();

            if (args.Length == 5)
            {
                string option = args[1];
                string game = args[2];
                string inputDir = args[3];
                string outMem = args[4];
                int gameId;
                try
                {
                    gameId = int.Parse(game);
                }
                catch
                {
                    gameId = 0;
                }
                if (gameId != 0)
                {
                    if (option.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("MEM v" + Application.ProductVersion + Environment.NewLine);
                        if (!CmdLineConverter.ConvertToMEM(gameId, inputDir, outMem))
                            Environment.Exit(1);
                    }
                }
            }
            else
            {
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

            unloadEmbeddedDlls();
        }
    }
}
