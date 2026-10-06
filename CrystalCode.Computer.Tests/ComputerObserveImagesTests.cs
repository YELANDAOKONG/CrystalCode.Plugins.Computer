using Crystal.Tools;

using CrystalCode.Plugins.Hooks;

using CrystalCode.Computer;

namespace CrystalCode.Computer.Tests;

public sealed class ComputerObserveImagesTests
{
    [Fact]
    public async Task AfterTool_LeavesOtherToolImages()
    {
        var hook = new ComputerObserveImages();
        var result = Capture("other");

        var next = await hook.AfterToolAsync(
            new ToolCall("1", "read", "{}"),
            result);

        Assert.Null(next);
        Assert.Single(result.Images);
    }

    [Fact]
    public async Task AfterTool_KeepsTheThreeNewestObserveCaptures()
    {
        var hook = new ComputerObserveImages();
        var first = Capture("first");
        var second = Capture("second");
        var third = Capture("third");
        var fourth = Capture("fourth");

        Assert.Null(await hook.AfterToolAsync(Observe("a"), first));
        Assert.Null(await hook.AfterToolAsync(Observe("b"), second));
        Assert.Null(await hook.AfterToolAsync(Observe("c"), third));
        Assert.Null(await hook.AfterToolAsync(Observe("d"), fourth));

        var expired = await hook.AfterToolAsync(Observe("a"), first);
        var kept = await hook.AfterToolAsync(Observe("d"), fourth);

        Assert.NotNull(expired);
        Assert.Equal(first.Text, expired.Text);
        Assert.Equal(first.Success, expired.Success);
        Assert.Empty(expired.Images);
        Assert.Null(kept);
    }

    [Fact]
    public async Task SessionStart_StartsANewWindow()
    {
        var hook = new ComputerObserveImages();
        await hook.AfterToolAsync(Observe("a"), Capture("first"));
        await hook.AfterToolAsync(Observe("b"), Capture("second"));
        await hook.AfterToolAsync(Observe("c"), Capture("third"));
        await hook.OnSessionStartedAsync(new PluginSession("/workspace", "session-2", "review"));

        Assert.Null(await hook.AfterToolAsync(Observe("e"), Capture("current")));
        Assert.Null(await hook.AfterToolAsync(Observe("f"), Capture("second")));
        Assert.Null(await hook.AfterToolAsync(Observe("g"), Capture("third")));
        Assert.Null(await hook.AfterToolAsync(Observe("h"), Capture("fourth")));

        var expired = await hook.AfterToolAsync(Observe("e"), Capture("current"));

        Assert.NotNull(expired);
        Assert.Empty(expired.Images);
    }

    private static ToolCall Observe(string callId) =>
        new(callId, ComputerObserveImages.ToolName, "{}");

    private static PluginToolResult Capture(string text) =>
        new(text, true, [new PluginImage("image/png", new byte[] { 1, 2, 3 })]);
}
