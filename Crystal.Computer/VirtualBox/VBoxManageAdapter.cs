using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using Crystal.Computer.Configuration;
using Crystal.Computer.Interfaces;

namespace Crystal.Computer.VirtualBox;

internal sealed class VBoxManageAdapter : IComputerAdapter
{
    private const int MaximumImageBytes = 20 * 1024 * 1024;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly SemaphoreSlim InputGate = new(1, 1);
    private readonly ComputerSettings _settings;

    public VBoxManageAdapter(ComputerSettings settings)
    {
        _settings = settings;
    }

    public async Task<string> GetStatusAsync(CancellationToken cancellationToken)
    {
        var output = await VBoxManageProcess.RunAsync(
            _settings.Executable,
            ["showvminfo", _settings.VmUuid, "--machinereadable"],
            cancellationToken);
        var expectedUuid = $"UUID=\"{_settings.VmUuid}\"";
        if (!output.Contains(expectedUuid, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("VirtualBox returned a different VM UUID.");
        }

        var state = output.Split('\n').FirstOrDefault(line =>
            line.StartsWith("VMState=", StringComparison.Ordinal));
        if (state is null)
        {
            throw new InvalidOperationException("VirtualBox did not report the VM state.");
        }

        return state["VMState=".Length..].Trim().Trim('"');
    }

    public async Task<ScreenCapture> CaptureAsync(
        int display,
        CancellationToken cancellationToken)
    {
        await RequireRunningAsync(cancellationToken);
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
            await VBoxManageProcess.RunAsync(
                _settings.Executable,
                ["controlvm", _settings.VmUuid, "screenshotpng", path,
                    display.ToString(CultureInfo.InvariantCulture)],
                cancellationToken);
            var file = new FileInfo(path);
            if (!file.Exists || file.Length is < 24 or > MaximumImageBytes)
            {
                throw new InvalidOperationException("VirtualBox screenshot size is invalid.");
            }

            var data = await File.ReadAllBytesAsync(path, cancellationToken);
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
        await InputGate.WaitAsync(cancellationToken);
        try
        {
            await RequireRunningAsync(cancellationToken);
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
                    await stream.WriteAsync(bytes, cancellationToken);
                }

                await VBoxManageProcess.RunAsync(
                    _settings.Executable,
                    ["controlvm", _settings.VmUuid, "keyboardputfile", path],
                    cancellationToken);
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

        await InputGate.WaitAsync(cancellationToken);
        try
        {
            await RequireRunningAsync(cancellationToken);
            var arguments = new List<string>
            {
                "controlvm", _settings.VmUuid, "keyboardputscancode"
            };
            arguments.AddRange(bytes);
            await VBoxManageProcess.RunAsync(
                _settings.Executable,
                arguments,
                cancellationToken);
        }
        finally
        {
            InputGate.Release();
        }
    }

    private async Task RequireRunningAsync(CancellationToken cancellationToken)
    {
        var state = await GetStatusAsync(cancellationToken);
        if (!string.Equals(state, "running", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The authorized VM is not running.");
        }
    }
}
