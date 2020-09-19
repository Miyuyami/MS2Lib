using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2SizeHeader : IMS2SizeHeader, IEquatable<MS2SizeHeader>
    {
        public long EncodedSize { get; }
        public long CompressedSize { get; }
        public long Size { get; }

        public MS2SizeHeader(long size) :
            this(size, size)
        {

        }

        public MS2SizeHeader(long compressedSize, long size) :
            this(compressedSize, compressedSize, size)
        {

        }

        public MS2SizeHeader(long encodedSize, long compressedSize, long size)
        {
            this.EncodedSize = encodedSize;
            this.CompressedSize = compressedSize;
            this.Size = size;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        protected virtual string DebuggerDisplay
            => $"{this.EncodedSize}->{this.CompressedSize}->{this.Size}";

        #region Equality
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MS2SizeHeader);
        }

        public virtual bool Equals(MS2SizeHeader other)
        {
            return other != null &&
                   this.EncodedSize == other.EncodedSize &&
                   this.CompressedSize == other.CompressedSize &&
                   this.Size == other.Size;
        }

        bool IEquatable<IMS2SizeHeader>.Equals(IMS2SizeHeader other)
        {
            return this.Equals(other as MS2SizeHeader);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.EncodedSize, this.CompressedSize, this.Size);
        }

        public static bool operator ==(MS2SizeHeader left, MS2SizeHeader right)
        {
            return EqualityComparer<MS2SizeHeader>.Default.Equals(left, right);
        }

        public static bool operator !=(MS2SizeHeader left, MS2SizeHeader right)
        {
            return !(left == right);
        }
        #endregion
    }
}
