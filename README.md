# PZA's Post-FX for Meitou (beta 0.7.5)

Formerly **PZA's Looks**. Post-processing effects for Meitou's picture: one-pick **Combos**, **Film Noir**, comic **Halftone** dots, an old **CRT / VHS** screen, **Toon** cel lighting, a painted **Kuwahara** look, **N64**
distance fog, an ink **Outline**, real **Tilt-Shift** miniature looks, a
**Fisheye** lens, and retro **pixel looks** (chunky pixel art with a desert palette, the PlayStation look with CRT scanlines, a soft
240-line N64 look). Each effect has ready-made presets and its own sliders. Menus, text and the pointer stay sharp: the effects are
drawn under Meitou's interface.

**New in 0.7.5:** eight built-in **recommended presets** in the Presets window (Settings > Mods > PZAPostFX > Presets manager):
[PZA] Anime, Hyper Realistic, Cinematic, Painted Miniature, Comic Book, Film Noir, Retro PS1, 16-Bit Miniature. Load one and play;
save a copy under your own name to tweak it. On Meitou 0.24.1 and newer, a **Meitou look** list (Clean, Realistic, Cinematic, Film)
sets Meitou's own tone map, bloom, depth of field, vignette and grain while picked (Off gives your own settings back), and PZA
Letterbox drives Meitou's own Letterbox, so there are never two sets of bars. Fix: a pixel look could freeze Meitou at start on
some AMD cards; each palette now builds a much smaller shader, and a launch that froze starts with the Look Off.

**New in 0.6.0:** Combos (one pick sets several effects: Painted Miniature, Ink Miniature, Retro Cel, CRT Anime, Soft 240 Toon,
16-Bit Miniature, HD-2D, Diorama, Film Noir, Comic Book, Handheld, Found Footage), Film Noir (Default, Sin City keeping red, Silver
Screen), Halftone (comic, newspaper, pop art), Screen drawn over everything (an old CRT's wiggle, a lens colour fringe, VHS, VHS Worn),
the Desert 8 / 32 / Night palettes, and PZA Game Boy (four greens at 144 lines: **a novelty with very limited use**, hard to read in
play; not recommended for normal play). Vignette, film grain and letterbox stay in Meitou's own graphics settings.
On the Steam Workshop: [PZA's Post-FX](https://steamcommunity.com/sharedfiles/filedetails/?id=3843020725).

**Download:** the mod zip and its source zip are on the [Releases page](https://github.com/PZA-Mods/PZAMeitouLooks/releases). Building from source: [BUILD.md](BUILD.md).

## Hotkeys
None of its own. Everything is in **Settings > Mods > PZAPostFX**. PZA's Director's Kit for Meitou has Tilt-Shift and Fisheye buttons
that drive it.

## Effects (in the order they are drawn)

| Effect | Presets |
|---|---|
| **Combo** | 12 one-pick bundles (see above); changing any effect afterwards shows Custom, Off turns every effect off |
| **Toon** (at the next start) | PZA Toon - Default, Kenshi, 1, Kenshi 3-Band, Cel, Anime, Wind Waker, PZA PS1 - Toon: flat bands of light (shadow, mid, lit), a tinted shadow and a rim, inside Meitou's lighting |
| **N64** | PZA N64 - Default (fog in ordinary views, sky clear), Soft 240: far land fades into fog in the scene's own colour |
| **Kuwahara** | PZA Kuwahara - Default, Painted Miniature, Soft, Heavy: a painted look, areas into strokes, edges kept |
| **Outline** | PZA Outline, Ink (2 px, darker), Soft (faint), Tilt-Shift: lines where the depth jumps (silhouettes) and where surfaces fold (creases) |
| **Tilt-Shift** | PZA Miniature (wide sharp band), HD-2D and Diorama (narrow): a tilt-shift from the picture's depth, the followed character sharp from feet to head |
| **Fisheye** | PZA Fisheye (fills the screen), Fisheye - Round (black corners) |
| **Film Noir** | PZA Noir - Default, Sin City (keeps red), Silver Screen |
| **Halftone** | PZA Halftone - Default (comic), Newspaper, Pop Art |
| **Pixel look** | PZA Pixel - Desert 8 / Desert 32 / Desert Night, PZA Game Boy (novelty, very limited use), PZA Pixel - 1 / 2 / 3 / 1 Night (16-colour desert palette, pattern dither), PZA - PS1 Main, PS1 - Standard, PS1 - CRT (15-bit colour, scanlines, CRT softness, colour smear), Soft 240 - Standard (240 lines, N64 colour), Diorama - Pixel (fine full-colour pixels) |
| **Screen** (drawn last) | PZA Screen - Default (CRT wiggle), Lens Fringe, VHS, VHS Worn |

"Compare (left half without)" shows the left half of the screen as Meitou draws it, for a before / after.

## Presets and sliders
Each effect is a list: **Off**, its presets, and **Custom**. Under it, while the effect is on, a group of sliders holds the values
actually drawn. Picking a preset fills its sliders; moving any slider turns the list to Custom. Your values are kept after a restart.
Pick the preset again to get its values back.

| Group | Settings |
|---|---|
| Pixel look | Pixel size (1-16), Palette, Levels, Dither, Contrast, Shadow lift (and only at night), Highlight hold, Scanlines, CRT softness, Colour smear, Sampling, Native lines (Off / 224 / 240 / 480 / 144), Keep pure black, Strength |
| Tilt-Shift | Sharp band, Falloff, Blur size, Far blur, Near blur, Upright, Band centre, Saturation, Contrast, Strength |
| Fisheye | Strength, Fit (Fill / Full width), Edge darkening |
| Outline | Depth sensitivity, Crease sensitivity (0 = no crease lines), Thickness (1-4 px), Strength, Line colour, Line distance |
| Kuwahara | Brush size, Edge keeping, Strength |
| N64 | Fog start, Fog end, Fog amount, Fog the sky, Fog colour, Colour strength, Strength |
| Film Noir | Contrast, Black crush, Tint colour and amount, Keep colour and amount, Edge darkening, Strength |
| Halftone | Dot size, Screen angle, Shadow start, Paper, Ink colour, Colour dots, Strength |
| Screen | Colour fringe, Wobble, Roll, Flicker, Tracking, Noise, Colour smear, Softness, Strength |
| Toon (at the next start) | Strength, Follow the sun, Light reference, Shadow / Highlight edge, Band softness, Shadow / Mid / Lit level, Shadow tint and colour, Rim light and colour, Colour strength |

## Requires
**Meitou 0.20.1 and newer**, on the **Vulkan** renderer, with **HDR output off** (the effects switch themselves off under HDR). Checked
on Meitou 0.20.1 to 0.24.0 and the 0.24.1 nightly; there is no upper version cap. The Meitou look list and the Letterbox
bridge need Meitou 0.24.1 or newer; on older Meitou the list is hidden and PZA Letterbox draws its own bars.

## Install
- On the Steam Workshop: subscribe, then tick **PZAPostFX** in Meitou's mod list. Or by hand: unpack `PZALooks.Meitou-0.7.5-beta.zip` into Kenshi's folder (`Kenshi\mods\PZAPostFX\`), then tick **PZAPostFX** in Meitou's mod list.
- Coming from PZA's Looks 0.2.x: untick **PZALooks** and delete `Kenshi\mods\PZALooks\` first, so it is not loaded twice. Your settings
  start fresh in the new folder.
- No items or world data; load order doesn't matter. Safe to remove at any time.

## This is a beta
**The effects run in Meitou (seen in test runs at 1080p: about 0.1 ms per frame for Pixel 1 and N64, 0.4 ms for PS1 CRT, 2 ms for
Kuwahara (5 ms at 4K); Outline and Toon within the noise), but few people have played with this build yet.** Your first impressions are exactly what we need.

Known limits:
- Vulkan only (Meitou's Metal renderer on macOS is not covered), SDR only.
- It hooks one step of Meitou's picture chain with a patch; a later Meitou version may need a new build of this mod.
- Texts are English only for now.
- With Ink (2 px lines), far ground can look a little scratchy; it suits close views best.
- Toon changes Meitou's lighting shaders as they compile at the start, so switching it needs a restart; the lamps keep Meitou's own
  light. If a newer Meitou changes its lighting, Toon stays off (and says so in the log) until this mod is updated.
- Kuwahara is the heaviest effect: about 5 ms per frame at 4K.

## Planned
Ideas, no dates:
- Russian, Chinese and Japanese texts.
- macOS (Metal) support.
- Perhaps as a built-in part of Meitou one day: the shader source is open for that.

## Reporting a bug
Which effect and preset, a screenshot, your graphics card, resolution and Meitou's graphics settings (upscaler, HDR), your Meitou
version and mod list, and Meitou's `log.txt` copied right after that session. Our lines start with `pza.looks`. Post it in its thread
on the Meitou Discord.

## Licence and source
GNU GPL version 3 or later; the source is attached to the release (`-source.zip`). Made by **PZA**. Unofficial fan-made mod; not
affiliated with Lo-Fi Games.
