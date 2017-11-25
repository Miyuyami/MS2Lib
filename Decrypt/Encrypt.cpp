// Encrypt.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
using namespace CryptoPP;

extern "C"
{
	__declspec(dllexport) void Encrypt(const byte src[], unsigned int srcSize, byte dst[], unsigned int dstSize, const byte key[], unsigned int keySize, const byte iv[])
	{
		ArraySource as(src, srcSize, true,
			new ZlibCompressor(
				new StreamTransformationFilter(CTR_Mode<AES>::Encryption(key, keySize, iv),
					new Base64Encoder(
						new ArraySink(dst, dstSize)
					)
				)
			)
		);
	}

	__declspec(dllexport) void EncryptNoCompress(const byte src[], unsigned int srcSize, byte dst[], unsigned int dstSize, const byte key[], unsigned int keySize, const byte iv[])
	{
		ArraySource as(src, srcSize, true,
			new StreamTransformationFilter(CTR_Mode<AES>::Encryption(key, keySize, iv),
				new Base64Encoder(
					new ArraySink(dst, dstSize)
				)
			)
		);
	}
}