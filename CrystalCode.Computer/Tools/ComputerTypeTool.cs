using System.Text.Json;

using Crystal.Tools;
using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerTypeTool : ITool
{
    private const int MaximumTextLength = 4096;
    private static readonly ToolDefinition Tool = new(
        "computer_type",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"text\":{\"type\":\"string\"}},\"required\":[\"text\"],\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Type text into the configured VirtualBox VM at its current focus.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        string? value;
        try
        {
            using var document = JsonDocument.Parse(call.Arguments);
            value = document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String
                ? text.GetString()
                : null;
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        if (string.IsNullOrEmpty(value) || value.Length > MaximumTextLength)
        {
            return Failure("Text must contain 1 to 4096 characters.");
        }

        try
        {
            var adapter = ComputerSettings.CreateAdapter();
            await adapter.TypeTextAsync(value, cancellationToken);
            return new ToolOutput("Text was sent to the configured VM.");
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
        catch (IOException)
        {
            return Failure("Text could not be sent to the configured VM.");
        }
    }

    private static ToolOutput Failure(string message) =>
        new(message, ToolResultStatus.Failure);
}
