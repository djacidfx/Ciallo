using System;
using System.Collections.Generic;
using Frent;
using ObservableCollections;

namespace Ciallo.GuiControl;

internal static class TimelineFrameRetiming
{
    public static bool InsertFramesWouldChange(ObservableSortedList<int, Entity> exposures, int frame, int frameCount) =>
        WouldRetimeChange(exposures, key => MapInsert(key, frame, frameCount), frameCount);

    public static bool DeleteFramesWouldChange(ObservableSortedList<int, Entity> exposures, int frame, int frameCount) =>
        WouldRetimeChange(exposures, key => MapDelete(key, frame, frameCount), frameCount);

    public static void InsertFrames(ObservableSortedList<int, Entity> exposures, int frame, int frameCount) =>
        Retime(exposures, key => MapInsert(key, frame, frameCount), frameCount);

    public static void DeleteFrames(ObservableSortedList<int, Entity> exposures, int frame, int frameCount) =>
        Retime(exposures, key => MapDelete(key, frame, frameCount), frameCount);

    public static int MapInsert(int marker, int frame, int frameCount)
    {
        if (frameCount <= 0) return marker;
        return marker >= frame ? marker + frameCount : marker;
    }

    public static int MapDelete(int marker, int frame, int frameCount)
    {
        if (frameCount <= 0) return marker;

        int end = frame + frameCount;
        if (marker < frame) return marker;
        return marker < end ? frame : marker - frameCount;
    }

    private static bool WouldRetimeChange(
        ObservableSortedList<int, Entity> exposures,
        Func<int, int> mapFrame,
        int frameCount)
    {
        if (exposures == null || exposures.Count == 0 || frameCount <= 0)
            return false;

        var mapped = BuildMappedEntries(exposures, mapFrame);
        return !HasSameEntries(exposures, mapped);
    }

    private static void Retime(
        ObservableSortedList<int, Entity> exposures,
        Func<int, int> mapFrame,
        int frameCount)
    {
        if (exposures == null || exposures.Count == 0 || frameCount <= 0)
            return;

        var originalKeys = new List<int>();
        foreach (var kv in exposures)
            originalKeys.Add(kv.Key);

        var mapped = BuildMappedEntries(exposures, mapFrame);
        if (HasSameEntries(exposures, mapped))
            return;

        for (int i = originalKeys.Count - 1; i >= 0; i--)
            exposures.Remove(originalKeys[i]);

        foreach (var kv in mapped)
            exposures.Add(kv.Key, kv.Value);
    }

    private static SortedDictionary<int, Entity> BuildMappedEntries(
        ObservableSortedList<int, Entity> exposures,
        Func<int, int> mapFrame)
    {
        var mapped = new SortedDictionary<int, Entity>();
        foreach (var kv in exposures)
            mapped[mapFrame(kv.Key)] = kv.Value;
        return mapped;
    }

    private static bool HasSameEntries(
        ObservableSortedList<int, Entity> exposures,
        SortedDictionary<int, Entity> mapped)
    {
        if (exposures.Count != mapped.Count)
            return false;

        int index = 0;
        foreach (var kv in mapped)
        {
            if (exposures.GetKeyAtIndex(index) != kv.Key ||
                exposures.GetValueAtIndex(index) != kv.Value)
                return false;
            index++;
        }

        return true;
    }
}
