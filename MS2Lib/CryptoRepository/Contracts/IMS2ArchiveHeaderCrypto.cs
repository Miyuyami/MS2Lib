using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2ArchiveHeaderCrypto
    {
        Task<(IMS2SizeHeader fileInfoSize, IMS2SizeHeader fileDataSize, long fileCount)> ReadAsync(Stream stream);
        Task WriteAsync(Stream stream, IMS2SizeHeader fileInfoSize, IMS2SizeHeader fileDataSize, long fileCount);
    }
}
