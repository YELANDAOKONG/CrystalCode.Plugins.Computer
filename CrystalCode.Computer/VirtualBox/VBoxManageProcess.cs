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
                RedirectStandardInput = true,
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

        Task<StreamCapture>? stdoutTask = null;
        Task<StreamCapture>? stderrTask = null;
        try
        {
            // The child must not read the host's stdin, so give it an empty one.
            process.StandardInput.Close();
            stdoutTask = ReadStreamAsync(process.StandardOutput, cancellationToken);
            stderrTask = ReadStreamAsync(process.StandardError, cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessResult(
                process.ExitCode,
                stdout.Text,
                stderr.Text,
                stdout.Truncated || stderr.Truncated);
        }
        catch
        {
            // Cancellation or a stream failure must not leave the child running.
            TryKill(process);
            await DrainAsync(stdoutTask, stderrTask).ConfigureAwait(false);
            throw;
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the check and the kill.
        }
    }

    private static async Task DrainAsync(params Task?[] tasks)
    {
        // Observe the stream readers before the process is disposed so a
        // cancelled read cannot surface later as an unobserved exception.
        foreach (var task in tasks)
        {
            if (task is null)
            {
                continue;
            }

            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The readers were cancelled alongside the process.
            }
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
