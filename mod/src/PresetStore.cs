// PZA's Post-FX for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.7.0: Named user presets saved as JSON files in <mod folder>\presets\ and compact single-line share codes (PZAFX1:...) for sharing on Discord.
// Pure core (no UI / GPU dependency), driven directly by the offline check and the fuzz harness.
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Meitou.Modding;

namespace PZA.Looks.Meitou;

/// <summary>One saved Post-FX preset: its display name and a dictionary of setting key -> invariant string value.</summary>
public sealed record PresetData(string Name, Dictionary<string, string> Values, int Version = 1);

public static class PresetStore
{
    public const string SharePrefix = "PZAFX1:";
    public const int MaxNameLength = 60;
    public const int MaxShareDecodedBytes = 65536;

    static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true };

    /// <summary>Every [Setting] property on LooksSettings that belongs in a preset (all effect lists + sliders; excludes Split and OpenPresets).</summary>
    public static readonly PropertyInfo[] PresetProps = typeof(LooksSettings)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<SettingAttribute>() is not null
            && p.Name is not (nameof(LooksSettings.Split) or nameof(LooksSettings.OpenPresets)))
        .ToArray();

    static readonly LooksSettings Defaults = new();

    /// <summary>Sanitizes a user preset name into a safe single filename stem (no path separators, traversal, control chars, or Windows device names);
    /// returns null when empty or invalid.</summary>
    public static string? SafeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var sb = new StringBuilder(Math.Min(name.Length, MaxNameLength));
        foreach (char ch in name.Trim())
        {
            if (sb.Length >= MaxNameLength) break;
            if (char.IsControl(ch) || ch is '\\' or '/' or ':' or '*' or '?' or '"' or '<' or '>' or '|') continue;
            sb.Append(ch);
        }
        string clean = sb.ToString().Trim().TrimEnd('.');
        while (clean.Contains("..", StringComparison.Ordinal)) clean = clean.Replace("..", "", StringComparison.Ordinal);
        clean = clean.Trim().TrimEnd('.');
        if (clean.Length == 0 || ReservedDeviceNames.Contains(clean)) return null;
        return clean;
    }

    public static string PresetsDir(string modFolder) => Path.Combine(modFolder, "presets");

    /// <summary>Resolves a preset's JSON file path strictly inside <paramref name="modFolder"/>\presets; null if the name is invalid.</summary>
    public static string? PresetPath(string modFolder, string? name)
    {
        if (SafeFileName(name) is not { } stem) return null;
        string dir = Path.GetFullPath(PresetsDir(modFolder));
        string full = Path.GetFullPath(Path.Combine(dir, stem + ".json"));
        string prefix = dir.EndsWith(Path.DirectorySeparatorChar) ? dir : dir + Path.DirectorySeparatorChar;
        return full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    /// <summary>Captures the current Post-FX settings into a PresetData. When <paramref name="nonDefaultOnly"/> is true (for compact share codes),
    /// only keys that differ from default values are stored.</summary>
    public static PresetData Capture(string name, LooksSettings s, bool nonDefaultOnly = false)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in PresetProps)
        {
            string val = FormatValue(p.GetValue(s));
            if (nonDefaultOnly && val == FormatValue(p.GetValue(Defaults))) continue;
            values[p.Name] = val;
        }
        return new PresetData(SafeFileName(name) ?? "Preset", values, 1);
    }

    /// <summary>Applies a PresetData to <paramref name="s"/> (and <paramref name="group"/> when present), resetting omitted keys to defaults first
    /// so compact share codes and partial presets produce an exact, deterministic look. Returns how many values were applied.</summary>
    public static int Apply(PresetData preset, LooksSettings s, ModSettingsGroup? group)
    {
        int applied = 0;
        LookValues.RunSuppressed(() =>
        {
            // Reset all preset properties to defaults first so omitted keys in a share code don't inherit old active effects.
            foreach (var p in PresetProps)
                SetValue(group, s, p, p.GetValue(Defaults)!);

            foreach (var (key, text) in preset.Values)
            {
                var p = PresetProps.FirstOrDefault(x => string.Equals(x.Name, key, StringComparison.Ordinal));
                if (p is null) continue;
                object? parsed = ParseValue(p, text);
                if (parsed is null) continue;
                SetValue(group, s, p, parsed);
                applied++;
            }
        });
        return applied;
    }

    /// <summary>Lists saved presets in <paramref name="modFolder"/>\presets, sorted alphabetically by name.</summary>
    public static List<PresetData> List(string modFolder)
    {
        var list = new List<PresetData>();
        string dir = PresetsDir(modFolder);
        if (!Directory.Exists(dir)) return list;
        foreach (string file in Directory.GetFiles(dir, "*.json").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (ReadJson(File.ReadAllText(file)) is { } data) list.Add(data);
            }
            catch { }
        }
        list.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
        return list;
    }

    /// <summary>Saves the current settings under <paramref name="name"/>. Returns the sanitized name on success, or null on failure.</summary>
    public static string? Save(string modFolder, string? name, LooksSettings s, out string error)
    {
        error = "";
        if (PresetPath(modFolder, name) is not { } path || SafeFileName(name) is not { } clean)
        {
            error = "Enter a valid preset name.";
            return null;
        }
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var data = Capture(clean, s, nonDefaultOnly: false);
            string json = JsonSerializer.Serialize(new StoredPreset(data.Name, data.Version, data.Values), WriteOpts);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, Encoding.UTF8);
            File.Move(tmp, path, overwrite: true);
            return clean;
        }
        catch (Exception e)
        {
            error = e.Message;
            return null;
        }
    }

    /// <summary>Loads a saved preset by name and applies it. Returns true on success.</summary>
    public static bool Load(string modFolder, string? name, LooksSettings s, ModSettingsGroup? group, out string error)
    {
        error = "";
        if (PresetPath(modFolder, name) is not { } path || !File.Exists(path))
        {
            error = "Preset file not found.";
            return false;
        }
        try
        {
            var data = ReadJson(File.ReadAllText(path, Encoding.UTF8));
            if (data is null) { error = "Preset file is unreadable."; return false; }
            Apply(data, s, group);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    /// <summary>Deletes a saved preset by name. Returns true if deleted.</summary>
    public static bool Delete(string modFolder, string? name, out string error)
    {
        error = "";
        if (PresetPath(modFolder, name) is not { } path || !File.Exists(path))
        {
            error = "Preset not found.";
            return false;
        }
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    /// <summary>Encodes a preset as a compact single-line Discord share code (PZAFX1:...).</summary>
    public static string EncodeShareCode(PresetData preset)
    {
        var compact = new StoredPreset(SafeFileName(preset.Name) ?? "Preset", 1, preset.Values);
        byte[] raw = JsonSerializer.SerializeToUtf8Bytes(compact);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            gz.Write(raw, 0, raw.Length);
        string b64 = Convert.ToBase64String(ms.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return SharePrefix + b64;
    }

    /// <summary>Decodes a PZAFX1:... share code safely (capped at MaxShareDecodedBytes); returns null on any invalid or hostile input.</summary>
    public static PresetData? DecodeShareCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        string trimmed = code.Trim();
        if (!trimmed.StartsWith(SharePrefix, StringComparison.Ordinal)) return null;
        string payload = trimmed[SharePrefix.Length..].Trim();
        if (payload.Length == 0 || payload.Length > MaxShareDecodedBytes * 2) return null;
        try
        {
            string b64 = payload.Replace('-', '+').Replace('_', '/');
            int pad = (4 - b64.Length % 4) % 4;
            if (pad > 0) b64 += new string('=', pad);
            byte[] compressed = Convert.FromBase64String(b64);
            using var input = new MemoryStream(compressed);
            using var gz = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            byte[] buf = new byte[4096];
            int total = 0, read;
            while ((read = gz.Read(buf, 0, buf.Length)) > 0)
            {
                total += read;
                if (total > MaxShareDecodedBytes) return null;
                output.Write(buf, 0, read);
            }
            return ReadJson(Encoding.UTF8.GetString(output.ToArray()));
        }
        catch
        {
            return null;
        }
    }

    static PresetData? ReadJson(string json)
    {
        try
        {
            var stored = JsonSerializer.Deserialize<StoredPreset>(json);
            if (stored is null || stored.Values is null) return null;
            string name = SafeFileName(stored.Name) ?? "Preset";
            var clean = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (k, v) in stored.Values)
                if (!string.IsNullOrEmpty(k) && v is not null) clean[k] = v;
            return new PresetData(name, clean, stored.Version > 0 ? stored.Version : 1);
        }
        catch
        {
            return null;
        }
    }

    static string FormatValue(object? v) => v switch
    {
        float f => (float.IsFinite(f) ? f : 0f).ToString("R", CultureInfo.InvariantCulture),
        double d => (double.IsFinite(d) ? d : 0d).ToString("R", CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        string s => s,
        _ => "",
    };

    static object? ParseValue(PropertyInfo p, string text)
    {
        var attr = p.GetCustomAttribute<SettingAttribute>();
        if (p.PropertyType == typeof(bool))
            return bool.TryParse(text, out bool b) ? b : null;
        if (p.PropertyType == typeof(int))
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return null;
            if (attr is not null && double.IsFinite(attr.Min) && double.IsFinite(attr.Max))
                i = Math.Clamp(i, (int)Math.Round(attr.Min), (int)Math.Round(attr.Max));
            return i;
        }
        if (p.PropertyType == typeof(float))
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) || !float.IsFinite(f)) return null;
            if (attr is not null && double.IsFinite(attr.Min) && double.IsFinite(attr.Max))
                f = (float)Math.Clamp(f, attr.Min, attr.Max);
            return f;
        }
        if (p.PropertyType == typeof(string))
        {
            if (attr?.Options is { Length: > 0 } opts && !opts.Contains(text, StringComparer.Ordinal)) return null;
            return text;
        }
        return null;
    }

    static void SetValue(ModSettingsGroup? group, LooksSettings s, PropertyInfo p, object value)
    {
        var setting = group?.Settings.FirstOrDefault(x => x.Key == p.Name);
        if (setting is not null) { setting.Set(value); return; }
        p.SetValue(s, value);
    }

    sealed record StoredPreset(string Name, int Version, Dictionary<string, string> Values);
}
