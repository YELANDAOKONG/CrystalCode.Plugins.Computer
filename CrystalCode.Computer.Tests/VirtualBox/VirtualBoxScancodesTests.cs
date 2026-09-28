using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class VirtualBoxScancodesTests
{
    [Fact]
    public void TryBuild_FunctionKey_ReleasesTheKey()
    {
        var built = VirtualBoxScancodes.TryBuild(["f1"], out var bytes);

        Assert.True(built);
        Assert.Equal(["3b", "bb"], bytes);
    }

    [Fact]
    public void TryBuild_PageUp_UsesTheExtendedBreakCode()
    {
        var built = VirtualBoxScancodes.TryBuild(["pageup"], out var bytes);

        Assert.True(built);
        Assert.Equal(["e0", "49", "e0", "c9"], bytes);
    }

    [Fact]
    public void TryBuild_DuplicateKey_IsRejected()
    {
        var built = VirtualBoxScancodes.TryBuild(["ctrl", "ctrl"], out _);

        Assert.False(built);
    }
}
