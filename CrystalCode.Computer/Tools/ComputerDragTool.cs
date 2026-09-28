using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerDragTool : ITool
{
    private static readonly ToolDefinition Tool = new(
        "computer_drag",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"display\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":7},\"fromX\":{\"type\":\"integer\",\"minimum\":0},\"fromY\":{\"type\":\"integer\",\"minimum\":0},\"toX\":{\"type\":\"integer\",\"minimum\":0},\"toY\":{\"type\":\"integer\",\"minimum\":0},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]}},\"required\":[\"fromX\",\"fromY\",\"toX\",\"toY\"],\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Drag from one pixel to another on a display of the configured VM. Coordinates use the same origin as computer_observe.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        int display;
        int fromX;
        int fromY;
        int toX;
        int toY;
        PointerButton button;
        try
        {
            using var document = JsonDocument.Parse(call.Arguments);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Failure("Tool arguments must be a JSON object.");
            }

            if (!PointerInput.TryReadDisplay(root, out display, out var displayError))
            {
                return Failure(displayError!);
            }

            if (!PointerInput.TryReadPoint(
                    root, "fromX", "fromY", out fromX, out fromY, out var fromError))
            {
                return Failure(fromError!);
            }

            if (!PointerInput.TryReadPoint(root, "toX", "toY", out toX, out toY, out var toError))
            {
                return Failure(toError!);
            }

            if (!PointerInput.TryReadButton(root, out button, out var buttonError))
            {
                return Failure(buttonError!);
            }
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        try
        {
            var adapter = ComputerAdapters.Open();
            await adapter.DragAsync(
                display, fromX, fromY, toX, toY, button, cancellationToken);
            return new ToolOutput("The pointer drag was sent to the configured VM.");
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static ToolOutput Failure(string message) =>
        new(message, ToolResultStatus.Failure);
}
