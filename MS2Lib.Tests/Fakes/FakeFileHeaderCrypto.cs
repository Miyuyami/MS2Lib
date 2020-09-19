using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using static MS2Lib.Tests.CryptoTestHelper;

namespace MS2Lib.Tests
{
    internal sealed class FakeFileHeaderCrypto : IMS2FileHeaderCrypto
    {
        public Task<IMS2FileHeader> ReadAsync(Stream stream)
        {
            using var br = new BinaryReader(stream, Encoding.ASCII, true);

            Mock<IMS2SizeHeader> sizeMock = CreateSizeMock(br.ReadInt64(), br.ReadInt64(), br.ReadInt64());
            Mock<IMS2FileHeader> result = CreateFileHeaderMock(sizeMock, (uint)br.ReadInt64(), br.ReadInt64(), (CompressionType)br.ReadInt64());

            return Task.FromResult(result.Object);
        }

        public Task WriteAsync(Stream stream, IMS2FileHeader fileHeader)
        {
            using var bw = new BinaryWriter(stream, Encoding.ASCII, true);

            bw.Write((long)fileHeader.Size.EncodedSize);
            bw.Write((long)fileHeader.Size.CompressedSize);
            bw.Write((long)fileHeader.Size.Size);
            bw.Write((long)fileHeader.Id);
            bw.Write((long)fileHeader.Offset);
            bw.Write((long)fileHeader.CompressionType);

            return Task.CompletedTask;
        }
    }
}
