namespace CrystalCode.Computer.Interfaces;

internal sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool Truncated);
