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
    public class ArchiveCreateFromExtractedUnitTests
    {
        [TestMethod]
        public async Task TestCreateConsistencyMS2F()
        {
            const string archiveName = "PrecomputedTerrain";
            const string folderToArchive = @"C:\Users\Miyu\Desktop\ReleaseOutput\Resource\" + archiveName;
            string folderToArchiveFullPath = Path.GetFullPath(folderToArchive) + @"\";
            string[] filesToArchive = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();

            const string headerFilePath = @"..\TestData\PrecomputedTerrain.m2h";
            const string dataFilePath = @"..\TestData\PrecomputedTerrain.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.MS2F;

            MS2File[] files = new MS2File[filesToArchive.Length];
            for (int i = 0; i < filesToArchive.Length; i++)
            {
                string file = filesToArchive[i];

                files[i] = MS2File.Create((uint)i + 1u, file.Remove(folderToArchiveFullPath), CompressionType.Zlib, archiveCryptoMode, file);
            }

            const string testArchiveName = "FromExtractedMS2F";
            const string headerTestFileName = testArchiveName + ".m2h";
            const string dataTestFileName = testArchiveName + ".m2d";

            try
            {
                await MS2Archive.Save(archiveCryptoMode, files, headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);
                Assert.IsTrue(File.Exists(headerTestFileName));
                Assert.IsTrue(File.Exists(dataTestFileName));

                using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
                {
                    Dictionary<uint, MS2File> mappedFiles = archive.Files.Zip(files, (o, s) => (o, s))
                                                                         .ToDictionary(f => f.o.Id, f => files.Where(sf => sf.Name == f.o.Name).First());
                    Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                    Assert.AreEqual(archive.Files.Count, filesToArchive.Length);
                    //Assert.AreEqual(archive.Name, ArchiveName);

                    Task[] tasks = new Task[filesToArchive.Length];

                    for (int i = 0; i < filesToArchive.Length; i++)
                    {
                        int ic = i;
                        tasks[ic] = Task.Run(async () =>
                        {
                            MS2File file = archive.Files[ic];
                            MS2File savedFile = mappedFiles[file.Id];
                            var (savedStream, savedShouldDispose) = await savedFile.GetDecryptedStreamAsync().ConfigureAwait(false);
                            try
                            {
                                Assert.AreEqual(file.Id, (uint)ic + 1);
                                Assert.AreEqual(file.CompressionType, CompressionType.Zlib);
                                Assert.IsTrue(file.IsZlibCompressed);
                                Assert.AreEqual(file.Name, savedFile.Name);
                                Assert.AreEqual(file.Header.CompressionType, file.CompressionType);
                                Assert.AreEqual(file.Header.Size, (uint)savedStream.Length);

                                (Stream stream, bool shouldDispose) = await file.GetDecryptedStreamAsync().ConfigureAwait(false);
                                try
                                {
                                    byte[] savedBytes = new byte[savedStream.Length];
                                    byte[] originalBytes = new byte[stream.Length];
                                    int savedReadBytes = await savedStream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
                                    int originalReadBytes = await stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                    Assert.AreEqual(originalReadBytes, savedReadBytes);

                                    CollectionAssert.AreEqual(originalBytes, savedBytes);
                                }
                                finally
                                {
                                    if (shouldDispose)
                                    {
                                        stream.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (savedShouldDispose)
                                {
                                    savedStream.Dispose();
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

        [TestMethod]
        public async Task TestCreateConsistencyNS2F()
        {
            const string archiveName = "Xml";
            const string folderToArchive = @"C:\Users\Miyu\Desktop\ReleaseOutput\" + archiveName;
            string folderToArchiveFullPath = Path.GetFullPath(folderToArchive) + @"\";
            string[] filesToArchive = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();

            const string headerFilePath = @"..\TestData\Xml.m2h";
            const string dataFilePath = @"..\TestData\Xml.m2d";
            MS2CryptoMode archiveCryptoMode = MS2CryptoMode.NS2F;

            MS2File[] files = new MS2File[filesToArchive.Length];
            for (int i = 0; i < filesToArchive.Length; i++)
            {
                string file = filesToArchive[i];

                files[i] = MS2File.Create((uint)i + 1u, file.Remove(folderToArchiveFullPath), CompressionType.Zlib, archiveCryptoMode, file);
            }

            const string testArchiveName = "FromExtractedNS2F";
            const string headerTestFileName = testArchiveName + ".m2h";
            const string dataTestFileName = testArchiveName + ".m2d";

            try
            {
                await MS2Archive.Save(archiveCryptoMode, files, headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);
                Assert.IsTrue(File.Exists(headerTestFileName));
                Assert.IsTrue(File.Exists(dataTestFileName));

                using (MS2Archive archive = await MS2Archive.Load(headerFilePath, dataFilePath).ConfigureAwait(false))
                {
                    Dictionary<uint, MS2File> mappedFiles = archive.Files.Zip(files, (o, s) => (o, s))
                                                                         .ToDictionary(f => f.o.Id, f => files.Where(sf => sf.Name == f.o.Name).First());
                    Assert.AreEqual(archive.CryptoMode, archiveCryptoMode);
                    Assert.AreEqual(archive.Files.Count, filesToArchive.Length);
                    //Assert.AreEqual(archive.Name, ArchiveName);

                    Task[] tasks = new Task[filesToArchive.Length];

                    for (int i = 0; i < filesToArchive.Length; i++)
                    {
                        int ic = i;
                        tasks[ic] = Task.Run(async () =>
                        {
                            MS2File file = archive.Files[ic];
                            MS2File savedFile = mappedFiles[file.Id];
                            var (savedStream, savedShouldDispose) = await savedFile.GetDecryptedStreamAsync().ConfigureAwait(false);
                            try
                            {
                                Assert.AreEqual(file.Id, (uint)ic + 1);
                                Assert.AreEqual(file.CompressionType, CompressionType.Zlib);
                                Assert.IsTrue(file.IsZlibCompressed);
                                Assert.AreEqual(file.Name, savedFile.Name);
                                Assert.AreEqual(file.Header.CompressionType, file.CompressionType);
                                Assert.AreEqual(file.Header.Size, (uint)savedStream.Length);

                                (Stream stream, bool shouldDispose) = await file.GetDecryptedStreamAsync().ConfigureAwait(false);
                                try
                                {
                                    byte[] savedBytes = new byte[savedStream.Length];
                                    byte[] originalBytes = new byte[stream.Length];
                                    int savedReadBytes = await savedStream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
                                    int originalReadBytes = await stream.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                                    Assert.AreEqual(originalReadBytes, savedReadBytes);

                                    CollectionAssert.AreEqual(originalBytes, savedBytes);
                                }
                                finally
                                {
                                    if (shouldDispose)
                                    {
                                        stream.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (savedShouldDispose)
                                {
                                    savedStream.Dispose();
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
}
