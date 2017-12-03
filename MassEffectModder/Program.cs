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
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
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
        private static Form progressForm;
        private static ProgressBar progressBar;

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
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
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
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
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
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
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

        static private string prepareForUpdate(bool nogui = false, bool onlycheck = false)
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers["Accept"] = "application/vnd.github.v3+json";
                webClient.Headers["user-agent"] = "MEM";
                string releaseString = webClient.DownloadString("https://api.github.com/repos/MassEffectModder/MassEffectModder/releases/latest");
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                var parsed = serializer.Deserialize<Dictionary<string, dynamic>>(releaseString);
                string githubVersion = parsed["tag_name"];
                if (int.Parse(githubVersion) > int.Parse(Application.ProductVersion))
                {
                    if (onlycheck)
                    {
                        return parsed["assets"][0]["browser_download_url"];
                    }
                    if (!nogui)
                    {
                        if (MessageBox.Show("New version of MEM is available: " + githubVersion + "\nDo you want update to new version?", "Update", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        {
                            return "";
                        }
                        progressBar = new ProgressBar();
                        progressBar.Size = new System.Drawing.Size(400, 40);
                        progressBar.Style = ProgressBarStyle.Marquee;
                        progressBar.MarqueeAnimationSpeed = 1;
                        progressForm = new Form();
                        progressForm.StartPosition = FormStartPosition.CenterScreen;
                        progressForm.Size = new System.Drawing.Size(400, 60);
                        progressForm.MinimumSize = new System.Drawing.Size(400, 60);
                        progressForm.MaximumSize = new System.Drawing.Size(400, 60);
                        progressForm.MinimizeBox = false;
                        progressForm.MaximizeBox = false;
                        progressForm.ControlBox = false;
                        progressForm.Controls.Add(progressBar);
                        progressForm.Show();
                    }

                    Uri url = new Uri(parsed["assets"][0]["browser_download_url"]);
                    string filename = parsed["assets"][0]["name"];
                    webClient.DownloadFileAsync(url, filename);
                    while (webClient.IsBusy) { Application.DoEvents(); Thread.Sleep(10); }

                    if (!nogui)
                        progressForm.Close();
                    return filename;
                }
            }
            catch
            {
                MessageBox.Show("Failed checking latest available program version!");
            }
            return "";
        }

        static private string unpackUpdate(string file)
        {
            IntPtr handle = IntPtr.Zero;
            int result;
            ulong numEntries = 0;
            string fileName = "";
            string fileNameExe = "";
            ulong dstLen = 0;
            ZlibHelper.Zip zip = new ZlibHelper.Zip();
            try
            {
                byte[] buffer = File.ReadAllBytes(file);
                handle = zip.Open(buffer, ref numEntries, 0);
                for (uint i = 0; i < numEntries; i++)
                {
                    result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                    if (result != 0)
                        throw new Exception();

                    byte[] data = new byte[dstLen];
                    result = zip.ReadCurrentFile(handle, data, dstLen);
                    if (result != 0)
                    {
                        throw new Exception();
                    }
                    if (File.Exists("new-" + fileName))
                        File.Delete("new-" + fileName);
                    using (FileStream fs = new FileStream("new-" + fileName, FileMode.CreateNew))
                    {
                        fs.WriteFromBuffer(data);
                    }
                    if (Path.GetExtension(fileName).ToLowerInvariant() == ".exe")
                        fileNameExe = "new-" + fileName;

                    zip.GoToNextFile(handle);
                }
            }
            catch
            {
                MessageBox.Show("Failed unpack update!");
            }

            if (handle != IntPtr.Zero)
                zip.Close(handle);

            File.Delete(file);

            return fileNameExe;
        }

        static private void cleanupPreviousUpdate()
        {
            string baseName = "new-" + Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            string fileExe = baseName + ".exe";
            string filePdb = baseName + ".pdb";
            if (File.Exists(fileExe))
                File.Delete(fileExe);
            if (File.Exists(filePdb))
                File.Delete(filePdb);
        }

        static void DisplayHelp()
        {
            Console.WriteLine(Environment.NewLine + Environment.NewLine +
                "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
            Console.WriteLine("Help:\n");
            Console.WriteLine("  -help\n");
            Console.WriteLine("     This help");
            Console.WriteLine("");
            Console.WriteLine("  -version\n");
            Console.WriteLine("     Display MEM version");
            Console.WriteLine("");
            Console.WriteLine("  -get-installed-games");
            Console.WriteLine("     Return bitmask installed games");
            Console.WriteLine("     bit 0 - ME1");
            Console.WriteLine("     bit 1 - ME2");
            Console.WriteLine("     bit 2 - ME3");
            Console.WriteLine("");
            Console.WriteLine("  -check-update");
            Console.WriteLine("     Return 0 if update is available, 1 if not.");
            Console.WriteLine("");
            Console.WriteLine("  -download-update");
            Console.WriteLine("     Download and install MEM update.");
            Console.WriteLine("");
            Console.WriteLine("  -convert-to-mem <game id> <input dir> <output file> [-ipc]\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory to be converted, containing following file extension(s):");
            Console.WriteLine("        MEM, MOD, TPF");
            Console.WriteLine("        BIN - package export raw data");
            Console.WriteLine("           Naming pattern used for package in DLC:");
            Console.WriteLine("             D<DLC dir length>-<DLC dir>-<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("             example: D10-DLC_HEN_PR-23-BioH_EDI_02_Explore.pcc-E6101.bin");
            Console.WriteLine("           Naming pattern used for package in base directory:");
            Console.WriteLine("             B<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("             example: B23-BioH_EDI_00_Explore.pcc-E5090.bin");
            Console.WriteLine("        DDS, BMP, TGA, PNG, JPG, JPEG");
            Console.WriteLine("           input format supported for DDS images:");
            Console.WriteLine("              DXT1, DXT3, DTX5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("           input format supported for TGA images:");
            Console.WriteLine("              uncompressed RGBA/RGB, compressed RGBA/RGB");
            Console.WriteLine("           input format supported for BMP images:");
            Console.WriteLine("              uncompressed RGBA/RGB/RGBX");
            Console.WriteLine("           Image filename must include texture CRC (0xhhhhhhhh)");
            Console.WriteLine("     ipc: turn on IPC traces");
            Console.WriteLine("");
            Console.WriteLine("  -convert-game-image <game id> <input image> <output image>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     Input file with following extension:");
            Console.WriteLine("        DDS, BMP, TGA, PNG, JPG, JPEG");
            Console.WriteLine("           input format supported for DDS images:");
            Console.WriteLine("              DXT1, DXT3, DTX5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("           input format supported for TGA images:");
            Console.WriteLine("              uncompressed RGBA/RGB, compressed RGBA/RGB");
            Console.WriteLine("           input format supported for BMP images:");
            Console.WriteLine("              uncompressed RGBA/RGB/RGBX");
            Console.WriteLine("           Image filename must include texture CRC (0xhhhhhhhh)");
            Console.WriteLine("     Output file is DDS image");
            Console.WriteLine("");
            Console.WriteLine("  -convert-game-images <game id> <input dir> <output dir>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory to be converted, containing following file extension(s):");
            Console.WriteLine("        Input files with following extension:");
            Console.WriteLine("        DDS, BMP, TGA, PNG, JPEG");
            Console.WriteLine("           input format supported for DDS images:");
            Console.WriteLine("              DXT1, DXT3, DTX5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("           input format supported for TGA images:");
            Console.WriteLine("              uncompressed RGBA/RGB, compressed RGBA/RGB");
            Console.WriteLine("           input pixel format supported for BMP images:");
            Console.WriteLine("              uncompressed RGBA/RGB/RGBX");
            Console.WriteLine("           Image filename must include texture CRC (0xhhhhhhhh)");
            Console.WriteLine("     output dir: directory where textures converted to DDS are placed");
            Console.WriteLine("");
            Console.WriteLine("  -extract-mod <game id> <input dir> <output dir>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory of ME3Explorer MOD file(s)");
            Console.WriteLine("     Can extract textures and package export raw data");
            Console.WriteLine("     Naming pattern used for package in DLC:");
            Console.WriteLine("        D<DLC dir length>-<DLC dir>-<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("        example: D10-DLC_HEN_PR-23-BioH_EDI_02_Explore.pcc-E6101.bin");
            Console.WriteLine("     Naming pattern used for package in base directory:");
            Console.WriteLine("        B<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("        example: B23-BioH_EDI_00_Explore.pcc-E5090.bin");
            Console.WriteLine("");
            Console.WriteLine("  -extract-tpf <input dir> <output dir>\n");
            Console.WriteLine("     input dir: directory containing the TPF file(s) to be extracted");
            Console.WriteLine("     Textures are extracted as they are in the TPF, no additional modifications are made.");
            Console.WriteLine("");
            Console.WriteLine("  -convert-image <output pixel format> [dxt1 alpha threshold] <input image> <output image>\n");
            Console.WriteLine("     input image file types: DDS, BMP, TGA, PNG, JPEG");
            Console.WriteLine("     output image file type: DDS");
            Console.WriteLine("     output pixel format: DXT1 (no alpha), DXT1a (alpha), DXT3, DXT5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("     For DXT1a you have to set the alpha threshold (0-255). 128 is suggested as a default value.");
            Console.WriteLine("");
            Console.WriteLine("  -extract-all-dds <game id> <output dir> [TFC filter name]\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     output dir: directory where textures converted to DDS are placed");
            Console.WriteLine("     TFC filter name: it will filter only textures stored in specific TFC file.");
            Console.WriteLine("     Textures are extracted as they are in game data, only DDS header is added.");
            Console.WriteLine("");
            Console.WriteLine("  -extract-all-png <game id> <output dir>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     output dir: directory where textures converted to PNG are placed");
            Console.WriteLine("     Textures are extracted with only top mipmap.");
            Console.WriteLine("");
            Console.WriteLine("  -me3dlcmod-for-mgamerz <mem file> <tfc name> [<guid in 16 hex digits>]\n");
            Console.WriteLine("     Replace textures from <mem file> and store in new <tfc name> file.");
            Console.WriteLine("     New TFC name must be added earlier to PCC files.");
            Console.WriteLine("");

            Console.WriteLine("\n");
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
            MeType gameId = 0;
            bool ipc = false;

            if (args.Length > 0)
            {
                cmd = args[0];
                loadEmbeddedDlls();
            }

            if (cmd.Equals("-help", StringComparison.OrdinalIgnoreCase))
            {
                DisplayHelp();
                unloadEmbeddedDlls();
                Environment.Exit(0);
            }
            if (cmd.Equals("-version", StringComparison.OrdinalIgnoreCase))
            {
                cleanupPreviousUpdate();
                Console.WriteLine(Environment.NewLine + Environment.NewLine +
                    "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                unloadEmbeddedDlls();
                Environment.Exit(0);
            }

            if (cmd.Equals("-get-installed-games", StringComparison.OrdinalIgnoreCase))
            {
                int gameMask = 0;
                ConfIni configIni = new ConfIni();
                GameData gameData = new GameData(MeType.ME1_TYPE, configIni, false, true);
                if (GameData.GamePath != null && Directory.Exists(GameData.GamePath) && gameData.getPackages(true, true))
                    gameMask |= 1;
                gameData = new GameData(MeType.ME2_TYPE, configIni, false, true);
                if (GameData.GamePath != null && Directory.Exists(GameData.GamePath) && gameData.getPackages(true, true))
                    gameMask |= 2;
                gameData = new GameData(MeType.ME3_TYPE, configIni, false, true);
                if (GameData.GamePath != null && Directory.Exists(GameData.GamePath) && gameData.getPackages(true, true))
                    gameMask |= 4;

                unloadEmbeddedDlls();
                Environment.Exit(gameMask);
            }

            if (cmd.Equals("-check-update", StringComparison.OrdinalIgnoreCase))
            {
                if (prepareForUpdate(true, true) != "")
                {
                    unloadEmbeddedDlls();
                    Environment.Exit(0);
                }
                else
                {
                    unloadEmbeddedDlls();
                    Environment.Exit(1);
                }
            }

            if (cmd.Equals("-download-update", StringComparison.OrdinalIgnoreCase))
            {
                string filename = prepareForUpdate(true, false);
                if (filename != "")
                {
                    filename = unpackUpdate(filename);
                }
                else
                {
                    Console.WriteLine("Failed download MEM update!");
                    goto fail;
                }
                if (filename != "")
                {
                    Process process = new Process();
                    process.StartInfo.FileName = filename;
                    process.StartInfo.Arguments = "-finish-update-mem " + filename;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    if (process.Start())
                    {
                        unloadEmbeddedDlls();
                        Environment.Exit(0);
                    }
                    Console.WriteLine("Failed start update MEM instance!");
                    goto fail;
                }
                Console.WriteLine("Failed unpack MEM update!");
                goto fail;
            }

            if (cmd.Equals("-update-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-finish-update-mem", StringComparison.OrdinalIgnoreCase))
            {
                if (cmd.Equals("-update-mem"))
                    ShowWindow(GetConsoleWindow(), 0);
                Thread.Sleep(1000);
                try
                {
                    if (args.Length != 2)
                        throw new Exception();
                    string baseName = Path.GetFileNameWithoutExtension(args[1]);
                    string fileExe = baseName.Substring("new-".Length) + ".exe";
                    string filePdb = baseName.Substring("new-".Length) + ".pdb";
                    if (File.Exists(fileExe))
                        File.Delete(fileExe);
                    if (File.Exists(filePdb))
                        File.Delete(filePdb);
                    File.Copy(baseName + ".exe", fileExe);
                    File.Copy(baseName + ".pdb", filePdb);

                    Process process = new Process();
                    if (cmd.Equals("-finish-update-mem"))
                        process.StartInfo.Arguments = "-version";
                    process.StartInfo.FileName = fileExe;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    if (!process.Start())
                        throw new Exception();

                    unloadEmbeddedDlls();
                    Environment.Exit(0);
                }
                catch
                {
                    if (cmd.Equals("-update-mem"))
                        MessageBox.Show("Failed update MEM!");
                    else
                        Console.WriteLine("Failed update MEM!");
                    goto fail;
                }
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase))
            {
                if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length == 5)
                    {
                        if (args[4].ToLowerInvariant() == "-ipc")
                            ipc = true;
                    }
                    else if (args.Length != 4 && args.Length != 5)
                    {
                        {
                            Console.WriteLine("Error: wrong arguments!");
                            DisplayHelp();
                            goto fail;
                        }
                    }
                }
                else if (args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                game = args[1];
                try
                {
                    gameId = (MeType)int.Parse(game);
                }
                catch
                {
                    gameId = 0;
                }
                if (gameId != MeType.ME1_TYPE && gameId != MeType.ME2_TYPE && gameId != MeType.ME3_TYPE)
                {
                    Console.WriteLine("Error: wrong game id!");
                    DisplayHelp();
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
                    if (!CmdLineConverter.ConvertToMEM(gameId, inputDir, outputFile, ipc))
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
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine("Error: input file not exists: " + inputFile);
                    goto fail;
                }
                else
                {
                    if (Path.GetExtension(outputFile).ToLowerInvariant() != ".dds")
                    {
                        Console.WriteLine("Error: output file is not dds: " + outputFile);
                        goto fail;
                    }
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
                    DisplayHelp();
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
                if (args.Length < 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                string format = args[1];
                string threshold = "128";
                if (format == "dxt1a")
                {
                    if (args.Length == 5)
                    {
                        threshold = args[2];
                        inputFile = args[3];
                        outputFile = args[4];
                    }
                    else
                    {
                        inputFile = args[2];
                        outputFile = args[3];
                    }
                }
                else
                {
                    inputFile = args[2];
                    outputFile = args[3];
                }

                if (!File.Exists(inputFile))
                {
                    Console.WriteLine("Error: input file not exists: " + inputFile);
                    goto fail;
                }
                else
                {
                    if (Path.GetExtension(outputFile).ToLowerInvariant() != ".dds")
                    {
                        Console.WriteLine("Error: output file is not dds: " + outputFile);
                        goto fail;
                    }
                    Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                    if (!CmdLineConverter.convertImage(inputFile, outputFile, format, threshold))
                    {
                        goto fail;
                    }
                }
            }
            else if (cmd.Equals("-extract-all-dds", StringComparison.OrdinalIgnoreCase) ||
                     cmd.Equals("-extract-all-png", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3 && args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                game = args[1];
                try
                {
                    gameId = (MeType)int.Parse(game);
                }
                catch
                {
                    gameId = 0;
                }
                if (gameId != MeType.ME1_TYPE && gameId != MeType.ME2_TYPE && gameId != MeType.ME3_TYPE)
                {
                    Console.WriteLine("Error: wrong game id!");
                    DisplayHelp();
                    goto fail;
                }
                outputDir = args[2];
                string tfcFilter = "";
                if (args.Length > 3)
                    tfcFilter = args[3];

                Console.WriteLine(Environment.NewLine + Environment.NewLine +
                    "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                if (cmd.Equals("-extract-all-dds", StringComparison.OrdinalIgnoreCase))
                    if (!CmdLineConverter.extractAllTextures(gameId, outputDir, false, tfcFilter))
                    {
                        goto fail;
                    }
                if (cmd.Equals("-extract-all-png", StringComparison.OrdinalIgnoreCase))
                    if (!CmdLineConverter.extractAllTextures(gameId, outputDir, true, ""))
                    {
                        goto fail;
                    }
            }
            else if (cmd.Equals("-me3dlcmod-for-mgamerz", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3 && args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                inputFile = args[1];
                string tfcName = args[2];
                byte[] guid;
                if (args.Length == 4)
                {
                    if (args[3].Length != 32)
                    {
                        Console.WriteLine("Error: wrong guid!");
                        DisplayHelp();
                        goto fail;
                    }
                    guid = new byte[16];
                    for (int i = 0; i < 32; i += 2)
                        guid[i / 2] = Convert.ToByte(args[3].Substring(i, 2), 16);
                }
                else
                {
                    guid = Guid.NewGuid().ToByteArray();
                }
                Console.WriteLine(Environment.NewLine + Environment.NewLine +
                        "--- MEM v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
                if (!CmdLineConverter.applyMEMSpecialModME3(inputFile, tfcName, guid))
                {
                    goto fail;
                }
            }
            else if (args.Length > 0)
            {
                DisplayHelp();
                unloadEmbeddedDlls();
                Environment.Exit(0);
            }

            if (args.Length == 0)
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


                cleanupPreviousUpdate();
                string filename = prepareForUpdate();
                if (filename != "")
                    filename = unpackUpdate(filename);
                if (filename != "")
                {
                    Process process = new Process();
                    process.StartInfo.FileName = filename;
                    process.StartInfo.Arguments = "-update-mem " + filename;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    if (process.Start())
                    {
                        unloadEmbeddedDlls();
                        Environment.Exit(0);
                    }
                    MessageBox.Show("Failed start update MEM instance!");
                }

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
            Environment.Exit(1);
        }

    }
}
