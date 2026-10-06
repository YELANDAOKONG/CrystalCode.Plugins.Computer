using System.Diagnostics;
using System.Globalization;

using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class VBoxManageProcessTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task RunAsync_CapturesOutputAndExitCode()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var result = await new VBoxManageProcess().RunAsync(
            "/bin/sh",
            ["-c", "echo out; echo err >&2; exit 3"],
            CancellationToken.None);

        Assert.Equal(3, result.ExitCode);
        Assert.Equal("out\n", result.StandardOutput);
        Assert.Equal("err\n", result.StandardError);
        Assert.False(result.Truncated);
    }

    [Fact]
    public async Task RunAsync_GivesTheChildAnEmptyStandardInput()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var result = await new VBoxManageProcess().RunAsync(
            "/bin/sh",
            ["-c", "if read line; then echo data; else echo eof; fi"],
            CancellationToken.None);

        Assert.Equal("eof\n", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_PassesTheEnvironment()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var result = await new VBoxManageProcess().RunAsync(
            "/bin/sh",
            ["-c", "printf %s \"$CRYSTAL_COMPUTER_TEST_VALUE\""],
            new Dictionary<string, string> { ["CRYSTAL_COMPUTER_TEST_VALUE"] = "value" },
            CancellationToken.None);

        Assert.Equal("value", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_LargeOutput_IsTruncated()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var result = await new VBoxManageProcess().RunAsync(
            "/bin/sh",
            ["-c", "head -c 150000 /dev/zero | tr '\\0' a"],
            CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Truncated);
        Assert.Equal(GuestProcessResult.MaximumCharacters, result.StandardOutput.Length);
    }

    [Fact]
    public async Task RunAsync_MissingExecutable_ThrowsProcessStartException()
    {
        await Assert.ThrowsAsync<ProcessStartException>(() =>
            new VBoxManageProcess().RunAsync(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                [],
                CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_Cancelled_KillsTheChild()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var pidFile = Path.GetTempFileName();
        try
        {
            using var cancellation = new CancellationTokenSource();
            var running = new VBoxManageProcess().RunAsync(
                "/bin/sh",
                ["-c", "echo $$ > \"$0\"; exec sleep 60", pidFile],
                cancellation.Token);

            var pid = await ReadPidAsync(pidFile);
            Assert.True(IsRunning(pid));

            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
            Assert.True(await StopsRunningAsync(pid));
        }
        finally
        {
            File.Delete(pidFile);
        }
    }

    private static async Task<int> ReadPidAsync(string pidFile)
    {
        var deadline = DateTime.UtcNow + Patience;
        while (DateTime.UtcNow < deadline)
        {
            var text = (await File.ReadAllTextAsync(pidFile)).Trim();
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
            {
                return pid;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException("The child did not report its process id.");
    }

    private static async Task<bool> StopsRunningAsync(int pid)
    {
        var deadline = DateTime.UtcNow + Patience;
        while (DateTime.UtcNow < deadline)
        {
            if (!IsRunning(pid))
            {
                return true;
            }

            await Task.Delay(20);
        }

        return false;
    }

    private static bool IsRunning(int pid)
    {
        var stat = $"/proc/{pid}/stat";
        if (File.Exists(stat))
        {
            try
            {
                // The state follows the parenthesised command name. A zombie
                // has exited and only waits for its parent to collect it.
                var text = File.ReadAllText(stat);
                var state = text.LastIndexOf(')') + 2;
                return state >= text.Length || text[state] != 'Z';
            }
            catch (IOException)
            {
                return false;
            }
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
