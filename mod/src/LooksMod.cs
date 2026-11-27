// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// The code mod's entry (meitou.json "entry"): the settings group, then the members and the patch. Anything missing: the mod stays off with a
// line saying why, and the game goes on with its own picture.
using System.Reflection;
using Meitou.Modding;

namespace PZA.Looks.Meitou;

public sealed class LooksMod : IMod
{
    public void Initialize(ModContext context)
    {
        Log.Sink = context.Log;
        LooksApi.Group = context.AddSettings(PixelPass.Settings);   // saved values are applied before this returns
        // 0.2.2: Meitou keeps only the Settings page's own changes in meitou.user.json; a change made through LooksApi (PZA's Camera's Tilt-Shift
        // buttons) was lost at the next start. PZA's Meitou UI 0.3.1 keeps every change in <mod folder>\pza.looks.settings.json and puts it back here.
        PZA.MeitouUi.PzaUi.Log = s => Log.Line(s);
        // 0.2.3: a test run (any PZA_LOOKS_* variable) keeps NOTHING. In 0.2.2 a button click during a test saved every value, the test's
        // overrides included, over the player's own (2026-11-17: the user's Pixel 2 + Diorama + Compare came back as Pixel 1 + Off).
        if (IsTestRun()) Log.Line("test run (PZA_LOOKS_* set): settings changed in this run are not kept");
        else
        {
            var kept = PZA.MeitouUi.PzaSettings.Keep(LooksApi.Group, context.Folder);
            foreach (var line in kept) Log.Line(line);
        }
        // 0.4.0: a preset fills its sliders, a slider makes its list Custom; named presets put their values back now
        LookValues.Hook(LooksApi.Group, PixelPass.Settings);
        TestOverrides();
        string version = typeof(LooksMod).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        int plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus];
        Log.Line($"{version} for Meitou, by PZA: look '{PixelPass.Settings.Look}', Tilt-Shift '{PixelPass.Settings.TiltShift}', Fisheye '{PixelPass.Settings.Fisheye}', Outline '{PixelPass.Settings.Outline}', Kuwahara '{PixelPass.Settings.Kuwahara}', N64 '{PixelPass.Settings.N64}', "
            + $"Toon '{PixelPass.Settings.Toon}' (Settings > Mods)");
        // 0.7.0: named preset manager window (PZA's Meitou UI), opened from Settings > Mods or F10 > Mods
        PresetsWindow.Setup(context.Folder);
        PZA.MeitouUi.PzaUi.Install(context);
        context.AddDebugCommand("PZA's Post-FX: Presets", _ => { PresetsWindow.Open(); return "opened PZA's Post-FX Presets window"; });
        // 0.5.0: Toon lives in Meitou's lighting shaders, which compile once: frozen now for this game (a change applies at the next start)
        try { Log.Line(ToonPatch.Apply(context.Harmony, LookValues.Toon(PixelPass.Settings))); }
        catch (Exception e) { Log.Line("toon: stays OFF: patching failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        Start(context.Harmony);
    }

    /// <summary>For screenshot runs: PZA_LOOKS_LOOK (a look's name or Off) and PZA_LOOKS_SPLIT (1/0) set this run's values without the
    /// Settings page; logged. Nothing is written back.</summary>
    /// <summary>Whether this run was started for a test (any PZA_LOOKS_* environment variable set, even empty).</summary>
    public static bool IsTestRun() => Environment.GetEnvironmentVariables().Keys.Cast<object>()
        .Any(k => k.ToString()!.StartsWith("PZA_LOOKS_", StringComparison.OrdinalIgnoreCase));

    static void TestOverrides()
    {
        // 0.6.0: PZA_LOOKS_COMBO first (it sets the other lists; a single list's variable below still wins)
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_COMBO") is { Length: > 0 } combo)
        {
            if (Combos.Names.Contains(combo, StringComparer.Ordinal) && combo != Looks.Custom)
            {
                PixelPass.Settings.Combo = combo;
                LookValues.ApplyCombo(null, PixelPass.Settings);
            }
            Log.Line($"PZA_LOOKS_COMBO={combo}: combo for this run '{PixelPass.Settings.Combo}'");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_LOOK") is { Length: > 0 } look)
        {
            if (look == Looks.Off || Looks.Find(look) is not null) PixelPass.Settings.Look = look;
            LookValues.ApplyPreset(null, PixelPass.Settings, nameof(LooksSettings.Look));
            Log.Line($"PZA_LOOKS_LOOK={look}: look for this run '{PixelPass.Settings.Look}'");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_SPLIT") is { Length: > 0 } split)
        {
            PixelPass.Settings.Split = split == "1";
            Log.Line($"PZA_LOOKS_SPLIT={split}: compare {(PixelPass.Settings.Split ? "on" : "off")} for this run");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_TILT") is { Length: > 0 } tilt)
        {
            if (tilt == Looks.Off || Tilts.Find(tilt) is not null) PixelPass.Settings.TiltShift = tilt;
            LookValues.ApplyPreset(null, PixelPass.Settings, nameof(LooksSettings.TiltShift));
            Log.Line($"PZA_LOOKS_TILT={tilt}: Tilt-Shift for this run '{PixelPass.Settings.TiltShift}'");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_FISHEYE") is { Length: > 0 } fish)
        {
            if (fish == Looks.Off || Fisheyes.Find(fish) is not null) PixelPass.Settings.Fisheye = fish;
            LookValues.ApplyPreset(null, PixelPass.Settings, nameof(LooksSettings.Fisheye));
            Log.Line($"PZA_LOOKS_FISHEYE={fish}: Fisheye for this run '{PixelPass.Settings.Fisheye}'");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_OUTLINE") is { Length: > 0 } outline)
        {
            if (outline == Looks.Off || Outlines.Find(outline) is not null) PixelPass.Settings.Outline = outline;
            LookValues.ApplyPreset(null, PixelPass.Settings, nameof(LooksSettings.Outline));
            Log.Line($"PZA_LOOKS_OUTLINE={outline}: Outline for this run '{PixelPass.Settings.Outline}'");
        }
        // 0.5.0: Kuwahara, N64, Toon by name for this run (as PZA_LOOKS_OUTLINE)
        foreach (var (env, list, names) in new[] { ("PZA_LOOKS_KUWAHARA", nameof(LooksSettings.Kuwahara), Kuwaharas.Names),
                     ("PZA_LOOKS_N64", nameof(LooksSettings.N64), N64s.Names), ("PZA_LOOKS_TOON", nameof(LooksSettings.Toon), Toons.Names),
                     ("PZA_LOOKS_NOIR", nameof(LooksSettings.Noir), Noirs.Names), ("PZA_LOOKS_HALFTONE", nameof(LooksSettings.Halftone), Halftones.Names),
                     ("PZA_LOOKS_SCREEN", nameof(LooksSettings.Screen), Screens.Names) })
        {
            if (Environment.GetEnvironmentVariable(env) is not { Length: > 0 } pick) continue;
            var prop = typeof(LooksSettings).GetProperty(list)!;
            if (names.Contains(pick, StringComparer.Ordinal) && pick != Looks.Custom) prop.SetValue(PixelPass.Settings, pick);
            LookValues.ApplyPreset(null, PixelPass.Settings, list);
            Log.Line($"{env}={pick}: {list} for this run '{prop.GetValue(PixelPass.Settings)}'");
        }
        // PZA_LOOKS_SET="PxSize=6;OlThickness=2": any slider for this run (screenshots of a Custom look), by key; logged, never kept
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_SET") is { Length: > 0 } sets)
            foreach (var pair in sets.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Log.Line($"PZA_LOOKS_SET {pair}: {TestSet(pair)}");
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_OUTLINE_DEBUG") == "1")
        {
            PixelPass.OutlineMask = true;
            Log.Line("PZA_LOOKS_OUTLINE_DEBUG=1: the Outline shows its lines alone (black on white) for this run");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_TILT_DEBUG") == "1")
        {
            PixelPass.TiltMask = true;
            Log.Line("PZA_LOOKS_TILT_DEBUG=1: Tilt-Shift shows its blur mask (black = sharp) for this run");
        }
    }

    /// <summary>One "Key=value" for a test run, straight onto the property (no group: nothing saved); the list turns Custom.</summary>
    static string TestSet(string pair)
    {
        int eq = pair.IndexOf('=');
        if (eq <= 0) return "ignored (want Key=value)";
        string key = pair[..eq], text = pair[(eq + 1)..];
        var p = typeof(LooksSettings).GetProperty(key);
        if (p is null || !p.CanWrite || LookValues.ListOf(key) is not { } list) return "ignored (no such slider)";
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        object? v = p.PropertyType == typeof(float) && float.TryParse(text, System.Globalization.NumberStyles.Float, inv, out float f) && float.IsFinite(f) ? f
            : p.PropertyType == typeof(int) && int.TryParse(text, System.Globalization.NumberStyles.Integer, inv, out int i) ? i
            : p.PropertyType == typeof(bool) && bool.TryParse(text, out bool b) ? b
            : p.PropertyType == typeof(string) ? text : null;
        if (v is null) return "ignored (not a value for it)";
        p.SetValue(PixelPass.Settings, v);
        var listProp = typeof(LooksSettings).GetProperty(list)!;
        if ((string)listProp.GetValue(PixelPass.Settings)! != Looks.Off) listProp.SetValue(PixelPass.Settings, Looks.Custom);
        return $"set for this run ({list} '{listProp.GetValue(PixelPass.Settings)}')";
    }

    /// <summary>Resolves the game's members and patches; false (with a line) when anything is missing. Public for the offline check.</summary>
    public static bool Start(HarmonyLib.Harmony harmony)
    {
        var missing = Access.ResolveAll();
        if (missing.Count > 0)
        {
            Log.Line("stays OFF: the game lacks " + string.Join("; ", missing) + " (built for Meitou 0.20.1)");
            return false;
        }
        try
        {
            Log.Line(PixelPass.Apply(harmony));
            return true;
        }
        catch (Exception e)
        {
            Log.Line("stays OFF: patching failed (" + e.GetType().Name + ": " + e.Message + ")");
            return false;
        }
    }
}

public static class Log
{
    public static Action<string>? Sink;

    public static void Line(string s)
    {
        if (Sink is { } sink) sink(s);
        else System.Diagnostics.Debug.WriteLine("[pza.looks] " + s);
    }
}
