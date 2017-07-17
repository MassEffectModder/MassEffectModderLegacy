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

        [DllImport("kernel32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static List<string> dlls = new List<string>();
        public static string dllPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public static Misc.MD5FileEntry[] entriesME1 = null;
        public static Misc.MD5FileEntry[] entriesME2 = null;
        public static Misc.MD5FileEntry[] entriesME3 = null;
        public static byte[] tableME1 = null;
        public static byte[] tableME2 = null;
        public static byte[] tableME3 = null;

        static void loadEmbeddedDlls()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            for (int l = 0; l < resources.Length; l++)
            {
                if (resources[l].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    string dllName = resources[l].Substring(resources[l].IndexOf("Dlls.") + "Dlls.".Length);
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
                else if (resources[l].EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    if (resources[l].Contains("MD5EntriesME1.bin"))
                    {
                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            tableME1 = s.ReadToBuffer(s.Length);
                        }
                    }
                    else if (resources[l].Contains("MD5EntriesME2.bin"))
                    {
                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            tableME2 = s.ReadToBuffer(s.Length);
                        }
                    }
                    else if (resources[l].Contains("MD5EntriesME3.bin"))
                    {
                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            tableME3 = s.ReadToBuffer(s.Length);
                        }
                    }
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

        static void loadMD5Tables()
        {
            MemoryStream tmp = new MemoryStream(tableME1);
            tmp.SkipInt32();
            byte[] decompressed = new byte[tmp.ReadInt32()];
            byte[] compressed = tmp.ReadToBuffer((uint)tableME1.Length - 8);
            if (ZlibHelper.Zlib.Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            tmp = new MemoryStream(decompressed);
            int count = tmp.ReadInt32();
            entriesME1 = new Misc.MD5FileEntry[count];
            for (int l = 0; l < count; l++)
            {
                entriesME1[l].md5 = tmp.ReadToBuffer(16);
                entriesME1[l].path = tmp.ReadStringASCIINull();
            }

            tmp = new MemoryStream(tableME2);
            tmp.SkipInt32();
            decompressed = new byte[tmp.ReadInt32()];
            compressed = tmp.ReadToBuffer((uint)tableME2.Length - 8);
            if (ZlibHelper.Zlib.Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            tmp = new MemoryStream(decompressed);
            count = tmp.ReadInt32();
            entriesME2 = new Misc.MD5FileEntry[count];
            for (int l = 0; l < count; l++)
            {
                entriesME2[l].md5 = tmp.ReadToBuffer(16);
                entriesME2[l].path = tmp.ReadStringASCIINull();
            }

            tmp = new MemoryStream(tableME3);
            tmp.SkipInt32();
            decompressed = new byte[tmp.ReadInt32()];
            compressed = tmp.ReadToBuffer((uint)tableME3.Length - 8);
            if (ZlibHelper.Zlib.Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            tmp = new MemoryStream(decompressed);
            count = tmp.ReadInt32();
            entriesME3 = new Misc.MD5FileEntry[count];
            for (int l = 0; l < count; l++)
            {
                entriesME3[l].md5 = tmp.ReadToBuffer(16);
                entriesME3[l].path = tmp.ReadStringASCIINull();
            }
        }

        [STAThread]

        static void Main(string[] args)
        {
            string cmd = "";
            string game;
            string inputDir;
            string outputDir;
            string inputFile;
            string outputFile;
            int gameId = 0;

            if (args.Length > 0)
            {
                cmd = args[0];
                loadEmbeddedDlls();
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    goto fail;
                }

                game = args[1];
                try
                {
                    gameId = int.Parse(game);
                }
                catch
                {
                    gameId = 0;
                }
                if (gameId != 1 && gameId != 2 && gameId != 3)
                {
                    Console.WriteLine("Error: wrong game id!");
                    goto fail;
                }
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase))
            {
                inputDir = args[2];
                outputFile = args[3];
                if (!Directory.Exists(inputDir))
                {
                    Console.WriteLine("Error: input path not exists: " + inputDir);
                    goto fail;
                }
                else
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.ConvertToMEM(gameId, inputDir, outputFile))
                    {
                        goto fail;
                    }
                }
            }
            else if (cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase))
            {
                inputDir = args[2];
                outputDir = args[3];
                if (!Directory.Exists(inputDir))
                {
                    Console.WriteLine("Error: input dir not exists: " + inputDir);
                    goto fail;
                }
                else
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.extractMOD(gameId, inputDir, outputDir))
                    {
                        goto fail;
                    }
                }
            }
            else if (cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase))
            {
                inputFile = args[2];
                outputFile = args[3];
                if (!Directory.Exists(inputFile))
                {
                    Console.WriteLine("Error: input file not exists: " + inputFile);
                    goto fail;
                }
                else
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.convertGameImage(gameId, inputFile, outputFile))
                    {
                        goto fail;
                    }
                }
            }
            else if (cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase))
            {
                inputDir = args[2];
                outputDir = args[3];
                if (!Directory.Exists(inputDir))
                {
                    Console.WriteLine("Error: input dir not exists: " + inputDir);
                    goto fail;
                }
                else
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.convertGameImages(gameId, inputDir, outputDir))
                    {
                        goto fail;
                    }
                }
            }
            else if (cmd.Equals("-extract-tpf", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    goto fail;
                }

                inputDir = args[1];
                outputDir = args[2];
                if (!Directory.Exists(inputDir))
                {
                    Console.WriteLine("Error: input dir not exists: " + inputDir);
                    goto fail;
                }
                else
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.extractTPF(inputDir, outputDir))
                    {
                        goto fail;
                    }
                }
            }
            else if (cmd.Equals("-convert-image", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    goto fail;
                }

                string format = args[1];
                inputFile = args[2];
                outputFile = args[3];
                if (!Directory.Exists(inputFile))
                {
                    Console.WriteLine("Error: input file not exists: " + inputFile);
                    goto fail;
                }
                else
                {
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.convertImage(inputFile, outputFile, format))
                    {
                        goto fail;
                    }
                }
            }
            else
            {
                ShowWindow(GetConsoleWindow(), 0);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                bool runAsAdmin = false;

                if (Misc.isRunAsAdministrator())
                {
                    runAsAdmin = true;
                }

                loadEmbeddedDlls();

                loadMD5Tables();

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
            Environment.Exit(0);
fail:
            unloadEmbeddedDlls();
            Environment.Exit(0);
        }
    }
}
