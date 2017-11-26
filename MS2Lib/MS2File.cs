﻿using System;
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

        public string Id => this.InfoHeader.Id;
        public string Name => this.InfoHeader.Name;
        public uint TypeId { get; }
        public bool IsCompressed
        {
            get
            {
                switch (this.TypeId)
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

        private MS2File(MS2FileInfoHeader infoHeader, MS2FileHeader header, MS2CryptoMode archiveCryptoMode, MemoryMappedFile dataFile)
        {
            this.InfoHeader = infoHeader ?? throw new ArgumentNullException(nameof(infoHeader));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
            this.ArchiveCryptoMode = archiveCryptoMode;
            this.DataMemoryMappedFile = dataFile ?? throw new ArgumentNullException(nameof(dataFile));
            this.TypeId = this.Header.TypeId;
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
            this.TypeId = this.Header.TypeId;
            this.IsDataEncrypted = true;
        }

        private MS2File(MS2FileInfoHeader infoHeader, uint typeId, MS2CryptoMode archiveCryptoMode, Stream dataStream)
        {
            if (!dataStream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(dataStream));
            }

            this.InfoHeader = infoHeader ?? throw new ArgumentNullException(nameof(infoHeader));
            this.ArchiveCryptoMode = archiveCryptoMode;
            this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            this.TypeId = typeId;
            this.IsDataEncrypted = false;
        }

        public static MS2File Create(MS2FileInfoHeader infoHeader, uint typeId, MS2CryptoMode archiveCryptoMode, Stream dataStream)
            => new MS2File(infoHeader, typeId, archiveCryptoMode, dataStream);

        internal static async Task<MS2File> Load(MS2CryptoMode cryptoMode, Stream headerStream, Stream dataStream, MemoryMappedFile dataMemoryMappedFile)
        {
            MS2FileInfoHeader fileInfoHeader = await MS2FileInfoHeader.Load(headerStream).ConfigureAwait(false);
            MS2FileHeader fileHeader = await MS2FileHeader.Load(cryptoMode, dataStream).ConfigureAwait(false);

            Debug.Assert(UInt32.Parse(fileInfoHeader.Id) == fileHeader.Id);
            Debug.Assert(UInt32.Parse(fileInfoHeader.TypeId) == fileHeader.TypeId);

            DLogger.Write($"Id={fileInfoHeader.Id}-{fileHeader.Id}, Type={fileInfoHeader.TypeId}-{fileHeader.TypeId}, Name={fileInfoHeader.Name}, Size={FileEx.FormatStorage(fileHeader.EncodedSize)}->{FileEx.FormatStorage(fileHeader.CompressedSize)}->{FileEx.FormatStorage(fileHeader.Size)}");

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
                Stream stream = this.DataMemoryMappedFile.CreateViewStream(this.Header.Offset, this.Header.EncodedSize, MemoryMappedFileAccess.Read);

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
                Stream stream = await DecryptStreamToStreamAsync(this.ArchiveCryptoMode, this.Header, this.IsCompressed, dataStream.Stream).ConfigureAwait(false);

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

        public async Task<(Stream Stream, bool ShouldDispose, MS2SizeHeader header)> GetEncryptedStreamAsync()
        {
            (Stream dataStream, bool shouldDispose) = this.GetDataStream();
            if (this.IsDataEncrypted)
            {
                return (dataStream, shouldDispose, this.Header);
            }

            Debug.Assert(this.Header == null);
            try
            {
                (Stream stream, MS2SizeHeader header) = await EncryptStreamToStreamAsync(this.ArchiveCryptoMode, this.IsCompressed, dataStream, this.Header.Size).ConfigureAwait(false);

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