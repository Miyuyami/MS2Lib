using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib;

namespace MS2LibUnitTest
{
    // encoded and compressed sizes can change depending on compression level
    // so we only check for the uncompressed/decrypted size
    [TestClass]
    public class ArchiveLoadSaveUnitTests
    {
        #region RunMode.Sync
        [TestMethod]
        public async Task LoadThenSaveTestSyncMS2F()
        {
            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.MS2F;

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                Assert.AreEqual(archive.Files.Count, 1694);
                Assert.AreEqual(archive.Header, new MS2SizeHeader(11056u, 8291u, 36097u));
                Assert.AreEqual(archive.Data, new MS2SizeHeader(31752u, 23813u, 81312u));

                const string testArchiveName = "SyncSaveTestMS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Sync).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreEqual(archive.Data.Size, testArchive.Data.Size);

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].Header?.Size, testArchive.Files[i].Header?.Size);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await archive.Files[i].GetEncryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[i].GetEncryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                Assert.AreEqual(original.Header.Size, saved.Header.Size);
                                Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                                byte[] originalBytes = new byte[original.Stream.Length];
                                byte[] savedBytes = new byte[saved.Stream.Length];
                                int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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

        [TestMethod]
        public async Task LoadThenSaveTestSyncNS2F()
        {
            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.NS2F;

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                Assert.AreEqual(archive.Files.Count, 66107);
                Assert.AreEqual(archive.Header, new MS2SizeHeader(502536u, 376900u, 3314723u));
                Assert.AreEqual(archive.Data, new MS2SizeHeader(776208u, 582154u, 2379852u));

                const string testArchiveName = "SyncSaveTestNS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Sync).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreEqual(archive.Data.Size, testArchive.Data.Size);

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].Header?.Size, testArchive.Files[i].Header?.Size);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await archive.Files[i].GetEncryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[i].GetEncryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                Assert.AreEqual(original.Header.Size, saved.Header.Size);
                                Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                                byte[] originalBytes = new byte[original.Stream.Length];
                                byte[] savedBytes = new byte[saved.Stream.Length];
                                int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
        #endregion

        #region RunMode.Async
        [TestMethod]
        public async Task LoadThenSaveTestAsyncMS2F()
        {
            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.MS2F;

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                Assert.AreEqual(archive.Files.Count, 1694);
                Assert.AreEqual(archive.Header, new MS2SizeHeader(11056u, 8291u, 36097u));
                Assert.AreEqual(archive.Data, new MS2SizeHeader(31752u, 23813u, 81312u));

                const string testArchiveName = "AsyncSaveTestMS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreEqual(archive.Data.Size, testArchive.Data.Size);

                        Task[] tasks = new Task[testArchive.Files.Count];

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            int ic = i;
                            Assert.AreEqual(archive.Files[ic].IsZlibCompressed, testArchive.Files[ic].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[ic].CompressionType, testArchive.Files[ic].CompressionType);
                            Assert.AreEqual(archive.Files[ic].Header?.Size, testArchive.Files[ic].Header?.Size);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Id, testArchive.Files[ic].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Name, testArchive.Files[ic].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.RootFolderId, testArchive.Files[ic].InfoHeader.RootFolderId);

                            tasks[ic] = Task.Run(async () =>
                            {
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await archive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);

                                try
                                {
                                    Assert.AreEqual(original.Header.Size, saved.Header.Size);
                                    Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                                    byte[] originalBytes = new byte[original.Stream.Length];
                                    byte[] savedBytes = new byte[saved.Stream.Length];
                                    int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                    int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
                            });
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                finally
                {
                    File.Delete(headerTestFileName);
                    File.Delete(dataTestFileName);
                }
            }
        }

        [TestMethod]
        public async Task LoadThenSaveTestAsyncNS2F()
        {
            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.NS2F;

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                Assert.AreEqual(archive.Files.Count, 66107);
                Assert.AreEqual(archive.Header, new MS2SizeHeader(502536u, 376900u, 3314723u));
                Assert.AreEqual(archive.Data, new MS2SizeHeader(776208u, 582154u, 2379852u));

                const string testArchiveName = "AsyncSaveTestNS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreEqual(archive.Data.Size, testArchive.Data.Size);

                        Task[] tasks = new Task[testArchive.Files.Count];

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            int ic = i;
                            Assert.AreEqual(archive.Files[ic].IsZlibCompressed, testArchive.Files[ic].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[ic].CompressionType, testArchive.Files[ic].CompressionType);
                            Assert.AreEqual(archive.Files[ic].Header?.Size, testArchive.Files[ic].Header?.Size);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Id, testArchive.Files[ic].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Name, testArchive.Files[ic].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.RootFolderId, testArchive.Files[ic].InfoHeader.RootFolderId);

                            tasks[ic] = Task.Run(async () =>
                            {
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await archive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);

                                try
                                {
                                    Assert.AreEqual(original.Header.Size, saved.Header.Size);
                                    Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                                    byte[] originalBytes = new byte[original.Stream.Length];
                                    byte[] savedBytes = new byte[saved.Stream.Length];
                                    int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                    int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
                            });
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                finally
                {
                    File.Delete(headerTestFileName);
                    File.Delete(dataTestFileName);
                }
            }
        }
        #endregion

        #region RunMode.Async2
        [TestMethod]
        public async Task LoadThenSaveTestAsync2MS2F()
        {
            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.MS2F;

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                Assert.AreEqual(archive.Files.Count, 1694);
                Assert.AreEqual(archive.Header, new MS2SizeHeader(11056u, 8291u, 36097u));
                Assert.AreEqual(archive.Data, new MS2SizeHeader(31752u, 23813u, 81312u));

                const string testArchiveName = "Async2SaveTestMS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreEqual(archive.Data.Size, testArchive.Data.Size);

                        Task[] tasks = new Task[testArchive.Files.Count];

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            int ic = i;
                            Assert.AreEqual(archive.Files[ic].IsZlibCompressed, testArchive.Files[ic].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[ic].CompressionType, testArchive.Files[ic].CompressionType);
                            Assert.AreEqual(archive.Files[ic].Header?.Size, testArchive.Files[ic].Header?.Size);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Id, testArchive.Files[ic].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Name, testArchive.Files[ic].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.RootFolderId, testArchive.Files[ic].InfoHeader.RootFolderId);

                            tasks[ic] = Task.Run(async () =>
                            {
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await archive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);

                                try
                                {
                                    Assert.AreEqual(original.Header.Size, saved.Header.Size);
                                    Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                                    byte[] originalBytes = new byte[original.Stream.Length];
                                    byte[] savedBytes = new byte[saved.Stream.Length];
                                    int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                    int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
                            });
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                finally
                {
                    File.Delete(headerTestFileName);
                    File.Delete(dataTestFileName);
                }
            }
        }

        [TestMethod]
        public async Task LoadThenSaveTestAsync2NS2F()
        {
            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.NS2F;

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                Assert.AreEqual(archive.Files.Count, 66107);
                Assert.AreEqual(archive.Header, new MS2SizeHeader(502536u, 376900u, 3314723u));
                Assert.AreEqual(archive.Data, new MS2SizeHeader(776208u, 582154u, 2379852u));

                const string testArchiveName = "Async2SaveTestNS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreEqual(archive.Data.Size, testArchive.Data.Size);

                        Task[] tasks = new Task[testArchive.Files.Count];

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            int ic = i;
                            Assert.AreEqual(archive.Files[ic].IsZlibCompressed, testArchive.Files[ic].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[ic].CompressionType, testArchive.Files[ic].CompressionType);
                            Assert.AreEqual(archive.Files[ic].Header?.Size, testArchive.Files[ic].Header?.Size);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Id, testArchive.Files[ic].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.Name, testArchive.Files[ic].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[ic].InfoHeader.RootFolderId, testArchive.Files[ic].InfoHeader.RootFolderId);

                            tasks[ic] = Task.Run(async () =>
                            {
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) original = await archive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);
                                (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) saved = await testArchive.Files[ic].GetEncryptedStreamAsync().ConfigureAwait(false);

                                try
                                {
                                    Assert.AreEqual(original.Header.Size, saved.Header.Size);
                                    Assert.AreEqual(original.Stream.Length, saved.Stream.Length);
                                    byte[] originalBytes = new byte[original.Stream.Length];
                                    byte[] savedBytes = new byte[saved.Stream.Length];
                                    int originalReadBytes = await original.Stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                    int savedReadBytes = await saved.Stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
                            });
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                finally
                {
                    File.Delete(headerTestFileName);
                    File.Delete(dataTestFileName);
                }
            }
        }
        #endregion
    }
}
