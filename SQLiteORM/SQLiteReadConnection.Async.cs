using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteORM;

public partial class SQLiteReadConnection {
    public async Task<List<object?[]>> SelectAsync(string sql, Type[] columnTypes, params object[] args) {
        await this._semaphore.WaitAsync();
        try {
            List<object?[]> result = await Task.Run(() => SelectToList(sql, columnTypes, args));
            return result;
        }
        finally {
            this._semaphore.Release();
        }
    }
    /// <summary>
    /// onRow run on subthread and must not access shared state.
    /// </summary>
    /// <param name="onRow"></param>
    /// <param name="sql"></param>
    /// <param name="columnTypes"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public async Task Aggregate(Action<object?[]> onRow, string sql, Type[] columnTypes, params object[] args) {
        await this._semaphore.WaitAsync();
        try {
            await Task.Run(() => {
                foreach (var row in SelectInner(sql, columnTypes, args)) {
                    onRow(row);
                }
            });
        }
        finally {
            this._semaphore.Release();
        }
    }
    public async Task<List<object?[]>> CustomFilterSelect(Func<object?[], bool> condition, string sql, Type[] columnTypes, params object[] args) {
        await this._semaphore.WaitAsync();
        try {
            return await Task.Run(() => {
                List<object?[]> result = [];
                foreach (var row in SelectInner(sql, columnTypes, args)) {
                    if (condition(row)) {
                        result.Add(row);
                    }
                }
                return result;
            });
        }
        finally {
            this._semaphore.Release();
        }
    }
    #region TupleAPI
    public async Task<List<ValueTuple<T1>>> SelectAsync<T1>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ValueTuple.Create((T1)r[0]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2>>> SelectAsync<T1, T2>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!));
        }
        finally { this._semaphore.Release(); }

    }
    public async Task<List<ValueTuple<T1, T2, T3>>> SelectAsync<T1, T2, T3>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4>>> SelectAsync<T1, T2, T3, T4>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5>>> SelectAsync<T1, T2, T3, T4, T5>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6>>> SelectAsync<T1, T2, T3, T4, T5, T6>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18), typeof(T19)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!, (T19)r[18]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18), typeof(T19)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!, (T19)r[18]!, (T20)r[19]!));
        }
        finally { this._semaphore.Release(); }
    }
    public async Task<List<ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>>>> SelectAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string sql, params object[] args) {
        await this._semaphore.WaitAsync();
        Type[] types = [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16), typeof(T17), typeof(T18), typeof(T19), typeof(T20)];
        try {
            List<object?[]> rows = await Task.Run(() => SelectToList(sql, types, args));
            return Cast(rows, r => ((T1)r[0]!, (T2)r[1]!, (T3)r[2]!, (T4)r[3]!, (T5)r[4]!, (T6)r[5]!, (T7)r[6]!, (T8)r[7]!, (T9)r[8]!, (T10)r[9]!, (T11)r[10]!, (T12)r[11]!, (T13)r[12]!, (T14)r[13]!, (T15)r[14]!, (T16)r[15]!, (T17)r[16]!, (T18)r[17]!, (T19)r[18]!, (T20)r[19]!, (T21)r[20]!));
        }
        finally { this._semaphore.Release(); }
    }
    #endregion
}
