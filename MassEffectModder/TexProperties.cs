/*
 * MassEffectModder
 *
 * Copyright (C) 2015-2016 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
using System.Collections.Generic;
using System.IO;
using StreamHelpers;

namespace MassEffectModder
{
    public class TexProperty
    {
        public struct TexPropertyEntry
        {
            public string type;
            public string name;
            public string valueName;
            public int valueInt;
            public float valueFloat;
            public bool valueBool;
            public byte[] valueRaw;
            public byte[] valueStruct;
            public int index;
            public bool fetched;
        }
        public List<TexPropertyEntry> texPropertyList;
        public int propertyEndOffset;
        uint headerData = 0;
        Package package;

        public TexProperty(Package pkg, byte[] data)
        {
            package = pkg;
            texPropertyList = new List<TexPropertyEntry>();
            headerData = BitConverter.ToUInt32(data, 0);
            getProperty(data, 4);
        }

        private void getProperty(byte[] data, int offset)
        {
            TexPropertyEntry texProperty = new TexPropertyEntry();
            int size, valueRawPos, nextOffset;

            texProperty.name = package.getName(BitConverter.ToInt32(data, offset));
            if (texProperty.name == "None")
            {
                nextOffset = offset;
                propertyEndOffset = valueRawPos = offset + 8;
                size = 0;
            }
            else
            {
                texProperty.type = package.getName(BitConverter.ToInt32(data, offset + 8));
                size = BitConverter.ToInt32(data, offset + 16);
                texProperty.index = BitConverter.ToInt32(data, offset + 20);

                valueRawPos = offset + 24;

                switch (texProperty.type)
                {
                    case "IntProperty":
                    case "StrProperty":
                    case "FloatProperty":
                    case "NameProperty":
                        break;
                    case "StructProperty":
                        size += 8;
                        break;
                    case "ByteProperty":
                        if (GameData.gameType == MeType.ME3_TYPE)
                            size += 8;
                        break;
                    case "BoolProperty":
                        if (GameData.gameType == MeType.ME3_TYPE)
                            size = 1;
                        else
                            size = 4;
                        break;
                    default:
                        throw new Exception();
                }
                nextOffset = valueRawPos + size;
            }
            texProperty.valueRaw = new byte[size];
            Array.Copy(data, valueRawPos, texProperty.valueRaw, 0, size);
            texPropertyList.Add(texProperty);

            if (nextOffset != offset)
                getProperty(data, nextOffset);
        }

        public TexPropertyEntry getProperty(string name)
        {
            fetchValue(name);
            return texPropertyList.Find(s => s.name == name);
        }

        public void fetchValue(string name)
        {
            fetchValue(texPropertyList.IndexOf(texPropertyList.Find(s => s.name == name)));
        }

        public void fetchValue(int index)
        {
            if (index < 0 || index >= texPropertyList.Count)
                new Exception("");
            TexPropertyEntry texProperty = texPropertyList[index];
            if (texProperty.fetched || texProperty.type == null)
                return;
            switch (texProperty.type)
            {
                case "IntProperty":
                    texProperty.valueInt = BitConverter.ToInt32(texProperty.valueRaw, 0);
                    break;
                case "ByteProperty":
                    texProperty.valueName = package.getName(BitConverter.ToInt32(texProperty.valueRaw, 0));
                    if (GameData.gameType == MeType.ME3_TYPE)
                    {
                        texProperty.valueName = package.getName(BitConverter.ToInt32(texProperty.valueRaw, 8));
                        texProperty.valueInt = BitConverter.ToInt32(texProperty.valueRaw, 12);
                    }
                    else
                    {
                        texProperty.valueInt = BitConverter.ToInt32(texProperty.valueRaw, 4);
                    }
                    break;
                case "BoolProperty":
                    texProperty.valueBool = texProperty.valueRaw[0] != 0;
                    break;
                case "StrProperty":
                    break;
                case "FloatProperty":
                    texProperty.valueFloat = BitConverter.ToSingle(texProperty.valueRaw, 0);
                    break;
                case "NameProperty":
                    texProperty.valueName = package.getName(BitConverter.ToInt32(texProperty.valueRaw, 0));
                    texProperty.valueInt = BitConverter.ToInt32(texProperty.valueRaw, 4);
                    break;
                case "StructProperty":
                    texProperty.valueName = package.getName(BitConverter.ToInt32(texProperty.valueRaw, 0));
                    texProperty.valueInt = BitConverter.ToInt32(texProperty.valueRaw, 4);
                    texProperty.valueStruct = new byte[texProperty.valueRaw.Length - 8];
                    Array.Copy(texProperty.valueRaw, 8, texProperty.valueStruct, 0, texProperty.valueStruct.Length);
                    break;
                default:
                    throw new Exception();
            }
            texProperty.fetched = true;
            texPropertyList[index] = texProperty;
        }

        public string getDisplayString(int index)
        {
            string result = "";
            if (index < 0 || index >= texPropertyList.Count)
                new Exception("");

            fetchValue(index);
            TexPropertyEntry texProperty = texPropertyList[index];
            if (texProperty.type == null)
                return result;

            result = "  " + texProperty.name + ": ";
            switch (texProperty.type)
            {
                case "IntProperty":
                    result += texProperty.valueInt + "\n";
                    break;
                case "ByteProperty":
                    result += texProperty.valueName + ": ";
                    result += texProperty.valueInt + "\n";
                    break;
                case "BoolProperty":
                    result += texProperty.valueBool + "\n";
                    break;
                case "StrProperty":
                    result += "\n";
                    break;
                case "FloatProperty":
                    result += texProperty.valueFloat + "\n";
                    break;
                case "NameProperty":
                    result += texProperty.valueName + ": ";
                    result += texProperty.valueInt + "\n";
                    break;
                case "StructProperty":
                    result += texProperty.valueName + ": ";
                    result += texProperty.valueInt + "\n";
                    break;
                default:
                    throw new Exception();
            }

            return result;
        }
        public bool exists(string name)
        {
            return texPropertyList.Exists(s => s.name == name);
        }

        public void setIntValue(string name, int value)
        {
            TexPropertyEntry texProperty = texPropertyList.Find(s => s.name == name);
            if (texProperty.type != "IntProperty")
                throw new Exception();
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, texProperty.valueRaw, 0, sizeof(int));
            texProperty.valueInt = value;
            texPropertyList[texPropertyList.FindIndex(s => s.name == name)] = texProperty;
        }

        public void setFloatValue(string name, float value)
        {
            TexPropertyEntry texProperty = texPropertyList.Find(s => s.name == name);
            if (texProperty.type != "FloatProperty")
                throw new Exception();
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, texProperty.valueRaw, 0, sizeof(float));
            texProperty.valueFloat = value;
            texPropertyList[texPropertyList.FindIndex(s => s.name == name)] = texProperty;
        }

        public void setByteValue(string name, string valueName, int valueInt = 0)
        {
            TexPropertyEntry texProperty = texPropertyList.Find(s => s.name == name);
            if (texProperty.type != "ByteProperty")
                throw new Exception();
            if (GameData.gameType == MeType.ME3_TYPE)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(package.getNameId(valueName)), 8, texProperty.valueRaw, 8, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(valueInt), 0, texProperty.valueRaw, 12, sizeof(int));
            }
            else
            {
                Buffer.BlockCopy(BitConverter.GetBytes(package.getNameId(valueName)), 0, texProperty.valueRaw, 0, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(valueInt), 0, texProperty.valueRaw, 4, sizeof(int));
            }
            texProperty.valueName = valueName;
            texProperty.valueInt = valueInt;
            texPropertyList[texPropertyList.FindIndex(s => s.name == name)] = texProperty;
        }

        public void setBoolValue(string name, bool value)
        {
            TexPropertyEntry texProperty = texPropertyList.Find(s => s.name == name);
            if (texProperty.type != "BoolProperty")
                throw new Exception();
            if (value)
                texProperty.valueRaw[0] = 1;
            else
                texProperty.valueRaw[0] = 0;
            texProperty.valueBool = value;
            texPropertyList[texPropertyList.FindIndex(s => s.name == name)] = texProperty;
        }

        public void setNameValue(string name, string valueName, int valueInt = 0)
        {
            TexPropertyEntry texProperty = texPropertyList.Find(s => s.name == name);
            if (texProperty.type != "NameProperty")
                throw new Exception();
            Buffer.BlockCopy(BitConverter.GetBytes(package.getNameId(valueName)), 0, texProperty.valueRaw, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(valueInt), 0, texProperty.valueRaw, 4, sizeof(int));
            texProperty.valueName = valueName;
            texProperty.valueInt = valueInt;
            texPropertyList[texPropertyList.FindIndex(s => s.name == name)] = texProperty;
        }

        public void setStructValue(string name, string valueName, byte[] valueStruct)
        {
            fetchValue(name);
            TexPropertyEntry texProperty = texPropertyList.Find(s => s.name == name);
            if (texProperty.type != "StructProperty" || texProperty.valueStruct.Length != valueStruct.Length)
                throw new Exception();
            Buffer.BlockCopy(BitConverter.GetBytes(package.getNameId(valueName)), 0, texProperty.valueRaw, 0, sizeof(int));
            Buffer.BlockCopy(valueStruct, 0, texProperty.valueRaw, 8, valueStruct.Length);
            texProperty.valueName = valueName;
            Buffer.BlockCopy(valueStruct, 0, texProperty.valueStruct, 0, valueStruct.Length);
            texPropertyList[texPropertyList.FindIndex(s => s.name == name)] = texProperty;
        }

        public byte[] toArray()
        {
            using (MemoryStream mem = new MemoryStream())
            {
                mem.WriteUInt32(headerData);
                for (int i = 0; i < texPropertyList.Count; i++)
                {
                    mem.WriteInt32(package.getNameId(texPropertyList[i].name));
                    mem.WriteInt32(0); // skip
                    if (texPropertyList[i].name == "None")
                        break;
                    mem.WriteInt32(package.getNameId(texPropertyList[i].type));
                    mem.WriteInt32(0); // skip
                    int size = texPropertyList[i].valueRaw.Length;
                    switch (texPropertyList[i].type)
                    {
                        case "StructProperty":
                            size -= 8;
                            break;
                        case "ByteProperty":
                            if (GameData.gameType == MeType.ME3_TYPE)
                                size -= 8;
                            break;
                        case "BoolProperty":
                            size = 0;
                            break;
                    }
                    mem.WriteInt32(size);
                    mem.WriteInt32(texPropertyList[i].index);
                    mem.Write(texPropertyList[i].valueRaw, 0, texPropertyList[i].valueRaw.Length);
                }

                return mem.ToArray();
            }
        }
    }
}
