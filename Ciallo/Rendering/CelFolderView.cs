using Frent;
using Godot;
using ObservableCollections;
using R3;
using System;
using Ciallo.Data;
using System.Collections.Generic;

namespace Ciallo.Rendering;

public partial class CelFolderView : FolderLayerView
{
    private readonly List<Node2D> _displayingOnionSkinViews = [];
    private Node2D _displayingLayerView;

    private Node2D DisplayingLayerView
    {
        get => _displayingLayerView;
        set
        {
            HideNode(_displayingLayerView);
            _displayingLayerView = value;
            ShowNode(_displayingLayerView);
        }
    }

    public CompositeDisposable Observe(
        FolderLayerSetting setting,
        LayerTreeNode layerNode,
        Observable<bool> shouldShowOnionSkin,
        Observable<SortedList<int, ShaderMaterial>> onionSkinMaterials)
    {
        CompositeDisposable subs = new();
        layerNode.ObserveAddChild().Subscribe(et =>
        {
            HideNode(GetLayerView(et.Value));
        }).AddTo(subs);

        // Can safely assume when exposures change, view nodes are already children of this node, so we can just update their visibility.
        setting.CurrentExposedCel.Subscribe(e =>
        {
            DisplayingLayerView = e.IsNull ? null : GetLayerView(e);
        }).AddTo(subs);

        layerNode.ObserveRemoveChild().Subscribe(et =>
        {
            ShowNode(GetLayerView(et.Value));
        }).AddTo(subs);

        // Note: Although CurrentOnionSkinCels could have duplicated entities (e.g. if the same cel is onion-skinned at multiple offsets)
        // Its Ok to ShowOnionSkin it twice
        shouldShowOnionSkin.CombineLatest(setting.CurrentOnionSkinCels, onionSkinMaterials, ValueTuple.Create)
            .Subscribe(tuple =>
            {
                var (shouldShow, onionSkinCels, materials) = tuple;
                HideDisplayingOnionSkinViews();

                if (!shouldShow)
                    return;

                foreach (var (offset, cel) in onionSkinCels)
                {
                    if (!materials.TryGetValue(offset, out var material))
                        continue;

                    var view = GetLayerView(cel);
                    if (view == DisplayingLayerView)
                        continue;

                    ShowOnionSkin(view, material);
                    _displayingOnionSkinViews.Add(view);
                }
            }).AddTo(subs);

        return subs;
    }

    private void HideDisplayingOnionSkinViews()
    {
        foreach (var view in _displayingOnionSkinViews)
            HideOnionSkin(view);
        _displayingOnionSkinViews.Clear();
        ShowNode(DisplayingLayerView);
    }

    private static void HideNode(Node2D node) => node?.VisibilityLayer = 0;
    private static void ShowNode(Node2D node) => node?.VisibilityLayer = 1 << 0;

    private static void ShowOnionSkin(Node2D node, ShaderMaterial material)
    {
        ShowNode(node);
        node?.Material = material;
        node?.ZIndex = -1;
    }

    private static void HideOnionSkin(Node2D node)
    {
        HideNode(node);
        node?.Material = null;
        node?.ZIndex = 0;
    }

    private static Node2D GetLayerView(Entity e)
    {
        if (e.Has<ShapeLayerView>())
            return e.Get<ShapeLayerView>();
        if (e.Has<FolderLayerView>())
            return e.Get<FolderLayerView>();
        if (e.Has<Sprite2D>())
            return e.Get<Sprite2D>();

        throw new Exception("Unknown layer type");
    }
}
