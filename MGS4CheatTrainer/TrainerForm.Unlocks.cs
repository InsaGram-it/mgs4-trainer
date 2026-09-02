using System.Drawing;
using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private IntPtr? _inventoryPointerTarget;
        private IntPtr? _inventoryPointerDataAddr;
        private readonly List<(int Slot, CheckBox Box)> _unlockRows = new();
        private Button _unlockAllButton = null!;
        private readonly List<Label> _pointerRequiredWarnings = new();

        private TabPage BuildInventoryTab()
        {
            var page = new TabPage("Inventory");

            var header = new Panel { Dock = DockStyle.Top, Height = 160 };
            var unlockEnable = MakeAobPatchToggle("Inventory Pointer", ToggleInventoryPointer, () => _inventoryPointerTarget);
            unlockEnable.Left = 12;
            unlockEnable.Top = 10;
            unlockEnable.Width = 300;
            var unlockNote = new Label
            {
                Text = "Enable first -- required by every sub-tab here.",
                Left = 12,
                Top = 40,
                Width = 300,
                Height = 36,
                AutoEllipsis = false,
            };
            _unlockAllButton = new Button { Text = "Unlock All", Left = 12, Top = 100, Width = 300, Height = 28, Enabled = false };
            _unlockAllButton.Click += (_, _) => UnlockAllInventorySlots();
            header.Controls.Add(unlockEnable);
            header.Controls.Add(unlockNote);
            header.Controls.Add(_unlockAllButton);
            _stretchOnly.Add((header, unlockEnable));
            _stretchOnly.Add((header, unlockNote));
            _stretchOnly.Add((header, _unlockAllButton));

            var subTabs = new TabControl { Dock = DockStyle.Fill };
            subTabs.TabPages.Add(BuildOutfitUnlocksSubTab());
            subTabs.TabPages.Add(BuildFaceCamoUnlocksSubTab());
            subTabs.TabPages.Add(BuildEquipmentUnlocksSubTab());
            subTabs.TabPages.Add(BuildConsumablesSubTab());
            subTabs.SelectedIndexChanged += (_, _) => OnVisibleTabChanged();

            page.Controls.Add(subTabs);
            page.Controls.Add(header);
            return page;
        }

        private TabPage BuildOutfitUnlocksSubTab()
        {
            var (page, content) = BuildPointerDependentSubTab("Outfits");
            int y = 12;
            foreach (var (name, slot) in Constants.InventoryUnlocks.OutfitSlots)
            {
                AddUnlockRow(content, ref y, name, slot);
            }
            return page;
        }

        private TabPage BuildFaceCamoUnlocksSubTab()
        {
            var (page, content) = BuildPointerDependentSubTab("FaceCamo");
            int y = 12;
            foreach (var (name, slot) in Constants.InventoryUnlocks.FaceCamoSlots)
            {
                AddUnlockRow(content, ref y, name, slot);
            }
            return page;
        }

        private (TabPage Page, Panel Content) BuildPointerDependentSubTab(string title)
        {
            var page = new TabPage(title);
            var content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            page.Controls.Add(content);

            var warning = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Enable Inventory Pointer above to use this tab",
                Font = new Font(Font, FontStyle.Bold),
            };
            page.Controls.Add(warning);
            _pointerRequiredWarnings.Add(warning);

            return (page, content);
        }

        private void AddUnlockRow(Control parent, ref int y, string label, int slot)
        {
            var checkbox = new LegibleCheckBox { Text = label, Left = 12, Top = y, Width = 300, Enabled = false };
            checkbox.CheckedChanged += (_, _) =>
            {
                if (_suppressEvents)
                {
                    return;
                }
                SetInventorySlotOwned(label, slot, checkbox.Checked);
            };

            parent.Controls.Add(checkbox);
            _stretchOnly.Add((parent, checkbox));
            _unlockRows.Add((slot, checkbox));
            y += 25;
        }

        private IntPtr? ResolveInventoryPointer()
        {
            if (_inventoryPointerDataAddr is not { } dataAddr)
            {
                return null;
            }
            IntPtr pInv = MemoryManager.ReadIntPtr(_processHandle, dataAddr);
            return pInv == IntPtr.Zero ? null : pInv;
        }

        private static IntPtr InventorySlotAddress(IntPtr pInv, int slot) =>
            IntPtr.Add(pInv, (int)(Constants.InventoryUnlocks.ItemBase + Constants.InventoryUnlocks.ItemStride * slot));

        private void RefreshInventoryUnlocks()
        {
            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                return;
            }
            foreach (var (slot, checkbox) in _unlockRows)
            {
                ushort? value = MemoryManager.ReadUInt16(_processHandle, InventorySlotAddress(pInv.Value, slot));
                if (value == null)
                {
                    continue;
                }
                // 0xFFFF is the only real "not owned" sentinel -- some items (Cardboard Box A) keep a
                // genuine usage/pickup count here even while owned, not a fixed 1, so anything else counts.
                SetCheckedSilently(checkbox, value != 0xFFFF);
            }
        }

        private void SetInventorySlotOwned(string label, int slot, bool owned)
        {
            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                SetStatus("Outfit/FaceCamo Unlocks: enable Inventory Pointer first");
                SetCheckedSilently(_unlockRows.First(r => r.Slot == slot).Box, false);
                return;
            }
            ushort value = owned ? (ushort)1 : (ushort)0xFFFF;
            bool ok = MemoryManager.WriteUInt16(_processHandle, InventorySlotAddress(pInv.Value, slot), value);
            SetStatus(ok ? $"{label}: {(owned ? "unlocked" : "locked")}" : $"{label}: write failed");
        }

        // Every control that writes through pInv is useless without it -- disabled by default at
        // creation (see AddUnlockRow / AddItemAmountRow) and flipped on/off here alongside the pointer
        // itself, rather than leaving them clickable and just failing with a status message each time.
        private void SetPointerDependentControlsEnabled(bool enabled)
        {
            _unlockAllButton.Enabled = enabled;
            _maxAllConsumablesButton.Enabled = enabled;
            foreach (var (_, checkbox) in _unlockRows)
            {
                checkbox.Enabled = enabled;
                if (!enabled)
                {
                    // Without the pointer there's no way to know the real state, so don't leave a stale
                    // checked/unchecked value showing -- reset to unchecked, matching how the
                    // Consumables fields blank out to "?" in the same situation.
                    SetCheckedSilently(checkbox, false);
                }
            }
            foreach (var (_, box, button) in _itemAmountFields.Values)
            {
                box.Enabled = enabled;
                button.Enabled = enabled;
            }
            foreach (var warning in _pointerRequiredWarnings)
            {
                warning.Visible = !enabled;
            }
        }

        private void DisableInventoryPointerOnClose()
        {
            if (_inventoryPointerTarget is { } target)
            {
                MemoryManager.WriteRawBytes(_processHandle, target, Constants.CodePatches.InventoryPointer.PatchBytes);
            }
        }

        private void UnlockAllInventorySlots()
        {
            DialogResult confirm = MessageBox.Show(
                $"This will unlock all {_unlockRows.Count} outfit/facecamo/equipment slots at once. Continue?",
                "Unlock All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            IntPtr? pInv = ResolveInventoryPointer();
            if (pInv == null)
            {
                SetStatus("Unlock All: enable Inventory Pointer first");
                return;
            }
            int count = 0;
            foreach (var (slot, checkbox) in _unlockRows)
            {
                if (MemoryManager.WriteUInt16(_processHandle, InventorySlotAddress(pInv.Value, slot), 1))
                {
                    SetCheckedSilently(checkbox, true);
                    count++;
                }
            }
            SetStatus($"Unlock All: unlocked {count}/{_unlockRows.Count} slots");
        }

        // Captures pInv the same way Actor Collector/Enemy Control capture their own pointers: the cave
        // stores rax to a trailing data cell (via "mov [addr],rax", the moffs64 form -- no scratch
        // register needed) then re-runs the stolen instruction and jumps back. Needed before any
        // [pInv]-relative address resolves.
        private async Task ToggleInventoryPointer()
        {
            var checkbox = _pendingToggles["Inventory Pointer"];
            if (checkbox.Checked)
            {
                byte[] scanPattern = Constants.CodePatches.InventoryPointer.ScanPattern;
                int patchLength = Constants.CodePatches.InventoryPointer.PatchLength;
                int dataOffset = 0;
                var result = await Task.Run(() => MemoryManager.InjectTrampolineScanned(
                    _processHandle, _baseAddress, _moduleSize,
                    scanPattern, new string('x', scanPattern.Length), patchLength,
                    (targetAddress, cave) =>
                    {
                        var body = new List<byte>
                        {
                            0x48,
                            0xA3 // mov [imm64], rax (moffs64 form -- stores rax directly, no scratch register)
                        };
                        int addrImmOffset = body.Count;
                        body.AddRange(new byte[8]); // placeholder for the data cell's address
                        body.AddRange(Constants.CodePatches.InventoryPointer.PatchBytes); // original: cmp word ptr [rax+0x14],0
                        IntPtr returnAddress = IntPtr.Add(targetAddress, patchLength);
                        body.AddRange(MemoryManager.JumpRel32(cave, body.Count, returnAddress));

                        dataOffset = body.Count;
                        byte[] addrBytes = BitConverter.GetBytes(cave.ToInt64() + dataOffset);
                        for (int i = 0; i < 8; i++)
                        {
                            body[addrImmOffset + i] = addrBytes[i];
                        }
                        body.AddRange(new byte[8]); // pInv data cell, starts at 0

                        return [.. body];
                    }));

                if (!IsUsable)
                {
                    return;
                }
                if (result == null)
                {
                    SetStatus($"Inventory Pointer: {PatternNotFoundHint}");
                    return;
                }
                _inventoryPointerTarget = result.Value.Target;
                _inventoryPointerDataAddr = IntPtr.Add(result.Value.Cave, dataOffset);
                SetPointerDependentControlsEnabled(true);
                SetStatus("Inventory Pointer: enabled");
            }
            else
            {
                _inventoryPointerDataAddr = null;
                SetPointerDependentControlsEnabled(false);
                if (_inventoryPointerTarget is { } target)
                {
                    bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target, Constants.CodePatches.InventoryPointer.PatchBytes));
                    if (!IsUsable)
                    {
                        return;
                    }
                    SetStatus(ok ? "Inventory Pointer: disabled" : "Inventory Pointer: write failed");
                }
            }
        }
    }
}
