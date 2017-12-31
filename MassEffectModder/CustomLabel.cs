/*
 * MassEffectModder
 *
 * Copyright (C) 2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MassEffectModder
{
    class CustomLabel : Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.FillRectangle(new SolidBrush(BackColor), ClientRectangle);

            using (GraphicsPath gfxPath = new GraphicsPath())
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                using (Pen outline = new Pen(Color.Black, 3) { LineJoin = LineJoin.Round })
                {
                    using (StringFormat format = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        using (Brush foreBrush = new SolidBrush(ForeColor))
                        {
                            gfxPath.AddString(Text, Font.FontFamily, (int)Font.Style, (int)e.Graphics.DpiY * Font.Size / 72,
                                e.ClipRectangle, format);
                            e.Graphics.DrawPath(outline, gfxPath);
                            e.Graphics.FillPath(foreBrush, gfxPath);
                        }
                    }
                }
            }
        }
    }
}
