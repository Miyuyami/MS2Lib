using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MS2Lib.MS2F;
using MS2Lib.NS2F;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2ArchiveCryptoRepositoryTests
    {
        [TestMethod]
        public void GetArchiveHeaderCrypto_ReturnsMS2FAssignableFrom_IsTrue()
        {
            var repo = new CryptoRepositoryMS2F();

            Type expectedType = typeof(MS2ArchiveHeaderMS2F);
            Type actualType = repo.GetArchiveHeaderCrypto().GetType();

            Assert.IsTrue(actualType.IsAssignableFrom(expectedType));
        }

        [TestMethod]
        public void GetArchiveHeaderCrypto_ReturnsNS2F_IsTrue()
        {
            var repo = new CryptoRepositoryNS2F();

            Type expectedType = typeof(MS2ArchiveHeaderNS2F);
            Type actualType = repo.GetArchiveHeaderCrypto().GetType();

            Assert.IsTrue(actualType.IsAssignableFrom(expectedType));
        }

        [TestMethod]
        public void GetFileHeaderCrypto_ReturnsMS2FAssignableFrom_IsTrue()
        {
            var repo = new CryptoRepositoryMS2F();

            Type expectedType = typeof(MS2FileHeaderMS2F);
            Type actualType = repo.GetFileHeaderCrypto().GetType();

            Assert.IsTrue(actualType.IsAssignableFrom(expectedType));
        }

        [TestMethod]
        public void GetFileHeaderCrypto_ReturnsNS2F_IsTrue()
        {
            var repo = new CryptoRepositoryNS2F();

            Type expectedType = typeof(MS2FileHeaderNS2F);
            Type actualType = repo.GetFileHeaderCrypto().GetType();

            Assert.IsTrue(actualType.IsAssignableFrom(expectedType));
        }

        [TestMethod]
        public void GetFileInfoReaderCrypto_ReturnsMS2FAssignableFrom_IsTrue()
        {
            var repo = new CryptoRepositoryMS2F();

            Type expectedType = typeof(MS2FileInfoCrypto);
            Type actualType = repo.GetFileInfoReaderCrypto().GetType();

            Assert.IsTrue(actualType.IsAssignableFrom(expectedType));
        }

        [TestMethod]
        public void GetFileInfoReaderCrypto_ReturnsNS2F_IsTrue()
        {
            var repo = new CryptoRepositoryMS2F();

            Type expectedType = typeof(MS2FileInfoCrypto);
            Type actualType = repo.GetFileInfoReaderCrypto().GetType();

            Assert.IsTrue(actualType.IsAssignableFrom(expectedType));
        }
    }
}
