using System;

namespace MS2Lib
{
    internal class Helpers
    {
        public static readonly bool Is64Bit = IntPtr.Size == 8;
        //public static readonly bool Is64Bit = ProcessArchitecture == Architecture.Arm64 || ProcessArchitecture == Architecture.X64;

        internal static void DeletePtr(IntPtr ptr)
        {
            if (Is64Bit)
            {
                NativeMethods.Delete64(ptr);
            }
            else
            {
                NativeMethods.Delete32(ptr);
            }
        }
    }
}
