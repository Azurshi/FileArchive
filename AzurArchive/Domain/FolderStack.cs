using System.Collections.Generic;

namespace AzurArchive.Domain;

public class FolderStack {
    private readonly List<long> _ids;
    private int _currentIndex = -1;
    public FolderStack() {
        this._ids = [];
    }
    public bool CanBackward() {
        return _currentIndex > 0;
    }
    public bool CanForward() {
        return _ids.Count > 0 && _currentIndex < _ids.Count - 1;
    }
    public long? Backward() {
        if (CanBackward()) {
            _currentIndex--;
            return this._ids[_currentIndex];
        } else {
            return null;
        }

    }
    public long? Forward() {
        if (CanForward()) {
            _currentIndex++;
            return this._ids[_currentIndex];
        } else {
            return null;
        }
    }
    public void Move(long id) {
        if (_currentIndex < _ids.Count - 1) {
            _ids.RemoveRange(_currentIndex + 1, _ids.Count - 1 - _currentIndex);
        }
        _ids.Add(id);
        _currentIndex++;
    }
}
