using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

public class DeleteImageLayerCmd : CommandBase
{
    private Entity _layerE;
    private Entity _parentE;
    private readonly int _originalIndex;

    private readonly Sprite2D _sprite;
    private readonly TransformOverlayBox _overlay;
    private readonly ImageLayerSetting _setting;

    public DeleteImageLayerCmd(Entity layerE)
    {
        _layerE = layerE;
        _originalIndex = Document.Get<LayerTreeNode>().FindPathTo(_layerE).Single();

        _sprite = _layerE.Get<Sprite2D>();
        _overlay = _layerE.Get<TransformOverlayBox>();
        _setting = _layerE.Get<ImageLayerSetting>();
    }

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(_layerE);
    public override IEnumerable<GodotObject> UndoRefObjects => new List<GodotObject> { _sprite, _overlay };

    public override void Do()
    {
        // Overlay
        _overlay.RemoveFromParent();
        _layerE.Remove<TransformOverlayBox>();

        // View
        _sprite.RemoveFromParent();
        _layerE.Remove<Sprite2D>();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(_layerE);

        // Data
        _parentE = _layerE.Get<LayerTreeNode>().Parent;
        _parentE.Get<LayerTreeNode>().RemoveChild(_originalIndex);
        _layerE.Remove<ImageLayerSetting>();
        _layerE.Detach<ToSerializeTag>();
    }

    public override void Undo()
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_originalIndex, _layerE);
        _layerE.Add(_setting);
        _layerE.Tag<ToSerializeTag>();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(_layerE, _originalIndex);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.InsertNodeAt(_sprite, _originalIndex);
        _layerE.Add(_sprite);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(_overlay);
        _layerE.Add(_overlay);
    }
}