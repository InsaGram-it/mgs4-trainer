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
            public const long RecoveryItemsUsedOffset = 0x0AE0;
            public const long FlashbacksViewedOffset = 0x5A34;
            public const int SnapshotSize = 0x5A36;
        }

        // Instruction patches sourced from a pre-existing community cheat table (mgs4-snake-swiss.ct)
        // and confirmed byte-for-byte against this game build via `readabs base+<offset> bytearray <len>`.
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
                // dec word [rax+0x30] -- suppressor durability decrement.
                public static readonly byte[] AobPattern = { 0x66, 0xFF, 0x48, 0x30 };
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
                // mov word ptr [rdx+0xB52], ax -- battery charge write.
                // Cave: only let it write if the new value is >= the current one (blocks drains, allows recharge).
                public static readonly byte[] AobPattern = { 0x66, 0x89, 0x82, 0x52, 0x0B, 0x00, 0x00 };
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
