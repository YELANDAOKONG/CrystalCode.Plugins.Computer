namespace CrystalCode.Computer.VirtualBox;

internal static class VirtualBoxHost
{
    public static string PythonExecutable()
    {
        var configured = Environment.GetEnvironmentVariable("CRYSTAL_COMPUTER_PYTHON");
        return string.IsNullOrWhiteSpace(configured) ? "python3" : configured;
    }

    public static IReadOnlyDictionary<string, string> PointerEnvironment(string vboxManage)
    {
        var environment = new Dictionary<string, string>(StringComparer.Ordinal);
        var home = InstallationDirectory(vboxManage);
        if (home is null)
        {
            return environment;
        }

        environment["VBOX_INSTALL_PATH"] = home;
        environment["VBOX_PROGRAM_PATH"] = home;
        var binding = OperatingSystem.IsWindows()
            ? Path.Combine(home, "sdk", "bindings", "mscom", "python")
            : Path.Combine(home, "sdk", "bindings", "xpcom", "python");
        if (Directory.Exists(binding))
        {
            var existing = Environment.GetEnvironmentVariable("PYTHONPATH");
            environment["PYTHONPATH"] = string.IsNullOrEmpty(existing)
                ? binding
                : binding + Path.PathSeparator + existing;
        }

        return environment;
    }

    private static string? InstallationDirectory(string vboxManage)
    {
        var configured = Environment.GetEnvironmentVariable("CRYSTAL_COMPUTER_VBOX_HOME");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (Path.IsPathRooted(vboxManage))
        {
            var directory = Path.GetDirectoryName(vboxManage);
            if (!string.IsNullOrEmpty(directory))
            {
                return directory;
            }
        }

        if (OperatingSystem.IsMacOS())
        {
            const string MacInstallation = "/Applications/VirtualBox.app/Contents/MacOS";
            if (Directory.Exists(MacInstallation))
            {
                return MacInstallation;
            }
        }

        if (OperatingSystem.IsLinux())
        {
            const string LinuxInstallation = "/usr/lib/virtualbox";
            if (Directory.Exists(LinuxInstallation))
            {
                return LinuxInstallation;
            }
        }

        if (OperatingSystem.IsWindows())
        {
            var windows = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Oracle",
                "VirtualBox");
            if (Directory.Exists(windows))
            {
                return windows;
            }
        }

        return null;
    }
}
