using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2Archive : IMS2Archive
    {
        public const string HeaderFileExtension = "m2h";
        public const string DataFileExtension = "m2d";

        protected MemoryMappedFile MappedDataFile { get; set; }
        protected IMS2SizeHeader FileInfoHeaderSize { get; set; }
        protected IMS2SizeHeader FileDataHeaderSize { get; set; }
        protected ConcurrentDictionary<long, IMS2File> Files { get; }

        public IMS2ArchiveCryptoRepository CryptoRepository { get; }
        public string Name { get; }
        public int Count => this.Files.Count;
        public ReadOnlyDictionary<long, IMS2File> FileDictionary => new ReadOnlyDictionary<long, IMS2File>(this.Files);
        public IEnumerable<long> Keys => this.Files.Keys;
        public IEnumerable<IMS2File> Values => this.Files.Values;

        public IMS2File this[long key]
        {
            get => this.Files[key];
            set
            {
                if (this.TryGetValue(key, out IMS2File file))
                {
                    file.Dispose();
                    this.Files[key] = value;
                }
            }
        }

        public MS2Archive(IMS2ArchiveCryptoRepository cryptoRepo) :
            this(cryptoRepo, Guid.NewGuid().ToString())
        {

        }

        public MS2Archive(IMS2ArchiveCryptoRepository cryptoRepo, string name)
        {
            this.CryptoRepository = cryptoRepo ?? throw new ArgumentNullException(nameof(cryptoRepo));
            this.Name = name;
            this.Files = new ConcurrentDictionary<long, IMS2File>();
        }

        public bool ContainsKey(long key) => this.Files.ContainsKey(key);
        public bool TryGetValue(long key, out IMS2File value) => this.Files.TryGetValue(key, out value);
        public IEnumerator<IMS2File> GetEnumerator() => this.Values.GetEnumerator();
        public bool Add(IMS2File value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return this.Files.TryAdd(value.Id, value);
        }

        public bool Remove(long key, bool disposeRemoved = true)
        {
            var result = this.Files.Remove(key, out IMS2File file);
            if (disposeRemoved)
            {
                file?.Dispose();
            }

            return result;
        }

        public void Clear(bool disposeRemoved = true)
        {
            if (disposeRemoved)
            {
                foreach (var f in this.Values)
                {
                    f.Dispose();
                }
            }

            this.Files.Clear();
        }

        #region LoadAsync
        public async Task LoadAsync(string headerFilePath, string dataFilePath)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2Archive));
            }

            this.Reset();

            using var headerStream = File.OpenRead(headerFilePath);
            using var dataStream = File.OpenRead(dataFilePath);

            await this.LoadAsync(headerStream, dataStream).ConfigureAwait(false);
        }

        protected async Task LoadAsync(FileStream headerStream, FileStream dataStream)
        {
            this.MappedDataFile = MemoryMappedFile.CreateFromFile(dataStream, this.Name, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, true);

            try
            {
                await this.InternalLoadAsync(headerStream).ConfigureAwait(false);
            }
            catch
            {
                this.Reset();
                throw;
            }
        }

        protected async Task InternalLoadAsync(FileStream headerStream)
        {
            using var br = new BinaryReader(headerStream, Encoding.ASCII, true);

            MS2CryptoMode cryptoMode = (MS2CryptoMode)br.ReadUInt32();
            if (this.CryptoRepository.CryptoMode != cryptoMode)
            {
                throw new BadMS2ArchiveException();
            }

            IMS2ArchiveHeaderCrypto archiveHeaderCrypto = this.CryptoRepository.GetArchiveHeaderCrypto();
            var (header, data, fileCount) = await archiveHeaderCrypto.ReadAsync(headerStream).ConfigureAwait(false);
            this.FileInfoHeaderSize = header;
            this.FileDataHeaderSize = data;

            await this.LoadFilesAsync(headerStream, fileCount).ConfigureAwait(false);
        }

        protected virtual async Task LoadFilesAsync(FileStream headerStream, long fileCount)
        {
            IMS2FileInfoCrypto fileInfoCrypto = this.CryptoRepository.GetFileInfoReaderCrypto();
            IMS2FileHeaderCrypto fileHeaderCrypto = this.CryptoRepository.GetFileHeaderCrypto();

            // TODO: are those always compressed?
            using Stream fileInfoHeaderDecrypted = await this.CryptoRepository.GetDecryptionStreamAsync(headerStream, this.FileInfoHeaderSize, true).ConfigureAwait(false);
            using Stream fileDataHeaderDecrypted = await this.CryptoRepository.GetDecryptionStreamAsync(headerStream, this.FileDataHeaderSize, true).ConfigureAwait(false);

            var reader = new StreamReader(fileInfoHeaderDecrypted);

            for (int i = 0; i < fileCount; i++)
            {
                var fileInfo = await fileInfoCrypto.ReadAsync(reader).ConfigureAwait(false);
                var fileHeader = await fileHeaderCrypto.ReadAsync(fileDataHeaderDecrypted).ConfigureAwait(false);
                var file = new MS2File(this, this.MappedDataFile, fileInfo, fileHeader, true);

                this.Add(file);
            }
        }
        #endregion

        #region SaveAsync
        public async Task SaveAsync(string headerFilePath, string dataFilePath, bool shouldSaveConcurrently)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2Archive));
            }

            using var headerStream = File.OpenWrite(headerFilePath);

            headerStream.SetLength(0L);

            if (shouldSaveConcurrently)
            {
                await this.SaveConcurrentAsync(headerStream, dataFilePath);
            }
            else
            {
                using var dataStream = File.OpenWrite(dataFilePath);
                dataStream.SetLength(0L);
                await this.SaveAsync(headerStream, dataStream);
            }
        }

        protected async Task SaveAsync(FileStream headerStream, FileStream dataStream)
        {
            IMS2ArchiveHeaderCrypto archiveHeaderCrypto = this.CryptoRepository.GetArchiveHeaderCrypto();
            IMS2FileInfoCrypto fileInfoCrypto = this.CryptoRepository.GetFileInfoReaderCrypto();
            IMS2FileHeaderCrypto fileHeaderCrypto = this.CryptoRepository.GetFileHeaderCrypto();

            using var fileInfoMemoryStream = new MemoryStream();
            using var fileHeaderMemoryStream = new MemoryStream();

            long fileCount = this.Files.Count;
            long offset = 0;

            using (var fileInfoWriter = new StreamWriter(fileInfoMemoryStream, Encoding.ASCII, 1 << 10, true))
            {
                foreach (IMS2File file in this.Files.Values)
                {
                    var (fileStream, fileSize) = await file.GetStreamForArchivingAsync().ConfigureAwait(false);

                    await fileStream.CopyToAsync(dataStream).ConfigureAwait(false);

                    await fileInfoCrypto.WriteAsync(fileInfoWriter, file.Info).ConfigureAwait(false);

                    IMS2FileHeader newFileHeader = new MS2FileHeader(fileSize, file.Header.Id, offset, file.Header.CompressionType);
                    await fileHeaderCrypto.WriteAsync(fileHeaderMemoryStream, newFileHeader).ConfigureAwait(false);

                    offset += fileSize.EncodedSize;
                }
            }

            fileInfoMemoryStream.Position = 0;
            fileHeaderMemoryStream.Position = 0;

            // TODO: are those always compressed?
            var (fileInfoEncryptedStream, fileInfoSize) = await this.CryptoRepository.GetEncryptionStreamAsync(fileInfoMemoryStream, fileInfoMemoryStream.Length, true).ConfigureAwait(false);
            var (fileHeaderEncryptedStream, fileHeaderSize) = await this.CryptoRepository.GetEncryptionStreamAsync(fileHeaderMemoryStream, fileHeaderMemoryStream.Length, true).ConfigureAwait(false);

            // write header stream (m2h)
            using var headerWriter = new BinaryWriter(headerStream, Encoding.ASCII, true);
            headerWriter.Write((uint)this.CryptoRepository.CryptoMode);

            await archiveHeaderCrypto.WriteAsync(headerStream, fileInfoSize, fileHeaderSize, fileCount).ConfigureAwait(false);

            using (fileInfoEncryptedStream)
            using (fileHeaderEncryptedStream)
            {
                await fileInfoEncryptedStream.CopyToAsync(headerStream).ConfigureAwait(false);
                await fileHeaderEncryptedStream.CopyToAsync(headerStream).ConfigureAwait(false);
            }
        }

        protected async Task SaveConcurrentAsync(FileStream headerStream, string dataFilePath)
        {
            FileStream dataStream = File.Open(dataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            dataStream.SetLength(0L);

            IMS2ArchiveHeaderCrypto archiveHeaderCrypto = this.CryptoRepository.GetArchiveHeaderCrypto();
            IMS2FileInfoCrypto fileInfoCrypto = this.CryptoRepository.GetFileInfoReaderCrypto();
            IMS2FileHeaderCrypto fileHeaderCrypto = this.CryptoRepository.GetFileHeaderCrypto();

            using var fileHeaderMemoryStream = new MemoryStream();

            IMS2File[] files = this.Files.Values.ToArray();
            long fileCount = files.Length;

            // prepare for writing (encrypt if necessary) load everything in memory
            Task<(Stream stream, IMS2SizeHeader size)>[] archivingTasks = files.Select(async file =>
            {
                await Task.Yield();

                var (stream, size) = await file.GetStreamForArchivingAsync().ConfigureAwait(false);

                if (stream is MemoryStream ms)
                {
                    return ((Stream)ms, size);
                }
                else if (stream is KeepOpenStreamProxy proxy &&
                         proxy.Stream is MemoryStream proxyMs)
                {
                    return (proxy, size);
                }
                else
                {
                    var buffer = new byte[size.EncodedSize];
                    var newMs = new MemoryStream(buffer);
                    await stream.CopyToAsync(newMs).ConfigureAwait(false);
                    stream.Dispose();
                    newMs.Position = 0;

                    return (newMs, size);
                }
            }).ToArray();

            await Task.WhenAll(archivingTasks).ConfigureAwait(false);

            long offset = 0;
            Stream[] streams = new Stream[fileCount];
            IMS2FileHeader[] fileHeaders = new IMS2FileHeader[fileCount];
            using StringWriter fileInfoWriter = new StringWriter();

            // calculate final data size
            // write raw file headers
            for (int i = 0; i < fileCount; i++)
            {
                var (stream, size) = await archivingTasks[i].ConfigureAwait(false);
                IMS2File file = files[i];

                streams[i] = stream;

                await fileInfoCrypto.WriteAsync(fileInfoWriter, file.Info).ConfigureAwait(false);

                IMS2FileHeader newFileHeader = new MS2FileHeader(size, file.Header.Id, offset, file.Header.CompressionType);
                fileHeaders[i] = newFileHeader;

                await fileHeaderCrypto.WriteAsync(fileHeaderMemoryStream, newFileHeader).ConfigureAwait(false);

                offset += size.EncodedSize;
            }

            // write data file
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(dataStream, Guid.NewGuid().ToString(), offset, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                Task[] dataWritingTasks = fileHeaders.Select(async (fileHeader, i) =>
                {
                    await Task.Yield();

                    using Stream stream = streams[i];

                    using var mmfStream = mmf.CreateViewStream(fileHeader.Offset, fileHeader.Size.EncodedSize, MemoryMappedFileAccess.Write);
                    await stream.CopyToAsync(mmfStream).ConfigureAwait(false);
                }).ToArray();

                await Task.WhenAll(dataWritingTasks).ConfigureAwait(false);
            }

            using var fileInfoMemoryStream = new MemoryStream(Encoding.ASCII.GetBytes(fileInfoWriter.ToString()));
            fileHeaderMemoryStream.Position = 0;

            // TODO: are those always compressed?
            var (fileInfoEncryptedStream, fileInfoSize) = await this.CryptoRepository.GetEncryptionStreamAsync(fileInfoMemoryStream, fileInfoMemoryStream.Length, true).ConfigureAwait(false);
            var (fileHeaderEncryptedStream, fileHeaderSize) = await this.CryptoRepository.GetEncryptionStreamAsync(fileHeaderMemoryStream, fileHeaderMemoryStream.Length, true).ConfigureAwait(false);

            // write header stream (m2h)
            using var headerWriter = new BinaryWriter(headerStream, Encoding.ASCII, true);
            headerWriter.Write((uint)this.CryptoRepository.CryptoMode);

            await archiveHeaderCrypto.WriteAsync(headerStream, fileInfoSize, fileHeaderSize, fileCount).ConfigureAwait(false);

            using (fileInfoEncryptedStream)
            using (fileHeaderEncryptedStream)
            {
                await fileInfoEncryptedStream.CopyToAsync(headerStream).ConfigureAwait(false);
                await fileHeaderEncryptedStream.CopyToAsync(headerStream).ConfigureAwait(false);
            }
        }
        #endregion

        protected virtual void Reset()
        {
            if (this.MappedDataFile != null)
            {
                this.MappedDataFile.Dispose();
                this.MappedDataFile = null;
            }

            this.FileInfoHeaderSize = null;
            this.FileDataHeaderSize = null;

            foreach (var kvp in this.Files)
            {
                kvp.Value.Dispose();
            }

            this.Files.Clear();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        protected virtual string DebuggerDisplay
            => $"Files = {this.Files.Count}, Name = {this.MappedDataFile}";

        #region IDisposable interface
        private bool IsDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    // managed
                    this.Reset();
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

        #region Hidden interfaces
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        #endregion

        #region static helpers
        public static IMS2Archive GetArchiveMS2F() => new MS2Archive(Repositories.Repos[MS2CryptoMode.MS2F]);
        public static IMS2Archive GetArchiveNS2F() => new MS2Archive(Repositories.Repos[MS2CryptoMode.NS2F]);

        public static async Task<IMS2Archive> GetAndLoadArchiveAsync(string headerFilePath, string dataFilePath)
        {
            using var headerStream = File.OpenRead(headerFilePath);
            using var dataStream = File.OpenRead(dataFilePath);

            if (headerStream.Length < 4)
            {
                throw new BadMS2ArchiveException("Given file is too small.");
            }

            MS2CryptoMode cryptoMode;
            using (var br = new BinaryReader(headerStream, Encoding.ASCII, true))
            {
                cryptoMode = (MS2CryptoMode)br.ReadInt32();
            }

            if (!Repositories.Repos.ContainsKey(cryptoMode))
            {
                throw new BadMS2ArchiveException("Unknown file format or unable to automatically determine the file format.");
            }

            headerStream.Position = 0;
            var archive = new MS2Archive(Repositories.Repos[cryptoMode]);
            await archive.LoadAsync(headerStream, dataStream);

            return archive;
        }
        #endregion
    }
}
