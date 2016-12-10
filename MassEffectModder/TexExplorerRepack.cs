/*
 * MassEffectModder
 *
 * Copyright (C) 2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.IO;
using System.Windows.Forms;

namespace MassEffectModder
{
    public partial class TexExplorer : Form
    {
        private void RepackTexturesTFC()
        {
            DialogResult result = MessageBox.Show("Repacking textures can be very long process.\n\n" +
                "Are you sure to proceed?", "Textures repacking (WIP feature!)", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                Close();
                return;
            }

            for (int i = 0; i < nodeList.Count; i++)
            {
                PackageTreeNode node = nodeList[i];
                {
                }
            }

            _mainWindow.updateStatusLabel("Done.");
            _mainWindow.updateStatusLabel("");
        }
    }
}
