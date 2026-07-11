using Microsoft.UI.Xaml.Media;

namespace AzurArchive.ViewModels;

public interface IUpdateble {
    public object Identify { get; }
}
public interface ISelectableItem {
    public bool Selected { get; set; }
}
public interface IEditableListItem {
    public bool ListEditing { get; set; }
    public bool ListEditVisibility { get; }
    public bool ListViewVisibility { get; }
}
public interface IListItem {
    public Brush Background { get; }
    public void SetOddBackgroundColor();
    public void SetEvenBackgroundColor();
    public void AutoBackgroundColor(int index);
}
public interface IEditableItem {
    public bool Editing { get; set; }
    public bool EditVisibility { get; }
    public bool ViewVisibility { get; }
}
public interface IMoveableItem {
    public bool IsDraggable { get; set; }
}