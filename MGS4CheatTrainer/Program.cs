using System.Diagnostics;
using System.Windows.Forms;
using MGS4CheatTrainer;
using static MGS4CheatTrainer.Constants;

Application.EnableVisualStyles();

Process? process = null;
while (process == null)
{
    process = MemoryManager.GetGameProcess();
    if (process != null)
    {
        break;
    }

    DialogResult choice = MessageBox.Show(
        $"'{Constants.PROCESS_NAME}.exe' isn't running.\n\nStart Metal Gear Solid 4, then click Retry.",
        "MGS4 Trainer",
        MessageBoxButtons.RetryCancel,
        MessageBoxIcon.Information);

    if (choice != DialogResult.Retry)
    {
        return;
    }
}

IntPtr processHandle = MemoryManager.OpenGameProcess(process);
if (processHandle == IntPtr.Zero)
{
    MessageBox.Show("Could not open a handle to the process. Try running the trainer as Administrator.", "MGS4 Trainer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    return;
}

IntPtr baseAddress;
long moduleSize;
try
{
    baseAddress = process.MainModule!.BaseAddress;
    moduleSize = process.MainModule.ModuleMemorySize;
}
catch (Exception ex)
{
    MessageBox.Show(
        $"Could not read module information for '{Constants.PROCESS_NAME}.exe': {ex.Message}\n\nTry running the trainer as Administrator.",
        "MGS4 Trainer",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
    MemoryManager.NativeMethods.CloseHandle(processHandle);
    return;
}

Application.Run(new TrainerForm(process, processHandle, baseAddress, moduleSize));

MemoryManager.NativeMethods.CloseHandle(processHandle);
