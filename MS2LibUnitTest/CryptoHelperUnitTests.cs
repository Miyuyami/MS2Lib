using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib;

namespace MS2LibUnitTest
{
    [TestClass]
    public class CryptoHelperUnitTests
    {
        string Encrypted;
        byte[] EncryptedBytes => Encoding.ASCII.GetBytes(this.Encrypted);
        string Decrypted;
        byte[] DecryptedBytes => Encoding.ASCII.GetBytes(this.Decrypted);

        [TestInitialize]
        public void Initialize()
        {
            this.Encrypted = "AXxeKl8JcbxRhV5tzStKrOtvNPbM";
            this.Decrypted = "1,luapack.o\r\n";
        }

        [TestMethod]
        public async Task TestEncrypt()
        {
            var expectedHeader = new MS2SizeHeader((uint)this.EncryptedBytes.Length, 21u, 13u);
            {
                (byte[] bytes, MS2SizeHeader header) = await CryptoHelper.EncryptDataToDataAsync(MS2CryptoMode.MS2F, true, this.DecryptedBytes).ConfigureAwait(false);

                Assert.AreEqual(header, expectedHeader);
                CollectionAssert.AreEqual(this.EncryptedBytes, bytes);
            }

            {
                (byte[] bytes, MS2SizeHeader header) = await CryptoHelper.EncryptStreamToDataAsync(MS2CryptoMode.MS2F, true, new MemoryStream(this.DecryptedBytes), (uint)this.DecryptedBytes.Length).ConfigureAwait(false);

                Assert.AreEqual(header, expectedHeader);
                CollectionAssert.AreEqual(this.EncryptedBytes, bytes);
            }

            {
                (Stream stream, MS2SizeHeader header) = await CryptoHelper.EncryptStreamToStreamAsync(MS2CryptoMode.MS2F, true, new MemoryStream(this.DecryptedBytes), (uint)this.DecryptedBytes.Length).ConfigureAwait(false);

                Assert.AreEqual(header, expectedHeader);
                byte[] bytes = new byte[header.EncodedSize];
                uint bytesRead = (uint)await stream.ReadAsync(bytes, 0, (int)header.EncodedSize).ConfigureAwait(false);
                Assert.AreEqual(bytesRead, header.EncodedSize);
                CollectionAssert.AreEqual(this.EncryptedBytes, bytes);
            }
        }

        [TestMethod]
        public async Task TestDecrypt()
        {
            var header = new MS2SizeHeader((uint)this.EncryptedBytes.Length, 21u, 13u);
            {
                byte[] bytes = await CryptoHelper.DecryptDataToDataAsync(MS2CryptoMode.MS2F, header, true, this.EncryptedBytes).ConfigureAwait(false);

                CollectionAssert.AreEqual(this.DecryptedBytes, bytes);
            }

            {
                byte[] bytes = await CryptoHelper.DecryptStreamToDataAsync(MS2CryptoMode.MS2F, header, true, new MemoryStream(this.EncryptedBytes)).ConfigureAwait(false);

                CollectionAssert.AreEqual(this.DecryptedBytes, bytes);
            }

            {
                Stream stream = await CryptoHelper.DecryptStreamToStreamAsync(MS2CryptoMode.MS2F, header, true, new MemoryStream(this.EncryptedBytes)).ConfigureAwait(false);

                byte[] bytes = new byte[header.Size];
                uint bytesRead = (uint)await stream.ReadAsync(bytes, 0, (int)header.Size).ConfigureAwait(false);
                Assert.AreEqual(bytesRead, header.Size);
                CollectionAssert.AreEqual(this.DecryptedBytes, bytes);
            }
        }

        [TestMethod]
        public async Task TestConsistency()
        {
            string message = "very encrypted";
            MS2CryptoMode cryptoMode = MS2CryptoMode.MS2F;
            Encoding encoding = Encoding.ASCII;
            byte[] bytesToEncrypt = encoding.GetBytes(message);

            {
                bool compress = true;

                (byte[] encryptedBytes, MS2SizeHeader header) = await CryptoHelper.EncryptDataToDataAsync(cryptoMode, compress, bytesToEncrypt).ConfigureAwait(false);
                byte[] decryptedBytes = await CryptoHelper.DecryptDataToDataAsync(cryptoMode, header, compress, encryptedBytes).ConfigureAwait(false);

                CollectionAssert.AreEqual(bytesToEncrypt, decryptedBytes);
                string decryptedMessage = encoding.GetString(decryptedBytes);
                Assert.AreEqual(message, decryptedMessage);
            }

            {
                bool compress = false;

                (byte[] encryptedBytes, MS2SizeHeader header) = await CryptoHelper.EncryptDataToDataAsync(cryptoMode, compress, bytesToEncrypt).ConfigureAwait(false);
                byte[] decryptedBytes = await CryptoHelper.DecryptDataToDataAsync(cryptoMode, header, compress, encryptedBytes).ConfigureAwait(false);

                CollectionAssert.AreEqual(bytesToEncrypt, decryptedBytes);
                string decryptedMessage = encoding.GetString(decryptedBytes);
                Assert.AreEqual(message, decryptedMessage);
            }
        }
    }
}
