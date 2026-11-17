# PZA's Looks for Meitou (beta 0.2.5)

Retro looks for Meitou's picture: chunky pixel art with a desert palette, the PlayStation look with CRT scanlines, a soft 240-line N64
look, and tilt-shift miniature looks. Menus, text and the pointer stay sharp: the look is drawn under Meitou's interface.

**Download:** the mod zip and its source zip are on the [Releases page](https://github.com/PZA-Mods/PZAMeitouLooks/releases). Building from source: [BUILD.md](BUILD.md).

## Hotkeys
None of its own. Pick a look by name in **Settings > Mods > PZA's Looks**. PZA's Camera for Meitou (also in this beta) has Tilt-Shift
buttons that drive it.

## Looks

| Look | What it is |
|---|---|
| PZA Pixel - 1 / 2 / 3 | 16-colour desert palette with pattern dithering; three pixel sizes |
| PZA - PS1 Main | 8 colour levels, dither, light scanlines, a little colour bleed |
| PZA PS1 - CRT | PS1 15-bit colour with the console's dither, full scanlines, CRT softness |
| PZA Soft 240 - Standard | 240 lines, N64 16-bit colour, soft |
| Tilt-Shift: Miniature / HD-2D / Diorama | sharp in the middle band, blurred above and below |

"Compare (left half without)" shows the left half of the screen as Meitou draws it, for a before / after.

## Requires
**Meitou 0.20.1 up to 0.21.x**, on the **Vulkan** renderer, with **HDR output off** (the looks switch themselves off under HDR). It refuses
to load on other Meitou versions.

## Install
- Unpack `PZALooks.Meitou-0.2.5-beta.zip` into Kenshi's folder (`Kenshi\mods\PZALooks\`), then tick **PZALooks** in
  Meitou's mod list.
- No items or world data; load order doesn't matter. Safe to remove at any time.

## This is a beta
**The looks run in Meitou (seen in test runs: about 0.1 ms per frame for Pixel 1, 0.4 ms for PS1 CRT at 1080p), but no one has played
with this build yet.** It matches the Dust presets of the same names for RE_Kenshi.

Known limits:
- Vulkan only (Meitou's Metal renderer on macOS is not covered), SDR only.
- It hooks one step of Meitou's picture chain with a patch; a later Meitou version may need a new build of this mod.

## Planned
Ideas, no dates:
- A **CRT wiggle** (the picture rolling and shimmering like an old screen).
- **Live sliders and named presets** over the picture, alongside the graphics work Meitou's developers have planned.
- More palettes (the Dust version's table palettes), and macOS (Metal) support.
- Perhaps as a built-in part of Meitou one day: the shader source is open for that.

## Reporting a bug
Which look, a screenshot, your graphics card, resolution and Meitou's graphics settings (upscaler, HDR), your Meitou version and mod list,
and Meitou's `log.txt` copied right after that session. Our lines start with `pza.looks`. Post it in its thread on the Meitou Discord or in the Workshop page's Bug reports discussion.

## Licence and source
GNU GPL version 3 or later; the source is attached to the release (`-source.zip`). Made by **PZA**. Unofficial fan-made mod; not affiliated
with Lo-Fi Games.
