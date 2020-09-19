using Moq;

namespace MS2Lib.Tests
{
    internal class CryptoTestBundle
    {
        public string Data { get; }
        public string EncryptedData { get; }
        public Mock<IMS2SizeHeader> SizeMock { get; }

        public CryptoTestBundle(string data, string encryptedData, Mock<IMS2SizeHeader> sizeMock)
        {
            this.Data = data;
            this.EncryptedData = encryptedData;
            this.SizeMock = sizeMock;
        }
    }
}
