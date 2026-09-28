namespace CrystalCode.Computer.VirtualBox;

internal static class GuestSession
{
    public const string UsernameVariable = "CRYSTAL_COMPUTER_GUEST_USERNAME";
    public const string PasswordFileVariable = "CRYSTAL_COMPUTER_GUEST_PASSWORD_FILE";

    public static (string Username, string PasswordFile) Require()
    {
        return Require(
            Environment.GetEnvironmentVariable(UsernameVariable),
            Environment.GetEnvironmentVariable(PasswordFileVariable));
    }

    public static (string Username, string PasswordFile) Require(
        string? username,
        string? passwordFile)
    {
        if (string.IsNullOrWhiteSpace(username)
            || username.Length > 256
            || username.Contains('\0')
            || username.Contains('\r')
            || username.Contains('\n'))
        {
            throw new InvalidOperationException(
                "Set CRYSTAL_COMPUTER_GUEST_USERNAME to run a guest process.");
        }

        if (string.IsNullOrWhiteSpace(passwordFile) || !File.Exists(passwordFile))
        {
            throw new InvalidOperationException(
                "Set CRYSTAL_COMPUTER_GUEST_PASSWORD_FILE to an existing password file.");
        }

        return (username, passwordFile);
    }
}
