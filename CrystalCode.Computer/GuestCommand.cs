namespace CrystalCode.Computer;

internal sealed record GuestCommand(
    string Executable,
    IReadOnlyList<string> Arguments,
    string? Directory,
    int TimeoutSeconds);
