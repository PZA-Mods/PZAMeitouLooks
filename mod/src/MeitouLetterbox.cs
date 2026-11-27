// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.7.3: PZA Letterbox drives MEITOU'S OWN Letterbox where Meitou has one (0.24.1 and newer: its post chain's last pass, a normal Graphics
// setting), so the bars are never drawn twice (the user, 2026-11-27: "our Letterbox does not need to overlap Meitou's ... allow PostFX to be
// another avenue to control Meitou's Letterbox").
//   - Found at run time by reflection on ModContext.Graphics (ModGraphics: Override / Release / Get / Settings), because this mod is built
//     against Meitou 0.20.1 and loads on every Meitou from 0.20.1 up. It counts only when Graphics.Settings lists BOTH keys below.
//   - A named preset or Custom: Override "Meitou: Letterbox" = 1 and "Letterbox aspect" = our aspect, held to Meitou's 1.33..2.76.
//     Meitou's bars are crisp and solid, so Soft draws Meitou's 2.39:1 bars (no feather), and our Edge softness / Strength sliders hide.
//   - Off: Release both, so the player's own Meitou Letterbox setting rules again. Nothing of ours is saved into Meitou's settings or presets
//     (ModGraphics lays the values over the player's look only while the world is drawn).
//   - Older Meitou (no Graphics, or no Letterbox in it): our own LetterboxPass draws the bars as in 0.7.0, the only Letterbox there is.
// Called from the host's thread: the settings group's Changed, LooksApi, the start of the mod.
namespace PZA.Looks.Meitou;

public static class MeitouLetterbox
{
    /// <summary>Meitou's keys (GraphicsOverrides: "Meitou: " + WorldFrame.LetterboxSwitch, and WorldFrame.LetterboxAspectSlider).</summary>
    public const string SwitchKey = "Meitou: Letterbox", AspectKey = "Letterbox aspect";

    /// <summary>Meitou's own range (Post.Letterbox.MinAspect / MaxAspect, 0.24.1): the 4:3 to Ultra Panavision shapes it takes.</summary>
    public const float MinAspect = 1.33f, MaxAspect = 2.76f;

    /// <summary>How the calls reach Meitou: Override(key, value) / Release(key). The offline check and the fuzzer pass their own.</summary>
    public sealed record Door(Func<string, float, bool> Override, Func<string, bool> Release);

    static Door? s_door;
    static (bool On, float Aspect)? s_held;   // what we last laid over Meitou's (null = nothing held)
    static bool s_softLogged;

    /// <summary>True when Meitou draws the bars and ours stays off.</summary>
    public static bool Active => s_door is not null;

    /// <summary>What Meitou's Letterbox should show for a PZA Letterbox (null look = Off: release). The aspect is finite and in Meitou's range.</summary>
    public static (bool On, float Aspect)? Plan(LetterboxLook? look)
    {
        if (look is null) return null;
        float a = float.IsFinite(look.Aspect) ? look.Aspect : 2.39f;
        return (true, MathF.Round(Math.Clamp(a, MinAspect, MaxAspect) * 100) / 100);   // Meitou's slider rounds to 0.01 itself
    }

    /// <summary>Finds ModContext.Graphics with the Letterbox keys; false (our pass stays) on an older Meitou. Logs either way.</summary>
    public static bool Bind(object? context)
    {
        s_door = null;
        s_held = null;
        try
        {
            var graphics = context?.GetType().GetProperty("Graphics")?.GetValue(context);
            if (graphics is null) { Log.Line("letterbox: this Meitou has no Letterbox of its own for mods (ModContext.Graphics): PZA Letterbox draws its own bars"); return false; }
            var t = graphics.GetType();
            var keys = t.GetProperty("Settings")?.GetValue(graphics) as IEnumerable<string>;
            var ov = t.GetMethod("Override", [typeof(string), typeof(float)]);
            var rel = t.GetMethod("Release", [typeof(string)]);
            bool keysOk = keys is not null && keys.Contains(SwitchKey) && keys.Contains(AspectKey);
            if (!keysOk || ov?.ReturnType != typeof(bool) || rel?.ReturnType != typeof(bool))
            {
                Log.Line("letterbox: Meitou's graphics overrides have no Letterbox (keys '" + SwitchKey + "', '" + AspectKey + "'): PZA Letterbox draws its own bars");
                return false;
            }
            MeitouLooks.Keys(keys!.ToList());   // 0.7.3: the Meitou look sets only what this Meitou lists
            return Use(new Door((k, v) => (bool)ov!.Invoke(graphics, [k, v])!, k => (bool)rel!.Invoke(graphics, [k])!),
                "letterbox: Meitou has its own Letterbox: PZA Letterbox drives it (no second set of bars); Off gives the player's Meitou setting back");
        }
        catch (Exception e)
        {
            s_door = null;
            Log.Line("letterbox: looking for Meitou's Letterbox failed (" + e.GetType().Name + ": " + e.Message + "): PZA Letterbox draws its own bars");
            return false;
        }
    }

    /// <summary>Uses a door (Bind's, or the offline check's / fuzzer's own). Null: back to our own pass.</summary>
    public static bool Use(Door? door, string? line = null)
    {
        s_door = door;
        s_held = null;
        if (door is null) MeitouLooks.Keys(null);
        if (line is not null) Log.Line(line);
        return door is not null;
    }

    /// <summary>Lays our Letterbox over Meitou's (or releases it) when it changed. Returns what is held now.</summary>
    public static (bool On, float Aspect)? Sync(LooksSettings s)
    {
        if (s_door is not { } door) return null;
        try { MeitouLooks.Sync(s, door); }   // 0.7.3: the Meitou look rides the same door
        catch (Exception e) { Log.Line("meitou look: failed (" + e.GetType().Name + ": " + e.Message + ")"); }
        var look = LookValues.Letterbox(s);
        var want = Plan(look);
        if (want == s_held) return s_held;
        try
        {
            if (want is { } w)
            {
                bool ok = door.Override(SwitchKey, 1f) & door.Override(AspectKey, w.Aspect);
                Log.Line($"letterbox: Meitou's Letterbox on at {w.Aspect:0.00}:1 for '{look!.Name}'" + (ok ? "" : " (Meitou refused a value)"));
                if (look.Softness > 0 && !s_softLogged)
                {
                    s_softLogged = true;
                    Log.Line("letterbox: Meitou's bars are crisp and solid: Edge softness and Strength apply only where PZA Letterbox draws its own bars");
                }
            }
            else
            {
                door.Release(SwitchKey);
                door.Release(AspectKey);
                Log.Line("letterbox: Off: Meitou's Letterbox is the player's own setting again");
            }
            s_held = want;
        }
        catch (Exception e)
        {
            Log.Line("letterbox: driving Meitou's Letterbox failed (" + e.GetType().Name + ": " + e.Message + ")");
        }
        return s_held;
    }
}
