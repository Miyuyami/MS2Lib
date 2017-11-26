// Encrypt.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
using namespace CryptoPP;

extern "C"
{
	__declspec(dllexport) size_t Encrypt(const byte src[], size_t srcSize, byte** dst, const byte key[], size_t keySize, const byte iv[])
	{
		std::string encoded;

		ArraySource as(src, srcSize, true,
			new ZlibCompressor(
				new StreamTransformationFilter(CTR_Mode<AES>::Encryption(key, keySize, iv),
					new Base64Encoder(
						new StringSink(encoded), false
					)
				), ZlibCompressor::MAX_DEFLATE_LEVEL, ZlibCompressor::MAX_LOG2_WINDOW_SIZE, false
			)
		);

		*dst = new byte[encoded.size()];
		memcpy(*dst, encoded.data(), encoded.size());

		return encoded.size();
	}

	__declspec(dllexport) size_t EncryptNoCompress(const byte src[], size_t srcSize, byte** dst, const byte key[], size_t keySize, const byte iv[])
	{
		std::string encoded;

		ArraySource as(src, srcSize, true,
			new StreamTransformationFilter(CTR_Mode<AES>::Encryption(key, keySize, iv),
				new Base64Encoder(
					new StringSink(encoded), false
				)
			)
		);

		*dst = new byte[encoded.size()];
		memcpy(*dst, encoded.data(), encoded.size());

		return encoded.size();
	}

	__declspec(dllexport) size_t Compress(const byte src[], size_t srcSize, byte** dst)
	{
		std::string compressed;

		ArraySource as(src, srcSize, true,
			new ZlibCompressor(
				new StringSink(compressed), ZlibCompressor::MAX_DEFLATE_LEVEL, ZlibCompressor::MAX_LOG2_WINDOW_SIZE, false
			)
		);

		*dst = new byte[compressed.size()];
		memcpy(*dst, compressed.data(), compressed.size());

		return compressed.size();
	}
}