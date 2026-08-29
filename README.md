# MGS4 Cheat Trainer

A memory trainer for the PC release of *Metal Gear Solid 4: Guns of the Patriots* (Master Collection, `mgs4.exe`). It attaches to the running game process and patches/reads its memory directly — no game files are modified.

This is an unofficial fan project. It is **not affiliated with, endorsed by, or sponsored by Konami**. "Metal Gear Solid" and all related names/assets are trademarks of Konami.

## Features

**Stats tab** — live-editable run statistics (kills, headshots, Drebin Points, play time, item/weapon pickups, and more), read directly from the game's in-memory run-stats struct. Auto-refreshes every 500ms while a run is active; a field you're editing is left alone until you click away. Includes a live Emblems sub-tab showing which of MGS4's 40 completion emblems your current run would qualify for. See [Credits](#credits) for where these offsets came from.

**Cheats tab**
- Survival: Infinite Life, Infinite Stamina, Never Stress
- Weapons & Items: Infinite Ammo, Never Reload, Infinite Throwables, Infinite Items, Infinite Suppressor
- Stealth & Misc: Camo Always 100%, No Alerts, Infinite Battery

## Requirements

- Windows 10/11, x64
- MGS4 (Master Collection) already running before you launch the trainer
- Administrator privileges may be required to open the game process, depending on your setup

## Usage

1. Start the game and load into a level.
2. Run `MGS4CheatTrainer.exe` (as Administrator if it fails to attach).
3. Toggle cheats on the Cheats tab, or edit stats on the Stats tab.

## Building from source

```
dotnet build MGS4CheatTrainer/MGS4CheatTrainer.csproj -c Release
```

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download). The build is framework-dependent, so running the built exe on another machine needs the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed — or publish a self-contained build instead:

```
dotnet publish MGS4CheatTrainer/MGS4CheatTrainer.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Disclaimer

- **Single-player/offline use only.** This tool reads and writes the game's process memory; do not use it in any online or competitive context.
- Antivirus software and Windows SmartScreen may flag this executable as suspicious. This is expected and common for any tool that reads/writes another process's memory (the same category as Cheat Engine or similar). If you don't trust a downloaded binary, build it yourself from source — the code is short and readable.
- Use at your own risk. No warranty; see [LICENSE](LICENSE).

## Credits

- Live run-stats memory layout (`linkvarbuf` struct offsets, stage codes) sourced from [zexk/bbtracker](https://github.com/zexk/bbtracker)'s [`mgs4_research.md`](https://github.com/zexk/bbtracker/blob/master/docs/mgs4_research.md) reverse-engineering notes.
- Code-patch offsets/patterns adapted from a pre-existing community cheat table (`mgs4-snake-swiss.ct`).
