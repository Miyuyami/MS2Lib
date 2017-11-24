namespace MS2Lib
{
    internal static class Cryptography
    {
        private const int Count = 128;
        private const int IvLength = 16;
        private const int KeyLength = 32;

        public static class MS2F
        {
            public const string FileNameIV = @"Cryptography\MS2F\IV";
            public const string FileNameKey = @"Cryptography\MS2F\Key";

            public readonly static MultiArrayFile IV = new MultiArrayFile(FileNameIV, Count, IvLength);
            public readonly static MultiArrayFile Key = new MultiArrayFile(FileNameKey, Count, KeyLength);
        }

        public static class NS2F
        {
            public const string FileNameIV = @"Cryptography\NS2F\IV";
            public const string FileNameKey = @"Cryptography\NS2F\Key";

            public readonly static MultiArrayFile IV = new MultiArrayFile(FileNameIV, Count, IvLength);
            public readonly static MultiArrayFile Key = new MultiArrayFile(FileNameKey, Count, KeyLength);
        }

        public static class OS2F
        {
            public const string FileNameIV = @"Cryptography\OS2F\IV";
            public const string FileNameKey = @"Cryptography\OS2F\Key";

            public readonly static MultiArrayFile IV = new MultiArrayFile(FileNameIV, Count, IvLength);
            public readonly static MultiArrayFile Key = new MultiArrayFile(FileNameKey, Count, KeyLength);
        }

        public static class PS2F
        {
            public const string FileNameIV = @"Cryptography\PS2F\IV";
            public const string FileNameKey = @"Cryptography\PS2F\Key";

            public readonly static MultiArrayFile IV = new MultiArrayFile(FileNameIV, Count, IvLength);
            public readonly static MultiArrayFile Key = new MultiArrayFile(FileNameKey, Count, KeyLength);
        }
    }
}
