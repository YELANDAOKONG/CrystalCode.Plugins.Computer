namespace CrystalCode.Computer.VirtualBox;

internal static class TypableText
{
    public static bool IsSupported(string text)
    {
        foreach (var character in text)
        {
            if (character is '\t' or '\n')
            {
                continue;
            }

            if (character < ' ' || character > '~')
            {
                return false;
            }
        }

        return true;
    }
}
