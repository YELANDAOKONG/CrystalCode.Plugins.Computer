using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class PointerCommandsTests
{
    [Fact]
    public void Click_PressesAndReleases()
    {
        var events = PointerCommands.Click(11, 21, PointerButtons.Left, clicks: 1);

        Assert.Equal(3, events.Count);
        Assert.Equal(0, events[0].Buttons);
        Assert.Equal(PointerButtons.Left, events[1].Buttons);
        Assert.Equal(0, events[2].Buttons);
        Assert.Equal(11, events[1].X);
        Assert.Equal(21, events[1].Y);
    }

    [Fact]
    public void Scroll_Up_UsesANegativeWheel()
    {
        var events = PointerCommands.Scroll(4, 5, ScrollDirection.Up, amount: 3);

        Assert.Equal(-3, events[1].VerticalWheel);
        Assert.Equal(0, events[1].HorizontalWheel);
    }

    [Fact]
    public void Scroll_Left_UsesAPositiveHorizontalWheel()
    {
        var events = PointerCommands.Scroll(4, 5, ScrollDirection.Left, amount: 2);

        Assert.Equal(2, events[1].HorizontalWheel);
        Assert.Equal(0, events[1].VerticalWheel);
    }
}
