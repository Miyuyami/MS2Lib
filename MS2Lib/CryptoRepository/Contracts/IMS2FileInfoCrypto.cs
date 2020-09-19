using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2FileInfoCrypto
    {
        Task<IMS2FileInfo> ReadAsync(TextReader textReader);
        Task WriteAsync(TextWriter textWriter, IMS2FileInfo fileInfo);
    }
}
