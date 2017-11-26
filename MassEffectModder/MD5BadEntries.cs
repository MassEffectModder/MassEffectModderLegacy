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

namespace MassEffectModder
{
    static partial class Misc
    {
        const string Bad_ME1Controller = "ME1 Controller v1.2.0 - MEUITM";
        const string Bad_ME1SameSexRomances = "ME1 Same-Sex Romances v2.0 - MEUITM";
        const string Bad_NoSharedCooldown = "ME2 No Shared Cooldown";

        static public MD5ModFileEntry[] badMOD = new MD5ModFileEntry[]
        {
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\BIOC_Materials.u",
md5 = new byte[] { 0x0D, 0x0E, 0x4D, 0x96, 0xAC, 0x8E, 0x86, 0x0E, 0xBE, 0x19, 0x71, 0x97, 0x3E, 0xB5, 0xA8, 0x5A, },
modName = Bad_ME1Controller,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Maps\EntryMenu.SFM",
md5 = new byte[] { 0xDF, 0xB7, 0x59, 0x83, 0x05, 0xC6, 0x81, 0x63, 0x7A, 0xA1, 0xFD, 0x3B, 0x01, 0xA1, 0x56, 0xFC, },
modName = Bad_ME1Controller,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Startup_int.upk",
md5 = new byte[] { 0xB5, 0x4E, 0x98, 0x0F, 0x58, 0x4F, 0xD2, 0x0C, 0xBA, 0x63, 0xAB, 0x02, 0x90, 0x9D, 0xB7, 0x3E, },
modName = Bad_ME1Controller,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Maps\NOR\DS2\BIOA_NOR10_09ashley_DS2.SFM",
md5 = new byte[] { 0xA5, 0x4A, 0x66, 0xA3, 0x68, 0xE9, 0xCC, 0xEE, 0xA4, 0x1F, 0x84, 0x5F, 0x1B, 0xD7, 0xE2, 0x22, },
modName = Bad_ME1SameSexRomances,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Maps\NOR\DS2\BIOA_NOR10_09kaidan_DS2.SFM",
md5 = new byte[] { 0x68, 0xA7, 0x46, 0x0E, 0x0D, 0xA1, 0x67, 0xB7, 0xA5, 0x55, 0x16, 0x4B, 0x50, 0xBE, 0xA7, 0x67, },
modName = Bad_ME1SameSexRomances,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Maps\NOR\DSG\BIOA_NOR10_04A_DSG.SFM",
md5 = new byte[] { 0x3D, 0x72, 0x6D, 0x40, 0x16, 0xB9, 0x50, 0x83, 0x1A, 0x1D, 0x15, 0x9F, 0x55, 0x60, 0xA8, 0x8C, },
modName = Bad_ME1SameSexRomances,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Maps\NOR\DSG\BIOA_NOR10_06wake_DSG.SFM",
md5 = new byte[] { 0x93, 0xE4, 0xE6, 0xBE, 0x7A, 0x0B, 0x09, 0x07, 0x0B, 0x27, 0xD9, 0x5C, 0xD7, 0x1F, 0xA4, 0x71, },
modName = Bad_ME1SameSexRomances,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\Maps\STA\DSG\BIOA_STA60_09A_DSG.SFM",
md5 = new byte[] { 0x77, 0xD8, 0x8D, 0x60, 0x18, 0x00, 0x82, 0xE0, 0xCC, 0x7B, 0x2A, 0x68, 0xC1, 0x60, 0x62, 0xCA, },
modName = Bad_ME1SameSexRomances,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\EntryMenu.pcc",
md5 = new byte[] { 0x39, 0x3D, 0x11, 0xE0, 0x1B, 0x96, 0xF6, 0xB5, 0x64, 0xFB, 0x0F, 0x94, 0xBA, 0x19, 0xEE, 0xA1, },
modName = Bad_NoSharedCooldown,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\EntryMenu.pcc",
md5 = new byte[] { 0xB0, 0x56, 0x45, 0x71, 0xA5, 0x7F, 0x6F, 0xEA, 0xFC, 0x88, 0x13, 0x29, 0xA6, 0xB6, 0xBA, 0x90, },
modName = Bad_NoSharedCooldown,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\SFXGame.pcc",
md5 = new byte[] { 0x15, 0x4D, 0xDA, 0x5B, 0xB3, 0xB4, 0x3C, 0x3C, 0x1B, 0xAD, 0xCB, 0xB4, 0x52, 0xFD, 0x72, 0xCF, },
modName = Bad_NoSharedCooldown,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\SFXGame.pcc",
md5 = new byte[] { 0xEB, 0x7D, 0xDC, 0xE5, 0x35, 0x65, 0xFB, 0x6B, 0x32, 0x51, 0x74, 0x2D, 0x6B, 0x2C, 0x0C, 0x3F, },
modName = Bad_NoSharedCooldown,
},
new MD5ModFileEntry
{
path = @"\BioGame\CookedPC\SFXGame.pcc",
md5 = new byte[] { 0x75, 0x0D, 0x73, 0xB8, 0x23, 0x58, 0xAA, 0x4C, 0x44, 0x18, 0xD5, 0xBA, 0xF2, 0x64, 0xFB, 0x4A, },
modName = Bad_NoSharedCooldown,
},

        };
    }
}
