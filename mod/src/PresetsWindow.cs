// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.7.0: Named Preset Manager window on PZA's Meitou UI (real Kenshi window: save, load, overwrite, delete presets in <mod folder>\presets\,
// and generate/import compact PZAFX1:... share codes for Discord). Opened from Settings > Mods ("Presets manager") or F10 > Mods.
using Meitou.Data.Ui;
using Meitou.Game.Ui;
using PZA.MeitouUi;
using L = PZA.MeitouUi.PzaLayout;

namespace PZA.Looks.Meitou;

public static class PresetsWindow
{
    public const string Id = "pza.looks.presets";
    public static PzaWindow? Window { get; private set; }

    static string s_folder = "";
    static List<PresetData> s_presets = [];
    static int s_selected = -1;
    static string s_name = "";
    static string s_share = "";
    static string s_status = "Type a name and click Save, or pick a saved preset to Load.";

    public static string LayoutXml() => L.Doc(L.Window("Root", "PZA'S POST-FX PRESETS", 0.31, 0.14, 0.38, 0.68,
        L.Tabs("Tabs", 0.03, 0.02, 0.94, 0.96,
            ("TabPresets", [
                L.Text("lbName", 0.03, 0.01, 0.94, 0.05),
                L.Edit("edName", 0.03, 0.065, 0.68, 0.065, maxLength: PresetStore.MaxNameLength),
                L.Button("Save", 0.73, 0.065, 0.24, 0.065, isDefault: true),
                L.Text("lbList", 0.03, 0.145, 0.94, 0.05),
                L.List("lstPresets", 0.03, 0.20, 0.94, 0.35),
                L.Button("Load", 0.03, 0.565, 0.29, 0.065),
                L.Button("Share", 0.355, 0.565, 0.29, 0.065),
                L.Button("Delete", 0.68, 0.565, 0.29, 0.065),
                L.Text("lbShare", 0.03, 0.65, 0.94, 0.05),
                L.Edit("edShare", 0.03, 0.705, 0.68, 0.065, maxLength: 4096),
                L.Button("Import", 0.73, 0.705, 0.24, 0.065),
                L.Text("lbStatus", 0.03, 0.79, 0.94, 0.19, wrap: true),
            ]),
            L.HelpList("TabHelp", "lstHelp"))));

    public static void Setup(string modFolder)
    {
        s_folder = modFolder;
        Refresh();
        if (Window is not null) return;
        Window = new PzaWindow(Id, LayoutXml)
        {
            Bind = Bind,
            Event = OnEvent,
            OpenChanged = open => { if (open) Refresh(); },
        };
        PzaUi.Add(Window);
    }

    public static void Open()
    {
        Refresh();
        Window?.Open();
    }

    public static void Refresh()
    {
        if (string.IsNullOrEmpty(s_folder)) return;
        string? keep = s_selected >= 0 && s_selected < s_presets.Count ? s_presets[s_selected].Name : null;
        s_presets = PresetStore.List(s_folder);
        s_selected = keep is not null ? s_presets.FindIndex(p => string.Equals(p.Name, keep, StringComparison.OrdinalIgnoreCase)) : -1;
    }

    static void Bind(UiBindings b)
    {
        bool hasSel = s_selected >= 0 && s_selected < s_presets.Count;
        PzaWindow.Enable(b, "Save", "Share", "Import");
        if (hasSel) PzaWindow.Enable(b, "Load", "Delete");
        else { b.Disabled.Add("Load"); b.Disabled.Add("Delete"); }

        b.Text["Root"] = "PZA'S POST-FX PRESETS";
        PzaWindow.TabText(b, "Tabs", 0, "PRESETS");
        PzaWindow.TabText(b, "Tabs", 1, "HELP");
        b.Text["lbName"] = "Preset name (saves all active Post-FX settings & sliders):";
        b.Text["edName"] = s_name;
        b.Text["Save"] = "Save";
        b.Text["lbList"] = $"Saved presets ({s_presets.Count}) — double-click to load:";
        PzaWindow.Lines(b, "lstPresets", s_presets.Select(p => p.Name), s_selected);
        b.Text["Load"] = "Load";
        b.Text["Share"] = "Share code";
        b.Text["Delete"] = "Delete";
        b.Text["lbShare"] = "Share code (PZAFX1:...) — click Share code to export, or paste & Import:";
        b.Text["edShare"] = s_share;
        b.Text["Import"] = "Import";
        b.Text["lbStatus"] = s_status;
        Window?.Paragraphs(b, "lstHelp",
            "SAVING PRESETS\nType a name in the top box and click Save. Every Post-FX effect and slider (Outline, Pixel Art, Tilt-Shift, Kuwahara, N64, Toon, Noir, Halftone, Screen) is saved to mods\\PZAPostFX\\presets\\<name>.json. Saving with an existing name overwrites it.\n\n"
            + "LOADING & DELETING\nClick a preset in the list and press Load (or double-click it). All sliders in Settings > Mods update immediately and survive restarts. Note: Toon changes apply on the next game start.\n\n"
            + "SHARING ON DISCORD\nClick 'Share code' to encode your current settings as a single-line PZAFX1:... code in the Share box (select & Ctrl+C). To try someone else's preset, paste their PZAFX1:... code into the Share box and click Import — it applies immediately and saves under its preset name.\n\n"
            + "OPENING THIS WINDOW\nToggle 'Presets manager' in Settings > Mods > PZA's Post-FX, or open it from F10 > Mods. Esc closes it; F7 hides it with the HUD.");
    }

    static void OnEvent(MyGuiEvent e)
    {
        switch (e.Action)
        {
            case "text" when e.Widget == "edName":
                s_name = e.Text ?? "";
                break;
            case "text" when e.Widget == "edShare":
                s_share = e.Text ?? "";
                break;
            case "select" when e.Widget == "lstPresets":
                s_selected = (int)Math.Round(e.Value);
                if (s_selected >= 0 && s_selected < s_presets.Count) s_name = s_presets[s_selected].Name;
                break;
            case "accept" when e.Widget == "lstPresets":
                s_selected = (int)Math.Round(e.Value);
                LoadSelected();
                break;
            case "Save":
                SaveCurrent();
                break;
            case "Load":
                LoadSelected();
                break;
            case "Delete":
                DeleteSelected();
                break;
            case "Share":
                ExportShareCode();
                break;
            case "Import":
                ImportShareCode();
                break;
        }
    }

    static void SaveCurrent()
    {
        if (string.IsNullOrEmpty(s_folder)) { s_status = "Mod folder not set."; return; }
        if (PresetStore.Save(s_folder, s_name, PixelPass.Settings, out string err) is { } saved)
        {
            Refresh();
            s_selected = s_presets.FindIndex(p => string.Equals(p.Name, saved, StringComparison.OrdinalIgnoreCase));
            s_name = saved;
            s_status = $"Saved preset '{saved}'.";
            Log.Line($"presets: saved '{saved}'");
        }
        else s_status = "Save failed: " + err;
    }

    static void LoadSelected()
    {
        if (s_selected < 0 || s_selected >= s_presets.Count) { s_status = "Select a preset first."; return; }
        string name = s_presets[s_selected].Name;
        if (PresetStore.Load(s_folder, name, PixelPass.Settings, LooksApi.Group, out string err))
        {
            s_status = $"Loaded preset '{name}'.";
            Log.Line($"presets: loaded '{name}'");
        }
        else s_status = "Load failed: " + err;
    }

    static void DeleteSelected()
    {
        if (s_selected < 0 || s_selected >= s_presets.Count) { s_status = "Select a preset first."; return; }
        string name = s_presets[s_selected].Name;
        if (PresetStore.Delete(s_folder, name, out string err))
        {
            s_selected = -1;
            Refresh();
            s_status = $"Deleted preset '{name}'.";
            Log.Line($"presets: deleted '{name}'");
        }
        else s_status = "Delete failed: " + err;
    }

    static void ExportShareCode()
    {
        string name = PresetStore.SafeFileName(s_name) ?? (s_selected >= 0 && s_selected < s_presets.Count ? s_presets[s_selected].Name : "Custom");
        var data = PresetStore.Capture(name, PixelPass.Settings, nonDefaultOnly: true);
        s_share = PresetStore.EncodeShareCode(data);
        s_status = $"Share code generated for '{data.Name}' ({s_share.Length} chars) — click the Share box and press Ctrl+C.";
        Log.Line($"presets: exported share code for '{data.Name}' ({s_share.Length} chars)");
    }

    static void ImportShareCode()
    {
        if (PresetStore.DecodeShareCode(s_share) is not { } data)
        {
            s_status = "Invalid share code (expected PZAFX1:...).";
            return;
        }
        PresetStore.Apply(data, PixelPass.Settings, LooksApi.Group);
        string? saved = string.IsNullOrEmpty(s_folder) ? null : PresetStore.Save(s_folder, data.Name, PixelPass.Settings, out _);
        Refresh();
        if (saved is not null)
        {
            s_selected = s_presets.FindIndex(p => string.Equals(p.Name, saved, StringComparison.OrdinalIgnoreCase));
            s_name = saved;
        }
        s_status = saved is not null ? $"Imported and saved preset '{saved}'." : $"Imported preset '{data.Name}'.";
        Log.Line($"presets: imported '{data.Name}' ({data.Values.Count} values)");
    }
}
