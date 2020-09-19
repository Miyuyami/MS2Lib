using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using static MS2Lib.Tests.CryptoTestHelper;

namespace MS2Lib.Tests
{
    internal sealed class FakeMS2ArchiveHeaderCrypto : IMS2ArchiveHeaderCrypto
    {
        public Task<(IMS2SizeHeader fileInfoSize, IMS2SizeHeader fileDataSize, long fileCount)> ReadAsync(Stream stream)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);

            Mock<IMS2SizeHeader> infoMock = CreateSizeMock(br.ReadInt64(), br.ReadInt64(), br.ReadInt64());
            Mock<IMS2SizeHeader> dataMock = CreateSizeMock(br.ReadInt64(), br.ReadInt64(), br.ReadInt64());
            long fileCount = br.ReadInt64();

            return Task.FromResult((infoMock.Object, dataMock.Object, fileCount));
        }

        public Task WriteAsync(Stream stream, IMS2SizeHeader fileInfoSize, IMS2SizeHeader fileDataSize, long fileCount)
        {
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);

            bw.Write((long)fileInfoSize.EncodedSize);
            bw.Write((long)fileInfoSize.CompressedSize);
            bw.Write((long)fileInfoSize.Size);
            bw.Write((long)fileDataSize.EncodedSize);
            bw.Write((long)fileDataSize.CompressedSize);
            bw.Write((long)fileDataSize.Size);
            bw.Write((long)fileCount);

            return Task.CompletedTask;
        }
    }
}
