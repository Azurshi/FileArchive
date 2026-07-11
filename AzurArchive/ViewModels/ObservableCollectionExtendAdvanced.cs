using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace AzurArchive.ViewModels;

public partial class ObservableCollectionExtendAdvanced<T> where T : IUpdateble {
    public ObservableCollection<T> Items => _processedCollection.Items;
    protected List<T> _originalCollection;
    protected readonly ObservableCollectionExtend<T> _processedCollection;
    protected Func<IReadOnlyList<T>, IReadOnlyList<T>>? _filter;
    public ObservableCollectionExtendAdvanced() {
        _originalCollection = [];
        _processedCollection = new([]);
    }
    public virtual void Update(IReadOnlyList<T> items, Action<T>? changeState = null) {
        _originalCollection = new(items);
        if (_filter != null) {
            _processedCollection.Update(_filter(_originalCollection), changeState);
        }
        else {
            _processedCollection.Update(_originalCollection, changeState);
        }
    }
    public virtual void SetFilter(Func<IReadOnlyList<T>, IReadOnlyList<T>>? filter, Action<T>? changeState = null) {
        _filter = filter;
        if (_filter != null) {
            _processedCollection.Update(_filter(_originalCollection), changeState);
        }
        else {
            _processedCollection.Update(_originalCollection, changeState);
        }
    }
}