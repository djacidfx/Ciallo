using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class VectorFillBrushPreviewList : Container
{
    public override void _Ready() { }

    public void Bind(ObservableList<Entity> brushes, ReactiveProperty<Entity> workingBrush)
    {
        var viewList = brushes.ToNotifyCollectionChanged<Entity, Control>(e =>
        {
            var box = new PanelContainer().QueueFreeWith(e);
            var background = new ColorRect();
            box.AddChild(background);
            var markerPreview = new TextureRect();
            box.AddChild(markerPreview);

            var setting = e.Get<VectorFillBrushSetting>();
            setting.MarkerTexture.Subscribe(markerPreview.SetTexture).AddTo(e);
            setting.MarkerColor.Subscribe(markerPreview.SetModulate).AddTo(e);
            setting.FillColor.Subscribe(background.SetColor).AddTo(e);

            return box;
        });
    }
}