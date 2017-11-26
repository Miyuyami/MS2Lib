using System;
using System.Runtime.InteropServices;
using static MS2Lib.Helpers;

namespace MS2Lib
{
    internal static class Extensions
    {
        public static byte[] ToArray(this IntPtr ptr, uint size)
        {
            byte[] dst = new byte[size];
            Marshal.Copy(ptr, dst, 0, (int)size);
            DeletePtr(ptr);

            return dst;
        }
    }
}
