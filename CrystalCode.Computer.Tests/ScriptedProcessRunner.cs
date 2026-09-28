using CrystalCode.Computer.Interfaces;

namespace CrystalCode.Computer.Tests;

internal sealed class ScriptedProcessRunner : IProcessRunner
{
    public List<IReadOnlyList<string>> Arguments { get; } = [];

    public Func<IReadOnlyList<string>, ProcessResult> Handle { get; set; } =
        _ => new ProcessResult(0, "", "", false);

    public Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        return RunAsync(
            executable,
            arguments,
            new Dictionary<string, string>(),
            cancellationToken);
    }

    public Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken)
    {
        Arguments.Add(arguments);
        return Task.FromResult(Handle(arguments));
    }
}
