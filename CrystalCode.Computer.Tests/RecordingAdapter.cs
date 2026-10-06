using CrystalCode.Computer.Interfaces;

namespace CrystalCode.Computer.Tests;

internal sealed class RecordingAdapter : IComputerAdapter
{
    public ComputerStatus Status { get; set; } = new(
        "running",
        new GuestAdditionsReport(GuestAdditionsState.Active, "7.1.6"));

    public bool Opened { get; private set; }

    public Exception? TypeFailure { get; set; }

    public int ClickX { get; private set; }

    public int ClickY { get; private set; }

    public PointerButton ClickButton { get; private set; }

    public int Clicks { get; private set; }

    public GuestCommand? Command { get; private set; }

    public string? Typed { get; private set; }

    public IReadOnlyList<string>? Keys { get; private set; }

    public Task<ComputerStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        Opened = true;
        return Task.FromResult(Status);
    }

    public Task<ScreenCapture> CaptureAsync(int display, CancellationToken cancellationToken)
    {
        Opened = true;
        throw new InvalidOperationException("Capture was not expected.");
    }

    public Task TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        Opened = true;
        if (TypeFailure is not null)
        {
            throw TypeFailure;
        }

        Typed = text;
        return Task.CompletedTask;
    }

    public Task PressKeysAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        Opened = true;
        Keys = keys;
        return Task.CompletedTask;
    }

    public Task ClickAsync(
        int display,
        int x,
        int y,
        PointerButton button,
        int clicks,
        CancellationToken cancellationToken)
    {
        Opened = true;
        ClickX = x;
        ClickY = y;
        ClickButton = button;
        Clicks = clicks;
        return Task.CompletedTask;
    }

    public Task DragAsync(
        int display,
        int fromX,
        int fromY,
        int toX,
        int toY,
        PointerButton button,
        CancellationToken cancellationToken)
    {
        Opened = true;
        return Task.CompletedTask;
    }

    public Task ScrollAsync(
        int display,
        int x,
        int y,
        ScrollDirection direction,
        int amount,
        CancellationToken cancellationToken)
    {
        Opened = true;
        return Task.CompletedTask;
    }

    public Task<GuestProcessResult> RunAsync(
        GuestCommand command,
        CancellationToken cancellationToken)
    {
        Opened = true;
        Command = command;
        return Task.FromResult(new GuestProcessResult(0, "ok", "", false));
    }
}
