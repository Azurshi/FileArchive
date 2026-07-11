using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FastCDC;

public enum CDCImplementation {
    CDC,
    Normalized,
    Normalized_2Bytes,
    Rolling_2Bytes
}
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
        Func<Span<byte>, int, int, int, int, int> method = impl switch {
            CDCImplementation.CDC => CDC_Origin,
            CDCImplementation.Normalized => NormalizedChunking,
            CDCImplementation.Normalized_2Bytes => NormalizedChunking_2Bytes,
            CDCImplementation.Rolling_2Bytes => RollingData_2Bytes,
            _ => throw new ArgumentOutOfRangeException()
        };
        if (chunkBuffer.Length < 2 * maxSize) {
            throw new ArgumentException("Buffer size need to 2x larger than maxSize");
        }
        // Need to esure bufferSize is much larger than maxSize
        int bufferRemaining = 0;
        bool eof = false;
        while (!(eof && bufferRemaining == 0)) {
            // Fill buffer
            while (!eof && bufferRemaining < maxSize) {
                // This may not fill buffer
                int byteRead = stream.Read(chunkBuffer.Span[bufferRemaining..]);
                bufferRemaining += byteRead;
                if (byteRead == 0) {
                    eof = true;
                }
            }
            // CDC
            // BufferRemaining always more or equal maxSize unless eof
            int chunkLength = method(chunkBuffer.Span, bufferRemaining, minSize, midSize, maxSize);
            yield return chunkBuffer[..chunkLength];

            // Shift buffer
            bufferRemaining -= chunkLength;
            chunkBuffer.Slice(chunkLength, bufferRemaining).CopyTo(chunkBuffer);
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
        Func<Span<byte>, int, int, int, int, int> method = impl switch {
            CDCImplementation.CDC => CDC_Origin,
            CDCImplementation.Normalized => NormalizedChunking,
            CDCImplementation.Normalized_2Bytes => NormalizedChunking_2Bytes,
            CDCImplementation.Rolling_2Bytes => RollingData_2Bytes,
            _ => throw new ArgumentOutOfRangeException()
        };
        if (chunkBuffer.Length < 2 * maxSize) {
            throw new ArgumentException("Buffer size need to 2x larger than maxSize");
        }
        // Need to esure bufferSize is much larger than maxSize
        int bufferRemaining = 0;
        bool eof = false;
        while (!(eof && bufferRemaining == 0)) {
            // Fill buffer
            while (!eof && bufferRemaining < maxSize) {
                // This may not fill buffer
                int byteRead = stream.Read(chunkBuffer[bufferRemaining..]);
                bufferRemaining += byteRead;
                if (byteRead == 0) {
                    eof = true;
                }
            }
            // CDC
            // BufferRemaining always more or equal maxSize unless eof
            int chunkLength = method(chunkBuffer, bufferRemaining, minSize, midSize, maxSize);
            consumer(chunkBuffer[..chunkLength]);

            // Shift buffer
            bufferRemaining -= chunkLength;
            chunkBuffer.Slice(chunkLength, bufferRemaining).CopyTo(chunkBuffer);
        }
    }
    public static async Task SplitToChunksAsync(
            Stream stream,
            Func<Memory<byte>, Task> consumer,
            Memory<byte> chunkBuffer,
            int minSize = 1 * 1024,
            int midSize = 16 * 1024,
            int maxSize = 32 * 1024,
            CDCImplementation impl = CDCImplementation.Normalized
        ) {
        ArgumentOutOfRangeException.ThrowIfNegative(minSize);
        ArgumentOutOfRangeException.ThrowIfNegative(midSize);
        ArgumentOutOfRangeException.ThrowIfNegative(maxSize);
        Func<Span<byte>, int, int, int, int, int> method = impl switch {
            CDCImplementation.CDC => CDC_Origin,
            CDCImplementation.Normalized => NormalizedChunking,
            CDCImplementation.Normalized_2Bytes => NormalizedChunking_2Bytes,
            CDCImplementation.Rolling_2Bytes => RollingData_2Bytes,
            _ => throw new ArgumentOutOfRangeException()
        };
        if (chunkBuffer.Length < 2 * maxSize) {
            throw new ArgumentException("Buffer size need to 2x larger than maxSize");
        }
        // Need to esure bufferSize is much larger than maxSize
        int bufferRemaining = 0;
        bool eof = false;
        while (!(eof && bufferRemaining == 0)) {
            // Fill buffer
            while (!eof && bufferRemaining < maxSize) {
                // This may not fill buffer
                int byteRead = stream.Read(chunkBuffer.Span[bufferRemaining..]);
                bufferRemaining += byteRead;
                if (byteRead == 0) {
                    eof = true;
                }
            }
            // CDC
            // BufferRemaining always more or equal maxSize unless eof
            int chunkLength = method(chunkBuffer.Span, bufferRemaining, minSize, midSize, maxSize);
            await consumer(chunkBuffer[0..chunkLength]);
            // Shift buffer
            bufferRemaining -= chunkLength;
            chunkBuffer.Slice(chunkLength, bufferRemaining).CopyTo(chunkBuffer);
        }
    }
    private static int NormalizedChunking(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
        ulong fingerprint = 0;
        int mid = midSize;
        if (n <= minSize) {
            return n;
        }
        if (n > maxSize) {
            n = maxSize;
        }
        else if (n < mid) {
            mid = n;
        }
        int i = minSize;
        while (i < mid) {
            fingerprint = (fingerprint << 1) + Constant.GEARv2[p[i]];
            if ((fingerprint & Constant.FING_GEAR_32KB) == 0) {
                return i + 1;
            }
            i++;
        }
        while (i < n) {
            fingerprint = (fingerprint << 1) + Constant.GEARv2[p[i]];
            if ((fingerprint & Constant.FING_GEAR_02KB) == 0) {
                return i + 1;
            }
            i++;
        }
        return n;
    }
    private static int NormalizedChunking_2Bytes(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
        ulong fingerprint = 0;
        int mid = midSize;
        if (n <= minSize) {
            return n;
        }
        if (n > maxSize) {
            n = maxSize;
        }
        else if (n < mid) {
            mid = n;
        }
        int i = minSize / 2;
        int halfMid = mid / 2;
        while (i < halfMid) {
            int a = i * 2;
            fingerprint = (fingerprint << 2) + Constant.LEARv2[p[a]];
            if ((fingerprint & Constant.FING_GEAR_32KB_ls) == 0) {
                return a + 1;
            }
            fingerprint += Constant.GEARv2[p[a + 1]];
            if ((fingerprint & Constant.FING_GEAR_32KB) == 0) {
                return a + 2;
            }
            i++;
        }
        int halfN = n / 2;
        while (i < halfN) {
            int a = i * 2;
            fingerprint = (fingerprint << 2) + Constant.LEARv2[p[a]];
            if ((fingerprint & Constant.FING_GEAR_02KB_ls) == 0) {
                return a + 1;
            }
            fingerprint += Constant.GEARv2[p[a + 1]];
            if ((fingerprint & Constant.FING_GEAR_02KB) == 0) {
                return a + 2;
            }
            i++;
        }
        return n;
    }
    private static int RollingData_2Bytes(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
        ulong fingerprint = 0;
        if (n <= minSize) {
            return n;
        }
        if (n > maxSize) {
            n = maxSize;
        }
        int i = minSize / 2;
        int halfN = n / 2;
        while (i < halfN) {
            int a = i * 2;
            fingerprint = (fingerprint << 2) + Constant.LEARv2[p[a]];
            if ((fingerprint & Constant.FING_GEAR_08KB_ls) == 0) {
                return a + 1;
            }
            fingerprint += Constant.GEARv2[p[a + 1]];
            if ((fingerprint & Constant.FING_GEAR_08KB) == 0) {
                return a + 2;
            }
            i++;
        }
        return n;
    }
    private static int CDC_Origin(Span<byte> p, int n, int minSize, int _, int maxSize) {
        ulong fingerprint = 0;
        int i = minSize;
        if (n < minSize) {
            return n;
        }
        if (n > maxSize) {
            n = maxSize;
        }
        while (i < n) {
            fingerprint = (fingerprint << 1) + Constant.GEARv2[p[i]];
            if ((fingerprint & Constant.FING_GEAR_08KB) == 0) {
                return i + 1;
            }
            i++;
        }
        return n;
    }
}
