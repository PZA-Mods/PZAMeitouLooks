// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.7.4: a safe start for the pixel looks (a player's report, 2026-11-27: an AMD RX 6800 XT froze building the pixel-art program at the first
// frame; the look was saved, so EVERY launch froze and it looked like "Meitou won't start"; their idea). Before a pixel-art program is drawn
// for the first time this session we write a marker into the mod folder, and clear it once that frame is drawn. A marker still there at the
// next start means that draw never finished: the Look is set Off (saved) and the log says which look and why. A freeze then costs one launch.
namespace PZA.Looks.Meitou;

public static class SafeStart
{
    public const string FileName = "pza.looks.safestart";

    static string? s_folder;
    static bool s_armed;

    /// <summary>Where the marker lives (the mod folder); null turns the marker off (the offline check without a folder).</summary>
    public static void Folder(string? folder) { s_folder = folder; s_armed = false; }

    public static string? MarkerPath => s_folder is null ? null : Path.Combine(s_folder, FileName);

    /// <summary>Before a new pixel-art program's first draw: write the marker (what is about to be drawn).</summary>
    public static void Arm(string what)
    {
        if (s_armed || MarkerPath is not { } path) return;
        try
        {
            File.WriteAllText(path, what);
            s_armed = true;
        }
        catch (Exception e) { Log.Line("safe start: could not write the marker (" + e.GetType().Name + ": " + e.Message + ")"); }
    }

    /// <summary>After that frame is drawn: clear the marker.</summary>
    public static void Disarm()
    {
        if (!s_armed || MarkerPath is not { } path) return;
        s_armed = false;
        try { File.Delete(path); }
        catch (Exception e) { Log.Line("safe start: could not clear the marker (" + e.GetType().Name + ": " + e.Message + ")"); }
    }

    public static bool Armed => s_armed;

    /// <summary>At the start, before anything is drawn: a marker left by the last start turns the pixel Look Off (through the settings group,
    /// so it is saved) and is cleared. True when it did.</summary>
    public static bool Check(LooksSettings s, global::Meitou.Modding.ModSettingsGroup? group)
    {
        if (MarkerPath is not { } path || !File.Exists(path)) return false;
        string what;
        try { what = File.ReadAllText(path).Trim(); }
        catch { what = "a pixel look"; }
        if (what.Length > 200) what = what[..200];
        try { File.Delete(path); } catch { }
        string was = s.Look;
        var setting = group?.Settings.FirstOrDefault(x => x.Key == nameof(LooksSettings.Look));
        if (setting is not null) setting.Set(Looks.Off);
        else s.Look = Looks.Off;
        Log.Line($"safe start: the last start stopped while the graphics driver built {what} and never drew its first frame "
            + $"(a frozen or closed game). Look '{was}' is set Off so Meitou starts; pick a look again in Settings > Mods to retry it.");
        return true;
    }
}
