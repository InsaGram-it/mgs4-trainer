using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        // Resolved live addresses for the AOB-based patches, needed to restore on disable.
        private IntPtr? _suppressorTarget;
        private IntPtr? _noAlertsTarget;
        private IntPtr? _camoTarget;
        private IntPtr? _batteryTarget;

        private TabPage BuildPlayerTab()
        {
            var page = new TabPage("Player");

            var survival = BuildGroup("Survival", 12, out int survivalBottom,
                MakeStaticToggle("Infinite Life", Constants.CodePatches.InfiniteLife.ModuleOffset, Constants.CodePatches.InfiniteLife.OriginalBytes),
                MakeStaticToggle("Infinite Stamina", Constants.CodePatches.InfiniteStamina.ModuleOffset, Constants.CodePatches.InfiniteStamina.OriginalBytes),
                MakeStaticToggle("Never Stress", Constants.CodePatches.NeverStress.ModuleOffset, Constants.CodePatches.NeverStress.OriginalBytes));

            var weapons = BuildGroup("Weapons && Items", survivalBottom + 10, out int weaponsBottom,
                MakeStaticToggle("Infinite Ammo", Constants.CodePatches.InfiniteAmmo.ModuleOffset, Constants.CodePatches.InfiniteAmmo.OriginalBytes),
                MakeStaticToggle("Never Reload", Constants.CodePatches.NeverReload.ModuleOffset, Constants.CodePatches.NeverReload.OriginalBytes),
                MakeStaticToggle("Infinite Throwables", Constants.CodePatches.InfiniteThrowables.ModuleOffset, Constants.CodePatches.InfiniteThrowables.OriginalBytes),
                MakeStaticToggle("Infinite Items", Constants.CodePatches.InfiniteItems.ModuleOffset, Constants.CodePatches.InfiniteItems.OriginalBytes),
                MakeAobPatchToggle("Infinite Suppressor", ToggleInfiniteSuppressor, () => _suppressorTarget));

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

        // Two decrements per silenced shot, both patched off the same scan match: the first (durability)
        // sits right at the match, the second (ammo stock) 8 bytes further in, with an untouched index
        // computation between them -- see the comment on Constants.CodePatches.InfiniteSuppressor.
        private async Task ToggleInfiniteSuppressor()
        {
            var checkbox = _pendingToggles["Infinite Suppressor"];
            if (checkbox.Checked)
            {
                IntPtr? target = await Task.Run(() => FindAob(Constants.CodePatches.InfiniteSuppressor.ScanPattern));
                if (!IsUsable)
                {
                    return;
                }
                if (target == null)
                {
                    SetStatus($"Infinite Suppressor: {PatternNotFoundHint}");
                    return;
                }
                _suppressorTarget = target;
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, IntPtr.Add(target.Value, Constants.CodePatches.InfiniteSuppressor.Patch1Offset), Nops(Constants.CodePatches.InfiniteSuppressor.Patch1Original.Length))
                    & MemoryManager.WriteRawBytes(_processHandle, IntPtr.Add(target.Value, Constants.CodePatches.InfiniteSuppressor.Patch2Offset), Nops(Constants.CodePatches.InfiniteSuppressor.Patch2Original.Length)));
                if (!IsUsable)
                {
                    return;
                }
                SetStatus(ok ? "Infinite Suppressor: enabled" : "Infinite Suppressor: write failed");
            }
            else if (_suppressorTarget is { } target)
            {
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, IntPtr.Add(target, Constants.CodePatches.InfiniteSuppressor.Patch1Offset), Constants.CodePatches.InfiniteSuppressor.Patch1Original)
                    & MemoryManager.WriteRawBytes(_processHandle, IntPtr.Add(target, Constants.CodePatches.InfiniteSuppressor.Patch2Offset), Constants.CodePatches.InfiniteSuppressor.Patch2Original));
                if (!IsUsable)
                {
                    return;
                }
                SetStatus(ok ? "Infinite Suppressor: disabled" : "Infinite Suppressor: write failed");
            }
        }

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
                    SetStatus($"No Alerts: {PatternNotFoundHint}");
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
                    body.AddRange([0x66, 0xC7, 0x87, 0x38, 0x01, 0x00, 0x00, 0x64, 0x00]); // mov word ptr [rdi+0x138], 100
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
                    SetStatus($"Camo Always 100%: {PatternNotFoundHint}");
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
                IntPtr? target = await Task.Run(() => FindAob(pattern));
                if (!IsUsable)
                {
                    return;
                }
                if (target == null)
                {
                    SetStatus($"Infinite Battery: {PatternNotFoundHint}");
                    return;
                }
                _batteryTarget = target;
                bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target.Value, Nops(pattern.Length)));
                if (!IsUsable)
                {
                    return;
                }
                if (ResolveLinkVarBuf() is { } linkVarBuf
                    && MemoryManager.ReadUInt16(_processHandle, IntPtr.Add(linkVarBuf, (int)Constants.CodePatches.InfiniteBattery.MaxOffset)) is { } max)
                {
                    MemoryManager.WriteUInt16(_processHandle, IntPtr.Add(linkVarBuf, (int)Constants.CodePatches.InfiniteBattery.CurrentOffset), max);
                }
                SetStatus(ok ? "Infinite Battery: enabled" : "Infinite Battery: write failed");
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
    }
}
