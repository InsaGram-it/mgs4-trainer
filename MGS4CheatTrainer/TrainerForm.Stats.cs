using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private enum StatType { UInt16, UInt32 }

        private sealed record StatField(string Label, long Offset, StatType Type, long? SecondaryOffset = null);

        private static readonly (string Name, ushort Value)[] DifficultyLevels =
        {
            ("Liquid Easy", 20),
            ("Naked Normal", 30),
            ("Solid Normal", 35),
            ("Big Boss Hard", 40),
            ("The Boss Extreme", 50),
        };

        private readonly Dictionary<string, (StatField Field, TextBox Box)> _statFields = new();
        private readonly List<(TextBox Box, long Offset)> _tickDisplayBoxes = new();
        private TextBox _stageCodeBox = null!;
        private ComboBox _difficultyCombo = null!;

        private TabPage BuildStatsTab()
        {
            var page = new TabPage("Stats");

            var refreshButton = new Button { Text = "Refresh Now", Dock = DockStyle.Top, Height = 28 };
            refreshButton.Click += (_, _) => RefreshStats(manual: true);

            var noteLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(12, 4, 12, 0),
                ForeColor = Color.DimGray,
                Text = $"Auto-updates every {StatsPollIntervalMs}ms while a run is active. A field you're editing is left alone until you click away.",
            };

            var subTabs = new TabControl { Dock = DockStyle.Fill };
            subTabs.TabPages.Add(BuildRunInfoTab());
            subTabs.TabPages.Add(BuildCombatTab());
            subTabs.TabPages.Add(BuildMovementTab());
            subTabs.TabPages.Add(BuildItemsSocialTab());
            subTabs.TabPages.Add(BuildEmblemsTab());
            subTabs.SelectedIndexChanged += (_, _) => OnVisibleTabChanged();

            page.Controls.Add(subTabs);
            page.Controls.Add(noteLabel);
            page.Controls.Add(refreshButton);
            return page;
        }

        private TabPage BuildRunInfoTab()
        {
            var page = new TabPage("Run Info") { AutoScroll = true };
            int y = 12;

            _stageCodeBox = AddReadOnlyRow(page, ref y, "Stage Code");
            AddDifficultyRow(page, ref y);
            AddStatRow(page, ref y, "Completed Playthroughs", Constants.LiveStats.CompletedPlaythroughsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Scenario Progress", Constants.LiveStats.ScenarioProgressOffset, StatType.UInt32);
            AddTicksRow(page, ref y, "Total Play Time", Constants.LiveStats.TotalPlayTimeOffset);
            AddStatRow(page, ref y, "Drebin Points", Constants.LiveStats.DrebinPointsOffset, StatType.UInt32, Constants.LiveStats.DrebinPointsCopyOffset);
            AddStatRow(page, ref y, "Continues", Constants.LiveStats.ContinuesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Alert Phases", Constants.LiveStats.AlertsOffset, StatType.UInt16);
            // AddStatRow(page, ref y, "Title-Init Field (raw)", Constants.LiveStats.TitleInitFieldOffset, StatType.UInt16);

            return page;
        }

        private TabPage BuildCombatTab()
        {
            var page = new TabPage("Combat") { AutoScroll = true };
            int y = 12;

            AddStatRow(page, ref y, "Kills", Constants.LiveStats.KillsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Headshots", Constants.LiveStats.HeadshotsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "CQC Uses", Constants.LiveStats.CqcUsesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Knife Kills", Constants.LiveStats.KnifeKillsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Knife Knockouts", Constants.LiveStats.KnifeKnockoutsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Combat Highs", Constants.LiveStats.CombatHighsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Special Item Use", Constants.LiveStats.SpecialItemUseOffset, StatType.UInt16);

            return page;
        }

        private TabPage BuildMovementTab()
        {
            var page = new TabPage("Movement") { AutoScroll = true };
            int y = 12;

            AddStatRow(page, ref y, "Prone Side Rolls", Constants.LiveStats.ProneSideRollsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Forward Rolls", Constants.LiveStats.ForwardRollsOffset, StatType.UInt16);
            AddTicksRow(page, ref y, "Standing Time", Constants.LiveStats.StandingTimeOffset);
            AddTicksRow(page, ref y, "Crouch Time", Constants.LiveStats.CrouchTimeOffset);
            AddTicksRow(page, ref y, "Crawl Time", Constants.LiveStats.CrawlTimeOffset);
            AddTicksRow(page, ref y, "On Back Time", Constants.LiveStats.OnBackTimeOffset);
            AddTicksRow(page, ref y, "Wall-Press Time", Constants.LiveStats.WallPressTimeOffset);
            AddTicksRow(page, ref y, "Box/Drum Timer A", Constants.LiveStats.BoxDrumTimerAOffset);
            AddTicksRow(page, ref y, "Box/Drum Timer B", Constants.LiveStats.BoxDrumTimerBOffset);

            return page;
        }

        private TabPage BuildItemsSocialTab()
        {
            var page = new TabPage("Items && Social") { AutoScroll = true };
            int y = 12;

            AddStatRow(page, ref y, "Weapon Pickups", Constants.LiveStats.WeaponPickupsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Item Pickups", Constants.LiveStats.ItemPickupsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Hold-Ups", Constants.LiveStats.HoldUpsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Body Searches", Constants.LiveStats.BodySearchesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Praises", Constants.LiveStats.PraisesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Items Donated", Constants.LiveStats.ItemsDonatedOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Syringe Uses", Constants.LiveStats.SyringeUsesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Scanning Plug Uses", Constants.LiveStats.ScanningPlugUsesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Playboy Pages Turned", Constants.LiveStats.PlayboyPagesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Emotion Mag. Pages Turned", Constants.LiveStats.EmotionMagazinePagesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Recovery Items Used", Constants.LiveStats.RecoveryItemsUsedOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Flashbacks Viewed", Constants.LiveStats.FlashbacksViewedOffset, StatType.UInt16);

            return page;
        }

        private void AddStatRow(Control parent, ref int y, string label, long offset, StatType type, long? secondaryOffset = null)
        {
            var nameLabel = new Label { Text = label, Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var textBox = new TextBox { Left = ValueLeft, Top = y, Width = ValueWidth };
            var setButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth, Height = 28 };

            var field = new StatField(label, offset, type, secondaryOffset);
            setButton.Click += (_, _) => SetStat(field, textBox);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(textBox);
            parent.Controls.Add(setButton);
            _valueRows.Add((parent, nameLabel, textBox, setButton));
            _statFields[label] = (field, textBox);
            y += RowHeight;
        }

        private TextBox AddReadOnlyRow(Control parent, ref int y, string label)
        {
            var nameLabel = new Label { Text = label, Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var box = new TextBox
            {
                Left = ValueLeft,
                Top = y,
                Width = ValueWidth + ButtonWidth + (ButtonLeft - ValueLeft - ValueWidth),
                ReadOnly = true,
            };
            parent.Controls.Add(nameLabel);
            parent.Controls.Add(box);
            _valueRows.Add((parent, nameLabel, box, null));
            y += RowHeight;
            return box;
        }

        private void AddTicksRow(Control parent, ref int y, string label, long offset)
        {
            AddStatRow(parent, ref y, $"{label} (ticks)", offset, StatType.UInt32);
            TextBox display = AddReadOnlyRow(parent, ref y, $"{label} (H:M:S)");
            _tickDisplayBoxes.Add((display, offset));
        }

        private void AddDifficultyRow(Control parent, ref int y)
        {
            var nameLabel = new Label { Text = "Difficulty", Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var combo = new ComboBox { Left = ValueLeft, Top = y, Width = ValueWidth, DropDownStyle = ComboBoxStyle.DropDown };
            foreach (var level in DifficultyLevels)
            {
                combo.Items.Add(level.Name);
            }
            var setButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth, Height = 28 };
            setButton.Click += (_, _) => SetDifficulty(combo);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(combo);
            parent.Controls.Add(setButton);
            _valueRows.Add((parent, nameLabel, combo, setButton));
            _difficultyCombo = combo;
            y += RowHeight;
        }

        // Every duration stat in linkvarbuf (play time, crouch/crawl/wall-press time, the box/drum
        // timers) is stored as 60Hz ticks (per bbtracker's mgs4_research.md), which isn't very
        // readable on its own -- AddTicksRow pairs the raw editable value with an H:M:S readout
        // using this.
        private static string FormatTicksAsTime(uint ticks)
        {
            long totalSeconds = ticks / 60;
            long hours = totalSeconds / 3600;
            int minutes = (int)(totalSeconds % 3600 / 60);
            int seconds = (int)(totalSeconds % 60);
            return $"{hours}:{minutes:D2}:{seconds:D2}";
        }

        private IntPtr? ResolveLinkVarBuf()
        {
            IntPtr pointerAddress = IntPtr.Add(_baseAddress, (int)Constants.LiveStats.LinkVarBufPointerOffset);
            IntPtr linkVarBuf = MemoryManager.ReadIntPtr(_processHandle, pointerAddress);
            return linkVarBuf == IntPtr.Zero ? null : linkVarBuf;
        }

        private void RefreshStats(bool manual)
        {
            RefreshExperimental();
            RefreshInventoryUnlocks();
            RefreshItems();
            RefreshWeapons();
            IntPtr? linkVarBuf = ResolveLinkVarBuf();
            byte[]? snapshot = linkVarBuf == null ? null : MemoryManager.ReadMemoryBytes(_processHandle, linkVarBuf.Value, Constants.LiveStats.SnapshotSize);
            RefreshEmblems(snapshot);
            if (linkVarBuf == null || snapshot == null)
            {
                if (manual)
                {
                    SetStatus("Stats: no active run found (load a save first)");
                }
                return;
            }

            if (_infiniteRexHealthEnabled
                && MemoryManager.ReadUInt32(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)Constants.LiveStats.RexHealthMaxOffset)) is { } rexMax)
            {
                MemoryManager.WriteUInt32(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)Constants.LiveStats.RexHealthOffset), rexMax);
            }

            foreach (var entry in _statFields)
            {
                (StatField field, TextBox textBox) = entry.Value;
                if (!manual && textBox.Focused)
                {
                    continue;
                }
                textBox.Text = field.Type == StatType.UInt16
                    ? BitConverter.ToUInt16(snapshot, (int)field.Offset).ToString()
                    : BitConverter.ToUInt32(snapshot, (int)field.Offset).ToString();
            }

            foreach (var (box, offset) in _tickDisplayBoxes)
            {
                if (manual || !box.Focused)
                {
                    box.Text = FormatTicksAsTime(BitConverter.ToUInt32(snapshot, (int)offset));
                }
            }

            if (manual || !_stageCodeBox.Focused)
            {
                string rawCode = Encoding.ASCII.GetString(snapshot, (int)Constants.LiveStats.StageCodeOffset, 7).TrimEnd('\0');
                _stageCodeBox.Text = Constants.StageNames.ByCode.TryGetValue(rawCode, out string? stageName)
                    ? $"{rawCode} — {stageName}"
                    : rawCode;
            }

            if (manual || (!_difficultyCombo.Focused && !_difficultyCombo.DroppedDown))
            {
                ushort difficulty = BitConverter.ToUInt16(snapshot, (int)Constants.LiveStats.DifficultyOffset);
                var match = DifficultyLevels.FirstOrDefault(l => l.Value == difficulty);
                _difficultyCombo.Text = match.Name ?? $"{difficulty} (unknown)";
            }

            if (manual)
            {
                SetStatus("Stats: refreshed");
            }
        }

        private void SetStat(StatField field, TextBox textBox)
        {
            IntPtr? linkVarBuf = ResolveLinkVarBuf();
            if (linkVarBuf == null)
            {
                SetStatus("Stats: no active run found (load a save first)");
                return;
            }

            bool ok;
            string display;
            if (field.Type == StatType.UInt16)
            {
                if (!ushort.TryParse(textBox.Text, out ushort value))
                {
                    SetStatus($"{field.Label}: enter a whole number 0-65535");
                    return;
                }
                ok = MemoryManager.WriteUInt16(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)field.Offset), value);
                if (ok && field.SecondaryOffset is { } secondary16)
                {
                    ok &= MemoryManager.WriteUInt16(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)secondary16), value);
                }
                display = value.ToString();
            }
            else
            {
                if (!uint.TryParse(textBox.Text, out uint value))
                {
                    SetStatus($"{field.Label}: enter a whole number 0-4294967295");
                    return;
                }
                ok = MemoryManager.WriteUInt32(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)field.Offset), value);
                if (ok && field.SecondaryOffset is { } secondary32)
                {
                    ok &= MemoryManager.WriteUInt32(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)secondary32), value);
                }
                display = value.ToString();
            }

            SetStatus(ok ? $"{field.Label}: set to {display}" : $"{field.Label}: write failed");
        }

        private void SetDifficulty(ComboBox combo)
        {
            IntPtr? linkVarBuf = ResolveLinkVarBuf();
            if (linkVarBuf == null)
            {
                SetStatus("Stats: no active run found (load a save first)");
                return;
            }

            string text = combo.Text.Trim();
            var matched = DifficultyLevels.FirstOrDefault(l => string.Equals(l.Name, text, StringComparison.OrdinalIgnoreCase));
            ushort value;
            if (matched.Name != null)
            {
                value = matched.Value;
            }
            else if (!ushort.TryParse(text, out value))
            {
                SetStatus("Difficulty: pick a level or enter a raw number");
                return;
            }

            bool ok = MemoryManager.WriteUInt16(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)Constants.LiveStats.DifficultyOffset), value);
            SetStatus(ok ? $"Difficulty: set to {value}" : "Difficulty: write failed");
        }
    }
}
