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
                else if (resources[l].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (resources[l].Contains("PermissionsGranter.exe"))
                    {
                        string exePath = Path.Combine(dllPath, "PermissionsGranter.exe");
                        if (!Directory.Exists(dllPath))
                            Directory.CreateDirectory(dllPath);

                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            byte[] buf = s.ReadToBuffer(s.Length);
                            if (File.Exists(exePath))
                                File.Delete(exePath);
                            File.WriteAllBytes(exePath, buf);
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

        static private string prepareForUpdate(bool onlycheck = false)
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
                    MessageBox.Show("New version of MEM is available: " + githubVersion, "Update");
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

                    Uri url = new Uri(parsed["assets"][0]["browser_download_url"]);
                    string filename = parsed["assets"][0]["name"];
                    webClient.DownloadFileAsync(url, filename);
                    while (webClient.IsBusy) { Application.DoEvents(); Thread.Sleep(10); }

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
                handle = zip.Open(file, ref numEntries, 0);
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

        [STAThread]

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("-update-mem", StringComparison.OrdinalIgnoreCase))
            {
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
                    process.StartInfo.FileName = fileExe;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    if (!process.Start())
                        throw new Exception();

                    Environment.Exit(0);
                }
                catch
                {
                    MessageBox.Show("Failed update MEM!");
                    Environment.Exit(1);
                }
            }

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

            unloadEmbeddedDlls();
            Environment.Exit(0);
        }
    }
}
