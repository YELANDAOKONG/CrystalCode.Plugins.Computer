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

    [Theory]
    [InlineData("{\"display\":\"0\",\"x\":4,\"y\":9}")]
    [InlineData("{\"x\":\"4\",\"y\":9}")]
    [InlineData("{\"x\":4,\"y\":false}")]
    [InlineData("{\"x\":4,\"y\":9,\"clicks\":\"2\"}")]
    public async Task Click_InvalidIntegerTypes_ReturnFailure(string arguments)
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerClickTool().InvokeAsync(
                new ToolCall("1", "computer_click", arguments));

            Assert.Equal(ToolResultStatus.Failure, output.Status);
            Assert.False(adapter.Opened);
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

    [Theory]
    [InlineData("{\"executable\":\"/bin/echo\",\"timeoutSeconds\":\"5\"}")]
    [InlineData("{\"executable\":\"/bin/echo\",\"timeoutSeconds\":true}")]
    [InlineData("{\"executable\":\"/bin/echo\",\"timeoutSeconds\":null}")]
    [InlineData("{\"executable\":\"/bin/echo\",\"timeoutSeconds\":1.5}")]
    [InlineData("{\"executable\":\"/bin/echo\",\"timeoutSeconds\":0}")]
    [InlineData("{\"executable\":\"/bin/echo\",\"timeoutSeconds\":61}")]
    public async Task Run_InvalidTimeout_ReturnsFailure(string arguments)
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerRunTool().InvokeAsync(
                new ToolCall("1", "computer_run", arguments));

            Assert.Equal(ToolResultStatus.Failure, output.Status);
            Assert.Contains("Timeout", output.Text, StringComparison.Ordinal);
            Assert.False(adapter.Opened);
        });
    }

    [Fact]
    public async Task Run_WithoutTimeout_UsesTheDefault()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerRunTool().InvokeAsync(
                new ToolCall("1", "computer_run", "{\"executable\":\"/bin/echo\"}"));

            Assert.Equal(ToolResultStatus.Success, output.Status);
            Assert.Equal(ComputerRunTool.DefaultTimeoutSeconds, adapter.Command!.TimeoutSeconds);
        });
    }

    [Theory]
    [InlineData("{\"x\":1,\"y\":1,\"direction\":\"down\",\"amount\":\"5\"}")]
    [InlineData("{\"x\":1,\"y\":1,\"direction\":\"down\",\"amount\":true}")]
    [InlineData("{\"x\":1,\"y\":1,\"direction\":\"down\",\"amount\":null}")]
    [InlineData("{\"x\":1,\"y\":1,\"direction\":\"down\",\"amount\":2.5}")]
    [InlineData("{\"x\":1,\"y\":1,\"direction\":\"down\",\"amount\":0}")]
    [InlineData("{\"x\":1,\"y\":1,\"direction\":\"down\",\"amount\":21}")]
    public async Task Scroll_InvalidAmount_ReturnsFailure(string arguments)
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerScrollTool().InvokeAsync(
                new ToolCall("1", "computer_scroll", arguments));

            Assert.Equal(ToolResultStatus.Failure, output.Status);
            Assert.Contains("Scroll amount", output.Text, StringComparison.Ordinal);
            Assert.False(adapter.Opened);
        });
    }

    [Fact]
    public async Task Scroll_ValidAmount_ReachesTheAdapter()
    {
        var adapter = new RecordingAdapter();
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerScrollTool().InvokeAsync(
                new ToolCall(
                    "1",
                    "computer_scroll",
                    "{\"x\":1,\"y\":1,\"direction\":\"up\",\"amount\":20}"));

            Assert.Equal(ToolResultStatus.Success, output.Status);
            Assert.True(adapter.Opened);
        });
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    public async Task Type_FileSystemFailure_ReturnsFailure(Type failure)
    {
        var adapter = new RecordingAdapter
        {
            TypeFailure = (Exception)Activator.CreateInstance(failure)!
        };
        await UsingAdapter(adapter, async () =>
        {
            var output = await new ComputerTypeTool().InvokeAsync(
                new ToolCall("1", "computer_type", "{\"text\":\"hi\"}"));

            Assert.Equal(ToolResultStatus.Failure, output.Status);
            Assert.Equal("Text could not be sent to the configured VM.", output.Text);
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
