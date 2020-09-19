using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MS2Lib.MS2F
{
    public sealed class MS2FileHeaderMS2F : IMS2FileHeaderCrypto
    {
        public Task<IMS2FileHeader> ReadAsync(Stream stream)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);

            uint unk = br.ReadUInt32(); // TODO: unknown/unused?
            uint id = br.ReadUInt32();
            var compressionType = (CompressionType)br.ReadInt64();
            long offset = br.ReadInt64();
            long encodedSize = br.ReadInt64();
            long compressedSize = br.ReadInt64();
            long size = br.ReadInt64();

            Debug.Assert(unk == 0, "unk is not 0. unk=" + unk);

            IMS2FileHeader fileHeader = new MS2FileHeader(encodedSize, compressedSize, size, id, offset, compressionType);

            return Task.FromResult(fileHeader);
        }

        public Task WriteAsync(Stream stream, IMS2FileHeader fileHeader)
        {
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);

            bw.Write(0u);
            bw.Write(fileHeader.Id);
            bw.Write((long)fileHeader.CompressionType);
            bw.Write(fileHeader.Offset);
            bw.Write(fileHeader.Size.EncodedSize);
            bw.Write(fileHeader.Size.CompressedSize);
            bw.Write(fileHeader.Size.Size);

            return Task.CompletedTask;
        }
    }
}
