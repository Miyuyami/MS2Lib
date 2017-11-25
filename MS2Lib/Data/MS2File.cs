using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;
using MiscUtils.IO;
using static MS2Lib.CryptoHelper;
using DLogger = MiscUtils.Logging.DebugLogger;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2File
    {
        private readonly MS2CryptoMode ArchiveDecryptionMode;
        private readonly MemoryMappedFile DataFile;

        public string Name => this.InfoHeader.Name;
        public uint Id => this.Header.Id;
        public uint Size => this.Header.Size;
        public bool IsCompressed
        {
            get
            {
                switch (this.Header.TypeId)
                {
                    case 4278190080u:
                        return false;
                    case 3992977408u:
                        return false;
                    default:
                        return true;
                }
            }
        }
        public MS2FileInfoHeader InfoHeader { get; }
        public MS2FileHeader Header { get; }

        private MS2File(MS2FileInfoHeader infoHeader, MS2FileHeader header, MS2CryptoMode archiveDecryptionMode, MemoryMappedFile dataFile)
        {
            this.InfoHeader = infoHeader;
            this.Header = header;
            this.ArchiveDecryptionMode = archiveDecryptionMode;
            this.DataFile = dataFile;
        }

        public static async Task<MS2File> Create(MS2CryptoMode decryptionMode, Stream headerStream, Stream dataStream, MemoryMappedFile dataMemoryMappedFile)
        {
            MS2FileInfoHeader fileInfoHeader = await MS2FileInfoHeader.Create(headerStream).ConfigureAwait(false);
            MS2FileHeader fileHeader = await MS2FileHeader.Create(decryptionMode, dataStream).ConfigureAwait(false);

            DLogger.Write($"Id={fileInfoHeader.Id}-{fileHeader.Id}, Name={fileInfoHeader.Name}, Size={FileEx.FormatStorage(fileHeader.EncodedSize)}->{FileEx.FormatStorage(fileHeader.CompressedSize)}->{FileEx.FormatStorage(fileHeader.Size)}");
            var file = new MS2File(fileInfoHeader, fileHeader, decryptionMode, dataMemoryMappedFile);

            return file;
        }

        public async Task<Stream> GetStreamAsync()
        {
            MemoryMappedViewStream dataStream = this.DataFile.CreateViewStream(this.Header.Offset, this.Header.EncodedSize, MemoryMappedFileAccess.Read);

            using (dataStream)
            {
                Stream stream = await DecryptStreamToStreamAsync(this.ArchiveDecryptionMode, this.Header, this.IsCompressed, dataStream).ConfigureAwait(false);

                return stream;
            }
        }

        private string DebuggerDisplay
            => $"Name = {this.Name}";
    }
}
