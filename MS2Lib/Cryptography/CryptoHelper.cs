using System;
using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public static class CryptoHelper
    {
        #region Decrypt
        public static Task<Stream> DecryptStreamToStreamAsync(MS2CryptoMode cryptoMode, MS2SizeHeader header, bool isCompressed, Stream stream)
        {
            return DecryptStreamToDataAsync(cryptoMode, header, isCompressed, stream).ContinueWith<Stream>(t => new MemoryStream(t.Result));
        }

        public static async Task<byte[]> DecryptStreamToDataAsync(MS2CryptoMode cryptoMode, MS2SizeHeader header, bool isCompressed, Stream stream)
        {
            if (header.EncodedSize == 0 || header.CompressedSize == 0 || header.Size == 0)
            {
                return new byte[0];
            }

            byte[] buffer = new byte[header.EncodedSize];
            uint readBytes = (uint)await stream.ReadAsync(buffer, 0, (int)header.EncodedSize).ConfigureAwait(false);
            if (readBytes != header.EncodedSize)
            {
                throw new Exception("Data length mismatch when reading data.");
            }

            return await DecryptDataToDataAsync(cryptoMode, header, isCompressed, buffer).ConfigureAwait(false);
        }

        public static async Task<byte[]> DecryptDataToDataAsync(MS2CryptoMode cryptoMode, MS2SizeHeader header, bool isCompressed, byte[] bytes)
        {
            if (header.EncodedSize == 0 || header.CompressedSize == 0 || header.Size == 0)
            {
                return new byte[0];
            }

            if (isCompressed)
            {
                return await Task.Run(() => Decrypt(cryptoMode, bytes, header.CompressedSize, header.Size)).ConfigureAwait(false);
            }
            else
            {
                return await Task.Run(() => DecryptNoDecompress(cryptoMode, bytes, header.Size)).ConfigureAwait(false);
            }
        }

        private static byte[] Decrypt(MS2CryptoMode cryptoMode, byte[] src, uint compressedSize, uint size)
        {
            IMultiArray key;
            IMultiArray iv;

            switch (cryptoMode)
            {
                case MS2CryptoMode.MS2F:
                    key = Cryptography.MS2F.Key;
                    iv = Cryptography.MS2F.IV;
                    break;
                case MS2CryptoMode.NS2F:
                    key = Cryptography.NS2F.Key;
                    iv = Cryptography.NS2F.IV;
                    break;
                default:
                case MS2CryptoMode.OS2F:
                case MS2CryptoMode.PS2F:
                    throw new NotImplementedException();
            }

            return Decryption.Decrypt(src, size, key[compressedSize], iv[compressedSize]);
        }

        private static byte[] DecryptNoDecompress(MS2CryptoMode cryptoMode, byte[] src, uint size)
        {
            IMultiArray key;
            IMultiArray iv;

            switch (cryptoMode)
            {
                case MS2CryptoMode.MS2F:
                    key = Cryptography.MS2F.Key;
                    iv = Cryptography.MS2F.IV;
                    break;
                case MS2CryptoMode.NS2F:
                    key = Cryptography.NS2F.Key;
                    iv = Cryptography.NS2F.IV;
                    break;
                default:
                case MS2CryptoMode.OS2F:
                case MS2CryptoMode.PS2F:
                    throw new NotImplementedException();
            }

            return Decryption.DecryptNoDecompress(src, size, key[size], iv[size]);
        }
        #endregion

        #region Encrypt
        public static Task<(Stream Stream, MS2SizeHeader Header)> EncryptStreamToStreamAsync(MS2CryptoMode cryptoMode, bool compress, Stream stream, uint count)
        {
            return EncryptStreamToDataAsync(cryptoMode, compress, stream, count).ContinueWith<(Stream, MS2SizeHeader)>(t => (new MemoryStream(t.Result.Bytes), t.Result.Header));
        }

        public static async Task<(byte[] Bytes, MS2SizeHeader Header)> EncryptStreamToDataAsync(MS2CryptoMode cryptoMode, bool compress, Stream stream, uint count)
        {
            if (count == 0)
            {
                return (new byte[0], new MS2SizeHeader(0u, 0u, 0u));
            }

            byte[] buffer = new byte[count];
            uint readBytes = (uint)await stream.ReadAsync(buffer, 0, (int)count).ConfigureAwait(false);
            if (readBytes != count)
            {
                throw new Exception("Data length mismatch when reading data.");
            }

            return await EncryptDataToDataAsync(cryptoMode, compress, buffer).ConfigureAwait(false);
        }

        public static async Task<(byte[] Bytes, MS2SizeHeader Header)> EncryptDataToDataAsync(MS2CryptoMode cryptoMode, bool compress, byte[] bytes)
        {
            if (compress)
            {
                byte[] compressedBytes = await Task.Run(() => Compress(bytes)).ConfigureAwait(false);
                byte[] encryptedBytes = await Task.Run(() => EncryptNoCompress(cryptoMode, compressedBytes, (uint)compressedBytes.Length)).ConfigureAwait(false);
                var header = new MS2SizeHeader((uint)encryptedBytes.Length, (uint)compressedBytes.Length, (uint)bytes.Length);

                return (encryptedBytes, header);
            }
            else
            {
                byte[] encryptedBytes = await Task.Run(() => EncryptNoCompress(cryptoMode, bytes, (uint)bytes.Length)).ConfigureAwait(false);
                var header = new MS2SizeHeader((uint)encryptedBytes.Length, (uint)bytes.Length, (uint)bytes.Length);

                return (encryptedBytes, header);
            }
        }

        private static byte[] Compress(byte[] src)
        {
            return Encryption.Compress(src);
        }

        private static byte[] EncryptNoCompress(MS2CryptoMode cryptoMode, byte[] src, uint compressedSize)
        {
            IMultiArray key;
            IMultiArray iv;

            switch (cryptoMode)
            {
                case MS2CryptoMode.MS2F:
                    key = Cryptography.MS2F.Key;
                    iv = Cryptography.MS2F.IV;
                    break;
                case MS2CryptoMode.NS2F:
                    key = Cryptography.NS2F.Key;
                    iv = Cryptography.NS2F.IV;
                    break;
                default:
                case MS2CryptoMode.OS2F:
                case MS2CryptoMode.PS2F:
                    throw new NotImplementedException();
            }

            return Encryption.EncryptNoCompress(src, key[compressedSize], iv[compressedSize]);
        }
        #endregion
    }
}
