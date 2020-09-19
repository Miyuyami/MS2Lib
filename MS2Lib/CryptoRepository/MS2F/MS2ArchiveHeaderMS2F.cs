using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MS2Lib.MS2F
{
    public sealed class MS2ArchiveHeaderMS2F : IMS2ArchiveHeaderCrypto
    {
        public Task<(IMS2SizeHeader fileInfoSize, IMS2SizeHeader fileDataSize, long fileCount)> ReadAsync(Stream stream)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);

            uint unk = br.ReadUInt32(); // TODO: unknown/unused?
            long dataCompressedSize = br.ReadInt64();
            long dataEncodedSize = br.ReadInt64();
            long size = br.ReadInt64();
            long compressedSize = br.ReadInt64();
            long encodedSize = br.ReadInt64();
            long fileCount = br.ReadInt64();
            long dataSize = br.ReadInt64();

            Debug.Assert(unk == 0, "unk is not 0. unk=" + unk);

            IMS2SizeHeader fileInfoSize = new MS2SizeHeader(encodedSize, compressedSize, size);
            IMS2SizeHeader fileDataSize = new MS2SizeHeader(dataEncodedSize, dataCompressedSize, dataSize);

            return Task.FromResult((fileInfoSize, fileDataSize, fileCount));
        }

        public Task WriteAsync(Stream stream, IMS2SizeHeader fileInfoSize, IMS2SizeHeader fileDataSize, long fileCount)
        {
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);

            bw.Write(0u);
            bw.Write(fileDataSize.CompressedSize);
            bw.Write(fileDataSize.EncodedSize);
            bw.Write(fileInfoSize.Size);
            bw.Write(fileInfoSize.CompressedSize);
            bw.Write(fileInfoSize.EncodedSize);
            bw.Write(fileCount);
            bw.Write(fileDataSize.Size);

            return Task.CompletedTask;
        }
    }
}
