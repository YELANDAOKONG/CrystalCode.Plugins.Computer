using CrystalCode.Plugins.Hooks;

namespace CrystalCode.Computer;

/// <summary>
/// Omits this plugin's older screenshots from one outbound model call.
/// The session keeps every capture. Only <c>computer_observe</c> images are removed.
/// </summary>
public sealed class ComputerObserveImages : IPluginHook
{
    public const int RetainedCaptures = 3;

    public const string ToolName = "computer_observe";

    public ValueTask<IReadOnlyList<PluginModelItem>?> TransformModelAsync(
        PluginModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var retained = RetainedIndexes(request.Items);
        if (retained is null)
        {
            return ValueTask.FromResult<IReadOnlyList<PluginModelItem>?>(null);
        }

        var items = new PluginModelItem[request.Items.Count];
        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            items[index] = item is PluginModelToolResult result && DropsImages(result, index, retained)
                ? WithoutImages(result)
                : item;
        }

        return ValueTask.FromResult<IReadOnlyList<PluginModelItem>?>(items);
    }

    private static HashSet<int>? RetainedIndexes(IReadOnlyList<PluginModelItem> items)
    {
        var captures = new List<int>();
        for (var index = 0; index < items.Count; index++)
        {
            if (items[index] is PluginModelToolResult result && IsCapture(result))
            {
                captures.Add(index);
            }
        }

        if (captures.Count <= RetainedCaptures)
        {
            return null;
        }

        return captures[^RetainedCaptures..].ToHashSet();
    }

    private static bool DropsImages(
        PluginModelToolResult result,
        int index,
        HashSet<int> retained) =>
        IsCapture(result) && !retained.Contains(index);

    private static bool IsCapture(PluginModelToolResult result) =>
        string.Equals(result.Name, ToolName, StringComparison.Ordinal)
        && result.Images.Count > 0;

    private static PluginModelToolResult WithoutImages(PluginModelToolResult result) =>
        new(result.Id, result.CallId, result.Name, result.Text, result.Success);
}
