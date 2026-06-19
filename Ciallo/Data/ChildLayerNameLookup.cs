using System;
using System.Collections.Generic;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Keeps child layer names available by child entity.
/// </summary>
public class ChildLayerNameLookup
{
    private readonly ObservableDictionary<Entity, string> _names = [];
    public IReadOnlyObservableDictionary<Entity, string> Names => _names;

    private readonly Entity _folderE;
    private CompositeDisposable _subs;

    public ChildLayerNameLookup(Entity e)
    {
        _folderE = e;
        Rebuild();
    }

    public void Subscribe()
    {
        _subs?.Dispose();
        var subs = _subs = new CompositeDisposable();
        var layerNode = _folderE.Get<LayerTreeNode>();
        var nameSubs = new Dictionary<Entity, IDisposable>();

        foreach (var layerE in layerNode.Children)
            nameSubs[layerE] = SubscribeName(layerE);

        layerNode.ObserveAddChild()
            .Select(et => et.Value)
            .Subscribe(layerE =>
            {
                _names[layerE] = layerE.Get<CommonLayerSetting>().Name.Value;
                nameSubs[layerE] = SubscribeName(layerE);
            })
            .AddTo(subs);

        layerNode.ObserveRemoveChild()
            .Select(et => et.Value)
            .Subscribe(layerE =>
            {
                nameSubs.Remove(layerE, out var nameSub);
                nameSub?.Dispose();
                _names.Remove(layerE);
            })
            .AddTo(subs);

        subs.Add(Disposable.Create(() =>
        {
            foreach (var sub in nameSubs.Values)
                sub.Dispose();
        }));
    }

    public void Unsubscribe()
    {
        _subs?.Dispose();
        _subs = null;
    }

    private IDisposable SubscribeName(Entity layerE)
    {
        return layerE.Get<CommonLayerSetting>().Name
            .Skip(1)
            .Subscribe(name =>
            {
                _names[layerE] = name;
            });
    }

    private void Rebuild()
    {
        _names.Clear();
        foreach (var layerE in _folderE.Get<LayerTreeNode>().Children)
            _names[layerE] = layerE.Get<CommonLayerSetting>().Name.Value;
    }
}
