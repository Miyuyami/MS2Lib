# What you need for building
1. You need CryptoPP.
2. Build the libraries as "*.lib" and name as following (NOTE: you can change those in "stdafx.h"):
* "cryptlib" for Release 32 bit
* "cryptlib_x64" for Release 64 bit
* "cryptlibd" for Debug 32 bit
* "cryptlibd_x64" for Debug 64 bit
3. Change "IncludePath", "LibraryPath" and "SourcePath" to the path where you have all those 4 libs.
4. In the same path also add all includes (main folder of CryptoPP with all headers).
5. Now "Decrypt" project should successfuly build.
