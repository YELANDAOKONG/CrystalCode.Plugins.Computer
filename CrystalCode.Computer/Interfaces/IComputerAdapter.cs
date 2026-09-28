namespace CrystalCode.Computer.Interfaces;

internal interface IComputerAdapter
{
    Task<ComputerStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<ScreenCapture> CaptureAsync(int display, CancellationToken cancellationToken);

    Task TypeTextAsync(string text, CancellationToken cancellationToken);

    Task PressKeysAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken);

    Task ClickAsync(
        int display,
        int x,
        int y,
        PointerButton button,
        int clicks,
        CancellationToken cancellationToken);

    Task DragAsync(
        int display,
        int fromX,
        int fromY,
        int toX,
        int toY,
        PointerButton button,
        CancellationToken cancellationToken);

    Task ScrollAsync(
        int display,
        int x,
        int y,
        ScrollDirection direction,
        int amount,
        CancellationToken cancellationToken);

    Task<GuestProcessResult> RunAsync(
        GuestCommand command,
        CancellationToken cancellationToken);
}
