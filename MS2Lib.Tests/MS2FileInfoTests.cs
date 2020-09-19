using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2FileInfoTests
    {
        [TestMethod]
        public void Equals_SameReference_True()
        {
            var expected = new MS2FileInfo("1", "Path");

            var actual = expected;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameReferenceWithRootFolder_True()
        {
            var expected = new MS2FileInfo("1", "Root/Path");

            var actual = expected;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValues_True()
        {
            var expected = new MS2FileInfo("1", "Path");

            var actual = new MS2FileInfo("1", "Path");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesWithRootFolder_True()
        {
            var expected = new MS2FileInfo("1", "Root/Path");

            var actual = new MS2FileInfo("1", "Root/Path");

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentId_False()
        {
            var notExpected = new MS2FileInfo("1", "Path");

            var actual = new MS2FileInfo("11", "Path");

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void Equals_SameValuesDifferentPath_False()
        {
            var notExpected = new MS2FileInfo("1", "Path");

            var actual = new MS2FileInfo("1", "Pat");

            Assert.AreNotEqual(notExpected, actual);
        }

        [TestMethod]
        public void InterfaceEquals_SameResultAsConcreteEquals_True()
        {
            var info1 = new MS2FileInfo("1", "Root/Path");
            var info2 = new MS2FileInfo("1", "Root/Path");

            Assert.AreEqual(info1.Equals(info2), ((IMS2FileInfo)info1).Equals(info2));
        }


        [TestMethod]
        public void GetHashCode_SameReference_True()
        {
            var expected = new MS2FileInfo("1", "Path");

            var actual = expected;

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_SameReferenceWithRootFolder_True()
        {
            var expected = new MS2FileInfo("1", "Root/Path");

            var actual = expected;

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_SameValues_True()
        {
            var expected = new MS2FileInfo("1", "Path");

            var actual = new MS2FileInfo("1", "Path");

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_SameValuesWithRootFolder_True()
        {
            var expected = new MS2FileInfo("1", "Root/Path");

            var actual = new MS2FileInfo("1", "Root/Path");

            Assert.AreEqual(expected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_DifferentValues_False()
        {
            var notExpected = new MS2FileInfo("1", "Path");

            var actual = new MS2FileInfo("1", "Pth");

            Assert.AreNotEqual(notExpected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_DifferentValuesWithRootFolder_False()
        {
            var notExpected = new MS2FileInfo("1", "Root/Path");

            var actual = new MS2FileInfo("", "Root/Path");

            Assert.AreNotEqual(notExpected.GetHashCode(), actual.GetHashCode());
        }

        [TestMethod]
        [DataRow("", "", "empty path")]
        [DataRow("root_folder/path", "66636368_546360525366", "root folder with underscore")]
        [DataRow("0123569/path", "0123569", "root folder with only numbers")]
        [DataRow("abcdefz/path", "49505152535474", "root folder with only non capitalized letters")]
        [DataRow("ABCDEFZ/path", "17181920212242", "root folder with only capitalized letters")]
        [DataRow("AbCDeFg/path", "17501920532255", "root folder with non capitalized and capitalized letters")]
        [DataRow("4Ab3CD2eFg1/path", "417503192025322551", "root folder with non capitalized and capitalized letters and numbers")]
        [DataRow("4Ab_3CD2eFg1/path", "41750_3192025322551", "root folder with non capitalized and capitalized letters and numbers and underscore")]
        [DataRow("root/path1/path2", "66636368", "path with one inner folder")]
        [DataRow("root/path1/path2/path3", "66636368", "path with two inner folders")]
        [DataRow("root/path1/path2/path3/path4", "66636368", "path with three inner folders")]
        public void Constructor_PathMultitests_RootFolderEqualsExpected(string path, string expectedRootFolderId, string description)
        {
            var info = new MS2FileInfo("1", path);
            string actual = info.RootFolderId;

            Assert.AreEqual(expectedRootFolderId, actual, description);
        }

        [TestMethod]
        [DataRow("ro#ot/path", "root folder with special ascii characters")]
        [DataRow("ro@ot/path", "root folder with special ascii characters")]
        [DataRow("ro]ot/path", "root folder with special ascii characters")]
        [DataRow("ro{ot/path", "root folder with special ascii characters")]
        [DataRow("ro\rot/path", "root folder with formatting ascii characters")]
        [DataRow("ro\not/path", "root folder with formatting ascii characters")]
        [DataRow("ãroot/path", "root folder with non-ascii characters")]
        [DataRow("あroot/path", "root folder with non-ascii characters")]
        [DataRow("ro ot/path", "root folder with space")]
        public void Constructor_PathMultitests_ThrowsArgumentException(string path, string description)
        {
            Assert.ThrowsException<ArgumentException>(() => new MS2FileInfo("1", path), description);
        }
    }
}
