using Crystal.Tools;

using CrystalCode.Plugins.Approvals;
using CrystalCode.Plugins.Tools;

using CrystalCode.Computer;

namespace CrystalCode.Computer.Tests;

public sealed class ComputerPluginTests
{
    [Fact]
    public void Contribute_RegistersTheWorkCatalog()
    {
        var contribution = new ComputerPlugin().Contribute();
        var names = contribution.Tools.Select(tool => tool.Name).ToArray();

        Assert.Equal(
            [
                "computer_status",
                "computer_observe",
                "computer_type",
                "computer_keys",
                "computer_click",
                "computer_drag",
                "computer_scroll",
                "computer_run"
            ],
            names);
        Assert.All(contribution.Tools, tool =>
        {
            Assert.True(tool.Catalogs.Contains(PluginToolCatalog.Work));
            Assert.False(tool.Catalogs.Contains(PluginToolCatalog.Plan));
            Assert.Equal(tool.Name, tool.Tool.Definition.Name);
        });

        var observe = Assert.Single(
            contribution.Tools,
            tool => tool.Name == "computer_observe");
        Assert.NotNull(observe.Multimodal);
        Assert.Equal("computer_observe", observe.Multimodal.Definition.Name);
        Assert.Equal("Computer", new ComputerPlugin().Name);
    }

    [Fact]
    public void Classifier_SeparatesObservationFromControl()
    {
        var classifier = Assert.Single(new ComputerPlugin().Contribute().Classifiers);
        Assert.True(classifier.TryClassify(
            new ToolCall("1", "computer_observe", "{}"),
            "/workspace",
            out var observed));
        Assert.Equal(PluginRisk.Read, observed!.Risk);
        Assert.Equal(PluginAuthority.Workspace, observed.Authority);

        Assert.True(classifier.TryClassify(
            new ToolCall("1", "computer_click", "{}"),
            "/workspace",
            out var clicked));
        Assert.Equal(PluginRisk.Privileged, clicked!.Risk);
        Assert.Equal(PluginAuthority.PrivilegedEscalation, clicked.Authority);

        Assert.False(classifier.TryClassify(
            new ToolCall("1", "read", "{}"),
            "/workspace",
            out _));
    }
}
