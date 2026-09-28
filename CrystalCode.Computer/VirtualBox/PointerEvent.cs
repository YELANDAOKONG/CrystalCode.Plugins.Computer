namespace CrystalCode.Computer.VirtualBox;

internal sealed record PointerEvent(
    int X,
    int Y,
    int VerticalWheel,
    int HorizontalWheel,
    int Buttons,
    int DelayMilliseconds);
