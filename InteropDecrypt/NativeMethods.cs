using System.Runtime.InteropServices;

namespace MS2Lib
{
    internal static class NativeMethods
    {
        #region Decryption
        [DllImport("Decrypt32.dll", EntryPoint = "Decrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Decrypt32([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);

        [DllImport("Decrypt32.dll", EntryPoint = "DecryptNoDecompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DecryptNoDecompress32([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);


        [DllImport("Decrypt64.dll", EntryPoint = "Decrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Decrypt64([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);

        [DllImport("Decrypt64.dll", EntryPoint = "DecryptNoDecompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DecryptNoDecompress64([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);
        #endregion

        #region Encryption
        [DllImport("Decrypt32.dll", EntryPoint = "Encrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Encrypt32([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);

        [DllImport("Decrypt32.dll", EntryPoint = "EncryptNoCompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EncryptNoCompress32([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);


        [DllImport("Decrypt64.dll", EntryPoint = "Encrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Encrypt64([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);

        [DllImport("Decrypt64.dll", EntryPoint = "EncryptNoCompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EncryptNoCompress64([In] byte[] src, [In] uint srcSize, [Out] byte[] dst, uint dstSize, [In] byte[] key, [In] uint keySize, [In] byte[] iv);
        #endregion
    }
}
