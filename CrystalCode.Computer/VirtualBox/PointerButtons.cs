namespace CrystalCode.Computer.VirtualBox;

internal static class PointerButtons
{
    public const int Left = 0x01;
    public const int Right = 0x02;
    public const int Middle = 0x04;

    public static int Bit(PointerButton button)
    {
        return button switch
        {
            PointerButton.Left => Left,
            PointerButton.Right => Right,
            PointerButton.Middle => Middle,
            _ => throw new InvalidOperationException("The pointer button is not supported.")
        };
    }
}
