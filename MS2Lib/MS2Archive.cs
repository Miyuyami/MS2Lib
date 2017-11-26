using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading.Tasks;
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
        public uint FileCount { get; }

        public MS2SizeHeader Header { get; }
        public MS2SizeHeader Data { get; }

        public List<MS2File> Files { get; }

        private MS2Archive(MS2CryptoMode cryptoMode, uint fileCount, MS2SizeHeader header, MS2SizeHeader data, string name, MemoryMappedFile dataFile, List<MS2File> files)
        {
            this.Name = name;
            this.DataFile = dataFile;

            this.CryptoMode = cryptoMode;
            this.FileCount = fileCount;

            this.Header = header;
            this.Data = data;

            this.Files = files;
        }

        #region create from archive factory
        public static async Task<MS2Archive> Load(string headerFilePath, string dataFilePath)
        {
            using (Stream headerStream = File.OpenRead(headerFilePath))
            using (FileStream dataStream = File.OpenRead(dataFilePath))
            {
                return await Load(headerStream, dataStream);
            }
        }

        public static Task<MS2Archive> Load(Stream headerStream, FileStream dataStream)
        {
            string mapName = Path.GetFileNameWithoutExtension(dataStream.Name);
            var dataMemoryMappedFile = MemoryMappedFile.CreateFromFile(dataStream, mapName, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            return Load(headerStream, mapName, dataMemoryMappedFile);
        }

        private static async Task<MS2Archive> Load(Stream headerStream, string mapName, MemoryMappedFile dataMemoryMappedFile)
        {
            using (var br = new BinaryReader(headerStream, Encoding.ASCII, true))
            {
                var cryptoMode = (MS2CryptoMode)br.ReadUInt32();

                switch (cryptoMode)
                {
                    case MS2CryptoMode.MS2F:
                        return await LoadMS2F(cryptoMode, br, mapName, dataMemoryMappedFile).ConfigureAwait(false);
                    case MS2CryptoMode.NS2F:
                        return await LoadNS2F(cryptoMode, br, mapName, dataMemoryMappedFile).ConfigureAwait(false);
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

                var archive = new MS2Archive(cryptoMode, fileCount, header, data, name, dataMemoryMappedFile, files);

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
                uint encodesSize = br.ReadUInt32() | br.ReadUInt32();
                uint dataSize = br.ReadUInt32() | br.ReadUInt32();

                var header = new MS2SizeHeader(encodesSize, compressedSize, size);
                var data = new MS2SizeHeader(dataEncodedSize, dataCompressedSize, dataSize);
                List<MS2File> files = await LoadFiles(cryptoMode, header, data, fileCount, br, dataMemoryMappedFile).ConfigureAwait(false);

                var archive = new MS2Archive(cryptoMode, fileCount, header, data, name, dataMemoryMappedFile, files);

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

        public Task Save(string headerFilePath, string dataFilePath)
            => Save(this.CryptoMode, this.Files, headerFilePath, dataFilePath);

        public Task Save(MS2CryptoMode newCryptoMode, string headerFilePath, string dataFilePath)
            => Save(newCryptoMode, this.Files, headerFilePath, dataFilePath);

        public Task Save(Stream headerStream, Stream dataStream)
            => Save(this.CryptoMode, this.Files, headerStream, dataStream);

        public Task Save(MS2CryptoMode newCryptoMode, Stream headerStream, Stream dataStream)
            => Save(newCryptoMode, this.Files, headerStream, dataStream);

        #region static saving
        public static async Task Save(MS2CryptoMode cryptoMode, List<MS2File> files, string headerFilePath, string dataFilePath)
        {
            using (Stream headerStream = File.OpenWrite(headerFilePath))
            using (Stream dataStream = File.OpenWrite(dataFilePath))
            {
                await Save(cryptoMode, files, headerStream, dataStream);
            }
        }

        public static async Task Save(MS2CryptoMode cryptoMode, List<MS2File> files, Stream headerStream, Stream dataStream)
        {
            MS2SizeHeader header;
            MS2SizeHeader dataHeader;
            Stream encryptedHeaderStream;
            Stream encryptedDataHeaderStream;

            using (var fileInfoHeaderStream = new MemoryStream())
            using (var fileHeaderStream = new MemoryStream())
            {
                for (int i = 0; i < files.Count; i++)
                {
                    uint id = (uint)i + 1u;
                    uint offset = (uint)dataStream.Position;
                    uint typeId = files[i].TypeId;
                    (Stream Stream, bool ShouldDispose, MS2SizeHeader Header) data = await files[i].GetEncryptedStreamAsync().ConfigureAwait(false);
                    try
                    {
                        var fileInfoHeader = MS2FileInfoHeader.Create(id.ToString(), files[i].InfoHeader);
                        var fileHeader = MS2FileHeader.Create(id, offset, typeId, data.Header);

                        await fileInfoHeader.Save(fileInfoHeaderStream).ConfigureAwait(false);
                        await fileHeader.Save(cryptoMode, fileHeaderStream).ConfigureAwait(false);
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

                // TODO: are those always compressed?
                (encryptedHeaderStream, header) = await EncryptStreamToStreamAsync(cryptoMode, true, fileInfoHeaderStream, (uint)fileInfoHeaderStream.Length).ConfigureAwait(false);
                (encryptedDataHeaderStream, dataHeader) = await EncryptStreamToStreamAsync(cryptoMode, true, fileHeaderStream, (uint)fileHeaderStream.Length).ConfigureAwait(false);
            }

            using (var bwHeader = new BinaryWriter(headerStream, Encoding.ASCII, true))
            {
                switch (cryptoMode)
                {
                    case MS2CryptoMode.MS2F:
                        await SaveMS2F(cryptoMode, (uint)files.Count, header, dataHeader, bwHeader).ConfigureAwait(false);
                        break;
                    case MS2CryptoMode.NS2F:
                        await SaveNS2F(cryptoMode, (uint)files.Count, header, dataHeader, bwHeader).ConfigureAwait(false);
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

        private static Task SaveMS2F(MS2CryptoMode cryptoMode, uint fileCount, MS2SizeHeader header, MS2SizeHeader dataHeader, BinaryWriter bwHeader)
        {
            return Task.Run(async () =>
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

        private static async Task SaveNS2F(MS2CryptoMode cryptoMode, uint fileCount, MS2SizeHeader header, MS2SizeHeader dataHeader, BinaryWriter bwHeader)
        {
            throw new NotImplementedException();
        }
        #endregion

        private string DebuggerDisplay
            => $"Files = {this.FileCount}, Name = {this.DataFile}, Mode = {this.CryptoMode}";

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
