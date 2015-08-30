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

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MEDataExplorer
{
    public class ConfIni
    {
        string _iniPath;

        [DllImport("kernel32")]
        private static extern UInt32 GetPrivateProfileString(string section, string key,
                string def, StringBuilder value, int size, string filename);

        [DllImport("kernel32")]
        private static extern Boolean WritePrivateProfileString(string section, string key,
                string value, string filename);

        public ConfIni(string iniPath = null)
        {
            if (iniPath != null)
                _iniPath = iniPath;
            else
                _iniPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),
                    Assembly.GetExecutingAssembly().GetName().Name + ".ini");
        }

        public string Read(string key, string section)
        {
            if (_iniPath == null || key == null)
                throw new Exception();

            var str = new StringBuilder(256);
            GetPrivateProfileString(section, key, "", str, str.MaxCapacity, _iniPath);
            return str.ToString();            
        }

        public bool Write(string key, string value, string section)
        {
            if (_iniPath == null || key == null || value == null)
                throw new Exception();

            return WritePrivateProfileString(section, key, value, _iniPath);
        }
    }
}
