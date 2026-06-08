using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Ciallo;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Geometry;

// Owns an Arrangement and synchronizes it with a fixed set of shape-layer polyline indexes.
public class ArrangementManager : IDisposable
{
    // The currently queryable Arrangement, or null if not ready (e.g. mid-rebuild on a worker thread).
    // Subscribers must handle the null case. Same reference is re-emitted on every mutation.
    private readonly ReactiveProperty<Arrangement> _arrReady;
    public ReadOnlyReactiveProperty<Arrangement> ArrReady => _arrReady;

    private static readonly SemaphoreSlim NativeConcurrency = new(Math.Max(1, System.Environment.ProcessorCount - 1));

    private readonly Arrangement _arr = new();
    private readonly Subject<Unit> _flushRequests = new();
    private readonly object _gate = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Dictionary<Entity, PendingPolylineChange> _pendingChanges = [];
    private readonly HashSet<Entity> _sourceShapes = [];
    private readonly IDisposable _flushSub;

    private IReadOnlyList<ShapeLayerPolylineIndex> _indexes;
    private CompositeDisposable _subs;
    private bool _drainRunning;
    private bool _arrDisposed;
    private bool _pendingClear;

    public ArrangementManager()
    {
        // Empty arrangement is queryable from the start.
        _arrReady = new ReactiveProperty<Arrangement>(_arr);

        _flushSub = _flushRequests
            .DebounceFrame(1, GodotFrameProvider.Process)
            .Subscribe(_ => FlushPendingChanges());
    }

    public IReadOnlySet<Entity> SourceShapes => _sourceShapes;

    /// <summary>
    /// Observe the given shape-layer polyline indexes and synchronize the arrangement with them.
    /// Would be burst called 100+ times on project load.
    /// </summary>
    /// <remarks>
    /// Would be called multiple times, clean up previous subscriptions and start observing the new set of indexes.
    /// </remarks>
    public void Observe(params ShapeLayerPolylineIndex[] indexes)
    {
        _indexes = indexes;
        RebuildSourceShapes();
        RebuildAsync();
    }

    public void SyncModification()
    {
        if (_indexes == null)
            return;
        _subs?.Dispose();
        var subs = _subs = new CompositeDisposable();

        foreach (var index in _indexes)
        {
            index.Polylines.ObserveDictionaryAdd().Subscribe(et =>
            {
                _sourceShapes.Add(et.Key);
                UpsertShape(et.Key, et.Value);
            }).AddTo(subs);

            index.Polylines.ObserveDictionaryRemove().Subscribe(et =>
            {
                _sourceShapes.Remove(et.Key);
                RemoveShape(et.Key);
            }).AddTo(subs);

            index.Polylines.ObserveDictionaryReplace().Subscribe(et =>
            {
                UpsertShape(et.Key, et.NewValue);
            }).AddTo(subs);

            index.Polylines.ObserveClear().Subscribe(_ =>
            {
                RebuildSourceShapes();
                Clear();
            }).AddTo(subs);
        }
    }

    public void DesyncModification()
    {
        _subs?.Dispose();
        _subs = null;
    }

    public void Dispose()
    {
        DesyncModification();
        NotifyNotReady();

        bool disposeNow;
        lock (_gate)
        {
            _pendingClear = false;
            _pendingChanges.Clear();
            disposeNow = !_drainRunning && !_arrDisposed;
            if (disposeNow)
                _arrDisposed = true;
        }

        _flushSub.Dispose();
        _flushRequests.Dispose();
        _disposeCts.Cancel();

        if (disposeNow)
            DisposeArrangement();
    }

    private void RebuildAsync()
    {
        Clear();
        foreach (var index in _indexes)
            foreach (var (shapeE, polyline) in index.Polylines)
                UpsertShape(shapeE, polyline);
    }

    private void RebuildSourceShapes()
    {
        _sourceShapes.Clear();
        foreach (var index in _indexes)
            foreach (var shapeE in index.Polylines.Keys)
                _sourceShapes.Add(shapeE);
    }

    // Bypass ReactiveProperty's equality dedup by going through OnNext directly:
    // _arr is the same reference each time, but downstream consumers must still be re-triggered.
    private void NotifyReady() => _arrReady.OnNext(_arr);
    private void NotifyNotReady() => _arrReady.OnNext(null);

    private void UpsertShape(Entity shapeE, IndexedPolyline polyline)
    {
        NotifyNotReady();
        lock (_gate)
        {
            if (IsDisposed) return;
            _pendingChanges[shapeE] = new(PendingPolylineAction.Upsert, polyline);
        }
        _flushRequests.OnNext(Unit.Default);
    }

    private void RemoveShape(Entity shapeE)
    {
        NotifyNotReady();
        lock (_gate)
        {
            if (IsDisposed) return;
            _pendingChanges[shapeE] = new(PendingPolylineAction.Remove, default);
        }
        _flushRequests.OnNext(Unit.Default);
    }

    private void Clear()
    {
        NotifyNotReady();
        lock (_gate)
        {
            if (IsDisposed) return;
            _pendingClear = true;
            _pendingChanges.Clear();
        }
        _flushRequests.OnNext(Unit.Default);
    }

    private void FlushPendingChanges()
    {
        bool startDrain;
        lock (_gate)
        {
            if (IsDisposed || (!_pendingClear && _pendingChanges.Count == 0))
                return;

            startDrain = !_drainRunning;
            if (startDrain)
                _drainRunning = true;
        }

        if (startDrain)
            _ = RunDrainBatchesAsync();
    }

    private async Task RunDrainBatchesAsync()
    {
        Exception fault = null;
        try
        {
            await DrainBatchesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            fault = ex;
        }

        lock (_gate)
            _drainRunning = false;
        FrameProviderDispatcher.Post(() =>
        {
            UpdateMainThreadState();
            if (fault != null)
                GD.PushError($"ArrangementManager drain faulted: {fault}");
        });
    }

    private async Task DrainBatchesAsync()
    {
        while (true)
        {
            bool clear;
            List<WorkerPolylineChange> changes;
            lock (_gate)
            {
                if (IsDisposed) return;
                if (!_pendingClear && _pendingChanges.Count == 0) return;

                clear = _pendingClear;
                changes = TakePendingChanges();
                _pendingClear = false;
                _pendingChanges.Clear();
            }

            try
            {
                await NativeConcurrency.WaitAsync(_disposeCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await Task.Run(() => ApplyBatch(clear, changes)).ConfigureAwait(false);
            }
            finally
            {
                NativeConcurrency.Release();
            }
        }
    }

    private List<WorkerPolylineChange> TakePendingChanges()
    {
        List<WorkerPolylineChange> changes = [];
        foreach (var pair in _pendingChanges)
        {
            changes.Add(new WorkerPolylineChange(
                pair.Value.Action,
                pair.Key.PackedValue,
                pair.Value.Polyline.Positions));
        }
        return changes;
    }

    private void ApplyBatch(bool clear, IReadOnlyList<WorkerPolylineChange> changes)
    {
        if (clear)
        {
            if (IsDisposed)
                return;
            _arr.Clear();
        }

        foreach (var change in changes)
        {
            if (IsDisposed)
                return;

            switch (change.Action)
            {
                case PendingPolylineAction.Upsert:
                    _arr.SetPolyline(change.Id, change.Positions);
                    break;
                case PendingPolylineAction.Remove:
                    _arr.RemovePolyline(change.Id);
                    break;
            }
        }
    }

    private void UpdateMainThreadState()
    {
        bool shouldDispose;
        bool shouldNotifyReady;

        lock (_gate)
        {
            shouldDispose = IsDisposed && !_drainRunning && !_arrDisposed;
            if (shouldDispose)
                _arrDisposed = true;
            shouldNotifyReady = !IsDisposed
                && !_drainRunning
                && !_pendingClear
                && _pendingChanges.Count == 0;
        }

        if (shouldDispose)
            DisposeArrangement();
        else if (shouldNotifyReady)
            NotifyReady();
    }

    private void DisposeArrangement()
    {
        _arr.Dispose();
        _disposeCts.Dispose();
        _arrReady.Dispose();
    }

    private bool IsDisposed => _disposeCts.IsCancellationRequested;

    private enum PendingPolylineAction
    {
        Upsert,
        Remove,
    }

    private readonly record struct PendingPolylineChange(
        PendingPolylineAction Action,
        IndexedPolyline Polyline);

    private readonly record struct WorkerPolylineChange(
        PendingPolylineAction Action,
        long Id,
        ImmutableArray<Vector2> Positions);

}
