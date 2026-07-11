using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AzurArchive.Data;

[StructLayout(LayoutKind.Sequential)]
public struct Hash256 {
    private ulong A, B, C, D;
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
    /// <summary>
    /// The span returned must have equal or less lifespan than <see cref="Hash256"/> struct.
    /// </summary>
    /// <returns></returns>
    [UnscopedRef]
    public Span<byte> AsSpan() {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
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
}

public sealed class SemaphoreLock {

}