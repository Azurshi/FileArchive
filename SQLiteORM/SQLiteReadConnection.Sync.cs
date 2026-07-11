using System;
using System.Collections.Generic;
using System.Text;

namespace SQLiteORM;

public partial class SQLiteReadConnection {
    public IEnumerable<object?[]> Select(string sql, Type[] columnTypes, params object[] args) {
        return SelectInner(sql, columnTypes, args);
    }
    #region TupleAPI
    public IEnumerable<ValueTuple<T1>> Select<T1>(string sql, params object[] args) {
        Type[] types = [typeof(T1)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ValueTuple.Create((T1)r[0]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2>> Select<T1, T2>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3>> Select<T1, T2, T3>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4>> Select<T1, T2, T3, T4>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5>> Select<T1, T2, T3, T4, T5>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6>> Select<T1, T2, T3, T4, T5, T6>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Select<T1, T2, T3, T4, T5, T6, T7>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>> Select<T1, T2, T3, T4, T5, T6, T7, T8>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18), typeof(T19)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!, (T19)r[18]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18), typeof(T19), typeof(T20)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!, (T19)r[18]!, (T20)r[19]!);
        }
    }
    public IEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>>> Select<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string sql, params object[] args) {
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18), typeof(T19), typeof(T20), typeof(T21)];
        foreach (var r in SelectInner(sql, types, args)) {
            yield return ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!, (T19)r[18]!, (T20)r[19]!, (T21)r[20]!);
        }
    }
    #endregion
}
