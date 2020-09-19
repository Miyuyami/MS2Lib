using System;

namespace MS2Lib
{
    public interface IMS2FileHeader : IEquatable<IMS2FileHeader>
    {
        CompressionType CompressionType { get; }
        uint Id { get; }
        long Offset { get; }
        IMS2SizeHeader Size { get; }
    }
}