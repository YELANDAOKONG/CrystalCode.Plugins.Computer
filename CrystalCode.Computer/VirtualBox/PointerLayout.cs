using System.Text.Json;

namespace CrystalCode.Computer.VirtualBox;

internal sealed record PointerLayout(bool Absolute, IReadOnlyList<GuestScreen> Screens)
{
    public const int MaximumPixel = 100_000;

    public static PointerLayout Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("absolute", out var absolute)
                || absolute.ValueKind is not JsonValueKind.True and not JsonValueKind.False
                || !root.TryGetProperty("screens", out var screens)
                || screens.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("The pointer layout could not be read.");
            }

            var parsed = new List<GuestScreen>();
            foreach (var screen in screens.EnumerateArray())
            {
                parsed.Add(ReadScreen(screen));
            }

            return new PointerLayout(absolute.GetBoolean(), parsed);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The pointer layout could not be read.", exception);
        }
    }

    public (int X, int Y) Locate(int display, int x, int y)
    {
        if (!Absolute)
        {
            throw new InvalidOperationException(
                "The guest does not support an absolute pointer. Use a USB Tablet mouse.");
        }

        if (x is < 0 or > MaximumPixel || y is < 0 or > MaximumPixel)
        {
            throw new InvalidOperationException("The pointer position is outside the display.");
        }

        GuestScreen? match = null;
        foreach (var screen in Screens)
        {
            if (screen.Display == display)
            {
                match = screen;
                break;
            }
        }

        if (match is null)
        {
            throw new InvalidOperationException("The display is not available.");
        }

        if (!match.Active || x >= match.Width || y >= match.Height)
        {
            throw new InvalidOperationException(
                match.Active
                    ? "The pointer position is outside the display."
                    : "The display is not active.");
        }

        var absoluteX = (long)match.X + x + 1;
        var absoluteY = (long)match.Y + y + 1;
        if (absoluteX is < int.MinValue or > int.MaxValue
            || absoluteY is < int.MinValue or > int.MaxValue
            || (absoluteX == -1 && absoluteY == -1)
            || absoluteX == int.MaxValue
            || absoluteY == int.MaxValue)
        {
            throw new InvalidOperationException("The pointer position is outside the virtual display.");
        }

        return ((int)absoluteX, (int)absoluteY);
    }

    private static GuestScreen ReadScreen(JsonElement screen)
    {
        if (screen.ValueKind != JsonValueKind.Object
            || !TryReadInt(screen, "display", out var display)
            || !TryReadInt(screen, "width", out var width)
            || !TryReadInt(screen, "height", out var height)
            || !TryReadInt(screen, "x", out var x)
            || !TryReadInt(screen, "y", out var y)
            || !screen.TryGetProperty("active", out var active)
            || active.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new InvalidOperationException("The pointer layout could not be read.");
        }

        return new GuestScreen(display, width, height, x, y, active.GetBoolean());
    }

    private static bool TryReadInt(JsonElement element, string name, out int value)
    {
        value = 0;
        return element.TryGetProperty(name, out var property)
            && property.TryGetInt32(out value);
    }
}
