using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static MS2Lib.Tests.CryptoTestHelper;
using static MS2Lib.Tests.TestHelper;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2ArchiveLoadAndSaveTests
    {
        #region static helpers
        private static void SetFileLength(string path, long length)
        {
            using var fs = File.OpenWrite(path);

            fs.SetLength(length);
        }

        private static void AddDataStringToArchive(IMS2Archive archive, string data, string dataForArchiving, Mock<IMS2SizeHeader> sizeMock, uint id, string name, CompressionType compressionType)
        {
            byte[] bytes = EncodingTest.GetBytes(data);
            var ms = new MemoryStream(bytes);
            byte[] bytesArchiving = EncodingTest.GetBytes(dataForArchiving);
            var msArchiving = new MemoryStream(bytesArchiving);

            var fileMock = CreateFileMock(archive, ms, msArchiving, sizeMock, id, name, compressionType);
            archive.Add(fileMock.Object);
        }

        private static Mock<IMS2File> CreateFileMock(IMS2Archive archive, MemoryStream stream, MemoryStream archivingStream, Mock<IMS2SizeHeader> archivingSizeMock, uint id, string name, CompressionType compressionType)
        {
            Mock<IMS2FileInfo> fileInfoMock = CreateFileInfoMock(id.ToString(), name);
            Mock<IMS2SizeHeader> sizeMock = CreateSizeMock(stream.Length, stream.Length, stream.Length);
            Mock<IMS2FileHeader> fileHeaderMock = CreateFileHeaderMock(sizeMock, id, 0, compressionType);
            var result = new Mock<IMS2File>(MockBehavior.Strict);

            result.SetupGet(f => f.Archive).Returns(archive);
            result.SetupGet(f => f.Header).Returns(fileHeaderMock.Object);
            result.SetupGet(f => f.Id).Returns(id);
            result.SetupGet(f => f.Info).Returns(fileInfoMock.Object);
            result.SetupGet(f => f.Name).Returns(name);
            result.Setup(f => f.GetStreamAsync()).Returns(Task.FromResult<Stream>(stream));
            result.Setup(f => f.GetStreamForArchivingAsync()).Returns(Task.FromResult<(Stream, IMS2SizeHeader)>((archivingStream, archivingSizeMock.Object)));
            result.Setup(f => f.Dispose()).Callback(() => { stream.Dispose(); archivingStream.Dispose(); });

            return result;
        }
        #endregion

        #region Load from file tests
        [TestMethod]
        [DataRow(2, "TestData\\MS2F", "archive MS2F encrypted")]
        [DataRow(123, "TestData\\NS2F", "archive NS2F encrypted")]
        public async Task GetAndLoadArchiveAsync_FilePaths_FileCountEqualsExpected(long fileCount, string pathWithoutExtension, string description)
        {
            long expected = fileCount;
            string headerPath = Path.ChangeExtension(pathWithoutExtension, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(pathWithoutExtension, MS2Archive.DataFileExtension);

            var archive = await MS2Archive.GetAndLoadArchiveAsync(headerPath, dataPath);
            long actual = archive.Count;

            Assert.AreEqual(expected, actual, description);
        }

        [TestMethod]
        public async Task LoadAsync_NotExistingFiles_ThrowsFileNotFoundException()
        {
            var archive = new MS2Archive(Repositories.Repos[MS2CryptoMode.MS2F]);

            await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => archive.LoadAsync("notexistsheader", "notexistsdata"));
        }
        #endregion

        #region Save integrity tests
        [TestMethod]
        public async Task Save_OneFileToPath_DataEqualsInput()
        {
            const string FileName = nameof(Save_OneFileToPath_DataEqualsInput);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123" + nameof(Save_OneFileToPath_DataEqualsInput);
            string encryptedInput = "encrypteddata654" + nameof(Save_OneFileToPath_DataEqualsInput);
            var sizeMock = CreateSizeMock(20, 30, 40);
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath, false);

            using var fsData = File.OpenRead(dataPath);
            string actual = await StreamToString(await repo.GetDecryptionStreamAsync(fsData, sizeMock.Object, false));
            Assert.AreEqual(input, actual);
        }

        [TestMethod]
        public async Task Save_OneFileToPath_ArchiveHeaderEqualsExpectedData()
        {
            const string FileName = nameof(Save_OneFileToPath_ArchiveHeaderEqualsExpectedData);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123" + nameof(Save_OneFileToPath_ArchiveHeaderEqualsExpectedData);
            string encryptedInput = "encrypteddata654" + nameof(Save_OneFileToPath_ArchiveHeaderEqualsExpectedData);
            var sizeMock = CreateSizeMock(1, 20, 8);
            IMS2SizeHeader expectedFileInfoSize = sizeMock.Object;
            IMS2SizeHeader expectedFileDataSize = sizeMock.Object;
            long expectedFileCount = 1;
            MS2CryptoMode expectedCryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(expectedCryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath, false);

            using var fsHeader = File.OpenRead(headerPath);
            using var br = new BinaryReader(fsHeader, EncodingTest, true);
            MS2CryptoMode actualCryptoMode = (MS2CryptoMode)br.ReadUInt32();
            var (actualFileInfoSize, actualFileDataSize, actualFileCount) = await repo.GetArchiveHeaderCrypto().ReadAsync(fsHeader);
            Assert.AreEqual(expectedCryptoMode, actualCryptoMode);
            Assert.AreEqual(expectedFileInfoSize.EncodedSize, actualFileInfoSize.EncodedSize);
            Assert.AreEqual(expectedFileInfoSize.CompressedSize, actualFileInfoSize.CompressedSize);
            Assert.AreEqual(expectedFileInfoSize.Size, actualFileInfoSize.Size);
            Assert.AreEqual(expectedFileDataSize.EncodedSize, actualFileDataSize.EncodedSize);
            Assert.AreEqual(expectedFileDataSize.CompressedSize, actualFileDataSize.CompressedSize);
            Assert.AreEqual(expectedFileDataSize.Size, actualFileDataSize.Size);
            Assert.AreEqual(expectedFileCount, actualFileCount);
        }

        [TestMethod]
        public async Task Save_OneFileToFile_FileInfoHeaderEqualsExpectedData()
        {
            const string FileName = nameof(Save_OneFileToFile_FileInfoHeaderEqualsExpectedData);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123," + nameof(Save_OneFileToFile_FileInfoHeaderEqualsExpectedData);
            string encryptedInput = "encrypteddata654," + nameof(Save_OneFileToFile_FileInfoHeaderEqualsExpectedData);
            var sizeMock = CreateSizeMock(1, 20, 8);
            MS2CryptoMode expectedCryptoMode = (MS2CryptoMode)12345;
            IMS2FileInfo expectedFileInfo = CreateFileInfoMock(1.ToString(), "singlefile").Object;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(expectedCryptoMode, EncodingTest, "1,singlefile", "1,singlefile", sizeMock.Object);

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath, false);

            using var fsHeader = File.OpenRead(headerPath);
            using var br = new BinaryReader(fsHeader, EncodingTest, true);
            MS2CryptoMode actualCryptoMode = (MS2CryptoMode)br.ReadUInt32();
            var (actualFileInfoSize, actualFileDataSize, actualFileCount) = await repo.GetArchiveHeaderCrypto().ReadAsync(fsHeader);
            var msFileInfo = await repo.GetDecryptionStreamAsync(fsHeader, actualFileInfoSize, true);
            using var srFileInfo = new StreamReader(msFileInfo, EncodingTest, true, -1, true);
            IMS2FileInfo actualFileInfo = await repo.GetFileInfoReaderCrypto().ReadAsync(srFileInfo);
            Assert.AreEqual(expectedFileInfo.Id, actualFileInfo.Id);
            Assert.AreEqual(expectedFileInfo.Path, actualFileInfo.Path);
            Assert.AreEqual(expectedFileInfo.RootFolderId, actualFileInfo.RootFolderId);
        }

        [TestMethod]
        public async Task Save_OneFileToFile_FileDataHeaderEqualsExpected()
        {
            const string FileName = nameof(Save_OneFileToFile_FileDataHeaderEqualsExpected);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123" + nameof(Save_OneFileToFile_FileDataHeaderEqualsExpected);
            string encryptedInput = "encrypteddata654" + nameof(Save_OneFileToFile_FileDataHeaderEqualsExpected);
            var sizeMock = CreateSizeMock(1, 20, 8);
            MS2CryptoMode expectedCryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(expectedCryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);
            IMS2FileHeader expectedFileData = await repo.GetFileHeaderCrypto().ReadAsync(StringToStream(input));

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath, false);

            using var fsHeader = File.OpenRead(headerPath);
            using var br = new BinaryReader(fsHeader, EncodingTest, true);
            MS2CryptoMode actualCryptoMode = (MS2CryptoMode)br.ReadUInt32();
            var (actualFileInfoSize, actualFileDataSize, actualFileCount) = await repo.GetArchiveHeaderCrypto().ReadAsync(fsHeader);
            var msFileInfo = await repo.GetDecryptionStreamAsync(fsHeader, actualFileInfoSize, true);
            var msFileData = await repo.GetDecryptionStreamAsync(fsHeader, actualFileDataSize, true);
            IMS2FileHeader actualFileData = await repo.GetFileHeaderCrypto().ReadAsync(msFileData);
            Assert.AreEqual(expectedFileData.Id, actualFileData.Id);
            Assert.AreEqual(expectedFileData.Offset, actualFileData.Offset);
            Assert.AreEqual(expectedFileData.CompressionType, actualFileData.CompressionType);
            Assert.AreEqual(expectedFileData.Size.EncodedSize, actualFileData.Size.EncodedSize);
            Assert.AreEqual(expectedFileData.Size.CompressedSize, actualFileData.Size.CompressedSize);
            Assert.AreEqual(expectedFileData.Size.Size, actualFileData.Size.Size);
        }


        [TestMethod]
        public async Task Save_ThreeFilesToFile_DataEqualsExpected()
        {
            const string FileName = nameof(Save_ThreeFilesToFile_DataEqualsExpected);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123" + nameof(Save_ThreeFilesToFile_DataEqualsExpected);
            string encryptedInput = "encrypteddata654" + nameof(Save_ThreeFilesToFile_DataEqualsExpected);
            var sbExpected = new StringBuilder();
            var sizeMock = CreateSizeMock(20, 30, 40);
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);
            const int fileCount = 3;

            var archive = MS2Archive.GetArchiveMS2F();
            for (uint i = 1; i <= fileCount; i++)
            {
                sbExpected.Append(input);
                AddDataStringToArchive(archive, input, encryptedInput, sizeMock, i, "file" + i, CompressionType.None);
            }
            await archive.SaveAsync(headerPath, dataPath, false);

            using var fsData = File.OpenRead(dataPath);
            StringBuilder sbActual = new StringBuilder();
            for (uint i = 1; i <= fileCount; i++)
            {
                string actual = await StreamToString(await repo.GetDecryptionStreamAsync(fsData, sizeMock.Object, false));
                sbActual.Append(actual);
            }
            Assert.AreEqual(sbExpected.ToString(), sbActual.ToString());
        }

        [TestMethod]
        public async Task Save_ThreeFilesCompressedToFile_DataEqualsExpected()
        {
            const string FileName = nameof(Save_ThreeFilesToFile_DataEqualsExpected);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123" + nameof(Save_ThreeFilesToFile_DataEqualsExpected);
            string encryptedInput = "encrypteddata654" + nameof(Save_ThreeFilesToFile_DataEqualsExpected);
            var sbExpected = new StringBuilder();
            var sizeMock = CreateSizeMock(20, 30, 40);
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);
            const int fileCount = 3;

            var archive = MS2Archive.GetArchiveMS2F();
            for (uint i = 1; i <= fileCount; i++)
            {
                sbExpected.Append(input);
                AddDataStringToArchive(archive, input, encryptedInput, sizeMock, i, "file" + i, CompressionType.Zlib);
            }
            await archive.SaveAsync(headerPath, dataPath, false);

            using var fsData = File.OpenRead(dataPath);
            StringBuilder sbActual = new StringBuilder();
            for (uint i = 1; i <= fileCount; i++)
            {
                string actual = await StreamToString(await repo.GetDecryptionStreamAsync(fsData, sizeMock.Object, false));
                sbActual.Append(actual);
            }
            Assert.AreEqual(sbExpected.ToString(), sbActual.ToString());
        }

        [TestMethod]
        public async Task Save_ThreeFilesToFile_ArchiveHeaderEqualsExpected()
        {
            Assert.Inconclusive();
            //const string FileName = "OneFileArchive";
            //string headerPath = (Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension));
            //string dataPath = (Path.ChangeExtension(FileName, MS2Archive.DataFileExtension));
            //var bundle = GetRandomCryptoDataMS2F();
            //string input = bundle.Data;
            //string encryptedInput = bundle.EncryptedData;
            //var sizeMock = bundle.SizeMock;
            //uint fileId = 1;
            //string fileName = "name";
            //IMS2SizeHeader expectedFileInfoSize = CreateSizeMock(28, 19, 11).Object;
            //IMS2SizeHeader expectedFileDataSize = CreateSizeMock(28, 21, 48).Object;
            //long expectedFileCount = 1;
            //MS2CryptoMode expectedCryptoMode = MS2CryptoMode.MS2F;
            //IMS2ArchiveCryptoRepository repo = Repositories.Repos[expectedCryptoMode];

            //var archive = MS2Archive.GetArchiveMS2F();
            //for (uint i = 1; i <= 3; i++)
            //{
            //    AddDataStringToArchive(archive, input, encryptedInput, sizeMock, i, "file" + i, CompressionType.None);
            //}
            //await archive.SaveAsync(headerPath, dataPath);

            //using var fs = File.OpenRead(headerPath);
            //using var br = new BinaryReader(fs, Encoding.ASCII, true);
            //MS2CryptoMode actualCryptoMode = (MS2CryptoMode)br.ReadUInt32();
            //Assert.AreEqual(expectedCryptoMode, actualCryptoMode);
            //var (actualFileInfoSize, actualfileDataSize, actualFileCount) = await repo.GetArchiveHeaderCrypto().ReadAsync(fs);
            //Assert.AreEqual(expectedFileInfoSize.EncodedSize, actualFileInfoSize.EncodedSize);
            //Assert.AreEqual(expectedFileInfoSize.CompressedSize, actualFileInfoSize.CompressedSize);
            //Assert.AreEqual(expectedFileInfoSize.Size, actualFileInfoSize.Size);
            //Assert.AreEqual(expectedFileDataSize.EncodedSize, actualfileDataSize.EncodedSize);
            //Assert.AreEqual(expectedFileDataSize.CompressedSize, actualfileDataSize.CompressedSize);
            //Assert.AreEqual(expectedFileDataSize.Size, actualfileDataSize.Size);
            //Assert.AreEqual(expectedFileCount, actualFileCount);
        }

        [TestMethod]
        public async Task Save_ThreeFilesToFile_FileInfoHeaderEqualsExpected()
        {
            Assert.Inconclusive();
            //const string FileName = "OneFileArchive";
            //string headerPath = (Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension));
            //string dataPath = (Path.ChangeExtension(FileName, MS2Archive.DataFileExtension));
            //var bundle = GetRandomCryptoDataMS2F();
            //string input = bundle.Data;
            //string encryptedInput = bundle.EncryptedData;
            //var sizeMock = bundle.SizeMock;
            //uint fileId = 1;
            //string fileName = "name";
            //IMS2FileInfo expectedFileInfo = CreateFileInfoMock(fileId.ToString(), fileName).Object;
            //IMS2ArchiveCryptoRepository repo = Repositories.Repos[MS2CryptoMode.MS2F];

            //var archive = MS2Archive.GetArchiveMS2F();
            //AddDataStringToArchive(archive, input, encryptedInput, sizeMock, fileId, fileName);
            //await archive.SaveAsync(headerPath, dataPath);

            //using var fs = File.OpenRead(headerPath);
            //using var br = new BinaryReader(fs, Encoding.ASCII, true);
            //MS2CryptoMode actualCryptoMode = (MS2CryptoMode)br.ReadUInt32();
            //var (actualFileInfoSize, actualfileDataSize, actualFileCount) = await repo.GetArchiveHeaderCrypto().ReadAsync(fs);
            //var msFileInfo = await GetDecryptionStreamAsync(fs, actualFileInfoSize, actualCryptoMode, true);
            //using var srFileInfo = new StreamReader(msFileInfo, Encoding.ASCII, true, -1, true);
            //IMS2FileInfo actualFileInfo = await repo.GetFileInfoReaderCrypto().ReadAsync(srFileInfo);
            //Assert.AreEqual(expectedFileInfo.Id, actualFileInfo.Id);
            //Assert.AreEqual(expectedFileInfo.Path, actualFileInfo.Path);
            //Assert.AreEqual(expectedFileInfo.RootFolderId, actualFileInfo.RootFolderId);
        }

        [TestMethod]
        public async Task Save_ThreeFilesToFile_FileDataHeaderEqualsExpected()
        {
            Assert.Inconclusive();
            //const string FileName = "OneFileArchive";
            //string headerPath = (Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension));
            //string dataPath = (Path.ChangeExtension(FileName, MS2Archive.DataFileExtension));
            //var bundle = GetRandomCryptoDataMS2F();
            //string input = bundle.Data;
            //string encryptedInput = bundle.EncryptedData;
            //var sizeMock = bundle.SizeMock;
            //uint expectedFileId = 1;
            //long expectedOffset = 0;
            //CompressionType expectedCompressionType = CompressionType.None;
            //string fileName = "name";
            //IMS2FileHeader expectedFileData = CreateFileHeaderMock(sizeMock, expectedFileId, expectedOffset, expectedCompressionType).Object;
            //IMS2ArchiveCryptoRepository repo = Repositories.Repos[MS2CryptoMode.MS2F];

            //var archive = MS2Archive.GetArchiveMS2F();
            //AddDataStringToArchive(archive, input, encryptedInput, sizeMock, expectedFileId, fileName);
            //await archive.SaveAsync(headerPath, dataPath);

            //using var fs = File.OpenRead(headerPath);
            //using var br = new BinaryReader(fs, Encoding.ASCII, true);
            //MS2CryptoMode actualCryptoMode = (MS2CryptoMode)br.ReadUInt32();
            //var (actualFileInfoSize, actualfileDataSize, actualFileCount) = await repo.GetArchiveHeaderCrypto().ReadAsync(fs);
            //var msFileInfo = await GetDecryptionStreamAsync(fs, actualFileInfoSize, actualCryptoMode, true);
            //using var srFileInfo = new StreamReader(msFileInfo, Encoding.ASCII, true, -1, true);
            //await repo.GetFileInfoReaderCrypto().ReadAsync(srFileInfo);
            //var msFileData = await GetDecryptionStreamAsync(fs, actualfileDataSize, actualCryptoMode, true);
            //IMS2FileHeader actualFileData = await repo.GetFileHeaderCrypto().ReadAsync(msFileData);
            //Assert.AreEqual(expectedFileData.Id, actualFileData.Id);
            //Assert.AreEqual(expectedFileData.Offset, actualFileData.Offset);
            //Assert.AreEqual(expectedFileData.CompressionType, actualFileData.CompressionType);
            //Assert.AreEqual(expectedFileData.Size.EncodedSize, actualFileData.Size.EncodedSize);
            //Assert.AreEqual(expectedFileData.Size.CompressedSize, actualFileData.Size.CompressedSize);
            //Assert.AreEqual(expectedFileData.Size.Size, actualFileData.Size.Size);
        }

        [TestMethod]
        public async Task Save_ToFileOverwriteAndExpand_OutputEqualExpectedSize()
        {
            const string FileName = nameof(Save_ToFileOverwriteAndExpand_OutputEqualExpectedSize);
            string headerPath = Path.ChangeExtension(FileName, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(FileName, MS2Archive.DataFileExtension);
            string input = "inputdata123" + nameof(Save_ToFileOverwriteAndExpand_OutputEqualExpectedSize);
            string encryptedInput = "encrypteddata654" + nameof(Save_ToFileOverwriteAndExpand_OutputEqualExpectedSize);
            var sizeMock = CreateSizeMock(1, 20, 8);
            MS2CryptoMode expectedCryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(expectedCryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);
            int expectedHeaderLength = 60 + encryptedInput.Length * 2;
            int expectedDataLength = encryptedInput.Length;

            SetFileLength(headerPath, 1 << 10);
            SetFileLength(dataPath, 1 << 10);
            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "overwritefile", CompressionType.None);
            await archive.SaveAsync(headerPath, dataPath, false);

            int actualHeaderLength = File.ReadAllText(headerPath).Length;
            int actualDataLength = File.ReadAllText(dataPath).Length;
            Assert.AreEqual(expectedHeaderLength, actualHeaderLength);
            Assert.AreEqual(expectedDataLength, actualDataLength);
        }
        #endregion

        #region Save concurrent test
        [TestMethod]
        [DataRow("TestData\\MS2F", "TestData\\MS2F_expectedoutput", "archive MS2F encrypted")]
        [DataRow("TestData\\NS2F", "TestData\\NS2F_expectedoutput", "archive NS2F encrypted")]
        public async Task SaveAsync_LoadFromExistingThenSaveWithShouldSaveConcurrentlyTrue_OutputEqualsExpected(string pathWithoutExtension, string expectedPathWithoutExtension, string description)
        {
            string headerPath = Path.ChangeExtension(pathWithoutExtension, MS2Archive.HeaderFileExtension);
            string dataPath = Path.ChangeExtension(pathWithoutExtension, MS2Archive.DataFileExtension);
            string expectedHeaderPath = Path.ChangeExtension(expectedPathWithoutExtension, MS2Archive.HeaderFileExtension);
            string expectedDataPath = Path.ChangeExtension(expectedPathWithoutExtension, MS2Archive.DataFileExtension);
            string outputHeaderPath = headerPath + ".out";
            string outputDataPath = dataPath + ".out";

            var archive = await MS2Archive.GetAndLoadArchiveAsync(headerPath, dataPath);
            await archive.SaveAsync(outputHeaderPath, outputDataPath, true);

            var expectedHeaderBytes = await File.ReadAllBytesAsync(expectedHeaderPath);
            var expectedDataBytes = await File.ReadAllBytesAsync(expectedDataPath);
            var actualHeaderBytes = await File.ReadAllBytesAsync(outputHeaderPath);
            var actualDataBytes = await File.ReadAllBytesAsync(outputDataPath);

            CollectionAssert.AreEqual(expectedHeaderBytes, actualHeaderBytes);
            CollectionAssert.AreEqual(expectedDataBytes, actualDataBytes);
        }
        #endregion
    }
}
