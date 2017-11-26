using System;
using System.Runtime.InteropServices;

namespace MS2Lib
{
    internal static class NativeMethods
    {
        [DllImport("Decrypt32.dll", EntryPoint = "Delete", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Delete32(IntPtr ptr);

        [DllImport("Decrypt64.dll", EntryPoint = "Delete", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Delete64(IntPtr ptr);

        #region Decryption
        [DllImport("Decrypt32.dll", EntryPoint = "Decrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Decrypt32(byte[] src, UIntPtr srcSize, [Out] byte[] dst, UIntPtr dstSize, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt32.dll", EntryPoint = "DecryptNoDecompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DecryptNoDecompress32(byte[] src, UIntPtr srcSize, [Out] byte[] dst, UIntPtr dstSize, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt32.dll", EntryPoint = "Decompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Decompress32(byte[] src, UIntPtr srcSize, [Out] byte[] dst, UIntPtr dstSize);


        [DllImport("Decrypt64.dll", EntryPoint = "Decrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Decrypt64(byte[] src, UIntPtr srcSize, [Out] byte[] dst, UIntPtr dstSize, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt64.dll", EntryPoint = "DecryptNoDecompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DecryptNoDecompress64(byte[] src, UIntPtr srcSize, [Out] byte[] dst, UIntPtr dstSize, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt64.dll", EntryPoint = "Decompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Decompress64(byte[] src, UIntPtr srcSize, [Out] byte[] dst, UIntPtr dstSize);
        #endregion

        #region Encryption
        [DllImport("Decrypt32.dll", EntryPoint = "Encrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr Encrypt32(byte[] src, UIntPtr srcSize, out IntPtr dst, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt32.dll", EntryPoint = "EncryptNoCompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr EncryptNoCompress32(byte[] src, UIntPtr srcSize, out IntPtr dst, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt32.dll", EntryPoint = "Compress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr Compress32(byte[] src, UIntPtr srcSize, out IntPtr dst);


        [DllImport("Decrypt64.dll", EntryPoint = "Encrypt", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr Encrypt64(byte[] src, UIntPtr srcSize, out IntPtr dst, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt64.dll", EntryPoint = "EncryptNoCompress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr EncryptNoCompress64(byte[] src, UIntPtr srcSize, out IntPtr dst, byte[] key, UIntPtr keySize, byte[] iv);

        [DllImport("Decrypt64.dll", EntryPoint = "Compress", CallingConvention = CallingConvention.Cdecl)]
        internal static extern UIntPtr Compress64(byte[] src, UIntPtr srcSize, out IntPtr dst);
        #endregion
    }
}
