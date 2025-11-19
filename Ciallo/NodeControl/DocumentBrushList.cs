using Ciallo.Data;
using Frent;
using Godot;
using R3;

namespace Ciallo.NodeControl;

public partial class DocumentBrushList : ItemList
{
    public void Add(Entity brushE, BrushManager bm)
    {
        var setting = brushE.Get<BrushSetting>();
        AddItem(setting.Name.Value);
        var sub = setting.Name.Subscribe(s =>
        {
            var idx = bm.Brushes.IndexOf(brushE);
            SetItemText(idx, s);
        });
        SetItemMetadata(ItemCount - 1, Callable.From(() => sub.Dispose()));
    }

    public void Remove(Entity brushE, BrushManager bm)
    {
        var idx = bm.Brushes.IndexOf(brushE);
        var subDispose = (Callable)GetItemMetadata(idx);
        subDispose.Call();
        RemoveItem(idx);
    }
}