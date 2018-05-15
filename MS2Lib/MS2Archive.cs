using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading.Tasks;
using MiscUtils.IO;
using static MS2Lib.CryptoHelper;
using Logger = MiscUtils.Logging.SimpleLogger;

namespace MS2Lib
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2Archive : IDisposable
    {
        public string Name { get; }
        public MemoryMappedFile DataFile { get; }

        public MS2CryptoMode CryptoMode { get; }

        public MS2SizeHeader Header { get; }
        public MS2SizeHeader Data { get; }

        public List<MS2File> Files { get; }

        private MS2Archive(MS2CryptoMode cryptoMode, MS2SizeHeader header, MS2SizeHeader data, string name, MemoryMappedFile dataFile, List<MS2File> files)
        {
            this.Name = name;
            this.DataFile = dataFile;

            this.CryptoMode = cryptoMode;

            this.Header = header;
            this.Data = data;

            this.Files = files;
        }

        #region load from existing factory
        public static async Task<MS2Archive> Load(string headerFilePath, string dataFilePath)
        {
            using (Stream headerStream = File.OpenRead(headerFilePath))
            using (FileStream dataStream = File.OpenRead(dataFilePath))
            {
                return await Load(headerStream, dataStream).ConfigureAwait(false);
            }
        }

        public static Task<MS2Archive> Load(Stream headerStream, FileStream dataStream)
        {
            string name = Path.GetFileNameWithoutExtension(dataStream.Name);
            var dataMemoryMappedFile = MemoryMappedFile.CreateFromFile(dataStream, Guid.NewGuid().ToString(), 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            return Load(headerStream, name, dataMemoryMappedFile);
        }

        private static async Task<MS2Archive> Load(Stream headerStream, string name, MemoryMappedFile dataMemoryMappedFile)
        {
            using (var br = new BinaryReader(headerStream, Encoding.ASCII, true))
            {
                var cryptoMode = (MS2CryptoMode)br.ReadUInt32();

                switch (cryptoMode)
                {
                    case MS2CryptoMode.MS2F:
                        return await LoadMS2F(cryptoMode, br, name, dataMemoryMappedFile).ConfigureAwait(false);
                    case MS2CryptoMode.NS2F:
                        return await LoadNS2F(cryptoMode, br, name, dataMemoryMappedFile).ConfigureAwait(false);
                    default:
                    case MS2CryptoMode.OS2F:
                    case MS2CryptoMode.PS2F:
                        throw new NotImplementedException();
                }
            }
        }

        private static Task<MS2Archive> LoadMS2F(MS2CryptoMode cryptoMode, BinaryReader br, string name, MemoryMappedFile dataMemoryMappedFile)
        {
            return Task.Run(async () =>
            {
                uint unk = br.ReadUInt32(); // TODO: unknown/unused?
                uint dataCompressedSize = br.ReadUInt32() | br.ReadUInt32();
                uint dataEncodedSize = br.ReadUInt32() | br.ReadUInt32();
                uint size = br.ReadUInt32() | br.ReadUInt32();
                uint compressedSize = br.ReadUInt32() | br.ReadUInt32();
                uint encodedSize = br.ReadUInt32() | br.ReadUInt32();
                uint fileCount = br.ReadUInt32() | br.ReadUInt32();
                uint dataSize = br.ReadUInt32() | br.ReadUInt32();

                if (unk != 0)
                {
                    Logger.Debug($"Archive header unk is \"{unk}\".");
                }

                var header = new MS2SizeHeader(encodedSize, compressedSize, size);
                var data = new MS2SizeHeader(dataEncodedSize, dataCompressedSize, dataSize);
                Logger.Verbose($"There are {fileCount} files in the archive.");
                List<MS2File> files = await LoadFiles(cryptoMode, header, data, fileCount, br, dataMemoryMappedFile).ConfigureAwait(false);

                var archive = new MS2Archive(cryptoMode, header, data, name, dataMemoryMappedFile, files);

                return archive;
            });
        }

        private static Task<MS2Archive> LoadNS2F(MS2CryptoMode cryptoMode, BinaryReader br, string name, MemoryMappedFile dataMemoryMappedFile)
        {
            return Task.Run(async () =>
            {
                uint fileCount = br.ReadUInt32();
                uint dataCompressedSize = br.ReadUInt32() | br.ReadUInt32();
                uint dataEncodedSize = br.ReadUInt32() | br.ReadUInt32();
                uint size = br.ReadUInt32() | br.ReadUInt32();
                uint compressedSize = br.ReadUInt32() | br.ReadUInt32();
                uint encodedSize = br.ReadUInt32() | br.ReadUInt32();
                uint dataSize = br.ReadUInt32() | br.ReadUInt32();

                var header = new MS2SizeHeader(encodedSize, compressedSize, size);
                var data = new MS2SizeHeader(dataEncodedSize, dataCompressedSize, dataSize);
                List<MS2File> files = await LoadFiles(cryptoMode, header, data, fileCount, br, dataMemoryMappedFile).ConfigureAwait(false);

                var archive = new MS2Archive(cryptoMode, header, data, name, dataMemoryMappedFile, files);

                return archive;
            });
        }

        private static async Task<List<MS2File>> LoadFiles(MS2CryptoMode cryptoMode, MS2SizeHeader header, MS2SizeHeader data, uint fileCount, BinaryReader br, MemoryMappedFile dataMemoryMappedFile)
        {
            var files = new List<MS2File>((int)fileCount);

            // TODO: are those always compressed?
            using (Stream headerStream = await DecryptStreamToStreamAsync(cryptoMode, header, true, br.BaseStream).ConfigureAwait(false))
            using (Stream dataStream = await DecryptStreamToStreamAsync(cryptoMode, data, true, br.BaseStream).ConfigureAwait(false))
            {
                for (int i = 0; i < fileCount; i++)
                {
                    files.Add(await MS2File.Load(cryptoMode, headerStream, dataStream, dataMemoryMappedFile).ConfigureAwait(false));
                }

                return files;
            }
        }
        #endregion

        #region saving archive
        public static async Task Save(MS2CryptoMode cryptoMode, MS2File[] files, string headerFilePath, string dataFilePath, RunMode runMode)
        {
            using (Stream headerStream = File.Open(headerFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                switch (runMode)
                {
                    case RunMode.Sync:
                    case RunMode.Async:
                        using (Stream dataStream = File.Open(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            await Save(cryptoMode, files, headerStream, dataStream, runMode).ConfigureAwait(false);
                        }
                        break;
                    case RunMode.Async2:
                        await SaveAsync2(cryptoMode, files, headerStream, dataFilePath).ConfigureAwait(false);
                        break;
                    default:
                        throw new ArgumentException("Given RunMode is not supported for this method.", nameof(runMode));
                }
            }
        }

        /// <summary>
        /// Only supports <see cref="RunMode.Sync"/> and <see cref="RunMode.Async"/>.
        /// </summary>
        /// <param name="cryptoMode"></param>
        /// <param name="files"></param>
        /// <param name="headerStream"></param>
        /// <param name="dataStream"></param>
        /// <param name="runMode"></param>
        /// <returns></returns>
        public static Task Save(MS2CryptoMode cryptoMode, MS2File[] files, Stream headerStream, Stream dataStream, RunMode runMode)
        {
            switch (runMode)
            {
                case RunMode.Sync:
                    return SaveSync(cryptoMode, files, headerStream, dataStream);
                case RunMode.Async:
                    return SaveAsync(cryptoMode, files, headerStream, dataStream);
                default:
                    throw new ArgumentException("Given RunMode is not supported for this method.", nameof(runMode));
            }
        }

        private static async Task SaveSync(MS2CryptoMode cryptoMode, MS2File[] files, Stream headerStream, Stream dataStream)
        {
            MS2SizeHeader header;
            MS2SizeHeader dataHeader;
            Stream encryptedHeaderStream;
            Stream encryptedDataHeaderStream;

            using (var archiveInfoHeaderStream = new MemoryStream())
            using (var archiveHeaderStream = new MemoryStream())
            {
                for (int i = 0; i < files.Length; i++)
                {
                    MS2File file = files[i];
                    uint offset = (uint)dataStream.Position;

                    (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) data = await file.GetEncryptedStreamAsync().ConfigureAwait(false);
                    try
                    {
                        var fileHeader = MS2FileHeader.Create(file.Id, offset, file.CompressionType, data.Header);

                        await file.InfoHeader.Save(archiveInfoHeaderStream).ConfigureAwait(false);
                        await fileHeader.Save(cryptoMode, archiveHeaderStream).ConfigureAwait(false);
                        await data.Stream.CopyToAsync(dataStream).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (data.ShouldDispose)
                        {
                            data.Stream.Dispose();
                        }
                    }
                }

                archiveInfoHeaderStream.Position = 0;
                archiveHeaderStream.Position = 0;
                // TODO: are those always compressed?
                (encryptedHeaderStream, header) = await EncryptStreamToStreamAsync(cryptoMode, true, archiveInfoHeaderStream, (uint)archiveInfoHeaderStream.Length).ConfigureAwait(false);
                (encryptedDataHeaderStream, dataHeader) = await EncryptStreamToStreamAsync(cryptoMode, true, archiveHeaderStream, (uint)archiveHeaderStream.Length).ConfigureAwait(false);
            }

            using (var bwHeader = new BinaryWriter(headerStream, Encoding.ASCII, true))
            {
                switch (cryptoMode)
                {
                    case MS2CryptoMode.MS2F:
                        await SaveMS2F(cryptoMode, (uint)files.Length, header, dataHeader, bwHeader).ConfigureAwait(false);
                        break;
                    case MS2CryptoMode.NS2F:
                        await SaveNS2F(cryptoMode, (uint)files.Length, header, dataHeader, bwHeader).ConfigureAwait(false);
                        break;
                    default:
                    case MS2CryptoMode.OS2F:
                    case MS2CryptoMode.PS2F:
                        throw new NotImplementedException();
                }
            }

            using (encryptedHeaderStream)
            using (encryptedDataHeaderStream)
            {
                await encryptedHeaderStream.CopyToAsync(headerStream).ConfigureAwait(false);
                await encryptedDataHeaderStream.CopyToAsync(headerStream).ConfigureAwait(false);
            }
        }

        private static async Task SaveAsync(MS2CryptoMode cryptoMode, MS2File[] files, Stream headerStream, Stream dataStream)
        {
            MS2SizeHeader header;
            MS2SizeHeader dataHeader;
            Stream encryptedHeaderStream;
            Stream encryptedDataHeaderStream;

            using (var archiveInfoHeaderStream = new MemoryStream())
            using (var archiveHeaderStream = new MemoryStream())
            {
                var tasks = new Task<(MemoryStream ms, MS2SizeHeader header)>[files.Length];

                for (int i = 0; i < files.Length; i++)
                {
                    MS2File file = files[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        CompressionType compressionType = file.CompressionType;

                        (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) data = await file.GetEncryptedStreamAsync().ConfigureAwait(false);
                        try
                        {
                            var ms = new MemoryStream((int)data.Header.EncodedSize);
                            await data.Stream.CopyToAsync(ms).ConfigureAwait(false);
                            return (ms, data.Header);
                        }
                        finally
                        {
                            if (data.ShouldDispose)
                            {
                                data.Stream.Dispose();
                            }
                        }
                    });
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                uint offset = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    MS2File file = files[i];
                    var (ms, sizeHeader) = await tasks[i].ConfigureAwait(false);
                    using (ms)
                    {
                        ms.Position = 0;
                        await ms.CopyToAsync(dataStream).ConfigureAwait(false);
                    }

                    var fileHeader = MS2FileHeader.Create(file.Id, offset, file.CompressionType, sizeHeader);

                    offset += sizeHeader.EncodedSize;

                    await file.InfoHeader.Save(archiveInfoHeaderStream).ConfigureAwait(false);
                    await fileHeader.Save(cryptoMode, archiveHeaderStream).ConfigureAwait(false);
                }

                archiveInfoHeaderStream.Position = 0;
                archiveHeaderStream.Position = 0;
                // TODO: are those always compressed?
                (encryptedHeaderStream, header) = await EncryptStreamToStreamAsync(cryptoMode, true, archiveInfoHeaderStream, (uint)archiveInfoHeaderStream.Length).ConfigureAwait(false);
                (encryptedDataHeaderStream, dataHeader) = await EncryptStreamToStreamAsync(cryptoMode, true, archiveHeaderStream, (uint)archiveHeaderStream.Length).ConfigureAwait(false);

                using (var bwHeader = new BinaryWriter(headerStream, Encoding.ASCII, true))
                {
                    switch (cryptoMode)
                    {
                        case MS2CryptoMode.MS2F:
                            await SaveMS2F(cryptoMode, (uint)files.Length, header, dataHeader, bwHeader).ConfigureAwait(false);
                            break;
                        case MS2CryptoMode.NS2F:
                            await SaveNS2F(cryptoMode, (uint)files.Length, header, dataHeader, bwHeader).ConfigureAwait(false);
                            break;
                        default:
                        case MS2CryptoMode.OS2F:
                        case MS2CryptoMode.PS2F:
                            throw new NotImplementedException();
                    }
                }

                using (encryptedHeaderStream)
                using (encryptedDataHeaderStream)
                {
                    await encryptedHeaderStream.CopyToAsync(headerStream).ConfigureAwait(false);
                    await encryptedDataHeaderStream.CopyToAsync(headerStream).ConfigureAwait(false);
                }
            }
        }

        private static async Task SaveAsync2(MS2CryptoMode cryptoMode, MS2File[] files, Stream headerStream, string dataFilePath)
        {
            MS2SizeHeader header;
            MS2SizeHeader dataHeader;
            Stream encryptedHeaderStream;
            Stream encryptedDataHeaderStream;

            using (var archiveInfoHeaderStream = new MemoryStream())
            using (var archiveHeaderStream = new MemoryStream())
            {
                var tasks = new Task<(MemoryStream ms, MS2SizeHeader header)>[files.Length];

                // load the files into memory streams
                for (int i = 0; i < files.Length; i++)
                {
                    MS2File file = files[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        CompressionType compressionType = file.CompressionType;

                        (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) data = await file.GetEncryptedStreamAsync().ConfigureAwait(false);
                        try
                        {
                            var ms = new MemoryStream((int)data.Header.EncodedSize);
                            await data.Stream.CopyToAsync(ms).ConfigureAwait(false);
                            ms.Position = 0;
                            return (ms, data.Header);
                        }
                        finally
                        {
                            if (data.ShouldDispose)
                            {
                                data.Stream.Dispose();
                            }
                        }
                    });
                }

                // create the header file
                uint offset = 0;
                var fileHeaders = new MS2FileHeader[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    MS2File file = files[i];
                    var (_, sizeHeader) = await tasks[i].ConfigureAwait(false);

                    fileHeaders[i] = MS2FileHeader.Create(file.Id, offset, file.CompressionType, sizeHeader);

                    offset += sizeHeader.EncodedSize;

                    await file.InfoHeader.Save(archiveInfoHeaderStream).ConfigureAwait(false);
                    await fileHeaders[i].Save(cryptoMode, archiveHeaderStream).ConfigureAwait(false);
                }

                // create the data file
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), offset, MemoryMappedFileAccess.ReadWrite))
                {
                    Task[] tasksWrite = new Task[files.Length];

                    for (int i = 0; i < files.Length; i++)
                    {
                        MS2File file = files[i];
                        MS2FileHeader fileHeader = fileHeaders[i];
                        var task = tasks[i];
                        tasksWrite[i] = Task.Run(async () =>
                        {
                            var (ms, _) = await task.ConfigureAwait(false);

                            using (Stream stream = mmf.CreateViewStream(fileHeader.Offset, fileHeader.EncodedSize, MemoryMappedFileAccess.Write))
                            using (ms)
                            {
                                await ms.CopyToAsync(stream).ConfigureAwait(false);
                            }
                        });
                    }

                    await Task.WhenAll(tasksWrite).ConfigureAwait(false);

                    // copy the file to the disk
                    using (var fs = FileEx.OpenWrite(dataFilePath))
                    using (var mmfStream = mmf.CreateViewStream(0L, offset))
                    {
                        await mmfStream.CopyToAsync(fs).ConfigureAwait(false);
                    }
                }

                archiveInfoHeaderStream.Position = 0;
                archiveHeaderStream.Position = 0;
                // TODO: are those always compressed?
                (encryptedHeaderStream, header) = await EncryptStreamToStreamAsync(cryptoMode, true, archiveInfoHeaderStream, (uint)archiveInfoHeaderStream.Length).ConfigureAwait(false);
                (encryptedDataHeaderStream, dataHeader) = await EncryptStreamToStreamAsync(cryptoMode, true, archiveHeaderStream, (uint)archiveHeaderStream.Length).ConfigureAwait(false);

                using (var bwHeader = new BinaryWriter(headerStream, Encoding.ASCII, true))
                {
                    switch (cryptoMode)
                    {
                        case MS2CryptoMode.MS2F:
                            await SaveMS2F(cryptoMode, (uint)files.Length, header, dataHeader, bwHeader).ConfigureAwait(false);
                            break;
                        case MS2CryptoMode.NS2F:
                            await SaveNS2F(cryptoMode, (uint)files.Length, header, dataHeader, bwHeader).ConfigureAwait(false);
                            break;
                        default:
                        case MS2CryptoMode.OS2F:
                        case MS2CryptoMode.PS2F:
                            throw new NotImplementedException();
                    }
                }

                using (encryptedHeaderStream)
                using (encryptedDataHeaderStream)
                {
                    await encryptedHeaderStream.CopyToAsync(headerStream).ConfigureAwait(false);
                    await encryptedDataHeaderStream.CopyToAsync(headerStream).ConfigureAwait(false);
                }
            }
        }

        private static Task SaveMS2F(MS2CryptoMode cryptoMode, uint fileCount, MS2SizeHeader header, MS2SizeHeader dataHeader, BinaryWriter bwHeader)
        {
            return Task.Run(() =>
            {
                // decryption mode
                bwHeader.Write((uint)cryptoMode);
                // unk
                bwHeader.Write(0u);
                // dataCompressedSize
                bwHeader.Write(dataHeader.CompressedSize); bwHeader.Write(0u);
                // dataEncodedSize
                bwHeader.Write(dataHeader.EncodedSize); bwHeader.Write(0u);
                // size
                bwHeader.Write(header.Size); bwHeader.Write(0u);
                // compressedSize
                bwHeader.Write(header.CompressedSize); bwHeader.Write(0u);
                // encodedSize
                bwHeader.Write(header.EncodedSize); bwHeader.Write(0u);
                // fileCount
                bwHeader.Write(fileCount); bwHeader.Write(0u);
                // dataSize
                bwHeader.Write(dataHeader.Size); bwHeader.Write(0u);
            });
        }

        private static Task SaveNS2F(MS2CryptoMode cryptoMode, uint fileCount, MS2SizeHeader header, MS2SizeHeader dataHeader, BinaryWriter bwHeader)
        {
            return Task.Run(() =>
            {
                // decryption mode
                bwHeader.Write((uint)cryptoMode);
                // fileCount
                bwHeader.Write(fileCount);
                // dataCompressedSize
                bwHeader.Write(dataHeader.CompressedSize); bwHeader.Write(0u);
                // dataEncodedSize
                bwHeader.Write(dataHeader.EncodedSize); bwHeader.Write(0u);
                // size
                bwHeader.Write(header.Size); bwHeader.Write(0u);
                // compressedSize
                bwHeader.Write(header.CompressedSize); bwHeader.Write(0u);
                // encodedSize
                bwHeader.Write(header.EncodedSize); bwHeader.Write(0u);
                // dataSize
                bwHeader.Write(dataHeader.Size); bwHeader.Write(0u);
            });
        }
        #endregion

        public Task Save(string headerFilePath, string dataFilePath, RunMode runMode)
            => Save(this.CryptoMode, this.Files.ToArray(), headerFilePath, dataFilePath, runMode);

        public Task Save(MS2CryptoMode newCryptoMode, string headerFilePath, string dataFilePath, RunMode runMode)
            => Save(newCryptoMode, this.Files.ToArray(), headerFilePath, dataFilePath, runMode);

        public Task Save(Stream headerStream, Stream dataStream, RunMode runMode)
            => Save(this.CryptoMode, this.Files.ToArray(), headerStream, dataStream, runMode);

        public Task Save(MS2CryptoMode newCryptoMode, Stream headerStream, Stream dataStream, RunMode runMode)
            => Save(newCryptoMode, this.Files.ToArray(), headerStream, dataStream, runMode);

        private string DebuggerDisplay
            => $"Files = {this.Files.Count}, Name = {this.DataFile}, Mode = {this.CryptoMode}";

        #region IDisposable interface
        private bool IsDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    // managed
                    this.DataFile.Dispose();

                    for (int i = 0; i < this.Files.Count; i++)
                    {
                        this.Files[i].Dispose();
                    }
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
