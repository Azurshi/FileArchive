using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AzurArchive.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Hash256: IEquatable<Hash256>, IComparable {
    private readonly ulong A, B, C, D;
    public readonly int ShardIndex => (int)(A >> 56) % Config.Shards;
    public static bool operator ==(Hash256 left, Hash256 right) {
        return left.A == right.A && left.B == right.B && left.C == right.C && left.D == right.D;
    }
    public static bool operator !=(Hash256 left, Hash256 right) {
        return left.A != right.A || left.B != right.B || left.C != right.C || left.D != right.D;
    }
    public override readonly int GetHashCode() {
        return HashCode.Combine(A, B, C, D);
    }
    public Hash256(ReadOnlySpan<byte> source) {
        if (source.Length != 32) {
            throw new ArgumentException("Source must be 32 bytes");
        }
        this = MemoryMarshal.Read<Hash256>(source);
    }
    public readonly ReadOnlySpan<byte> AsReadOnlySpan() {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in this, 1));
    }
    public readonly override bool Equals(object? obj) {
        if (obj is Hash256 hash) {
            return A == hash.A && B == hash.B && C == hash.C && D == hash.D;
        }
        else {
            return false;
        }
    }

    public bool Equals(Hash256 other) {
        return A == other.A && B == other.B && C == other.C && D == other.D;
    }

    public int CompareTo(object? obj) {
        if (obj is Hash256 other) {
            int result = A.CompareTo(other.A);
            if (result == 0) {
                result = B.CompareTo(other.B);
                if (result == 0) {
                    result = C.CompareTo(other.C);
                    if (result == 0) {
                        result = D.CompareTo(other.D);
                    }
                }
            }
            return result;
        }
        else {
            return -1;
        }
    }
}

public sealed class SemaphoreLock {

}