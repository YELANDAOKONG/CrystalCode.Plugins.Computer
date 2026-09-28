using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class MachineInfoTests
{
    private const string Uuid = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public void Read_ActiveGuestAdditions_ReportsVersion()
    {
        var status = MachineInfo.Read(
            """
            UUID="11111111-1111-1111-1111-111111111111"
            VMState="running"
            GuestAdditionsRunLevel=3
            GuestAdditionsVersion="7.1.6 r162802"
            """,
            Uuid);

        Assert.True(status.IsRunning);
        Assert.Equal(GuestAdditionsState.Active, status.Additions.State);
        Assert.Equal("7.1.6 r162802", status.Additions.Version);
        Assert.Contains("Guest Additions 7.1.6 r162802 are active.", status.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Read_DriversOnly_AreNotActive()
    {
        var status = MachineInfo.Read(
            """
            UUID="11111111-1111-1111-1111-111111111111"
            VMState="running"
            GuestAdditionsRunLevel=1
            GuestAdditionsVersion="7.0.14 r161095"
            """,
            Uuid);

        Assert.Equal(GuestAdditionsState.DriversLoaded, status.Additions.State);
        Assert.Contains("guest service is not active", status.Additions.Describe(), StringComparison.Ordinal);
    }

    [Fact]
    public void Read_RunningWithoutAdditions_IsNotLoaded()
    {
        var status = MachineInfo.Read(
            """
            UUID="11111111-1111-1111-1111-111111111111"
            VMState="running"
            GuestAdditionsRunLevel=0
            """,
            Uuid);

        Assert.Equal(GuestAdditionsState.NotLoaded, status.Additions.State);
        Assert.Equal("Guest Additions are not loaded.", status.Additions.Describe());
    }

    [Fact]
    public void Read_PoweredOff_DoesNotInventAdditions()
    {
        var status = MachineInfo.Read(
            """
            UUID="11111111-1111-1111-1111-111111111111"
            VMState="poweroff"
            """,
            Uuid);

        Assert.False(status.IsRunning);
        Assert.Equal(GuestAdditionsState.Unavailable, status.Additions.State);
    }

    [Fact]
    public void Read_UnescapesQuotedVersion()
    {
        var status = MachineInfo.Read(
            """
            UUID="11111111-1111-1111-1111-111111111111"
            VMState="running"
            GuestAdditionsRunLevel=2
            GuestAdditionsVersion="7.1.6 \"guest\""
            """,
            Uuid);

        Assert.Equal("7.1.6 \"guest\"", status.Additions.Version);
    }

    [Fact]
    public void Read_WrongUuid_Fails()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            MachineInfo.Read(
                """
                UUID="22222222-2222-2222-2222-222222222222"
                VMState="running"
                """,
                Uuid));

        Assert.Equal("VirtualBox returned a different VM UUID.", exception.Message);
    }
}
