using System;
using static MS2Lib.Helpers;

namespace MS2Lib
{
    public static class Decryption
    {
        public static byte[] Decrypt(byte[] src, uint dstSize, byte[] key, byte[] iv)
        {
            byte[] dst = new byte[dstSize];

            if (Is64Bit)
            {
                NativeMethods.Decrypt64(src, (UIntPtr)src.Length, dst, (UIntPtr)dst.Length, key, (UIntPtr)key.Length, iv);
            }
            else
            {
                NativeMethods.Decrypt32(src, (UIntPtr)src.Length, dst, (UIntPtr)dst.Length, key, (UIntPtr)key.Length, iv);
            }

            return dst;
        }

        public static byte[] DecryptNoDecompress(byte[] src, uint dstSize, byte[] key, byte[] iv)
        {
            byte[] dst = new byte[dstSize];

            if (Is64Bit)
            {
                NativeMethods.DecryptNoDecompress64(src, (UIntPtr)src.Length, dst, (UIntPtr)dst.Length, key, (UIntPtr)key.Length, iv);
            }
            else
            {
                NativeMethods.DecryptNoDecompress32(src, (UIntPtr)src.Length, dst, (UIntPtr)dst.Length, key, (UIntPtr)key.Length, iv);
            }

            return dst;
        }

        public static byte[] Decompress(byte[] src, uint dstSize)
        {
            byte[] dst = new byte[dstSize];

            if (Is64Bit)
            {
                NativeMethods.Decompress64(src, (UIntPtr)src.Length, dst, (UIntPtr)dst.Length);
            }
            else
            {
                NativeMethods.Decompress32(src, (UIntPtr)src.Length, dst, (UIntPtr)dst.Length);
            }

            return dst;
        }
    }
}
