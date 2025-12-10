using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.NodeControl;
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

    public override void Do(Entity layerE)
    {
        // Overlay
        _overlay ??= layerE.Get<TransformOverlayBox>();
        _overlay.RemoveFromParent();
        layerE.Remove<TransformOverlayBox>();

        // View
        _sprite ??= layerE.Get<Sprite2D>();
        _sprite.RemoveFromParent();
        layerE.Remove<Sprite2D>();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(layerE);

        // Data
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);
        if (_parentE.IsNull) _parentE = layerE.Get<LayerTreeNode>().Parent;
        _parentE.Get<LayerTreeNode>().RemoveChild(_index);
        _setting ??= layerE.Get<ImageLayerSetting>();
        layerE.Remove<ImageLayerSetting>();
        layerE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity layerE)
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