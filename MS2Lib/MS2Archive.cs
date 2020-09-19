using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
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
        protected Dictionary<long, IMS2File> Files { get; }

        public IMS2ArchiveCryptoRepository CryptoRepository { get; }
        public string Name { get; }
        public int Count => this.Files.Count;
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
            this.Files = new Dictionary<long, IMS2File>();
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

        public async Task LoadAsync(Stream headerStream, Stream dataStream)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2Archive));
            }

            this.Reset();

            MemoryMappedFile mmf;

            if (dataStream is FileStream fileStream)
            {
                mmf = MemoryMappedFile.CreateFromFile(fileStream, this.Name, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            }
            else if (dataStream.CanSeek) // TODO: if CanSeek is true, does it always mean Length is supported?
            {
                using var mmfTemp = MemoryMappedFile.CreateNew(this.Name, dataStream.Length, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
                mmf = MemoryMappedFile.OpenExisting(this.Name, MemoryMappedFileRights.Read, HandleInheritability.None);

                using var s = mmfTemp.CreateViewStream();
                await dataStream.CopyToAsync(s).ConfigureAwait(false);
            }
            else
            {
                // TODO: is there a way to optimize this?
                using var ms = new MemoryStream();
                await dataStream.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;

                using var mmfTemp = MemoryMappedFile.CreateNew(this.Name, ms.Length, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
                mmf = MemoryMappedFile.OpenExisting(this.Name, MemoryMappedFileRights.Read, HandleInheritability.None);

                using var s = mmfTemp.CreateViewStream();
                await ms.CopyToAsync(s).ConfigureAwait(false);
            }

            this.MappedDataFile = mmf;

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

        protected async Task InternalLoadAsync(Stream headerStream)
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

        protected virtual async Task LoadFilesAsync(Stream headerStream, long fileCount)
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
        public async Task SaveAsync(string headerFilePath, string dataFilePath)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2Archive));
            }

            using var headerStream = File.OpenWrite(headerFilePath);
            using var dataStream = File.OpenWrite(dataFilePath);

            headerStream.SetLength(0L);
            dataStream.SetLength(0L);

            await this.SaveAsync(headerStream, dataStream).ConfigureAwait(false);
        }

        public async Task SaveAsync(Stream headerStream, Stream dataStream)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(MS2Archive));
            }

            IMS2ArchiveHeaderCrypto archiveHeaderCrypto = this.CryptoRepository.GetArchiveHeaderCrypto();
            IMS2FileInfoCrypto fileInfoCrypto = this.CryptoRepository.GetFileInfoReaderCrypto();
            IMS2FileHeaderCrypto fileHeaderCrypto = this.CryptoRepository.GetFileHeaderCrypto();

            using var fileInfoMemoryStream = new MemoryStream();
            using var fileHeaderMemoryStream = new MemoryStream();

            long fileCount = this.Files.Count;
            long offset = 0;

            //var tasks = new Task<(Stream, IMS2SizeHeader)>[fileCount];

            using (var fileInfoWriter = new StreamWriter(fileInfoMemoryStream, Encoding.UTF8, 1 << 10, true))
            {
                foreach (IMS2File file in this.Files.Values)
                {
                    //tasks[i] = file.GetStreamForArchivingAsync();
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
            using var headerWriter = new BinaryWriter(headerStream, Encoding.UTF8, true);
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
