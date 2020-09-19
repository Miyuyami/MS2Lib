using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MS2Lib
{
    public interface IMS2Archive : IDisposable, IEnumerable<IMS2File>
    {
        ReadOnlyDictionary<long, IMS2File> FileDictionary { get; }
        IMS2ArchiveCryptoRepository CryptoRepository { get; }
        string Name { get; }
        int Count { get; }

        Task LoadAsync(string headerFilePath, string dataFilePath);

        Task SaveAsync(string headerFilePath, string dataFilePath, bool shouldSaveConcurrently, Func<IMS2File, CompressionType> fileCompressionTypeFunc);

        bool ContainsKey(long key);
        bool TryGetValue(long key, out IMS2File value);

        bool Add(IMS2File value);
        bool Remove(long key, bool disposeRemoved = true);
        void Clear(bool disposeRemoved = true);
    }
}
