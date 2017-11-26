// Encrypt.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"

extern "C"
{
	__declspec(dllexport) void Delete(byte** dst)
	{
		delete[] dst;
	}
}