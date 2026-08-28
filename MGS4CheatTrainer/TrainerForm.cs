using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public class TrainerForm : Form
    {
        private const int LabelLeft = 12;
        private const int LabelWidth = 170;
        private const int ValueLeft = 186;
        private const int ValueWidth = 110;
        private const int ButtonLeft = 302;
        private const int ButtonWidth = 60;
        private const int RowHeight = 25;

        private const int StatsPollIntervalMs = 500;
        private const int ResizeDebounceMs = 150;

        private readonly Process _gameProcess;
        private readonly IntPtr _processHandle;
        private readonly IntPtr _baseAddress;
        private readonly long _moduleSize;
        private readonly Label _status;
        private readonly TabControl _tabs;
        private readonly System.Windows.Forms.Timer _statsTimer;
        private readonly System.Windows.Forms.Timer _resizeDebounceTimer;
        private Bitmap? _wallpaperSource;

        private bool _suppressEvents;
        private bool _gameClosedHandled;

        // Rows/groups reposition themselves directly off each control's live parent size (see
        // ReflowAll) instead of relying on WinForms' AnchorStyles distance-caching -- a TabPage nested
        // two tab-levels deep isn't reliably given its real size until it's actually been selected at
        // least once, so a captured anchor distance taken before that (e.g. at row-creation time, or
        // even at the form's Load if that tab was never shown) ends up computed against a bogus tiny
        // size and pushes right-pinned controls (the Set buttons) off past the visible edge once the
        // real layout runs. Reading the parent's current ClientSize fresh on every reflow sidesteps
        // that entirely.
        private const int RowOuterMargin = 12;
        private const int RowGap = 6;
        private const int MinFieldWidth = 40;

        private readonly List<(Control Parent, Control Value, Control? Button)> _valueRows = new();
        private readonly List<(Control Parent, Control Control)> _stretchOnly = new();

        private void ReflowAll()
        {
            foreach (var (parent, value, button) in _valueRows)
            {
                int rightEdge = parent.ClientSize.Width - RowOuterMargin;
                if (button != null)
                {
                    button.Left = rightEdge - button.Width;
                    value.Width = Math.Max(MinFieldWidth, button.Left - RowGap - value.Left);
                }
                else
                {
                    value.Width = Math.Max(MinFieldWidth, rightEdge - value.Left);
                }
            }

            foreach (var (parent, control) in _stretchOnly)
            {
                control.Width = Math.Max(MinFieldWidth, parent.ClientSize.Width - control.Left - RowOuterMargin);
            }
        }

        // Resolved live addresses for the AOB-based patches, needed to restore on disable.
        private IntPtr? _suppressorTarget;
        private IntPtr? _noAlertsTarget;
        private IntPtr? _camoTarget;
        private IntPtr? _batteryTarget;

        public TrainerForm(Process gameProcess, IntPtr processHandle, IntPtr baseAddress, long moduleSize)
        {
            _gameProcess = gameProcess;
            _processHandle = processHandle;
            _baseAddress = baseAddress;
            _moduleSize = moduleSize;

            Text = "MGS4 Trainer";
            Width = 620;
            Height = 620;
            MinimumSize = new Size(460, 620);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 46 };

            _status = new Label
            {
                Left = 12,
                Top = 4,
                Width = 260,
                Height = 38,
                Text = "Ready.",
                ForeColor = Color.DimGray,
            };

            var trademark = new Label
            {
                Text = "Made by Insa",
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font(Font.FontFamily, 7.5f, FontStyle.Italic),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };

            bottomPanel.Controls.Add(_status);
            bottomPanel.Controls.Add(trademark);
            trademark.Left = bottomPanel.ClientSize.Width - trademark.Width - 12;
            trademark.Top = bottomPanel.ClientSize.Height - trademark.Height - 4;

            _tabs = new TabControl { Dock = DockStyle.Fill };
            _tabs.TabPages.Add(BuildCheatsTab());
            _tabs.TabPages.Add(BuildStatsTab());
            // A tab page only gets its real size once it's actually been selected/shown -- reflow and
            // recomposite the wallpaper again each time the visible tab changes, to catch whichever
            // page was still unsized (and so still showing a flat fallback color) until now.
            _tabs.SelectedIndexChanged += (_, _) => OnVisibleTabChanged();

            Controls.Add(_tabs);
            Controls.Add(bottomPanel);

            ApplyDarkTheme(this);
            bottomPanel.BackColor = ColorSurface;

            _resizeDebounceTimer = new System.Windows.Forms.Timer { Interval = ResizeDebounceMs };
            _resizeDebounceTimer.Tick += (_, _) =>
            {
                _resizeDebounceTimer.Stop();
                ApplyWallpaperBackgrounds();
            };
            // Row positions are cheap to recompute, so that happens on every resize tick directly; only
            // the wallpaper recomposite (an actual bitmap redraw) waits for dragging to stop.
            Resize += (_, _) =>
            {
                ReflowAll();
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Start();
            };
            Load += (_, _) =>
            {
                _wallpaperSource = LoadEmbeddedWallpaper();
                OnVisibleTabChanged();
            };

            // Live-poll the Stats tab so values track the game without a manual refresh, and use the
            // same tick to notice the game process closing (all reads/writes go quietly nowhere against
            // a dead handle otherwise, which just looks like every field silently breaking).
            _statsTimer = new System.Windows.Forms.Timer { Interval = StatsPollIntervalMs };
            _statsTimer.Tick += (_, _) =>
            {
                if (_gameProcess.HasExited)
                {
                    HandleGameClosed();
                    return;
                }
                RefreshStats(manual: false);
            };
            _statsTimer.Start();
            FormClosed += (_, _) =>
            {
                _statsTimer.Stop();
                _resizeDebounceTimer.Stop();
            };
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

        private const int DwmwaUseImmersiveDarkMode = 20;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int enabled = 1;
            // Best-effort: fails harmlessly (non-zero HRESULT, no throw) on Windows versions without this attribute.
            DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
        }

        private void HandleGameClosed()
        {
            if (_gameClosedHandled)
            {
                return;
            }
            _gameClosedHandled = true;

            _statsTimer.Stop();
            _tabs.Enabled = false;
            _status.ForeColor = Color.Firebrick;
            SetStatus("MGS4 has closed. Relaunch the game and restart this trainer to reconnect.");
        }

        private static byte[] Nops(int count) => Enumerable.Repeat((byte)0x90, count).ToArray();

        private void SetStatus(string message) => _status.Text = message;

        // A background AOB scan (Task.Run) can still be in flight when the window closes; its
        // continuation resumes on this form's SynchronizationContext regardless, so every await
        // that's followed by touching a control checks this first instead of risking an
        // ObjectDisposedException/InvalidOperationException on a torn-down form.
        private bool IsUsable => !IsDisposed && !Disposing;

        private void OnVisibleTabChanged()
        {
            ReflowAll();
            ApplyWallpaperBackgrounds();
        }

        #region Cheats tab (code-patch toggles)

        private TabPage BuildCheatsTab()
        {
            var page = new TabPage("Cheats");

            var survival = BuildGroup("Survival", 12, out int survivalBottom,
                MakeStaticToggle("Infinite Life", Constants.CodePatches.InfiniteLife.ModuleOffset, Constants.CodePatches.InfiniteLife.OriginalBytes),
                MakeStaticToggle("Infinite Stamina", Constants.CodePatches.InfiniteStamina.ModuleOffset, Constants.CodePatches.InfiniteStamina.OriginalBytes),
                MakeStaticToggle("Never Stress", Constants.CodePatches.NeverStress.ModuleOffset, Constants.CodePatches.NeverStress.OriginalBytes));

            var weapons = BuildGroup("Weapons && Items", survivalBottom + 10, out int weaponsBottom,
                MakeStaticToggle("Infinite Ammo", Constants.CodePatches.InfiniteAmmo.ModuleOffset, Constants.CodePatches.InfiniteAmmo.OriginalBytes),
                MakeStaticToggle("Never Reload", Constants.CodePatches.NeverReload.ModuleOffset, Constants.CodePatches.NeverReload.OriginalBytes),
                MakeStaticToggle("Infinite Throwables", Constants.CodePatches.InfiniteThrowables.ModuleOffset, Constants.CodePatches.InfiniteThrowables.OriginalBytes),
                MakeStaticToggle("Infinite Items", Constants.CodePatches.InfiniteItems.ModuleOffset, Constants.CodePatches.InfiniteItems.OriginalBytes),
                MakeAobFreezeToggle("Infinite Suppressor", Constants.CodePatches.InfiniteSuppressor.AobPattern, () => _suppressorTarget, v => _suppressorTarget = v));

            var stealth = BuildGroup("Stealth && Misc", weaponsBottom + 10, out _,
                MakeAobPatchToggle("Camo Always 100%", ToggleCamo100, () => _camoTarget),
                MakeAobPatchToggle("No Alerts", ToggleNoAlerts, () => _noAlertsTarget),
                MakeAobPatchToggle("Infinite Battery", ToggleBattery, () => _batteryTarget));

            page.Controls.Add(survival);
            page.Controls.Add(weapons);
            page.Controls.Add(stealth);
            _stretchOnly.Add((page, survival));
            _stretchOnly.Add((page, weapons));
            _stretchOnly.Add((page, stealth));
            return page;
        }

        private GroupBox BuildGroup(string title, int top, out int bottom, params CheckBox[] checkboxes)
        {
            var group = new GroupBox { Text = title, Left = 12, Top = top, Width = 330 };
            int y = 22;
            foreach (var checkbox in checkboxes)
            {
                checkbox.Left = 12;
                checkbox.Top = y;
                checkbox.Width = 300;
                _stretchOnly.Add((group, checkbox));
                group.Controls.Add(checkbox);
                y += 25;
            }
            group.Height = y + 8;
            bottom = group.Bottom;
            return group;
        }

        // Freeze cheat at a known, always-valid static module offset.
        private CheckBox MakeStaticToggle(string label, long moduleOffset, byte[] originalBytes)
        {
            var checkbox = new CheckBox { Text = label };
            checkbox.CheckedChanged += (_, _) =>
            {
                if (_suppressEvents)
                {
                    return;
                }

                IntPtr address = IntPtr.Add(_baseAddress, (int)moduleOffset);
                byte[] bytes = checkbox.Checked ? Nops(originalBytes.Length) : originalBytes;
                bool ok = MemoryManager.WriteRawBytes(_processHandle, address, bytes);
                SetStatus(ok ? $"{label}: {(checkbox.Checked ? "enabled" : "disabled")}" : $"{label}: write failed");
            };
            return checkbox;
        }

        // Freeze cheat located by AOB scan each time it's enabled (no fixed offset recorded).
        // The scan walks the whole module and can take a moment, so the checkbox is locked
        // and the status shows "Scanning..." until the write actually lands.
        private CheckBox MakeAobFreezeToggle(string label, byte[] aobPattern, Func<IntPtr?> getTarget, Action<IntPtr?> setTarget)
        {
            var checkbox = new CheckBox { Text = label };
            checkbox.CheckedChanged += async (_, _) =>
            {
                if (_suppressEvents)
                {
                    return;
                }

                checkbox.Enabled = false;
                try
                {
                    if (checkbox.Checked)
                    {
                        SetStatus($"{label}: scanning...");
                        IntPtr? target = await Task.Run(() => FindAob(aobPattern));
                        if (!IsUsable)
                        {
                            return;
                        }
                        if (target == null)
                        {
                            SetStatus($"{label}: pattern not found");
                            SetCheckedSilently(checkbox, false);
                            return;
                        }
                        setTarget(target);
                        bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target.Value, Nops(aobPattern.Length)));
                        if (!IsUsable)
                        {
                            return;
                        }
                        SetStatus(ok ? $"{label}: enabled" : $"{label}: write failed");
                    }
                    else if (getTarget() is { } target)
                    {
                        bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target, aobPattern));
                        if (!IsUsable)
                        {
                            return;
                        }
                        SetStatus(ok ? $"{label}: disabled" : $"{label}: write failed");
                    }
                }
                finally
                {
                    if (IsUsable)
                    {
                        checkbox.Enabled = true;
                    }
                }
            };
            return checkbox;
        }

        // Custom AOB-based patches (byte-replace or code injection) that need cheat-specific logic on enable.
        // toggleAction does the scan/patch work; it can be slow, so lock the checkbox and show a status
        // while it runs instead of letting the box flip instantly with no feedback.
        private CheckBox MakeAobPatchToggle(string label, Func<Task> toggleAction, Func<IntPtr?> getTarget)
        {
            var checkbox = new CheckBox { Text = label };
            checkbox.CheckedChanged += async (_, _) =>
            {
                if (_suppressEvents)
                {
                    return;
                }
                checkbox.Enabled = false;
                try
                {
                    if (checkbox.Checked)
                    {
                        SetStatus($"{label}: scanning...");
                    }
                    await toggleAction();
                    if (!IsUsable)
                    {
                        return;
                    }
                    if (checkbox.Checked && getTarget() == null)
                    {
                        // Enable failed (pattern not found / cave alloc failed) -- the toggle method already set status.
                        SetCheckedSilently(checkbox, false);
                    }
                }
                finally
                {
                    if (IsUsable)
                    {
                        checkbox.Enabled = true;
                    }
                }
            };
            _pendingToggles[label] = checkbox;
            return checkbox;
        }

        private void SetCheckedSilently(CheckBox checkbox, bool value)
        {
            _suppressEvents = true;
            checkbox.Checked = value;
            _suppressEvents = false;
        }

        private IntPtr? FindAob(byte[] pattern)
        {
            string mask = new string('x', pattern.Length);
            var matches = MemoryManager.ScanForAllInstances(_processHandle, _baseAddress, _moduleSize, pattern, mask);
            return matches.Count > 0 ? matches[0] : null;
        }

        private readonly Dictionary<string, CheckBox> _pendingToggles = new();

        private async Task ToggleNoAlerts()
        {
            var checkbox = _pendingToggles["No Alerts"];
            if (checkbox.Checked)
            {
                IntPtr? target = await Task.Run(() => FindAob(Constants.CodePatches.NoAlerts.AobPattern));
                if (!IsUsable)
                {
                    return;
                }
                if (target == null)
                {
                    SetStatus("No Alerts: pattern not found");
                    return;
                }
                _noAlertsTarget = target;
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target.Value, Constants.CodePatches.NoAlerts.EnableBytes));
                if (!IsUsable)
                {
                    return;
                }
                SetStatus(ok ? "No Alerts: enabled" : "No Alerts: write failed");
            }
            else if (_noAlertsTarget is { } target)
            {
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target, Constants.CodePatches.NoAlerts.DisableBytes));
                if (!IsUsable)
                {
                    return;
                }
                SetStatus(ok ? "No Alerts: disabled" : "No Alerts: write failed");
            }
        }

        private async Task ToggleCamo100()
        {
            var checkbox = _pendingToggles["Camo Always 100%"];
            if (checkbox.Checked)
            {
                byte[] pattern = Constants.CodePatches.Camo100.AobPattern;
                IntPtr? target = await Task.Run(() => MemoryManager.InjectTrampoline(_processHandle, _baseAddress, _moduleSize, pattern, (targetAddress, cave) =>
                {
                    var body = new List<byte>();
                    body.AddRange(new byte[] { 0x66, 0xC7, 0x87, 0x38, 0x01, 0x00, 0x00, 0x64, 0x00 }); // mov word ptr [rdi+0x138], 100
                    body.AddRange(pattern); // original: movsx eax, word ptr [rdi+0x138]
                    IntPtr returnAddress = IntPtr.Add(targetAddress, pattern.Length);
                    body.AddRange(MemoryManager.JumpRel32(cave, body.Count, returnAddress));
                    return body.ToArray();
                }));

                if (!IsUsable)
                {
                    return;
                }
                if (target == null)
                {
                    SetStatus("Camo Always 100%: pattern not found / cave alloc failed");
                    return;
                }
                _camoTarget = target;
                SetStatus("Camo Always 100%: enabled");
            }
            else if (_camoTarget is { } target)
            {
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target, Constants.CodePatches.Camo100.AobPattern));
                if (!IsUsable)
                {
                    return;
                }
                SetStatus(ok ? "Camo Always 100%: disabled" : "Camo Always 100%: write failed");
            }
        }

        private async Task ToggleBattery()
        {
            var checkbox = _pendingToggles["Infinite Battery"];
            if (checkbox.Checked)
            {
                byte[] pattern = Constants.CodePatches.InfiniteBattery.AobPattern;
                IntPtr? target = await Task.Run(() => MemoryManager.InjectTrampoline(_processHandle, _baseAddress, _moduleSize, pattern, (targetAddress, cave) =>
                {
                    var body = new List<byte>();
                    body.AddRange(new byte[] { 0x66, 0x3B, 0x82, 0x52, 0x0B, 0x00, 0x00 }); // cmp ax, word ptr [rdx+0xB52]
                    int jbOpcodeOffset = body.Count;
                    body.Add(0x72); // jb rel8 (placeholder operand, patched below)
                    body.Add(0x00);
                    body.AddRange(pattern); // original: mov word ptr [rdx+0xB52], ax (only runs if ax >= current)
                    int skipDestinationOffset = body.Count;
                    body[jbOpcodeOffset + 1] = (byte)(skipDestinationOffset - (jbOpcodeOffset + 2));
                    IntPtr returnAddress = IntPtr.Add(targetAddress, pattern.Length);
                    body.AddRange(MemoryManager.JumpRel32(cave, body.Count, returnAddress));
                    return body.ToArray();
                }));

                if (!IsUsable)
                {
                    return;
                }
                if (target == null)
                {
                    SetStatus("Infinite Battery: pattern not found / cave alloc failed");
                    return;
                }
                _batteryTarget = target;
                SetStatus("Infinite Battery: enabled");
            }
            else if (_batteryTarget is { } target)
            {
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target, Constants.CodePatches.InfiniteBattery.AobPattern));
                if (!IsUsable)
                {
                    return;
                }
                SetStatus(ok ? "Infinite Battery: disabled" : "Infinite Battery: write failed");
            }
        }

        #endregion

        #region Stats tab (live linkvarbuf fields)

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
        private TextBox _stageCodeBox = null!;
        private TextBox _playTimeDisplayBox = null!;
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
            subTabs.SelectedIndexChanged += (_, _) => OnVisibleTabChanged();

            page.Controls.Add(subTabs);
            page.Controls.Add(noteLabel);
            page.Controls.Add(refreshButton);
            return page;
        }

        private TabPage BuildRunInfoTab()
        {
            var page = new TabPage("Run Info");
            int y = 12;

            _stageCodeBox = AddReadOnlyRow(page, ref y, "Stage Code");
            AddDifficultyRow(page, ref y);
            AddStatRow(page, ref y, "Completed Playthroughs", Constants.LiveStats.CompletedPlaythroughsOffset, StatType.UInt32);
            AddStatRow(page, ref y, "Scenario Progress", Constants.LiveStats.ScenarioProgressOffset, StatType.UInt32);
            AddStatRow(page, ref y, "Total Play Time (ticks)", Constants.LiveStats.TotalPlayTimeOffset, StatType.UInt32);
            _playTimeDisplayBox = AddReadOnlyRow(page, ref y, "Total Play Time (H:M:S)");
            AddStatRow(page, ref y, "Drebin Points", Constants.LiveStats.DrebinPointsOffset, StatType.UInt32, Constants.LiveStats.DrebinPointsCopyOffset);
            AddStatRow(page, ref y, "Continues", Constants.LiveStats.ContinuesOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Alert Phases", Constants.LiveStats.AlertsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Title-Init Field (raw)", Constants.LiveStats.TitleInitFieldOffset, StatType.UInt16);

            return page;
        }

        private TabPage BuildCombatTab()
        {
            var page = new TabPage("Combat");
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
            var page = new TabPage("Movement");
            int y = 12;

            AddStatRow(page, ref y, "Prone Side Rolls", Constants.LiveStats.ProneSideRollsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Forward Rolls", Constants.LiveStats.ForwardRollsOffset, StatType.UInt16);
            AddStatRow(page, ref y, "Crouch Time (ticks)", Constants.LiveStats.CrouchTimeOffset, StatType.UInt32);
            AddStatRow(page, ref y, "Crawl Time (ticks)", Constants.LiveStats.CrawlTimeOffset, StatType.UInt32);
            AddStatRow(page, ref y, "Wall-Press Time (ticks)", Constants.LiveStats.WallPressTimeOffset, StatType.UInt32);
            AddStatRow(page, ref y, "Box/Drum Timer A (ticks)", Constants.LiveStats.BoxDrumTimerAOffset, StatType.UInt32);
            AddStatRow(page, ref y, "Box/Drum Timer B (ticks)", Constants.LiveStats.BoxDrumTimerBOffset, StatType.UInt32);

            return page;
        }

        private TabPage BuildItemsSocialTab()
        {
            var page = new TabPage("Items && Social");
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
            var setButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth };

            var field = new StatField(label, offset, type, secondaryOffset);
            setButton.Click += (_, _) => SetStat(field, textBox);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(textBox);
            parent.Controls.Add(setButton);
            _valueRows.Add((parent, textBox, setButton));
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
            _valueRows.Add((parent, box, null));
            y += RowHeight;
            return box;
        }

        private void AddDifficultyRow(Control parent, ref int y)
        {
            var nameLabel = new Label { Text = "Difficulty", Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var combo = new ComboBox { Left = ValueLeft, Top = y, Width = ValueWidth, DropDownStyle = ComboBoxStyle.DropDown };
            foreach (var level in DifficultyLevels)
            {
                combo.Items.Add(level.Name);
            }
            var setButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth };
            setButton.Click += (_, _) => SetDifficulty(combo);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(combo);
            parent.Controls.Add(setButton);
            _valueRows.Add((parent, combo, setButton));
            _difficultyCombo = combo;
            y += RowHeight;
        }

        // Total play time is stored as 60Hz ticks (per bbtracker's mgs4_research.md); shown alongside
        // the raw editable value since ticks alone aren't very readable.
        private static string FormatPlayTime(uint ticks)
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

        // manual: true for the button (updates the status bar and overwrites every field, even a
        // stale-looking one) vs. false for the background poll (silent, and never fights the user
        // mid-edit -- a focused text box or an open combo dropdown is left untouched this tick).
        private void RefreshStats(bool manual)
        {
            IntPtr? linkVarBuf = ResolveLinkVarBuf();
            if (linkVarBuf == null)
            {
                if (manual)
                {
                    SetStatus("Stats: no active run found (load a save first)");
                }
                return;
            }

            foreach (var entry in _statFields)
            {
                (StatField field, TextBox textBox) = entry.Value;
                if (!manual && textBox.Focused)
                {
                    continue;
                }
                textBox.Text = field.Type == StatType.UInt16
                    ? MemoryManager.ReadUInt16(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)field.Offset))?.ToString() ?? "?"
                    : MemoryManager.ReadUInt32(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)field.Offset))?.ToString() ?? "?";
            }

            if (manual || !_playTimeDisplayBox.Focused)
            {
                uint? ticks = MemoryManager.ReadUInt32(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)Constants.LiveStats.TotalPlayTimeOffset));
                _playTimeDisplayBox.Text = ticks.HasValue ? FormatPlayTime(ticks.Value) : "?";
            }

            if (manual || !_stageCodeBox.Focused)
            {
                byte[]? stageBytes = MemoryManager.ReadMemoryBytes(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)Constants.LiveStats.StageCodeOffset), 7);
                string rawCode = stageBytes == null ? "?" : Encoding.ASCII.GetString(stageBytes).TrimEnd('\0');
                _stageCodeBox.Text = Constants.StageNames.ByCode.TryGetValue(rawCode, out string? stageName)
                    ? $"{rawCode} — {stageName}"
                    : rawCode;
            }

            if (manual || (!_difficultyCombo.Focused && !_difficultyCombo.DroppedDown))
            {
                ushort? difficulty = MemoryManager.ReadUInt16(_processHandle, IntPtr.Add(linkVarBuf.Value, (int)Constants.LiveStats.DifficultyOffset));
                if (difficulty.HasValue)
                {
                    var match = DifficultyLevels.FirstOrDefault(l => l.Value == difficulty.Value);
                    _difficultyCombo.Text = match.Name ?? $"{difficulty.Value} (unknown)";
                }
                else
                {
                    _difficultyCombo.Text = "?";
                }
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

        #endregion

        #region Theming (dark palette + embedded wallpaper)

        private static readonly Color ColorBackground = Color.FromArgb(15, 17, 21);
        private static readonly Color ColorSurface = Color.FromArgb(27, 31, 37);
        private static readonly Color ColorSurfaceRaised = Color.FromArgb(38, 43, 50);
        private static readonly Color ColorAccent = Color.FromArgb(90, 169, 230);
        private static readonly Color ColorTextPrimary = Color.FromArgb(230, 232, 235);
        private static readonly Color ColorTextSecondary = Color.FromArgb(154, 162, 170);

        // Walks the whole control tree once (after every tab/group/row has been built) applying a
        // dark, flat palette. Kept generic by control type rather than styling each control at the
        // call site, so new rows/tabs added later inherit the theme automatically.
        private void ApplyDarkTheme(Control root)
        {
            BackColor = ColorBackground;

            foreach (Control control in root.Controls)
            {
                switch (control)
                {
                    case TabControl tabControl:
                        StyleTabControl(tabControl);
                        break;
                    case TabPage tabPage:
                        tabPage.BackColor = ColorBackground;
                        break;
                    case GroupBox groupBox:
                        // Solid "card" rather than transparent: WinForms' transparent-BackColor trick is
                        // reliable one level deep (a Label straight on an imaged TabPage), but chaining it
                        // through a GroupBox into its CheckBox children is prone to render glitches.
                        groupBox.BackColor = ColorSurface;
                        groupBox.ForeColor = ColorAccent;
                        groupBox.Font = new Font(groupBox.Font, FontStyle.Bold);
                        break;
                    case CheckBox checkBox:
                        checkBox.BackColor = Color.Transparent;
                        checkBox.ForeColor = ColorTextPrimary;
                        checkBox.FlatStyle = FlatStyle.Flat;
                        checkBox.FlatAppearance.CheckedBackColor = ColorAccent;
                        checkBox.FlatAppearance.BorderColor = ColorSurfaceRaised;
                        break;
                    case Button button:
                        button.FlatStyle = FlatStyle.Flat;
                        button.BackColor = ColorSurfaceRaised;
                        button.ForeColor = ColorTextPrimary;
                        button.FlatAppearance.BorderColor = ColorAccent;
                        button.FlatAppearance.BorderSize = 1;
                        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(ColorSurfaceRaised, 0.2f);
                        break;
                    case ComboBox comboBox:
                        comboBox.FlatStyle = FlatStyle.Flat;
                        comboBox.BackColor = ColorSurfaceRaised;
                        comboBox.ForeColor = ColorTextPrimary;
                        break;
                    case TextBox textBox:
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        textBox.BackColor = ColorSurfaceRaised;
                        textBox.ForeColor = ColorTextPrimary;
                        break;
                    case Panel panel:
                        panel.BackColor = Color.Transparent;
                        break;
                    case Label label:
                        label.BackColor = Color.Transparent;
                        label.ForeColor = ColorTextSecondary;
                        break;
                }

                if (control.HasChildren)
                {
                    ApplyDarkTheme(control);
                }
            }
        }

        private static void StyleTabControl(TabControl tabControl)
        {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.Padding = new Point(12, 5);
            tabControl.Font = new Font(tabControl.Font.FontFamily, 8.75f);
            tabControl.DrawItem -= TabControl_DrawItem;
            tabControl.DrawItem += TabControl_DrawItem;
        }

        private static void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl)
            {
                return;
            }

            TabPage page = tabControl.TabPages[e.Index];
            bool selected = e.Index == tabControl.SelectedIndex;

            using (var backBrush = new SolidBrush(selected ? ColorAccent : ColorSurface))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            Color textColor = selected ? Color.Black : ColorTextPrimary;
            TextRenderer.DrawText(e.Graphics, page.Text, tabControl.Font, e.Bounds, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // Composites a darkened, "cover"-scaled copy of the embedded wallpaper behind every tab page
        // (editable controls sit on their own solid-colored surface on top, so only the empty margins
        // and the transparent-background labels/checkboxes/group boxes actually show it through).
        private void ApplyWallpaperBackgrounds()
        {
            if (_wallpaperSource == null)
            {
                return;
            }

            foreach (TabPage page in CollectTabPages(this))
            {
                if (page.ClientSize.Width <= 0 || page.ClientSize.Height <= 0)
                {
                    continue;
                }

                Image? previous = page.BackgroundImage;
                page.BackgroundImage = ComposeBackground(_wallpaperSource, page.ClientSize);
                page.BackgroundImageLayout = ImageLayout.None;
                previous?.Dispose();
            }
        }

        private static IEnumerable<TabPage> CollectTabPages(Control root)
        {
            foreach (Control control in root.Controls)
            {
                if (control is TabPage page)
                {
                    yield return page;
                }
                foreach (TabPage nested in CollectTabPages(control))
                {
                    yield return nested;
                }
            }
        }

        private static Bitmap? LoadEmbeddedWallpaper()
        {
            Assembly assembly = typeof(TrainerForm).Assembly;
            string? resourceName = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith("wallpaper.jpg", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
            {
                return null;
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return null;
            }

            using Image decoded = Image.FromStream(stream);
            return new Bitmap(decoded);
        }

        private static Bitmap ComposeBackground(Image source, Size destSize)
        {
            var composed = new Bitmap(destSize.Width, destSize.Height);
            using Graphics g = Graphics.FromImage(composed);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float scale = Math.Max((float)destSize.Width / source.Width, (float)destSize.Height / source.Height);
            int scaledWidth = (int)Math.Ceiling(source.Width * scale);
            int scaledHeight = (int)Math.Ceiling(source.Height * scale);
            int offsetY = (destSize.Height - scaledHeight) / 2;

            g.Clear(ColorBackground);
            // Left-anchored (offsetX = 0): the subject sits on the left of the source image, so a
            // centered crop would cut into it -- anchoring left keeps it fully in frame instead.
            g.DrawImage(source, 0, offsetY, scaledWidth, scaledHeight);

            using var overlay = new SolidBrush(Color.FromArgb(180, ColorBackground));
            g.FillRectangle(overlay, 0, 0, destSize.Width, destSize.Height);

            return composed;
        }

        #endregion
    }
}
