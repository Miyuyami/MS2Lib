using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib;

namespace MS2LibUnitTest
{
    [TestClass]
    public class ArchiveLoadSaveUnitTests
    {
        private static MS2Archive Archive;

        private const string HeaderFilePath = @"..\TestData\Xml.m2h";
        private const string DataFilePath = @"..\TestData\Xml.m2d";

        [ClassInitialize]
        public static async Task Initialize(TestContext testContext)
        {
            Archive = await MS2Archive.Load(HeaderFilePath, DataFilePath);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            Archive.Dispose();
        }

        [TestMethod]
        public void ArchiveIntegrityTestNS2F()
        {
            Assert.AreEqual(Archive.CryptoMode, MS2CryptoMode.NS2F);
            Assert.AreEqual(Archive.Files.Count, 66107);
            Assert.AreEqual(Archive.Header, new MS2SizeHeader(502536u, 376900u, 3314723u));
            Assert.AreEqual(Archive.Data, new MS2SizeHeader(776208u, 582154u, 2379852u));
        }

        [TestMethod]
        // encoded and compressed sizes can change depending on compression level
        // so we only check for the uncompressed/decrypted size
        public async Task SaveTestNS2F()
        {
            const string testArchiveName = "SaveTest";
            const string headerTestFileName = testArchiveName + ".m2h";
            const string dataTestFileName = testArchiveName + ".m2d";

            try
            {
                await Archive.Save(headerTestFileName, dataTestFileName);

                using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName))
                {
                    Assert.AreEqual(Archive.CryptoMode, testArchive.CryptoMode);
                    Assert.AreEqual(Archive.Files.Count, testArchive.Files.Count);
                    Assert.AreEqual(Archive.Header.Size, testArchive.Header.Size);
                    Assert.AreEqual(Archive.Data.Size, testArchive.Data.Size);

                    for (int i = 0; i < testArchive.Files.Count; i++)
                    {
                        Assert.AreEqual(Archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                        Assert.AreEqual(Archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                        Assert.AreEqual(Archive.Files[i].Header?.Size, testArchive.Files[i].Header?.Size);
                        Assert.AreEqual(Archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                        Assert.AreEqual(Archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                        Assert.AreEqual(Archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                        (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await Archive.Files[i].GetEncryptedStreamAsync();
                        (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[i].GetEncryptedStreamAsync();

                        try
                        {
                            Assert.AreEqual(original.Header.Size, saved.Header.Size);
                            Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                            byte[] originalBytes = new byte[original.Stream.Length];
                            byte[] savedBytes = new byte[saved.Stream.Length];
                            int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length);
                            int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length);
                            Assert.AreEqual(originalReadBytes, savedReadBytes);

                            CollectionAssert.AreEqual(originalBytes, savedBytes);
                        }
                        finally
                        {
                            if (original.ShouldDispose)
                            {
                                original.Stream.Dispose();
                            }

                            if (saved.ShouldDispose)
                            {
                                saved.Stream.Dispose();
                            }
                        }
                    }
                }
            }
            finally
            {
                File.Delete(headerTestFileName);
                File.Delete(dataTestFileName);
            }
        }
    }
}
