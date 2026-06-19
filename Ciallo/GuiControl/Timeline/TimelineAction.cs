using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class TimelineAction : Container
{
    public Entity Document;
    private SelectionManager _selectionManager;
    private TimelineSetting _timelineSetting;
    private Texture2D _playIcon;
    private Texture2D _stopIcon;
    private readonly ReactiveProperty<bool> _isPlaying = new(false);
    private double _playbackAccumulator;
    public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

    public override void _Ready()
    {
        SetProcess(false);
        _playIcon = PlayStop.Icon;
        _stopIcon = GD.Load<Texture2D>("res://Icon/stop.svg");

        AddCelFolder.Pressed += OnAddCelFolder;
        NewAnimationCel.Pressed += OnNewAnimationCel;
        GoToStart.Pressed += () => NavigateToFrame(GetPlaybackStart());
        PreviousFrame.Pressed += () => NavigateRelative(-1);
        PlayStop.Pressed += TogglePlayback;
        NextFrame.Pressed += () => NavigateRelative(1);
        GoToEnd.Pressed += () => NavigateToFrame(GetPlaybackLastFrame());
    }

    public void Init(Entity document)
    {
        Document = document;
        _selectionManager = document.Get<SelectionManager>();
        _timelineSetting = document.Get<TimelineSetting>();
        var subs = new CompositeDisposable();
        NewAnimationCel.VisibleIf(_selectionManager.WorkingCelFolder, e => !e.IsNull, subs);
        BindCheckButton.BindBool(LoopPlay, _timelineSetting.LoopPlaybackEnabled, subs);
        BindCheckButton.BindBool(OnionSkin, _timelineSetting.OnionSkinEnabled, subs);
        FrameRate.BindNumber(_timelineSetting.FrameRate);
        subs.AddTo(document);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isPlaying.Value) return;
        if (@event is InputEventMouseMotion) return;
        if (@event is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Middle
                or MouseButton.WheelUp
                or MouseButton.WheelDown
                or MouseButton.WheelLeft
                or MouseButton.WheelRight
            }) return;
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton &&
            PlayStop.GetGlobalRect().HasPoint(mouseButton.GlobalPosition))
            return;

        GetViewport().SetInputAsHandled();
    }

    private void NavigateRelative(int frameOffset)
    {
        if (_selectionManager == null) return;
        NavigateToFrame(_selectionManager.CurrentFrame.Value + frameOffset);
    }

    public override void _Process(double delta)
    {
        if (!_isPlaying.Value || _selectionManager == null || _timelineSetting == null) return;

        _playbackAccumulator += delta * Mathf.Max(_timelineSetting.FrameRate.Value, 1f);
        while (_playbackAccumulator >= 1.0 && _isPlaying.Value)
        {
            _playbackAccumulator -= 1.0;
            AdvancePlaybackFrame();
        }
    }

    private void NavigateToFrame(int targetFrame)
    {
        if (_selectionManager == null) return;

        int oldFrame = _selectionManager.CurrentFrame.Value;
        int newFrame = ClampPlaybackFrame(targetFrame);
        if (oldFrame == newFrame) return;

        var cmd = new CommandBuilder()
            .SetProperty(_selectionManager.CurrentFrame, oldFrame, newFrame);

        var newWorkingLayer = _selectionManager.ResolveWorkingLayerForTimelineFrameSelection(newFrame);
        if (!newWorkingLayer.IsNull && newWorkingLayer != _selectionManager.WorkingLayer.Value)
            cmd.SetTarget(newWorkingLayer).SetWorkingLayer();

        cmd.CommitOpenSequence();
    }

    private void TogglePlayback()
    {
        if (_isPlaying.Value)
        {
            SetPlaying(false);
            return;
        }

        StartPlayback();
    }

    private void StartPlayback()
    {
        if (_selectionManager == null || _timelineSetting == null) return;

        int frame = ClampPlaybackFrame(_selectionManager.CurrentFrame.Value);
        if (frame >= GetPlaybackLastFrame())
            frame = GetPlaybackStart();

        SetPlaying(true);
        SetFrameDirect(frame);
    }

    private void AdvancePlaybackFrame()
    {
        int currentFrame = ClampPlaybackFrame(_selectionManager.CurrentFrame.Value);
        int lastFrame = GetPlaybackLastFrame();

        if (currentFrame >= lastFrame)
        {
            if (_timelineSetting.LoopPlaybackEnabled.Value)
            {
                SetFrameDirect(GetPlaybackStart());
                return;
            }

            SetFrameDirect(lastFrame);
            SetPlaying(false);
            return;
        }

        int nextFrame = currentFrame + 1;
        SetFrameDirect(nextFrame);
        if (!_timelineSetting.LoopPlaybackEnabled.Value && nextFrame >= lastFrame)
            SetPlaying(false);
    }

    private void SetFrameDirect(int newFrame)
    {
        _selectionManager.CurrentFrame.Value = newFrame;
    }

    private int ClampPlaybackFrame(int frame) => Mathf.Clamp(frame, GetPlaybackStart(), GetPlaybackLastFrame());

    private int GetPlaybackStart() => _timelineSetting?.PlaybackStart.Value ?? 0;

    private int GetPlaybackLastFrame()
    {
        if (_timelineSetting == null) return 0;
        int start = _timelineSetting.PlaybackStart.Value;
        return Mathf.Max(start, _timelineSetting.PlaybackEnd.Value - 1);
    }

    private void SetPlaying(bool playing)
    {
        bool wasPlaying = _isPlaying.Value;
        if (wasPlaying && !playing)
            SwitchWorkingLayerAfterPlayback();

        _isPlaying.Value = playing;
        _playbackAccumulator = 0.0;
        PlayStop.Icon = playing ? _stopIcon : _playIcon;
        PlayStop.TooltipText = playing ? "Stop playback" : "Play";
        SetProcess(playing);
    }

    private void SwitchWorkingLayerAfterPlayback()
    {
        if (_selectionManager == null) return;

        int currentFrame = _selectionManager.CurrentFrame.Value;
        var newWorkingLayer = _selectionManager.ResolveWorkingLayerForTimelineFrameSelection(currentFrame);
        if (!newWorkingLayer.IsNull && newWorkingLayer != _selectionManager.WorkingLayer.Value)
            new CommandBuilder(newWorkingLayer).SetWorkingLayer().Do();
    }

    private void OnAddCelFolder()
    {
        var folder = Document.World.Create();
        var workingLayer = Document.Get<SelectionManager>().WorkingLayer.Value;
        var cursor = workingLayer.IsNull ? Document : workingLayer;
        Entity firstNonAnimFolder = Entity.Null;
        Entity animFolderParent = Entity.Null;

        while (true)
        {
            if (cursor.Has<FolderLayerSetting>())
            {
                if (cursor.Get<FolderLayerSetting>().IsCelFolder)
                {
                    animFolderParent = cursor.Get<LayerTreeNode>().ParentValue;
                    break;
                }
                if (firstNonAnimFolder.IsNull)
                    firstNonAnimFolder = cursor;
            }
            if (cursor.IsDocument) break;
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        var parent = animFolderParent.IsNull ? firstNonAnimFolder : animFolderParent;

        new CommandBuilder(folder)
            .NewCelFolder()
            .AddToLayerTree(parent)
            .SetWorkingLayer()
            .Commit();
    }

    private void OnNewAnimationCel()
    {
        var celFolder = Document.Get<SelectionManager>().WorkingCelFolder.CurrentValue;
        if (celFolder.IsNull) return;

        int currentFrame = Document.Get<SelectionManager>().CurrentFrame.Value;
        (int frame, string name) = GetNewAnimationCelFrameName(celFolder, currentFrame);
        var celE = Document.World.Create();
        var shapeLayerE = Document.World.Create();

        new CommandBuilder(celE)
            .NewFolderLayer()
            .SetProperty(e => e.Get<CommonLayerSetting>().Name, name)
            .AddToLayerTree(celFolder)
            .SetTarget(shapeLayerE)
            .NewShapeLayer()
            .AddToLayerTree(celE)
            .SetWorkingLayer()
            .SetTarget(celFolder)
            .SetObservableCollection(
                celFolder.Get<FolderLayerSetting>().Exposures,
                exposures => exposures.Add(frame, celE))
            .SetTarget(Document)
            .SetProperty(e => e.Get<SelectionManager>().CurrentFrame, frame)
            .Commit();
    }

    /// <summary>
    /// Picks a frame and cel name for a new cel in <paramref name="celFolder"/>.
    /// </summary>
    public static (int, string) GetNewAnimationCelFrameName(Entity celFolder, int currentFrame)
    {
        var exposures = celFolder.Get<FolderLayerSetting>().Exposures;
        int frame = currentFrame;
        var usedNames = GetUsedCelNames(celFolder);

        if (exposures == null || exposures.Count == 0)
            return (frame, MakeUniqueNumericName(1, usedNames));

        if (exposures.ContainsKey(frame))
            frame = FindRhythmicUnoccupiedFrame(exposures, frame);

        return (frame, GetNewAnimationCelName(exposures, frame, usedNames));
    }

    internal static int FindRhythmicUnoccupiedFrame(ObservableSortedList<int, Entity> exposures, int currentFrame)
    {
        int currentIndex = exposures.FloorIndex(currentFrame);
        int candidate;

        if (currentIndex > 0)
        {
            int previousFrame = exposures.GetKeyAtIndex(currentIndex - 1);
            candidate = currentFrame + currentFrame - previousFrame;
        }
        else if (currentIndex + 1 < exposures.Count)
        {
            int nextFrame = exposures.GetKeyAtIndex(currentIndex + 1);
            candidate = currentFrame + nextFrame - currentFrame;
        }
        else
        {
            candidate = currentFrame + 1;
        }

        return FindNearestUnoccupiedFrame(exposures, candidate);
    }

    internal static int FindNearestUnoccupiedFrame(ObservableSortedList<int, Entity> exposures, int candidate)
    {
        int floorIndex = exposures.FloorIndex(candidate);
        if (floorIndex < 0 || exposures.GetKeyAtIndex(floorIndex) != candidate)
            return candidate;

        for (int distance = 1;; distance++)
        {
            long earlier = (long)candidate - distance;
            if (earlier >= int.MinValue && !exposures.ContainsKey((int)earlier))
                return (int)earlier;

            long later = (long)candidate + distance;
            if (later <= int.MaxValue && !exposures.ContainsKey((int)later))
                return (int)later;
        }
    }

    internal static string GetNewAnimationCelName(
        ObservableSortedList<int, Entity> exposures,
        int frame,
        HashSet<string> usedNames)
    {
        bool hasPrev = TryGetCelLabelNumber(exposures, exposures.FloorIndex(frame), out int prevNumber);
        bool hasNext = TryGetCelLabelNumber(exposures, exposures.CeilingIndex(frame), out int nextNumber);

        if (!hasPrev && !hasNext)
            return MakeUniqueNumericName(1, usedNames);

        if (hasPrev && !hasNext)
            return MakeUniqueNumericName(prevNumber + 1, usedNames);

        if (!hasPrev)
            return MakeUniqueNumericName(nextNumber - 1, usedNames);

        int gap = nextNumber - prevNumber;
        if (gap == 1)
            return MakeUniqueSuffixedName(prevNumber, usedNames);

        return MakeUniqueNumericName(prevNumber + 1, usedNames);
    }

    internal static bool TryGetCelLabelNumber(
        ObservableSortedList<int, Entity> exposures,
        int index,
        out int number)
    {
        number = 0;
        if (index < 0) return false;

        var cel = exposures.GetValueAtIndex(index);
        if (cel.IsNull || !cel.IsAlive || !cel.Has<CommonLayerSetting>())
            return false;

        return TryParseCelLabel(cel.Get<CommonLayerSetting>().Name.Value, out number, out char suffix);
    }

    internal static bool TryParseCelLabel(string label, out int number, out char suffix)
    {
        number = 0;
        suffix = '\0';
        if (string.IsNullOrEmpty(label)) return false;

        int digitCount = 0;
        while (digitCount < label.Length && char.IsDigit(label[digitCount]))
            digitCount++;

        if (digitCount == 0) return false;

        if (digitCount == label.Length)
            return int.TryParse(label, out number);

        if (digitCount != label.Length - 1 || label[^1] is < 'a' or > 'z')
            return false;

        suffix = label[^1];
        return int.TryParse(label[..digitCount], out number);
    }

    internal static HashSet<string> GetUsedCelNames(Entity celFolder)
    {
        var usedNames = new HashSet<string>();
        foreach (var cel in celFolder.Get<LayerTreeNode>().Children)
        {
            if (cel.IsNull || !cel.IsAlive || !cel.Has<CommonLayerSetting>())
                continue;

            var name = cel.Get<CommonLayerSetting>().Name.Value;
            if (!string.IsNullOrEmpty(name))
                usedNames.Add(name);
        }

        return usedNames;
    }

    internal static string MakeUniqueNumericName(int baseNumber, HashSet<string> usedNames)
    {
        for (int number = baseNumber;; number++)
        {
            string candidate = number.ToString();
            if (!usedNames.Contains(candidate))
                return candidate;

            if (number == int.MaxValue)
                return MakeUniqueFallbackName(baseNumber, usedNames);
        }
    }

    internal static string MakeUniqueSuffixedName(int number, HashSet<string> usedNames)
    {
        for (char suffix = 'a'; suffix <= 'z'; suffix++)
        {
            string candidate = $"{number}{suffix}";
            if (!usedNames.Contains(candidate))
                return candidate;
        }

        return number == int.MaxValue
            ? MakeUniqueFallbackName(number, usedNames)
            : MakeUniqueNumericName(number + 1, usedNames);
    }

    internal static string MakeUniqueFallbackName(int number, HashSet<string> usedNames)
    {
        for (int i = 1;; i++)
        {
            string candidate = $"{number}_{i}";
            if (!usedNames.Contains(candidate))
                return candidate;
        }
    }
}