using System.Windows.Forms;

namespace MGS4CheatTrainer
{
    public partial class TrainerForm
    {
        private IntPtr? _enemyControlTarget;
        private IntPtr? _enemyControlModeAddr;
        private ComboBox _enemyActionCombo = null!;

        // Enemy actor hook: the mode byte (0 off / 1 kill / 2 sleep) lives PAST the trailing jmp-back, as
        // dead data nothing ever executes into -- the jmp-out redirect lands on cave+0, which has to be
        // the first real instruction.
        private async Task ToggleEnemyControl()
        {
            var checkbox = _pendingToggles["Enemy Control"];
            if (checkbox.Checked)
            {
                byte[] scanPattern = Constants.CodePatches.EnemyControl.ScanPattern;
                int patchLength = Constants.CodePatches.EnemyControl.PatchLength;
                int modeByteOffset = 0;
                var result = await Task.Run(() => MemoryManager.InjectTrampolineScanned(
                    _processHandle, _baseAddress, _moduleSize,
                    scanPattern, new string('x', scanPattern.Length), patchLength,
                    (targetAddress, cave) =>
                    {
                        var body = new List<byte>
                        {
                            0x48,
                            0xB8 // mov rax, imm64 (address of the mode byte, patched in below)
                        };
                        int modeAddrImmOffset = body.Count;
                        body.AddRange(new byte[8]); // placeholder
                        body.AddRange([0x80, 0x38, 0x01]); // cmp byte ptr [rax],1
                        int jne1 = body.Count;
                        body.Add(0x75);
                        body.Add(0x00);
                        body.AddRange([0xC7, 0x87, 0x14, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]); // mov dword ptr [rdi+0x314],0 (health)
                        body[jne1 + 1] = (byte)(body.Count - (jne1 + 2));
                        body.AddRange([0x80, 0x38, 0x02]); // cmp byte ptr [rax],2
                        int jne2 = body.Count;
                        body.Add(0x75);
                        body.Add(0x00);
                        body.AddRange([0xC7, 0x87, 0x24, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]); // mov dword ptr [rdi+0x324],0 (sleep timer)
                        body[jne2 + 1] = (byte)(body.Count - (jne2 + 2));
                        body.AddRange(Constants.CodePatches.EnemyControl.PatchBytes); // original: mov eax,[rdi+0x324]
                        IntPtr returnAddress = IntPtr.Add(targetAddress, patchLength);
                        body.AddRange(MemoryManager.JumpRel32(cave, body.Count, returnAddress));

                        modeByteOffset = body.Count;
                        byte[] modeAddrBytes = BitConverter.GetBytes(cave.ToInt64() + modeByteOffset);
                        for (int i = 0; i < 8; i++)
                        {
                            body[modeAddrImmOffset + i] = modeAddrBytes[i];
                        }
                        body.Add(0x00);

                        return body.ToArray();
                    }));

                if (!IsUsable)
                {
                    return;
                }
                if (result == null)
                {
                    SetStatus($"Enemy Control: {PatternNotFoundHint}");
                    return;
                }
                _enemyControlTarget = result.Value.Target;
                _enemyControlModeAddr = IntPtr.Add(result.Value.Cave, modeByteOffset);
                _suppressEvents = true;
                _enemyActionCombo.SelectedIndex = 0;
                _suppressEvents = false;
                _enemyActionCombo.Enabled = true;
                long enemyOffset = _enemyControlTarget.Value.ToInt64() - _baseAddress.ToInt64();
                SetStatus($"Enemy Control: enabled at mgs4.exe+{enemyOffset:X}");
            }
            else
            {
                _enemyActionCombo.Enabled = false;
                _enemyControlModeAddr = null;
                if (_enemyControlTarget is { } target)
                {
                    bool ok = await Task.Run(() => MemoryManager.WriteRawBytes(_processHandle, target, Constants.CodePatches.EnemyControl.PatchBytes));
                    if (!IsUsable)
                    {
                        return;
                    }
                    SetStatus(ok ? "Enemy Control: disabled" : "Enemy Control: write failed");
                }
            }
        }
    }
}
