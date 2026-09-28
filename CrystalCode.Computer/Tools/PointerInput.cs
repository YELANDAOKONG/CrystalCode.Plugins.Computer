using System.Text.Json;

namespace CrystalCode.Computer.Tools;

internal static class PointerInput
{
    public const int MaximumDisplay = 7;
    public const int MaximumClicks = 2;
    public const int MaximumWheelMoves = 20;

    public static bool TryReadDisplay(JsonElement root, out int display, out string? error)
    {
        display = 0;
        error = null;
        if (!root.TryGetProperty("display", out var value))
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out display)
            || display is < 0 or > MaximumDisplay)
        {
            error = "Display must be an integer from 0 through 7.";
            return false;
        }

        return true;
    }

    public static bool TryReadPoint(
        JsonElement root,
        string xName,
        string yName,
        out int x,
        out int y,
        out string? error)
    {
        x = 0;
        y = 0;
        error = null;
        if (!root.TryGetProperty(xName, out var xValue)
            || !root.TryGetProperty(yName, out var yValue)
            || xValue.ValueKind != JsonValueKind.Number
            || yValue.ValueKind != JsonValueKind.Number
            || !xValue.TryGetInt32(out x)
            || !yValue.TryGetInt32(out y)
            || x < 0
            || y < 0)
        {
            error = "Pointer coordinates must be non-negative integers.";
            return false;
        }

        return true;
    }

    public static bool TryReadButton(JsonElement root, out PointerButton button, out string? error)
    {
        button = PointerButton.Left;
        error = null;
        if (!root.TryGetProperty("button", out var value))
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            error = "Button must be left, right, or middle.";
            return false;
        }

        switch (value.GetString())
        {
            case "left":
                button = PointerButton.Left;
                return true;
            case "right":
                button = PointerButton.Right;
                return true;
            case "middle":
                button = PointerButton.Middle;
                return true;
            default:
                error = "Button must be left, right, or middle.";
                return false;
        }
    }
}
