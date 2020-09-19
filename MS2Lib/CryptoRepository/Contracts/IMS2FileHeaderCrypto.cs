using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2FileHeaderCrypto
    {
        Task<IMS2FileHeader> ReadAsync(Stream stream);
        Task WriteAsync(Stream stream, IMS2FileHeader fileHeader);
    }
}
