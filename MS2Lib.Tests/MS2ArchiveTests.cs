using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MS2Lib.Tests
{
    [TestClass]
    public class MS2ArchiveTests
    {
        private static IMS2Archive CreateArchive() => MS2Archive.GetArchiveMS2F();
        private static IMS2Archive CreateArchiveMS2F() => MS2Archive.GetArchiveMS2F();
        private static IMS2Archive CreateArchiveNS2F() => MS2Archive.GetArchiveNS2F();

        private static List<Mock<IMS2File>> AddFileMocksToArchive(IMS2Archive archive, int fileCount)
        {
            var mocks = new List<Mock<IMS2File>>();

            for (int i = 0; i < fileCount; i++)
            {
                var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
                fileMock.SetupGet(f => f.Id).Returns(i);
                fileMock.Setup(f => f.Dispose()).Verifiable();
                archive.Add(fileMock.Object);
                mocks.Add(fileMock);
            }

            return mocks;
        }

        [TestMethod]
        public void Count_EmptyArchive_EqualsExpected()
        {
            int expected = 0;
            var archive = CreateArchive();

            int actual = archive.Count;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Count_AfterAddingFiles_EqualsExpected()
        {
            int expected = 2;
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            var fileMock2 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock2.SetupGet(f => f.Id).Returns(2);
            archive.Add(fileMock1.Object);
            archive.Add(fileMock2.Object);

            int actual = archive.Count;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Add_NewFile_IsTrue()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);

            bool added = archive.Add(fileMock1.Object);

            Assert.IsTrue(added);
        }

        [TestMethod]
        public void Add_AlreadyExistingFile_IsFalse()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            archive.Add(fileMock1.Object);

            bool added = archive.Add(fileMock1.Object);

            Assert.IsFalse(added);
        }

        [TestMethod]
        public void Add_DifferentFileWithSameId_IsFalse()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            var fileMock2 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock2.SetupGet(f => f.Id).Returns(1);
            archive.Add(fileMock1.Object);

            bool added = archive.Add(fileMock2.Object);

            Assert.IsFalse(added);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullObject_ThrowsArgumentNullException()
        {
            var archive = CreateArchive();

            archive.Add(null);
        }

        [TestMethod]
        public void Remove_ExistingFile_IsTrue()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Loose);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            archive.Add(fileMock1.Object);

            bool removed = archive.Remove(1);

            Assert.IsTrue(removed);
        }

        [TestMethod]
        public void Remove_NonExistingFile_IsFalse()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            archive.Add(fileMock1.Object);

            bool removed = archive.Remove(2);

            Assert.IsFalse(removed);
        }

        [TestMethod]
        public void Remove_WithDisposingTrue_DisposeCalledOnce()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            fileMock1.Setup(f => f.Dispose()).Verifiable();
            archive.Add(fileMock1.Object);

            bool removed = archive.Remove(1, true);

            fileMock1.Verify(f => f.Dispose(), Times.Once());
        }

        [TestMethod]
        public void Remove_WithDisposingFalse_DisposeNeverCalled()
        {
            var archive = CreateArchive();
            var fileMock1 = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock1.SetupGet(f => f.Id).Returns(1);
            fileMock1.Setup(f => f.Dispose()).Verifiable();
            archive.Add(fileMock1.Object);

            bool removed = archive.Remove(1, false);

            fileMock1.Verify(f => f.Dispose(), Times.Never());
        }

        [TestMethod]
        public void Clear_EmptyArchiveHasCountZero_IsZero()
        {
            var archive = CreateArchive();

            archive.Clear();

            Assert.AreEqual(0, archive.Count);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(5)]
        public void Clear_ArchiveHasCountZero_IsZero(int filesToAdd)
        {
            var archive = CreateArchive();
            var mocks = AddFileMocksToArchive(archive, filesToAdd);

            archive.Clear();

            Assert.AreEqual(0, archive.Count);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(5)]
        public void Clear_WithDisposingTrue_DisposeCalledOnce(int filesToAdd)
        {
            var archive = CreateArchive();
            var mocks = AddFileMocksToArchive(archive, filesToAdd);

            archive.Clear(true);

            mocks.ForEach(m => m.Verify(f => f.Dispose(), Times.Once()));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(5)]
        public void Clear_WithDisposingFalse_DisposeNeverCalled(int filesToAdd)
        {
            var archive = CreateArchive();
            var mocks = AddFileMocksToArchive(archive, filesToAdd);

            archive.Clear(false);

            mocks.ForEach(m => m.Verify(f => f.Dispose(), Times.Never()));
        }

        [TestMethod]
        public void ContainsKey_EmptyArchive_IsFalse()
        {
            var archive = CreateArchive();
            long id = 1;

            bool actual = archive.ContainsKey(id);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ContainsKey_NonExistingId_IsFalse()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.ContainsKey(Int64.MaxValue);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void ContainsKey_ExistingId_IsTrue()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.ContainsKey(id);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TryGetValue_EmptyArchiveReturns_IsFalse()
        {
            var archive = CreateArchive();
            long id = 1;

            bool actual = archive.TryGetValue(id, out IMS2File file);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void TryGetValue_EmptyArchiveOutParam_IsNull()
        {
            var archive = CreateArchive();
            long id = 1;

            bool actual = archive.TryGetValue(id, out IMS2File file);

            Assert.IsNull(file);
        }

        [TestMethod]
        public void TryGetValue_NonExistingIdReturns_IsFalse()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.TryGetValue(Int64.MaxValue, out IMS2File file);

            Assert.IsFalse(actual);
        }

        [TestMethod]
        public void TryGetValue_NonExistingIdOutParam_IsNull()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.TryGetValue(Int64.MaxValue, out IMS2File file);

            Assert.IsNull(file);
        }

        [TestMethod]
        public void TryGetValue_ExistingIdReturns_IsTrue()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.TryGetValue(id, out IMS2File file);

            Assert.IsTrue(actual);
        }

        [TestMethod]
        public void TryGetValue_ExistingIdOutParam_IsNotNull()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.TryGetValue(id, out IMS2File file);

            Assert.IsNotNull(file);
        }

        [TestMethod]
        public void TryGetValue_ExistingIdOutParam_IsSameAsAdded()
        {
            var archive = CreateArchive();
            long id = 1;
            var fileMock = new Mock<IMS2File>(MockBehavior.Strict);
            fileMock.SetupGet(f => f.Id).Returns(id);
            archive.Add(fileMock.Object);

            bool actual = archive.TryGetValue(id, out IMS2File file);

            Assert.IsTrue(ReferenceEquals(fileMock.Object, file));
        }
    }
}
