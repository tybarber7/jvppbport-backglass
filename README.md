# jvppbport-backglass
AI-built Windows backglass companion for JVPPBPort 0.2.0. Created with OpenAI Codex for virtual pinball cabinets. Shows the Ruby or Sapphire table, live scores, animated area art, and recent Pokémon catches on a second screen.

## Build and preview

Run `powershell -NoProfile -File .\build.ps1` from this folder on Windows with .NET Framework 4.x. The build uses the Windows C# compiler and places the executable and generated artwork in `dist`.

Open `dist\CabinetBackglass.exe`, choose a display, and click **Preview**. Press Esc to close the preview. To play, reopen the companion, browse to your installed `JVPPBPort.exe`, and click **Launch game**. You can also use `CabinetBackglass.exe --screen 2 --game "C:\Games\JVPPBPort\JVPPBPort.exe"` with the corresponding screen number from the setup window.

## How it connects

The companion launches JVPPBPort with its existing `JVPPBPORT_GAMEDUMP` environment variable pointing to a Windows named pipe. The 0.2.0 port writes a six-byte header followed by a 0x1410-byte game-state record. The companion decodes area, score, ball and coin counts, event time, and capture count from this record. It derives the table badge from the area ID and keeps the travel habitat on display during bonus stages.

- `src/Backglass.cs` contains the Windows launcher, pipe reader, game-state decoder, and animation state.
- `src/BackglassArt.cs` contains the backbox layout and renderer.
- `src/AreaPortraits.cs` and `src/PartyIcons.cs` load optional visual assets.
- `assets/Panoramas` contains the generated habitat panoramas used by Preview and gameplay.

The game-state record is specific to the 0.2.0 build. If the format changes upstream, update the reader and the launcher version check together. The upstream record writer is in `JVPPBPort/src/testhooks.c` at the 0.2.0 tag.

## Optional Pokémon assets

The nine generated area panoramas are included. The only optional game artwork is the animated party icon beside each catch or evolution.

1. Open the [pret Pokémon Emerald source archive](https://github.com/pret/pokeemerald) and choose **Code → Download ZIP**.
2. Extract the archive and find `graphics/pokemon/<species>/icon.png`. These are 32 × 64 PNG sheets containing two stacked 32 × 32 frames. The companion supports the 205 species listed in [SpeciesNames.cs](src/SpeciesNames.cs).
3. Copy each icon you want into a `PartyIcons` folder beside `CabinetBackglass.exe` and name it after its species. For example, copy `graphics/pokemon/mudkip/icon.png` to `dist/PartyIcons/mudkip.png`. Do the same for `lotad.png` and `shroomish.png` to try the sample catches in Preview.

The companion shows a small marker and the species name when the icon is absent. Keep your local sprite files outside the Git repository.

The game and ROM are separate from the companion. Use your existing [JVPPBPort 0.2.0 Windows installation](https://github.com/jonnisvibes/jvppbport/releases/tag/v0.2.0) configured with your own U.S. Pokémon Pinball: Ruby & Sapphire GBA ROM.

[Upstream game-state writer](https://github.com/jonnisvibes/jvppbport/blob/v0.2.0/JVPPBPort/src/testhooks.c) · [Emerald party icon file references](https://github.com/pret/pokeemerald/blob/master/src/data/graphics/pokemon.h)
