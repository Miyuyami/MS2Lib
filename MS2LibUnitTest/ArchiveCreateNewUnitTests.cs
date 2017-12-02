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
    public class ArchiveCreateNewUnitTests
    {
        [TestMethod]
        public async Task TestCreateCompletelyNewMS2F()
        {
            const string folderForArchiving = "TestFolderForArchiving";
            string folderToArchive = Path.Combine(@"..\TestData", folderForArchiving);
            string folderToArchiveFullPath = Path.GetFullPath(folderToArchive) + @"\";
            string[] filesToArchive = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();

            MS2CryptoMode cryptoMode = MS2CryptoMode.MS2F;

            List<MS2File> files = new List<MS2File>(filesToArchive.Length);
            for (int i = 0; i < filesToArchive.Length; i++)
            {
                string file = filesToArchive[i];

                files.Add(MS2File.Create((uint)i + 1u, file.Remove(folderToArchiveFullPath), CompressionType.Zlib, cryptoMode, file));
            }

            const string testArchiveName = "TestArchiveMS2F";
            const string headerTestFileName = testArchiveName + ".m2h";
            const string dataTestFileName = testArchiveName + ".m2d";

            try
            {
                await MS2Archive.Save(cryptoMode, files, headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);
                Assert.IsTrue(File.Exists(headerTestFileName));
                Assert.IsTrue(File.Exists(dataTestFileName));

                using (MS2Archive archive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                {
                    Assert.AreEqual(archive.CryptoMode, cryptoMode);
                    Assert.AreEqual(archive.Files.Count, filesToArchive.Length);
                    //Assert.AreEqual(archive.Name, ArchiveFolder);

                    for (int i = 0; i < filesToArchive.Length; i++)
                    {
                        FileStream fsFile = File.OpenRead(filesToArchive[i]);
                        MS2File file = archive.Files[i];
                        Assert.AreEqual(file.Id, (uint)i + 1);
                        Assert.AreEqual(file.CompressionType, CompressionType.Zlib);
                        Assert.IsTrue(file.IsZlibCompressed);
                        Assert.AreEqual(file.Name, filesToArchive[i].Remove(folderToArchiveFullPath));
                        Assert.AreEqual(file.Header.CompressionType, file.CompressionType);
                        Assert.AreEqual(file.Header.Size, (uint)fsFile.Length);

                        (Stream stream, bool shouldDispose) = await file.GetDecryptedStreamAsync().ConfigureAwait(false);
                        try
                        {
                            byte[] originalBytes = new byte[fsFile.Length];
                            byte[] savedBytes = new byte[stream.Length];
                            int originalReadBytes = await fsFile.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                            int savedReadBytes = await stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
                }
            }
            finally
            {
                File.Delete(headerTestFileName);
                File.Delete(dataTestFileName);
            }
        }

        [TestMethod]
        public async Task TestCreateCompletelyNewNS2F()
        {
            const string folderForArchiving = "TestFolderForArchiving";
            string folderToArchive = Path.Combine(@"..\TestData", folderForArchiving);
            string folderToArchiveFullPath = Path.GetFullPath(folderToArchive) + @"\";
            string[] filesToArchive = Directory.GetFiles(folderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();

            MS2CryptoMode cryptoMode = MS2CryptoMode.NS2F;

            List<MS2File> files = new List<MS2File>(filesToArchive.Length);
            for (int i = 0; i < filesToArchive.Length; i++)
            {
                string file = filesToArchive[i];

                files.Add(MS2File.Create((uint)i + 1u, file.Remove(folderToArchiveFullPath), CompressionType.Zlib, cryptoMode, file));
            }

            const string testArchiveName = "TestArchiveNS2F";
            const string headerTestFileName = testArchiveName + ".m2h";
            const string dataTestFileName = testArchiveName + ".m2d";

            try
            {
                await MS2Archive.Save(cryptoMode, files, headerTestFileName, dataTestFileName, RunMode.Async2).ConfigureAwait(false);
                Assert.IsTrue(File.Exists(headerTestFileName));
                Assert.IsTrue(File.Exists(dataTestFileName));

                using (MS2Archive archive = await MS2Archive.Load(headerTestFileName, dataTestFileName).ConfigureAwait(false))
                {
                    Assert.AreEqual(archive.CryptoMode, cryptoMode);
                    Assert.AreEqual(archive.Files.Count, filesToArchive.Length);
                    //Assert.AreEqual(archive.Name, ArchiveFolder);

                    for (int i = 0; i < filesToArchive.Length; i++)
                    {
                        FileStream fsFile = File.OpenRead(filesToArchive[i]);
                        MS2File file = archive.Files[i];
                        Assert.AreEqual(file.Id, (uint)i + 1);
                        Assert.AreEqual(file.CompressionType, CompressionType.Zlib);
                        Assert.IsTrue(file.IsZlibCompressed);
                        Assert.AreEqual(file.Name, filesToArchive[i].Remove(folderToArchiveFullPath));
                        Assert.AreEqual(file.Header.CompressionType, file.CompressionType);
                        Assert.AreEqual(file.Header.Size, (uint)fsFile.Length);

                        (Stream stream, bool shouldDispose) = await file.GetDecryptedStreamAsync().ConfigureAwait(false);
                        try
                        {
                            byte[] originalBytes = new byte[fsFile.Length];
                            byte[] savedBytes = new byte[stream.Length];
                            int originalReadBytes = await fsFile.ReadAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                            int savedReadBytes = await stream.ReadAsync(savedBytes, 0, savedBytes.Length).ConfigureAwait(false);
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
