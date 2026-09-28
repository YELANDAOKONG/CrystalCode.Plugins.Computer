namespace CrystalCode.Computer.VirtualBox;

internal static class PointerCommands
{
    private const int PressMilliseconds = 40;
    private const int DoubleClickGapMilliseconds = 120;

    public static IReadOnlyList<PointerEvent> Click(int x, int y, int buttons, int clicks)
    {
        var events = new List<PointerEvent>
        {
            new(x, y, 0, 0, 0, 0)
        };
        for (var index = 0; index < clicks; index++)
        {
            var gap = index == 0 ? PressMilliseconds : DoubleClickGapMilliseconds;
            events.Add(new PointerEvent(x, y, 0, 0, buttons, gap));
            events.Add(new PointerEvent(x, y, 0, 0, 0, PressMilliseconds));
        }

        return events;
    }

    public static IReadOnlyList<PointerEvent> Drag(
        int fromX,
        int fromY,
        int toX,
        int toY,
        int buttons)
    {
        return
        [
            new PointerEvent(fromX, fromY, 0, 0, 0, 0),
            new PointerEvent(fromX, fromY, 0, 0, buttons, PressMilliseconds),
            new PointerEvent(toX, toY, 0, 0, buttons, PressMilliseconds),
            new PointerEvent(toX, toY, 0, 0, 0, PressMilliseconds)
        ];
    }

    public static IReadOnlyList<PointerEvent> Scroll(
        int x,
        int y,
        ScrollDirection direction,
        int amount)
    {
        var vertical = direction switch
        {
            ScrollDirection.Up => -amount,
            ScrollDirection.Down => amount,
            ScrollDirection.Left => 0,
            ScrollDirection.Right => 0,
            _ => throw new InvalidOperationException("The scroll direction is not supported.")
        };
        var horizontal = direction switch
        {
            ScrollDirection.Left => amount,
            ScrollDirection.Right => -amount,
            ScrollDirection.Up => 0,
            ScrollDirection.Down => 0,
            _ => throw new InvalidOperationException("The scroll direction is not supported.")
        };
        return
        [
            new PointerEvent(x, y, 0, 0, 0, 0),
            new PointerEvent(x, y, vertical, horizontal, 0, PressMilliseconds)
        ];
    }
}
