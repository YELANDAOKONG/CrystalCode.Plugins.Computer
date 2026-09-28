using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using CrystalCode.Computer.Configuration;
using CrystalCode.Computer.Interfaces;

namespace CrystalCode.Computer.VirtualBox;

internal sealed class VBoxManageAdapter : IComputerAdapter
{
    private const int MaximumImageBytes = 20 * 1024 * 1024;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly SemaphoreSlim InputGate = new(1, 1);
    private readonly ComputerSettings _settings;
    private readonly IProcessRunner _processes;
    private readonly PointerClient _pointer;

    public VBoxManageAdapter(ComputerSettings settings, IProcessRunner processes)
        : this(settings, processes, scriptPath: null)
    {
    }

    public VBoxManageAdapter(
        ComputerSettings settings,
        IProcessRunner processes,
        string? scriptPath)
    {
        _settings = settings;
        _processes = processes;
        _pointer = new PointerClient(settings, processes, scriptPath);
    }

    public async Task<ComputerStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var output = await RunVBoxAsync(
            ["showvminfo", _settings.VmUuid, "--machinereadable"],
            cancellationToken).ConfigureAwait(false);
        return MachineInfo.Read(output, _settings.VmUuid);
    }

    public async Task<ScreenCapture> CaptureAsync(
        int display,
        CancellationToken cancellationToken)
    {
        await RequireRunningAsync(cancellationToken).ConfigureAwait(false);
        var directory = Directory.CreateTempSubdirectory("crystal-computer-");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                directory.FullName,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var path = Path.Combine(directory.FullName, "display.png");
        try
        {
            await RunVBoxAsync(
                [
                    "controlvm", _settings.VmUuid, "screenshotpng", path,
                    display.ToString(CultureInfo.InvariantCulture)
                ],
                cancellationToken).ConfigureAwait(false);
            var file = new FileInfo(path);
            if (!file.Exists || file.Length is < 24 or > MaximumImageBytes)
            {
                throw new InvalidOperationException("VirtualBox screenshot size is invalid.");
            }

            var data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            if (!data.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature)
                || !data.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            {
                throw new InvalidOperationException("VirtualBox did not produce a PNG image.");
            }

            var width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4));
            var height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4));
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("VirtualBox screenshot dimensions are invalid.");
            }

            return new ScreenCapture(data, width, height);
        }
        finally
        {
            File.Delete(path);
            directory.Delete();
        }
    }

    public async Task TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        await InputGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RequireRunningAsync(cancellationToken).ConfigureAwait(false);
            var path = Path.Combine(
                Path.GetTempPath(),
                $"crystal-computer-{Guid.NewGuid():N}.txt");
            try
            {
                var options = new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous
                };
                if (!OperatingSystem.IsWindows())
                {
                    options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                }

                await using (var stream = new FileStream(path, options))
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                }

                await RunVBoxAsync(
                    ["controlvm", _settings.VmUuid, "keyboardputfile", path],
                    cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                File.Delete(path);
            }
        }
        finally
        {
            InputGate.Release();
        }
    }

    public async Task PressKeysAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        if (!VirtualBoxScancodes.TryBuild(keys, out var bytes))
        {
            throw new InvalidOperationException("The key combination is not supported.");
        }

        await InputGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RequireRunningAsync(cancellationToken).ConfigureAwait(false);
            var arguments = new List<string>
            {
                "controlvm", _settings.VmUuid, "keyboardputscancode"
            };
            arguments.AddRange(bytes);
            await RunVBoxAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            InputGate.Release();
        }
    }

    public async Task ClickAsync(
        int display,
        int x,
        int y,
        PointerButton button,
        int clicks,
        CancellationToken cancellationToken)
    {
        await InputGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RequireRunningAsync(cancellationToken).ConfigureAwait(false);
            await _pointer.ClickAsync(display, x, y, button, clicks, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            InputGate.Release();
        }
    }

    public async Task DragAsync(
        int display,
        int fromX,
        int fromY,
        int toX,
        int toY,
        PointerButton button,
        CancellationToken cancellationToken)
    {
        await InputGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RequireRunningAsync(cancellationToken).ConfigureAwait(false);
            await _pointer.DragAsync(
                display, fromX, fromY, toX, toY, button, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            InputGate.Release();
        }
    }

    public async Task ScrollAsync(
        int display,
        int x,
        int y,
        ScrollDirection direction,
        int amount,
        CancellationToken cancellationToken)
    {
        await InputGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RequireRunningAsync(cancellationToken).ConfigureAwait(false);
            await _pointer.ScrollAsync(display, x, y, direction, amount, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            InputGate.Release();
        }
    }

    public async Task<GuestProcessResult> RunAsync(
        GuestCommand command,
        CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        if (!status.IsRunning)
        {
            throw new InvalidOperationException(
                "The authorized VM is not reported as running by this VirtualBox session.");
        }

        if (status.Additions.State != GuestAdditionsState.Active)
        {
            throw new InvalidOperationException(
                status.Additions.Describe() + " A guest process requires active Guest Additions.");
        }

        var (username, passwordFile) = GuestSession.Require();
        var result = await RunProcessAsync(
            GuestControl.Arguments(_settings.VmUuid, command, username, passwordFile),
            cancellationToken).ConfigureAwait(false);
        return new GuestProcessResult(
            result.ExitCode,
            Redact(result.StandardOutput, passwordFile),
            Redact(result.StandardError, passwordFile),
            result.Truncated);
    }

    private async Task RequireRunningAsync(CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        if (!status.IsRunning)
        {
            throw new InvalidOperationException(
                "The authorized VM is not reported as running by this VirtualBox session.");
        }
    }

    private async Task<string> RunVBoxAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"VBoxManage failed with exit code {result.ExitCode}.");
        }

        return result.StandardOutput;
    }

    private async Task<ProcessResult> RunProcessAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _processes.RunAsync(
                _settings.Executable,
                arguments,
                cancellationToken).ConfigureAwait(false);
        }
        catch (ProcessStartException exception)
        {
            throw new InvalidOperationException("VBoxManage could not be started.", exception);
        }
    }

    private static string Redact(string text, string passwordFile)
    {
        if (passwordFile.Length == 0
            || !text.Contains(passwordFile, StringComparison.Ordinal))
        {
            return text;
        }

        return text.Replace(passwordFile, "[password file]", StringComparison.Ordinal);
    }
}
