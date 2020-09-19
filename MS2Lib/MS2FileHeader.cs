using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileHeader : IMS2FileHeader, IEquatable<MS2FileHeader>
    {
        public uint Id { get; }
        public long Offset { get; }
        public CompressionType CompressionType { get; }
        public IMS2SizeHeader Size { get; }

        public MS2FileHeader(long size, uint id, long offset, CompressionType compressionType = CompressionType.None) :
            this(new MS2SizeHeader(size), id, offset, compressionType)
        {

        }

        public MS2FileHeader(long encodedSize, long compressedSize, long size, uint id, long offset, CompressionType compressionType = CompressionType.None) :
            this(new MS2SizeHeader(encodedSize, compressedSize, size), id, offset, compressionType)
        {

        }

        public MS2FileHeader(IMS2SizeHeader size, uint id, long offset, CompressionType compressionType = CompressionType.None)
        {
            this.Size = size ?? throw new ArgumentNullException(nameof(size));
            this.Id = id;
            this.Offset = offset;
            this.CompressionType = compressionType;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        protected virtual string DebuggerDisplay
            => $"Offset = {this.Offset}, {this.Size.EncodedSize}->{this.Size.CompressedSize}->{this.Size.Size}";

        #region Equality
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MS2FileHeader);
        }

        public virtual bool Equals(MS2FileHeader other)
        {
            return other != null &&
                   this.Id == other.Id &&
                   this.Offset == other.Offset &&
                   this.CompressionType == other.CompressionType &&
                   EqualityComparer<IMS2SizeHeader>.Default.Equals(this.Size, other.Size);
        }

        bool IEquatable<IMS2FileHeader>.Equals(IMS2FileHeader other)
        {
            return this.Equals(other as MS2FileHeader);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Id, this.Offset, this.CompressionType, this.Size);
        }

        public static bool operator ==(MS2FileHeader left, MS2FileHeader right)
        {
            return EqualityComparer<MS2FileHeader>.Default.Equals(left, right);
        }

        public static bool operator !=(MS2FileHeader left, MS2FileHeader right)
        {
            return !(left == right);
        }
        #endregion
    }
}
