using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MS2Lib
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class KeepOpenStreamProxy : Stream
    {
        public Stream Stream { get; }

        public override bool CanRead => this.Stream.CanRead;
        public override bool CanSeek => this.Stream.CanSeek;
        public override bool CanWrite => this.Stream.CanWrite;
        public override long Length => this.Stream.Length;
        public override bool CanTimeout => this.Stream.CanTimeout;

        public override long Position
        {
            get => this.Stream.Position;
            set => this.Stream.Position = value;
        }
        public override int ReadTimeout
        {
            get => this.Stream.ReadTimeout;
            set => this.Stream.ReadTimeout = value;
        }
        public override int WriteTimeout
        {
            get => this.Stream.WriteTimeout;
            set => this.Stream.WriteTimeout = value;
        }

        public KeepOpenStreamProxy(Stream stream)
        {
            this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public override void Flush() =>
            this.Stream.Flush();
        public override int Read(byte[] buffer, int offset, int count) =>
            this.Stream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) =>
            this.Stream.Seek(offset, origin);
        public override void SetLength(long value) =>
            this.Stream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) =>
            this.Stream.Write(buffer, offset, count);
        public override object InitializeLifetimeService() =>
            this.Stream.InitializeLifetimeService();
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            this.Stream.BeginRead(buffer, offset, count, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            this.Stream.BeginWrite(buffer, offset, count, callback, state);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            this.Stream.CopyToAsync(destination, bufferSize, cancellationToken);
        public override int EndRead(IAsyncResult asyncResult) =>
            this.Stream.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) =>
            this.Stream.EndWrite(asyncResult);
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            this.Stream.FlushAsync(cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            this.Stream.ReadAsync(buffer, offset, count, cancellationToken);
        public override int ReadByte() =>
            this.Stream.ReadByte();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            this.Stream.WriteAsync(buffer, offset, count, cancellationToken);
        public override void WriteByte(byte value) =>
            this.Stream.WriteByte(value);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            this.Stream.WriteAsync(buffer, cancellationToken);
        public override void Write(ReadOnlySpan<byte> buffer) =>
            this.Stream.Write(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            this.Stream.ReadAsync(buffer, cancellationToken);
        public override int Read(Span<byte> buffer) =>
            this.Stream.Read(buffer);
        public override void CopyTo(Stream destination, int bufferSize) =>
            this.Stream.CopyTo(destination, bufferSize);
    }
}
