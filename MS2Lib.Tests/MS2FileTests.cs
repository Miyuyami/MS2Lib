using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static MS2Lib.Tests.CryptoTestHelper;
using static MS2Lib.Tests.TestHelper;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2FileTests
    {
        #region static helpers
        private static Mock<IMS2Archive> CreateArchiveMock()
        {
            var mock = new Mock<IMS2Archive>(MockBehavior.Strict);

            return mock;
        }

        private static Mock<IMS2Archive> CreateArchiveMock(IMS2ArchiveCryptoRepository repository)
        {
            var mock = new Mock<IMS2Archive>(MockBehavior.Strict);

            mock.SetupGet(a => a.CryptoRepository).Returns(repository);

            return mock;
        }
        #endregion

        #region Dispose
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task Dispose_GetStreamAsync_ThrowsObjectDisposedException()
        {
            var expectedBytes = new byte[] { 0x30, 0x60 };
            var archiveMock = new Mock<IMS2Archive>(MockBehavior.Strict);
            var stream = new MemoryStream(expectedBytes);
            var info = new MS2FileInfo("1", "testfile");
            var header = new MS2FileHeader(expectedBytes.Length, 1, 0);
            var file = new MS2File(archiveMock.Object, stream, info, header, false);

            file.Dispose();

            await file.GetStreamAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task Dispose_GetStreamForArchivingAsync_ThrowsObjectDisposedException()
        {
            var expectedBytes = new byte[] { 0x30, 0x60 };
            var archiveMock = new Mock<IMS2Archive>(MockBehavior.Strict);
            var stream = new MemoryStream(expectedBytes);
            var info = new MS2FileInfo("1", "testfile");
            var header = new MS2FileHeader(expectedBytes.Length, 1, 0);
            var file = new MS2File(archiveMock.Object, stream, info, header, false);

            file.Dispose();

            await file.GetStreamForArchivingAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Dispose_EncapsulatedStream_ThrowsObjectDisposedException()
        {
            var expectedBytes = new byte[] { 0x30, 0x60 };
            var archiveMock = new Mock<IMS2Archive>(MockBehavior.Strict);
            var stream = new MemoryStream(expectedBytes);
            var info = new MS2FileInfo("1", "testfile");
            var header = new MS2FileHeader(expectedBytes.Length, 1, 0);
            var file = new MS2File(archiveMock.Object, stream, info, header, false);

            file.Dispose();

            stream.ReadByte();
        }

        [TestMethod]
        public void Dispose_EncapsulatedMemoryMappedFile_DoesNotThrowObjectDisposedException()
        {
            var expectedBytes = new byte[] { 0x30, 0x60 };
            var archiveMock = new Mock<IMS2Archive>(MockBehavior.Strict);
            var mappedFile = MemoryMappedFile.CreateNew(nameof(Dispose_EncapsulatedMemoryMappedFile_DoesNotThrowObjectDisposedException), expectedBytes.Length, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
            var info = new MS2FileInfo("1", "testfile");
            var header = new MS2FileHeader(expectedBytes.Length, 1, 0);
            var file = new MS2File(archiveMock.Object, mappedFile, info, header, false);

            file.Dispose();

            mappedFile.CreateViewAccessor();
        }
        #endregion

        #region GetStreamAsync
        [TestMethod]
        public async Task GetStreamAsync_StreamEncryptedFalseAndCompressionNone_EqualsInput()
        {
            string input = "testdatainput";
            string expected = input;
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock();
            var infoMock = CreateFileInfoMock("1", "testfile");
            var sizeMock = CreateSizeMock(input.Length, input.Length, input.Length);
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.None);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, false);

            var actualStream = await file.GetStreamAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetStreamAsync_StreamEncryptedFalseAndCompressionZlib_EqualsInput()
        {
            string input = "testdatainputcompressed";
            string expected = input;
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock();
            var infoMock = CreateFileInfoMock("1", "testfile");
            var sizeMock = CreateSizeMock(input.Length, input.Length, input.Length);
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.Zlib);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, false);

            var actualStream = await file.GetStreamAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetStreamAsync_StreamEncryptedTrueAndCompressionNone_EqualsExpected()
        {
            string input = "testdatainputencrypted";
            string expected = "testdataexpected";
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            var sizeMock = CreateSizeMock(10, 20, 30);
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, expected, input, sizeMock.Object);
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock(repo);
            var infoMock = CreateFileInfoMock("1", "testfile");
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.None);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, true);

            var actualStream = await file.GetStreamAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetStreamAsync_StreamEncryptedTrueAndCompressionZlib_EqualsExpected()
        {
            string input = "testdatainputencryptedcompressed";
            string expected = "testdataexpected";
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            var sizeMock = CreateSizeMock(10, 20, 30);
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, expected, input, sizeMock.Object);
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock(repo);
            var infoMock = CreateFileInfoMock("1", "testfile");
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.Zlib);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, true);

            var actualStream = await file.GetStreamAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
        }
        #endregion

        #region GetStreamForArchivingAsync
        [TestMethod]
        public async Task GetStreamForArchivingAsync_StreamEncryptedFalseAndCompressionNone_EqualsExpected()
        {
            string input = "testdatainput";
            string expected = "testdataexpected";
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            var sizeMock = CreateSizeMock(10, 10, 10);
            var expectedSize = sizeMock.Object;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, expected, sizeMock.Object);
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock(repo);
            var infoMock = CreateFileInfoMock("1", "testfile");
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.None);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, false);

            var (actualStream, actualSize) = await file.GetStreamForArchivingAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expectedSize.EncodedSize, actualSize.EncodedSize);
            Assert.AreEqual(expectedSize.CompressedSize, actualSize.CompressedSize);
            Assert.AreEqual(expectedSize.Size, actualSize.Size);
        }

        [TestMethod]
        public async Task GetStreamForArchivingAsync_StreamEncryptedFalseAndCompressionZlib_EqualsExpected()
        {
            string input = "testdatainput";
            string expected = "testdatacompressedexpected";
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            var sizeMock = CreateSizeMock(10, 10, 10);
            var expectedSize = sizeMock.Object;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, expected, sizeMock.Object);
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock(repo);
            var infoMock = CreateFileInfoMock("1", "testfile");
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.Zlib);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, false);

            var (actualStream, actualSize) = await file.GetStreamForArchivingAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expectedSize.EncodedSize, actualSize.EncodedSize);
            Assert.AreEqual(expectedSize.CompressedSize, actualSize.CompressedSize);
            Assert.AreEqual(expectedSize.Size, actualSize.Size);
        }

        [TestMethod]
        public async Task GetStreamForArchivingAsync_StreamEncryptedTrueAndCompressionNone_EqualsInput()
        {
            string input = "testdatainput";
            string expected = input;
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            var sizeMock = CreateSizeMock(10, 10, 10);
            var expectedSize = sizeMock.Object;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, expected, sizeMock.Object);
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock(repo);
            var infoMock = CreateFileInfoMock("1", "testfile");
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.None);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, true);

            var (actualStream, actualSize) = await file.GetStreamForArchivingAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expectedSize.EncodedSize, actualSize.EncodedSize);
            Assert.AreEqual(expectedSize.CompressedSize, actualSize.CompressedSize);
            Assert.AreEqual(expectedSize.Size, actualSize.Size);
        }

        [TestMethod]
        public async Task GetStreamForArchivingAsync_StreamEncryptedTrueAndCompressionZlib_EqualsInput()
        {
            string input = "testdatainput";
            string expected = input;
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            var sizeMock = CreateSizeMock(10, 10, 10);
            var expectedSize = sizeMock.Object;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, expected, sizeMock.Object);
            using MemoryStream inputStream = StringToStream(input);
            var archiveMock = CreateArchiveMock(repo);
            var infoMock = CreateFileInfoMock("1", "testfile");
            var headerMock = CreateFileHeaderMock(sizeMock, 1, 0, CompressionType.Zlib);
            var file = new MS2File(archiveMock.Object, inputStream, infoMock.Object, headerMock.Object, true);

            var (actualStream, actualSize) = await file.GetStreamForArchivingAsync();
            string actual = await StreamToString(actualStream);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expectedSize.EncodedSize, actualSize.EncodedSize);
            Assert.AreEqual(expectedSize.CompressedSize, actualSize.CompressedSize);
            Assert.AreEqual(expectedSize.Size, actualSize.Size);
        }
        #endregion
    }
}
