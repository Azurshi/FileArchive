using System.Collections.Generic;

namespace AzurArchive.Domain;

public class FolderStack {
    private readonly List<long> _ids;
    private int _currentIdex = -1;
    public FolderStack() {
        this._ids = [];
    }
    public bool CanBackward() {
        return _currentIdex > 0;
    }
    public bool CanForward() {
        return _ids.Count > 0 && _currentIdex < _ids.Count - 1;
    }
    public long? Backward() {
        if (CanBackward()) {
            _currentIdex--;
            return this._ids[_currentIdex];
        } else {
            return null;
        }

    }
    public long? Forward() {
        if (CanForward()) {
            _currentIdex++;
            return this._ids[_currentIdex];
        } else {
            return null;
        }
    }
    public void Move(long id) {
        if (_currentIdex < _ids.Count - 1) {
            _ids.RemoveRange(_currentIdex, _ids.Count - 1 - _currentIdex);
        }
        _ids.Add(id);
        _currentIdex++;
    }
}
