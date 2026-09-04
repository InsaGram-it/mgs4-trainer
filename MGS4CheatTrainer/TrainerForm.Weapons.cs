using System.Drawing;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private readonly Dictionary<string, (int Slot, TextBox Box, Button ActionButton)> _weaponAmountFields = new();
        private readonly List<(int Id, CheckBox Box)> _weaponOwnedRows = new();
        private Button _maxAllWeaponAmmoButton = null!;

        private TabPage BuildWeaponsTab()
        {
            var page = new TabPage("Weapons");
            var header = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var unlockAllButton = new Button { Text = "Unlock All", Left = LabelLeft, Top = 8, Width = 300, Height = 28 };
            unlockAllButton.Click += (_, _) => UnlockAllWeapons();
            header.Controls.Add(unlockAllButton);

            var rowsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            page.Controls.Add(rowsPanel);
            page.Controls.Add(header);

            int y = 8;
            foreach (var (name, id) in Constants.WeaponUnlocks.Names)
            {
                AddWeaponOwnedRow(rowsPanel, ref y, name, id);
            }
            return page;
        }

        private void AddWeaponOwnedRow(Control parent, ref int y, string label, int id)
        {
            var checkbox = new LegibleCheckBox { Text = label, Left = 12, Top = y, Width = 300 };
            checkbox.CheckedChanged += (_, _) =>
            {
                if (_suppressEvents)
                {
                    return;
                }
                SetWeaponOwned(label, id, checkbox.Checked);
            };
            parent.Controls.Add(checkbox);
            _stretchOnly.Add((parent, checkbox));
            _weaponOwnedRows.Add((id, checkbox));
            y += 25;
        }

        private IntPtr WeaponStateAddress(int id) =>
            IntPtr.Add(_baseAddress, (int)(Constants.WeaponUnlocks.TableBaseOffset + Constants.WeaponUnlocks.StateBase + Constants.WeaponUnlocks.StateStride * id));

        private void RefreshWeaponOwnership()
        {
            foreach (var (id, checkbox) in _weaponOwnedRows)
            {
                ushort? value = MemoryManager.ReadUInt16(_processHandle, WeaponStateAddress(id));
                if (value == null)
                {
                    continue;
                }
                SetCheckedSilently(checkbox, value == Constants.WeaponUnlocks.Unlocked);
            }
        }

        private void SetWeaponOwned(string label, int id, bool owned)
        {
            ushort value = owned ? Constants.WeaponUnlocks.Unlocked : Constants.WeaponUnlocks.Locked;
            bool ok = MemoryManager.WriteUInt16(_processHandle, WeaponStateAddress(id), value);
            SetStatus(ok ? $"{label}: {(owned ? "unlocked" : "locked")}" : $"{label}: write failed");
        }

        private void UnlockAllWeapons()
        {
            DialogResult confirm = MessageBox.Show(
                $"This will unlock all {_weaponOwnedRows.Count} weapons at once. Continue?",
                "Unlock All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            int count = 0;
            foreach (var (id, checkbox) in _weaponOwnedRows)
            {
                if (MemoryManager.WriteUInt16(_processHandle, WeaponStateAddress(id), Constants.WeaponUnlocks.Unlocked))
                {
                    SetCheckedSilently(checkbox, true);
                    count++;
                }
            }
            SetStatus($"Unlock All: unlocked {count}/{_weaponOwnedRows.Count} weapons");
        }

        private TabPage BuildWeaponAmmoSubTab()
        {
            var (page, content) = BuildPointerDependentSubTab("Ammo");
            var header = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            _maxAllWeaponAmmoButton = new Button { Text = "Max All Ammo", Left = LabelLeft, Top = 8, Width = 300, Height = 28, Enabled = false };
            _maxAllWeaponAmmoButton.Click += (_, _) => MaxAllWeaponAmmo();
            header.Controls.Add(_maxAllWeaponAmmoButton);

            var rowsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            content.Controls.Add(rowsPanel);
            content.Controls.Add(header);

            int y = 8;
            foreach (var (label, slot) in Constants.Weapons.AmmoSlots)
            {
                AddWeaponAmountRow(rowsPanel, ref y, label, slot);
            }
            return page;
        }

        private void AddWeaponAmountRow(Control parent, ref int y, string label, int slot)
        {
            var nameLabel = new Label { Text = label, Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var textBox = new TextBox { Left = ValueLeft, Top = y, Width = ValueWidth, Enabled = false };
            var actionButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth, Height = 28, Enabled = false };
            actionButton.Click += (_, _) => HandleWeaponAmountAction(label, slot, textBox, actionButton);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(textBox);
            parent.Controls.Add(actionButton);
            _valueRows.Add((parent, nameLabel, textBox, actionButton));
            _weaponAmountFields[label] = (slot, textBox, actionButton);
            y += RowHeight;
        }

        private static IntPtr WeaponAmmoAddress(IntPtr pInv, int slot) =>
            IntPtr.Add(pInv, (int)(Constants.Weapons.AmmoBaseOffset + Constants.Weapons.AmmoStride * slot));

        private void RefreshWeapons()
        {
            RefreshWeaponOwnership();
            IntPtr? pInv = ResolveInventoryPointer();
            foreach (var (slot, box, button) in _weaponAmountFields.Values)
            {
                if (box.Focused)
                {
                    continue;
                }
                if (pInv == null)
                {
                    box.Text = "?";
                    continue;
                }
                ushort? current = MemoryManager.ReadUInt16(_processHandle, WeaponAmmoAddress(pInv.Value, slot));
                if (current == null)
                {
                    box.Text = "?";
                    continue;
                }
                if (current == Constants.Weapons.LockedSentinel)
                {
                    box.Text = string.Empty;
                    box.ReadOnly = true;
                }
                else
                {
                    box.ReadOnly = false;
                    box.Text = current.Value.ToString();
                }
            }
        }

        private void HandleWeaponAmountAction(string label, int slot, TextBox textBox, Button actionButton)
        {
            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                SetStatus("Ammo: enable Inventory Pointer first");
                return;
            }
            IntPtr addr = WeaponAmmoAddress(pInv.Value, slot);
            if (!ushort.TryParse(textBox.Text, out ushort value) || value == Constants.Weapons.LockedSentinel)
            {
                SetStatus($"{label}: enter a whole number 0-65534");
                return;
            }
            bool ok = MemoryManager.WriteUInt16(_processHandle, addr, value);
            SetStatus(ok ? $"{label}: set to {value}" : $"{label}: write failed");
        }

        private void MaxAllWeaponAmmo()
        {
            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                SetStatus("Max All Ammo: enable Inventory Pointer first");
                return;
            }
            int count = 0;
            foreach (var (slot, _, _) in _weaponAmountFields.Values)
            {
                IntPtr baseAddr = WeaponAmmoAddress(pInv.Value, slot);
                ushort? current = MemoryManager.ReadUInt16(_processHandle, baseAddr);
                if (current == Constants.Weapons.LockedSentinel)
                {
                    continue; // not owned yet -- Max shouldn't silently "unlock" it
                }
                ushort? max = MemoryManager.ReadUInt16(_processHandle, IntPtr.Add(baseAddr, 2));
                if (max != null && MemoryManager.WriteUInt16(_processHandle, baseAddr, max.Value))
                {
                    count++;
                }
            }
            SetStatus($"Max All Ammo: topped up {count}/{_weaponAmountFields.Count} slots");
        }
    }
}
