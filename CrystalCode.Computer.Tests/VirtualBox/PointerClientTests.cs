using CrystalCode.Computer.Configuration;
using CrystalCode.Computer.Interfaces;
using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class PointerClientTests
{
    [Fact]
    public async Task Click_SendsOneBasedPixels()
    {
        var script = Path.GetTempFileName();
        try
        {
            var runner = new ScriptedProcessRunner
            {
                Handle = arguments => arguments.Contains("layout")
                    ? new ProcessResult(
                        0,
                        """
                        {"absolute":true,"screens":[{"display":0,"width":100,"height":80,"x":0,"y":0,"active":true}]}
                        """,
                        "",
                        false)
                    : new ProcessResult(0, "", "", false)
            };
            var client = new PointerClient(
                new ComputerSettings("11111111-1111-1111-1111-111111111111", "VBoxManage"),
                runner,
                script);

            await client.ClickAsync(0, 10, 20, PointerButton.Left, 1, CancellationToken.None);

            var events = runner.Arguments[1];
            Assert.Equal("events", events[1]);
            Assert.Equal("11", events[4]);
            Assert.Equal("21", events[5]);
            Assert.Equal("1", events[14]);
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Fact]
    public async Task Click_MissingPointerApi_UsesAFixedMessage()
    {
        var script = Path.GetTempFileName();
        try
        {
            var runner = new ScriptedProcessRunner
            {
                Handle = _ => new ProcessResult(1, "api-unavailable", "", false)
            };
            var client = new PointerClient(
                new ComputerSettings("11111111-1111-1111-1111-111111111111", "VBoxManage"),
                runner,
                script);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.ClickAsync(0, 1, 1, PointerButton.Left, 1, CancellationToken.None));

            Assert.Contains("CRYSTAL_COMPUTER_VBOX_HOME", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(script);
        }
    }
}
