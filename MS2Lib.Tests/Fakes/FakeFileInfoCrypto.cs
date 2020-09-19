using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using static MS2Lib.Tests.CryptoTestHelper;

namespace MS2Lib.Tests
{
    internal sealed class FakeFileInfoCrypto : IMS2FileInfoCrypto
    {
        public Task<IMS2FileInfo> ReadAsync(TextReader textReader)
        {
            var info = textReader.ReadLine().Split(',');
            Mock<IMS2FileInfo> result = CreateFileInfoMock(info[0], info[1]);

            return Task.FromResult(result.Object);
        }

        public Task WriteAsync(TextWriter textWriter, IMS2FileInfo fileInfo)
        {
            textWriter.WriteLine(String.Join(',', fileInfo.Id, fileInfo.Path));

            return Task.CompletedTask;
        }
    }
}
