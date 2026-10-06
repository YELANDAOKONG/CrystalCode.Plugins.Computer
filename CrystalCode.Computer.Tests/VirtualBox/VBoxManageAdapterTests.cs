using System.Buffers.Binary;

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

    [Fact]
    public async Task Capture_ReadsTheScreenshotAndRemovesItsDirectory()
    {
        string? directory = null;
        UnixFileMode? mode = null;
        var runner = new ScriptedProcessRunner
        {
            Handle = arguments =>
            {
                if (arguments[0] == "showvminfo")
                {
                    return new ProcessResult(0, RunningAdditions(), "", false);
                }

                var path = arguments[3];
                directory = Path.GetDirectoryName(path);
                if (!OperatingSystem.IsWindows())
                {
                    mode = File.GetUnixFileMode(directory!);
                }

                File.WriteAllBytes(path, Png(640, 480));
                return new ProcessResult(0, "", "", false);
            }
        };
        var adapter = new VBoxManageAdapter(
            new ComputerSettings(Uuid, "VBoxManage"),
            runner,
            "pointer.py");

        var capture = await adapter.CaptureAsync(0, CancellationToken.None);

        Assert.Equal(640, capture.Width);
        Assert.Equal(480, capture.Height);
        var command = runner.Arguments[1];
        Assert.Equal(["controlvm", Uuid, "screenshotpng"], command.Take(3));
        Assert.Equal("0", command[4]);
        Assert.NotNull(directory);
        Assert.False(Directory.Exists(directory));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
                mode);
        }
    }

    [Fact]
    public async Task Capture_InvalidImage_FailsAndRemovesItsDirectory()
    {
        string? directory = null;
        var runner = new ScriptedProcessRunner
        {
            Handle = arguments =>
            {
                if (arguments[0] == "showvminfo")
                {
                    return new ProcessResult(0, RunningAdditions(), "", false);
                }

                directory = Path.GetDirectoryName(arguments[3]);
                File.WriteAllBytes(arguments[3], new byte[64]);
                return new ProcessResult(0, "", "", false);
            }
        };
        var adapter = new VBoxManageAdapter(
            new ComputerSettings(Uuid, "VBoxManage"),
            runner,
            "pointer.py");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.CaptureAsync(0, CancellationToken.None));

        Assert.Equal("VirtualBox did not produce a PNG image.", exception.Message);
        Assert.NotNull(directory);
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task Capture_VBoxManageFailure_RemovesItsDirectory()
    {
        string? directory = null;
        var runner = new ScriptedProcessRunner
        {
            Handle = arguments =>
            {
                if (arguments[0] == "showvminfo")
                {
                    return new ProcessResult(0, RunningAdditions(), "", false);
                }

                directory = Path.GetDirectoryName(arguments[3]);
                return new ProcessResult(1, "", "", false);
            }
        };
        var adapter = new VBoxManageAdapter(
            new ComputerSettings(Uuid, "VBoxManage"),
            runner,
            "pointer.py");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.CaptureAsync(0, CancellationToken.None));

        Assert.Equal("VBoxManage failed with exit code 1.", exception.Message);
        Assert.NotNull(directory);
        Assert.False(Directory.Exists(directory));
    }

    private static byte[] Png(int width, int height)
    {
        var data = new byte[33];
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        signature.CopyTo(data, 0);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(8, 4), 13);
        "IHDR"u8.CopyTo(data.AsSpan(12));
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(20, 4), height);
        return data;
    }

    private static string RunningAdditions() =>
        $"""
         UUID="{Uuid}"
         VMState="running"
         GuestAdditionsRunLevel=2
         GuestAdditionsVersion="7.1.6"
         """;
}
