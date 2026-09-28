using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerStatusTool : ITool
{
    private static readonly ToolDefinition Tool = new(
        "computer_status",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{},\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Report the state of the configured VirtualBox VM and whether Guest Additions are loaded.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adapter = ComputerAdapters.Open();
            var state = await adapter.GetStatusAsync(cancellationToken);
            return new ToolOutput(state.Describe());
        }
        catch (InvalidOperationException exception)
        {
            return new ToolOutput(exception.Message, ToolResultStatus.Failure);
        }
    }
}
