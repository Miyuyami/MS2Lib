using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2FileHeaderTests
    {
        [TestMethod]
        public void Equals_SameReference_True()
        {
            var expected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = expected;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValues_True()
        {
            var expected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentSize_False()
        {
            var notExpected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(2, 2, 3), 4, 5, CompressionType.Zlib);

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentId_False()
        {
            var notExpected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 5, 5, CompressionType.Zlib);

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentOffset_False()
        {
            var notExpected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 6, CompressionType.Zlib);

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentCompressionType_False()
        {
            var notExpected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Png);

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void InterfaceEquals_SameResultAsConcreteEquals_True()
        {
            var header1 = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);
            var header2 = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            Assert.AreEqual(header1.Equals(header2), ((IMS2FileHeader)header1).Equals(header2));
        }


        [TestMethod]
        public void GetHashCode_SameReference_True()
        {
            var expected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = expected;

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_SameValues_True()
        {
            var expected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_DifferentValues_False()
        {
            var notExpected = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 5, CompressionType.Zlib);

            var actual = new MS2FileHeader(new MS2SizeHeader(1, 2, 3), 4, 6, CompressionType.Zlib);

            Assert.AreNotEqual(notExpected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void Constructor_NullSize_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new MS2FileHeader(null, 0, 0));
        }
    }
}
