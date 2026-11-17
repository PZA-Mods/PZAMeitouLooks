// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
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
        TestOverrides();
        string version = typeof(LooksMod).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        int plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus];
        Log.Line($"{version} for Meitou, by PZA: look '{PixelPass.Settings.Look}', Tilt-Shift '{PixelPass.Settings.TiltShift}' (Settings > Mods)");
        Start(context.Harmony);
    }

    /// <summary>For screenshot runs: PZA_LOOKS_LOOK (a look's name or Off) and PZA_LOOKS_SPLIT (1/0) set this run's values without the
    /// Settings page; logged. Nothing is written back.</summary>
    /// <summary>Whether this run was started for a test (any PZA_LOOKS_* environment variable set, even empty).</summary>
    public static bool IsTestRun() => Environment.GetEnvironmentVariables().Keys.Cast<object>()
        .Any(k => k.ToString()!.StartsWith("PZA_LOOKS_", StringComparison.OrdinalIgnoreCase));

    static void TestOverrides()
    {
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_LOOK") is { Length: > 0 } look)
        {
            if (look == Looks.Off || Looks.Find(look) is not null) PixelPass.Settings.Look = look;
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
            Log.Line($"PZA_LOOKS_TILT={tilt}: Tilt-Shift for this run '{PixelPass.Settings.TiltShift}'");
        }
        if (Environment.GetEnvironmentVariable("PZA_LOOKS_TILT_DEBUG") == "1")
        {
            PixelPass.TiltMask = true;
            Log.Line("PZA_LOOKS_TILT_DEBUG=1: Tilt-Shift shows its blur mask (black = sharp) for this run");
        }
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
