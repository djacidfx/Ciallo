using Ciallo.Data;
using Ciallo.Misc;
using Frent;
using Frent.Components;
using Godot;
using R3;

namespace Ciallo.GuiControl;

public partial class DocumentBrushList : ItemList, IInitable
{
    private Entity _document;
    public BrushManager Manager => _document.Get<BrushManager>();

    public DocumentBrushList()
    {
        TooltipText = "[Document Brush List Tooltip]".Tr();
    }

    public void Init(Entity document)
    {
        _document = document;
    }

    public void Add(Entity brushE)
    {
        var setting = brushE.Get<BrushSetting>();
        AddItem(setting.Name.Value);
        var sub = setting.Name.Subscribe(s =>
        {
            var idx = Manager.Brushes.IndexOf(brushE);
            SetItemText(idx, s);
        });
        SetItemMetadata(ItemCount - 1, Callable.From(() => sub.Dispose()));
    }

    public void Remove(Entity brushE)
    {
        var idx = Manager.Brushes.IndexOf(brushE);
        var subDispose = (Callable)GetItemMetadata(idx);
        subDispose.Call();
        RemoveItem(idx);
    }
}