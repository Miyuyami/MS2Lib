using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2SizeHeaderTests
    {
        [TestMethod]
        public void Equals_SameReference_True()
        {
            var expected = new MS2SizeHeader(1, 2, 3);

            var actual = expected;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValues_True()
        {
            var expected = new MS2SizeHeader(1, 2, 3);

            var actual = new MS2SizeHeader(1, 2, 3);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentEncodedSize_False()
        {
            var notExpected = new MS2SizeHeader(1, 2, 3);

            var actual = new MS2SizeHeader(2, 2, 3);

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentCompressedSize_False()
        {
            var notExpected = new MS2SizeHeader(1, 2, 3);

            var actual = new MS2SizeHeader(1, 3, 3);

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentSize_False()
        {
            var notExpected = new MS2SizeHeader(1, 2, 3);

            var actual = new MS2SizeHeader(1, 2, 4);

            Assert.AreNotEqual(notExpected, actual);
        }


        [TestMethod]
        public void GetHashCode_SameReference_True()
        {
            var expected = new MS2SizeHeader(1, 2, 3);

            var actual = expected;

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_SameValues_True()
        {
            var expected = new MS2SizeHeader(1, 2, 3);

            var actual = new MS2SizeHeader(1, 2, 3);

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_DifferentValues_False()
        {
            var notExpected = new MS2SizeHeader(1, 2, 3);

            var actual = new MS2SizeHeader(1, 2, 4);

            Assert.AreNotEqual(notExpected.GetHashCode(), actual.GetHashCode());
        }
    }
}
