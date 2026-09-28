using System.Text.Json;

using Crystal.Tools;

using Crystal.Computer.Configuration;

namespace Crystal.Computer.Tools;

public sealed class ComputerKeysTool : ITool
{
    private static readonly ToolDefinition Tool = new(
        "computer_keys",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"keys\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"minItems\":1,\"maxItems\":4}},\"required\":[\"keys\"],\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Press a key or shortcut in the configured VM. Keys are ordered, for example [ctrl, c]. Supported keys: letters, digits, enter, escape, tab, backspace, space, delete, arrows, home, end, ctrl, alt, shift, meta.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        string[] keys;
        try
        {
            using var document = JsonDocument.Parse(call.Arguments);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("keys", out var values)
                || values.ValueKind != JsonValueKind.Array
                || values.GetArrayLength() is < 1 or > 4
                || values.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
            {
                return Failure("Keys must be an array of 1 to 4 key names.");
            }

            keys = values.EnumerateArray().Select(item => item.GetString()!).ToArray();
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        try
        {
            var adapter = ComputerSettings.CreateAdapter();
            await adapter.PressKeysAsync(keys, cancellationToken);
            return new ToolOutput("Keys were sent to the configured VM.");
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static ToolOutput Failure(string message) =>
        new(message, ToolResultStatus.Failure);
}
