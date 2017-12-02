using System;
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
    public class MS2File : IDisposable
    {
        private readonly bool IsDataEncrypted;
        private readonly MS2CryptoMode ArchiveCryptoMode;
        private readonly MemoryMappedFile DataMemoryMappedFile;
        private readonly Stream DataStream;

        public uint Id
        {
            get
            {
                if (UInt32.TryParse(this.InfoHeader.Id, out uint result))
                {
                    return result;
                }
                else if (this.Header != null)
                {
                    return this.Header.Id;
                }
                else
                {
                    throw new Exception("The file does not have a valid ID.");
                }
            }
        }
        public string Name => this.InfoHeader.Name;
        public CompressionType CompressionType { get; }
        public bool IsZlibCompressed
        {
            get
            {
                switch (this.CompressionType)
                {
                    case CompressionType.Usm:
                        return false;
                    case CompressionType.Png:
                        return false;
                    case CompressionType.Zlib:
                        return true;
                    default:
                        throw new Exception($"Unrecognised compression type [{this.CompressionType}].");
                }
            }
        }
        public MS2FileInfoHeader InfoHeader { get; internal set; }
        public MS2FileHeader Header { get; internal set; }

        private MS2File(MS2FileInfoHeader infoHeader, MS2FileHeader header, MS2CryptoMode archiveCryptoMode, MemoryMappedFile dataFile)
        {
            this.InfoHeader = infoHeader ?? throw new ArgumentNullException(nameof(infoHeader));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
            this.ArchiveCryptoMode = archiveCryptoMode;
            this.DataMemoryMappedFile = dataFile ?? throw new ArgumentNullException(nameof(dataFile));
            this.CompressionType = this.Header.CompressionType;
            this.IsDataEncrypted = true;
        }

        private MS2File(MS2FileInfoHeader infoHeader, MS2FileHeader header, MS2CryptoMode archiveCryptoMode, Stream dataStream)
        {
            if (!dataStream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(dataStream));
            }

            this.InfoHeader = infoHeader ?? throw new ArgumentNullException(nameof(infoHeader));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
            this.ArchiveCryptoMode = archiveCryptoMode;
            this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            this.CompressionType = this.Header.CompressionType;
            this.IsDataEncrypted = true;
        }

        private MS2File(MS2FileInfoHeader infoHeader, CompressionType compressionType, MS2CryptoMode archiveCryptoMode, Stream dataStream)
        {
            if (!dataStream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(dataStream));
            }

            this.InfoHeader = infoHeader ?? throw new ArgumentNullException(nameof(infoHeader));
            this.ArchiveCryptoMode = archiveCryptoMode;
            this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            this.CompressionType = compressionType;
            this.IsDataEncrypted = false;
        }

        public static MS2File Create(uint id, string pathInArchive, CompressionType compressionType, MS2CryptoMode archiveCryptoMode, string dataFilePath)
            => Create(id, pathInArchive, compressionType, archiveCryptoMode, File.OpenRead(dataFilePath));

        public static MS2File Create(uint id, string pathInArchive, CompressionType compressionType, MS2CryptoMode archiveCryptoMode, Stream dataStream)
            => new MS2File(MS2FileInfoHeader.Create(id.ToString(), pathInArchive), compressionType, archiveCryptoMode, dataStream);

        internal static async Task<MS2File> Load(MS2CryptoMode cryptoMode, Stream headerStream, Stream dataStream, MemoryMappedFile dataMemoryMappedFile)
        {
            MS2FileInfoHeader fileInfoHeader = await MS2FileInfoHeader.Load(headerStream).ConfigureAwait(false);
            MS2FileHeader fileHeader = await MS2FileHeader.Load(cryptoMode, dataStream).ConfigureAwait(false);

            DLogger.Write($"Id={fileInfoHeader.Id}-{fileHeader.Id}, CompressionId={fileHeader.CompressionType}, RootFolder={fileInfoHeader.RootFolderId}, Name={fileInfoHeader.Name}, Size={FileEx.FormatStorage(fileHeader.EncodedSize)}->{FileEx.FormatStorage(fileHeader.CompressedSize)}->{FileEx.FormatStorage(fileHeader.Size)}");

            var file = new MS2File(fileInfoHeader, fileHeader, cryptoMode, dataMemoryMappedFile);

            return file;
        }

        private (Stream Stream, bool ShouldDispose) GetDataStream()
        {
            if (this.DataStream != null)
            {
                this.DataStream.Position = 0;

                return (this.DataStream, false);
            }

            if (this.DataMemoryMappedFile != null)
            {
                uint size = this.IsDataEncrypted ? this.Header.EncodedSize : this.Header.Size;
                if (size == 0)
                {
                    return (new MemoryStream(0), true);
                }

                Stream stream = this.DataMemoryMappedFile.CreateViewStream(this.Header.Offset, size, MemoryMappedFileAccess.Read);

                return (stream, true);
            }

            throw new Exception("Cannot aquire stream");
        }

        public async Task<(Stream Stream, bool ShouldDispose)> GetDecryptedStreamAsync()
        {
            (Stream Stream, bool ShouldDispose) dataStream = this.GetDataStream();
            if (!this.IsDataEncrypted)
            {
                return dataStream;
            }

            Debug.Assert(this.Header != null);
            try
            {
                Stream stream = await DecryptStreamToStreamAsync(this.ArchiveCryptoMode, this.Header, this.IsZlibCompressed, dataStream.Stream).ConfigureAwait(false);

                return (stream, true);
            }
            finally
            {
                if (dataStream.ShouldDispose)
                {
                    dataStream.Stream.Dispose();
                }
            }
        }

        public async Task<(Stream Stream, bool ShouldDispose, MS2SizeHeader Header)> GetEncryptedStreamAsync()
        {
            (Stream dataStream, bool shouldDispose) = this.GetDataStream();
            if (this.IsDataEncrypted)
            {
                return (dataStream, shouldDispose, this.Header);
            }

            Debug.Assert(this.Header == null);
            try
            {
                (Stream stream, MS2SizeHeader header) = await EncryptStreamToStreamAsync(this.ArchiveCryptoMode, this.IsZlibCompressed, dataStream, (uint)dataStream.Length).ConfigureAwait(false);

                return (stream, true, header);
            }
            finally
            {
                if (shouldDispose)
                {
                    dataStream.Dispose();
                }
            }
        }

        private string DebuggerDisplay
            => $"Name = {this.Name}";

        #region IDisposable interface
        private bool IsDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    // managed
                    this.DataMemoryMappedFile?.Dispose();
                    this.DataStream?.Dispose();
                }

                // unmanaged

                this.IsDisposed = true;
            }
        }

        //~MS2Archive()
        //{
        //    this.Dispose(false);
        //}

        public void Dispose()
        {
            this.Dispose(true);

            //GC.SuppressFinalize(this);
        }
        #endregion
    }
}
