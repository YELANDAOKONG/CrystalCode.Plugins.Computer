using System.Globalization;
using System.Text;

namespace CrystalCode.Computer.VirtualBox;

internal static class MachineInfo
{
    public static ComputerStatus Read(string output, string expectedUuid)
    {
        var expected = $"UUID=\"{expectedUuid}\"";
        if (!output.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("VirtualBox returned a different VM UUID.");
        }

        string? vmState = null;
        int? runLevel = null;
        string? version = null;
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("VMState=", StringComparison.Ordinal))
            {
                vmState = line["VMState=".Length..].Trim().Trim('"');
            }
            else if (line.StartsWith("GuestAdditionsRunLevel=", StringComparison.Ordinal)
                && int.TryParse(
                    line["GuestAdditionsRunLevel=".Length..].Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var level))
            {
                runLevel = level;
            }
            else if (line.StartsWith("GuestAdditionsVersion=", StringComparison.Ordinal))
            {
                version = CleanVersion(ReadQuoted(line["GuestAdditionsVersion=".Length..]));
            }
        }

        if (string.IsNullOrEmpty(vmState))
        {
            throw new InvalidOperationException("VirtualBox did not report the VM state.");
        }

        var running = string.Equals(vmState, "running", StringComparison.OrdinalIgnoreCase);
        var additions = Classify(running, runLevel, version);
        return new ComputerStatus(vmState, additions);
    }

    private static GuestAdditionsReport Classify(bool running, int? runLevel, string? version)
    {
        if (runLevel is null)
        {
            return running
                ? new GuestAdditionsReport(GuestAdditionsState.NotLoaded, null)
                : new GuestAdditionsReport(GuestAdditionsState.Unavailable, null);
        }

        if (runLevel.Value >= 2)
        {
            return new GuestAdditionsReport(GuestAdditionsState.Active, version);
        }

        if (runLevel.Value == 1)
        {
            return new GuestAdditionsReport(GuestAdditionsState.DriversLoaded, version);
        }

        return new GuestAdditionsReport(GuestAdditionsState.NotLoaded, version);
    }

    private static string? CleanVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            return null;
        }

        foreach (var character in value)
        {
            if (character < ' ' || character > '~')
            {
                return null;
            }
        }

        return value;
    }

    private static string? ReadQuoted(string value)
    {
        if (value.Length < 2 || value[0] != '"')
        {
            return null;
        }

        var builder = new StringBuilder();
        for (var index = 1; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '\\' && index + 1 < value.Length)
            {
                builder.Append(value[index + 1]);
                index++;
                continue;
            }

            if (character == '"')
            {
                return builder.ToString();
            }

            builder.Append(character);
        }

        return null;
    }
}
