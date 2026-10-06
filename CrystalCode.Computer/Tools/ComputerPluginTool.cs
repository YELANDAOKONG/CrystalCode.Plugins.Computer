using Crystal.Multimodal.Tools;
using Crystal.Tools;

using CrystalCode.Plugins.Tools;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerPluginTool : IPluginTool
{
    public ComputerPluginTool(ITool tool, IMultimodalTool? multimodal = null)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (multimodal is not null
            && !string.Equals(
                multimodal.Definition.Name,
                tool.Definition.Name,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The multimodal tool name must match the text tool.",
                nameof(multimodal));
        }

        Name = tool.Definition.Name;
        Tool = tool;
        Multimodal = multimodal;
    }

    public string Name { get; }

    public PluginToolCatalogs Catalogs => PluginToolCatalogs.Work;

    public ITool Tool { get; }

    public IMultimodalTool? Multimodal { get; }
}
