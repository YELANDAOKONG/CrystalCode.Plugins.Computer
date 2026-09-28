using CrystalCode.Computer.Interfaces;
using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Configuration;

internal sealed record ComputerSettings(string VmUuid, string Executable)
{
    public static IComputerAdapter CreateAdapter()
    {
        var value = Environment.GetEnvironmentVariable("CRYSTAL_COMPUTER_VM_UUID");
        if (!Guid.TryParse(value, out var uuid))
        {
            throw new InvalidOperationException(
                "Set CRYSTAL_COMPUTER_VM_UUID to the UUID of the authorized VirtualBox VM.");
        }

        var executable = Environment.GetEnvironmentVariable("CRYSTAL_COMPUTER_VBOXMANAGE");
        var settings = new ComputerSettings(
            uuid.ToString("D"),
            string.IsNullOrWhiteSpace(executable) ? "VBoxManage" : executable);
        return new VBoxManageAdapter(settings);
    }
}
