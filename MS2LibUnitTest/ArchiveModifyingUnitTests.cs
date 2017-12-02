using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiscUtils;
using MS2Lib;

namespace MS2LibUnitTest
{
    [TestClass]
    public class ArchiveModifyingUnitTests
    {
        private static Random Random = new Random();

        private static void AddRandomFilesToArchive(MS2Archive archive)
        {
            const int minFilesForRandom = 10;
            const string folderForAddingName = "FilesForAddingToArchive";
            string folderToArchive = Path.Combine(@"..\TestData", folderForAddingName);
            string folderToArchiveFullPath = Path.GetFullPath(folderToArchive) + @"\";
            string[] filesToArchive = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();
            Assert.IsTrue(filesToArchive.Length > minFilesForRandom, $"you need at least {minFilesForRandom} files in the adding archive folder for this test");

            uint count = (uint)Random.Next(minFilesForRandom, filesToArchive.Length);
            HashSet<string> distinctFiles = new HashSet<string>();
            uint i = 0;
            while (i < count)
            {
                if (distinctFiles.Add(filesToArchive[Random.Next(0, filesToArchive.Length)]))
                {
                    i++;
                }
            }
            string[] files = distinctFiles.ToArray();
            Assert.AreEqual(files.Length, (int)count);
            for (i = 0; i < count; i++)
            {
                string file = files[i];

                archive.Files.Add(MS2File.Create((uint)archive.Files.Count + i, file.Remove(folderToArchiveFullPath), CompressionType.Zlib, archive.CryptoMode, file));
            }
        }

        private static void RemoveRandomFilesFromArchive(MS2Archive archive)
        {
            const int minFilesForRandom = 10;
            const int maxFilesForRandom = 100;
            Assert.IsTrue(archive.Files.Count > minFilesForRandom, $"you need at least {minFilesForRandom} files in the archive for this test");

            uint fileCount = (uint)Random.Next(minFilesForRandom, maxFilesForRandom);
            for (uint i = 0; i < fileCount; i++)
            {
                int index = Random.Next(0, archive.Files.Count);

                archive.Files.RemoveAt(index);
            }
        }

        private static void UpdateRandomFilesFromArchive(MS2Archive archive)
        {
            const int minFilesForRandom = 10;
            const string folderForAddingName = "FilesForAddingToArchive";
            string folderToArchive = Path.Combine(@"..\TestData", folderForAddingName);
            string folderToArchiveFullPath = Path.GetFullPath(folderToArchive) + @"\";
            string[] filesToArchive = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();
            Assert.IsTrue(filesToArchive.Length > minFilesForRandom, $"you need at least {minFilesForRandom} files in the adding archive folder for this test");

            uint count = (uint)Random.Next(minFilesForRandom, filesToArchive.Length);
            HashSet<string> distinctFiles = new HashSet<string>();
            uint i = 0;
            while (i < count)
            {
                if (distinctFiles.Add(filesToArchive[Random.Next(0, filesToArchive.Length)]))
                {
                    i++;
                }
            }
            string[] files = distinctFiles.ToArray();
            Assert.AreEqual(files.Length, (int)count);
            for (i = 0; i < count; i++)
            {
                string file = files[i];
                int index = Random.Next(0, archive.Files.Count);
                MS2File previousFile = archive.Files[index];

                archive.Files[index] = MS2File.CreateUpdate(previousFile, file);
            }
        }

        #region MS2F
        [TestMethod]
        public async Task AddingMS2F()
        {
            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                AddRandomFilesToArchive(archive);

                const string testArchiveName = "AddingTestMS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreNotEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreNotEqual(archive.Data.Size, testArchive.Data.Size);

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose) original = await archive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose) saved = await testArchive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                if (archive.Files[i].Header != null && testArchive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, testArchive.Files[i].Header.Size);
                                else if (archive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, saved.Stream.Length);
                                else if (testArchive.Files[i].Header != null) Assert.AreEqual(original.Stream.Length, testArchive.Files[i].Header.Size);
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
        public async Task RemovingMS2F()
        {
            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                RemoveRandomFilesFromArchive(archive);

                const string testArchiveName = "RemovingTestMS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreNotEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreNotEqual(archive.Data.Size, testArchive.Data.Size);

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose) original = await archive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose) saved = await testArchive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                if (archive.Files[i].Header != null && testArchive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, testArchive.Files[i].Header.Size);
                                else if (archive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, saved.Stream.Length);
                                else if (testArchive.Files[i].Header != null) Assert.AreEqual(original.Stream.Length, testArchive.Files[i].Header);
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
        public async Task UpdatingMS2F()
        {
            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                UpdateRandomFilesFromArchive(archive);

                const string testArchiveName = "UpdatingTestMS2F";
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

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose) original = await archive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose) saved = await testArchive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                if (archive.Files[i].Header != null && testArchive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, testArchive.Files[i].Header.Size);
                                else if (archive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, saved.Stream.Length);
                                else if (testArchive.Files[i].Header != null) Assert.AreEqual(original.Stream.Length, testArchive.Files[i].Header.Size);
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

        #region NS2F
        [TestMethod]
        public async Task AddingNS2F()
        {
            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                AddRandomFilesToArchive(archive);

                const string testArchiveName = "AddingTestNS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreNotEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreNotEqual(archive.Data.Size, testArchive.Data.Size);

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose) original = await archive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose) saved = await testArchive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                if (archive.Files[i].Header != null && testArchive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, testArchive.Files[i].Header.Size);
                                else if (archive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, saved.Stream.Length);
                                else if (testArchive.Files[i].Header != null) Assert.AreEqual(original.Stream.Length, testArchive.Files[i].Header.Size);
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
        public async Task RemovingNS2F()
        {
            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                RemoveRandomFilesFromArchive(archive);

                const string testArchiveName = "RemovingTestNS2F";
                const string headerTestFileName = testArchiveName + ".m2h";
                const string dataTestFileName = testArchiveName + ".m2d";

                try
                {
                    await archive.Save(headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);

                    using (MS2Archive testArchive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                    {
                        Assert.AreEqual(archive.CryptoMode, testArchive.CryptoMode);
                        Assert.AreEqual(archive.Files.Count, testArchive.Files.Count);
                        Assert.AreNotEqual(archive.Header.Size, testArchive.Header.Size);
                        Assert.AreNotEqual(archive.Data.Size, testArchive.Data.Size);

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose) original = await archive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose) saved = await testArchive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                if (archive.Files[i].Header != null && testArchive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, testArchive.Files[i].Header.Size);
                                else if (archive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, saved.Stream.Length);
                                else if (testArchive.Files[i].Header != null) Assert.AreEqual(original.Stream.Length, testArchive.Files[i].Header);
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
        public async Task UpdatingNS2F()
        {
            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";

            using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
            {
                UpdateRandomFilesFromArchive(archive);

                const string testArchiveName = "UpdatingTestNS2F";
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

                        for (int i = 0; i < testArchive.Files.Count; i++)
                        {
                            Assert.AreEqual(archive.Files[i].IsZlibCompressed, testArchive.Files[i].IsZlibCompressed);
                            Assert.AreEqual(archive.Files[i].CompressionType, testArchive.Files[i].CompressionType);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Id, testArchive.Files[i].InfoHeader.Id);
                            Assert.AreEqual(archive.Files[i].InfoHeader.Name, testArchive.Files[i].InfoHeader.Name);
                            Assert.AreEqual(archive.Files[i].InfoHeader.RootFolderId, testArchive.Files[i].InfoHeader.RootFolderId);

                            (Stream Stream, bool ShouldDispose) original = await archive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);
                            (Stream Stream, bool ShouldDispose) saved = await testArchive.Files[i].GetDecryptedStreamAsync().ConfigureAwait(false);

                            try
                            {
                                if (archive.Files[i].Header != null && testArchive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, testArchive.Files[i].Header.Size);
                                else if (archive.Files[i].Header != null) Assert.AreEqual(archive.Files[i].Header.Size, saved.Stream.Length);
                                else if (testArchive.Files[i].Header != null) Assert.AreEqual(original.Stream.Length, testArchive.Files[i].Header.Size);
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
    }
}
