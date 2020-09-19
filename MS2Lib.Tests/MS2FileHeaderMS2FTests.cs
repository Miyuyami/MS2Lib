using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib.MS2F;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2FileHeaderMS2FTests
    {
        [TestMethod]
        public async Task Read_Stream_EqualsTrue()
        {
            MS2FileHeader expectedHeader = new MS2FileHeader(1, 2, 3, 4, 5, CompressionType.Zlib);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(0u);
            bw.Write(expectedHeader.Id);
            bw.Write((long)expectedHeader.CompressionType);
            bw.Write(expectedHeader.Offset);
            bw.Write(expectedHeader.Size.EncodedSize);
            bw.Write(expectedHeader.Size.CompressedSize);
            bw.Write(expectedHeader.Size.Size);
            ms.Position = 0;

            var obj = new MS2FileHeaderMS2F();
            var actualHeader = await obj.ReadAsync(ms);

            Assert.AreEqual(expectedHeader, actualHeader);
        }

        [TestMethod]
        public async Task Read_FromWrite_EqualsTrue()
        {
            MS2FileHeader expectedHeader = new MS2FileHeader(1, 2, 3, 4, 5, CompressionType.Zlib);
            var obj = new MS2FileHeaderMS2F();
            using var ms = new MemoryStream();
            await obj.WriteAsync(ms, expectedHeader);
            ms.Position = 0;

            var actualHeader = await obj.ReadAsync(ms);

            Assert.AreEqual(expectedHeader, actualHeader);
        }

        [TestMethod]
        public async Task Write_Data_EqualsTrue()
        {
            MS2FileHeader expectedHeader = new MS2FileHeader(1, 2, 3, 4, 5, CompressionType.Zlib);

            var obj = new MS2FileHeaderMS2F();
            using var ms = new MemoryStream();
            await obj.WriteAsync(ms, expectedHeader);
            ms.Position = 0;

            using var br = new BinaryReader(ms);
            uint unk = br.ReadUInt32();
            uint id = br.ReadUInt32();
            var compressionType = (CompressionType)br.ReadInt64();
            long offset = br.ReadInt64();
            long encodedSize = br.ReadInt64();
            long compressedSize = br.ReadInt64();
            long size = br.ReadInt64();

            var actualUnk = unk;
            var actualHeader = new MS2FileHeader(encodedSize, compressedSize, size, id, offset, compressionType);

            Assert.AreEqual(0u, actualUnk);
            Assert.AreEqual(expectedHeader, actualHeader);
        }

        [TestMethod]
        public async Task Write_FromRead_EqualsTrue()
        {
            byte[] bytes = Enumerable.Range(0, 100).SelectMany(i => BitConverter.GetBytes(i)).ToArray();
            using var ms = new MemoryStream(bytes);

            var obj = new MS2FileHeaderMS2F();
            var expectedHeader = await obj.ReadAsync(ms);
            ms.Position = 0;

            await obj.WriteAsync(ms, expectedHeader);
            ms.Position = 0;

            var actualHeader = await obj.ReadAsync(ms);

            Assert.AreEqual(expectedHeader, actualHeader);
        }
    }
}
