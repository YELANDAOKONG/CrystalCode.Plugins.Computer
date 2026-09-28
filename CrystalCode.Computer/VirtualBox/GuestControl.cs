using System.Globalization;

namespace CrystalCode.Computer.VirtualBox;

internal static class GuestControl
{
    public static IReadOnlyList<string> Arguments(
        string vmUuid,
        GuestCommand command,
        string username,
        string passwordFile)
    {
        var arguments = new List<string>
        {
            "guestcontrol",
            vmUuid,
            "run",
            "--exe",
            command.Executable,
            "--username",
            username,
            "--passwordfile",
            passwordFile,
            "--timeout",
            (command.TimeoutSeconds * 1000).ToString(CultureInfo.InvariantCulture),
            "--wait-stdout",
            "--wait-stderr",
            "--quiet"
        };
        if (!string.IsNullOrEmpty(command.Directory))
        {
            arguments.Add("--cwd");
            arguments.Add(command.Directory);
        }

        arguments.Add("--");
        arguments.AddRange(command.Arguments);
        return arguments;
    }
}
