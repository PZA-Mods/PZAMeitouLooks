// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.7.3: recommended presets that ship inside the mod (a player on Discord, 2026-11-27: "too much work to move sliders, so being able to pick a
// preset and see if I like that look would be a good sell"; the user: "build the recommended presets"). Built in code from the named
// presets, so they always match this build's effects; listed first in the Presets window as "[PZA] ...", loaded like a saved preset, and
// never saved over or deleted. The Meitou look part (Realistic, Cinematic...) applies on Meitou 0.24.1+ only; older Meitou gets the rest.
namespace PZA.Looks.Meitou;

public static class RecommendedPresets
{
    public const string Prefix = "[PZA] ";

    /// <summary>One recommended look: a combo (or none), single lists set after it, and the Meitou look.</summary>
    sealed record Recipe(string Name, string Blurb, string? Combo, (string List, string Preset)[] Lists, string MeitouLook);

    static readonly Recipe[] Recipes =
    [
        new("Anime", "cel lighting, ink lines, soft painted shading", null,
            [(nameof(LooksSettings.Toon), "PZA Toon - Anime"), (nameof(LooksSettings.Outline), "PZA Outline - Ink"), (nameof(LooksSettings.Kuwahara), "PZA Kuwahara - Soft")],
            "PZA Meitou - Clean"),
        new("Hyper Realistic", "no stylising: Meitou's own photographic chain and a faint lens fringe", null,
            [(nameof(LooksSettings.Screen), "PZA Screen - Lens Fringe")], "PZA Meitou - Realistic"),
        new("Cinematic", "2.39:1 bars, shallow focus, film grain", null,
            [(nameof(LooksSettings.Letterbox), "PZA Letterbox - 2.39:1 Cinema"), (nameof(LooksSettings.Screen), "PZA Screen - Lens Fringe")], "PZA Meitou - Cinematic"),
        new("Painted Miniature", "a painted tabletop diorama", "PZA Combo - Painted Miniature", [], "PZA Meitou - Clean"),
        new("Comic Book", "cel lighting, ink lines, halftone dots", "PZA Combo - Comic Book", [], "PZA Meitou - Clean"),
        new("Film Noir", "black and white on an old print", "PZA Combo - Film Noir", [], "PZA Meitou - Film"),
        new("Retro PS1", "the PlayStation on a CRT", null,
            [(nameof(LooksSettings.Look), "PZA PS1 - CRT"), (nameof(LooksSettings.Screen), "PZA Screen - Default")], "PZA Meitou - Clean"),
        new("16-Bit Miniature", "chunky desert pixel art, tilt-shift", "PZA Combo - 16-Bit Miniature", [], "PZA Meitou - Clean"),
    ];

    static List<PresetData>? s_all;

    /// <summary>Every recommended preset, in the window's order.</summary>
    public static IReadOnlyList<PresetData> All => s_all ??= [.. Recipes.Select(Build)];

    /// <summary>The one-line description shown when it is loaded.</summary>
    public static string Blurb(string name) => Recipes.FirstOrDefault(r => Prefix + r.Name == name)?.Blurb ?? "";

    public static bool IsRecommended(string? name) => name is not null && name.StartsWith(Prefix.Trim(), StringComparison.OrdinalIgnoreCase);

    public static PresetData? Find(string? name) => All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.Ordinal));

    /// <summary>Whether any recommended preset turns Toon on (Toon applies at the next start).</summary>
    public static bool HasToon(PresetData p) => p.Values.TryGetValue(nameof(LooksSettings.Toon), out var t) && t != Looks.Off;

    static PresetData Build(Recipe r)
    {
        var s = new LooksSettings { Look = Looks.Off };
        LookValues.RunSuppressed(() =>
        {
            foreach (var list in LookValues.Lists) typeof(LooksSettings).GetProperty(list)!.SetValue(s, Looks.Off);
        });
        if (r.Combo is { } combo)
        {
            s.Combo = combo;
            LookValues.ApplyCombo(null, s);
        }
        foreach (var (list, preset) in r.Lists)
        {
            typeof(LooksSettings).GetProperty(list)!.SetValue(s, preset);
            LookValues.ApplyPreset(null, s, list);
        }
        s.MeitouLook = r.MeitouLook;
        var data = PresetStore.Capture(Prefix + r.Name, s);
        // Capture sanitises the name; the "[PZA] " prefix survives it (no path characters)
        return data with { Name = Prefix + r.Name };
    }
}
