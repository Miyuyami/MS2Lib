using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2ArchiveCryptoRepository
    {
        MS2CryptoMode CryptoMode { get; }

        IMS2ArchiveHeaderCrypto GetArchiveHeaderCrypto();
        IMS2FileInfoCrypto GetFileInfoReaderCrypto();
        IMS2FileHeaderCrypto GetFileHeaderCrypto();

        Task<Stream> GetDecryptionStreamAsync(Stream input, IMS2SizeHeader size, bool zlibCompressed);
        Task<(Stream output, IMS2SizeHeader size)> GetEncryptionStreamAsync(Stream input, long inputSize, bool zlibCompress);
    }
}
