using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Logger = MiscUtils.Logging.SimpleLogger;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileHeader : MS2SizeHeader, IEquatable<MS2FileHeader>
    {
        public uint Id { get; }
        public uint Offset { get; }
        public uint TypeId { get; }

        private MS2FileHeader(uint encodedSize, uint compressedSize, uint size, uint id, uint offset, uint typeId) : base(encodedSize, compressedSize, size)
        {
            this.Id = id;
            this.Offset = offset;
            this.TypeId = typeId;
        }

        internal static MS2FileHeader Create(uint id, uint offset, uint typeId, MS2SizeHeader header)
            => new MS2FileHeader(header.EncodedSize, header.CompressedSize, header.Size, id, offset, typeId);

        internal static async Task<MS2FileHeader> Load(MS2CryptoMode cryptoMode, Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                switch (cryptoMode)
                {
                    case MS2CryptoMode.MS2F:
                        return await CreateMS2F(br).ConfigureAwait(false);
                    case MS2CryptoMode.NS2F:
                        return await CreateNS2F(br).ConfigureAwait(false);
                    default:
                    case MS2CryptoMode.OS2F:
                    case MS2CryptoMode.PS2F:
                        throw new NotImplementedException();
                }
            }
        }

        private static Task<MS2FileHeader> CreateMS2F(BinaryReader br)
        {
            return Task.Run(() =>
            {
                uint unk = br.ReadUInt32(); // TODO: unknown/unused?
                uint fileId = br.ReadUInt32();
                uint fileType = br.ReadUInt32();
                uint unk2 = br.ReadUInt32(); // TODO: unknown/unused?
                uint offset = br.ReadUInt32() | br.ReadUInt32();
                uint encodedSize = br.ReadUInt32() | br.ReadUInt32();
                uint compressedSize = br.ReadUInt32() | br.ReadUInt32();
                uint size = br.ReadUInt32() | br.ReadUInt32();

                if (unk != 0)
                {
                    Logger.Debug($"File Header unk is \"{unk}\".");
                }
                if (unk2 != 0)
                {
                    Logger.Debug($"File Header second unk is \"{unk2}\".");
                }

                return new MS2FileHeader(encodedSize, compressedSize, size, fileId, offset, fileType);
            });
        }

        private static Task<MS2FileHeader> CreateNS2F(BinaryReader br)
        {
            return Task.Run(() =>
            {
                uint fileType = br.ReadUInt32();
                uint fileId = br.ReadUInt32();
                uint encodedSize = br.ReadUInt32();
                uint compressedSize = br.ReadUInt32() | br.ReadUInt32();
                uint size = br.ReadUInt32() | br.ReadUInt32();
                uint offset = br.ReadUInt32() | br.ReadUInt32();

                return new MS2FileHeader(encodedSize, compressedSize, size, fileId, offset, fileType);
            });
        }

        internal async Task Save(MS2CryptoMode cryptoMode, Stream stream)
        {
            using (var bw = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                switch (cryptoMode)
                {
                    case MS2CryptoMode.MS2F:
                        await this.SaveMS2F(bw).ConfigureAwait(false);
                        break;
                    case MS2CryptoMode.NS2F:
                        await this.SaveNS2F(bw).ConfigureAwait(false);
                        break;
                    default:
                    case MS2CryptoMode.OS2F:
                    case MS2CryptoMode.PS2F:
                        throw new NotImplementedException();
                }
            }
        }

        private Task SaveMS2F(BinaryWriter bw)
        {
            return Task.Run(() =>
            {
                // unk
                bw.Write(0u);
                // fileId
                bw.Write(this.Id);
                // fileType
                bw.Write(this.TypeId);
                // unk2
                bw.Write(0u);
                // offset
                bw.Write(this.Offset); bw.Write(0u);
                // encodedSize
                bw.Write(this.EncodedSize); bw.Write(0u);
                // compressedSize
                bw.Write(this.CompressedSize); bw.Write(0u);
                // size
                bw.Write(this.Size); bw.Write(0u);
            });
        }

        private Task SaveNS2F(BinaryWriter bw)
        {
            return Task.Run(() =>
            {
                // fileType
                bw.Write(this.TypeId);
                // fileId
                bw.Write(this.Id);
                // encodedSize
                bw.Write(this.EncodedSize);
                // compressedSize
                bw.Write(this.CompressedSize); bw.Write(0u);
                // size
                bw.Write(this.Size); bw.Write(0u);
                // offset
                bw.Write(this.Offset); bw.Write(0u);
            });
        }

        protected override string DebuggerDisplay
            => $"Offset = {this.Offset}, {base.DebuggerDisplay}";

        #region Equality
        public override bool Equals(object obj)
        {
            return this.Equals(obj as MS2FileHeader);
        }

        public bool Equals(MS2FileHeader other)
        {
            return other != null &&
                   base.Equals(other) &&
                   this.Id == other.Id &&
                   this.Offset == other.Offset &&
                   this.TypeId == other.TypeId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = -1295240622;
                hashCode *= -1521134295 + base.GetHashCode();
                hashCode *= -1521134295 + this.Id.GetHashCode();
                hashCode *= -1521134295 + this.Offset.GetHashCode();
                hashCode *= -1521134295 + this.TypeId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(MS2FileHeader header1, MS2FileHeader header2)
        {
            return EqualityComparer<MS2FileHeader>.Default.Equals(header1, header2);
        }

        public static bool operator !=(MS2FileHeader header1, MS2FileHeader header2)
        {
            return !(header1 == header2);
        }
        #endregion
    }
}
