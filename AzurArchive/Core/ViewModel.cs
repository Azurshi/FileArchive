using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AzurArchive.Core;

public abstract partial class ObservableObject : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
    public void OnPropertyChanged(object? sender, string propertyName = "") {
        PropertyChanged?.Invoke(sender, new(propertyName));
    }
    public int GetPropertyChangedHandlerCount() {
        return PropertyChanged?.GetInvocationList().Length ?? 0;
    }
}
public partial class SyncCommand<T> : ICommand {
    protected readonly Action<T?> _execute;
    protected bool _enabled = true;
    public event EventHandler? CanExecuteChanged;
    public bool Enabled {
        get => _enabled;
        set {
            _enabled = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public SyncCommand(Action<T?> execute) {
        this._execute = execute;
    }
    public virtual bool CanExecute(object? parameter) {
        return this._enabled;
    }
    public virtual void Execute(object? parameter) {
        this._execute((T?)parameter);
    }
    protected void InnerNotifyCanExecute() {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
public partial class SyncCommandExtend<T> : SyncCommand<T> {
    private readonly Func<T?, bool> _canExecute;
    public SyncCommandExtend(Action<T?> execute, Func<T?, bool> canExecute) : base(execute) {
        this._canExecute = canExecute;
    }
    public override bool CanExecute(object? parameter) {
        if (parameter is T casted) {
            if (this._enabled && this._canExecute.Invoke(casted)) {
                return true;
            }
        }
        return false;
    }
    public void NotifyCanExecute() {
        InnerNotifyCanExecute();
    }
}
public partial class SyncCommand : ICommand {
    private readonly Action _execute;
    protected bool _enabled = true;
    public event EventHandler? CanExecuteChanged;
    public bool Enabled {
        get => _enabled;
        set {
            _enabled = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public SyncCommand(Action execute) {
        this._execute = execute;
    }
    public virtual bool CanExecute(object? parameter) {
        return this._enabled;
    }
    public void Execute(object? parameter) {
        this._execute();
    }
    protected void InnerNotifyCanExecute() {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
public partial class SyncCommandExtend : SyncCommand {
    private readonly Func<bool> _canExecute;
    public SyncCommandExtend(Action execute, Func<bool> canExecute) : base(execute) {
        this._canExecute = canExecute;
    }
    public override bool CanExecute(object? parameter) {
        if (this._enabled && this._canExecute.Invoke()) {
            return true;
        }
        return false;
    }
    public void NotifyCanExecute() {
        InnerNotifyCanExecute();
    }
}
public partial class AsyncCommand<T> : ICommand {
    private readonly Func<T?, Task> _execute;
    protected bool _enabled = true;
    private bool _busy = false;
    public event EventHandler? CanExecuteChanged;
    public bool Enabled {
        get => _enabled;
        set {
            _enabled = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public AsyncCommand(Func<T?, Task> execute) {
        this._execute = execute;
    }
    public virtual bool CanExecute(object? parameter) {
        return !this._busy && this._enabled;
    }
    public void Execute(object? parameter) {
        if (!this.CanExecute(parameter)) return;
        this.InnerExecute(parameter).FireAndForgetAsync();
    }
    private async Task InnerExecute(object? parameter) {
        this._busy = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try {
            await this._execute((T?)parameter);
        }
        finally {
            this._busy = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    protected void InnerNotifyCanExecute() {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
public partial class AsyncCommandExtend<T> : AsyncCommand<T> {
    private readonly Func<T?, bool> _canExecute;
    public AsyncCommandExtend(Func<T?, Task> execute, Func<T?, bool> canExecute) : base(execute) {
        this._canExecute = canExecute;
    }
    public override bool CanExecute(object? parameter) {
        if (parameter is T casted) {
            if (this._enabled && this._canExecute.Invoke(casted)) {
                return true;
            }
        }
        return false;
    }
    public void NotifyCanExecute() {
        InnerNotifyCanExecute();
    }

}
public partial class AsyncCommand : ICommand {
    private readonly Func<Task> _execute;
    protected bool _enabled = true;
    private bool _busy = false;
    public event EventHandler? CanExecuteChanged;
    public bool Enabled {
        get => _enabled;
        set {
            _enabled = value;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public AsyncCommand(Func<Task> execute) {
        this._execute = execute;
    }
    public virtual bool CanExecute(object? parameter) {
        return !this._busy && this._enabled;
    }
    public void Execute(object? parameter) {
        if (!this.CanExecute(parameter)) return;
        this.InnerExecute(parameter).FireAndForgetAsync();
    }
    private async Task InnerExecute(object? parameter) {
        this._busy = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try {
            await this._execute();
        }
        finally {
            this._busy = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    protected void InnerNotifyCanExecute() {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
public partial class AsyncCommandExtend : AsyncCommand {
    private readonly Func<bool> _canExecute;
    public AsyncCommandExtend(Func<Task> execute, Func<bool> canExecute) : base(execute) {
        this._canExecute = canExecute;
    }
    public override bool CanExecute(object? parameter) {
        if (this._enabled && this._canExecute.Invoke()) {
            return true;
        }
        return false;
    }
    public void NotifyCanExecute() {
        InnerNotifyCanExecute();
    }
}