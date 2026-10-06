using CrystalCode.Plugins;

using CrystalCode.Computer.Tools;

namespace CrystalCode.Computer;

public sealed class ComputerPlugin : IPlugin
{
    public string Name => "Computer";

    public PluginContribution Contribute()
    {
        var observe = new ComputerObserveTool();
        return new PluginContribution(
            tools:
            [
                new ComputerPluginTool(new ComputerStatusTool()),
                new ComputerPluginTool(observe, observe),
                new ComputerPluginTool(new ComputerTypeTool()),
                new ComputerPluginTool(new ComputerKeysTool()),
                new ComputerPluginTool(new ComputerClickTool()),
                new ComputerPluginTool(new ComputerDragTool()),
                new ComputerPluginTool(new ComputerScrollTool()),
                new ComputerPluginTool(new ComputerRunTool())
            ],
            classifiers: [new ComputerClassifier()],
            hooks: [new ComputerObserveImages()]);
    }
}
