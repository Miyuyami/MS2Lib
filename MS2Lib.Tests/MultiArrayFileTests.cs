using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS2Lib.Tests
{
    [TestClass]
    [DeploymentItem("TestData")]
    public class MultiArrayFileTests
    {
        [TestMethod]
        public void Indexer_IndexSmallerThanSize_ExpectedResult()
        {
            var maf = new MultiArrayFile("fileArray.bin", 16, 16);
            var expected = new byte[] { 0x73, 0x9A, 0x8C, 0xA1, 0x16, 0x52, 0x42, 0xE1, 0xBA, 0x2F, 0xD0, 0xC9, 0x0E, 0x1B, 0xC6, 0x94 };

            var actual = maf[12];

            CollectionAssert.AreEquivalent(expected, actual);
        }

        [TestMethod]
        public void Indexer_IndexHigherThanSize_ExpectedResult()
        {
            var maf = new MultiArrayFile("fileArray.bin", 16, 16);
            var expected = new byte[] { 0xDE, 0x04, 0x58, 0xEB, 0xE4, 0x3A, 0xD7, 0xC6, 0x63, 0xDB, 0xF9, 0x4F, 0xEB, 0x87, 0x2D, 0xA2 };

            var actual = maf[20];

            CollectionAssert.AreEquivalent(expected, actual);
        }

        [TestMethod]
        public void Indexer_ArraySizeMatchesGivenSize_ExpectedResult()
        {
            var expected = 6;
            var maf = new MultiArrayFile("fileArray.bin", 10, expected);

            var actual = maf[20].Length;

            Assert.AreEqual(expected, actual);
        }
    }
}
