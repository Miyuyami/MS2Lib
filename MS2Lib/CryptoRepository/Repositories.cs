using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS2Lib.MS2F;
using MS2Lib.NS2F;

namespace MS2Lib
{
    public static class Repositories
    {
        public readonly static ReadOnlyDictionary<MS2CryptoMode, IMS2ArchiveCryptoRepository> Repos =
            new ReadOnlyDictionary<MS2CryptoMode, IMS2ArchiveCryptoRepository>(
                new Dictionary<MS2CryptoMode, IMS2ArchiveCryptoRepository>()
                {
                    { MS2CryptoMode.MS2F, new CryptoRepositoryMS2F() },
                    { MS2CryptoMode.NS2F, new CryptoRepositoryNS2F() },
                });
    }
}
