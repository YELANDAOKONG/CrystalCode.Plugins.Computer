using CrystalCode.Computer.Configuration;
using CrystalCode.Computer.Interfaces;
using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

[Collection(AdapterCollection.Name)]
public sealed class VBoxManageAdapterTests
{
    private const string Uuid = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task Run_RedactsThePasswordFile()
    {
        var password = Path.GetTempFileName();
        var script = Path.GetTempFileName();
        var previousUser = Environment.GetEnvironmentVariable(GuestSession.UsernameVariable);
        var previousFile = Environment.GetEnvironmentVariable(GuestSession.PasswordFileVariable);
        try
        {
            Environment.SetEnvironmentVariable(GuestSession.UsernameVariable, "guest");
            Environment.SetEnvironmentVariable(GuestSession.PasswordFileVariable, password);
            var runner = new ScriptedProcessRunner
            {
                Handle = arguments => arguments[0] == "showvminfo"
                    ? new ProcessResult(0, RunningAdditions(), "", false)
                    : new ProcessResult(7, "out " + password, "err " + password, false)
            };
            var adapter = new VBoxManageAdapter(
                new ComputerSettings(Uuid, "VBoxManage"),
                runner,
                script);

            var result = await adapter.RunAsync(
                new GuestCommand("/bin/echo", ["hi"], null, 5),
                CancellationToken.None);
            var text = result.Format();

            Assert.Contains("Exit code: 7", text, StringComparison.Ordinal);
            Assert.Contains("[password file]", text, StringComparison.Ordinal);
            Assert.DoesNotContain(password, text, StringComparison.Ordinal);
            Assert.Equal(2, runner.Arguments.Count);
        }
        finally
        {
            Environment.SetEnvironmentVariable(GuestSession.UsernameVariable, previousUser);
            Environment.SetEnvironmentVariable(GuestSession.PasswordFileVariable, previousFile);
            File.Delete(password);
            File.Delete(script);
        }
    }

    [Fact]
    public async Task Run_InactiveGuestAdditions_DoesNotStartTheGuest()
    {
        var runner = new ScriptedProcessRunner
        {
            Handle = _ => new ProcessResult(
                0,
                $"""
                 UUID="{Uuid}"
                 VMState="running"
                 GuestAdditionsRunLevel=0
                 """,
                "",
                false)
        };
        var adapter = new VBoxManageAdapter(
            new ComputerSettings(Uuid, "VBoxManage"),
            runner,
            "pointer.py");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.RunAsync(
                new GuestCommand("/bin/true", [], null, 5),
                CancellationToken.None));

        Assert.Contains("not loaded", exception.Message, StringComparison.Ordinal);
        Assert.Contains("active Guest Additions", exception.Message, StringComparison.Ordinal);
        Assert.Single(runner.Arguments);
    }

    private static string RunningAdditions() =>
        $"""
         UUID="{Uuid}"
         VMState="running"
         GuestAdditionsRunLevel=2
         GuestAdditionsVersion="7.1.6"
         """;
}
