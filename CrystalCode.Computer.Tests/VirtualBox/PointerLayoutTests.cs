using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class PointerLayoutTests
{
    private const string LayoutJson =
        """
        {"absolute":true,"screens":[
          {"display":0,"width":1920,"height":1080,"x":0,"y":0,"active":true},
          {"display":1,"width":1280,"height":720,"x":1920,"y":0,"active":false}
        ]}
        """;

    [Fact]
    public void Locate_PrimaryOrigin_IsOneBased()
    {
        var layout = PointerLayout.Parse(LayoutJson);

        Assert.Equal((1, 1), layout.Locate(0, 0, 0));
        Assert.Equal((1920, 1080), layout.Locate(0, 1919, 1079));
    }

    [Fact]
    public void Locate_OutsideTheImage_IsRejected()
    {
        var layout = PointerLayout.Parse(LayoutJson);

        var exception = Assert.Throws<InvalidOperationException>(() => layout.Locate(0, 1920, 0));

        Assert.Equal("The pointer position is outside the display.", exception.Message);
    }

    [Fact]
    public void Locate_InactiveDisplay_IsRejected()
    {
        var layout = PointerLayout.Parse(LayoutJson);

        var exception = Assert.Throws<InvalidOperationException>(() => layout.Locate(1, 0, 0));

        Assert.Equal("The display is not active.", exception.Message);
    }

    [Fact]
    public void Locate_RelativePointer_IsRejected()
    {
        var layout = PointerLayout.Parse(
            """
            {"absolute":false,"screens":[
              {"display":0,"width":800,"height":600,"x":0,"y":0,"active":true}
            ]}
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => layout.Locate(0, 1, 1));

        Assert.Contains("USB Tablet", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Locate_SecondDisplay_AddsItsOrigin()
    {
        var layout = PointerLayout.Parse(
            """
            {"absolute":true,"screens":[
              {"display":0,"width":1920,"height":1080,"x":0,"y":0,"active":true},
              {"display":1,"width":1280,"height":720,"x":1920,"y":0,"active":true}
            ]}
            """);

        Assert.Equal((1921, 1), layout.Locate(1, 0, 0));
    }
}
