# MGS4 Cheat Trainer

A memory trainer for the PC release of *Metal Gear Solid 4: Guns of the Patriots* (Master Collection, `mgs4.exe`). It attaches to the running game process and patches/reads its memory directly — no game files are modified.

This is an unofficial fan project. It is **not affiliated with, endorsed by, or sponsored by Konami**. "Metal Gear Solid" and all related names/assets are trademarks of Konami.

## Features

**Stats tab** — live-editable run statistics, read directly from the game's in-memory run-stats struct. Auto-refreshes every 500ms while a run is active; a field you're editing is left alone until you click away. Split into sub-tabs:
- Run Info: stage/difficulty, completed playthroughs, scenario progress, total play time, Drebin Points, continues, alert phases
- Combat: kills, headshots, CQC uses, knife kills/knockouts, combat highs, special item use
- Movement: prone/forward rolls, and H:M:S-readable standing/crouch/crawl/on-back/wall-press/box-drum time
- Items & Social: weapon/item pickups, hold-ups, body searches, praises, items donated, syringe/scanning plug/recovery-item uses, playboy pages, flashbacks viewed
- Emblems: live pass/fail preview for all 40 of MGS4's completion emblems against your current run's counters

See [Credits](#credits) for where these offsets came from.

**Player tab** — code-patch cheats:
- Survival: Infinite Life, Infinite Stamina, Never Stress, Infinite REX Health (Act 4 boss fight)
- Weapons & Items: Infinite Ammo, Never Reload, Infinite Throwables, Infinite Items, Infinite Suppressor
- Stealth & Misc: Camo Always 100%, No Alerts, Infinite Battery

**Experimental tab**
- Appearance: directly edit Snake's Outfit and FaceCamo byte values and vest colour. Applies on the next area change or save load, not instantly.
- Enemy Control: kill or put every enemy to sleep, persistently, with a mode dropdown (Off / Kill / Sleep).

**Inventory tab** — enable "Inventory Pointer" first, then use its sub-tabs (or "Unlock All" to flip every checkbox below at once):
- Outfits / FaceCamo / Body / Music: ownership checkboxes for outfits, face camo variants, vest/OctoCamo body colours, and iPod tracks not normally available yet (Altair, Suit, the Beauty facecamos, Raiden, Big Boss, Khaki, Navy Blue, Snake Eater, Theme of Solid Snake, etc.) - check one and it becomes pickable in the game's own Customize/iPod menu.
- Equipment: one-time unlock checkboxes for Solid Eye, MG Mk. II, Camera, Cardboard Box, Drum Can, iPod, Radio, Cigs, Muna, Bandana, Stealth, Syringe, Scanning Plug
- Consumables: real stock counters for Ration/E. Bar/E. Drink/Pentazemin/Stupe, each with an Unlock-then-Set button and a "Max All Consumables" shortcut
- Ammo: real ammo-type counters, persistent across area changes/saves. One row per ammo type/caliber rather than per weapon -- some guns share a pool, and Mk.2 alone has 5 (base round plus its 4 non-lethal "emotion" rounds). Each has a Set button plus a "Max All Ammo" shortcut.

**Weapons tab** - real ownership checkbox for every weapon in the game (handguns through launchers, explosives, melee), no pointer needed; checking a never-owned weapon actually grants it in-game. "Unlock All" flips every checkbox at once.

## Requirements

- Windows 10/11, x64
- MGS4 (Master Collection) already running before you launch the trainer
- Administrator privileges may be required to open the game process, depending on your setup

## Usage

1. Start the game and load into a level.
2. Run `MGS4CheatTrainer.exe` (as Administrator if it fails to attach).
3. Toggle cheats on the Player/Experimental tabs, unlock outfits/items on the Inventory tab, or edit stats on the Stats tab.

## Building from source

```
dotnet build MGS4CheatTrainer/MGS4CheatTrainer.csproj -c Release
```

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download). The build is framework-dependent, so running the built exe on another machine needs the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) installed — or publish a self-contained build instead:

```
dotnet publish MGS4CheatTrainer/MGS4CheatTrainer.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Disclaimer

- **Back up your save file before using this trainer.** Editing stats or unlocking items directly in memory can put your save into a state the game didn't expect — this can corrupt progress or interfere with unlocking certain achievements/trophies (e.g. the completion emblems, which are evaluated against your run's tracked counters). Keep a copy of your save so you can roll back if something looks wrong.
- **Single-player/offline use only.** This tool reads and writes the game's process memory; do not use it in any online or competitive context.
- Antivirus software and Windows SmartScreen may flag this executable as suspicious. This is expected and common for any tool that reads/writes another process's memory (the same category as Cheat Engine or similar). If you don't trust a downloaded binary, build it yourself from source — the code is short and readable.
- Use at your own risk. No warranty; see [LICENSE](LICENSE).

## Credits

- Live run-stats memory layout (`linkvarbuf` struct offsets, stage codes) sourced from [zexk/bbtracker](https://github.com/zexk/bbtracker)'s [`mgs4_research.md`](https://github.com/zexk/bbtracker/blob/master/docs/mgs4_research.md) reverse-engineering notes.
- Code-patch offsets/patterns adapted from a pre-existing community cheat table (`mgs4-snake-swiss.ct`, v4), including the Inventory Pointer signature and the base item/weapon table layout.
- Outfit/FaceCamo/Equipment ownership-flag locations and the byte-value → name mappings on the Appearance and Inventory tabs were found by hand, through live memory diffing while unlocking each one in-game.
