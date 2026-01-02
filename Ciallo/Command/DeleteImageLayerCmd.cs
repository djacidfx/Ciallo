using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteImageLayerCmd : CommandBase
{
    private Entity _parentE;
    private int _index;

    private ImageLayerSetting _setting;
    private Sprite2D _sprite;
    private TransformOverlayBox _overlay;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects => [_sprite, _overlay];

    protected override void BeforeFirstDo(Entity layerE)
    {
        _overlay = layerE.Get<TransformOverlayBox>();
        _sprite = layerE.Get<Sprite2D>();
        _setting = layerE.Get<ImageLayerSetting>();

        _parentE = layerE.Get<LayerTreeNode>().Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);
    }

    protected override void Do(Entity layerE)
    {
        // Overlay
        _overlay.RemoveFromParent();
        layerE.Remove<TransformOverlayBox>();

        // View
        _sprite.RemoveFromParent();
        layerE.Remove<Sprite2D>();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(layerE);

        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(_index);
        layerE.Remove<ImageLayerSetting>();
        layerE.Detach<ToSerializeTag>();
    }

    protected override void Undo(Entity layerE)
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, layerE);
        layerE.Add(_setting);
        layerE.Tag<ToSerializeTag>();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(layerE, _index);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.InsertNodeAt(_sprite, _index);
        layerE.Add(_sprite);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_overlay);
        layerE.Add(_overlay);
    }
}