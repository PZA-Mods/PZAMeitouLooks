// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// The door for PZA's other Meitou mods (first: PZA's Camera's Tilt-Shift buttons). Found by type name, so neither mod references the other:
//   var api = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PZA.Looks.Meitou.LooksApi")).FirstOrDefault(t => t is not null);
//   api?.GetMethod("SetTiltShift")?.Invoke(null, ["PZA Miniature"]);
// A change goes through Meitou's settings group (ModSetting.Set), so it is saved and shows on Settings > Mods like a change made there.
// Call it from the interface thread (a window's click); the pass reads the value at the next frame.
using Meitou.Modding;

namespace PZA.Looks.Meitou;

public static class LooksApi
{
    /// <summary>Bumped when a member changes meaning; a caller checks it is at least the version it was written for.</summary>
    public const int Version = 3;   // 2 (0.3.0): + FisheyeNames / Fisheye / SetFisheye; 3 (0.4.0): + OutlineNames / Outline / SetOutline,
                                    // every list ends with "Custom" (the player's own slider values), and a preset set here fills its sliders

    /// <summary>Meitou's settings group for PZA's Post-FX (set by Initialize; null before, then changes go straight to the property).</summary>
    public static ModSettingsGroup? Group;

    /// <summary>Off and every Tilt-Shift's name, in the Settings page's order.</summary>
    public static string[] TiltShiftNames => [.. Tilts.Names];

    /// <summary>The Tilt-Shift now (a name from <see cref="TiltShiftNames"/>).</summary>
    public static string TiltShift => PixelPass.Settings.TiltShift;

    /// <summary>Sets the Tilt-Shift by name ("Off" turns it off); false (nothing changed) for an unknown name.</summary>
    public static bool SetTiltShift(string name) => Set(nameof(LooksSettings.TiltShift), name, Tilts.Names);

    /// <summary>Off and every look's name, in the Settings page's order.</summary>
    public static string[] LookNames => [.. Looks.Names];

    /// <summary>The look now (a name from <see cref="LookNames"/>).</summary>
    public static string Look => PixelPass.Settings.Look;

    /// <summary>Sets the look by name ("Off" turns it off); false (nothing changed) for an unknown name.</summary>
    public static bool SetLook(string name) => Set(nameof(LooksSettings.Look), name, Looks.Names);

    /// <summary>Version 2: Off and every Fisheye's name, in the Settings page's order ("PZA Fisheye", "PZA Fisheye - Round").</summary>
    public static string[] FisheyeNames => [.. Fisheyes.Names];

    /// <summary>Version 2: the Fisheye now (a name from <see cref="FisheyeNames"/>).</summary>
    public static string Fisheye => PixelPass.Settings.Fisheye;

    /// <summary>Version 2: sets the Fisheye by name ("Off" turns it off); false (nothing changed) for an unknown name.</summary>
    public static bool SetFisheye(string name) => Set(nameof(LooksSettings.Fisheye), name, Fisheyes.Names);

    /// <summary>Version 3: Off, every Outline's name and Custom, in the Settings page's order.</summary>
    public static string[] OutlineNames => [.. Outlines.Names];

    /// <summary>Version 3: the Outline now (a name from <see cref="OutlineNames"/>).</summary>
    public static string Outline => PixelPass.Settings.Outline;

    /// <summary>Version 3: sets the Outline by name ("Off" turns it off); false (nothing changed) for an unknown name.</summary>
    public static bool SetOutline(string name) => Set(nameof(LooksSettings.Outline), name, Outlines.Names);

    static bool Set(string key, string name, string[] allowed)
    {
        if (!allowed.Contains(name, StringComparer.Ordinal)) return false;
        var setting = Group?.Settings.FirstOrDefault(s => s.Key == key);
        if (setting is not null) setting.Set(name);
        else
        {   // no group (the offline check): the preset's sliders as the group's Changed would fill them
            typeof(LooksSettings).GetProperty(key)!.SetValue(PixelPass.Settings, name);
            LookValues.ApplyPreset(null, PixelPass.Settings, key);
        }
        return true;
    }
}
