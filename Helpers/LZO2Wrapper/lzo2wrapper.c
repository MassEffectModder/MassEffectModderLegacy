/*
 * LZO DLL Wrapper
 *
 * Copyright (C) 2014 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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
#include "lzo1x.h"
#include "lzo_asm.h"

#if !_WIN64
#define USE_ASM
#endif

BOOL WINAPI DllMain(HINSTANCE hin, DWORD reason, LPVOID lpvReserved) { return TRUE; }

#define LZO_EXPORT __declspec(dllexport)

#define HEAP_ALLOC(var, size) \
	lzo_align_t __LZO_MMODEL var [ ((size) + (sizeof(lzo_align_t) - 1)) / sizeof(lzo_align_t) ]

static HEAP_ALLOC(wrkmem, LZO1X_999_MEM_COMPRESS);

LZO_EXPORT int LZODecompress(unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	int status;

	status = lzo_init();
	if (status != LZO_E_OK)
		return status;

#ifdef USE_ASM
	status = lzo1x_decompress_asm(src, src_len, dst, (lzo_uint *)dst_len, NULL);
#else
	status = lzo1x_decompress(src, src_len, dst, (lzo_uint *)dst_len, NULL);
#endif

	return status;
}

LZO_EXPORT int LZOCompress(unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	int status;

	status = lzo_init();
	if (status != LZO_E_OK)
		return status;

	status = lzo1x_999_compress(src, src_len, dst, (lzo_uint *)dst_len, wrkmem);

	return status;
}
