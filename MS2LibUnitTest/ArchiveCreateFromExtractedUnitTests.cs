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
        private const string HeaderFilePath = @"..\TestData\Xml.m2h";
        private const string DataFilePath = @"..\TestData\Xml.m2d";

        private const string ArchiveName = "Xml";
        private const string FolderToArchive = @"C:\Users\Miyu\Desktop\ReleaseOutput\" + ArchiveName;
        private static string FolderToArchiveFullPath = Path.GetFullPath(FolderToArchive) + @"\";
        private static string[] FilesToArchive = Directory.GetFiles(FolderToArchive, "*.*", SearchOption.AllDirectories).Select(p => Path.GetFullPath(p)).ToArray();

        [TestMethod]
        public async Task TestCreateConsistencyNS2F()
        {
            MS2CryptoMode cryptoMode = MS2CryptoMode.NS2F;

            List<MS2File> files = new List<MS2File>(FilesToArchive.Length);
            for (int i = 0; i < FilesToArchive.Length; i++)
            {
                string file = FilesToArchive[i];

                files.Add(MS2File.Create((uint)i, file.Remove(FolderToArchiveFullPath), CompressionType.Zlib, cryptoMode, file));
            }

            const string headerTestFileName = ArchiveName + ".m2h";
            const string dataTestFileName = ArchiveName + ".m2d";

            try
            {
                await MS2Archive.Save(cryptoMode, files, headerTestFileName, dataTestFileName);
                Assert.IsTrue(File.Exists(headerTestFileName));
                Assert.IsTrue(File.Exists(dataTestFileName));

                using (MS2Archive archive = await MS2Archive.Load(HeaderFilePath, DataFilePath))
                {
                    Assert.AreEqual(archive.CryptoMode, cryptoMode);
                    Assert.AreEqual(archive.Files.Count, FilesToArchive.Length);
                    //Assert.AreEqual(archive.Name, ArchiveName);

                    for (int i = 0; i < FilesToArchive.Length; i++)
                    {
                        FileStream fsFile = File.OpenRead(FilesToArchive[i]);
                        MS2File file = archive.Files[i];
                        Assert.AreEqual(file.Id, (i + 1).ToString());
                        Assert.AreEqual(file.CompressionType, CompressionType.Zlib);
                        Assert.IsTrue(file.IsZlibCompressed);
                        Assert.AreEqual(file.Name, FilesToArchive[i].Remove(FolderToArchiveFullPath));
                        Assert.AreEqual(file.Header.CompressionType, file.CompressionType);
                        Assert.AreEqual(file.Header.Size, (uint)fsFile.Length);

                        (Stream stream, bool shouldDispose) = await file.GetDecryptedStreamAsync();
                        try
                        {
                            byte[] originalBytes = new byte[fsFile.Length];
                            byte[] savedBytes = new byte[stream.Length];
                            int originalReadBytes = await fsFile.ReadAsync(originalBytes, 0, originalBytes.Length);
                            int savedReadBytes = await stream.ReadAsync(savedBytes, 0, savedBytes.Length);
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
