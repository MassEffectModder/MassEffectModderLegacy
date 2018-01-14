/*
 * xdelta 3 DLL Wrapper
 *
 * Copyright (C) 2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

#define _WIN32_WINNT 0x0501
#include <windows.h>
#include <string.h>
#include <stdio.h>

#include "xdelta3.h"

BOOL WINAPI DllMain(HINSTANCE hin, DWORD reason, LPVOID lpvReserved) { return TRUE; }

#ifdef TEST_MODE
#define XDELTA3_EXPORT
#else
#define XDELTA3_EXPORT __declspec(dllexport)
#endif

XDELTA3_EXPORT int XDelta3Compress(unsigned char *src1, unsigned char *src2, unsigned int src_len,
	unsigned char *delta, unsigned int *delta_len)
{
	int result;
	usize_t len = 0;

	result = xd3_encode_memory((const uint8_t *)src2, (usize_t)src_len, (const uint8_t *)src1, (usize_t)src_len,
		(uint8_t *)delta, &len, (usize_t)src_len, XD3_SEC_DJW | XD3_ADLER32);

	if (result == 0)
		*delta_len = (unsigned int)len;

	return result;
}

XDELTA3_EXPORT int XDelta3Decompress(unsigned char *src, unsigned int src_len,
	unsigned char *delta, unsigned int delta_len, unsigned char *dst, unsigned int *dst_len)
{
	int result;
	usize_t len = 0;

	result = xd3_decode_memory((const uint8_t *)delta, (usize_t)delta_len, (const uint8_t *)src, (usize_t)src_len,
		(uint8_t *)dst, &len, (usize_t)src_len, 0);

	if (result == 0)
		*dst_len = (unsigned int)len;

	return result;
}

#ifdef TEST_MODE
int main(int argc, char** argv)
{
	FILE *file;
	int result;

	if (argc != 4)
	{
		printf("Wrong arguments!\n");
		return -1;
	}

	fopen_s(&file, argv[1], "rb");
	if (file == NULL)
	{
		printf("Can not open file: %s !\n", argv[1]);
		return -1;
	}
	fseek(file, 0, SEEK_END);
	size_t size = ftell(file);
	fseek(file, 0, SEEK_SET);
	unsigned char *src1 = malloc(size);
	fread(src1, 1, size, file);
	fclose(file);

	fopen_s(&file, argv[2], "rb");
	if (file == NULL)
	{
		printf("Can not open file: %s !\n", argv[2]);
		return -1;
	}
	unsigned char *src2 = malloc(size);
	fread(src2, 1, size, file);
	fclose(file);

	unsigned char *delta = malloc(size);
	unsigned int delta_len = 0;

	result = XDelta3Compress(src1, src2, (unsigned int)size, delta, &delta_len);

	fopen_s(&file, argv[3], "wb");
	if (file == NULL)
	{
		printf("Can not open file: %s !\n", argv[3]);
		return -1;
	}
	fwrite(delta, 1, delta_len, file);
	fclose(file);

	unsigned int output_len = 0;
	result = XDelta3Decompress(src1, (unsigned int)size, delta, delta_len, src2, &output_len);

	fopen_s(&file, "output", "wb");
	if (file == NULL)
	{
		printf("Can not open file: output !\n");
		return -1;
	}
	fwrite(src2, 1, output_len, file);
	fclose(file);

	return 0;
}

#endif
