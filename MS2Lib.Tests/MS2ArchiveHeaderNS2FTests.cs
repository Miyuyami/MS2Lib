using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib.NS2F;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2ArchiveHeaderNS2FTests
    {
        [TestMethod]
        public async Task Read_Stream_EqualsTrue()
        {
            MS2SizeHeader expectedHeader = new MS2SizeHeader(1, 2, 3);
            MS2SizeHeader expectedData = new MS2SizeHeader(4, 5, 6);
            long expectedFileCount = 7;

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write((uint)expectedFileCount);
            bw.Write(expectedData.CompressedSize);
            bw.Write(expectedData.EncodedSize);
            bw.Write(expectedHeader.Size);
            bw.Write(expectedHeader.CompressedSize);
            bw.Write(expectedHeader.EncodedSize);
            bw.Write(expectedData.Size);
            ms.Position = 0;

            var obj = new MS2ArchiveHeaderNS2F();
            var (actualFileInfoSize, actualFileDataSize, actualFileCount) = await obj.ReadAsync(ms);

            Assert.AreEqual(expectedHeader, actualFileInfoSize);
            Assert.AreEqual(expectedData, actualFileDataSize);
            Assert.AreEqual(expectedFileCount, actualFileCount);
        }

        [TestMethod]
        public async Task Read_FromWrite_EqualsTrue()
        {
            MS2SizeHeader expectedHeader = new MS2SizeHeader(1, 2, 3);
            MS2SizeHeader expectedData = new MS2SizeHeader(4, 5, 6);
            long expectedFileCount = 7;
            var obj = new MS2ArchiveHeaderNS2F();
            using var ms = new MemoryStream();
            await obj.WriteAsync(ms, expectedHeader, expectedData, expectedFileCount);
            ms.Position = 0;

            var (actualFileInfoSize, actualFileDataSize, actualFileCount) = await obj.ReadAsync(ms);

            Assert.AreEqual(expectedHeader, actualFileInfoSize);
            Assert.AreEqual(expectedData, actualFileDataSize);
            Assert.AreEqual(expectedFileCount, actualFileCount);
        }

        [TestMethod]
        public async Task Write_Data_EqualsTrue()
        {
            MS2SizeHeader expectedHeader = new MS2SizeHeader(1, 2, 3);
            MS2SizeHeader expectedData = new MS2SizeHeader(4, 5, 6);
            long expectedFileCount = 7;

            var obj = new MS2ArchiveHeaderNS2F();
            using var ms = new MemoryStream();
            await obj.WriteAsync(ms, expectedHeader, expectedData, expectedFileCount);
            ms.Position = 0;

            using var br = new BinaryReader(ms);
            long fileCount = br.ReadUInt32();
            long dataCompressedSize = br.ReadInt64();
            long dataEncodedSize = br.ReadInt64();
            long size = br.ReadInt64();
            long compressedSize = br.ReadInt64();
            long encodedSize = br.ReadInt64();
            long dataSize = br.ReadInt64();

            var actualFileInfoSize = new MS2SizeHeader(encodedSize, compressedSize, size);
            var actualFileDataSize = new MS2SizeHeader(dataEncodedSize, dataCompressedSize, dataSize);
            var actualFileCount = fileCount;

            Assert.AreEqual(expectedHeader, actualFileInfoSize);
            Assert.AreEqual(expectedData, actualFileDataSize);
            Assert.AreEqual(expectedFileCount, actualFileCount);
        }

        [TestMethod]
        public async Task Write_FromRead_EqualsTrue()
        {
            byte[] bytes = Enumerable.Range(1, 100).SelectMany(i => BitConverter.GetBytes(i)).ToArray();
            using var ms = new MemoryStream(bytes);

            var obj = new MS2ArchiveHeaderNS2F();
            var (expectedHeader, expectedData, expectedFileCount) = await obj.ReadAsync(ms);
            ms.Position = 0;

            await obj.WriteAsync(ms, expectedHeader, expectedData, expectedFileCount);
            ms.Position = 0;

            var (actualFileInfoSize, actualFileDataSize, actualFileCount) = await obj.ReadAsync(ms);

            Assert.AreEqual(expectedHeader, actualFileInfoSize);
            Assert.AreEqual(expectedData, actualFileDataSize);
            Assert.AreEqual(expectedFileCount, actualFileCount);
        }
    }
}
