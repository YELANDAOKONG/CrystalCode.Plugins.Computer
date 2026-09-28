using Crystal.Tools;

using CrystalCode.Computer.Configuration;
using CrystalCode.Computer.Tools;

namespace CrystalCode.Computer.Tests.Tools;

[Collection(AdapterCollection.Name)]
public sealed class ComputerToolTests
{
    [Fact]
    public async Task Status_ReportsActiveGuestAdditions()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerStatusTool().InvokeAsync(
                new ToolCall("1", "computer_status", "{}"));

            Assert.Equal(ToolResultStatus.Success, output.Status);
            Assert.Contains("running", output.Text, StringComparison.Ordinal);
            Assert.Contains("Guest Additions 7.1.6 are active.", output.Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Type_RejectsCharactersVirtualBoxWouldSkip()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerTypeTool().InvokeAsync(
                new ToolCall("1", "computer_type", "{\"text\":\"caf\\u00e9\"}"));

            Assert.Equal(ToolResultStatus.Failure, output.Status);
            Assert.False(adapter.Opened);
        });
    }

    [Fact]
    public async Task Click_PassesPixelsToTheAdapter()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerClickTool().InvokeAsync(
                new ToolCall(
                    "1",
                    "computer_click",
                    "{\"x\":4,\"y\":9,\"button\":\"right\",\"clicks\":2}"));

            Assert.Equal(ToolResultStatus.Success, output.Status);
            Assert.Equal(4, adapter.ClickX);
            Assert.Equal(9, adapter.ClickY);
            Assert.Equal(PointerButton.Right, adapter.ClickButton);
            Assert.Equal(2, adapter.Clicks);
        });
    }

    [Fact]
    public async Task Run_PassesTheGuestCommand()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerRunTool().InvokeAsync(
                new ToolCall(
                    "1",
                    "computer_run",
                    "{\"executable\":\"/bin/echo\",\"arguments\":[\"hi\"],\"timeoutSeconds\":5}"));

            Assert.Equal(ToolResultStatus.Success, output.Status);
            Assert.Equal("/bin/echo", adapter.Command!.Executable);
            Assert.Equal(["hi"], adapter.Command.Arguments);
            Assert.Equal(5, adapter.Command.TimeoutSeconds);
            Assert.Contains("Exit code: 0", output.Text, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Keys_AcceptsFunctionKeys()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerKeysTool().InvokeAsync(
                new ToolCall("1", "computer_keys", "{\"keys\":[\"f12\"]}"));

            Assert.Equal(ToolResultStatus.Success, output.Status);
            Assert.Equal(["f12"], adapter.Keys);
        });
    }

    private static async Task UsingAdapter(RecordingAdapter adapter, Func<Task> action)
    {
        var previous = ComputerAdapters.Create;
        ComputerAdapters.Create = () => adapter;
        try
        {
            await action();
        }
        finally
        {
            ComputerAdapters.Create = previous;
        }
    }
}
