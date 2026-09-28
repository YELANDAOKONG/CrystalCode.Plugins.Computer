namespace CrystalCode.Computer;

internal sealed record ComputerStatus(string VmState, GuestAdditionsReport Additions)
{
    public bool IsRunning =>
        string.Equals(VmState, "running", StringComparison.OrdinalIgnoreCase);

    public string Describe() => $"The configured VM is {VmState}. {Additions.Describe()}";
}
