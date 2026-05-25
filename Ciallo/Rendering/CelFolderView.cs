using Frent;
using Godot;
using ObservableCollections;
using R3;
using System;
using Ciallo.Data;

namespace Ciallo.Rendering;

public partial class CelFolderView : FolderLayerView
{
    public Node2D DisplayingLayerView
    {
        get;
        set
        {
            HideNode(field);
            field = value;
            ShowNode(field);
        }
    }

    public CelFolderView()
    {
    }

    public void Observe(Observable<Entity> currentExposedCel, LayerTreeNode layerNode, CompositeDisposable subs)
    {
        layerNode.ObserveAddChild().Subscribe(et =>
        {
            HideNode(GetLayerView(et.Value));
        }).AddTo(subs);

        // Can safely assume when exposures change, view nodes are already children of this node, so we can just update their visibility.
        currentExposedCel.Subscribe(e =>
            {
                DisplayingLayerView = e.IsNull ? null : GetLayerView(e);
            }).AddTo(subs);

        layerNode.ObserveRemoveChild().Subscribe(et =>
        {
            ShowNode(GetLayerView(et.Value));
        }).AddTo(subs);

    }

    public void HideNode(Node2D node) => node?.VisibilityLayer = 0;
    public void ShowNode(Node2D node) => node?.VisibilityLayer = 1 << 0;

    public Node2D GetLayerView(Entity e)
    {
        if (e.Has<ShapeLayerView>())
            return e.Get<ShapeLayerView>();
        if (e.Has<FolderLayerView>())
            return e.Get<FolderLayerView>();
        if (e.Has<Sprite2D>())
            return e.Get<Sprite2D>();
        else
        {
            throw new Exception("Unknown layer type");
        }
    }
}