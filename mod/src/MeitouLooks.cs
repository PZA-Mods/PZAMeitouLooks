// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.7.3: "Meitou look": named sets of MEITOU'S OWN picture settings (tone map, grade, bloom, depth of field, vignette, film grain, light shafts,
// exposure) that PZA's Post-FX lays over the player's values through the same bridge as the Letterbox (ModContext.Graphics, Meitou 0.24.1+;
// MeitouLetterbox.cs). Nothing is written into Meitou's settings, saved file or look presets: Off (or another set) releases what we held and
// the player's own values show again. On older Meitou the list is hidden and does nothing.
// Keys and ranges as Meitou 0.24.1-nightly.20261127's GraphicsOverrides / WorldFrame.AddLookSliders; a key a later Meitou drops is skipped
// (logged once), never sent.
namespace PZA.Looks.Meitou;

public sealed record MeitouLook(string Name, (string Key, float Value)[] Values);

public static class MeitouLooks
{
    // Meitou's keys: a slider's label, or "Meitou: " + a switch's label (switch: 1 on, 0 off)
    public const string ToneMap = "Tone map (0 = game)", Grading = "Grading (0 = game)", Bloom = "Meitou: Bloom", BloomStrength = "Bloom strength (0 = game)",
        Dof = "Meitou: Depth of field", DofAperture = "Depth of field aperture (f-number)", Vignette = "Meitou: Vignette", VignetteStrength = "Vignette strength",
        Grain = "Meitou: Film grain", GrainStrength = "Film grain strength", Shafts = "Light shafts strength", Exposure = "Adaptive exposure strength (max x)";

    /// <summary>Tone map slider values (Meitou's ToneMapOperator): 1 shoulder, 2 ACES, 3 hybrid (clamp + ACES).</summary>
    public const float Shoulder = 1, Aces = 2, Hybrid = 3;

    public static readonly MeitouLook[] All =
    [
        // for the stylised looks (anime, pixel, comic): Meitou's lens effects off so they do not fight ours
        new("PZA Meitou - Clean", [(Vignette, 0), (Grain, 0), (Dof, 0)]),
        // as close to a photograph as Meitou's own chain goes: grade, a little bloom, soft vignette, fine grain, light light shafts. Headless shot
        // 2026-11-27 (0.24.1 nightly): overriding the tone map (hybrid) and exposure (1.5) and 0.6 shafts came out darker, greyer and hazier
        // than the plain picture, so the player's tone map and exposure stay and the shafts are halved
        new("PZA Meitou - Realistic", [(Grading, 1), (Bloom, 1), (BloomStrength, 0.04f), (Dof, 0), (Vignette, 1), (VignetteStrength, 0.15f),
            (Grain, 1), (GrainStrength, 0.05f), (Shafts, 0.3f)]),
        // a film camera: ACES, bloom, a shallow depth of field, a stronger vignette and grain, strong light shafts
        // (headless shot 2026-11-27: 0.7 shafts washed it milky and f/2.8 blurred most of the ground: 0.35 and f/4)
        new("PZA Meitou - Cinematic", [(ToneMap, Aces), (Grading, 1), (Bloom, 1), (BloomStrength, 0.06f), (Dof, 1), (DofAperture, 4f), (Vignette, 1),
            (VignetteStrength, 0.35f), (Grain, 1), (GrainStrength, 0.15f), (Shafts, 0.35f)]),
        // an old print: heavy grain and vignette (pairs with Film Noir and VHS)
        new("PZA Meitou - Film", [(ToneMap, Aces), (Grading, 1), (Bloom, 1), (BloomStrength, 0.08f), (Dof, 0), (Vignette, 1), (VignetteStrength, 0.5f),
            (Grain, 1), (GrainStrength, 0.4f)]),
    ];

    public static readonly string[] Names = [Looks.Off, .. All.Select(l => l.Name)];

    public static MeitouLook? Find(string? name) => All.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.Ordinal));

    static readonly Dictionary<string, float> s_held = new(StringComparer.Ordinal);
    static readonly HashSet<string> s_skipped = new(StringComparer.Ordinal);
    static IReadOnlyCollection<string>? s_keys;

    /// <summary>The keys this Meitou takes (Graphics.Settings), from MeitouLetterbox.Bind; null = take every key (the offline check's door).</summary>
    public static void Keys(IReadOnlyCollection<string>? keys) { s_keys = keys; s_held.Clear(); }

    /// <summary>What the door holds for us now (the offline check and the fuzzer read it).</summary>
    public static IReadOnlyDictionary<string, float> Held => s_held;

    /// <summary>The values a choice asks Meitou for: its set, finite, keys this Meitou knows only. Off or an unknown name: none.</summary>
    public static Dictionary<string, float> Plan(string? choice)
    {
        var want = new Dictionary<string, float>(StringComparer.Ordinal);
        if (Find(choice) is not { } look) return want;
        foreach (var (key, value) in look.Values)
        {
            if (!float.IsFinite(value)) continue;
            if (s_keys is not null && !s_keys.Contains(key))
            {
                if (s_skipped.Add(key)) Log.Line($"meitou look: this Meitou has no '{key}' to set: skipped");
                continue;
            }
            want[key] = value;
        }
        return want;
    }

    /// <summary>Lays the chosen set over Meitou's values through the door: releases what we held that the set no longer has, overrides what changed.</summary>
    public static void Sync(LooksSettings s, MeitouLetterbox.Door door)
    {
        var want = Plan(s.MeitouLook);
        bool changed = false;
        foreach (var key in s_held.Keys.Where(k => !want.ContainsKey(k)).ToList())
        {
            door.Release(key);
            s_held.Remove(key);
            changed = true;
        }
        foreach (var (key, value) in want)
        {
            if (s_held.TryGetValue(key, out float had) && had == value) continue;
            if (door.Override(key, value)) s_held[key] = value;
            else Log.Line($"meitou look: Meitou refused '{key}' = {value}");
            changed = true;
        }
        if (changed)
            Log.Line(want.Count == 0 ? "meitou look: Off: Meitou's picture settings are the player's own again"
                : $"meitou look: '{s.MeitouLook}' laid over Meitou's settings ({want.Count} values)");
    }
}
