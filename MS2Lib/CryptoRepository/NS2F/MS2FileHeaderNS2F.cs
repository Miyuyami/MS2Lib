using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MS2Lib.NS2F
{
    public sealed class MS2FileHeaderNS2F : IMS2FileHeaderCrypto
    {
        public Task<IMS2FileHeader> ReadAsync(Stream stream)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);

            var compressionType = (CompressionType)br.ReadUInt32();
            uint id = br.ReadUInt32();
            uint encodedSize = br.ReadUInt32();
            long compressedSize = br.ReadInt64();
            long size = br.ReadInt64();
            long offset = br.ReadInt64();

            IMS2FileHeader fileHeader = new MS2FileHeader(encodedSize, compressedSize, size, id, offset, compressionType);

            return Task.FromResult(fileHeader);
        }

        public Task WriteAsync(Stream stream, IMS2FileHeader fileHeader)
        {
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);

            bw.Write((uint)fileHeader.CompressionType);
            bw.Write(fileHeader.Id);
            bw.Write((uint)fileHeader.Size.EncodedSize);
            bw.Write(fileHeader.Size.CompressedSize);
            bw.Write(fileHeader.Size.Size);
            bw.Write(fileHeader.Offset);

            return Task.CompletedTask;
        }
    }
}
