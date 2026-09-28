using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using CrystalCode.Computer.Interfaces;

namespace CrystalCode.Computer.VirtualBox;

internal sealed class VBoxManageProcess : IProcessRunner
{
    private const int OutputCharacterBudget = GuestProcessResult.MaximumCharacters;

    public Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        return RunCoreAsync(executable, arguments, environment: null, cancellationToken);
    }

    public Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken)
    {
        return RunCoreAsync(executable, arguments, environment, cancellationToken);
    }

    private async Task<ProcessResult> RunCoreAsync(
        string executable,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string>? environment,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                process.StartInfo.Environment[pair.Key] = pair.Value;
            }
        }

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new ProcessStartException(exception);
        }

        try
        {
            var stdoutTask = ReadStreamAsync(process.StandardOutput, cancellationToken);
            var stderrTask = ReadStreamAsync(process.StandardError, cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessResult(
                process.ExitCode,
                stdout.Text,
                stderr.Text,
                stdout.Truncated || stderr.Truncated);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }
    }

    private static async Task<StreamCapture> ReadStreamAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var retained = new StringBuilder();
        var buffer = new char[4096];
        var truncated = false;
        while (true)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            if (count == 0)
            {
                return new StreamCapture(retained.ToString(), truncated);
            }

            var remaining = OutputCharacterBudget - retained.Length;
            if (remaining > 0)
            {
                var take = Math.Min(count, remaining);
                retained.Append(buffer, 0, take);
                if (take < count)
                {
                    truncated = true;
                }
            }
            else
            {
                truncated = true;
            }
        }
    }

    private sealed record StreamCapture(string Text, bool Truncated);
}
