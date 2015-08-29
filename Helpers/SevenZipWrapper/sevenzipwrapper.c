/*
 * 7Zip DLL Wrapper
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
#include "LzmaLib.h"

BOOL WINAPI DllMain(HINSTANCE hin, DWORD reason, LPVOID lpvReserved) { return TRUE; }

#define SEVENZIP_EXPORT __declspec(dllexport)

SEVENZIP_EXPORT int SevenZipDecompress(unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	size_t len = *dst_len, sLen = src_len - LZMA_PROPS_SIZE;

	int status = LzmaUncompress(dst, &len, &src[LZMA_PROPS_SIZE], &src_len, src, LZMA_PROPS_SIZE);
	if (status == SZ_OK)
		*dst_len = len;

	return status;
}

SEVENZIP_EXPORT int SevenZipCompress(int compression_level, unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	size_t len = *dst_len, propsSize = LZMA_PROPS_SIZE;

	int status = LzmaCompress(&dst[LZMA_PROPS_SIZE], &len, src, src_len, dst, &propsSize, compression_level, 1 << 16, -1, -1, -1, -1, -1);
	if (status == SZ_OK)
		*dst_len = len + LZMA_PROPS_SIZE;

	return status;
}
