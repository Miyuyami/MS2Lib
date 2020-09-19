using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS2Lib.Tests
{
    [TestClass]
    public class StreamProxyTests
    {
        [TestMethod]
        public void Dispose_OuterStreamRead_DoesNotThrowObjectDisposedException()
        {
            Random rand = new Random();
            byte[] bytes = new byte[1024];
            rand.NextBytes(bytes);
            var ms = new MemoryStream(bytes);
            var sp = new StreamProxy(ms);

            sp.Read(new byte[10], 0, 10);
            sp.Dispose();

            ms.Read(new byte[10], 0, 10);
        }

        [TestMethod]
        public void Read_OuterStreamRead_MatchesOuterStream()
        {
            Random rand = new Random();
            byte[] bytes = new byte[1024];
            rand.NextBytes(bytes);
            var ms = new MemoryStream(bytes);
            var sp = new StreamProxy(ms);

            byte[] expectedReadBytes = new byte[10];
            ms.Read(expectedReadBytes, 0, expectedReadBytes.Length);
            ms.Position = 0;
            byte[] actualReadBytes = new byte[10];
            sp.Read(actualReadBytes, 0, actualReadBytes.Length);

            CollectionAssert.AreEqual(expectedReadBytes, actualReadBytes);
        }
    }
}
