using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiscUtils;
using Moq;
using static MS2Lib.Tests.CryptoTestHelper;
using static MS2Lib.Tests.TestHelper;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2ArchiveLoadAndSaveTests
    {
        private const string HeaderFileExtension = ".m2h";
        private const string DataFileExtension = ".m2d";

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
            string headerPath = pathWithoutExtension + HeaderFileExtension;
            string dataPath = pathWithoutExtension + DataFileExtension;

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
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
            string input = "inputdata123" + nameof(Save_OneFileToPath_DataEqualsInput);
            string encryptedInput = "encrypteddata654" + nameof(Save_OneFileToPath_DataEqualsInput);
            var sizeMock = CreateSizeMock(20, 30, 40);
            MS2CryptoMode cryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(cryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath);

            using var fsData = File.OpenRead(dataPath);
            string actual = await StreamToString(await repo.GetDecryptionStreamAsync(fsData, sizeMock.Object, false));
            Assert.AreEqual(input, actual);
        }

        [TestMethod]
        public async Task Save_OneFileToPath_ArchiveHeaderEqualsExpectedData()
        {
            const string FileName = nameof(Save_OneFileToPath_ArchiveHeaderEqualsExpectedData);
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
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
            await archive.SaveAsync(headerPath, dataPath);

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
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
            string input = "inputdata123," + nameof(Save_OneFileToFile_FileInfoHeaderEqualsExpectedData);
            string encryptedInput = "encrypteddata654," + nameof(Save_OneFileToFile_FileInfoHeaderEqualsExpectedData);
            var sizeMock = CreateSizeMock(1, 20, 8);
            MS2CryptoMode expectedCryptoMode = (MS2CryptoMode)12345;
            IMS2FileInfo expectedFileInfo = CreateFileInfoMock(1.ToString(), "singlefile").Object;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(expectedCryptoMode, EncodingTest, "1,singlefile", "1,singlefile", sizeMock.Object);

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath);

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
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
            string input = "inputdata123" + nameof(Save_OneFileToFile_FileDataHeaderEqualsExpected);
            string encryptedInput = "encrypteddata654" + nameof(Save_OneFileToFile_FileDataHeaderEqualsExpected);
            var sizeMock = CreateSizeMock(1, 20, 8);
            MS2CryptoMode expectedCryptoMode = (MS2CryptoMode)12345;
            IMS2ArchiveCryptoRepository repo = new FakeCryptoRepository(expectedCryptoMode, EncodingTest, input, encryptedInput, sizeMock.Object);
            IMS2FileHeader expectedFileData = await repo.GetFileHeaderCrypto().ReadAsync(StringToStream(input));

            var archive = new MS2Archive(repo);
            AddDataStringToArchive(archive, input, encryptedInput, sizeMock, 1, "singlefile", CompressionType.Zlib);
            await archive.SaveAsync(headerPath, dataPath);

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
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
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
            await archive.SaveAsync(headerPath, dataPath);

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
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
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
            await archive.SaveAsync(headerPath, dataPath);

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
        public async Task Save_ToFileOverwriteAndExpand_OutputEqualExpectedSize()
        {
            const string FileName = nameof(Save_ToFileOverwriteAndExpand_OutputEqualExpectedSize);
            string headerPath = FileName + HeaderFileExtension;
            string dataPath = FileName + DataFileExtension;
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
            await archive.SaveAsync(headerPath, dataPath);

            int actualHeaderLength = File.ReadAllText(headerPath).Length;
            int actualDataLength = File.ReadAllText(dataPath).Length;
            Assert.AreEqual(expectedHeaderLength, actualHeaderLength);
            Assert.AreEqual(expectedDataLength, actualDataLength);
        }
        #endregion

        #region Save concurrent test
        [TestMethod]
        [DataRow("TestData\\MS2F", "TestData\\MS2F.out", "TestData\\MS2F_expectedoutput", "archive MS2F encrypted")]
        [DataRow("TestData\\NS2F", "TestData\\NS2F.out", "TestData\\NS2F_expectedoutput", "archive NS2F encrypted")]
        public async Task SaveConcurrentlyAsync_LoadFromExistingThenSave_OutputEqualsExpected(string pathWithoutExtension, string outputPathWithoutExtension, string expectedPathWithoutExtension, string description)
        {
            string headerPath = pathWithoutExtension + HeaderFileExtension;
            string dataPath = pathWithoutExtension + DataFileExtension;
            string expectedHeaderPath = expectedPathWithoutExtension + HeaderFileExtension;
            string expectedDataPath = expectedPathWithoutExtension + DataFileExtension;
            string outputHeaderPath = outputPathWithoutExtension + HeaderFileExtension;
            string outputDataPath = outputPathWithoutExtension + DataFileExtension;

            var archive = await MS2Archive.GetAndLoadArchiveAsync(headerPath, dataPath);
            await archive.SaveConcurrentlyAsync(outputHeaderPath, outputDataPath);

            var expectedHeaderBytes = await File.ReadAllBytesAsync(expectedHeaderPath);
            var expectedDataBytes = await File.ReadAllBytesAsync(expectedDataPath);
            var actualHeaderBytes = await File.ReadAllBytesAsync(outputHeaderPath);
            var actualDataBytes = await File.ReadAllBytesAsync(outputDataPath);

            CollectionAssert.AreEqual(expectedHeaderBytes, actualHeaderBytes);
            CollectionAssert.AreEqual(expectedDataBytes, actualDataBytes);
        }

        [TestMethod]
        [DataRow("TestData\\MS2F", "TestData\\MS2F_Files.out", "TestData\\MS2F_Files_expectedoutput", "TestData\\MS2F_Files", "MS2F")]
        [DataRow("TestData\\NS2F", "TestData\\NS2F_Files.out", "TestData\\NS2F_Files_expectedoutput", "TestData\\NS2F_Files", "NS2F")]
        public async Task SaveConcurrentlyAsync_FromEmptyAddExtractedFromExisting_OutputEqualsExpected(string pathWithoutExtension, string outputPathWithoutExtension, string expectedPathWithoutExtension, string extractFolderPath, string cryptoModeString)
        {
            var cryptoMode = Enum.Parse<MS2CryptoMode>(cryptoModeString, true);
            string headerPath = pathWithoutExtension + HeaderFileExtension;
            string dataPath = pathWithoutExtension + DataFileExtension;
            string expectedHeaderPath = expectedPathWithoutExtension + HeaderFileExtension;
            string expectedDataPath = expectedPathWithoutExtension + DataFileExtension;
            string outputHeaderPath = outputPathWithoutExtension + HeaderFileExtension;
            string outputDataPath = outputPathWithoutExtension + DataFileExtension;

            await ExtractArchiveFilesAsync(headerPath, dataPath, extractFolderPath);

            var archive = new MS2Archive(Repositories.Repos[cryptoMode]);
            var filePaths = GetFilesRelative(extractFolderPath);
            for (uint i = 0; i < filePaths.Length; i++)
            {
                AddAndCreateFileToArchive(archive, filePaths, i);
            }
            await archive.SaveConcurrentlyAsync(outputHeaderPath, outputDataPath);

            var expectedHeaderBytes = await File.ReadAllBytesAsync(expectedHeaderPath);
            var expectedDataBytes = await File.ReadAllBytesAsync(expectedDataPath);
            var actualHeaderBytes = await File.ReadAllBytesAsync(outputHeaderPath);
            var actualDataBytes = await File.ReadAllBytesAsync(outputDataPath);

            CollectionAssert.AreEqual(expectedHeaderBytes, actualHeaderBytes);
            CollectionAssert.AreEqual(expectedDataBytes, actualDataBytes);
        }
        #endregion


        private static async Task ExtractArchiveFilesAsync(string headerFile, string dataFile, string extractPath)
        {
            using IMS2Archive archive = await MS2Archive.GetAndLoadArchiveAsync(headerFile, dataFile).ConfigureAwait(false);

            foreach (var file in archive)
            {
                await ExtractFileAsync(extractPath, file).ConfigureAwait(false);
            }
        }

        private static async Task ExtractFileAsync(string destinationPath, IMS2File file)
        {
            string fileDestinationPath = Path.Combine(destinationPath, file.Name);

            using Stream stream = await file.GetStreamAsync().ConfigureAwait(false);

            await stream.CopyToAsync(fileDestinationPath).ConfigureAwait(false);
        }

        private static (string FullPath, string RelativePath)[] GetFilesRelative(string path)
        {
            if (!path.EndsWith(@"\"))
            {
                path += @"\";
            }

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            var result = new (string FullPath, string RelativePath)[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                result[i] = (files[i], files[i].Remove(path));
            }

            return result;
        }

        private static void AddAndCreateFileToArchive(IMS2Archive archive, (string fullPath, string relativePath)[] filePaths, uint index)
        {
            var (filePath, relativePath) = filePaths[index];

            uint id = index + 1;
            FileStream fsFile = File.OpenRead(filePath);
            IMS2FileInfo info = new MS2FileInfo(id.ToString(), relativePath);
            IMS2FileHeader header = new MS2FileHeader(fsFile.Length, id, 0, GetCompressionTypeFromFileExtension(filePath));
            IMS2File file = new MS2File(archive, fsFile, info, header, false);

            archive.Add(file);
        }

        private static CompressionType GetCompressionTypeFromFileExtension(string filePath, CompressionType defaultCompressionType = CompressionType.Zlib) =>
            (Path.GetExtension(filePath)) switch
            {
                ".png" => CompressionType.Png,
                ".usm" => CompressionType.Usm,
                ".zlib" => CompressionType.Zlib,
                _ => defaultCompressionType,
            };
    }
}
