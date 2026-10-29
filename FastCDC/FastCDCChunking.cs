using System;

namespace FastCDC;

public enum CDCImplementation {
    CDC,
    Normalized,
    NormalizedPointer,
    Normalized_2Bytes,
    Rolling_2Bytes
}

internal static class FastCDCChunking {
    public static Func<Span<byte>, int, int, int, int, int> GetMethod(CDCImplementation variant) {
        return variant switch {
            CDCImplementation.CDC => CDC_Origin,
            CDCImplementation.Normalized => NormalizedChunking,
            CDCImplementation.Normalized_2Bytes => NormalizedChunking_2Bytes,
            CDCImplementation.Rolling_2Bytes => RollingData_2Bytes,
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
    }
    /// <summary>
    /// Provide little improment
    /// </summary>
    internal static int NormalizedChunkingPointer(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
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
        unsafe {
            fixed (byte* ptr = p) {
                while (i < mid) {
                    fingerprint = (fingerprint << 1) + Constant.GEARv2[ptr[i]];
                    if ((fingerprint & Constant.FING_GEAR_32KB) == 0) {
                        return i + 1;
                    }
                    i++;
                }
                while (i < n) {
                    fingerprint = (fingerprint << 1) + Constant.GEARv2[ptr[i]];
                    if ((fingerprint & Constant.FING_GEAR_02KB) == 0) {
                        return i + 1;
                    }
                    i++;
                }
            }
        }
        return n;
    }
    internal static int NormalizedChunking(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
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
    internal static int NormalizedChunking_2Bytes(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
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
    internal static int RollingData_2Bytes(Span<byte> p, int n, int minSize, int midSize, int maxSize) {
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
    internal static int CDC_Origin(Span<byte> p, int n, int minSize, int _, int maxSize) {
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
