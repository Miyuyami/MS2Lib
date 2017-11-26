using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib;

namespace InteropDecryptUnitTest
{
    [TestClass]
    public class EncrpytionUnitTests
    {
        byte[] Key;
        byte[] IV;
        string Encrypted;
        string Decrypted;
        byte[] BytesToEncrypt => Encoding.ASCII.GetBytes(this.Decrypted);

        [TestInitialize]
        public void Initialize()
        {
            this.Key = new byte[]
            {
                0x7E, 0x4A, 0xC5, 0xF2, 0xA2, 0xEC, 0xAD, 0xA8, 0xE5, 0x4A, 0x03, 0x85, 0x51, 0x63, 0x2F, 0xFD,
                0x33, 0x4E, 0x3D, 0xF1, 0x06, 0x3A, 0x42, 0xE5, 0xC5, 0x5B, 0x99, 0x3D, 0x0F, 0xD7, 0xB0, 0xE0,
            };
            this.IV = new byte[]
            {
                0xDA, 0x91, 0x9C, 0x91, 0x6B, 0x95, 0x33, 0xB1, 0xAA, 0x70, 0x66, 0x80, 0xF0, 0x0B, 0xEC, 0x9E,
            };
            this.Encrypted = "AXxeKl8JcbxRhV5tzStKrOtvNPbM";
            this.Decrypted = "1,luapack.o\r\n";
        }

        [TestMethod]
        public void TestEncrypt()
        {
            byte[] bytesEncrypted = Encryption.Encrypt(this.BytesToEncrypt, this.Key, this.IV);
            string encryptedString = Encoding.ASCII.GetString(bytesEncrypted);

            Assert.AreEqual(encryptedString, this.Encrypted);
        }

        [TestMethod]
        public void TestEncryptWithCompress()
        {
            byte[] bytesCompressed = Encryption.Compress(this.BytesToEncrypt);
            byte[] bytesEncrypted = Encryption.EncryptNoCompress(bytesCompressed, this.Key, this.IV);
            string encryptedString = Encoding.ASCII.GetString(bytesEncrypted);

            Assert.AreEqual(encryptedString, this.Encrypted);
        }

        [TestMethod]
        public void TestConsistency()
        {
            byte[] bytesEncrypted1 = Encryption.Encrypt(this.BytesToEncrypt, this.Key, this.IV);
            string encryptedString1 = Encoding.ASCII.GetString(bytesEncrypted1);

            byte[] bytesCompressed = Encryption.Compress(this.BytesToEncrypt);
            byte[] bytesEncrypted2 = Encryption.EncryptNoCompress(bytesCompressed, this.Key, this.IV);
            string encryptedString2 = Encoding.ASCII.GetString(bytesEncrypted2);

            Assert.AreEqual(encryptedString1, encryptedString2);
        }
    }
}
