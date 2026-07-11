using AzurArchive.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace AzurArchive.ViewModels;

public partial class ObservableCollectionExtend<T> : ObservableObject where T : IUpdateble {
    private enum ChangeKind {
        Insert,
        Remove,
        Move,
        Shuffle,
        Replace,
        State
    }
    private void HandleMoveOrShuffle(IReadOnlyList<T> newItems) {
        var currentItems = Items.ToList();
        List<ValueTuple<int, int>> l2rMovePath = [];
        List<ValueTuple<int, int>> r2lMovePath = [];
        for (int i = 0; i < newItems.Count; i++) {
            if (!currentItems[i].Identify.Equals(newItems[i].Identify)) {
                var targetIdentify = newItems[i].Identify;
                int targetIndex = -1;
                for (int j = 0; j < currentItems.Count; j++) {
                    if (currentItems[j].Identify.Equals(targetIdentify)) {
                        targetIndex = j;
                        break;
                    }
                }
                var value = currentItems[targetIndex];
                currentItems.RemoveAt(targetIndex);
                currentItems.Insert(i, value);
                l2rMovePath.Add((targetIndex, i));
            }
        }
        currentItems = Items.ToList();
        for (int i = newItems.Count - 1; i >= 0; i--) {
            if (!currentItems[i].Identify.Equals(newItems[i].Identify)) {
                var targetIdentify = newItems[i].Identify;
                int targetIndex = -1;
                for (int j = 0; j < currentItems.Count; j++) {
                    if (currentItems[j].Identify.Equals(targetIdentify)) {
                        targetIndex = j;
                        break;
                    }
                }
                var value = currentItems[targetIndex];
                currentItems.RemoveAt(targetIndex);
                currentItems.Insert(i, value);
                r2lMovePath.Add((targetIndex, i));
            }
        }
        List<ValueTuple<int, int>> movePath;
        if (l2rMovePath.Count > r2lMovePath.Count) {
            movePath = r2lMovePath;
        }
        else {
            movePath = l2rMovePath;
        }
        foreach (var (fromIndex, toIndex) in movePath) {
            var value = Items[fromIndex];
            Items.RemoveAt(fromIndex);
            Items.Insert(toIndex, value);
            //Debug.WriteLine($"{fromIndex} -> {toIndex}");
        }
    }
    private void HandleInsert(IReadOnlyList<T> items, int diffIndex) {
        Items.Insert(diffIndex, items[diffIndex]);
    }
    private void HandleRemove(IReadOnlyList<T> items, int diffIndex) {
        Items.RemoveAt(diffIndex);
    }
    private void HandleReplace(IReadOnlyList<T> newItems) {
        int oldCount = Items.Count;
        int newCount = newItems.Count;
        if (newCount > oldCount) {
            // New list have more items
            for (int i = 0; i < oldCount; i++) {
                if (!Items[i].Identify.Equals(newItems[i].Identify)) {
                    Items[i] = newItems[i];
                }
            }
            for (int i = oldCount; i < newCount; i++) {
                Items.Add(newItems[i]);
            }
        }
        else if (oldCount > newCount) {
            for (int i = 0; i < newCount; i++) {
                if (!Items[i].Identify.Equals(newItems[i].Identify)) {
                    Items[i] = newItems[i];
                }
            }
            // Old list have more items
            for (int i = newCount; i < oldCount; i++) {
                Items.RemoveAt(newCount);
            }
        }
        else {
            // Have same count
            for (int i = 0; i < oldCount; i++) {
                if (!Items[i].Identify.Equals(newItems[i].Identify)) {
                    Items[i] = newItems[i];
                }
            }
        }
    }
    private void HandleState(IReadOnlyList<T> items) {
        // Nothing changed
    }
    private ValueTuple<ChangeKind, int?> DetectChangeKinds(IReadOnlyList<T> newItems) {
        int oldCount = Items.Count;
        int newCount = newItems.Count;

        if (oldCount == newCount) {
            // Maye move, shuffle or state
            // Check state
            int count = oldCount;
            bool isStateOnly = true;
            for (int i = 0; i < count; i++) {
                if (!Items[i].Identify.Equals(newItems[i].Identify)) {
                    isStateOnly = false;
                    break;
                }
            }
            if (isStateOnly) {
                return (ChangeKind.State, null);
            }
            // Check move
            Dictionary<object, T> oldDict = [];
            Dictionary<object, T> newDict = [];
            foreach (var item in Items) {
                oldDict[item.Identify] = item;
            }
            foreach (var item in newItems) {
                newDict[item.Identify] = item;
            }
            if (oldDict.Keys.Count != Items.Count || newDict.Keys.Count != newItems.Count) {
                throw new ArgumentException($"Non-unique id detected");
            }
            var oldIds = oldDict.Keys.Order().ToList();
            var newIds = newDict.Keys.Order().ToList();
            // Check if have same items
            for (int i = 0; i < count; i++) {
                if (!oldIds[i].Equals(newIds[i])) {
                    return (ChangeKind.Replace, null);
                }
            }
            // Either move or shuffle
            // Since shuffle or move use same method
            return (ChangeKind.Shuffle, null);
        }
        else {
            if (newCount == oldCount + 1) {
                // Maybe insert
                int diffIndex = -1;
                int oldIndex = 0;
                int newIndex = 0;
                while (oldIndex < oldCount) {
                    if (!Items[oldIndex].Identify.Equals(newItems[newIndex].Identify)) {
                        if (diffIndex == -1) {
                            diffIndex = newIndex;
                            newIndex++;
                        }
                        else {
                            return (ChangeKind.Replace, null);
                        }
                    }
                    else {
                        oldIndex++;
                        newIndex++;
                    }
                }
                if (diffIndex == -1) {
                    diffIndex = oldCount;
                }
                return (ChangeKind.Insert, diffIndex);
            }
            else if (newCount == oldCount - 1) {
                // Maybe remove
                int diffIndex = -1;
                int oldIndex = 0;
                int newIndex = 0;
                while (newIndex < newCount) {
                    if (!Items[oldIndex].Identify.Equals(newItems[newIndex].Identify)) {
                        if (diffIndex == -1) {
                            diffIndex = oldIndex;
                            oldIndex++;
                        }
                        else {
                            return (ChangeKind.Replace, null);
                        }
                    }
                    else {
                        oldIndex++;
                        newIndex++;
                    }
                }
                if (diffIndex == -1) {
                    diffIndex = newCount;
                }
                return (ChangeKind.Remove, diffIndex);
            }
            else {
                // Replace
                return (ChangeKind.Replace, null);
            }
        }
    }

    public ObservableCollection<T> Items { get; private set; }
    public ObservableCollectionExtend(IReadOnlyList<T> items) {
        Items = new(items);
    }
    public ObservableCollectionExtend(ObservableCollection<T> items) {
        Items = items;
    }
    public void Update(IReadOnlyList<T> items, Action<T>? changeState = null) {
        var (kind, diffIndex) = DetectChangeKinds(items);
        Console.WriteLine(kind);
        switch (kind) {
            case ChangeKind.Insert:
                HandleInsert(items, diffIndex!.Value);
                break;
            case ChangeKind.Remove:
                HandleRemove(items, diffIndex!.Value);
                break;
            case ChangeKind.Replace:
                HandleReplace(items);
                break;
            case ChangeKind.State:
                HandleState(items);
                break;
            case ChangeKind.Shuffle:
                HandleMoveOrShuffle(items);
                break;
            case ChangeKind.Move:
                HandleMoveOrShuffle(items);
                break;
            default:
                throw new InvalidOperationException();
        }
        Debug.WriteLine(kind);
        RefreshState(changeState);
    }
    public void RefreshState(Action<T>? changeState = null) {
        for (int i = 0; i < Items.Count; i++) {
            var item = this.Items[i];
            if (item is IListItem listItem) {
                listItem.AutoBackgroundColor(i);
            }
            changeState?.Invoke(item);
        }
    }
}
