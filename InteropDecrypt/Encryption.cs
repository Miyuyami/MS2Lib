using static MS2Lib.Helpers;

namespace MS2Lib
{
    public static class Encryption
    {
        public static byte[] Encrypt(byte[] src, uint dstSize, byte[] key, byte[] iv)
        {
            byte[] dst = new byte[dstSize];

            if (Is64Bit)
            {
                NativeMethods.Encrypt64(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }
            else
            {
                NativeMethods.Encrypt32(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }

            return dst;
        }

        public static byte[] EncryptNoCompress(byte[] src, uint dstSize, byte[] key, byte[] iv)
        {
            byte[] dst = new byte[dstSize];

            if (Is64Bit)
            {
                NativeMethods.EncryptNoCompress64(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }
            else
            {
                NativeMethods.EncryptNoCompress32(src, (uint)src.Length, dst, (uint)dst.Length, key, (uint)key.Length, iv);
            }

            return dst;
        }
    }
}
