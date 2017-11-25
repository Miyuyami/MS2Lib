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

            if (Is64Bit)
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
