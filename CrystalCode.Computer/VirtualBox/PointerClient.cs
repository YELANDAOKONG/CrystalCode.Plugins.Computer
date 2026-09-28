using System.Globalization;

using CrystalCode.Computer.Configuration;
using CrystalCode.Computer.Interfaces;

namespace CrystalCode.Computer.VirtualBox;

internal sealed class PointerClient
{
    private readonly ComputerSettings _settings;
    private readonly IProcessRunner _processes;
    private readonly string _scriptPath;

    public PointerClient(
        ComputerSettings settings,
        IProcessRunner processes,
        string? scriptPath = null)
    {
        _settings = settings;
        _processes = processes;
        _scriptPath = scriptPath ?? DefaultScriptPath();
    }

    public async Task ClickAsync(
        int display,
        int x,
        int y,
        PointerButton button,
        int clicks,
        CancellationToken cancellationToken)
    {
        var point = await LocateAsync(display, x, y, cancellationToken).ConfigureAwait(false);
        await SendAsync(
            PointerCommands.Click(point.X, point.Y, PointerButtons.Bit(button), clicks),
            cancellationToken).ConfigureAwait(false);
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
        var layout = await ReadLayoutAsync(cancellationToken).ConfigureAwait(false);
        var from = layout.Locate(display, fromX, fromY);
        var to = layout.Locate(display, toX, toY);
        await SendAsync(
            PointerCommands.Drag(from.X, from.Y, to.X, to.Y, PointerButtons.Bit(button)),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ScrollAsync(
        int display,
        int x,
        int y,
        ScrollDirection direction,
        int amount,
        CancellationToken cancellationToken)
    {
        var point = await LocateAsync(display, x, y, cancellationToken).ConfigureAwait(false);
        await SendAsync(
            PointerCommands.Scroll(point.X, point.Y, direction, amount),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<(int X, int Y)> LocateAsync(
        int display,
        int x,
        int y,
        CancellationToken cancellationToken)
    {
        var layout = await ReadLayoutAsync(cancellationToken).ConfigureAwait(false);
        return layout.Locate(display, x, y);
    }

    private async Task<PointerLayout> ReadLayoutAsync(CancellationToken cancellationToken)
    {
        var result = await RunScriptAsync(
            ["layout", _settings.VmUuid],
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.Truncated)
        {
            throw new InvalidOperationException(DescribeFailure(result.StandardOutput));
        }

        return PointerLayout.Parse(result.StandardOutput);
    }

    private async Task SendAsync(
        IReadOnlyList<PointerEvent> events,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string> { "events", _settings.VmUuid };
        foreach (var pointerEvent in events)
        {
            arguments.Add(pointerEvent.DelayMilliseconds.ToString(CultureInfo.InvariantCulture));
            arguments.Add(pointerEvent.X.ToString(CultureInfo.InvariantCulture));
            arguments.Add(pointerEvent.Y.ToString(CultureInfo.InvariantCulture));
            arguments.Add(pointerEvent.VerticalWheel.ToString(CultureInfo.InvariantCulture));
            arguments.Add(pointerEvent.HorizontalWheel.ToString(CultureInfo.InvariantCulture));
            arguments.Add(pointerEvent.Buttons.ToString(CultureInfo.InvariantCulture));
        }

        var result = await RunScriptAsync(arguments, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(DescribeFailure(result.StandardOutput));
        }
    }

    private async Task<ProcessResult> RunScriptAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_scriptPath))
        {
            throw new InvalidOperationException(
                "The pointer helper is not installed beside the tool assembly.");
        }

        var command = new List<string> { _scriptPath };
        command.AddRange(arguments);
        try
        {
            return await _processes.RunAsync(
                VirtualBoxHost.PythonExecutable(),
                command,
                VirtualBoxHost.PointerEnvironment(_settings.Executable),
                cancellationToken).ConfigureAwait(false);
        }
        catch (ProcessStartException exception)
        {
            throw new InvalidOperationException(
                "The VirtualBox pointer API is not available. Set CRYSTAL_COMPUTER_PYTHON if python3 is not on PATH.",
                exception);
        }
    }

    private static string DescribeFailure(string token)
    {
        return token.Trim() switch
        {
            "api-unavailable" =>
                "The VirtualBox pointer API is not available. Set CRYSTAL_COMPUTER_VBOX_HOME to the VirtualBox installation directory.",
            "absolute-unavailable" =>
                "The guest does not support an absolute pointer. Use a USB Tablet mouse.",
            "display-inactive" => "The display is not active.",
            "invalid-arguments" => "The pointer request was rejected.",
            _ => "The pointer event could not be sent."
        };
    }

    private static string DefaultScriptPath()
    {
        var directory = Path.GetDirectoryName(typeof(PointerClient).Assembly.Location);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException(
                "The pointer helper is not installed beside the tool assembly.");
        }

        return Path.Combine(directory, "Pointer.py");
    }
}
