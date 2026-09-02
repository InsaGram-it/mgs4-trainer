using System.Drawing;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        // Snapshot of the linkvarbuf fields the 40 emblem predicates need, read once per stats tick.
        private sealed record EmblemMetrics(
            ushort Difficulty, ushort Alerts, ushort Kills, ushort Continues, ushort Recovery,
            ushort SpecialItemUse, uint PlayTimeTicks, ushort CqcUses, ushort Headshots,
            ushort KnifeKills, ushort KnifeKnockouts, ushort ItemsDonated, ushort Praises,
            ushort BodySearches, ushort HoldUps, uint BoxDrumA, uint BoxDrumB, ushort PlayboyPages,
            ushort SyringeUses, ushort ScanningPlugUses, uint WallPressTime, ushort ProneSideRolls,
            ushort ForwardRolls, uint CrawlTime, uint CrouchTime, ushort WeaponPickups,
            ushort ItemPickups, ushort CombatHighs)
        {
            public double Hours => PlayTimeTicks / 216000.0; // 60 ticks/sec * 3600 sec/hour
            public double KnifeDefeats => KnifeKills + KnifeKnockouts;
            public double MedicalUses => SyringeUses + ScanningPlugUses;
            public double BoxDrumMinutes => (BoxDrumA + BoxDrumB) / 3600.0; // 60 ticks/sec * 60 sec/min
            public double WallPressMinutes => WallPressTime / 3600.0;
            public double CrawlMinutes => CrawlTime / 3600.0;
            public double CrouchMinutes => CrouchTime / 3600.0;
            public double PickupsTotal => WeaponPickups + ItemPickups;
        }

        private static readonly (string Name, string Description, Func<EmblemMetrics, bool?> Check)[] EmblemRules =
        {
            ("BIG BOSS", "Extreme; 0 alerts/kills/continues/recovery; ≤5h; no special item",
                m => m.Difficulty == 50 && m.Alerts == 0 && m.Kills == 0 && m.Continues == 0 && m.Recovery == 0 && m.SpecialItemUse == 0 && m.Hours <= 5),
            ("FOX HOUND", "Hard+; ≤3 alerts; 0 kills/continues/recovery; ≤5.5h; no special item",
                m => m.Difficulty >= 40 && m.Alerts <= 3 && m.Kills == 0 && m.Continues == 0 && m.Recovery == 0 && m.SpecialItemUse == 0 && m.Hours <= 5.5),
            ("FOX", "Solid Normal+; ≤5 alerts; 0 kills/continues/recovery; ≤6h; no special item",
                m => m.Difficulty >= 35 && m.Alerts <= 5 && m.Kills == 0 && m.Continues == 0 && m.Recovery == 0 && m.SpecialItemUse == 0 && m.Hours <= 6),
            ("HOUND", "Naked Normal+; ≤10 alerts; 0 kills/continues/recovery; ≤6.5h; no special item",
                m => m.Difficulty >= 30 && m.Alerts <= 10 && m.Kills == 0 && m.Continues == 0 && m.Recovery == 0 && m.SpecialItemUse == 0 && m.Hours <= 6.5),
            ("MANTIS", "0 alerts/continues/recovery; ≤5h",
                m => m.Alerts == 0 && m.Continues == 0 && m.Recovery == 0 && m.Hours <= 5),
            ("WOLF", "0 continues; 0 recovery items",
                m => m.Continues == 0 && m.Recovery == 0),
            ("RAVEN", "Finish in ≤5h",
                m => m.Hours <= 5),
            ("OCTOPUS", "0 alert phases",
                m => m.Alerts == 0),
            ("BEAR", "≥100 CQC holds",
                m => m.CqcUses >= 100),
            ("EAGLE", "≥150 headshots",
                m => m.Headshots >= 150),
            ("ASSASSIN", "≥50 knife kills/stuns; ≥50 CQC holds; ≤25 alerts",
                m => m.KnifeDefeats >= 50 && m.CqcUses >= 50 && m.Alerts <= 25),
            ("PIGEON", "0 kills",
                m => m.Kills == 0),
            ("BLUE BIRD", "≥50 items donated",
                m => m.ItemsDonated >= 50),
            ("HAWK", "≥25 praises from allies",
                m => m.Praises >= 25),
            ("LITTLE GRAY", "Collect all 69 weapons (needs weapon-inventory data, not tracked here)",
                _ => null),
            ("ANT", "≥50 body searches",
                m => m.BodySearches >= 50),
            ("GIBBON", "≥50 hold-ups",
                m => m.HoldUps >= 50),
            ("TORTOISE", "≥60 min inside box/drum can",
                m => m.BoxDrumMinutes >= 60),
            ("RABBIT", "≥100 Playboy pages turned",
                m => m.PlayboyPages >= 100),
            ("BEE", "≥50 Syringe + Scanning Plug uses",
                m => m.MedicalUses >= 50),
            ("GECKO", "≥60 min wall-pressing",
                m => m.WallPressMinutes >= 60),
            ("SCARAB", "≥100 prone side rolls",
                m => m.ProneSideRolls >= 100),
            ("FROG", "≥200 forward rolls",
                m => m.ForwardRolls >= 200),
            ("INCH WORM", "≥60 min crawling",
                m => m.CrawlMinutes >= 60),
            ("LOBSTER", "≥150 min crouching",
                m => m.CrouchMinutes >= 150),
            ("HYENA", "≥400 weapon/item pickups",
                m => m.PickupsTotal >= 400),
            ("HOG", "≥10 combat highs",
                m => m.CombatHighs >= 10),
            ("PIG", "≥40 recovery items used",
                m => m.Recovery >= 40),
            ("COW", "≥100 alert phases",
                m => m.Alerts >= 100),
            ("CROCODILE", "≥400 kills",
                m => m.Kills >= 400),
            ("GIANT PANDA", "≥30h play time",
                m => m.Hours >= 30),
            ("SCORPION", "≤75 alerts, ≤250 kills, ≤25 continues",
                m => m.Alerts <= 75 && m.Kills <= 250 && m.Continues <= 25),
            ("TARANTULA", "≤75 alerts, >250 kills, ≤25 continues",
                m => m.Alerts <= 75 && m.Kills > 250 && m.Continues <= 25),
            ("CENTIPEDE", "≤75 alerts, ≤250 kills, >25 continues",
                m => m.Alerts <= 75 && m.Kills <= 250 && m.Continues > 25),
            ("SPIDER", "≤75 alerts, >250 kills, >25 continues",
                m => m.Alerts <= 75 && m.Kills > 250 && m.Continues > 25),
            ("JAGUAR", ">75 alerts, ≤250 kills, ≤25 continues",
                m => m.Alerts > 75 && m.Kills <= 250 && m.Continues <= 25),
            ("PANTHER", ">75 alerts, >250 kills, ≤25 continues",
                m => m.Alerts > 75 && m.Kills > 250 && m.Continues <= 25),
            ("LEOPARD", ">75 alerts, ≤250 kills, >25 continues",
                m => m.Alerts > 75 && m.Kills <= 250 && m.Continues > 25),
            ("PUMA", ">75 alerts, >250 kills, >25 continues",
                m => m.Alerts > 75 && m.Kills > 250 && m.Continues > 25),
            ("CHICKEN", "≥150 alerts, ≥500 kills, ≥50 continues, ≥50 recovery, ≥35h",
                m => m.Alerts >= 150 && m.Kills >= 500 && m.Continues >= 50 && m.Recovery >= 50 && m.Hours >= 35),
        };

        private readonly List<(Label Status, Func<EmblemMetrics, bool?> Evaluate)> _emblemRows = new();

        private TabPage BuildEmblemsTab()
        {
            var page = new TabPage("Emblems") { AutoScroll = true };
            int y = 8;

            var noteLabel = new Label
            {
                Left = LabelLeft,
                Top = y,
                Height = 42,
                AutoSize = false,
                Text = "Live progress toward MGS4's 40 completion emblems, judged against a single continuous playthrough",
                ForeColor = Color.DimGray,
            };
            page.Controls.Add(noteLabel);
            _stretchOnly.Add((page, noteLabel));
            y += 48;

            foreach (var (name, description, check) in EmblemRules)
            {
                AddEmblemRow(page, ref y, name, description, check);
            }

            return page;
        }

        private void AddEmblemRow(Control parent, ref int y, string name, string description, Func<EmblemMetrics, bool?> check)
        {
            var nameLabel = new Label
            {
                Text = name,
                Left = LabelLeft,
                Top = y,
                Width = 240,
                Height = 18,
                Font = new Font(Font, FontStyle.Bold),
            };
            var statusLabel = new Label
            {
                Text = "?",
                Left = 0,
                Top = y,
                Width = 110,
                Height = 18,
                TextAlign = ContentAlignment.MiddleRight,
            };
            var descLabel = new Label
            {
                Text = description,
                Left = LabelLeft,
                Top = y + 19,
                Height = 32,
                AutoEllipsis = false,
                ForeColor = ColorTextSecondary,
                Font = new Font(Font.FontFamily, 8.5f),
            };

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(statusLabel);
            parent.Controls.Add(descLabel);
            _valueRows.Add((parent, null, nameLabel, statusLabel));
            _stretchOnly.Add((parent, descLabel));
            _emblemRows.Add((statusLabel, check));
            y += 56;
        }

        private void RefreshEmblems(byte[]? snapshot)
        {
            if (snapshot == null)
            {
                foreach (var (status, _) in _emblemRows)
                {
                    status.Text = "?";
                    status.ForeColor = ColorTextSecondary;
                }
                return;
            }

            ushort Read16(long offset) => BitConverter.ToUInt16(snapshot, (int)offset);
            uint Read32(long offset) => BitConverter.ToUInt32(snapshot, (int)offset);

            var metrics = new EmblemMetrics(
                Read16(Constants.LiveStats.DifficultyOffset),
                Read16(Constants.LiveStats.AlertsOffset),
                Read16(Constants.LiveStats.KillsOffset),
                Read16(Constants.LiveStats.ContinuesOffset),
                Read16(Constants.LiveStats.RecoveryItemsUsedOffset),
                Read16(Constants.LiveStats.SpecialItemUseOffset),
                Read32(Constants.LiveStats.TotalPlayTimeOffset),
                Read16(Constants.LiveStats.CqcUsesOffset),
                Read16(Constants.LiveStats.HeadshotsOffset),
                Read16(Constants.LiveStats.KnifeKillsOffset),
                Read16(Constants.LiveStats.KnifeKnockoutsOffset),
                Read16(Constants.LiveStats.ItemsDonatedOffset),
                Read16(Constants.LiveStats.PraisesOffset),
                Read16(Constants.LiveStats.BodySearchesOffset),
                Read16(Constants.LiveStats.HoldUpsOffset),
                Read32(Constants.LiveStats.BoxDrumTimerAOffset),
                Read32(Constants.LiveStats.BoxDrumTimerBOffset),
                Read16(Constants.LiveStats.PlayboyPagesOffset),
                Read16(Constants.LiveStats.SyringeUsesOffset),
                Read16(Constants.LiveStats.ScanningPlugUsesOffset),
                Read32(Constants.LiveStats.WallPressTimeOffset),
                Read16(Constants.LiveStats.ProneSideRollsOffset),
                Read16(Constants.LiveStats.ForwardRollsOffset),
                Read32(Constants.LiveStats.CrawlTimeOffset),
                Read32(Constants.LiveStats.CrouchTimeOffset),
                Read16(Constants.LiveStats.WeaponPickupsOffset),
                Read16(Constants.LiveStats.ItemPickupsOffset),
                Read16(Constants.LiveStats.CombatHighsOffset));

            foreach (var (status, check) in _emblemRows)
            {
                bool? result = check(metrics);
                if (result == null)
                {
                    status.Text = "N/A";
                    status.ForeColor = ColorTextSecondary;
                }
                else if (result.Value)
                {
                    status.Text = "✓ Qualified";
                    status.ForeColor = Color.FromArgb(120, 210, 120);
                }
                else
                {
                    status.Text = "Not yet";
                    status.ForeColor = Color.FromArgb(210, 110, 110);
                }
            }
        }
    }
}
