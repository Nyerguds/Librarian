using System;
using System.IO;

namespace Nyerguds.Util
{
    /// <summary>
    /// A wrapper for a stream that allows using it in a StreamWriter in a way that
    /// prevents the stream from being closed when the StreamWriter is disposed. Can be used as:
    /// <i>using (StreamWriter sw = new StreamWriter(new NonDisposingStream(stream), encoding))</i>
    /// </summary>
    /// <author>Maarten Meuris</author>
    public class NonDisposingStream : Stream
    {
        private Stream stream;

        /// <summary>
        /// A wrapper for a stream that allows using it in a StreamWriter in a way that
        /// prevents the stream from being closed when the StreamWriter is disposed. Can be used as:
        /// <i>using (StreamWriter sw = new StreamWriter(new NonDisposingStream(stream), encoding))</i>
        /// </summary>
        /// <param name="stream">The stream to wrap.</param>
        public NonDisposingStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            this.stream = stream;
        }

        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return stream.CanSeek; }
        }

        public override bool CanTimeout
        {
            get { return stream.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return stream.CanWrite; }
        }

        public override long Length
        {
            get { return stream.Length; }
        }

        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return stream.ReadTimeout; }
            set { stream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return stream.WriteTimeout; }
            set { stream.WriteTimeout = value; }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            // do nothing. Do not close the stream.
        }

        //public void Dispose()
        // No need to override this one; it just calls Close()

        protected override void Dispose(bool disposing)
        {
            // do nothing. Do not close the stream.
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return stream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            stream.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return stream.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
        }
    }
}