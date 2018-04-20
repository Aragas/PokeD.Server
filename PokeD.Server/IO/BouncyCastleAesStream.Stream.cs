using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.IO
{
    internal partial class BouncyCastleAesStream
    {
        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.Length;

        public override long Position { get => Stream.Position; set => Stream.Position = value; }

        public override int WriteTimeout { get => Stream.WriteTimeout; set => Stream.WriteTimeout = value; }

        public override int ReadTimeout { get => Stream.ReadTimeout; set => Stream.ReadTimeout = value; }

        public override bool CanTimeout => Stream.CanTimeout;

        public override void Flush() { Stream.Flush(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = Stream.Read(buffer, offset, count);
            var decrypted = ED.Decrypt(buffer, 0, count);
            Buffer.BlockCopy(decrypted, 0, buffer, offset, decrypted.Length);
            return length;
        }

        public override int ReadByte()
        {
            var buffer = new byte[1];
            return Read(buffer, 0, 1) != 0 ? buffer[0] : -1;

            //var @byte = new byte[1];
            //var length = Read(@byte, 0, 1);
            //return @byte[0];
        }

        public override long Seek(long offset, SeekOrigin origin) { return Stream.Seek(offset, origin); }

        public override void SetLength(long value) { Stream.SetLength(value); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var encrypted = ED.Encrypt(buffer, offset, count);
            Stream.Write(encrypted, 0, encrypted.Length);
        }

        public override void WriteByte(byte value) { Write(new [] { value }, 0, 1); } // TODO: Why is that here?

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return Stream.WriteAsync(buffer, offset, count, cancellationToken); }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return Stream.ReadAsync(buffer, offset, count, cancellationToken); }

        public override Task FlushAsync(CancellationToken cancellationToken) { return Stream.FlushAsync(cancellationToken); }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) { return Stream.CopyToAsync(destination, bufferSize, cancellationToken); }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Stream?.Dispose();
        }
    }
}