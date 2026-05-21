using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class TimelineAction : Container
{
    public Entity Document;

    public override void _Ready()
    {
        AddCelFolder.Pressed += OnAddCelFolder;
        NewAnimationCel.Pressed += OnNewAnimationCel;
    }

    public void Init(Entity document)
    {
        Document = document;
        var sm = document.Get<SelectionManager>();
        NewAnimationCel.VisibleIf(sm.WorkingCelFolder, e => !e.IsNull).AddTo(document);
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
                if (cursor.Get<FolderLayerSetting>().IsCel)
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
            .Commit();
    }

    private void OnNewAnimationCel()
    {
        var celFolder = Document.Get<SelectionManager>().WorkingCelFolder.CurrentValue;
        if (celFolder.IsNull) return;

        int currentFrame = Document.Get<SelectionManager>().CurrentFrame.Value;
        (int frame, string name) = GetNewAnimationCelFrameName(celFolder, currentFrame);
        var cel = Document.World.Create();

        new CommandBuilder(cel)
            .NewShapeLayer()
            .SetProperty(e => e.Get<CommonLayerSetting>().Name, name)
            .AddToLayerTree(celFolder)
            .SetWorkingLayer()
            .SetTarget(celFolder)
            .SetObservableCollection(
                e => e.Get<FolderLayerSetting>().Exposures,
                exposures => exposures.Add(frame, cel))
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
        if (candidate < 0)
            candidate = 0;

        for (int distance = 0; ; distance++)
        {
            int earlier = candidate - distance;
            if (earlier >= 0 && !exposures.ContainsKey(earlier))
                return earlier;

            if (distance == 0) continue;

            int later = candidate + distance;
            if (!exposures.ContainsKey(later))
                return later;
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
        for (int number = baseNumber; ; number++)
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
        for (int i = 1; ; i++)
        {
            string candidate = $"{number}_{i}";
            if (!usedNames.Contains(candidate))
                return candidate;
        }
    }
}