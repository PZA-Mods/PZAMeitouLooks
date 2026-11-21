# PZA's Post-FX for Meitou (beta 0.5.0)

Formerly **PZA's Looks**. Post-processing effects for Meitou's picture: **Toon** cel lighting, a painted **Kuwahara** look, **N64**
distance fog, an ink **Outline**, real **Tilt-Shift** miniature looks, a
**Fisheye** lens, and retro **pixel looks** (chunky pixel art with a desert palette, the PlayStation look with CRT scanlines, a soft
240-line N64 look). Each effect has ready-made presets and its own sliders. Menus, text and the pointer stay sharp: the effects are
drawn under Meitou's interface.

**New in 0.5.0:** Toon (the light banded into flat cel steps inside Meitou's own lighting, so the bands follow the light and never the
textures, characters and foliage included; **applies at the next start**), Kuwahara (a painted look that keeps edges), N64 (far land fades
into fog in the scene's own colour). Each has a Default preset that shows it plainly, plus presets with the values of PZA's Dust looks.
On the Steam Workshop: [PZA's Post-FX](https://steamcommunity.com/sharedfiles/filedetails/?id=3843020725).

**Download:** the mod zip and its source zip are on the [Releases page](https://github.com/PZA-Mods/PZAMeitouLooks/releases). Building from source: [BUILD.md](BUILD.md).

## Hotkeys
None of its own. Everything is in **Settings > Mods > PZAPostFX**. PZA's Director's Kit for Meitou has Tilt-Shift and Fisheye buttons
that drive it.

## Effects (in the order they are drawn)

| Effect | Presets |
|---|---|
| **Toon** (at the next start) | PZA Toon - Default, Kenshi, 1, Kenshi 3-Band, Cel, Anime, Wind Waker, PZA PS1 - Toon: flat bands of light (shadow, mid, lit), a tinted shadow and a rim, inside Meitou's lighting |
| **N64** | PZA N64 - Default (fog in ordinary views, sky clear), Soft 240: far land fades into fog in the scene's own colour |
| **Kuwahara** | PZA Kuwahara - Default, Painted Miniature, Soft, Heavy: a painted look, areas into strokes, edges kept |
| **Outline** | PZA Outline, Ink (2 px, darker), Soft (faint), Tilt-Shift: lines where the depth jumps (silhouettes) and where surfaces fold (creases) |
| **Tilt-Shift** | PZA Miniature (wide sharp band), HD-2D and Diorama (narrow): a tilt-shift from the picture's depth, the followed character sharp from feet to head |
| **Fisheye** | PZA Fisheye (fills the screen), Fisheye - Round (black corners) |
| **Pixel look** | PZA Pixel - 1 / 2 / 3 / 1 Night (16-colour desert palette, pattern dither), PZA - PS1 Main, PS1 - Standard, PS1 - CRT (15-bit colour, scanlines, CRT softness, colour smear), Soft 240 - Standard (240 lines, N64 colour), Diorama - Pixel (fine full-colour pixels) |

"Compare (left half without)" shows the left half of the screen as Meitou draws it, for a before / after.

## Presets and sliders
Each effect is a list: **Off**, its presets, and **Custom**. Under it, while the effect is on, a group of sliders holds the values
actually drawn. Picking a preset fills its sliders; moving any slider turns the list to Custom. Your values are kept after a restart.
Pick the preset again to get its values back.

| Group | Settings |
|---|---|
| Pixel look | Pixel size (1-16), Palette, Levels, Dither, Contrast, Shadow lift (and only at night), Highlight hold, Scanlines, CRT softness, Colour smear, Sampling, Native lines (Off / 224 / 240 / 480), Keep pure black, Strength |
| Tilt-Shift | Sharp band, Falloff, Blur size, Far blur, Near blur, Upright, Band centre, Saturation, Contrast, Strength |
| Fisheye | Strength, Fit (Fill / Full width), Edge darkening |
| Outline | Depth sensitivity, Crease sensitivity (0 = no crease lines), Thickness (1-4 px), Strength, Line colour, Line distance |
| Kuwahara | Brush size, Edge keeping, Strength |
| N64 | Fog start, Fog end, Fog amount, Fog the sky, Fog colour, Colour strength, Strength |
| Toon (at the next start) | Strength, Follow the sun, Light reference, Shadow / Highlight edge, Band softness, Shadow / Mid / Lit level, Shadow tint and colour, Rim light and colour, Colour strength |

## Requires
**Meitou 0.20.1 and newer**, on the **Vulkan** renderer, with **HDR output off** (the effects switch themselves off under HDR). Checked
on Meitou 0.20.1, 0.21.0 and 0.22.1; there is no upper version cap.

## Install
- On the Steam Workshop: subscribe, then tick **PZAPostFX** in Meitou's mod list. Or by hand: unpack `PZALooks.Meitou-0.5.0-beta.zip` into Kenshi's folder (`Kenshi\mods\PZAPostFX\`), then tick **PZAPostFX** in Meitou's mod list.
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
- A **CRT wiggle** (the picture rolling and shimmering like an old screen).
- Russian, Chinese and Japanese texts.
- More palettes, and macOS (Metal) support.
- Perhaps as a built-in part of Meitou one day: the shader source is open for that.

## Reporting a bug
Which effect and preset, a screenshot, your graphics card, resolution and Meitou's graphics settings (upscaler, HDR), your Meitou
version and mod list, and Meitou's `log.txt` copied right after that session. Our lines start with `pza.looks`. Post it in its thread
on the Meitou Discord.

## Licence and source
GNU GPL version 3 or later; the source is attached to the release (`-source.zip`). Made by **PZA**. Unofficial fan-made mod; not
affiliated with Lo-Fi Games.
