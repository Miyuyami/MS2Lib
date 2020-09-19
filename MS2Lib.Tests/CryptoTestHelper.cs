using System;
using System.Text;
using MiscUtils.IO;
using Moq;

namespace MS2Lib.Tests
{
    internal static class CryptoTestHelper
    {
        #region Data
        private static readonly string[] DataMS2F = new string[]
        {
                "ZpJ@KwV&nJb4HPa7*EB!IcW00F$nQEpL%n4Twd5CZz0Fm2KI$z$5lm6^*",
        };
        private static readonly string[] EncryptedDataMS2F = new string[]
        {
                "SRUZ7LQ7ifESK2V5Uws5hHXIlprehy2XSFCIZTaibVhzQlmJiqeeJmo3QgIumiRlgka8k8vQqRw5",
        };
        private static readonly Mock<IMS2SizeHeader>[] SizesMS2F = new Mock<IMS2SizeHeader>[]
        {
                CreateSizeMock(76, 57, 57),
        };

        private static readonly string[] DataMS2FCompressed = new string[]
        {
                "WnXmgC5c26fIFwCD$2nXRnv7yxq*HG^!xDPh3",
        };
        private static readonly string[] EncryptedDataMS2FCompressed = new string[]
        {
                "CYRTjoKbfLZ6UuVerYzchJ0oFeFrnk6nvHgz7/m6N9Zjm+e9IC8S9+8x33KK",
        };
        private static readonly Mock<IMS2SizeHeader>[] SizesMS2FCompressed = new Mock<IMS2SizeHeader>[]
        {
                CreateSizeMock(60, 45, 37),
        };

        private static readonly string[] DataNS2F = new string[]
        {
                "02Aby0tB@3@U@d5h*$la5bSm2@*BlIH5WF^VUW4DwlscPJQQQ%Mw2B@7uzt8MrXaIScz^V^p",
        };
        private static readonly string[] EncryptedDataNS2F = new string[]
        {
                "ZwPaarO9y1ZbfWc+ARPRBYrv3RH5h3hitDDL0mNc6bMGL2P2PXr6RzMvsjJiqCxG+NM/EHM0audV3rtpcKyUNCblVJPJ0DW3",
        };
        private static readonly Mock<IMS2SizeHeader>[] SizesNS2F = new Mock<IMS2SizeHeader>[]
        {
                CreateSizeMock(96, 72, 72),
        };

        private static readonly string[] DataNS2FCompressed = new string[]
        {
                "U9iniEcUcuP&HHZ5q8aV8ING365sEXl3NS4%JJ#m3wFFCz3Oo2#Kug",
        };
        private static readonly string[] EncryptedDataNS2FCompressed = new string[]
        {
                "oalMl51KuN/R5JeR42zw4cIEmZohAVOJIW8X85WoppN0QMSERJ7GzDV7f26pxGR/8bvDMV+AHFCblFDToLE=",
        };
        private static readonly Mock<IMS2SizeHeader>[] SizesNS2FCompressed = new Mock<IMS2SizeHeader>[]
        {
                CreateSizeMock(84, 62, 54),
        };
        #endregion

        private readonly static Random Random = new Random();

        public static CryptoTestBundle GetRandomCryptoDataMS2F()
        {
            int index = Random.Next(0, DataMS2F.Length);

            return new CryptoTestBundle(DataMS2F[index], EncryptedDataMS2F[index], SizesMS2F[index]);
        }

        public static CryptoTestBundle GetRandomCryptoDataMS2FCompressed()
        {
            int index = Random.Next(0, DataMS2FCompressed.Length);

            return new CryptoTestBundle(DataMS2FCompressed[index], EncryptedDataMS2FCompressed[index], SizesMS2FCompressed[index]);
        }

        public static CryptoTestBundle GetRandomCryptoDataNS2F()
        {
            int index = Random.Next(0, DataMS2F.Length);

            return new CryptoTestBundle(DataNS2F[index], EncryptedDataNS2F[index], SizesNS2F[index]);
        }

        public static CryptoTestBundle GetRandomCryptoDataNS2FCompressed()
        {
            int index = Random.Next(0, DataMS2FCompressed.Length);

            return new CryptoTestBundle(DataNS2FCompressed[index], EncryptedDataNS2FCompressed[index], SizesNS2FCompressed[index]);
        }


        public static Mock<IMS2FileHeader> CreateFileHeaderMock(Mock<IMS2SizeHeader> sizeMock, uint id, long offset, CompressionType compressionType)
        {
            var result = new Mock<IMS2FileHeader>(MockBehavior.Strict);

            result.SetupGet(f => f.Id).Returns(id);
            result.SetupGet(f => f.Size).Returns(sizeMock.Object);
            result.SetupGet(f => f.Offset).Returns(offset);
            result.SetupGet(f => f.CompressionType).Returns(compressionType);

            return result;
        }

        public static Mock<IMS2SizeHeader> CreateSizeMock(long encodedSize, long compressedSize, long size)
        {
            var result = new Mock<IMS2SizeHeader>(MockBehavior.Strict);

            result.SetupGet(s => s.EncodedSize).Returns(encodedSize);
            result.SetupGet(s => s.CompressedSize).Returns(compressedSize);
            result.SetupGet(s => s.Size).Returns(size);

            return result;
        }

        public static Mock<IMS2FileInfo> CreateFileInfoMock(string id, string path)
        {
            var result = new Mock<IMS2FileInfo>(MockBehavior.Strict);

            result.SetupGet(f => f.Id).Returns(id);
            result.SetupGet(f => f.Path).Returns(path);
            result.SetupGet(f => f.RootFolderId).Returns(BuildRootFolderId(path));

            return result;
        }

        public static string BuildRootFolderId(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return String.Empty;
            }

            string rootDirectory = PathEx.GetRootDirectory(path);
            if (String.IsNullOrWhiteSpace(rootDirectory))
            {
                return String.Empty;
            }

            var sb = new StringBuilder(rootDirectory.Length * 2);

            for (int i = 0; i < rootDirectory.Length; i++)
            {
                char c = rootDirectory[i];
                if (c == '_')
                {
                    sb.Append(c);
                    continue;
                }

                if (c >= '0' && c <= '9' ||
                    c >= 'A' && c <= 'Z' ||
                    c >= 'a' && c <= 'z')
                {
                    // valid
                    sb.Append((byte)(c - '0'));
                }
                else
                {
                    throw new Exception($"Unrecognised character in root directory [{c}].");
                }
            }

            return sb.ToString();
        }
    }
}
