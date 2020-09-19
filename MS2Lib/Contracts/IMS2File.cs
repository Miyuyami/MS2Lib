using System;
using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2File : IDisposable
    {
        IMS2Archive Archive { get; }
        IMS2FileInfo Info { get; }
        IMS2FileHeader Header { get; }
        long Id { get; }
        string Name { get; }

        Task<Stream> GetStreamAsync();
        Task<(Stream stream, IMS2SizeHeader size)> GetStreamForArchivingAsync();
    }
}
