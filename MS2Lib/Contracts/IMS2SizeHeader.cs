using System;

namespace MS2Lib
{
    public interface IMS2SizeHeader : IEquatable<IMS2SizeHeader>
    {
        long CompressedSize { get; }
        long EncodedSize { get; }
        long Size { get; }
    }
}