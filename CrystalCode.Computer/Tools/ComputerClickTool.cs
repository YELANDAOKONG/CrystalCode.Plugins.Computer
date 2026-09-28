using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerClickTool : ITool
{
    private static readonly ToolDefinition Tool = new(
        "computer_click",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"display\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":7},\"x\":{\"type\":\"integer\",\"minimum\":0},\"y\":{\"type\":\"integer\",\"minimum\":0},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"clicks\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":2}},\"required\":[\"x\",\"y\"],\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Click at pixel coordinates on a display of the configured VM. The origin is the top left of the computer_observe image. The VM must use an absolute pointer, such as a USB Tablet.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        int display;
        int x;
        int y;
        PointerButton button;
        int clicks;
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

            if (!PointerInput.TryReadPoint(root, "x", "y", out x, out y, out var pointError))
            {
                return Failure(pointError!);
            }

            if (!PointerInput.TryReadButton(root, out button, out var buttonError))
            {
                return Failure(buttonError!);
            }

            clicks = 1;
            if (root.TryGetProperty("clicks", out var clickValue)
                && (!clickValue.TryGetInt32(out clicks)
                    || clicks is < 1 or > PointerInput.MaximumClicks))
            {
                return Failure("Clicks must be 1 or 2.");
            }
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        try
        {
            var adapter = ComputerAdapters.Open();
            await adapter.ClickAsync(display, x, y, button, clicks, cancellationToken);
            return new ToolOutput("The pointer click was sent to the configured VM.");
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static ToolOutput Failure(string message) =>
        new(message, ToolResultStatus.Failure);
}
