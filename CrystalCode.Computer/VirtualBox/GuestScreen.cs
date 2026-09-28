namespace CrystalCode.Computer.VirtualBox;

internal sealed record GuestScreen(
    int Display,
    int Width,
    int Height,
    int X,
    int Y,
    bool Active);
