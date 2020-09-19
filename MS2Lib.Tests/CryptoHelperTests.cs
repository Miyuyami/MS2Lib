using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static MS2Lib.Tests.CryptoTestHelper;

namespace MS2Lib.Tests
{
    [TestClass]
    public class CryptoHelperTests
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        #region GetDecryptionStreamAsync
        [TestMethod]
        public async Task GetDecryptionStreamAsync_MS2F_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataMS2F();
            string input = bundle.EncryptedData;
            string expected = bundle.Data;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(76, 57, 57);

            var actualStream = await CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.MS2F.Key, Cryptography.MS2F.IV, false);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetDecryptionStreamAsync_MS2FWithCompress_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataMS2FCompressed();
            string input = bundle.EncryptedData;
            string expected = bundle.Data;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(60, 45, 37);

            var actualStream = await CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.MS2F.Key, Cryptography.MS2F.IV, true);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetDecryptionStreamAsync_NS2F_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataNS2F();
            string input = bundle.EncryptedData;
            string expected = bundle.Data;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(96, 72, 72);

            var actualStream = await CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.NS2F.Key, Cryptography.NS2F.IV, false);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetDecryptionStreamAsync_NS2FWithCompress_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataNS2FCompressed();
            string input = bundle.EncryptedData;
            string expected = bundle.Data;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(84, 62, 54);

            var actualStream = await CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.NS2F.Key, Cryptography.NS2F.IV, true);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetDecryptionStreamAsync_CompressedSizeDifferentThanDecodedSize_ThrowsArgumentException()
        {
            var bundle = GetRandomCryptoDataNS2FCompressed();
            string input = bundle.EncryptedData;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(84, 61, 54);

            await Assert.ThrowsExceptionAsync<ArgumentException>(() => CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.NS2F.Key, Cryptography.NS2F.IV, true));
        }

        [TestMethod]
        public async Task GetDecryptionStreamAsync_SizeHigherThanDecryptedSize_ThrowsArgumentException()
        {
            var bundle = GetRandomCryptoDataNS2FCompressed();
            string input = bundle.EncryptedData;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(84, 62, 55);

            await Assert.ThrowsExceptionAsync<ArgumentException>(() => CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.NS2F.Key, Cryptography.NS2F.IV, true));
        }

        [TestMethod]
        public async Task GetDecryptionStreamAsync_UndecodableData_ThrowsIOExceptionWithBase64Text()
        {
            string input = "notbase64decodable";
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);
            MS2SizeHeader size = new MS2SizeHeader(84, 61, 54);

            var ex = await Assert.ThrowsExceptionAsync<IOException>(() => CryptoHelper.GetDecryptionStreamAsync(inputStream, size, Cryptography.NS2F.Key, Cryptography.NS2F.IV, true));
            Assert.IsTrue(ex.Message.Contains("base64"));
        }
        #endregion

        #region GetEncryptionStreamAsync
        [TestMethod]
        public async Task GetEncryptionStreamAsync_MS2F_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataMS2F();
            string input = bundle.Data;
            string expected = bundle.EncryptedData;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);

            var (actualStream, _) = await CryptoHelper.GetEncryptionStreamAsync(inputStream, inputStream.Length, Cryptography.MS2F.Key, Cryptography.MS2F.IV, false);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetEncryptionStreamAsync_MS2FWithCompress_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataMS2FCompressed();
            string input = bundle.Data;
            string expected = bundle.EncryptedData;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);

            var (actualStream, _) = await CryptoHelper.GetEncryptionStreamAsync(inputStream, inputStream.Length, Cryptography.MS2F.Key, Cryptography.MS2F.IV, true);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetEncryptionStreamAsync_NS2F_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataNS2F();
            string input = bundle.Data;
            string expected = bundle.EncryptedData;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);

            var (actualStream, _) = await CryptoHelper.GetEncryptionStreamAsync(inputStream, inputStream.Length, Cryptography.NS2F.Key, Cryptography.NS2F.IV, false);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetEncryptionStreamAsync_NS2FWithCompress_EqualsExpected()
        {
            var bundle = GetRandomCryptoDataNS2FCompressed();
            string input = bundle.Data;
            string expected = bundle.EncryptedData;
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);

            var (actualStream, _) = await CryptoHelper.GetEncryptionStreamAsync(inputStream, inputStream.Length, Cryptography.NS2F.Key, Cryptography.NS2F.IV, true);
            byte[] actualBytes = actualStream.ToArray();
            string actual = Encoding.GetString(actualBytes);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public async Task GetEncryptionStreamAsync_InputSizeHigherThanStreamSizeWithCompression_EqualsExpected()
        {
            string input = "test";
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);

            await Assert.ThrowsExceptionAsync<EndOfStreamException>(() => CryptoHelper.GetEncryptionStreamAsync(inputStream, inputStream.Length + 1, Cryptography.NS2F.Key, Cryptography.NS2F.IV, true));
        }

        [TestMethod]
        public async Task GetEncryptionStreamAsync_InputSizeHigherThanStreamSizeNoCompression_EqualsExpected()
        {
            string input = "test";
            byte[] inputBytes = Encoding.GetBytes(input);
            using MemoryStream inputStream = new MemoryStream(inputBytes);

            await Assert.ThrowsExceptionAsync<EndOfStreamException>(() => CryptoHelper.GetEncryptionStreamAsync(inputStream, inputStream.Length + 1, Cryptography.NS2F.Key, Cryptography.NS2F.IV, false));
        }
        #endregion
    }
}
