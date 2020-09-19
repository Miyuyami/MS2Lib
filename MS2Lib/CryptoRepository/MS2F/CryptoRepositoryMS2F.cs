using System.IO;
using System.Threading.Tasks;

namespace MS2Lib.MS2F
{
    public class CryptoRepositoryMS2F : IMS2ArchiveCryptoRepository
    {
        public MS2CryptoMode CryptoMode =>
            MS2CryptoMode.MS2F;

        public IMS2ArchiveHeaderCrypto GetArchiveHeaderCrypto() =>
            new MS2ArchiveHeaderMS2F();

        public IMS2FileHeaderCrypto GetFileHeaderCrypto() =>
            new MS2FileHeaderMS2F();

        public IMS2FileInfoCrypto GetFileInfoReaderCrypto() =>
            new MS2FileInfoCrypto();

        public async Task<Stream> GetDecryptionStreamAsync(Stream input, IMS2SizeHeader size, bool zlibCompressed) =>
            await CryptoHelper.GetDecryptionStreamAsync(input, size, Cryptography.MS2F.Key, Cryptography.MS2F.IV, zlibCompressed).ConfigureAwait(false);

        public async Task<(Stream output, IMS2SizeHeader size)> GetEncryptionStreamAsync(Stream input, long inputSize, bool zlibCompress) =>
            await CryptoHelper.GetEncryptionStreamAsync(input, inputSize, Cryptography.MS2F.Key, Cryptography.MS2F.IV, zlibCompress).ConfigureAwait(false);
    }
}
