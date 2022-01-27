using System;
using System.IO;
using System.Linq;
using System.Text;
using net.r_eg.MvsSln.Extensions;

namespace Common
{
    public class ReadLineEventStream: Stream
    {
        private readonly ReadLineEvent readLineEvent;

        public ReadLineEventStream(ReadLineEvent readLineEvent)
        {
            this.readLineEvent = readLineEvent;
        }

        public override void Flush()
        {
            // Flush is ignored
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (readLineEvent == null)
                return;

            var sb = new StringBuilder();
            for (var i = 0; i < count; i++)
                sb.Append((char)buffer[i]);
            var line = sb.ToString();
            // TODO (DonMorozov): не нравится то, что сначала получение строки из буфера, а потом сплит - выглядит неэффективно
            line.Split(new[] {Environment.NewLine}, StringSplitOptions.None).ForEach(x => readLineEvent(x));
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
    }
}