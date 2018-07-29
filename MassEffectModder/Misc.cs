/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using Microsoft.Win32;
using StreamHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MassEffectModder
{
    public enum MeType
    {
        ME1_TYPE = 1,
        ME2_TYPE,
        ME3_TYPE
    }

    static class LODSettings
    {
        static public void readLOD(MeType gameId, ConfIni engineConf, ref string log)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                log += "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_512" + engineConf.Read("TEXTUREGROUP_VFX_512", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_GUI=" + engineConf.Read("TEXTUREGROUP_GUI", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "TextureLODSettings") + Environment.NewLine;
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                log += "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_RenderTarget=" + engineConf.Read("TEXTUREGROUP_RenderTarget", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_512=" + engineConf.Read("TEXTUREGROUP_VFX_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_UI=" + engineConf.Read("TEXTUREGROUP_UI", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "SystemSettings") + Environment.NewLine;
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                log += "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldSpecular=" + engineConf.Read("TEXTUREGROUP_WorldSpecular", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_ShadowMap=" + engineConf.Read("TEXTUREGROUP_ShadowMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_RenderTarget=" + engineConf.Read("TEXTUREGROUP_RenderTarget", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_512=" + engineConf.Read("TEXTUREGROUP_VFX_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_UI=" + engineConf.Read("TEXTUREGROUP_UI", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "SystemSettings") + Environment.NewLine;
            }
            else
            {
                throw new Exception("");
            }
        }

        static public void readLODIpc(MeType gameId, ConfIni engineConf)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_512" + engineConf.Read("TEXTUREGROUP_VFX_512", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_GUI=" + engineConf.Read("TEXTUREGROUP_GUI", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "TextureLODSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "TextureLODSettings"));
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_RenderTarget=" + engineConf.Read("TEXTUREGROUP_RenderTarget", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_512=" + engineConf.Read("TEXTUREGROUP_VFX_512", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_UI=" + engineConf.Read("TEXTUREGROUP_UI", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "SystemSettings"));
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_WorldSpecular=" + engineConf.Read("TEXTUREGROUP_WorldSpecular", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_ShadowMap=" + engineConf.Read("TEXTUREGROUP_ShadowMap", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_RenderTarget=" + engineConf.Read("TEXTUREGROUP_RenderTarget", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_512=" + engineConf.Read("TEXTUREGROUP_VFX_512", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_UI=" + engineConf.Read("TEXTUREGROUP_UI", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "SystemSettings"));
                Console.WriteLine("[IPC]LODLINE " + "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "SystemSettings"));
            }
            else
            {
                throw new Exception("");
            }
        }

        static public void updateLOD(MeType gameId, ConfIni engineConf)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_GUI", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_RenderTarget", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_WorldSpecular", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_ShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_RenderTarget", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
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
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=16,MaxLODSize=4096,LODBias=2)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=16,MaxLODSize=4096,LODBias=2)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=32,MaxLODSize=64,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=32,MaxLODSize=128,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=32,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=32,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=8,MaxLODSize=64,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=8,MaxLODSize=128,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=8,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=8,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=8,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=32,MaxLODSize=128,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=32,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=32,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_GUI", "(MinLODSize=8,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=32,MaxLODSize=2048,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=32,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=32,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.DeleteKey("TEXTUREGROUP_World", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_WorldNormalMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_AmbientLightMap", "SystemSettings");
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
                engineConf.DeleteKey("TEXTUREGROUP_World", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_WorldSpecular", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_WorldNormalMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_AmbientLightMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_ShadowMap", "SystemSettings");
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
            else
            {
                throw new Exception("");
            }
        }

        static public void updateGFXSettings(MeType gameId, ConfIni engineConf, bool softShadowsME1, bool meuitmMode)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.Write("MaxShadowResolution", "2048", "Engine.Engine");
                engineConf.Write("MaxShadowResolution", "2048", "Engine.GameEngine");
                if (softShadowsME1)
                {
                    engineConf.Write("MinShadowResolution", "16", "Engine.Engine");
                    engineConf.Write("MinShadowResolution", "16", "Engine.GameEngine");
                }
                else
                {
                    engineConf.Write("MinShadowResolution", "64", "Engine.Engine");
                    engineConf.Write("MinShadowResolution", "64", "Engine.GameEngine");
                }
                engineConf.Write("DynamicShadows", "True", "SystemSettings");
                engineConf.Write("EnableDynamicShadows", "True", "WinDrv.WindowsClient");
                if (softShadowsME1 && meuitmMode)
                {
                    engineConf.Write("DepthBias", "0.006000", "Engine.Engine");
                    engineConf.Write("DepthBias", "0.006000", "Engine.GameEngine");
                }
                else
                {
                    engineConf.Write("DepthBias", "0.030000", "Engine.Engine");
                    engineConf.Write("DepthBias", "0.030000", "Engine.GameEngine");
                }
                engineConf.Write("ShadowFilterQualityBias", "2", "SystemSettings");
                if (softShadowsME1)
                {
                    engineConf.Write("ShadowFilterRadius", "2", "Engine.Engine");
                    engineConf.Write("ShadowFilterRadius", "2", "Engine.GameEngine");
                }
                else
                {
                    engineConf.Write("ShadowFilterRadius", "5", "Engine.Engine");
                    engineConf.Write("ShadowFilterRadius", "5", "Engine.GameEngine");
                }
                engineConf.Write("bEnableBranchingPCFShadows", "True", "Engine.Engine");
                engineConf.Write("bEnableBranchingPCFShadows", "True", "Engine.GameEngine");
                engineConf.Write("MaxAnisotropy", "16", "SystemSettings");
                engineConf.Write("TextureLODLevel", "3", "WinDrv.WindowsClient");
                engineConf.Write("FilterLevel", "2", "WinDrv.WindowsClient");
                engineConf.Write("Trilinear", "True", "SystemSettings");
                engineConf.Write("MotionBlur", "True", "SystemSettings");
                engineConf.Write("DepthOfField", "True", "SystemSettings");
                engineConf.Write("Bloom", "True", "SystemSettings");
                engineConf.Write("QualityBloom", "True", "SystemSettings");
                engineConf.Write("ParticleLODBias", "-1", "SystemSettings");
                engineConf.Write("SkeletalMeshLODBias", "-1", "SystemSettings");
                engineConf.Write("DetailMode", "2", "SystemSettings");
                engineConf.Write("PoolSize", "1536", "TextureStreaming");
                engineConf.Write("MinTimeToGuaranteeMinMipCount", "0", "TextureStreaming");
                engineConf.Write("MaxTimeToGuaranteeMinMipCount", "0", "TextureStreaming");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.Write("MaxShadowResolution", "2048", "SystemSettings");
                engineConf.Write("MinShadowResolution", "64", "SystemSettings");
                engineConf.Write("ShadowFilterQualityBias", "2", "SystemSettings");
                engineConf.Write("ShadowFilterRadius", "5", "SystemSettings");
                engineConf.Write("bEnableBranchingPCFShadows", "True", "SystemSettings");
                engineConf.Write("MaxAnisotropy", "16", "SystemSettings");
                engineConf.Write("Trilinear", "True", "SystemSettings");
                engineConf.Write("MotionBlur", "True", "SystemSettings");
                engineConf.Write("DepthOfField", "True", "SystemSettings");
                engineConf.Write("Bloom", "True", "SystemSettings");
                engineConf.Write("QualityBloom", "True", "SystemSettings");
                engineConf.Write("ParticleLODBias", "-1", "SystemSettings");
                engineConf.Write("SkeletalMeshLODBias", "-1", "SystemSettings");
                engineConf.Write("DetailMode", "2", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.Write("MaxShadowResolution", "2048", "SystemSettings");
                engineConf.Write("MinShadowResolution", "64", "SystemSettings");
                engineConf.Write("ShadowFilterQualityBias", "2", "SystemSettings");
                engineConf.Write("ShadowFilterRadius", "5", "SystemSettings");
                engineConf.Write("bEnableBranchingPCFShadows", "True", "SystemSettings");
                engineConf.Write("MaxAnisotropy", "16", "SystemSettings");
                engineConf.Write("MotionBlur", "True", "SystemSettings");
                engineConf.Write("DepthOfField", "True", "SystemSettings");
                engineConf.Write("Bloom", "True", "SystemSettings");
                engineConf.Write("QualityBloom", "True", "SystemSettings");
                engineConf.Write("ParticleLODBias", "-1", "SystemSettings");
                engineConf.Write("SkeletalMeshLODBias", "-1", "SystemSettings");
                engineConf.Write("DetailMode", "2", "SystemSettings");
            }
            else
            {
                throw new Exception("");
            }

        }
    }

    public struct BinaryMod
    {
        public string packagePath;
        public int exportId;
        public byte[] data;
        public int binaryModType;
        public string textureName;
        public uint textureCrc;
        public bool markConvert;
        public long offset;
        public long size;
    };

    static partial class Misc
    {
        public static bool generateModsMd5Entries = false;
        public static bool generateMd5Entries = false;

        public struct MD5FileEntry
        {
            public string path;
            public byte[] md5;
            public int size;
        }

        public struct MD5ModFileEntry
        {
            public string path;
            public byte[] md5;
            public string modName;
        }

        static public bool ApplyLAAForME1Exe()
        {
            if (File.Exists(GameData.GameExePath))
            {
                using (FileStream fs = new FileStream(GameData.GameExePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.JumpTo(0x3C); // jump to offset of COFF header
                    uint offset = fs.ReadUInt32() + 4; // skip PE signature too
                    fs.JumpTo(offset + 0x12); // jump to flags entry
                    ushort flag = fs.ReadUInt16(); // read flags
                    if ((flag & 0x20) != 0x20) // check for LAA flag
                    {
                        Console.WriteLine("Patching ME1 for LAA: " + GameData.GameExePath);
                        flag |= 0x20;
                        fs.Skip(-2);
                        fs.WriteUInt16(flag); // write LAA flag
                    }
                    else
                    {
                        Console.WriteLine("File already has LAA flag enabled: " + GameData.GameExePath);
                    }
                }
                return true;
            }
            else
            {
                Console.WriteLine("File not found: " + GameData.GameExePath);
                return false;
            }
        }

        static public bool ChangeProductNameForME1Exe()
        {
            if (File.Exists(GameData.GameExePath))
            {
                // search for "ProductName Mass Effect"
                byte[] pattern = { 0x50, 0, 0x72, 0, 0x6F, 0, 0x64, 0, 0x75, 0, 0x63, 0, 0x74, 0, 0x4E, 0, 0x61, 0, 0x6D, 0, 0x65, 0, 0, 0, 0, 0,
                                   0x4D, 0, 0x61, 0, 0x73, 0, 0x73, 0, 0x20, 0, 0x45, 0, 0x66, 0, 0x66, 0, 0x65, 0, 0x63, 0, 0x74, 0 };
                byte[] buffer = File.ReadAllBytes(GameData.GameExePath);
                int pos = -1;
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == pattern[0])
                    {
                        bool found = true;
                        for (int l = 1; l < pattern.Length; l++)
                        {
                            if (buffer[i + l] != pattern[l])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            pos = i;
                            break;
                        }
                    }
                }
                if (pos != -1)
                {
                    // replace to "Mass_Effect"
                    buffer[pos + 34] = 0x5f;
                    File.WriteAllBytes(GameData.GameExePath, buffer);
                    Console.WriteLine("Patching ME1 for Product Name: " + GameData.GameExePath);
                }
                else
                {
                    Console.WriteLine("Specific Product Name not found or already changed: " + GameData.GameExePath);
                }
                return true;
            }
            else
            {
                Console.WriteLine("File not found: " + GameData.GameExePath);
                return false;
            }
        }

        static public bool checkWriteAccessDir(string path)
        {
            try
            {
                using (FileStream fs = File.Create(Path.Combine(path, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        static public bool checkWriteAccessFile(string path)
        {
            try
            {
                using (FileStream fs = File.OpenWrite(path)) { }
                return true;
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

        static public bool CheckAndCorrectAccessToGame(MeType gameId)
        {
            bool writeAccess = false;
            if (checkWriteAccessDir(GameData.MainData))
                writeAccess = true;

            bool uac = false;
            int? value = (int?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", null);
            if (value != null && value > 0)
                uac = true;

            bool registryExists = false;
            if (gameId == MeType.ME1_TYPE)
            {
                string keyRegistry = @"SOFTWARE\WOW6432Node\AGEIA Technologies";
                RegistryKey key;
                if (isRunAsAdministrator())
                {
                    string userName = WindowsIdentity.GetCurrent().Name;
                    key = Registry.LocalMachine.CreateSubKey(keyRegistry);
                    RegistrySecurity security = new RegistrySecurity();
                    security = key.GetAccessControl();
                    security.AddAccessRule(new RegistryAccessRule(userName, RegistryRights.WriteKey |
                        RegistryRights.ReadKey | RegistryRights.Delete |
                        RegistryRights.FullControl, InheritanceFlags.ContainerInherit |
                        InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    key.SetAccessControl(security);
                    key.Close();
                    registryExists = true;
                }
                else
                {
                    try
                    {
                        key = Registry.LocalMachine.OpenSubKey(keyRegistry, true);
                    }
                    catch
                    {
                        key = null;
                    }

                    if (key != null)
                    {
                        key.Close();
                        registryExists = true;
                    }
                }
            }

            string msg;
            if (!uac && ((gameId == MeType.ME1_TYPE && !registryExists) || !writeAccess))
            {
                msg = "MEM is not able to";
                if (!writeAccess)
                {
                    msg += " grant write access to game folders";
                }
                if (gameId == MeType.ME1_TYPE && !registryExists)
                {
                    if (!writeAccess)
                        msg += " and";
                    msg += " fix a ME1 launch issue";
                }
                msg += " because MEM does not have administrative rights and UAC is disabled.";
                MessageBox.Show(msg);
                return false;
            }

            if (!writeAccess || (gameId == MeType.ME1_TYPE && !registryExists))
            {
                msg = "Some";
                if (!writeAccess)
                {
                    msg += " game folders";
                }
                if (gameId == MeType.ME1_TYPE && !registryExists)
                {
                    if (!writeAccess)
                        msg += " and";
                    msg += " registry keys";
                }

                msg += " are not writeable by your user account.\nMEM will attempt to grant access to";

                if (!writeAccess)
                {
                    msg += " game folders";
                }
                if (gameId == MeType.ME1_TYPE && !registryExists)
                {
                    if (!writeAccess)
                        msg += " and";
                    msg += " registry keys";
                }

                msg += " with PermissionsGranter.exe program.\n\n";

                if (!writeAccess)
                {
                    msg += "Game folder: " + GameData.GamePath + "\n\n";
                }
                if (gameId == MeType.ME1_TYPE && !registryExists)
                {
                    msg += "Registry: HKLM\\SOFTWARE\\WOW6432Node\\AGEIA Technologies\n";
                    msg += "(Fixes a ME1 launch issue)";
                }

                MessageBox.Show(msg, "Granting permissions");

                bool failedAccess = true;
                string userName = WindowsIdentity.GetCurrent().Name;
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = Path.Combine(Program.dllPath, "PermissionsGranter.exe");
                    process.StartInfo.Arguments = "\"" + userName + "\"";
                    if (gameId == MeType.ME1_TYPE && !registryExists)
                        process.StartInfo.Arguments += " -create-hklm-reg-key \"SOFTWARE\\WOW6432Node\\AGEIA Technologies\"";
                    if (!writeAccess)
                        process.StartInfo.Arguments += " \"" + GameData.GamePath + "\"";
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit(60000);
                    if (process.ExitCode == 0)
                        failedAccess = false;
                }
                catch
                {
                    failedAccess = true;
                }

                if (failedAccess)
                {
                    msg = "MEM is not able to";
                    if (!writeAccess)
                    {
                        msg += " grant write access to game folders";
                    }
                    if (gameId == MeType.ME1_TYPE && !registryExists)
                    {
                        if (!writeAccess)
                            msg += " and";
                        msg += " fix a ME1 launch issue";
                    }
                    msg += " because MEM does not have administrative rights.";
                    MessageBox.Show(msg);
                    return false;
                }

                registryExists = true;
            }

            if (gameId == MeType.ME1_TYPE)
                ApplyLAAForME1Exe();

            if (gameId == MeType.ME1_TYPE && registryExists)
            {
                string gameExePath = GameData.GameExePath;
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true);
                if (key != null)
                {
                    string entry = (string)key.GetValue(gameExePath, null);
                    if (entry != null)
                    {
                        entry = entry.Replace("RUNASADMIN", "");
                        entry = entry.Replace("WINXPSP3", "");
                        key.SetValue(gameExePath, entry);
                    }
                    key.Close();
                }

                ChangeProductNameForME1Exe();
            }

            return true;
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
                return string.Format("{0:0.00} GB", size / 1024 / 1024 / 1024.0);
        }

        static Stopwatch timer;
        public static void startTimer()
        {
            timer = Stopwatch.StartNew();
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

        static public int ParseLegacyMe3xScriptMod(List<FoundTexture> textures, string script, string textureName)
        {
            Regex parts = new Regex("pccs.Add[(]\"[A-z,0-9/,..]*\"");
            Match match = parts.Match(script);
            if (match.Success)
            {
                string packageName = match.ToString().Replace('/', '\\').Split('\"')[1].Split('\\').Last().Split('.')[0].ToLowerInvariant();
                parts = new Regex("IDs.Add[(][0-9]*[)];");
                match = parts.Match(script);
                if (match.Success)
                {
                    int exportId = int.Parse(match.ToString().Split('(')[1].Split(')')[0]);
                    if (exportId != 0)
                    {
                        textureName = textureName.ToLowerInvariant();
                        for (int i = 0; i < textures.Count; i++)
                        {
                            if (textures[i].name.ToLowerInvariant() == textureName)
                            {
                                for (int l = 0; l < textures[i].list.Count; l++)
                                {
                                    if (textures[i].list[l].path == "")
                                        continue;
                                    if (textures[i].list[l].exportID == exportId)
                                    {
                                        string pkg = textures[i].list[l].path.Split('\\').Last().Split('.')[0].ToLowerInvariant();
                                        if (pkg == packageName)
                                        {
                                            return i;
                                        }
                                    }
                                }
                            }
                        }
                        // search again but without name match
                        for (int i = 0; i < textures.Count; i++)
                        {
                            for (int l = 0; l < textures[i].list.Count; l++)
                            {
                                if (textures[i].list[l].path == "")
                                    continue;
                                if (textures[i].list[l].exportID == exportId)
                                {
                                    string pkg = textures[i].list[l].path.Split('\\').Last().Split('.')[0].ToLowerInvariant();
                                    if (pkg == packageName)
                                    {
                                        return i;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    textureName = textureName.ToLowerInvariant();
                    for (int i = 0; i < textures.Count; i++)
                    {
                        if (textures[i].name.ToLowerInvariant() == textureName)
                        {
                            for (int l = 0; l < textures[i].list.Count; l++)
                            {
                                if (textures[i].list[l].path == "")
                                    continue;
                                string pkg = textures[i].list[l].path.Split('\\').Last().Split('.')[0].ToLowerInvariant();
                                if (pkg == packageName)
                                {
                                    return i;
                                }
                            }
                        }
                    }
                }
            }

            return -1;
        }

        static public void ParseME3xBinaryScriptMod(string script, ref string package, ref int expId, ref string path)
        {
            Regex parts = new Regex("int objidx = [0-9]*");
            Match match = parts.Match(script);
            if (match.Success)
            {
                expId = int.Parse(match.ToString().Split(' ').Last());

                parts = new Regex("string filename = \"[A-z,0-9,.]*\";");
                match = parts.Match(script);
                if (match.Success)
                {
                    package = match.ToString().Split('\"')[1].Replace("\\\\", "\\");

                    parts = new Regex("string pathtarget = ME3Directory.cookedPath;");
                    match = parts.Match(script);
                    if (match.Success)
                    {
                        path = @"\BioGame\CookedPCConsole";
                        return;
                    }
                    else
                    {
                        parts = new Regex("string pathtarget = Path.GetDirectoryName[(]ME3Directory[.]cookedPath[)];");
                        match = parts.Match(script);
                        if (match.Success)
                        {
                            path = @"\BioGame";
                            return;
                        }
                        else
                        {
                            parts = new Regex("string pathtarget = new DirectoryInfo[(]ME3Directory[.]cookedPath[)][.]Parent.FullName [+] \"[A-z,0-9,_,.]*\";");
                            match = parts.Match(script);
                            if (match.Success)
                            {
                                path = Path.GetDirectoryName(@"\BioGame\" + match.ToString().Split('\"')[1]);
                                return;
                            }
                        }
                    }
                }
            }
        }

        static public PixelFormat changeTextureType(PixelFormat gamePixelFormat, PixelFormat texturePixelFormat, TexProperty.TextureTypes flags)
        {
            if ((gamePixelFormat == PixelFormat.DXT5 || gamePixelFormat == PixelFormat.DXT1 || gamePixelFormat == PixelFormat.ATI2) &&
                (texturePixelFormat == PixelFormat.RGB || texturePixelFormat == PixelFormat.ARGB ||
                 texturePixelFormat == PixelFormat.ATI2 || texturePixelFormat == PixelFormat.V8U8))
            {
                if (texturePixelFormat == PixelFormat.ARGB && flags == TexProperty.TextureTypes.OneBitAlpha)
                {
                    gamePixelFormat = PixelFormat.ARGB;
                }
                else if (texturePixelFormat == PixelFormat.ATI2 &&
                    gamePixelFormat == PixelFormat.DXT1 &&
                    flags == TexProperty.TextureTypes.Normalmap)
                {
                    gamePixelFormat = PixelFormat.ATI2;
                }
                else if (GameData.gameType != MeType.ME3_TYPE && texturePixelFormat == PixelFormat.ARGB &&
                    flags == TexProperty.TextureTypes.Normalmap)
                {
                    gamePixelFormat = PixelFormat.ARGB;
                }
                else if ((gamePixelFormat == PixelFormat.DXT5 || gamePixelFormat == PixelFormat.DXT1) &&
                    (texturePixelFormat == PixelFormat.ARGB || texturePixelFormat == PixelFormat.RGB) &&
                    flags == TexProperty.TextureTypes.Normal)
                {
                    gamePixelFormat = PixelFormat.ARGB;
                }
                else if ((gamePixelFormat == PixelFormat.DXT5 || gamePixelFormat == PixelFormat.DXT1) &&
                    texturePixelFormat == PixelFormat.V8U8 && GameData.gameType == MeType.ME3_TYPE &&
                    flags == TexProperty.TextureTypes.Normal)
                {
                    gamePixelFormat = PixelFormat.V8U8;
                }
            }

            return gamePixelFormat;
        }

        static public bool convertDataModtoMem(string inputDir, string memFilePath, List<FoundTexture> textures,
            MeType gameId, MainWindow mainWindow, ref string errors, bool markToConvert, bool onlyIndividual, bool ipc)
        {
            string[] files = null;

            Console.WriteLine("Mods conversion started...");

            List<string> list;
            List<string> list2;
            if (!onlyIndividual)
            {
                list = Directory.GetFiles(inputDir, "*.mem").Where(item => item.EndsWith(".mem", StringComparison.OrdinalIgnoreCase)).ToList();
                list.Sort(StringComparer.OrdinalIgnoreCase);
                list2 = Directory.GetFiles(inputDir, "*.tpf").Where(item => item.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase)).ToList();
                list2.AddRange(Directory.GetFiles(inputDir, "*.mod").Where(item => item.EndsWith(".mod", StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                list = new List<string>();
                list2 = new List<string>();
            }
            list2.AddRange(Directory.GetFiles(inputDir, "*.bin").Where(item => item.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.xdelta").Where(item => item.EndsWith(".xdelta", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.dds").Where(item => item.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.png").Where(item => item.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.bmp").Where(item => item.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.tga").Where(item => item.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.jpg").Where(item => item.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)));
            list2.AddRange(Directory.GetFiles(inputDir, "*.jpeg").Where(item => item.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)));
            list2.Sort(StringComparer.OrdinalIgnoreCase);
            list.AddRange(list2);
            files = list.ToArray();

            int result;
            string fileName = "";
            ulong dstLen = 0;
            string[] ddsList = null;
            ulong numEntries = 0;
            FileStream outFs;

            List<BinaryMod> mods = new List<BinaryMod>();
            List<MipMaps.FileMod> modFiles = new List<MipMaps.FileMod>();

            if (File.Exists(memFilePath))
                File.Delete(memFilePath);
            outFs = new FileStream(memFilePath, FileMode.Create, FileAccess.Write);
            outFs.WriteUInt32(TreeScan.TextureModTag);
            outFs.WriteUInt32(TreeScan.TextureModVersion);
            outFs.WriteInt64(0); // filled later

            int lastProgress = -1;
            for (int n = 0; n < files.Count(); n++)
            {
                string file = files[n];
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Creating MEM: " + Path.GetFileName(memFilePath));
                    mainWindow.updateStatusLabel2("File " + (n + 1) + " of " + files.Count() + ", " + Path.GetFileName(file));
                }
                string relativeFilePath = file.Substring(inputDir.TrimEnd('\\').Length + 1);
                if (ipc)
                {
                    Console.WriteLine("[IPC]PROCESSING_FILE " + Path.GetFileName(file));
                    int newProgress = (n * 100) / files.Count();
                    if (lastProgress != newProgress)
                    {
                        Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                        lastProgress = newProgress;
                    }
                    Console.Out.Flush();
                }
                else
                {
                    Console.WriteLine("File: " + relativeFilePath);
                }


                if (file.EndsWith(".mem", StringComparison.OrdinalIgnoreCase))
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        uint tag = fs.ReadUInt32();
                        uint version = fs.ReadUInt32();
                        if (tag != TreeScan.TextureModTag || version != TreeScan.TextureModVersion)
                        {
                            if (version != TreeScan.TextureModVersion)
                            {
                                errors += "File " + file + " was made with an older version of MEM, skipping..." + Environment.NewLine;
                            }
                            else
                            {
                                errors += "File " + file + " is not a valid MEM mod, skipping..." + Environment.NewLine;
                            }
                            if (ipc)
                            {
                                Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                Console.Out.Flush();
                            }
                            continue;
                        }
                        else
                        {
                            uint gameType = 0;
                            fs.JumpTo(fs.ReadInt64());
                            gameType = fs.ReadUInt32();
                            if ((MeType)gameType != gameId)
                            {
                                if (ipc)
                                {
                                    Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                    Console.Out.Flush();
                                }
                                else
                                {
                                	errors += "File " + file + " is not a MEM mod valid for this game" + Environment.NewLine;
                                }
                                continue;
                            }
                        }
                        int numFiles = fs.ReadInt32();
                        for (int l = 0; l < numFiles; l++)
                        {
                            MipMaps.FileMod fileMod = new MipMaps.FileMod();
                            fileMod.tag = fs.ReadUInt32();
                            fileMod.name = fs.ReadStringASCIINull();
                            fileMod.offset = fs.ReadInt64();
                            fileMod.size = fs.ReadInt64();
                            long prevPos = fs.Position;
                            fs.JumpTo(fileMod.offset);
                            fileMod.offset = outFs.Position;
                            if (fileMod.tag == MipMaps.FileTextureTag || fileMod.tag == MipMaps.FileTextureTag2)
                            {
                                outFs.WriteStringASCIINull(fs.ReadStringASCIINull());
                                outFs.WriteUInt32(fs.ReadUInt32());
                            }
                            else if (fileMod.tag == MipMaps.FileBinaryTag)
                            {
                                outFs.WriteInt32(fs.ReadInt32());
                                outFs.WriteStringASCIINull(fs.ReadStringASCIINull());
                            }
                            else if (fileMod.tag == MipMaps.FileXdeltaTag)
                            {
                                outFs.WriteInt32(fs.ReadInt32());
                                outFs.WriteStringASCIINull(fs.ReadStringASCIINull());
                            }
                            else
                                throw new Exception();

                            outFs.WriteFromStream(fs, fileMod.size);
                            fs.JumpTo(prevPos);
                            modFiles.Add(fileMod);
                        }
                    }
                }
                else if (file.EndsWith(".mod", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            string package = "";
                            int len = fs.ReadInt32();
                            string version = fs.ReadStringASCIINull();
                            if (version.Length < 5) // legacy .mod
                                fs.SeekBegin();
                            else
                            {
                                fs.SeekBegin();
                                len = fs.ReadInt32();
                                version = fs.ReadStringASCII(len); // version
                            }
                            numEntries = fs.ReadUInt32();
                            for (uint i = 0; i < numEntries; i++)
                            {
                                BinaryMod mod = new BinaryMod();
                                len = fs.ReadInt32();
                                string desc = fs.ReadStringASCII(len); // description
                                len = fs.ReadInt32();
                                string scriptLegacy = fs.ReadStringASCII(len);
                                string path = "";
                                if (desc.Contains("Binary Replacement"))
                                {
                                    try
                                    {
                                        ParseME3xBinaryScriptMod(scriptLegacy, ref package, ref mod.exportId, ref path);
                                        if (mod.exportId == -1 || package == "" || path == "")
                                            throw new Exception();
                                    }
                                    catch
                                    {
                                        len = fs.ReadInt32();
                                        fs.Skip(len);
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        }
                                        continue;
                                    }
                                    mod.packagePath = Path.Combine(path, package);
                                    mod.binaryModType = 1;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);
                                }
                                else
                                {
                                    string textureName = desc.Split(' ').Last();
                                    FoundTexture f;
                                    int index = -1;
                                    try
                                    {
                                        index = ParseLegacyMe3xScriptMod(textures, scriptLegacy, textureName);
                                        if (index == -1)
                                            throw new Exception();
                                        f = textures[index];
                                    }
                                    catch
                                    {
                                        len = fs.ReadInt32();
                                        fs.Skip(len);
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            errors += "Skipping not compatible content, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        }
                                        continue;
                                    }
                                    mod.textureCrc = f.crc;
                                    mod.textureName = f.name;
                                    mod.binaryModType = 0;
                                    len = fs.ReadInt32();
                                    mod.data = fs.ReadToBuffer(len);

                                    PixelFormat pixelFormat = f.pixfmt;
                                    Image image = new Image(mod.data, Image.ImageFormat.DDS);

                                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                        f.width / f.height)
                                    {
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            errors += "Error in texture: " + f.name + string.Format("_0x{0:X8}", f.crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        }
                                        continue;
                                    }

                                    PixelFormat newPixelFormat = pixelFormat;
                                    if (markToConvert)
                                        newPixelFormat = changeTextureType(pixelFormat, image.pixelFormat, f.flags);

                                    if (!image.checkDDSHaveAllMipmaps() ||
                                       (f.list.Find(s => s.path != "").numMips > 1 && image.mipMaps.Count() <= 1) ||
                                       (markToConvert && image.pixelFormat != newPixelFormat) ||
                                       (!markToConvert && image.pixelFormat != pixelFormat))
                                    {
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]PROCESSING_FILE Converting " + textureName);
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            Console.WriteLine("Converting/correcting texture: " + textureName);
                                        }
                                        bool dxt1HasAlpha = false;
                                        byte dxt1Threshold = 128;
                                        if (f.flags == TexProperty.TextureTypes.OneBitAlpha)
                                        {
                                            dxt1HasAlpha = true;
                                            if (image.pixelFormat == PixelFormat.ARGB ||
                                                image.pixelFormat == PixelFormat.DXT3 ||
                                                image.pixelFormat == PixelFormat.DXT5)
                                            {
                                                errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                                            }
                                        }
                                        image.correctMips(newPixelFormat, dxt1HasAlpha, dxt1Threshold);
                                        mod.data = image.StoreImageToDDS();
                                    }
                                }
                                mod.markConvert = markToConvert;
                                mods.Add(mod);
                            }
                        }
                    }
                    catch
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Mod is not compatible: " + file + Environment.NewLine;
                        }
                        continue;
                    }
                }
                else if (file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".xdelta", StringComparison.OrdinalIgnoreCase))
                {
                    BinaryMod mod = new BinaryMod();
                    try
                    {
                        string filename = Path.GetFileNameWithoutExtension(file);
                        string dlcName = "";
                        int posStr = 0;
                        if (filename.ToUpperInvariant()[0] == 'D')
                        {
                            string tmpDLC = filename.Split('-')[0];
                            int lenDLC = int.Parse(tmpDLC.Substring(1));
                            dlcName = filename.Substring(tmpDLC.Length + 1, lenDLC);
                            posStr += tmpDLC.Length + lenDLC + 1;
                            if (filename[posStr++] != '-')
                                throw new Exception();
                        }
                        else if (filename.ToUpperInvariant()[0] == 'B')
                        {
                            posStr += 1;
                        }
                        else
                            throw new Exception();
                        string tmpPkg = filename.Substring(posStr).Split('-')[0];
                        posStr += tmpPkg.Length + 1;
                        int lenPkg = int.Parse(tmpPkg.Substring(0));
                        string pkgName = filename.Substring(posStr, lenPkg);
                        posStr += lenPkg;
                        if (filename[posStr++] != '-')
                            throw new Exception();
                        if (filename.ToUpperInvariant()[posStr++] != 'E')
                            throw new Exception();
                        string tmpExp = filename.Substring(posStr);
                        mod.exportId = int.Parse(tmpExp.Substring(0));
                        if (dlcName != "")
                        {
                            if (gameId == MeType.ME1_TYPE)
                                mod.packagePath = @"\DLC\" + dlcName + @"\CookedPC\" + pkgName;
                            else if (gameId == MeType.ME2_TYPE)
                                mod.packagePath = @"\BioGame\DLC\" + dlcName + @"\CookedPC\" + pkgName;
                            else
                                mod.packagePath = @"\BIOGame\DLC\" + dlcName + @"\CookedPCConsole\" + pkgName;
                        }
                        else
                        {
                            if (gameId == MeType.ME1_TYPE || gameId == MeType.ME2_TYPE)
                                mod.packagePath = @"\BioGame\CookedPC\" + pkgName;
                            else
                                mod.packagePath = @"\BIOGame\CookedPCConsole\" + pkgName;
                        }
                        if (file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                            mod.binaryModType = 1;
                        else if (file.EndsWith(".xdelta", StringComparison.OrdinalIgnoreCase))
                            mod.binaryModType = 2;
                        mod.data = File.ReadAllBytes(file);
                        mods.Add(mod);
                    }
                    catch
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Filename not valid: " + file + Environment.NewLine;
                        }
                        continue;
                    }
                }
                else if (file.EndsWith(".tpf", StringComparison.OrdinalIgnoreCase))
                {
                    int indexTpf = -1;
                    IntPtr handle = IntPtr.Zero;
                    ZlibHelper.Zip zip = new ZlibHelper.Zip();
                    try
                    {
                        handle = zip.Open(file, ref numEntries, 1);
                        for (ulong i = 0; i < numEntries; i++)
                        {
                            result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                            if (result != 0)
                                throw new Exception();
                            fileName = fileName.Trim();
                            if (Path.GetExtension(fileName).ToLowerInvariant() == ".def" ||
                                Path.GetExtension(fileName).ToLowerInvariant() == ".log")
                            {
                                indexTpf = (int)i;
                                break;
                            }
                            result = zip.GoToNextFile(handle);
                            if (result != 0)
                                throw new Exception();
                        }
                        byte[] listText = new byte[dstLen];
                        result = zip.ReadCurrentFile(handle, listText, dstLen);
                        if (result != 0)
                            throw new Exception();
                        ddsList = Encoding.ASCII.GetString(listText).Trim('\0').Replace("\r", "").TrimEnd('\n').Split('\n');

                        result = zip.GoToFirstFile(handle);
                        if (result != 0)
                            throw new Exception();

                        for (uint i = 0; i < numEntries; i++)
                        {
                            if (i == indexTpf)
                            {
                                result = zip.GoToNextFile(handle);
                                continue;
                            }
                            BinaryMod mod = new BinaryMod();
                            try
                            {
                                uint crc = 0;
                                result = zip.GetCurrentFileInfo(handle, ref fileName, ref dstLen);
                                if (result != 0)
                                    throw new Exception();
                                string filename = Path.GetFileName(fileName).Trim();
                                foreach (string dds in ddsList)
                                {
                                    string ddsFile = dds.Split('|')[1];
                                    if (ddsFile.ToLowerInvariant().Trim() != filename.ToLowerInvariant())
                                        continue;
                                    crc = uint.Parse(dds.Split('|')[0].Substring(2), System.Globalization.NumberStyles.HexNumber);
                                    break;
                                }
                                if (crc == 0)
                                {
                                    if (Path.GetExtension(filename).ToLowerInvariant() != ".def" &&
                                        Path.GetExtension(filename).ToLowerInvariant() != ".log")
                                    {
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                            Console.Out.Flush();
                                        }
                                        else
                                        {
                                            errors += "Skipping file: " + filename + " not found in definition file, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                        }
                                    }
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                                if (foundCrcList.Count == 0)
                                {
                                    Console.WriteLine("Texture skipped. File " + filename + string.Format(" - 0x{0:X8}", crc) +
                                        " is not present in your game setup - mod: " + relativeFilePath);
                                    zip.GoToNextFile(handle);
                                    continue;
                                }

                                string textureName = foundCrcList[0].name;
                                mod.textureName = textureName;
                                mod.binaryModType = 0;
                                mod.textureCrc = crc;
                                mod.data = new byte[dstLen];
                                result = zip.ReadCurrentFile(handle, mod.data, dstLen);
                                if (result != 0)
                                {
                                    zip.GoToNextFile(handle);
                                    if (ipc)
                                    {
                                        Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                        Console.Out.Flush();
                                    }
                                    else
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + ", skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    }
                                    continue;
                                }

                                PixelFormat pixelFormat = foundCrcList[0].pixfmt;
                                Image image = new Image(mod.data, Path.GetExtension(filename));

                                if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                                    foundCrcList[0].width / foundCrcList[0].height)
                                {
                                    zip.GoToNextFile(handle);
                                    if (ipc)
                                    {
                                        Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                        Console.Out.Flush();
                                    }
                                    else
                                    {
                                        errors += "Error in texture: " + textureName + string.Format("_0x{0:X8}", crc) + " This texture has wrong aspect ratio, skipping texture, entry: " + (i + 1) + " - mod: " + file + Environment.NewLine;
                                    }
                                    continue;
                                }

                                PixelFormat newPixelFormat = pixelFormat;
                                if (markToConvert)
                                    newPixelFormat = changeTextureType(pixelFormat, image.pixelFormat, foundCrcList[0].flags);

                                if (!image.checkDDSHaveAllMipmaps() ||
                                   (foundCrcList[0].list.Find(s => s.path != "").numMips > 1 && image.mipMaps.Count() <= 1) ||
                                   (markToConvert && image.pixelFormat != newPixelFormat) ||
                                   (!markToConvert && image.pixelFormat != pixelFormat))
                                {
                                    if (ipc)
                                    {
                                        Console.WriteLine("[IPC]PROCESSING_FILE Converting " + relativeFilePath);
                                        Console.Out.Flush();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Converting/correcting texture: " + textureName);
                                    }
                                    bool dxt1HasAlpha = false;
                                    byte dxt1Threshold = 128;
                                    if (foundCrcList[0].flags == TexProperty.TextureTypes.OneBitAlpha)
                                    {
                                        dxt1HasAlpha = true;
                                        if (image.pixelFormat == PixelFormat.ARGB ||
                                            image.pixelFormat == PixelFormat.DXT3 ||
                                            image.pixelFormat == PixelFormat.DXT5)
                                        {
                                            errors += "Warning for texture: " + textureName + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                                        }
                                    }
                                    image.correctMips(newPixelFormat, dxt1HasAlpha, dxt1Threshold);
                                    mod.data = image.StoreImageToDDS();
                                }
                                mod.markConvert = markToConvert;
                                mods.Add(mod);
                            }
                            catch
                            {
                                if (ipc)
                                {
                                    Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                                    Console.Out.Flush();
                                }
                                else
                                {
                                    errors += "Skipping not compatible content, entry: " + (i + 1) + " file: " + fileName + " - mod: " + file + Environment.NewLine;
                                }
                            }
                            zip.GoToNextFile(handle);
                        }
                        zip.Close(handle);
                        handle = IntPtr.Zero;
                    }
                    catch
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Mod is not compatible: " + file + Environment.NewLine;
                        }
                        if (handle != IntPtr.Zero)
                            zip.Close(handle);
                        handle = IntPtr.Zero;
                        continue;
                    }
                }
                else if (file.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    BinaryMod mod = new BinaryMod();
                    string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!filename.Contains("0x"))
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }
                    uint crc;
                    string crcStr = filename.Substring(idx + 2, 8);
                    try
                    {
                        crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        Console.WriteLine("Texture skipped. Texture " + relativeFilePath + " is not present in your game setup.");
                        continue;
                    }

                    idx = filename.IndexOf("-memconvert");
                    if (idx > 0)
                        markToConvert = true;

                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        PixelFormat pixelFormat = foundCrcList[0].pixfmt;

                        mod.data = fs.ReadToBuffer((int)fs.Length);
                        Image image = new Image(mod.data, Image.ImageFormat.DDS);

                        if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                            foundCrcList[0].width / foundCrcList[0].height)
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                            continue;
                        }

                        PixelFormat newPixelFormat = pixelFormat;
                        if (markToConvert)
                            newPixelFormat = changeTextureType(pixelFormat, image.pixelFormat, foundCrcList[0].flags);

                        if (!image.checkDDSHaveAllMipmaps() ||
                           (foundCrcList[0].list.Find(s => s.path != "").numMips > 1 && image.mipMaps.Count() <= 1) ||
                           (markToConvert && image.pixelFormat != newPixelFormat) ||
                           (!markToConvert && image.pixelFormat != pixelFormat))
                        {
                            if (ipc)
                            {
                                Console.WriteLine("[IPC]PROCESSING_FILE Converting " + Path.GetFileName(file));
                                Console.Out.Flush();
                            }
                            else
                            {
                                Console.WriteLine("Converting/correcting texture: " + relativeFilePath);
                            }
                            bool dxt1HasAlpha = false;
                            byte dxt1Threshold = 128;
                            if (foundCrcList[0].flags == TexProperty.TextureTypes.OneBitAlpha)
                            {
                                dxt1HasAlpha = true;
                                if (image.pixelFormat == PixelFormat.ARGB ||
                                    image.pixelFormat == PixelFormat.DXT3 ||
                                    image.pixelFormat == PixelFormat.DXT5)
                                {
                                    errors += "Warning for texture: " + Path.GetFileName(file) + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                                }
                            }
                            image.correctMips(newPixelFormat, dxt1HasAlpha, dxt1Threshold);
                            mod.data = image.StoreImageToDDS();
                        }

                        mod.textureName = foundCrcList[0].name;
                        mod.binaryModType = 0;
                        mod.textureCrc = crc;
                        mod.markConvert = markToConvert;
                        mods.Add(mod);
                    }
                }
                else if (
                    file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    BinaryMod mod = new BinaryMod();
                    string filename = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                    if (!filename.Contains("0x"))
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }
                    int idx = filename.IndexOf("0x");
                    if (filename.Length - idx < 10)
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }
                    uint crc;
                    string crcStr = filename.Substring(idx + 2, 8);
                    try
                    {
                        crc = uint.Parse(crcStr, System.Globalization.NumberStyles.HexNumber);
                    }
                    catch
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Texture filename not valid: " + Path.GetFileName(file) + " Texture filename must include texture CRC (0xhhhhhhhh). Skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }

                    List<FoundTexture> foundCrcList = textures.FindAll(s => s.crc == crc);
                    if (foundCrcList.Count == 0)
                    {
                        Console.WriteLine("Texture skipped. Texture " + relativeFilePath + " is not present in your game setup.");
                        continue;
                    }

                    idx = filename.IndexOf("-memconvert");
                    if (idx > 0)
                        markToConvert = true;

                    PixelFormat pixelFormat = foundCrcList[0].pixfmt;
                    Image image = new Image(file, Image.ImageFormat.Unknown).convertToARGB();

                    if (image.mipMaps[0].origWidth / image.mipMaps[0].origHeight !=
                        foundCrcList[0].width / foundCrcList[0].height)
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_FILE_NOT_COMPATIBLE " + relativeFilePath);
                            Console.Out.Flush();
                        }
                        else
                        {
                            errors += "Error in texture: " + Path.GetFileName(file) + " This texture has wrong aspect ratio, skipping texture..." + Environment.NewLine;
                        }
                        continue;
                    }

                    PixelFormat newPixelFormat = pixelFormat;
                    if (markToConvert)
                        newPixelFormat = changeTextureType(pixelFormat, image.pixelFormat, foundCrcList[0].flags);

                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE Converting " + Path.GetFileName(file));
                        Console.Out.Flush();
                    }
                    else
                    {
                        Console.WriteLine("Converting/correcting texture: " + relativeFilePath);
                    }
                    bool dxt1HasAlpha = false;
                    byte dxt1Threshold = 128;
                    if (foundCrcList[0].flags == TexProperty.TextureTypes.OneBitAlpha)
                    {
                        dxt1HasAlpha = true;
                        if (image.pixelFormat == PixelFormat.ARGB ||
                            image.pixelFormat == PixelFormat.DXT3 ||
                            image.pixelFormat == PixelFormat.DXT5)
                        {
                            errors += "Warning for texture: " + Path.GetFileName(file) + ". This texture converted from full alpha to binary alpha." + Environment.NewLine;
                        }
                    }
                    image.correctMips(newPixelFormat, dxt1HasAlpha, dxt1Threshold);
                    mod.data = image.StoreImageToDDS();
                    mod.textureName = foundCrcList[0].name;
                    mod.binaryModType = 0;
                    mod.textureCrc = crc;
                    mod.markConvert = markToConvert;
                    mods.Add(mod);
                }

                for (int l = 0; l < mods.Count; l++)
                {
                    MipMaps.FileMod fileMod = new MipMaps.FileMod();
                    Stream dst = MipMaps.compressData(mods[l].data);
                    dst.SeekBegin();
                    fileMod.offset = outFs.Position;
                    fileMod.size = dst.Length;

                    if (mods[l].binaryModType == 1)
                    {
                        fileMod.tag = MipMaps.FileBinaryTag;
                        if (mods[l].packagePath.Contains("\\DLC\\"))
                        {
                            string dlcName = mods[l].packagePath.Split('\\')[3];
                            fileMod.name = "D" + dlcName.Length + "-" + dlcName + "-";
                        }
                        else
                        {
                            fileMod.name = "B";
                        }
                        fileMod.name += Path.GetFileName(mods[l].packagePath).Length + "-" + Path.GetFileName(mods[l].packagePath) + "-E" + mods[l].exportId + ".bin";

                        outFs.WriteInt32(mods[l].exportId);
                        outFs.WriteStringASCIINull(mods[l].packagePath);
                    }
                    else if (mods[l].binaryModType == 2)
                    {
                        fileMod.tag = MipMaps.FileXdeltaTag;
                        if (mods[l].packagePath.Contains("\\DLC\\"))
                        {
                            string dlcName = mods[l].packagePath.Split('\\')[3];
                            fileMod.name = "D" + dlcName.Length + "-" + dlcName + "-";
                        }
                        else
                        {
                            fileMod.name = "B";
                        }
                        fileMod.name += Path.GetFileName(mods[l].packagePath).Length + "-" + Path.GetFileName(mods[l].packagePath) + "-E" + mods[l].exportId + ".xdelta";

                        outFs.WriteInt32(mods[l].exportId);
                        outFs.WriteStringASCIINull(mods[l].packagePath);
                    }
                    else
                    {
                        if (mods[l].markConvert)
                            fileMod.tag = MipMaps.FileTextureTag2;
                        else
                            fileMod.tag = MipMaps.FileTextureTag;
                        fileMod.name = mods[l].textureName + string.Format("_0x{0:X8}", mods[l].textureCrc) + ".dds";
                        outFs.WriteStringASCIINull(mods[l].textureName);
                        outFs.WriteUInt32(mods[l].textureCrc);
                    }
                    outFs.WriteFromStream(dst, dst.Length);
                    modFiles.Add(fileMod);
                }
                mods.Clear();
            }

            if (modFiles.Count == 0)
            {
                outFs.Close();
                if (File.Exists(memFilePath))
                    File.Delete(memFilePath);
                if (ipc)
                {
                    Console.WriteLine("[IPC]ERROR_NO_BUILDABLE_FILES");
                    Console.Out.Flush();
                }
                return false;
            }

            long pos = outFs.Position;
            outFs.SeekBegin();
            outFs.WriteUInt32(TreeScan.TextureModTag);
            outFs.WriteUInt32(TreeScan.TextureModVersion);
            outFs.WriteInt64(pos);
            outFs.JumpTo(pos);
            outFs.WriteUInt32((uint)gameId);
            outFs.WriteInt32(modFiles.Count);
            for (int i = 0; i < modFiles.Count; i++)
            {
                outFs.WriteUInt32(modFiles[i].tag);
                outFs.WriteStringASCIINull(modFiles[i].name);
                outFs.WriteInt64(modFiles[i].offset);
                outFs.WriteInt64(modFiles[i].size);
            }

            outFs.Close();

            return true;
        }

        static public byte[] calculateMD5(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (MD5 md5 = MD5.Create())
                {
                    md5.Initialize();
                    return md5.ComputeHash(fs);
                }
            }
        }

        static public byte[] calculateMD5Fast(string filePath)
        {
            long size = new FileInfo(filePath).Length;
            size = Math.Min(4000, size);
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = fs.ReadToBuffer(size);
                using (MD5 md5 = MD5.Create())
                {
                    md5.Initialize();
                    return md5.ComputeHash(buffer, 0, (int)size);
                }
            }
        }

        static public List<string> detectMods(MeType gameType)
        {
            List<string> mods = new List<string>();

            for (int l = 0; l < modsEntries.Count(); l++)
            {
                if (!File.Exists(GameData.GamePath + modsEntries[l].path))
                    continue;
                byte[] md5 = calculateMD5(GameData.GamePath + modsEntries[l].path);
                if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, modsEntries[l].md5))
                {
                    if (!mods.Exists(s => s == modsEntries[l].modName))
                        mods.Add(modsEntries[l].modName);
                }
            }

            return mods;
        }

        static public List<string> detectBrokenMod(MeType gameType)
        {
            List<string> packageMainFiles = null;
            List<string> packageDLCFiles = null;
            List<string> sfarFiles = null;
            List<string> mods = new List<string>();

            if (gameType == MeType.ME1_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.*",
                SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                        StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("localshadercache-pc-d3d-sm3.upk"));
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("refshadercache-pc-d3d-sm3.upk"));
            }
            else if (gameType == MeType.ME2_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (gameType == MeType.ME3_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                    sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                    for (int i = 0; i < sfarFiles.Count; i++)
                    {
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                            sfarFiles.RemoveAt(i--);
                    }
                    packageDLCFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
                }
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
            }

            packageMainFiles.Sort(StringComparer.OrdinalIgnoreCase);
            if (packageDLCFiles != null)
                packageDLCFiles.Sort(StringComparer.OrdinalIgnoreCase);
            if (sfarFiles != null)
                sfarFiles.Sort(StringComparer.OrdinalIgnoreCase);

            for (int l = 0; l < badMOD.Count(); l++)
            {
                if (!File.Exists(GameData.GamePath + badMOD[l].path))
                    continue;
                byte[] md5 = calculateMD5(GameData.GamePath + badMOD[l].path);
                if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, badMOD[l].md5))
                {
                    if (!mods.Exists(s => s == badMOD[l].modName))
                        mods.Add(badMOD[l].modName);
                }
            }

            return mods;
        }

        static public bool unpackSFARisNeeded()
        {
            if (Directory.Exists(GameData.DLCData))
            {
                List<string> sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                for (int i = 0; i < sfarFiles.Count; i++)
                {
                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                        return true;
                }
            }

            return false;
        }

        static public bool checkGameFiles(MeType gameType, ref string errors, ref List<string> mods, MainWindow mainWindow, bool ipc)
        {
            bool vanilla = true;
            List<string> packageMainFiles = null;
            List<string> packageDLCFiles = null;
            List<string> sfarFiles = null;
            List<string> tfcFiles = null;
            MD5FileEntry[] entries = null;

            if (mainWindow != null)
                startTimer();

            if (gameType == MeType.ME1_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.*",
                SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                        StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("localshadercache-pc-d3d-sm3.upk"));
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("refshadercache-pc-d3d-sm3.upk"));
                entries = new MD5FileEntry[Program.entriesME1.Count() + Program.entriesME1PL.Count()];
                for (int i = 0; i < Program.entriesME1.Count(); i++)
                {
                    entries[i] = Program.entriesME1[i];
                }
                for (int i = 0; i < Program.entriesME1PL.Count(); i++)
                {
                    entries[Program.entriesME1.Count() + i] = Program.entriesME1PL[i];
                }
            }
            else if (gameType == MeType.ME2_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                tfcFiles = Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                    tfcFiles.AddRange(Directory.GetFiles(GameData.DLCData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList());
                }
                entries = Program.entriesME2;
            }
            else if (gameType == MeType.ME3_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                tfcFiles = Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                    sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                    for (int i = 0; i < sfarFiles.Count; i++)
                    {
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                            sfarFiles.RemoveAt(i--);
                    }
                    sfarFiles.Add(GameData.bioGamePath + "\\Patches\\PCConsole\\Patch_001.sfar");
                    packageDLCFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
                    tfcFiles.AddRange(Directory.GetFiles(GameData.DLCData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList());
                }
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
                entries = Program.entriesME3;
            }

            packageMainFiles.Sort(StringComparer.OrdinalIgnoreCase);
            int allFilesCount = packageMainFiles.Count();
            int progress = 0;
            if (packageDLCFiles != null)
            {
                packageDLCFiles.Sort(StringComparer.OrdinalIgnoreCase);
                allFilesCount += packageDLCFiles.Count();
            }
            if (sfarFiles != null)
            {
                sfarFiles.Sort(StringComparer.OrdinalIgnoreCase);
                allFilesCount += sfarFiles.Count();
            }
            if (tfcFiles != null)
            {
                tfcFiles.Sort(StringComparer.OrdinalIgnoreCase);
                allFilesCount += tfcFiles.Count();
            }

            mods.Clear();
            FileStream fs = null;
            if (generateModsMd5Entries)
                fs = new FileStream("MD5ModFileEntry" + (int)gameType + ".cs", FileMode.Create, FileAccess.Write);
            if (generateMd5Entries)
                fs = new FileStream("MD5FileEntry" + (int)gameType + ".cs", FileMode.Create, FileAccess.Write);

            int lastProgress = -1;
            for (int l = 0; l < packageMainFiles.Count; l++)
            {
                if (mainWindow != null)
                {
                    mainWindow.updateStatusLabel("Checking main PCC files - " + (l + 1) + " of " + packageMainFiles.Count);
                }
                int newProgress = (l + progress) * 100 / allFilesCount;
                if (ipc)
                {
                    if (lastProgress != newProgress)
                    {
                        Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                        Console.Out.Flush();
                        lastProgress = newProgress;
                    }
                }
                byte[] md5 = calculateMD5(packageMainFiles[l]);
                bool found = false;
                for (int p = 0; p < entries.Count(); p++)
                {
                    if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                    continue;

                found = false;
                for (int p = 0; p < modsEntries.Count(); p++)
                {
                    if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, modsEntries[p].md5))
                    {
                        found = true;
                        if (!mods.Exists(s => s == modsEntries[p].modName))
                            mods.Add(modsEntries[p].modName);
                        break;
                    }
                }
                if (found)
                    continue;

                found = false;
                for (int p = 0; p < badMOD.Count(); p++)
                {
                    if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, badMOD[p].md5))
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                    continue;

                int index = -1;
                for (int p = 0; p < entries.Count(); p++)
                {
                    if (GameData.RelativeGameData(packageMainFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                    {
                        if (generateMd5Entries)
                        {
                            if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                            {
                                index = p;
                                break;
                            }
                        }
                        else
                        {
                            index = p;
                            break;
                        }
                    }
                }
                if (!generateMd5Entries && index == -1)
                    continue;
                if (generateMd5Entries && index != -1)
                    continue;

                vanilla = false;

                if (generateModsMd5Entries)
                {
                    fs.WriteStringASCII("new MD5ModFileEntry\n{\npath = @\"" + GameData.RelativeGameData(packageMainFiles[l]) + "\",\nmd5 = new byte[] { ");
                    for (int i = 0; i < md5.Length; i++)
                    {
                        fs.WriteStringASCII(string.Format("0x{0:X2}, ", md5[i]));
                    }
                    fs.WriteStringASCII("},\nmodName = \"\",\n},\n");
                }
                if (generateMd5Entries)
                {
                    fs.WriteStringASCII("new MD5FileEntry\n{\npath = @\"" + GameData.RelativeGameData(packageMainFiles[l]) + "\",\nmd5 = new byte[] { ");
                    for (int i = 0; i < md5.Length; i++)
                    {
                        fs.WriteStringASCII(string.Format("0x{0:X2}, ", md5[i]));
                    }
                    fs.WriteStringASCII("},\nsize = " + new FileInfo(packageMainFiles[l]).Length + ",\n},\n");
                }

                if (!generateMd5Entries && !generateModsMd5Entries)
                {
                    errors += "File " + packageMainFiles[l] + " has wrong MD5 checksum: ";
                    for (int i = 0; i < md5.Count(); i++)
                    {
                        errors += string.Format("{0:x2}", md5[i]);
                    }
                    errors += "\n, expected: ";
                    for (int i = 0; i < entries[index].md5.Count(); i++)
                    {
                        errors += string.Format("{0:x2}", entries[index].md5[i]);
                    }
                    errors += Environment.NewLine;
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR " + packageMainFiles[l]);
                        Console.Out.Flush();
                    }
                }
            }
            progress += packageMainFiles.Count();

            if (packageDLCFiles != null)
            {
                for (int l = 0; l < packageDLCFiles.Count; l++)
                {
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("Checking DLC PCC files - " + (l + 1) + " of " + packageDLCFiles.Count);
                    }
                    if (ipc)
                    {
                        int newProgress = (l + progress) * 100 / allFilesCount;
                        if (lastProgress != newProgress)
                        {
                            Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                            Console.Out.Flush();
                            lastProgress = newProgress;
                        }
                    }
                    byte[] md5 = calculateMD5(packageDLCFiles[l]);
                    bool found = false;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;

                    found = false;
                    for (int p = 0; p < modsEntries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, modsEntries[p].md5))
                        {
                            found = true;
                            if (!mods.Exists(s => s == modsEntries[p].modName))
                                mods.Add(modsEntries[p].modName);
                            break;
                        }
                    }
                    if (found)
                        continue;

                    found = false;
                    for (int p = 0; p < badMOD.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, badMOD[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;

                    int index = -1;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (GameData.RelativeGameData(packageDLCFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                        {
                            if (generateMd5Entries)
                            {
                                if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                                {
                                    index = p;
                                    break;
                                }
                            }
                            else
                            {
                                index = p;
                                break;
                            }
                        }
                    }
                    if (!generateMd5Entries && index == -1)
                        continue;
                    if (generateMd5Entries && index != -1)
                        continue;

                    vanilla = false;

                    if (generateModsMd5Entries)
                    {
                        fs.WriteStringASCII("new MD5ModFileEntry\n{\npath = @\"" + GameData.RelativeGameData(packageDLCFiles[l]) + "\",\nmd5 = new byte[] { ");
                        for (int i = 0; i < md5.Length; i++)
                        {
                            fs.WriteStringASCII(string.Format("0x{0:X2}, ", md5[i]));
                        }
                        fs.WriteStringASCII("},\nmodName = \"\",\n},\n");
                    }
                    if (generateMd5Entries)
                    {
                        fs.WriteStringASCII("new MD5FileEntry\n{\npath = @\"" + GameData.RelativeGameData(packageDLCFiles[l]) + "\",\nmd5 = new byte[] { ");
                        for (int i = 0; i < md5.Length; i++)
                        {
                            fs.WriteStringASCII(string.Format("0x{0:X2}, ", md5[i]));
                        }
                        fs.WriteStringASCII("},\nsize = " + new FileInfo(packageDLCFiles[l]).Length + ",\n},\n");
                    }

                    if (!generateMd5Entries && !generateModsMd5Entries)
                    {
                        errors += "File " + packageDLCFiles[l] + " has wrong MD5 checksum: ";
                        for (int i = 0; i < md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", md5[i]);
                        }
                        errors += "\n, expected: ";
                        for (int i = 0; i < entries[index].md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", entries[index].md5[i]);
                        }
                        errors += Environment.NewLine;

                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR " + packageDLCFiles[l]);
                            Console.Out.Flush();
                        }
                    }
                }
                progress += packageDLCFiles.Count();
            }

            if (sfarFiles != null)
            {
                for (int l = 0; l < sfarFiles.Count; l++)
                {
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("Checking DLC archive files - " + (l + 1) + " of " + sfarFiles.Count);
                    }
                    if (ipc)
                    {
                        int newProgress = (l + progress) * 100 / allFilesCount;
                        if (lastProgress != newProgress)
                        {
                            Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                            Console.Out.Flush();
                            lastProgress = newProgress;
                        }
                    }
                    byte[] md5 = calculateMD5(sfarFiles[l]);
                    bool found = false;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;
                    int index = -1;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (GameData.RelativeGameData(sfarFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                        {
                            index = p;
                            break;
                        }
                    }
                    if (index == -1)
                        continue;

                    vanilla = false;

                    if (!generateMd5Entries && !generateModsMd5Entries)
                    {
                        errors += "File " + sfarFiles[l] + " has wrong MD5 checksum: ";
                        for (int i = 0; i < md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", md5[i]);
                        }
                        errors += "\n, expected: ";
                        for (int i = 0; i < entries[index].md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", entries[index].md5[i]);
                        }
                        errors += Environment.NewLine;

                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR " + sfarFiles[l]);
                            Console.Out.Flush();
                        }
                    }
                }
                progress += sfarFiles.Count();
            }

            if (tfcFiles != null)
            {
                for (int l = 0; l < tfcFiles.Count; l++)
                {
                    if (mainWindow != null)
                    {
                        mainWindow.updateStatusLabel("Checking TFC archive files - " + (l + 1) + " of " + tfcFiles.Count);
                    }
                    if (ipc)
                    {
                        int newProgress = (l + progress) * 100 / allFilesCount;
                        if (lastProgress != newProgress)
                        {
                            Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                            Console.Out.Flush();
                            lastProgress = newProgress;
                        }
                    }
                    byte[] md5 = calculateMD5(tfcFiles[l]);
                    bool found = false;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;
                    int index = -1;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (GameData.RelativeGameData(tfcFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                        {
                            index = p;
                            break;
                        }
                    }
                    if (index == -1)
                        continue;

                    vanilla = false;

                    if (!generateMd5Entries && !generateModsMd5Entries)
                    {
                        errors += "File " + tfcFiles[l] + " has wrong MD5 checksum: ";
                        for (int i = 0; i < md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", md5[i]);
                        }
                        errors += "\n, expected: ";
                        for (int i = 0; i < entries[index].md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", entries[index].md5[i]);
                        }
                        errors += Environment.NewLine;
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR " + tfcFiles[l]);
                            Console.Out.Flush();
                        }
                    }
                }
                progress += tfcFiles.Count();
            }
            if (generateModsMd5Entries || generateMd5Entries)
                fs.Close();

            if (mainWindow != null)
            {
                var time = stopTimer();
                mainWindow.updateStatusLabel("Checking game files. Process total time: " + Misc.getTimerFormat(time));
            }

            return vanilla;
        }

    }
}
