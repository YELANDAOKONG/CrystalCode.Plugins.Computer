using Crystal.Chat;

using CrystalCode.Plugins.Hooks;

using CrystalCode.Computer;

namespace CrystalCode.Computer.Tests;

public sealed class ComputerObserveImagesTests
{
    [Fact]
    public async Task TransformModel_KeepsTheThreeNewestObserveCaptures()
    {
        var user = Message("user", 9);
        var read = Result("read-result", "read", 8);
        var first = Observe("0", 1);
        var second = Observe("1", 2);
        var quiet = ObserveWithoutImage("2");
        var third = Observe("3", 3);
        var fourth = Observe("4", 4);
        var fifth = Observe("5", 5);
        var request = Request([user, read, first, second, quiet, third, fourth, fifth]);

        var next = await new ComputerObserveImages().TransformModelAsync(request);

        Assert.NotNull(next);
        Assert.Equal(request.Items.Count, next.Count);
        Assert.Same(user, next[0]);
        Assert.Same(read, next[1]);
        Assert.Empty(Assert.IsType<PluginModelToolResult>(next[2]).Images);
        Assert.Equal(first.Text, Assert.IsType<PluginModelToolResult>(next[2]).Text);
        Assert.Equal(first.Success, Assert.IsType<PluginModelToolResult>(next[2]).Success);
        Assert.Empty(Assert.IsType<PluginModelToolResult>(next[3]).Images);
        Assert.Same(quiet, next[4]);
        Assert.Same(third, next[5]);
        Assert.Same(fourth, next[6]);
        Assert.Same(fifth, next[7]);
        Assert.Single(Assert.IsType<PluginModelToolResult>(next[1]).Images);
        Assert.Single(user.Images);
    }

    [Fact]
    public async Task TransformModel_LeavesThreeOrFewerCaptures()
    {
        var request = Request([Observe("0", 1), Observe("1", 2), Observe("2", 3)]);

        var next = await new ComputerObserveImages().TransformModelAsync(request);

        Assert.Null(next);
    }

    private static PluginModelRequest Request(IReadOnlyList<PluginModelItem> items) =>
        new(PluginModelPurpose.Work, items, acceptsImages: true);

    private static PluginModelMessage Message(string id, int imageNumber) =>
        new(id, ChatRole.User, $"see [Image #{imageNumber}]", [Image(imageNumber)]);

    private static PluginModelToolResult Result(string id, string name, int imageNumber) =>
        new(id, "call-" + id, name, "text", true, [Image(imageNumber)]);

    private static PluginModelToolResult Observe(string id, int imageNumber) =>
        new(
            id,
            "call-" + id,
            ComputerObserveImages.ToolName,
            $"VM display 0: 1280x800. [Image #{imageNumber}]",
            true,
            [Image(imageNumber)]);

    private static PluginModelToolResult ObserveWithoutImage(string id) =>
        new(id, "call-" + id, ComputerObserveImages.ToolName, "The display is off.", true);

    private static PluginModelImage Image(int number) =>
        new(number, "image/png");
}
