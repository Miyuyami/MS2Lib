using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2Archive : IDisposable, IEnumerable<IMS2File>
    {
        IMS2ArchiveCryptoRepository CryptoRepository { get; }
        string Name { get; }
        int Count { get; }

        Task LoadAsync(string headerFilePath, string dataFilePath);
        Task LoadAsync(Stream headerStream, Stream dataStream);

        Task SaveAsync(string headerFilePath, string dataFilePath);
        Task SaveAsync(Stream headerStream, Stream dataStream);

        bool ContainsKey(long key);
        bool TryGetValue(long key, out IMS2File value);

        bool Add(IMS2File value);
        bool Remove(long key, bool disposeRemoved = true);
        void Clear(bool disposeRemoved = true);
    }
}
