using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;
using System;

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
        ChildEnteredTree += OnChildEnteredTree;
        ChildExitingTree += OnChildExitingTree;
    }

    private void OnChildEnteredTree(Node node)
    {
        var n = (Node2D)node;
        HideNode(n);
    }

    private void OnChildExitingTree(Node node)
    {
        var n = (Node2D)node;
        ShowNode(n);
        if (DisplayingLayerView == n)
        {
            DisplayingLayerView = null;
        }
    }

    public void Observe(ObservableSortedList<int, Entity> exposures, ReactiveProperty<int> currentFrame, CompositeDisposable subs)
    {
        // Can safely assume when exposures change, view nodes are already children of this node, so we can just update their visibility.
        exposures.ObserveChanged().ToReadOnlyReactiveProperty()
            .CombineLatest(currentFrame, (_, currentFrame) => exposures.FloorKey(currentFrame))
            .Select(key => key.HasValue ? exposures[key.Value] : Entity.Null)
            .Subscribe(e =>
            {
                DisplayingLayerView = e.IsNull ? null : GetLayerView(e);
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