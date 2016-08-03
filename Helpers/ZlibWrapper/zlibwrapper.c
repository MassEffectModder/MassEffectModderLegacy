/*
 * Zlib DLL Wrapper
 *
 * Copyright (C) 2015 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

#define _WIN32_WINNT 0x0501
#include <windows.h>
#include "zlib.h"

BOOL WINAPI DllMain(HINSTANCE hin, DWORD reason, LPVOID lpvReserved) { return TRUE; }

#define ZLIB_EXPORT __declspec(dllexport)

ZLIB_EXPORT int ZlibDecompress(unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	uLongf len = *dst_len;

	int status = uncompress((Bytef *)dst, &len, (Bytef *)src, (uLong)src_len);
	if (status == Z_OK)
		*dst_len = len;
	else
		*dst_len = 0;

	return status;
}

ZLIB_EXPORT int ZlibCompress(int compression_level, unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	uLongf len = *dst_len;

	int status = compress2((Bytef *)dst, &len, (Bytef *)src, (uLong)src_len, compression_level);
	if (status == Z_OK)
		*dst_len = len;
	else
		*dst_len = 0;

	return status;
}
