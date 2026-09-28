using System.Globalization;
using System.Text;

namespace CrystalCode.Computer;

internal sealed record GuestProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool Truncated)
{
    public const int MaximumCharacters = 100_000;

    public string Format()
    {
        var builder = new StringBuilder();
        builder.Append("Exit code: ");
        builder.Append(ExitCode.ToString(CultureInfo.InvariantCulture));
        if (StandardOutput.Length > 0)
        {
            builder.Append('\n');
            builder.Append(StandardOutput);
        }

        if (StandardError.Length > 0)
        {
            builder.Append('\n');
            builder.Append(StandardError);
        }

        var text = builder.ToString();
        if (!Truncated && text.Length <= MaximumCharacters)
        {
            return text;
        }

        if (text.Length > MaximumCharacters)
        {
            text = text[..MaximumCharacters];
        }

        return text + "\n[output truncated]";
    }
}
