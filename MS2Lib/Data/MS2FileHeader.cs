using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Logger = MiscUtils.Logging.SimpleLogger;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2FileHeader : MS2SizeHeader
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

        public static async Task<MS2FileHeader> Create(MS2CryptoMode decryptionMode, Stream stream)
        {
            using (var br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                switch (decryptionMode)
                {
                    case MS2CryptoMode.MS2F:
                        return await CreateFileHeaderMS2F(decryptionMode, br).ConfigureAwait(false);
                    case MS2CryptoMode.NS2F:
                        return await CreateFileHeaderNS2F(decryptionMode, br).ConfigureAwait(false);
                    default:
                    case MS2CryptoMode.OS2F:
                    case MS2CryptoMode.PS2F:
                        throw new NotImplementedException();
                }
            }
        }

        private static Task<MS2FileHeader> CreateFileHeaderMS2F(MS2CryptoMode decryptionMode, BinaryReader br)
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
                    Logger.Debug($"File Header unk2 is \"{unk2}\".");
                }

                return new MS2FileHeader(encodedSize, compressedSize, size, fileId, offset, fileType);
            });
        }

        private static Task<MS2FileHeader> CreateFileHeaderNS2F(MS2CryptoMode decryptionMode, BinaryReader br)
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

        private string DebuggerDisplay
            => $"Offset = {this.Offset}, {this.EncodedSize}->{this.CompressedSize}->{this.Size}";
    }
}
