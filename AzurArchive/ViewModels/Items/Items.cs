using AzurArchive.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AzurArchive.ViewModels.Items;

public partial class ListItem: ObservableObject, IListItem {
    private static readonly Brush EvenBrush = new SolidColorBrush(Colors.Transparent);
    private static readonly Brush OddBrush = new SolidColorBrush(Color.FromArgb(0x1F, 0x80, 0x80, 0x80));
    private Brush _background = EvenBrush;
    private bool _isActive = true;
    public bool IsActive {
        get => _isActive;
        set {
            if (_isActive != value) {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }
    public Brush Background {
        get => _background;
        set {
            _background = value;
            OnPropertyChanged();
        }
    }
    public void SetOddBackgroundColor() {
        Background = OddBrush;
    }
    public void SetEvenBackgroundColor() {
        Background = EvenBrush;
    }
    public void AutoBackgroundColor(int index) {
        if (index % 2 == 0) {
            Background = OddBrush;
        }
        else {
            Background = EvenBrush;
        }
    }
}

public partial class ViewOnlyListItem: ListItem, IEditableListItem, ISelectableItem {
    private bool _listEditing = false;
    public bool ListEditing {
        get => _listEditing;
        set {
            if (_listEditing != value) {
                _listEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ListEditVisibility));
                OnPropertyChanged(nameof(ListViewVisibility));
            }
        }
    }
    public bool ListEditVisibility => _listEditing;
    public bool ListViewVisibility => !_listEditing;
    protected bool _selected = false;
    public bool Selected {
        get => _selected;
        set {
            if (_selected != value) {
                _selected = value;
                OnPropertyChanged();
            }
        }
    }
}

public partial class EditableListItem : ViewOnlyListItem, IEditableItem, IMoveableItem {
    private bool _editing = false;
    public bool Editing {
        get => _editing;
        set {
            if (_editing != value) {
                _editing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditVisibility));
                OnPropertyChanged(nameof(ViewVisibility));
            }
        }
    }
    public bool EditVisibility => _editing;
    public bool ViewVisibility => !_editing;
    private bool _idDraggable = false;
    public bool IsDraggable {
        get => _idDraggable;
        set {
            if (_idDraggable != value) {
                _idDraggable= value;
                OnPropertyChanged();
            }
        }
    }
}