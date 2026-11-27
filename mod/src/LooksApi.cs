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
    public const int Version = 6;   // 6 (0.6.3): + Letterbox names, getter and setter
    // was 5   // 5 (0.6.0): + Noir / Halftone / Screen / Combo names, getters and setters
    // was 4   // 4 (0.5.0): + Kuwahara / N64 / Toon names, getters and setters (Toon applies at the next start)
    // 2 (0.3.0): + FisheyeNames / Fisheye / SetFisheye; 3 (0.4.0): + OutlineNames / Outline / SetOutline,
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

    /// <summary>Version 4: Off, every Kuwahara's name and Custom.</summary>
    public static string[] KuwaharaNames => [.. Kuwaharas.Names];
    public static string Kuwahara => PixelPass.Settings.Kuwahara;
    public static bool SetKuwahara(string name) => Set(nameof(LooksSettings.Kuwahara), name, Kuwaharas.Names);

    /// <summary>Version 4: Off, every N64's name and Custom.</summary>
    public static string[] N64Names => [.. N64s.Names];
    public static string N64 => PixelPass.Settings.N64;
    public static bool SetN64(string name) => Set(nameof(LooksSettings.N64), name, N64s.Names);

    /// <summary>Version 4: Off, every Toon's name and Custom. A Toon set here is SAVED but drawn from the next start (it is baked into
    /// Meitou's lighting shaders when they compile); <see cref="ToonRunning"/> is the one this game draws.</summary>
    public static string[] ToonNames => [.. Toons.Names];
    public static string Toon => PixelPass.Settings.Toon;
    public static string ToonRunning => ToonPatch.Frozen?.Name ?? Looks.Off;
    public static bool SetToon(string name) => Set(nameof(LooksSettings.Toon), name, Toons.Names);

    /// <summary>Version 5: Off, every Noir's name and Custom.</summary>
    public static string[] NoirNames => [.. Noirs.Names];
    public static string Noir => PixelPass.Settings.Noir;
    public static bool SetNoir(string name) => Set(nameof(LooksSettings.Noir), name, Noirs.Names);

    /// <summary>Version 5: Off, every Halftone's name and Custom.</summary>
    public static string[] HalftoneNames => [.. Halftones.Names];
    public static string Halftone => PixelPass.Settings.Halftone;
    public static bool SetHalftone(string name) => Set(nameof(LooksSettings.Halftone), name, Halftones.Names);

    /// <summary>Version 5: Off, every Screen's name (CRT wiggle, lens fringe, VHS) and Custom.</summary>
    public static string[] ScreenNames => [.. Screens.Names];
    public static string Screen => PixelPass.Settings.Screen;
    public static bool SetScreen(string name) => Set(nameof(LooksSettings.Screen), name, Screens.Names);

    /// <summary>Version 6: Off, every Letterbox's name (2.39:1, 2.00:1, 1.85:1, Soft) and Custom.</summary>
    public static string[] LetterboxNames => [.. Letterboxes.Names];
    public static string Letterbox => PixelPass.Settings.Letterbox;
    public static bool SetLetterbox(string name) => Set(nameof(LooksSettings.Letterbox), name, Letterboxes.Names);

    /// <summary>Version 5: Off, every Combo's name and Custom. A combo sets every effect list (Off turns them all off); a combo with a Toon
    /// draws its Toon from the next start.</summary>
    public static string[] ComboNames => [.. Combos.Names];
    public static string Combo => PixelPass.Settings.Combo;
    public static bool SetCombo(string name) => Set(nameof(LooksSettings.Combo), name, Combos.Names);

    static bool Set(string key, string name, string[] allowed)
    {
        if (!allowed.Contains(name, StringComparer.Ordinal)) return false;
        var setting = Group?.Settings.FirstOrDefault(s => s.Key == key);
        if (setting is not null) setting.Set(name);
        else
        {   // no group (the offline check): the preset's sliders as the group's Changed would fill them
            typeof(LooksSettings).GetProperty(key)!.SetValue(PixelPass.Settings, name);
            LookValues.OnChanged(null, PixelPass.Settings, key);
            MeitouLetterbox.Sync(PixelPass.Settings);   // 0.7.3 (with a group, its Changed does this)   // 0.6.0: as the group's Changed (a combo, or a list changed under one)
        }
        return true;
    }
}
