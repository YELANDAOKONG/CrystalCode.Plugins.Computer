using Crystal.Tools;

using CrystalCode.Plugins.Hooks;

namespace CrystalCode.Computer;

/// <summary>
/// Drops screenshots from this plugin's older <c>computer_observe</c> results.
/// The three newest captures keep their images. Other tools are unchanged.
/// </summary>
public sealed class ComputerObserveImages : IPluginHook
{
    public const int RetainedCaptures = 3;

    public const string ToolName = "computer_observe";

    private readonly Queue<string> _retainedCallIds = new();

    private readonly HashSet<string> _seenCallIds = new(StringComparer.Ordinal);

    private readonly object _gate = new();

    public ValueTask OnSessionStartedAsync(
        PluginSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _retainedCallIds.Clear();
            _seenCallIds.Clear();
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<PluginToolResult?> AfterToolAsync(
        ToolCall call,
        PluginToolResult result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(call.Name, ToolName, StringComparison.Ordinal)
            || result.Images.Count == 0)
        {
            return ValueTask.FromResult<PluginToolResult?>(null);
        }

        var retain = false;
        lock (_gate)
        {
            if (_seenCallIds.Add(call.CallId))
            {
                _retainedCallIds.Enqueue(call.CallId);
                while (_retainedCallIds.Count > RetainedCaptures)
                {
                    _retainedCallIds.Dequeue();
                }
            }

            retain = _retainedCallIds.Contains(call.CallId);
        }

        if (retain)
        {
            return ValueTask.FromResult<PluginToolResult?>(null);
        }

        return ValueTask.FromResult<PluginToolResult?>(
            new PluginToolResult(result.Text, result.Success));
    }
}
