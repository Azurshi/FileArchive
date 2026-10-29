using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FastCDC;

public static class FastCDC {
    public static IEnumerable<Memory<byte>> SplitToChunks(
        Stream stream,
        Memory<byte> chunkBuffer,
        int minSize = 1 * 1024,
        int midSize = 16 * 1024,
        int maxSize = 32 * 1024,
        CDCImplementation impl = CDCImplementation.Normalized
    ) {
        ArgumentOutOfRangeException.ThrowIfNegative(minSize);
        ArgumentOutOfRangeException.ThrowIfNegative(midSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSize);
        var method = FastCDCChunking.GetMethod(impl);
        if (chunkBuffer.Length < 2 * maxSize) {
            throw new ArgumentException("Buffer size need to 2x larger than maxSize");
        }
        // Need to esure bufferSize is much larger than maxSize
        int bufferRemaining = 0;
        int bufferOffset = 0;
        bool eof = false;
        while (!(eof && bufferRemaining == 0)) {
            // Fill buffer
            while (!eof && bufferRemaining < maxSize) {
                // Shift remaining
                chunkBuffer.Slice(bufferOffset, bufferRemaining).CopyTo(chunkBuffer);
                // Read new data. This may not fill buffer
                int byteRead = stream.Read(chunkBuffer.Span[bufferRemaining..]);
                bufferRemaining += byteRead;
                bufferOffset = 0;
                if (byteRead == 0) {
                    eof = true;
                }
            }
            // CDC
            // BufferRemaining always more or equal maxSize unless eof
            var chunkSpan = chunkBuffer.Span[bufferOffset..];
            int chunkLength = method(chunkSpan, bufferRemaining, minSize, midSize, maxSize);
            var end = bufferOffset + chunkLength;
            yield return chunkBuffer[bufferOffset..end];

            // Shift buffer
            bufferRemaining -= chunkLength;
            bufferOffset += chunkLength;
        }
    }
    public static void SplitToChunks(
        Stream stream,
        Span<byte> chunkBuffer,
        Action<Span<byte>> consumer,
        int minSize = 1 * 1024,
        int midSize = 16 * 1024,
        int maxSize = 32 * 1024,
        CDCImplementation impl = CDCImplementation.Normalized
    ) {
        ArgumentOutOfRangeException.ThrowIfNegative(minSize);
        ArgumentOutOfRangeException.ThrowIfNegative(midSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSize);
        var method = FastCDCChunking.GetMethod(impl);
        if (chunkBuffer.Length < 2 * maxSize) {
            throw new ArgumentException("Buffer size need to 2x larger than maxSize");
        }
        // Need to esure bufferSize is much larger than maxSize
        int bufferRemaining = 0;
        int bufferOffset = 0;
        bool eof = false;
        while (!(eof && bufferRemaining == 0)) {
            // Fill buffer
            while (!eof && bufferRemaining < maxSize) {
                // Shift remaining
                chunkBuffer.Slice(bufferOffset, bufferRemaining).CopyTo(chunkBuffer);
                // Read new data. This may not fill buffer
                int byteRead = stream.Read(chunkBuffer[bufferRemaining..]);
                bufferRemaining += byteRead;
                bufferOffset = 0;
                if (byteRead == 0) {
                    eof = true;
                }
            }
            // CDC
            // BufferRemaining always more or equal maxSize unless eof
            var chunkSpan = chunkBuffer[bufferOffset..];
            int chunkLength = method(chunkSpan, bufferRemaining, minSize, midSize, maxSize);
            var end = bufferOffset + chunkLength;
            consumer(chunkBuffer[bufferOffset..end]);

            // Shift buffer
            bufferRemaining -= chunkLength;
            bufferOffset += chunkLength;
        }
    }

}
