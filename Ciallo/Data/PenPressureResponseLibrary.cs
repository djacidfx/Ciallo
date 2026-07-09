using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Newtonsoft.Json.Linq;
using FileAccess = Godot.FileAccess;

namespace Ciallo.Data;

/// <summary>
/// One measured pressure response: a single physical pen unit measured on one tablet in one session.
/// <see cref="Records"/> are (physicalPressureGram, digitalPressure) samples, monotone in X,
/// where digitalPressure is normalized to 0..1 (the source JSON stores it as a 0..100 percent).
/// </summary>
public sealed record PenPressureEntry(
    string Brand,
    string PenEntityId,
    string TabletEntityId,
    string Date,
    ImmutableArray<Vector2> Records)
{
    // Display name = the segment after the last '.', e.g. "huion.pen.pw517" -> "pw517".
    public static string ShortName(string entityId)
    {
        int dot = entityId.LastIndexOf('.');
        return dot >= 0 ? entityId[(dot + 1)..] : entityId;
    }
}

/// <summary>
/// Loads every <c>ExternalData/pressure-response/*.json</c> file into a flat, cached entry list.
/// Loaded lazily on first access and kept for the process lifetime (the data is static, ~1.5 MB).
/// </summary>
public static class PenPressureResponseLibrary
{
    private const string Dir = "res://ExternalData/pressure-response/";

    private static ImmutableArray<PenPressureEntry>? _cache;
    public static ImmutableArray<PenPressureEntry> Entries => _cache ??= Load();

    private static ImmutableArray<PenPressureEntry> Load()
    {
        var result = ImmutableArray.CreateBuilder<PenPressureEntry>();

        using var dir = DirAccess.Open(Dir);
        if (dir == null)
        {
            GD.PushWarning($"Pressure-response directory not found: {Dir}");
            return [];
        }

        foreach (var fileName in dir.GetFiles())
        {
            if (!fileName.EndsWith(".json"))
                continue;
            LoadFile(Dir + fileName, result);
        }

        return result.ToImmutable();
    }

    private static void LoadFile(string path, ImmutableArray<PenPressureEntry>.Builder sink)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning($"Cannot open pressure-response file: {path}");
            return;
        }

        var responses = JObject.Parse(file.GetAsText())["PressureResponse"] as JArray;
        if (responses == null)
            return;

        foreach (var entry in responses)
        {
            if (entry["Records"] is not JArray records)
                continue;

            var samples = ImmutableArray.CreateBuilder<Vector2>(records.Count);
            foreach (var r in records)
                // X = physical gram (kept as-is); Y = digital pressure normalized 0..100 -> 0..1.
                samples.Add(new Vector2((float)r[0], (float)r[1] / 100f));

            sink.Add(new PenPressureEntry(
                Brand: (string)entry["Brand"] ?? "",
                PenEntityId: (string)entry["PenEntityId"] ?? "",
                TabletEntityId: (string)entry["TabletEntityId"] ?? "",
                Date: (string)entry["Date"] ?? "",
                Records: samples.MoveToImmutable()));
        }
    }

    // Cascade helpers ------------------------------------------------------

    public static IReadOnlyList<string> Brands() =>
        Entries.Select(e => e.Brand).Distinct().Order().ToImmutableArray();

    public static IReadOnlyList<string> PensOf(string brand) =>
        Entries.Where(e => e.Brand == brand)
            .Select(e => e.PenEntityId).Distinct().Order().ToImmutableArray();

    public static IReadOnlyList<string> TabletsOf(string brand, string pen) =>
        Entries.Where(e => e.Brand == brand && e.PenEntityId == pen)
            .Select(e => e.TabletEntityId).Distinct().Order().ToImmutableArray();

    public static IReadOnlyList<PenPressureEntry> Match(string brand, string pen, string tablet) =>
        Entries.Where(e => e.Brand == brand && e.PenEntityId == pen && e.TabletEntityId == tablet)
            .ToImmutableArray();

    /// <summary>
    /// The single most recently measured entry for the triple, or null if none match.
    /// Dates are ISO <c>YYYY-MM-DD</c>, so ordinal string ordering is chronological.
    /// </summary>
    public static PenPressureEntry MatchLatest(string brand, string pen, string tablet) =>
        Entries.Where(e => e.Brand == brand && e.PenEntityId == pen && e.TabletEntityId == tablet)
            .MaxBy(e => e.Date, System.StringComparer.Ordinal);
}
