using System;
using System.IO;
using System.Threading.Tasks;
using Ionic.Zlib;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;

namespace MS2Lib
{
    public static class CryptoHelper
    {
        #region Decrypt
        public static async Task<MemoryStream> GetDecryptionStreamAsync(Stream input, IMS2SizeHeader size, IMultiArray key, IMultiArray iv, bool zlibCompressed)
        {
            using var ms = new MemoryStream();

            byte[] encodedBytes = new byte[size.EncodedSize];
            await input.ReadAsync(encodedBytes, 0, encodedBytes.Length).ConfigureAwait(false);

            var encoder = new Base64Encoder();
            encoder.Decode(encodedBytes, 0, encodedBytes.Length, ms);
            if (ms.Length != size.CompressedSize)
            {
                throw new ArgumentException("Compressed bytes from input do not match with header size.", nameof(input));
            }

            ms.Position = 0;
            return await InternalGetDecryptionStreamAsync(ms, size, key, iv, zlibCompressed).ConfigureAwait(false);
        }

        private static async Task<MemoryStream> InternalGetDecryptionStreamAsync(Stream input, IMS2SizeHeader size, IMultiArray key, IMultiArray iv, bool zlibCompressed)
        {
            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            var keyParam = ParameterUtilities.CreateKeyParameter("AES", key[(uint)size.CompressedSize]);
            cipher.Init(true, new ParametersWithIV(keyParam, iv[(uint)size.CompressedSize]));

            return await InternalGetDecryptionStreamAsync(input, size, cipher, zlibCompressed).ConfigureAwait(false);
        }

        private static async Task<MemoryStream> InternalGetDecryptionStreamAsync(Stream input, IMS2SizeHeader size, IBufferedCipher cipher, bool zlibCompressed)
        {
            using var cs = new CipherStream(input, cipher, null);
            byte[] bytes = new byte[size.Size];

            int readBytes;
            if (zlibCompressed)
            {
                using var z = new ZlibStream(cs, CompressionMode.Decompress, true);
                readBytes = await z.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
            else
            {
                readBytes = await cs.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            if (readBytes != size.Size)
            {
                throw new ArgumentException("Size bytes from input do not match with header size.", nameof(input));
            }

            return new MemoryStream(bytes);
        }
        #endregion

        #region Encrypt
        public static async Task<(MemoryStream output, IMS2SizeHeader size)> GetEncryptionStreamAsync(Stream input, long inputSize, IMultiArray key, IMultiArray iv, bool zlibCompress)
        {
            if (zlibCompress)
            {
                using var ms = new MemoryStream();
                using (var z = new ZlibStream(ms, CompressionMode.Compress, CompressionLevel.BestCompression, true))
                {
                    byte[] inputBytes = new byte[inputSize];
                    int read = await input.ReadAsync(inputBytes, 0, (int)inputSize).ConfigureAwait(false);
                    if (read != inputSize)
                    {
                        throw new EndOfStreamException();
                    }
                    await z.WriteAsync(inputBytes, 0, (int)inputSize).ConfigureAwait(false);
                }

                ms.Position = 0;
                return await InternalGetEncryptionStreamAsync(ms, ms.Length, key, iv, inputSize).ConfigureAwait(false);
            }

            return await InternalGetEncryptionStreamAsync(input, inputSize, key, iv, inputSize).ConfigureAwait(false);
        }

        private static async Task<(MemoryStream output, IMS2SizeHeader size)> InternalGetEncryptionStreamAsync(Stream input, long inputSize, IMultiArray key, IMultiArray iv, long headerSize)
        {
            var cipher = CipherUtilities.GetCipher("AES/CTR/NoPadding");
            var keyParam = ParameterUtilities.CreateKeyParameter("AES", key[inputSize]);
            cipher.Init(true, new ParametersWithIV(keyParam, iv[inputSize]));

            return await InternalGetEncryptionStreamAsync(input, inputSize, cipher, headerSize).ConfigureAwait(false);
        }

        private static async Task<(MemoryStream output, IMS2SizeHeader size)> InternalGetEncryptionStreamAsync(Stream input, long inputSize, IBufferedCipher cipher, long headerSize)
        {
            byte[] inputBytes = new byte[inputSize];
            int read = await input.ReadAsync(inputBytes, 0, (int)inputSize).ConfigureAwait(false);
            if (inputSize != read)
            {
                throw new EndOfStreamException();
            }
            using var msInput = new MemoryStream(inputBytes);

            using var cs = new CipherStream(msInput, cipher, null);
            using var ms = new MemoryStream();

            var output = new MemoryStream();
            await cs.CopyToAsync(ms).ConfigureAwait(false);
            var data = ms.ToArray();
            var encoder = new Base64Encoder();

            encoder.Encode(data, 0, data.Length, output);

            var header = new MS2SizeHeader(output.Length, inputSize, headerSize);
            output.Position = 0;
            return (output, header);
        }
        #endregion
    }
}
