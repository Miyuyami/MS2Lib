using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2FileInfoCryptoTests
    {
        [TestMethod]
        public async Task Read_String_EqualsTrue()
        {
            MS2FileInfo expectedInfo = new MS2FileInfo("1", "TestFile");
            var obj = new MS2FileInfoCrypto();
            string line = "1,TestFile";
            var sr = new StringReader(line);

            var actualInfo = await obj.ReadAsync(sr);

            Assert.AreEqual(expectedInfo, actualInfo);
        }

        [TestMethod]
        public async Task Read_StringWithRootFolder_EqualsTrue()
        {
            MS2FileInfo expectedInfo = new MS2FileInfo("1", "TestRoot/TestFile");
            var obj = new MS2FileInfoCrypto();
            string line = "1,3653676834636368,TestRoot/TestFile";
            var sr = new StringReader(line);

            var actualInfo = await obj.ReadAsync(sr);

            Assert.AreEqual(expectedInfo, actualInfo);
        }

        [TestMethod]
        public async Task Read_FromWrite_EqualsTrue()
        {
            MS2FileInfo expectedInfo = new MS2FileInfo("1", "TestFile");
            var obj = new MS2FileInfoCrypto();
            var sw = new StringWriter();
            await obj.WriteAsync(sw, expectedInfo);
            var sr = new StringReader(sw.ToString());

            var actualInfo = await obj.ReadAsync(sr);

            Assert.AreEqual(expectedInfo, actualInfo);
        }

        [TestMethod]
        public async Task Read_FromWriteWithRootFolder_EqualsTrue()
        {
            MS2FileInfo expectedInfo = new MS2FileInfo("1", "TestRoot/TestFile");
            var obj = new MS2FileInfoCrypto();
            var sw = new StringWriter();
            await obj.WriteAsync(sw, expectedInfo);
            var sr = new StringReader(sw.ToString());

            var actualInfo = await obj.ReadAsync(sr);

            Assert.AreEqual(expectedInfo, actualInfo);
        }

        [TestMethod]
        public async Task Write_String_EqualsTrue()
        {
            MS2FileInfo expectedInfo = new MS2FileInfo("1", "TestFile");
            string expectedLine = "1,TestFile\r\n";

            var obj = new MS2FileInfoCrypto();
            var sw = new StringWriter();

            await obj.WriteAsync(sw, expectedInfo);

            string actualLine = sw.ToString();

            Assert.AreEqual(expectedLine, actualLine);
        }

        [TestMethod]
        public async Task Write_StringWithRootFolder_EqualsTrue()
        {
            MS2FileInfo expectedInfo = new MS2FileInfo("1", "TestRoot/TestFile");
            string expectedLine = "1,3653676834636368,TestRoot/TestFile\r\n";

            var obj = new MS2FileInfoCrypto();
            var sw = new StringWriter();

            await obj.WriteAsync(sw, expectedInfo);

            string actualLine = sw.ToString();

            Assert.AreEqual(expectedLine, actualLine);
        }

        [TestMethod]
        public async Task Write_FromRead_EqualsTrue()
        {
            var sw = new StringWriter();

            var obj = new MS2FileInfoCrypto();
            string expectedLine = "1,TestFile\r\n";
            var sr = new StringReader(expectedLine);
            var info = await obj.ReadAsync(sr);

            await obj.WriteAsync(sw, info);
            string actualLine = sw.ToString();

            Assert.AreEqual(expectedLine, actualLine);
        }

        [TestMethod]
        public async Task Write_FromReadWithRootFolder_EqualsTrue()
        {
            var sw = new StringWriter();

            var obj = new MS2FileInfoCrypto();
            string expectedLine = "1,3653676834636368,TestRoot/TestFile\r\n";
            var sr = new StringReader(expectedLine);
            var info = await obj.ReadAsync(sr);

            await obj.WriteAsync(sw, info);
            string actualLine = sw.ToString();

            Assert.AreEqual(expectedLine, actualLine);
        }

        [TestMethod]
        public async Task ReadAsync_GivenFourProperties_ThrowsArgumentException()
        {
            var sw = new StringWriter();

            var obj = new MS2FileInfoCrypto();
            string expectedLine = "1,3653676834636368,TestRoot/TestFile,bad\r\n";
            var sr = new StringReader(expectedLine);

            await Assert.ThrowsExceptionAsync<ArgumentException>(() => obj.ReadAsync(sr));
        }
    }
}
