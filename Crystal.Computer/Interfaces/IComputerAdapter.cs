namespace Crystal.Computer.Interfaces;

internal interface IComputerAdapter
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken);

    Task<ScreenCapture> CaptureAsync(int display, CancellationToken cancellationToken);

    Task TypeTextAsync(string text, CancellationToken cancellationToken);

    Task PressKeysAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken);
}
