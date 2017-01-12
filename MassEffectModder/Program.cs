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
using System.IO;
using System.Windows.Forms;

namespace MassEffectModder
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Misc.isRunAsAdministrator())
            {
                MessageBox.Show("Warning: Tool started with Administrator rights!");
            }

            string iniPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "installer.ini");
            if (File.Exists(iniPath))
            {
                Installer installer = new Installer();
                if (installer.Run())
                    Application.Run(installer);
            }
            else
                Application.Run(new MainWindow());
        }
    }
}
