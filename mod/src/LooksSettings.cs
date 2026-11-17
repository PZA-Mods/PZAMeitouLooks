// PZA's Looks for Meitou. Author: PZA. Licence: GPLv3 or later.
// The Settings > Mods group (MeitouClient docs/modding.md "Settings"): a look by name, and a compare switch. Read by the pass every frame.
// UI strings live in Strings (translatable; en now, ru / zh / ja before a 1.0).
using Meitou.Modding;

namespace PZA.Looks.Meitou;

public sealed class LooksSettings
{
    /// <summary>The look (Off = Meitou's picture untouched).</summary>
    [Setting(Strings.En.LookLabel, Hint = Strings.En.LookHint, Options = ["Off", "PZA Pixel - 1", "PZA Pixel - 2", "PZA Pixel - 3", "PZA - PS1 Main",
        "PZA PS1 - CRT", "PZA Soft 240 - Standard"])]
    public string Look { get; set; } = "PZA Pixel - 1";

    /// <summary>Tilt-Shift, before the look (as in Dust: Tilt-Shift 205, Pixel Art 215). Off = none.</summary>
    [Setting(Strings.En.TiltLabel, Hint = Strings.En.TiltHint, Options = ["Off", "PZA Miniature", "PZA HD-2D", "PZA Diorama"])]
    public string TiltShift { get; set; } = "Off";

    /// <summary>Compare: the left half of the screen as Meitou draws it, the right half with the look.</summary>
    [Setting(Strings.En.SplitLabel, Hint = Strings.En.SplitHint)]
    public bool Split { get; set; }
}

static class Strings
{
    public static class En
    {
        public const string LookLabel = "Look";
        public const string LookHint = "PZA's looks from Dust, by name. Off leaves Meitou's picture as it is. Menus and text stay sharp.";
        public const string TiltLabel = "Tilt-Shift";
        public const string TiltHint = "A real tilt-shift from depth: the character the camera follows stays sharp from feet to head, nearer and farther things blur like a miniature photo. Miniature = a wide sharp band, HD-2D and Diorama = a narrow one.";
        public const string SplitLabel = "Compare (left half without)";
        public const string SplitHint = "Shows the left half of the screen without the look, for a before / after.";
    }
}
