using CrystalCode.Computer.Interfaces;

namespace CrystalCode.Computer.Configuration;

internal static class ComputerAdapters
{
    private static Func<IComputerAdapter> _create = ComputerSettings.CreateAdapter;

    internal static Func<IComputerAdapter> Create
    {
        get => _create;
        set => _create = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal static IComputerAdapter Open() => _create();
}
