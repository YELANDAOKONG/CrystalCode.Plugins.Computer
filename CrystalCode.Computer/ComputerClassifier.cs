using Crystal.Tools;

using CrystalCode.Plugins.Approvals;

namespace CrystalCode.Computer;

public sealed class ComputerClassifier : IPluginClassifier
{
    private static readonly HashSet<string> ReadTools = new(StringComparer.Ordinal)
    {
        "computer_status",
        "computer_observe"
    };

    private static readonly HashSet<string> ControlTools = new(StringComparer.Ordinal)
    {
        "computer_type",
        "computer_keys",
        "computer_click",
        "computer_drag",
        "computer_scroll",
        "computer_run"
    };

    public bool TryClassify(
        ToolCall call,
        string workspaceRoot,
        out PluginClassification? classification)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        classification = null;
        if (ReadTools.Contains(call.Name))
        {
            classification = new PluginClassification(
                PluginRisk.Read,
                PluginAuthority.Workspace,
                "Read the configured VM");
            return true;
        }

        if (ControlTools.Contains(call.Name))
        {
            classification = new PluginClassification(
                PluginRisk.Privileged,
                PluginAuthority.PrivilegedEscalation,
                "Control the configured VM");
            return true;
        }

        return false;
    }
}
