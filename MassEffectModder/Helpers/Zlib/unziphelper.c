/* unziphelper.c

        Copyright (C) 2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>

        ---------------------------------------------------------------------------------

        Condition of use and distribution are the same than zlib :

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgement in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  ---------------------------------------------------------------------------------
*/

#include <string.h>

#include "iomemapi.h"
#include "unzip.h"

typedef struct
{
	zlib_filefunc64_def api;
	voidpf handle;
	unzFile file;
	unz_global_info globalInfo;
	unz_file_info curFileInfo;
	int tpfMode;
} UnzipHandle;

static const char tpfPassword[] =
{ 
	0x73, 0x2A, 0x63, 0x7D, 0x5F, 0x0A, 0xA6, 0xBD,
	0x7D, 0x65, 0x7E, 0x67, 0x61, 0x2A, 0x7F, 0x7F,
	0x74, 0x61, 0x67, 0x5B, 0x60, 0x70, 0x45, 0x74,
	0x5C, 0x22, 0x74, 0x5D, 0x6E, 0x6A, 0x73, 0x41,
	0x77, 0x6E, 0x46, 0x47, 0x77, 0x49, 0x0C, 0x4B,
	0x46, 0x6F, '\0'
};

static const unsigned char tpfXorKey[2] = { 0xA4, 0x3F };

#ifdef TEST_CODE
#define ZLIB_EXPORT
#else
#define ZLIB_EXPORT __declspec(dllexport)
#endif

ZLIB_EXPORT void *ZipOpen(unsigned char *src, unsigned long srcLen, unsigned long *numEntries, int tpf)
{
	UnzipHandle *unzipHandle;
	int result;

	if (tpf)
	{
		for (unsigned long i = 0; i < srcLen; i++)
			src[i] = (unsigned char)(tpfXorKey[i % 2] ^ src[i]);
	}

	unzipHandle = malloc(sizeof(UnzipHandle));
	if (unzipHandle == Z_NULL || numEntries == Z_NULL)
		return Z_NULL;

	unzipHandle->tpfMode = tpf;

	unzipHandle->handle = create_iomem_from_buffer(&unzipHandle->api, src, srcLen);
	if (unzipHandle->handle == Z_NULL)
	{
		free(unzipHandle);
		return Z_NULL;
	}
	unzipHandle->file = unzOpenIoMem(unzipHandle->handle, &unzipHandle->api, 1);
	if (unzipHandle->file == Z_NULL)
	{
		free(unzipHandle);
		return Z_NULL;
	}
	result = unzGetGlobalInfo(unzipHandle->file, &unzipHandle->globalInfo);
	if (result != UNZ_OK)
	{
		unzClose(unzipHandle->file);
		free(unzipHandle);
		return Z_NULL;
	}

	*numEntries = unzipHandle->globalInfo.number_entry;

	return (void *)unzipHandle;
}

ZLIB_EXPORT int ZipGetCurrentFileInfo(void *handle, char *fileName, unsigned long sizeOfFileName, unsigned long *dstLen)
{
	UnzipHandle *unzipHandle = handle;
	int result;
	char f[256];

	if (unzipHandle == Z_NULL || sizeOfFileName == 0 || dstLen == Z_NULL)
		return -1;

	result = unzGetCurrentFileInfo(unzipHandle->file, &unzipHandle->curFileInfo, f, sizeOfFileName, NULL, 0, NULL, 0);
	if (result != UNZ_OK)
		return result;

	strcpy_s(fileName, 256, f);
	*dstLen = unzipHandle->curFileInfo.uncompressed_size;

	return 0;
}

ZLIB_EXPORT int ZipGoToFirstFile(void *handle)
{
	UnzipHandle *unzipHandle = handle;
	int result;

	if (unzipHandle == Z_NULL)
		return -1;

	result = unzGoToFirstFile(unzipHandle->file);
	if (result != UNZ_OK)
		return result;

	return 0;
}

ZLIB_EXPORT int ZipGoToNextFile(void *handle)
{
	UnzipHandle *unzipHandle = handle;
	int result;

	if (unzipHandle == Z_NULL)
		return -1;

	result = unzGoToNextFile(unzipHandle->file);
	if (result != UNZ_OK)
		return result;

	return 0;
}

ZLIB_EXPORT int ZipLocateFile(void *handle, const char *filename)
{
	UnzipHandle *unzipHandle = handle;
	int result;

	if (unzipHandle == Z_NULL || filename == Z_NULL)
		return -1;

	result = unzLocateFile(unzipHandle->file, filename, 2);
	if (result != UNZ_OK)
		return result;

	return 0;
}

ZLIB_EXPORT int ZipReadCurrentFile(void *handle, unsigned char *dst, unsigned long dst_len, unsigned char *pass)
{
	UnzipHandle *unzipHandle = handle;
	int result;

	if (unzipHandle == Z_NULL || dst == Z_NULL)
		return -1;

	if ((unzipHandle->curFileInfo.flag & 1) != 0)
	{
#ifdef TEST_CODE
		result = unzOpenCurrentFilePassword(unzipHandle->file, tpfPassword);
#else
		result = unzOpenCurrentFilePassword(unzipHandle->file, unzipHandle->tpfMode == 1 ? tpfPassword : pass == Z_NULL ? "" : (const char *)pass);
#endif
	}
	else
	{
		result = unzOpenCurrentFile(unzipHandle->file);
	}
	if (result != UNZ_OK)
		return result;

	result = unzReadCurrentFile(unzipHandle->file, dst, dst_len);
	if (result < 0)
		return result;

	result = unzCloseCurrentFile(unzipHandle->file);
	if (result != UNZ_OK)
		return result;

	return 0;
}

ZLIB_EXPORT void ZipClose(void *handle)
{
	UnzipHandle *unzipHandle = handle;

	if (unzipHandle == Z_NULL)
		return;

	unzClose(unzipHandle->file);
}

#ifdef TEST_CODE

int main(int argc, char** argv)
{
	FILE *file;
	int result;

	if (argc == 1)
	{
		printf("Missing file name argument!\n");
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
	unsigned char *buffer = malloc(size);
	if (buffer == NULL)
	{
		fclose(file);
		return -1;
	}
	if (fread(buffer, 1, size, file) != size)
	{
		free(buffer);
		fclose(file);
		return -1;
	}
	fclose(file);

	char fileName[256];
	unsigned long fileSize = 0, numEntries = 0;

	void *handle = ZipOpen(buffer, size, &numEntries, 1);
	result = ZipLocateFile(handle, "texmod.def");
	if (result < 0)
	{
		ZipClose(handle);
		return -1;
	}
	result = ZipGetCurrentFileInfo(handle, fileName, sizeof(fileName), &fileSize);
	if (result < 0)
	{
		ZipClose(handle);
		return -1;
	}
	unsigned char* fileBuffer = malloc(fileSize);
	result = ZipReadCurrentFile(handle, fileBuffer, fileSize, "");
	if (result < 0)
	{
		free(fileBuffer);
		ZipClose(handle);
		return -1;
	}
	free(fileBuffer);

	result = ZipGoToFirstFile(handle);
	if (result < 0)
	{
		ZipClose(handle);
		return -1;
	}

	for (unsigned long i = 0; i < numEntries; i++)
	{
		result = ZipGetCurrentFileInfo(handle, fileName, sizeof(fileName), &fileSize);
		if (result < 0)
		{
			ZipClose(handle);
			return -1;
		}

		fileBuffer = malloc(fileSize);
		result = ZipReadCurrentFile(handle, fileBuffer, fileSize, "");
		if (result < 0)
		{
			free(fileBuffer);
			ZipClose(handle);
			return -1;
		}
		free(fileBuffer);


		result = ZipGoToNextFile(handle);
		if (result < 0)
		{
			ZipClose(handle);
			return -1;
		}
	}

	ZipClose(handle);

	return 0;
}

#endif
