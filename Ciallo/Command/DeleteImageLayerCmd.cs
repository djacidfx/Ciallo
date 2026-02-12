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

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity layerE)
    {
        _parentE = layerE.Get<LayerTreeNode>().Parent;
        _index = _parentE.Get<LayerTreeNode>().Children.IndexOf(layerE);
    }

    public override void Do(Entity layerE)
    {
        // Overlay
        layerE.Get<TransformOverlayBox>().RemoveFromParent();

        // View
        layerE.Get<Sprite2D>().RemoveFromParent();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.RemoveFree(layerE);

        // Data
        _parentE.Get<LayerTreeNode>().RemoveChild(_index);
        layerE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity layerE)
    {
        // Data
        var parentNode = _parentE.Get<LayerTreeNode>();
        parentNode.InsertChild(_index, layerE);
        layerE.Tag<ToSerializeTag>();

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateInsert(layerE, _index);

        // View
        var worldView = Document.Get<WorldView>();
        worldView.InsertNodeAt(layerE.Get<Sprite2D>(), _index);

        // Overlay
        var worldOverlay = Document.Get<WorldOverlay>();
        worldOverlay.AddChild(layerE.Get<TransformOverlayBox>());
    }
}