using System;
using static MS2Lib.Helpers;

namespace MS2Lib
{
    public static class Encryption
    {
        public static byte[] Encrypt(byte[] src, byte[] key, byte[] iv)
        {
            IntPtr ptr;
            uint size;

            if (Is64Bit)
            {
                size = NativeMethods.Encrypt64(src, (UIntPtr)src.LongLength, out ptr, key, (UIntPtr)key.Length, iv).ToUInt32();
            }
            else
            {
                size = NativeMethods.Encrypt32(src, (UIntPtr)src.LongLength, out ptr, key, (UIntPtr)key.Length, iv).ToUInt32();
            }

            return ptr.ToArray(size);
        }

        public static byte[] EncryptNoCompress(byte[] src, byte[] key, byte[] iv)
        {
            IntPtr ptr;
            uint size;

            if (Is64Bit)
            {
                size = NativeMethods.EncryptNoCompress64(src, (UIntPtr)src.LongLength, out ptr, key, (UIntPtr)key.Length, iv).ToUInt32();
            }
            else
            {
                size = NativeMethods.EncryptNoCompress32(src, (UIntPtr)src.LongLength, out ptr, key, (UIntPtr)key.Length, iv).ToUInt32();
            }

            return ptr.ToArray(size);
        }

        public static byte[] Compress(byte[] src)
        {
            IntPtr ptr;
            uint size;

            if (Is64Bit)
            {
                size = NativeMethods.Compress64(src, (UIntPtr)src.LongLength, out ptr).ToUInt32();
            }
            else
            {
                size = NativeMethods.Compress32(src, (UIntPtr)src.LongLength, out ptr).ToUInt32();
            }

            return ptr.ToArray(size);
        }
    }
}
