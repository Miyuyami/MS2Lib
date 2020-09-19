﻿namespace MS2Lib
{
    public static class Cryptography
    {
        private const int Count = 128;
        private const int IvLength = 16;
        private const int KeyLength = 32;

        public static class MS2F
        {
            private const string FileNameIV = "MS2F_IV";
            private const string FileNameKey = "MS2F_Key";

            public readonly static IMultiArray IV = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameIV, Count, IvLength);
            public readonly static IMultiArray Key = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameKey, Count, KeyLength);
        }

        public static class NS2F
        {
            private const string FileNameIV = "NS2F_IV";
            private const string FileNameKey = "NS2F_Key";

            public readonly static IMultiArray IV = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameIV, Count, IvLength);
            public readonly static IMultiArray Key = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameKey, Count, KeyLength);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static class OS2F
        {
            private const string FileNameIV = "OS2F_IV";
            private const string FileNameKey = "OS2F_Key";

            public readonly static IMultiArray IV = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameIV, Count, IvLength);
            public readonly static IMultiArray Key = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameKey, Count, KeyLength);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static class PS2F
        {
            private const string FileNameIV = "PS2F_IV";
            private const string FileNameKey = "PS2F_Key";

            public readonly static IMultiArray IV = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameIV, Count, IvLength);
            public readonly static IMultiArray Key = new MultiArrayResource(Properties.Resources.ResourceManager, FileNameKey, Count, KeyLength);
        }
    }
}
