using System.Collections.Generic;

namespace MGS4CheatTrainer
{
    public static class Constants
    {
        // MGS4 (Master Collection) process name, confirmed via Get-Process while the game is running.
        public const string PROCESS_NAME = "mgs4";

        // Stage-code -> display name, sourced from zexk/bbtracker's mgs4_research.md ("Stage codes"
        // table). Names come from the game's own scenerio.gcx selector; a combined name means one
        // internal stage code covers multiple visible in-game locations.
        public static class StageNames
        {
            public static readonly Dictionary<string, string> ByCode = new()
            {
                ["s00a00l"] = "Prologue Cemetery",
                ["s00a10l"] = "Ending Cemetery",
                ["s01a00l"] = "Middle East Infiltration",
                ["s01a05l"] = "Middle East Infiltration",
                ["s01a10l"] = "Red Zone",
                ["s01a20l"] = "Militia Safehouse",
                ["s01a30l"] = "Urban Ruins",
                ["s01a40l"] = "Advent Palace",
                ["s01a50l"] = "Crescent Meridian",
                ["s01a55l"] = "Crescent Meridian",
                ["s01a57l"] = "Millennium Park",
                ["s01a60l"] = "Liquid's Encampment",
                ["s02a10l"] = "Cove Valley Village",
                ["s02a20l"] = "Power Station",
                ["s02a25l"] = "Power Station",
                ["s02a30l"] = "Confinement Facility",
                ["s02a40l"] = "Vista Mansion",
                ["s02a50l"] = "Research Lab",
                ["s02a60l"] = "Mountain Trail / Riverside",
                ["s02a70l"] = "Vamp Ambush",
                ["s02a73l"] = "Stryker Escape",
                ["s02a75l"] = "Stryker Escape",
                ["s02a78l"] = "Stryker Escape",
                ["s02a80l"] = "High Woodlands Highway",
                ["s02a85l"] = "Marketplace Entrance",
                ["s02a90l"] = "Marketplace",
                ["s02a95l"] = "Marketplace Plaza",
                ["s03a00l"] = "Eastern Europe Station",
                ["s03a10l"] = "Midtown: Resistance Tail",
                ["s03a15l"] = "Midtown: Resistance Tail",
                ["s03a16l"] = "Midtown: Canals",
                ["s03a20l"] = "Midtown: Plaza",
                ["s03a25l"] = "Midtown: North Sector",
                ["s03a30l"] = "Church Courtyard",
                ["s03a35l"] = "Motorcycle Chase",
                ["s03a40l"] = "Motorcycle Chase",
                ["s03a60l"] = "Motorcycle Chase",
                ["s03a50l"] = "Raging Raven Ambush",
                ["s03a65l"] = "Echo's Beacon",
                ["s03a70l"] = "Echo's Beacon",
                ["s03a90l"] = "Volta River",
                ["s04a05l"] = "Metal Gear Solid Flashback",
                ["s04a10l"] = "Snowfield / Heliport / Tank Hangar",
                ["s04a20l"] = "Nuclear Warhead Storage Building",
                ["s04a30l"] = "Snowfield / Communications Tower",
                ["s04a40l"] = "Blast Furnace / Casting Facility",
                ["s04a50l"] = "Underground Base",
                ["s04a60l"] = "Underground Supply Tunnel",
                ["s04a65l"] = "REX Escape",
                ["s04a68l"] = "Port Area",
                ["s04a70l"] = "Port Area: REX vs. RAY",
                ["s04a75l"] = "Outer Haven Arrival",
                ["s05a10l"] = "Ship Bow",
                ["s05a20l"] = "Command Center / Missile Hangar",
                ["s05a30l"] = "Microwave Corridor",
                ["s05a40l"] = "GW",
                ["s05a45l"] = "Liquid Ocelot: Prelude",
                ["s05a50l"] = "Liquid Ocelot",
                ["s05a55l"] = "Liquid Ocelot: Aftermath",
                ["s10a10l"] = "Nomad Mission Briefing",
                ["s10a20l"] = "Nomad: South America Briefing",
                ["s10a30l"] = "Nomad: Eastern Europe Briefing",
                ["s10a40l"] = "Nomad: Shadow Moses Briefing",
                ["s20a00l"] = "USS Missouri",
                ["s20a10l"] = "USS Missouri vs. Outer Haven",
                ["s20a20l"] = "Campbell's Room",
                ["s30a00l"] = "Wedding",
                ["s30a10l"] = "Hospital",
            };
        }

        // Live run-stats struct ("linkvarbuf"), sourced from zexk/bbtracker's mgs4_research.md.
        // module base + LinkVarBufPointerOffset holds a pointer to the active linkvarbuf; only valid
        // while a real campaign run is loaded (menus/results screens leave it null or stale).
        // Offsets not listed here (weapon-state / item-state / inventory arrays) are per-slot ID
        // tables rather than simple scalar stats, so they're left out of the editable list.
        public static class LiveStats
        {
            public const long LinkVarBufPointerOffset = 0x1C28B28;
            public const long CompletedPlaythroughsOffset = 0x0000;
            public const long TitleInitFieldOffset = 0x0004;
            public const long DifficultyOffset = 0x0006;
            public const long StageCodeOffset = 0x0034;
            public const long ScenarioProgressOffset = 0x0054;
            public const long ContinuesOffset = 0x0158;
            public const long TotalPlayTimeOffset = 0x0168;
            public const long AlertsOffset = 0x016E;
            public const long KillsOffset = 0x0178;
            public const long SpecialItemUseOffset = 0x017A;
            public const long CqcUsesOffset = 0x0180;
            public const long HeadshotsOffset = 0x0182;
            public const long KnifeKillsOffset = 0x0184;
            public const long KnifeKnockoutsOffset = 0x0186;
            public const long ProneSideRollsOffset = 0x0188;
            public const long ForwardRollsOffset = 0x018A;
            public const long CombatHighsOffset = 0x018C;
            public const long WeaponPickupsOffset = 0x018E;
            public const long ItemPickupsOffset = 0x0190;
            public const long HoldUpsOffset = 0x0192;
            public const long BodySearchesOffset = 0x0194;
            public const long PraisesOffset = 0x0196;
            public const long ItemsDonatedOffset = 0x0198;
            public const long SyringeUsesOffset = 0x019A;
            public const long ScanningPlugUsesOffset = 0x019C;
            public const long PlayboyPagesOffset = 0x019E;
            public const long EmotionMagazinePagesOffset = 0x01A0;
            public const long StandingTimeOffset = 0x01A4;
            public const long CrouchTimeOffset = 0x01A8;
            public const long CrawlTimeOffset = 0x01AC;
            public const long OnBackTimeOffset = 0x01B0;
            public const long WallPressTimeOffset = 0x01B4;
            public const long BoxDrumTimerAOffset = 0x01B8;
            public const long BoxDrumTimerBOffset = 0x01BC;
            public const long DrebinPointsOffset = 0x01C0;
            public const long DrebinPointsCopyOffset = 0x01C4;
            public const long RexHealthOffset = 0x0B70;
            public const long RexHealthMaxOffset = 0x0B74;
            public const long WeaponStatesOffset = 0x01D4;
            public const long RecoveryItemsUsedOffset = 0x0AE0;
            public const long FlashbacksViewedOffset = 0x5A34;
            public const int SnapshotSize = 0x5A36;
        }

        // Outfit/FaceCamo bytes: static mgs4.exe+offset (no AOB, not update-safe -- source CT flags this
        // itself). Confirmed via breakpoint that they're not real independent statics -- they alias
        // linkvarbuf+0xB26/+0xB27 (same struct as LiveStats above), just fixed-looking because that
        // struct sits at a constant spot in the module. Writing them updates the save correctly but the
        // 3D model only re-reads at stage/save load, so change requires an area change or reload; traced
        // the setter/validator functions and found no reload call to hook, so this isn't fixable without
        // catching a *read* during an actual stage load. Name mapping is filled in from hand-testing
        // in-game, not a documented source -- unmapped values still work, just show as a raw number.
        //
        // A related DWORD at linkvarbuf+0xB28 (right after FaceCamo) holds the OctoCamo pattern and DOES
        // apply live (confirmed by forcing it via trampoline) -- not implemented because the in-game
        // Customize menu reuses that same write to preview/cycle patterns, so pinning it while the menu
        // is open crashes the game. Changing the outfit/costume directly (below) is the safer path.
        public static class Appearance
        {
            public const long OutfitOffset = 0x23FED446;

            public static readonly (string Name, byte Value)[] OutfitNames =
            {
                ("Octocamo", 0),
                ("Middle East Rebel Disguise", 1),
                ("South American Rebel Disguise", 2),
                ("Civilian Disguise", 3),
                ("Suit", 4),
                ("Snake Black sleeve shirt/camo pants", 5),
                ("Altair", 7),
            };

        }

        public static class Weapons
        {
            public const long AmmoBaseOffset = -0x2598;
            public const int AmmoStride = 0x18;
            public const ushort LockedSentinel = 0xFFFF;

            // Ammo-type slots discovered by live-dumping this array (0x00-0x42 is real data, past that
            // is unrelated memory). Deliberately a flat (Label, Slot) list rather than one-slot-per-gun:
            // several weapons share an ammo pool by caliber (e.g. G3A3/Mk.17 both draw from the same
            // 7.62mm slot), and Mk.2 alone has 5 separate slots (base tranq round + 4 non-lethal
            // "emotion" rounds).
            public static readonly (string Label, int Slot)[] AmmoSlots =
            {
                ("Mk.2 Pistol (Tranq.)", 0x00),
                ("Mk.2 Scream Ammo", 0x01),
                ("Mk.2 Laugh Ammo", 0x02),
                ("Mk.2 Rage Ammo", 0x03),
                ("Mk.2 Cry Ammo", 0x04),
                ("Mosin-Nagant Tranq. Normal (7,62mm)", 0x05),
                ("Mosin-Nagant Tranq. Scream (7,62mm)", 0x06),
                ("Mosin-Nagant Tranq. Laugh (7,62mm)", 0x07),
                ("Mosin-Nagant Tranq. Rage (7,62mm)", 0x08),
                ("Mosin-Nagant Tranq. Cry (7,62mm)", 0x09),
                ("Ammo 9x23mm (Race Gun)", 0x0A),
                ("Ammo cal .45 (Operator)", 0x0B),
                ("Thor .45-70 Ammo", 0x0C),
                ("Ammo cal .50 AE (D.E.)", 0x0D),
                ("Ammo cal .50 BMG (M82A2)", 0x0E),
                ("Ammo 4,6x30mm (MP7)", 0x0F),
                ("Ammo 5,45x39mm (AN94)", 0x10),
                ("Ammo 5,56mm (AK-102)", 0x11),
                ("Ammo 7,62mm (Mk.17)", 0x14),
                ("Ammo 7,62x54mmR (SVD)", 0x15),
                ("Ammo 9x18mm (VZ.83)", 0x16),
                ("Ammo 9x19mm (G18C)", 0x17),
                ("Sniper Ammo 9x39mm (VSS)", 0x18),
                ("Ammo 7,62x67mm (DSR-1)", 0x19),
                ("Ammo lead ball (Tanegashima)", 0x1A),
                ("Ammo 12GA. (PLT 00)", 0x1B),
                ("Ammo Balls", 0x1C),
                ("Ammo Vortex (Tranq. Balls)", 0x1D),
                ("Ammo 40mm (Grenade)", 0x1F),
                ("Ammo 40mm (White Phosphorus Grenade)", 0x20),
                ("Ammo 40mm (Stun Grenade)", 0x21),
                ("Ammo 40mm (Smoke Grenade)", 0x22),
                ("Ammo 25mm A.B.G.", 0x23),
                ("Ammo FIM-92A", 0x24),
                ("Ammo Javelin", 0x25),
                ("Ammo RPG-7", 0x26),
                ("Ammo M72A3", 0x27),
                ("Grenade", 0x28),
                ("WP Grenade (M34 White Phosphorus)", 0x29),
                ("Stun Grenade", 0x2A),
                ("Chaff Grenade", 0x2B),
                ("Smoke Grenade", 0x2C),
                ("Smoke Grenade (Red)", 0x2D),
                ("Smoke Grenade (Green)", 0x2E),
                ("Smoke Grenade (Yellow)", 0x2F),
                ("Smoke Grenade (Blue)", 0x30),
                ("Petrol Bomb (Molotov)", 0x31),
                ("Claymore", 0x34),
                ("Sleep Gas Mine", 0x35),
                ("C4", 0x36),
                ("Sleep Gas Satchel", 0x37),
                ("Playboy", 0x39),
                ("Emotion Magazine", 0x3A),
                ("Rail Gun Ammo", 0x3B),
            };
        }

        // Real weapon *ownership* table -- distinct from Weapons above, which is only ammo. Found from
        // a user-supplied "MGS4_UNLOCK.CT" (community table, author "khuong") whose every entry is a
        // plain static address, no AssemblerScript anywhere -- confirmed by cross-checking its item
        // offsets against our own dynamically-captured Inventory Pointer: its Ration entry
        // (mgs4.exe+1D81E70+24FC) and Solid Eye entry (+2664) are exactly ItemStride*5 apart, matching
        // InventoryUnlocks.ItemStride, meaning mgs4.exe+1D81E70 is a static, ASLR-relative-only offset
        // to the very same struct Inventory Pointer captures dynamically -- so this table needs no
        // pointer at all. Confirmed live: writing 2 ("Unlocked - usable") to a weapon never owned before
        // (GSR) actually granted it in-game, unlike Constants.Weapons's array (ammo-only, doesn't gate
        // real ownership) or linkvarbuf+0x1D4 (doesn't survive a save reload). Per-weapon layout is
        // State (2 Bytes: 0 Not found / 1 Found - ID locked / 2 Unlocked - usable / 65535 Locked) at
        // TableBaseOffset+StateBase+StateStride*id; the CT also exposes an "infinite ammo" byte at
        // State+0x16 per weapon, not used here since Player tab already has a global Infinite Ammo cheat.
        public static class WeaponUnlocks
        {
            public const long TableBaseOffset = 0x1D81E70;
            public const long StateBase = 0x71A;
            public const long StateStride = 0x50;
            public const ushort Unlocked = 2;
            public const ushort Locked = 0xFFFF;

            public static readonly (string Name, int Id)[] Names =
            {
                ("Stun Knife", 0x01),
                ("Mk.2 Pistol", 0x02),
                ("Operator", 0x03),
                ("Mk.23", 0x04),
                ("PMM", 0x05),
                ("Five-seveN", 0x06),
                ("GSR", 0x07),
                ("D.E.", 0x08),
                ("D.E. (L.B.)", 0x09),
                ("Thor .45-70", 0x0A),
                ("1911 Custom", 0x0B),
                ("Race Gun", 0x0C),
                ("Solar Gun", 0x0D),
                ("PSS", 0x0E),
                ("G18C", 0x0F),
                ("Type 17", 0x10),
                ("MP7", 0x11),
                ("MP5SD2", 0x12),
                ("M10", 0x13),
                ("P90", 0x14),
                ("Bizon", 0x15),
                ("Patriot", 0x16),
                ("VZ.83", 0x17),
                ("M4 Custom", 0x18),
                ("AK-102", 0x19),
                ("G3A3", 0x1A),
                ("AN94", 0x1B),
                ("FAL Carbine", 0x1C),
                ("Tanegashima", 0x1D),
                ("Mk.17", 0x1E),
                ("XM8", 0x1F),
                ("Mk.46 Mod1", 0x20),
                ("HK21E", 0x21),
                ("PKM", 0x22),
                ("M60E4", 0x23),
                ("Twin Barrel", 0x24),
                ("M870 Custom", 0x25),
                ("Saiga12", 0x26),
                ("VSS", 0x27),
                ("M82A2", 0x28),
                ("DSR-1", 0x29),
                ("M14 EBR", 0x2A),
                ("Mosin-Nagant", 0x2B),
                ("SVD", 0x2C),
                ("Rail Gun", 0x2D),
                ("MGL-140", 0x2E),
                ("XM25", 0x2F),
                ("FIM-92A", 0x30),
                ("Javelin", 0x31),
                ("RPG-7", 0x32),
                ("M72A3", 0x33),
                ("Grenade", 0x34),
                ("WP Grenade", 0x35),
                ("Stun Grenade", 0x36),
                ("Chaff Grenade", 0x37),
                ("Smoke Grenade", 0x38),
                ("Smoke Grenade (Red)", 0x39),
                ("Smoke Grenade (Green)", 0x3A),
                ("Smoke Grenade (Yellow)", 0x3B),
                ("Smoke Grenade (Blue)", 0x3C),
                ("Petrol Bomb", 0x3D),
                ("Magazine", 0x3E),
                ("Claymore", 0x40),
                ("SG Mine", 0x41),
                ("C4", 0x42),
                ("SG Satchel", 0x43),
                ("SOP Destabilizer", 0x44),
                ("Playboy", 0x45),
                ("Emotion Magazine", 0x46),
                ("Mantis Doll", 0x47),
                ("Sorrow Doll", 0x48),
            };
        }

        public static class InventoryUnlocks
        {
            public const long ItemBase = -0x154;
            public const long ItemStride = 0x48;

            public static readonly (string Name, int Slot)[] OutfitSlots =
            {
                // ("OctoCamo (default -- likely always 1, no unlock needed)", 0x18),
                ("Middle East Rebel Disguise", 0x19),
                ("South American Rebel Disguise (Available for selection after ACT 2)", 0x1A),
                ("Civilian Disguise (Available for selection after ACT 3)", 0x1B),
                ("Altair", 0x1C),
                ("Suit", 0x1D),
            };

            public static readonly (string Name, int Slot)[] FaceCamoSlots =
            {
                ("Young Snake", 0x1F),
                ("Screaming Beauty", 0x20),
                ("Laughing Beauty", 0x21),
                ("Raging Beauty", 0x22),
                ("Crying Beauty", 0x23),
                ("Young Snake w/ Bandana", 0x24),
                ("Otacon", 0x25),
                ("Campbell", 0x26),
                ("Big Boss", 0x27),
                ("Drebin", 0x28),
                ("MGS1", 0x29),
                ("Raiden A", 0x2A),
                ("Raiden B", 0x2B)
            };

            public static readonly (string Name, int Slot)[] BodyColourSlots =
            {
                ("Khaki", 0x2C),
                ("Olive", 0x2D),
                ("Black", 0x2E),
                ("Gray", 0x2F),
                ("Navy Blue", 0x30),
                ("Green", 0x31),
                ("Blue", 0x32),
                ("Red", 0x33),
                ("Orange", 0x34),
                ("Tan", 0x35),
            };

            public static readonly (string Name, int Slot)[] SongSlots =
            {
                ("Warhead Storage", 0x3C),
                ("On Alert", 0x3D),
                ("Metal Gear Solid Theme (Document Remix)", 0x3E),
                ("On the Edge", 0x3F),
                ("'Yell' Dead Cell", 0x40),
                ("Sailor", 0x41),
                ("Show Time", 0x42),
                ("Old L.A. 2040", 0x43),
                ("Policenauts End Time", 0x44),
                ("Love Theme Action", 0x45),
                ("Lunar Knights Main Theme", 0x46),
                ("Flowing Destiny", 0x47),
                ("Beyond the Bounds", 0x48),
                ("Inori no Uta", 0x49),
                ("Bio Hazard", 0x4A),
                ("One Night in Neo Kobe City", 0x4B),
                ("Theme of Solid Snake", 0x4C),
                ("Zanzibarland Breeze", 0x4D),
                ("Metal Gear Solid 20 Years History (Part 2)", 0x4E),
                ("Metal Gear Solid 20 Years History (Part 3)", 0x4F),
                ("Big Boss", 0x50),
                ("The Essence of Vince", 0x51),
                ("The Subject of Duality", 0x52),
                ("Theme of Tara", 0x53),
                ("Subsistance Action", 0x54),
                ("Shin Bokura no Taiyou Theme", 0x55),
                ("MPO+ Theme", 0x56),
                ("The Fury", 0x57),
                ("Level 3 Warning", 0x58),
                ("The Best is Yet to Come", 0x59),
                ("Rock Me Baby", 0x5A),
                ("Destiny Calls", 0x5B),
                ("Boktai 2 Theme", 0x5C),
                ("Snake Eater", 0x5D),
                ("Gekko", 0x5E),
                ("Desperate Chase", 0x5F),
                ("Midnight Shadow", 0x60),
                ("Mobs Alive", 0x61),
            };
        }

        // The consumable/equipment item array itself -- same [pInv]-0x154 + 0x48*slot addressing as
        // InventoryUnlocks above (same struct, slots 0x00-0x17 instead of the outfit/facecamo flags
        // past 0x17), confirmed against the source CT's own Items group. Two different behaviors live
        // in this same array, confirmed by hand in-game:
        //  - Ration..Compress (0x00-0x04) are real stock counts -- current/max are an actual carried
        //    amount, editable like any other stat.
        //  - Solid Eye..Scanning Plug (0x05-0x11) are one-time equipment unlocks, not counts -- current
        //    uses the exact same sentinel as the Outfit/FaceCamo ownership flags (0xFFFF = not owned,
        //    1 = owned; max is irrelevant), so these reuse InventoryUnlocks' AddUnlockRow/_unlockRows
        //    machinery directly rather than a numeric field.
        public static class InventoryItems
        {
            public static readonly (string Name, int Slot)[] AmountSlots =
            {
                ("Ration", 0x00),
                ("Noodles", 0x01),
                ("Regain", 0x02),
                ("Pentazemin", 0x03),
                ("Compress", 0x04),
            };

            public static readonly (string Name, int Slot)[] UnlockSlots =
            {
                ("Solid Eye", 0x05),
                ("MG Mk. II", 0x06),
                ("Camera", 0x07),
                ("C. Box A", 0x08),
                ("Drum Can", 0x09),
                ("iPod", 0x0A),
                ("Radio", 0x0B),
                ("Cigs", 0x0C),
                ("Muna", 0x0D),
                ("Bandana", 0x0E),
                ("Stealth", 0x0F),
                ("Syringe", 0x10),
                ("Scanning Plug", 0x11),
            };
        }

        public static class CodePatches
        {
            // ---- Static-offset freeze cheats: NOP `OriginalBytes.Length` bytes at ModuleOffset to enable,
            // write OriginalBytes back to disable. The write instruction just never executes.

            public static class InfiniteLife
            {
                // mov [r9+0xB48], ax
                public const long ModuleOffset = 0x96C563;
                public static readonly byte[] OriginalBytes = { 0x66, 0x41, 0x89, 0x89, 0x48, 0x0B, 0x00, 0x00 };
            }

            public static class InfiniteStamina
            {
                // mov [r9+0xB4C], ax
                public const long ModuleOffset = 0x96C5C6;
                public static readonly byte[] OriginalBytes = { 0x66, 0x41, 0x89, 0x89, 0x4C, 0x0B, 0x00, 0x00 };
            }

            public static class NeverStress
            {
                // mov [r9+0xB50], ax
                public const long ModuleOffset = 0x9716F1;
                public static readonly byte[] OriginalBytes = { 0x66, 0x41, 0x89, 0x81, 0x50, 0x0B, 0x00, 0x00 };
            }

            public static class InfiniteAmmo
            {
                // mov [r8+0xA0], ax -- writes the new clip count back after firing/reloading.
                public const long ModuleOffset = 0x6E2C7;
                public static readonly byte[] OriginalBytes = { 0x66, 0x41, 0x89, 0x80, 0xA0, 0x00, 0x00, 0x00 };
            }

            public static class NeverReload
            {
                // mov [rbx+0x30], cx -- tested on AK-102 only.
                public const long ModuleOffset = 0x6E25D;
                public static readonly byte[] OriginalBytes = { 0x66, 0x89, 0x4B, 0x30 };
            }

            public static class InfiniteThrowables
            {
                // mov [r8+0xA0], ax -- tested on frag/magazine.
                public const long ModuleOffset = 0x6E3BF;
                public static readonly byte[] OriginalBytes = { 0x66, 0x41, 0x89, 0x80, 0xA0, 0x00, 0x00, 0x00 };
            }

            public static class InfiniteItems
            {
                // mov [rax+0x14], cx -- tested on Rations only.
                public const long ModuleOffset = 0x6CE3B;
                public static readonly byte[] OriginalBytes = { 0x66, 0x89, 0x48, 0x14 };
            }

            // ---- AOB-based freeze: no static offset recorded in the source table, located by pattern each toggle.
            // Enable = NOP the whole pattern length; disable = write AobPattern back (it doubles as the original bytes).

            public static class InfiniteSuppressor
            {
                // Two separate decrements fire per silenced shot
                // durability right at the scan match (dec word ptr [rax+0x30])
                // then the ammo-stock entry 8 bytes further into the same instruction stream (dec word ptr
                // [rdi+rdx*8+0xA0]), with an untouched "lea rdx,[rcx+rcx]" index-compute instruction
                // sitting between them that must be left alone.
                public static readonly byte[] ScanPattern = { 0x66, 0xFF, 0x48, 0x30, 0x48, 0x8D, 0x14, 0x49 };
                public const int Patch1Offset = 0;
                public static readonly byte[] Patch1Original = { 0x66, 0xFF, 0x48, 0x30 };
                public const int Patch2Offset = 8;
                public static readonly byte[] Patch2Original = { 0x66, 0xFF, 0x8C, 0xD7, 0xA0, 0x00, 0x00, 0x00 };
            }

            // ---- AOB-based custom patch: enable/disable byte counts differ, so both are stored explicitly.

            public static class NoAlerts
            {
                // Located via the alert-trigger function's prologue; enabling overwrites its first 3 bytes
                // with "xor eax,eax; ret" so the function does nothing and returns immediately.
                public static readonly byte[] AobPattern = { 0x48, 0x89, 0x4C, 0x24, 0x08, 0x53, 0x48, 0x81, 0xEC, 0x90, 0x00 };
                public static readonly byte[] EnableBytes = { 0x33, 0xC0, 0xC3 };
                public static readonly byte[] DisableBytes = { 0x48, 0x89, 0x4C, 0x24, 0x08 };
            }

            // ---- Code injection (trampoline): the stored value gets recalculated every frame, so a plain
            // write gets stomped. Instead we hijack the instruction that reads/writes it and force our own
            // value through every time it runs. AobPattern doubles as the bytes to restore on disable.

            public static class Camo100
            {
                // movsx eax, word ptr [rdi+0x138] -- both of the game's camo-% update paths converge here.
                // Cave: mov word ptr [rdi+0x138], 100; then re-run the original movsx; then jmp back.
                public static readonly byte[] AobPattern = { 0x0F, 0xBF, 0x87, 0x38, 0x01, 0x00, 0x00 };
            }

            public static class InfiniteBattery
            {
                // The real drain write is "mov [r8+0xB52],cx"
                // (fires while a battery-consuming item like Solid Eye/Scanning Plug is active); the real
                // recharge write is a separate "mov [r9+0xB52],dx", left untouched. NOPping the
                // drain write alone (it never gets to overwrite the current value with the lower computed
                // one) stops the decrease, and one immediate top-off write to max on enable (both offsets
                // live at linkvarbuf+0xB52/+0xB54)
                public static readonly byte[] AobPattern = { 0x66, 0x41, 0x89, 0x88, 0x52, 0x0B, 0x00, 0x00 };
                public const long CurrentOffset = 0xB52;
                public const long MaxOffset = 0xB54;
            }

            public static class InventoryPointer
            {
                // rax lands on the inventory/weapon-table anchor here (cmp word ptr [rax+0x14],0),
                // captured the same way as this file's other AOB-based pointer captures. Needed before
                // Constants.InventoryUnlocks below (or any other [pInv]-relative address) resolves.
                public static readonly byte[] ScanPattern = { 0x66, 0x83, 0x78, 0x14, 0x00, 0x7E, 0x20, 0xF6, 0x83 };
                public static readonly byte[] PatchBytes = { 0x66, 0x83, 0x78, 0x14, 0x00 };
                public const int PatchLength = 5;
            }

            public static class EnemyControl
            {
                // mov eax,[rdi+0x324] (enemy sleep timer read) followed by fixed context bytes that make the
                // 6-byte instruction alone unique enough to scan for. Only PatchLength bytes get overwritten;
                // the context bytes after them are read-only and must stay untouched.
                public static readonly byte[] ScanPattern =
                {
                    0x8B, 0x87, 0x24, 0x03, 0x00, 0x00, 0xF7, 0xD8, 0x48, 0x63, 0xC8,
                    0x48, 0xC1, 0xF9, 0x3F, 0x48, 0x83, 0xC1, 0x01, 0x74, 0x04,
                };
                public static readonly byte[] PatchBytes = { 0x8B, 0x87, 0x24, 0x03, 0x00, 0x00 };
                public const int PatchLength = 6;
                public const long HealthOffset = 0x314;
                public const long SleepTimerOffset = 0x324;
            }
        }
    }
}
