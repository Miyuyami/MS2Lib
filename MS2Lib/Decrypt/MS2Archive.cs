using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading.Tasks;
using static MS2Lib.CryptoHelper;
using Logger = MiscUtils.Logging.SimpleLogger;

namespace MS2Lib.Decrypt
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MS2Archive : IDisposable
    {
        public string Name { get; }
        public MemoryMappedFile DataFile { get; }

        public MS2CryptoMode DecryptionMode { get; }
        public uint FileCount { get; }

        public MS2SizeHeader Header { get; }
        public MS2SizeHeader Data { get; }

        public ReadOnlyCollection<MS2File> Files { get; }

        private MS2Archive(MS2CryptoMode decryptionMode, uint fileCount, MS2SizeHeader header, MS2SizeHeader data, string name, MemoryMappedFile dataFile, ReadOnlyCollection<MS2File> files)
        {
            this.Name = name;
            this.DataFile = dataFile;

            this.DecryptionMode = decryptionMode;
            this.FileCount = fileCount;

            this.Header = header;
            this.Data = data;

            this.Files = files;
        }

        public static Task<MS2Archive> Create(string headerFilePath, string dataFilePath)
        {
            return Create(File.OpenRead(headerFilePath), File.OpenRead(dataFilePath));
        }

        public static Task<MS2Archive> Create(Stream headerStream, FileStream dataStream)
        {
            string mapName = Path.GetFileNameWithoutExtension(dataStream.Name);
            var dataMemoryMappedFile = MemoryMappedFile.CreateFromFile(dataStream, mapName, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            return Create(headerStream, mapName, dataMemoryMappedFile);
        }

        private static async Task<MS2Archive> Create(Stream headerStream, string mapName, MemoryMappedFile dataMemoryMappedFile)
        {
            using (var br = new BinaryReader(headerStream, Encoding.ASCII, false))
            {
                var decryptionMode = (MS2CryptoMode)br.ReadUInt32();

                switch (decryptionMode)
                {
                    case MS2CryptoMode.MS2F:
                        return await CreateMS2F(decryptionMode, br, mapName, dataMemoryMappedFile).ConfigureAwait(false);
                    case MS2CryptoMode.NS2F:
                        return await CreateNS2F(decryptionMode, br, mapName, dataMemoryMappedFile).ConfigureAwait(false);
                    default:
                    case MS2CryptoMode.OS2F:
                    case MS2CryptoMode.PS2F:
                        throw new NotImplementedException();
                }
            }
        }

        private static Task<MS2Archive> CreateMS2F(MS2CryptoMode decryptionMode, BinaryReader br, string name, MemoryMappedFile dataMemoryMappedFile)
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
                ReadOnlyCollection<MS2File> files = await CreateFiles(decryptionMode, header, data, fileCount, br, dataMemoryMappedFile).ConfigureAwait(false);

                var archive = new MS2Archive(decryptionMode, fileCount, header, data, name, dataMemoryMappedFile, files);

                return archive;
            });
        }

        private static Task<MS2Archive> CreateNS2F(MS2CryptoMode decryptionMode, BinaryReader br, string name, MemoryMappedFile dataMemoryMappedFile)
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
                ReadOnlyCollection<MS2File> files = await CreateFiles(decryptionMode, header, data, fileCount, br, dataMemoryMappedFile).ConfigureAwait(false);

                var archive = new MS2Archive(decryptionMode, fileCount, header, data, name, dataMemoryMappedFile, files);

                return archive;
            });
        }

        private static async Task<ReadOnlyCollection<MS2File>> CreateFiles(MS2CryptoMode decryptionMode, MS2SizeHeader header, MS2SizeHeader data, uint fileCount, BinaryReader br, MemoryMappedFile dataMemoryMappedFile)
        {
            MS2File[] files = new MS2File[fileCount];

            // TODO: are those always compressed?
            using (Stream headerStream = await DecryptStreamToStreamAsync(decryptionMode, header, true, br.BaseStream).ConfigureAwait(false))
            using (Stream dataStream = await DecryptStreamToStreamAsync(decryptionMode, data, true, br.BaseStream).ConfigureAwait(false))
            {
                for (int i = 0; i < fileCount; i++)
                {
                    files[i] = await MS2File.Create(decryptionMode, headerStream, dataStream, dataMemoryMappedFile).ConfigureAwait(false);
                }

                return Array.AsReadOnly(files);
            }
        }

        private string DebuggerDisplay
            => $"Files = {this.FileCount}, Name = {this.DataFile}, Mode = {this.DecryptionMode}";

        #region IDisposable interface
        private bool IsDisposed = false; // To detect redundant calls

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
