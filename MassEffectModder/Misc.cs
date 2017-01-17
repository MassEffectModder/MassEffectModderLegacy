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
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace MassEffectModder
{
    static class LODSettings
    {
        static public void updateLOD(MeType gameId, ConfIni engineConf)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_GUI", "(MinLODSize=64,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "TextureLODSettings");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_RenderTarget", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=-1)", "SystemSettings");
            }
            else
            {
                throw new Exception("");
            }
        }

        static public void removeLOD(MeType gameId, ConfIni engineConf)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.DeleteKey("TEXTUREGROUP_LightAndShadowMap", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_64", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_128", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_256", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_512", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_1024", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_64", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_128", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_256", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_512", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_1024", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_128", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_256", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_512", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_1024", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_GUI", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Promotional", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_1024", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Diff", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Norm", "TextureLODSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Spec", "TextureLODSettings");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.DeleteKey("TEXTUREGROUP_LightAndShadowMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_RenderTarget", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_UI", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Promotional", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Diff", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Norm", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Spec", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.DeleteKey("TEXTUREGROUP_Environment_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_UI", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Promotional", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Diff", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Norm", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Spec", "SystemSettings");
            }
            else
            {
                throw new Exception("");
            }
        }
    }

    static class Misc
    {
        static public void VerifyME1Exe(GameData gameData, bool gui = true)
        {
            if (!File.Exists(gameData.GameExePath))
                throw new FileNotFoundException("Game executable not found: " + gameData.GameExePath);

            using (FileStream fs = new FileStream(gameData.GameExePath, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.JumpTo(0x3C); // jump to offset of COFF header
                uint offset = fs.ReadUInt32() + 4; // skip PE signature too
                fs.JumpTo(offset + 0x12); // jump to flags entry
                ushort flag = fs.ReadUInt16(); // read flags
                if ((flag & 0x20) != 0x20) // check for LAA flag
                {
                    if (gui)
                        MessageBox.Show("Large Aware Address flag is not enabled on Mass Effect executable file.Correcting...");
                    flag |= 0x20;
                    fs.Skip(-2);
                    fs.WriteUInt16(flag); // write LAA flag
                }
            }
        }

        static public bool checkWriteAccess(string path)
        {
            try
            {
                using (FileStream fs = File.Create(Path.Combine(GameData.GamePath, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        static public bool isRunAsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        static public long getDiskFreeSpace(string path)
        {
            string drive = path.Substring(0, 3);
            foreach (DriveInfo drv in DriveInfo.GetDrives())
            {
                if (string.Compare(drv.Name, drive, true) == 0)
                    return drv.TotalFreeSpace;
            }

            return -1;
        }

        public static long getDirectorySize(string dir)
        {
            return new DirectoryInfo(dir).GetFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        public static string getBytesFormat(long size)
        {
            if (size / 1024 == 0)
                return string.Format("{0:0.00} Bytes", size);
            else if (size / 1024 / 1024 == 0)
                return string.Format("{0:0.00} KB", size / 1024.0);
            else if (size / 1024 / 1024 / 1024 == 0)
                return string.Format("{0:0.00} MB", size / 1024 / 1024.0);
            else
                return string.Format("{0:0.00} GB", size / 1024/ 1024 / 1024.0);
        }

        static System.Diagnostics.Stopwatch timer;
        public static void startTimer()
        {
            timer = System.Diagnostics.Stopwatch.StartNew();
        }

        public static long stopTimer()
        {
            timer.Stop();
            return timer.ElapsedMilliseconds;
        }

        public static string getTimerFormat(long time)
        {
            if (time / 1000 == 0)
                return string.Format("{0} milliseconds", time);
            else if (time / 1000 / 60 == 0)
                return string.Format("{0} seconds", time / 1000);
            else if (time / 1000 / 60 / 60 == 0)
                return string.Format("{0} min - {1} sec", time / 1000 / 60, time / 1000 % 60);
            else
            {
                long hours = time / 1000 / 60 / 60;
                long minutes = (time - (hours * 1000 * 60 * 60)) / 1000 / 60;
                long seconds = (time - (hours * 1000 * 60 * 60) - (minutes * 1000 * 60)) / 1000 / 60;
                return string.Format("{0} hours - {1} min - {2} sec", hours, minutes, seconds);
            }
        }
    }
}
