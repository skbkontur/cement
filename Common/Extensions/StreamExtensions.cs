using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace Common.Extensions;

internal static class StreamExtensions
{
    public static byte[] ReadAllBytes(this Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    public static byte[] ReadAllBytesWithProgress(this Stream source, Action<long> reportProgress)
    {
        using var destination = new MemoryStream();
        using var streamReader = new StreamReader(source, Encoding.UTF8);

        // This value was originally picked to be the largest multiple of 4096 that is still smaller than
        // the large object heap threshold (85K).
        const int readBufferSize = 4096 * 20;
        var readBuffer = ArrayPool<byte>.Shared.Rent(readBufferSize);

        var totalBytesRead = 0L;

        try
        {
            int bytesRead;
            while ((bytesRead = source.Read(readBuffer, 0, readBuffer.Length)) != 0)
            {
                destination.Write(readBuffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                reportProgress(totalBytesRead);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }

        return destination.ToArray();
    }
}
