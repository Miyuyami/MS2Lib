using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MS2Lib.Tests
{
    internal sealed class FakeCryptoRepository : IMS2ArchiveCryptoRepository
    {
        public MS2CryptoMode CryptoMode { get; }
        public Encoding Encoding { get; }
        public string Decrypted { get; }
        public string Encrypted { get; }
        public IMS2SizeHeader EncryptedSize { get; }

        public FakeCryptoRepository(MS2CryptoMode cryptoMode, Encoding encoding, string decrypted, string encrypted, IMS2SizeHeader encryptedSize)
        {
            this.CryptoMode = cryptoMode;
            this.Encoding = encoding;
            this.Decrypted = decrypted;
            this.Encrypted = encrypted;
            this.EncryptedSize = encryptedSize;
        }

        public IMS2ArchiveHeaderCrypto GetArchiveHeaderCrypto() => new FakeMS2ArchiveHeaderCrypto();
        public IMS2FileHeaderCrypto GetFileHeaderCrypto() => new FakeFileHeaderCrypto();
        public IMS2FileInfoCrypto GetFileInfoReaderCrypto() => new FakeFileInfoCrypto();

        public Task<Stream> GetDecryptionStreamAsync(Stream input, IMS2SizeHeader size, bool zlibCompressed)
        {
            Stream result = new MemoryStream(this.Encoding.GetBytes(this.Decrypted));

            return Task.FromResult(result);
        }

        public Task<(Stream output, IMS2SizeHeader size)> GetEncryptionStreamAsync(Stream input, long inputSize, bool zlibCompress)
        {
            Stream result = new MemoryStream(this.Encoding.GetBytes(this.Encrypted));

            return Task.FromResult((result, this.EncryptedSize));
        }
    }
}
