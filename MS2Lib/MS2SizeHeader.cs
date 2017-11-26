using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2SizeHeader : IEquatable<MS2SizeHeader>
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

        protected virtual string DebuggerDisplay
            => $"{this.EncodedSize}->{this.CompressedSize}->{this.Size}";

        #region Equality
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MS2SizeHeader);
        }

        public bool Equals(MS2SizeHeader other)
        {
            return other != null &&
                   this.EncodedSize == other.EncodedSize &&
                   this.CompressedSize == other.CompressedSize &&
                   this.Size == other.Size;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = -861978407;
                hashCode *= -1521134295 + this.EncodedSize.GetHashCode();
                hashCode *= -1521134295 + this.CompressedSize.GetHashCode();
                hashCode *= -1521134295 + this.Size.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(MS2SizeHeader header1, MS2SizeHeader header2)
        {
            return EqualityComparer<MS2SizeHeader>.Default.Equals(header1, header2);
        }

        public static bool operator !=(MS2SizeHeader header1, MS2SizeHeader header2)
        {
            return !(header1 == header2);
        }
        #endregion
    }
}
