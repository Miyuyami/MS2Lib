using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;
using static MS2Lib.CryptoHelper;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2File : IMS2File
    {
        public IMS2Archive Archive { get; }
        public IMS2FileInfo Info { get; }
        public IMS2FileHeader Header { get; }

        protected Stream DataStream { get; }
        protected MemoryMappedFile DataMemoryMappedFile { get; }

        public long Id { get; }
        public string Name => this.Info.Path;
        public bool IsDataEncrypted { get; }

        protected CompressionType CompressionType => this.Header.CompressionType;
        protected bool IsZlibCompressed =>
            this.Header.CompressionType switch
            {
                CompressionType.None => false,
                CompressionType.Usm => false,
                CompressionType.Png => false,
                CompressionType.Zlib => true,
                _ => throw new Exception($"Unrecognised compression type [{this.CompressionType}]."),
            };

        public MS2File(IMS2Archive archive, Stream dataStream, IMS2FileInfo info, IMS2FileHeader header, bool isStreamEncrypted)
        {
            if (!dataStream.CanSeek)
            {
                throw new ArgumentException("The given stream must be seekable.", nameof(dataStream));
            }

            this.Archive = archive ?? throw new ArgumentNullException(nameof(archive));
            this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            this.Info = info ?? throw new ArgumentNullException(nameof(info));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
            this.IsDataEncrypted = isStreamEncrypted;
            this.Id = InternalGetId(info, header);
        }

        public MS2File(IMS2Archive archive, MemoryMappedFile dataMemoryMappedFile, IMS2FileInfo info, IMS2FileHeader header, bool isStreamEncrypted)
        {
            this.Archive = archive ?? throw new ArgumentNullException(nameof(archive));
            this.DataMemoryMappedFile = dataMemoryMappedFile ?? throw new ArgumentNullException(nameof(dataMemoryMappedFile));
            this.Info = info ?? throw new ArgumentNullException(nameof(info));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
            this.IsDataEncrypted = isStreamEncrypted;
            this.Id = InternalGetId(info, header);
        }

        protected virtual Stream GetDataStream()
        {
            if (this.DataStream != null)
            {
                this.DataStream.Position = this.Header.Offset;

                return new KeepOpenStreamProxy(this.DataStream);
            }

            if (this.DataMemoryMappedFile != null)
            {
                return this.DataMemoryMappedFile.CreateViewStream(this.Header.Offset, this.Header.Size.EncodedSize, MemoryMappedFileAccess.Read);
            }

            throw new InvalidOperationException();
        }

        public virtual Task<Stream> GetStreamAsync()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2File));
            }

            var dataStream = this.GetDataStream();
            if (this.IsDataEncrypted)
            {
                return this.Archive.CryptoRepository.GetDecryptionStreamAsync(dataStream, this.Header.Size, this.IsZlibCompressed);
            }

            return Task.FromResult(dataStream);
        }

        public virtual Task<(Stream stream, IMS2SizeHeader size)> GetStreamForArchivingAsync()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2File));
            }

            var dataStream = this.GetDataStream();
            if (this.IsDataEncrypted)
            {
                return Task.FromResult((dataStream, this.Header.Size));
            }

            return this.Archive.CryptoRepository.GetEncryptionStreamAsync(dataStream, this.Header.Size.EncodedSize, this.IsZlibCompressed);
        }

        protected static long InternalGetId(IMS2FileInfo info, IMS2FileHeader header)
        {
            if (Int64.TryParse(info.Id, out long result))
            {
                Debug.Assert(result == header.Id);

                return result;
            }
            else
            {
                return header.Id;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
                    this.DataStream?.Dispose();
                }

                // unmanaged

                this.IsDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
