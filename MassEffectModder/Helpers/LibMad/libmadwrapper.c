/*
 * LibMad DLL Wrapper
 *
 * Copyright (C) 2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 * Copyright (C) 2000-2004 Underbit Technologies, Inc. 
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
#include <string.h>
#include <stdio.h>

#include "mad.h"

BOOL WINAPI DllMain(HINSTANCE hin, DWORD reason, LPVOID lpvReserved) { return TRUE; }

#ifdef TEST_MODE
#define LIBMAD_EXPORT
#else
#define LIBMAD_EXPORT __declspec(dllexport)
#endif

unsigned short *outputBuffer;
int outputBufferLen;
int outputBufferPos;
int srcBufferLen;

struct mad_buffer {
	unsigned char const *start;
	unsigned long length;
};

enum mad_flow input(void *data, struct mad_stream *stream)
{
	struct mad_buffer *buffer = data;

	if (!buffer->length)
		return MAD_FLOW_STOP;

	mad_stream_buffer(stream, buffer->start, buffer->length);

	buffer->length = 0;

	return MAD_FLOW_CONTINUE;
}

inline signed int scale(mad_fixed_t sample)
{
	/* round */
	sample += (1L << (MAD_F_FRACBITS - 16));

	/* clip */
	if (sample >= MAD_F_ONE)
		sample = MAD_F_ONE - 1;
	else if (sample < -MAD_F_ONE)
		sample = -MAD_F_ONE;

	/* quantize */
	return sample >> (MAD_F_FRACBITS + 1 - 16);
}

enum mad_flow output(void *data, struct mad_header const *header, struct mad_pcm *pcm)
{
	unsigned int nchannels, nsamples;
	mad_fixed_t const *left_ch, *right_ch;
	struct mad_buffer *buffer = data;

	nchannels = pcm->channels;
	nsamples = pcm->length;
	left_ch = pcm->samples[0];
	right_ch = pcm->samples[1];

	if (outputBufferPos == 0)
	{
		outputBufferLen = (srcBufferLen * 8 / header->bitrate + 1) * nchannels * pcm->samplerate * 2;
		outputBufferLen += 44;
		outputBuffer = malloc(outputBufferLen);
		outputBufferPos = 44 / 2;

		int bits = 16;
		int s_size = outputBufferLen - 44;
		int rate = pcm->samplerate;
		unsigned char wav[44];
		wav[0] = 'R';
		wav[1] = 'I';
		wav[2] = 'F';
		wav[3] = 'F';
		wav[4] = (s_size + 36) & 0xff;
		wav[5] = ((s_size + 36) >> 8) & 0xff;
		wav[6] = ((s_size + 36) >> 16) & 0xff;
		wav[7] = ((s_size + 36) >> 24) & 0xff;
		wav[8] = 'W';
		wav[9] = 'A';
		wav[10] = 'V';
		wav[11] = 'E';
		wav[12] = 'f';
		wav[13] = 'm';
		wav[14] = 't';
		wav[15] = 0x20;
		wav[16] = 16;
		wav[17] = 0;
		wav[18] = 0;
		wav[19] = 0;
		wav[20] = 1;
		wav[21] = 0;
		wav[22] = nchannels;
		wav[23] = 0;
		wav[24] = rate & 0xff;
		wav[25] = (rate >> 8) & 0xff;
		wav[26] = (rate >> 16) & 0xff;
		wav[27] = (rate >> 24) & 0xff;
		wav[28] = (rate * nchannels * (bits / 8)) & 0xff;
		wav[29] = ((rate * nchannels * (bits / 8)) >> 8) & 0xff;
		wav[30] = ((rate * nchannels * (bits / 8)) >> 16) & 0xff;
		wav[31] = ((rate * nchannels * (bits / 8)) >> 24) & 0xff;
		wav[32] = (nchannels * (bits / 8)) & 0xff;
		wav[33] = ((nchannels * (bits / 8)) >> 8) & 0xff;
		wav[34] = bits;
		wav[35] = 0;
		wav[36] = 'd';
		wav[37] = 'a';
		wav[38] = 't';
		wav[39] = 'a';
		wav[40] = s_size & 0xff;
		wav[41] = (s_size >> 8) & 0xff;
		wav[42] = (s_size >> 16) & 0xff;
		wav[43] = (s_size >> 24) & 0xff;

		memcpy((char *)outputBuffer, &wav, 44);
	}

	while (nsamples--) {
		signed int sample;

		/* output sample(s) in 16-bit signed little-endian PCM */

		sample = scale(*left_ch++);
		if (outputBufferPos < (outputBufferLen / 2))
			outputBuffer[outputBufferPos++] = sample;
		else
			return MAD_FLOW_STOP;

		if (nchannels == 2) {
			sample = scale(*right_ch++);
			if (outputBufferPos < (outputBufferLen / 2))
				outputBuffer[outputBufferPos++] = sample;
			else
				return MAD_FLOW_STOP;
		}
	}

	return MAD_FLOW_CONTINUE;
}

enum mad_flow error(void *data, struct mad_stream *stream, struct mad_frame *frame)
{
	return MAD_FLOW_CONTINUE;
}

LIBMAD_EXPORT int LibMadDecompress(unsigned char *src, unsigned int src_len, void **dst, unsigned int *dst_len)
{
	int result;
	struct mad_buffer buffer;
	struct mad_decoder decoder;

	buffer.start = src;
	buffer.length = src_len;
	outputBufferPos = 0;
	srcBufferLen = src_len;

	mad_decoder_init(&decoder, &buffer, input, 0, 0, output, error, 0);
	result = mad_decoder_run(&decoder, MAD_DECODER_MODE_SYNC);
	mad_decoder_finish(&decoder);

	*dst = outputBuffer;
	*dst_len = outputBufferPos * 2;

	unsigned char *wav = (unsigned char *)outputBuffer;
	int s_size = *dst_len - 44;
	wav[4] = (s_size + 36) & 0xff;
	wav[5] = ((s_size + 36) >> 8) & 0xff;
	wav[6] = ((s_size + 36) >> 16) & 0xff;
	wav[7] = ((s_size + 36) >> 24) & 0xff;
	wav[40] = s_size & 0xff;
	wav[41] = (s_size >> 8) & 0xff;
	wav[42] = (s_size >> 16) & 0xff;
	wav[43] = (s_size >> 24) & 0xff;

	return result;
}

LIBMAD_EXPORT void LibMadFreeBuffer(void *buffer)
{
	if (buffer)
		free(buffer);
}

#ifdef TEST_MODE
int main(int argc, char** argv)
{
	FILE *file;
	int result;
	unsigned int dst_len = 0;
	unsigned char *dst = 0;

	if (argc == 1)
	{
		printf("Missing file name argument!\n");
		return -1;
	}

	if (argc == 2)
	{
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

		result = LibMadDecompress(buffer, (unsigned int)size, &dst, &dst_len);
		LibMadFreeBuffer(dst);
	}

	return 0;
}

#endif
