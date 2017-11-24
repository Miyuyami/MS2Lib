using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace MS2Lib
{
    public static class Decryption
    {
        public static byte[] Decrypt(byte[] src, uint dstSize, byte[] key, byte[] iv)
        {
            byte[] dst = new byte[dstSize];

            bool is64Bit = ProcessArchitecture == Architecture.Arm64 || ProcessArchitecture == Architecture.X64;

            if (is64Bit)
            {
                NativeMethods.Decrypt64(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }
            else
            {
                NativeMethods.Decrypt32(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }

            return dst;
        }

        public static byte[] DecryptNoDecompress(byte[] src, uint dstSize, byte[] key, byte[] iv)
        {
            byte[] dst = new byte[dstSize];

            bool is64Bit = ProcessArchitecture == Architecture.Arm64 || ProcessArchitecture == Architecture.X64;

            if (is64Bit)
            {
                NativeMethods.DecryptNoDecompress64(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }
            else
            {
                NativeMethods.DecryptNoDecompress32(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }

            return dst;
        }
    }
}
