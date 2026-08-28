using System.Diagnostics;
using System.Runtime.InteropServices;
using static MGS4CheatTrainer.Constants;

namespace MGS4CheatTrainer
{
    public static class MemoryManager
    {
        public static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        }

        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;

        public static Process? GetGameProcess()
        {
            return Process.GetProcessesByName(Constants.PROCESS_NAME).FirstOrDefault();
        }

        public static IntPtr OpenGameProcess(Process process)
        {
            return NativeMethods.OpenProcess(
                PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION,
                false,
                process.Id);
        }

        public static byte[]? ReadMemoryBytes(IntPtr processHandle, IntPtr address, int bytesToRead)
        {
            byte[] buffer = new byte[bytesToRead];
            if (NativeMethods.ReadProcessMemory(processHandle, address, buffer, (uint)buffer.Length, out _))
            {
                return buffer;
            }
            return null;
        }

        /// <summary>
        /// Dereferences an 8-byte pointer (this trainer only targets x64 processes).
        /// </summary>
        public static IntPtr ReadIntPtr(IntPtr processHandle, IntPtr address)
        {
            byte[]? buffer = ReadMemoryBytes(processHandle, address, 8);
            return buffer == null ? IntPtr.Zero : new IntPtr(BitConverter.ToInt64(buffer, 0));
        }

        public static ushort? ReadUInt16(IntPtr processHandle, IntPtr address)
        {
            byte[]? buffer = ReadMemoryBytes(processHandle, address, 2);
            return buffer == null ? null : BitConverter.ToUInt16(buffer, 0);
        }

        public static bool WriteUInt16(IntPtr processHandle, IntPtr address, ushort value)
        {
            return NativeMethods.WriteProcessMemory(processHandle, address, BitConverter.GetBytes(value), 2, out _);
        }

        public static uint? ReadUInt32(IntPtr processHandle, IntPtr address)
        {
            byte[]? buffer = ReadMemoryBytes(processHandle, address, 4);
            return buffer == null ? null : BitConverter.ToUInt32(buffer, 0);
        }

        public static bool WriteUInt32(IntPtr processHandle, IntPtr address, uint value)
        {
            return NativeMethods.WriteProcessMemory(processHandle, address, BitConverter.GetBytes(value), 4, out _);
        }

        /// <summary>
        /// Writes raw bytes at an address, temporarily marking the page PAGE_EXECUTE_READWRITE first.
        /// Use this for instruction patches (NOPing/restoring code in .text), which is normally not writable.
        /// </summary>
        public static bool WriteRawBytes(IntPtr processHandle, IntPtr address, byte[] bytes)
        {
            bool hasOldProtect = NativeMethods.VirtualProtectEx(processHandle, address, (uint)bytes.Length, PAGE_EXECUTE_READWRITE, out uint oldProtect);
            bool wrote = NativeMethods.WriteProcessMemory(processHandle, address, bytes, (uint)bytes.Length, out _);
            if (hasOldProtect)
            {
                NativeMethods.VirtualProtectEx(processHandle, address, (uint)bytes.Length, oldProtect, out _);
            }
            return wrote;
        }

        #region AOB scanning

        public static bool IsMatch(byte[] buffer, int position, byte[] pattern, string mask)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                char maskChar = mask[i] == ' ' ? 'x' : mask[i];
                if (maskChar == '?' || buffer[position + i] == pattern[i])
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Scans [startAddress, startAddress+size) for a byte pattern. mask: 'x' = exact match, '?' = wildcard, one char per pattern byte.
        /// </summary>
        public static List<IntPtr> ScanForAllInstances(IntPtr processHandle, IntPtr startAddress, long size, byte[] pattern, string mask)
        {
            var found = new List<IntPtr>();
            const int bufferSize = 1_000_000;
            byte[] buffer = new byte[bufferSize];

            long endAddress = startAddress.ToInt64() + size;
            for (long address = startAddress.ToInt64(); address < endAddress; address += bufferSize - pattern.Length)
            {
                int effectiveSize = (int)Math.Min(bufferSize, endAddress - address);
                if (effectiveSize <= pattern.Length)
                {
                    break;
                }

                bool success = NativeMethods.ReadProcessMemory(processHandle, new IntPtr(address), buffer, (uint)effectiveSize, out int bytesRead);
                if (!success || bytesRead == 0)
                {
                    continue;
                }

                for (int i = 0; i <= bytesRead - pattern.Length; i++)
                {
                    if (IsMatch(buffer, i, pattern, mask))
                    {
                        found.Add(new IntPtr(address + i));
                    }
                }
            }

            return found;
        }

        #endregion

        #region Code injection (trampolines)

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;

        /// <summary>
        /// Reserves+commits executable memory within +/-256MB of nearAddress, so a 5-byte rel32 jmp
        /// can reach it. Tries progressively further 64KB-aligned candidates on both sides until one succeeds.
        /// </summary>
        public static IntPtr AllocateNear(IntPtr processHandle, IntPtr nearAddress, uint size)
        {
            long anchor = nearAddress.ToInt64() & ~0xFFFFL;
            const long step = 0x10000;
            const int maxAttempts = 4096; // +/- ~256MB, comfortably inside the +/-2GB rel32 range

            for (int i = 1; i < maxAttempts; i++)
            {
                IntPtr below = NativeMethods.VirtualAllocEx(processHandle, new IntPtr(anchor - i * step), size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (below != IntPtr.Zero)
                {
                    return below;
                }

                IntPtr above = NativeMethods.VirtualAllocEx(processHandle, new IntPtr(anchor + i * step), size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (above != IntPtr.Zero)
                {
                    return above;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Classic AOB trampoline: finds the first match of originalInstruction in the module, allocates a
        /// nearby code cave, fills it via buildCaveBody(targetAddress, caveAddress) (which must end with a
        /// jmp back to targetAddress + originalInstruction.Length), then overwrites the original instruction
        /// with "jmp cave" padded with NOPs to the same length. Returns the patched target address (needed to
        /// restore later by writing originalInstruction back), or null if the pattern wasn't found or allocation failed.
        /// </summary>
        public static IntPtr? InjectTrampoline(
            IntPtr processHandle,
            IntPtr moduleBase,
            long moduleSize,
            byte[] originalInstruction,
            Func<IntPtr, IntPtr, byte[]> buildCaveBody)
        {
            string mask = new string('x', originalInstruction.Length);
            var matches = ScanForAllInstances(processHandle, moduleBase, moduleSize, originalInstruction, mask);
            if (matches.Count == 0)
            {
                return null;
            }

            IntPtr target = matches[0];

            IntPtr cave = AllocateNear(processHandle, target, 128);
            if (cave == IntPtr.Zero)
            {
                return null;
            }

            byte[] caveBytes = buildCaveBody(target, cave);
            if (!WriteRawBytes(processHandle, cave, caveBytes))
            {
                return null;
            }

            int patternLength = originalInstruction.Length;
            var jumpOut = new byte[patternLength];
            jumpOut[0] = 0xE9;
            int rel32Out = (int)(cave.ToInt64() - (target.ToInt64() + 5));
            BitConverter.GetBytes(rel32Out).CopyTo(jumpOut, 1);
            for (int i = 5; i < patternLength; i++)
            {
                jumpOut[i] = 0x90;
            }

            return WriteRawBytes(processHandle, target, jumpOut) ? target : null;
        }

        /// <summary>
        /// Computes the rel32 for a jmp/call placed at caveOffset bytes into the cave, targeting returnAddress.
        /// </summary>
        public static byte[] JumpRel32(IntPtr caveAddress, int caveOffsetOfJump, IntPtr returnAddress)
        {
            int rel32 = (int)(returnAddress.ToInt64() - (caveAddress.ToInt64() + caveOffsetOfJump + 5));
            var bytes = new byte[5];
            bytes[0] = 0xE9;
            BitConverter.GetBytes(rel32).CopyTo(bytes, 1);
            return bytes;
        }

        #endregion
    }
}
