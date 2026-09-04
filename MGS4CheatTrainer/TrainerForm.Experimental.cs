using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private readonly Dictionary<string, (long Offset, TextBox Box)> _experimentalFields = new();

        private sealed record ExperimentalEnumField(string Label, long Offset, (string Name, byte Value)[] Names);

        private readonly Dictionary<string, (ExperimentalEnumField Field, ComboBox Combo)> _experimentalEnumFields = new();

        private TabPage BuildExperimentalTab()
        {
            var page = new TabPage("Experimental") { AutoScroll = true };

            var appearanceGroup = new GroupBox { Text = "Appearance", Left = 12, Top = 12, Width = 330 };
            var appearanceNote = new Label
            {
                Text = "The byte writes instantly, but the 3D model only re-reads it on a reload -- "
                     + "change area (or reload a save) after clicking Set to see it applied.",
                Left = 12,
                Top = 20,
                Width = 300,
                Height = 42,
                AutoEllipsis = false,
            };
            appearanceGroup.Controls.Add(appearanceNote);
            int appearanceY = 66;
            AddAppearanceEnumRow(appearanceGroup, ref appearanceY, "Outfit", Constants.Appearance.OutfitOffset, Constants.Appearance.OutfitNames);
            appearanceGroup.Height = appearanceY + 8;
            _stretchOnly.Add((appearanceGroup, appearanceNote));

            page.Controls.Add(appearanceGroup);
            _stretchOnly.Add((page, appearanceGroup));

            var enemyGroup = new GroupBox { Text = "Enemy Control", Left = 12, Top = appearanceGroup.Top + appearanceGroup.Height + 12, Width = 330 };
            var enemyEnable = MakeAobPatchToggle("Enemy Control", ToggleEnemyControl, () => _enemyControlTarget);
            enemyEnable.Left = 12;
            enemyEnable.Top = 22;
            enemyEnable.Width = 300;

            var actionLabel = new Label { Text = "Mode:", Left = 12, Top = 52, Width = 50 };
            _enemyActionCombo = new ComboBox { Left = 66, Top = 49, Width = 252, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            _enemyActionCombo.Items.AddRange(new object[] { "Off", "Kill enemies (persistent)", "Put enemies to sleep (persistent)" });
            _enemyActionCombo.SelectedIndex = 0;
            _enemyActionCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_enemyControlModeAddr is not { } modeAddr)
                {
                    return;
                }
                bool ok = MemoryManager.WriteRawBytes(_processHandle, modeAddr, new[] { (byte)_enemyActionCombo.SelectedIndex });
                SetStatus(ok ? $"Enemy Control: mode set to \"{_enemyActionCombo.SelectedItem}\"" : "Enemy Control: mode write failed");
            };

            enemyGroup.Controls.Add(enemyEnable);
            enemyGroup.Controls.Add(actionLabel);
            enemyGroup.Controls.Add(_enemyActionCombo);
            enemyGroup.Height = 92;
            _stretchOnly.Add((enemyGroup, enemyEnable));
            _stretchOnly.Add((enemyGroup, _enemyActionCombo));

            page.Controls.Add(enemyGroup);
            _stretchOnly.Add((page, enemyGroup));
            return page;
        }

        private void AddAppearanceRow(Control parent, ref int y, string label, long offset)
        {
            var nameLabel = new Label { Text = label, Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var textBox = new TextBox { Left = ValueLeft, Top = y, Width = ValueWidth };
            var setButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth, Height = 28 };
            setButton.Click += (_, _) => SetAppearanceByte(label, offset, textBox);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(textBox);
            parent.Controls.Add(setButton);
            _valueRows.Add((parent, nameLabel, textBox, setButton));
            _experimentalFields[label] = (offset, textBox);
            y += RowHeight;
        }

        private void AddAppearanceEnumRow(Control parent, ref int y, string label, long offset, (string Name, byte Value)[] names)
        {
            var nameLabel = new Label { Text = label, Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var combo = new ComboBox { Left = ValueLeft, Top = y, Width = ValueWidth, DropDownStyle = ComboBoxStyle.DropDown };
            foreach (var n in names)
            {
                combo.Items.Add(n.Name);
            }
            var setButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth, Height = 28 };
            var field = new ExperimentalEnumField(label, offset, names);
            setButton.Click += (_, _) => SetAppearanceEnum(field, combo);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(combo);
            parent.Controls.Add(setButton);
            _valueRows.Add((parent, nameLabel, combo, setButton));
            _experimentalEnumFields[label] = (field, combo);
            y += RowHeight;
        }

        private void RefreshExperimental()
        {
            foreach (var (offset, box) in _experimentalFields.Values)
            {
                if (box.Focused)
                {
                    continue;
                }
                byte? value = MemoryManager.ReadByte(_processHandle, IntPtr.Add(_baseAddress, (int)offset));
                box.Text = value?.ToString() ?? "?";
            }

            foreach (var (field, combo) in _experimentalEnumFields.Values)
            {
                if (combo.Focused || combo.DroppedDown)
                {
                    continue;
                }
                byte? value = MemoryManager.ReadByte(_processHandle, IntPtr.Add(_baseAddress, (int)field.Offset));
                if (value == null)
                {
                    combo.Text = "?";
                    continue;
                }
                var match = field.Names.FirstOrDefault(n => n.Value == value);
                combo.Text = match.Name ?? $"{value} (unconfirmed)";
            }
        }

        private void SetAppearanceByte(string label, long offset, TextBox textBox)
        {
            if (!byte.TryParse(textBox.Text, out byte value))
            {
                SetStatus($"{label}: enter a whole number 0-255");
                return;
            }
            bool ok = MemoryManager.WriteByte(_processHandle, IntPtr.Add(_baseAddress, (int)offset), value);
            SetStatus(ok ? $"{label}: set to {value}" : $"{label}: write failed");
        }

        private void SetAppearanceEnum(ExperimentalEnumField field, ComboBox combo)
        {
            string text = combo.Text.Trim();
            var matched = field.Names.FirstOrDefault(n => string.Equals(n.Name, text, StringComparison.OrdinalIgnoreCase));
            byte value;
            if (matched.Name != null)
            {
                value = matched.Value;
            }
            else if (!byte.TryParse(text, out value))
            {
                SetStatus($"{field.Label}: pick a value or enter a raw number 0-255");
                return;
            }
            bool ok = MemoryManager.WriteByte(_processHandle, IntPtr.Add(_baseAddress, (int)field.Offset), value);
            SetStatus(ok ? $"{field.Label}: set to {value}{(matched.Name != null ? $" ({matched.Name})" : "")}" : $"{field.Label}: write failed");
        }
    }
}
