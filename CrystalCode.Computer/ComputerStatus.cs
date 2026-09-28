namespace CrystalCode.Computer;

internal sealed record ComputerStatus(string VmState, GuestAdditionsReport Additions)
{
    public bool IsRunning =>
        string.Equals(VmState, "running", StringComparison.OrdinalIgnoreCase);

    public string Describe() =>
        $"VirtualBox reports the configured VM as {VmState} in this session. {Additions.Describe()}";
}
