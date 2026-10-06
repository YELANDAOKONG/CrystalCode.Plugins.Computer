using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerScrollTool : ITool
{
    private static readonly ToolDefinition Tool = new(
        "computer_scroll",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"display\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":7},\"x\":{\"type\":\"integer\",\"minimum\":0},\"y\":{\"type\":\"integer\",\"minimum\":0},\"direction\":{\"type\":\"string\",\"enum\":[\"up\",\"down\",\"left\",\"right\"]},\"amount\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":20}},\"required\":[\"x\",\"y\",\"direction\"],\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Scroll the pointer wheel at pixel coordinates on a display of the configured VM. Coordinates use the same origin as computer_observe.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        int display;
        int x;
        int y;
        ScrollDirection direction;
        int amount;
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

            if (!TryReadDirection(root, out direction, out var directionError))
            {
                return Failure(directionError!);
            }

            amount = 1;
            if (root.TryGetProperty("amount", out var amountValue)
                && (amountValue.ValueKind != JsonValueKind.Number
                    || !amountValue.TryGetInt32(out amount)
                    || amount is < 1 or > PointerInput.MaximumWheelMoves))
            {
                return Failure("Scroll amount must be an integer from 1 through 20.");
            }
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        try
        {
            var adapter = ComputerAdapters.Open();
            await adapter.ScrollAsync(display, x, y, direction, amount, cancellationToken);
            return new ToolOutput("The pointer wheel was sent to the configured VM.");
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static bool TryReadDirection(
        JsonElement root,
        out ScrollDirection direction,
        out string? error)
    {
        direction = ScrollDirection.Down;
        error = null;
        if (!root.TryGetProperty("direction", out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            error = "Direction must be up, down, left, or right.";
            return false;
        }

        switch (value.GetString())
        {
            case "up":
                direction = ScrollDirection.Up;
                return true;
            case "down":
                direction = ScrollDirection.Down;
                return true;
            case "left":
                direction = ScrollDirection.Left;
                return true;
            case "right":
                direction = ScrollDirection.Right;
                return true;
            default:
                error = "Direction must be up, down, left, or right.";
                return false;
        }
    }

    private static ToolOutput Failure(string message) =>
        new(message, ToolResultStatus.Failure);
}
