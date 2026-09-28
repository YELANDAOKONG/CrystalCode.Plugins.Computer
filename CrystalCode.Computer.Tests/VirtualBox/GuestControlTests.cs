using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class GuestControlTests
{
    [Fact]
    public void Arguments_PlaceGuestArgumentsAfterTheSeparator()
    {
        var arguments = GuestControl.Arguments(
            "11111111-1111-1111-1111-111111111111",
            new GuestCommand("/bin/echo", ["hello", "world"], "/tmp", 15),
            "guest",
            "/secret/password");

        Assert.Contains("--passwordfile", arguments);
        Assert.Contains("/secret/password", arguments);
        Assert.DoesNotContain("--password", arguments);
        var separator = arguments.ToList().IndexOf("--");
        Assert.True(separator > 0);
        Assert.Equal(["hello", "world"], arguments.Skip(separator + 1));
        Assert.Contains("15000", arguments);
    }

    [Fact]
    public void Require_MissingPasswordFile_DoesNotEchoThePath()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GuestSession.Require("guest", "/missing/password"));

        Assert.DoesNotContain("/missing/password", exception.Message, StringComparison.Ordinal);
    }
}
