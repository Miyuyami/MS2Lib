using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS2Lib;

namespace Testing
{
    class Program
    {
        static void Main(string[] args)
        {
            //Encrypt();
            Decrypt();
        }

        private static void Decrypt()
        {
            byte[] key = new byte[]
                        {
                0x7E, 0x4A, 0xC5, 0xF2, 0xA2, 0xEC, 0xAD, 0xA8, 0xE5, 0x4A, 0x03, 0x85, 0x51, 0x63, 0x2F, 0xFD,
                0x33, 0x4E, 0x3D, 0xF1, 0x06, 0x3A, 0x42, 0xE5, 0xC5, 0x5B, 0x99, 0x3D, 0x0F, 0xD7, 0xB0, 0xE0,
                        };
            byte[] iv = new byte[]
            {
                0xDA, 0x91, 0x9C, 0x91, 0x6B, 0x95, 0x33, 0xB1, 0xAA, 0x70, 0x66, 0x80, 0xF0, 0x0B, 0xEC, 0x9E,
            };
            string encrypted = "AXxeKl8JcbxRhV5tzStKrOtvNPbM";
            byte[] bytesToDecrypt = Encoding.ASCII.GetBytes(encrypted);

            byte[] bytesDecrypted1 = Decryption.Decrypt(bytesToDecrypt, 13u, key, iv);
            string decryptedString1 = Encoding.ASCII.GetString(bytesDecrypted1);

            byte[] bytesCompressed = Decryption.DecryptNoDecompress(bytesToDecrypt, 21u, key, iv);
            byte[] bytesDecrypted2 = Decryption.Decompress(bytesCompressed, 13);
            string decryptedString2 = Encoding.ASCII.GetString(bytesDecrypted2);

            bool areSame = decryptedString1 == decryptedString2;

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Encrypt()
        {
            byte[] key = new byte[]
                        {
                0x7E, 0x4A, 0xC5, 0xF2, 0xA2, 0xEC, 0xAD, 0xA8, 0xE5, 0x4A, 0x03, 0x85, 0x51, 0x63, 0x2F, 0xFD,
                0x33, 0x4E, 0x3D, 0xF1, 0x06, 0x3A, 0x42, 0xE5, 0xC5, 0x5B, 0x99, 0x3D, 0x0F, 0xD7, 0xB0, 0xE0,
                        };
            byte[] iv = new byte[]
            {
                0xDA, 0x91, 0x9C, 0x91, 0x6B, 0x95, 0x33, 0xB1, 0xAA, 0x70, 0x66, 0x80, 0xF0, 0x0B, 0xEC, 0x9E,
            };
            string stringToEncrypt = "1,luapack.o\r\n";
            byte[] bytesToEncrypt = Encoding.ASCII.GetBytes(stringToEncrypt);

            byte[] bytesEncrypted1 = Encryption.Encrypt(bytesToEncrypt, key, iv);
            string encryptedString1 = Encoding.ASCII.GetString(bytesEncrypted1);

            byte[] bytesCompressed = Encryption.Compress(bytesToEncrypt);
            byte[] bytesEncrypted2 = Encryption.EncryptNoCompress(bytesCompressed, key, iv);
            string encryptedString2 = Encoding.ASCII.GetString(bytesEncrypted2);

            bool areSame = encryptedString1 == encryptedString2;

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
