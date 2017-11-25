namespace MS2Lib
{
    public class MS2SizeHeader
    {
        public uint EncodedSize { get; }
        public uint CompressedSize { get; }
        public uint Size { get; }

        public MS2SizeHeader(uint encodedSize, uint compressedSize, uint size)
        {
            this.EncodedSize = encodedSize;
            this.CompressedSize = compressedSize;
            this.Size = size;
        }
    }
}
