using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm : Form
    {
        private const int LabelLeft = 12;
        private const int LabelWidth = 170;
        private const int ValueLeft = 186;
        private const int ValueWidth = 110;
        private const int ButtonLeft = 302;
        private const int ButtonWidth = 80;
        private const int RowHeight = 32;

        private const int StatsPollIntervalMs = 500;

        private readonly Process _gameProcess;
        private readonly IntPtr _processHandle;
        private readonly IntPtr _baseAddress;
        private readonly long _moduleSize;
        private readonly TextBox _status;
        private readonly TabControl _tabs;
        private readonly System.Windows.Forms.Timer _statsTimer;

       private const string UiFontFamily = "Verdana";

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
        private const int RowOuterMargin = 14;
        private const int RowGap = 6;
        private const int MinFieldWidth = 40;

       private const double LabelWidthFraction = 0.34;
        private const int MinLabelWidth = 170;
        private const int MaxLabelWidth = 260;

        private readonly List<(Control Parent, Control? Label, Control Value, Control? Button)> _valueRows = new();
        private readonly List<(Control Parent, Control Control)> _stretchOnly = new();

        private void ReflowAll()
        {
            foreach (var (parent, label, value, button) in _valueRows)
            {

                if (parent.ClientSize.Width <= 0)
                {
                    continue;
                }
                int rightEdge = parent.ClientSize.Width - RowOuterMargin;
                if (label != null)
                {
                    int labelWidth = Math.Clamp((int)(parent.ClientSize.Width * LabelWidthFraction), MinLabelWidth, MaxLabelWidth);
                    label.Width = labelWidth;
                    value.Left = LabelLeft + labelWidth + RowGap;
                }

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
                if (parent.ClientSize.Width <= 0)
                {
                    continue;
                }
                control.Width = Math.Max(MinFieldWidth, parent.ClientSize.Width - control.Left - RowOuterMargin);
            }
        }

        public TrainerForm(Process gameProcess, IntPtr processHandle, IntPtr baseAddress, long moduleSize)
        {
            _gameProcess = gameProcess;
            _processHandle = processHandle;
            _baseAddress = baseAddress;
            _moduleSize = moduleSize;

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);

            // Version comes from the csproj's <Version> -- bump that alongside creating a new git tag.
            Version? version = Assembly.GetExecutingAssembly().GetName().Version;
            Text = version == null ? "MGS4 Trainer" : $"MGS4 Trainer v{version.ToString(3)}";
            Width = 960;
            Height = 460;
            MinimumSize = new Size(460, 960);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            Font = new Font(UiFontFamily, 9.5f);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 140 };

            _status = new TextBox
            {
                Left = 12,
                Top = 4,
                Height = 108,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "Ready.",
                ForeColor = Color.DimGray,
            };

            var trademark = new LinkLabel
            {
                Text = "Made by Insa",
                AutoSize = true,
                LinkColor = Color.Gray,
                ActiveLinkColor = ColorAccent,
                VisitedLinkColor = Color.Gray,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Font = new Font(Font.FontFamily, 7.5f, FontStyle.Italic),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            };
            trademark.Links.Add(0, trademark.Text.Length, "https://ko-fi.com/insagram");
            trademark.LinkClicked += (_, e) =>
            {
                if (e.Link?.LinkData is string url)
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            };

            bottomPanel.Controls.Add(_status);
            bottomPanel.Controls.Add(trademark);
            trademark.BringToFront();
            _stretchOnly.Add((bottomPanel, _status));
            Load += (_, _) =>
            {
                trademark.Left = bottomPanel.ClientSize.Width - trademark.Width - 12;
                trademark.Top = bottomPanel.ClientSize.Height - trademark.Height - 4;
            };

            _tabs = new TabControl { Dock = DockStyle.Fill };
            _tabs.TabPages.Add(BuildStatsTab());
            _tabs.TabPages.Add(BuildPlayerTab());
            _tabs.TabPages.Add(BuildInventoryTab());
            _tabs.TabPages.Add(BuildWeaponsTab());
            _tabs.TabPages.Add(BuildExperimentalTab());
            // A tab page only gets its real size once it's actually been selected/shown -- reflow
            // again each time the visible tab changes, to catch whichever page was still unsized
            // until now.
            _tabs.SelectedIndexChanged += (_, _) => OnVisibleTabChanged();

            Controls.Add(_tabs);
            Controls.Add(bottomPanel);

            ApplyDarkTheme(this);
            bottomPanel.BackColor = ColorSurface;
            foreach (var warning in _pointerRequiredWarnings)
            {
                warning.BackColor = Color.FromArgb(120, 90, 20);
                warning.ForeColor = Color.White;
            }

            Resize += (_, _) => ReflowAll();
            Activated += (_, _) => ReflowAll();
            Load += (_, _) => OnVisibleTabChanged();

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
            FormClosed += (_, _) => _statsTimer.Stop();
            // Safe to always do on close, checked or not: this only removes the passive capture hook
            // (restores 5 original bytes) -- it doesn't relock anything already unlocked, since those
            // are independent writes already committed to the game's own inventory struct.
            FormClosing += (_, _) => DisableInventoryPointerOnClose();
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

        // A pattern scan looks for the ORIGINAL, unpatched bytes. If this same cheat was enabled earlier
        // in the current game session (even from a previous run of the trainer, or one that crashed before
        // it could disable cleanly) and the game itself was never restarted, those bytes are still sitting
        // there patched -- so the scan comes up empty even though nothing is actually wrong. Restarting
        // MGS4 puts the original bytes back and lets the scan find them again.
        private const string PatternNotFoundHint =
            "pattern not found / cave alloc failed -- if MGS4 wasn't restarted since this was last enabled, its patched bytes are still there and no longer match; restart the game and try again";

        private void SetStatus(string message)
        {
            _status.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            _status.SelectionStart = _status.TextLength;
            _status.ScrollToCaret();
        }

        // A background AOB scan (Task.Run) can still be in flight when the window closes; its
        // continuation resumes on this form's SynchronizationContext regardless, so every await
        // that's followed by touching a control checks this first instead of risking an
        // ObjectDisposedException/InvalidOperationException on a torn-down form.
        private bool IsUsable => !IsDisposed && !Disposing;

        private void OnVisibleTabChanged()
        {
            ReflowAll();
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
            var checkbox = new LegibleCheckBox { Text = label };
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

        // Custom AOB-based patches (byte-replace or code injection) that need cheat-specific logic on enable.
        // toggleAction does the scan/patch work; it can be slow, so lock the checkbox and show a status
        // while it runs instead of letting the box flip instantly with no feedback.
        private CheckBox MakeAobPatchToggle(string label, Func<Task> toggleAction, Func<IntPtr?> getTarget)
        {
            var checkbox = new LegibleCheckBox { Text = label };
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

        private IntPtr? FindAob(byte[] pattern) => FindAob(pattern, new string('x', pattern.Length));

        private IntPtr? FindAob(byte[] pattern, string mask)
        {
            var matches = MemoryManager.ScanForAllInstances(_processHandle, _baseAddress, _moduleSize, pattern, mask);
            return matches.Count > 0 ? matches[0] : null;
        }

        private readonly Dictionary<string, CheckBox> _pendingToggles = new();
    }
}
