using System.Drawing;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private readonly Dictionary<string, (int Slot, TextBox Box, Button ActionButton)> _itemAmountFields = new();
        private Button _maxAllConsumablesButton = null!;

        private TabPage BuildEquipmentUnlocksSubTab()
        {
            var (page, content) = BuildPointerDependentSubTab("Equipment");
            int equipY = 12;
            foreach (var (name, slot) in Constants.InventoryItems.UnlockSlots)
            {
                AddUnlockRow(content, ref equipY, name, slot);
            }
            return page;
        }

        private TabPage BuildConsumablesSubTab()
        {
            var (page, content) = BuildPointerDependentSubTab("Consumables");
            var header = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var amountNote = new Label
            {
                Text = "Real stock counts -- if not yet found in-game the button reads Unlock; once owned "
                     + "it becomes Set so you can type an exact amount.",
                Left = LabelLeft,
                Top = 8,
                AutoSize = true,
                MaximumSize = new Size(300, 0),
            };
            _maxAllConsumablesButton = new Button { Text = "Max All Consumables", Left = LabelLeft, Top = amountNote.Bottom + 8, Width = 300, Height = 28, Enabled = false };
            _maxAllConsumablesButton.Click += (_, _) => MaxAllItemAmounts();
            header.Controls.Add(amountNote);
            header.Controls.Add(_maxAllConsumablesButton);

            var rowsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            content.Controls.Add(rowsPanel);
            content.Controls.Add(header);

            int amountY = 8;
            foreach (var (name, slot) in Constants.InventoryItems.AmountSlots)
            {
                AddItemAmountRow(rowsPanel, ref amountY, name, slot);
            }
            return page;
        }

        private void AddItemAmountRow(Control parent, ref int y, string label, int slot)
        {
            var nameLabel = new Label { Text = label, Left = LabelLeft, Top = y + 3, Width = LabelWidth, AutoEllipsis = true };
            var textBox = new TextBox { Left = ValueLeft, Top = y, Width = ValueWidth, Enabled = false };
            var actionButton = new Button { Text = "Set", Left = ButtonLeft, Top = y - 1, Width = ButtonWidth, Height = 28, Enabled = false };
            actionButton.Click += (_, _) => HandleItemAmountAction(label, slot, textBox, actionButton);

            parent.Controls.Add(nameLabel);
            parent.Controls.Add(textBox);
            parent.Controls.Add(actionButton);
            _valueRows.Add((parent, nameLabel, textBox, actionButton));
            _itemAmountFields[label] = (slot, textBox, actionButton);
            y += RowHeight;
        }

        private void RefreshItems()
        {
            IntPtr? pInv = ResolveInventoryPointer();
            foreach (var (slot, box, button) in _itemAmountFields.Values)
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
                ushort? current = MemoryManager.ReadUInt16(_processHandle, InventorySlotAddress(pInv.Value, slot));
                if (current == null)
                {
                    box.Text = "?";
                    continue;
                }
                if (current == 0xFFFF)
                {
                    box.Text = string.Empty;
                    box.ReadOnly = true;
                    button.Text = "Unlock";
                }
                else
                {
                    box.ReadOnly = false;
                    box.Text = current.Value.ToString();
                    button.Text = "Set";
                }
            }
        }

        private void HandleItemAmountAction(string label, int slot, TextBox textBox, Button actionButton)
        {
            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                SetStatus("Items: enable Inventory Pointer first");
                return;
            }
            IntPtr addr = InventorySlotAddress(pInv.Value, slot);
            ushort? current = MemoryManager.ReadUInt16(_processHandle, addr);
            if (current == 0xFFFF)
            {
                bool unlockOk = MemoryManager.WriteUInt16(_processHandle, addr, 1);
                SetStatus(unlockOk ? $"{label}: unlocked" : $"{label}: write failed");
                return;
            }
            if (!ushort.TryParse(textBox.Text, out ushort value) || value == 0xFFFF)
            {
                SetStatus($"{label}: enter a whole number 0-65534");
                return;
            }
            bool ok = MemoryManager.WriteUInt16(_processHandle, addr, value);
            SetStatus(ok ? $"{label}: set to {value}" : $"{label}: write failed");
        }

        private void MaxAllItemAmounts()
        {
            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                SetStatus("Max All Consumables: enable Inventory Pointer first");
                return;
            }
            int count = 0;
            foreach (var (slot, _, _) in _itemAmountFields.Values)
            {
                IntPtr baseAddr = InventorySlotAddress(pInv.Value, slot);
                ushort? current = MemoryManager.ReadUInt16(_processHandle, baseAddr);
                if (current == 0xFFFF)
                {
                    continue; // not owned yet -- Max shouldn't silently "unlock" it with a max value
                }
                ushort? max = MemoryManager.ReadUInt16(_processHandle, IntPtr.Add(baseAddr, 2));
                if (max != null && MemoryManager.WriteUInt16(_processHandle, baseAddr, max.Value))
                {
                    count++;
                }
            }
            SetStatus($"Max All Consumables: topped up {count}/{_itemAmountFields.Count} items");
        }
    }
}
