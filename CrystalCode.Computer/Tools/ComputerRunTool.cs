using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerRunTool : ITool
{
    public const int MaximumExecutableLength = 1024;
    public const int MaximumArgumentCount = 32;
    public const int MaximumArgumentLength = 1024;
    public const int DefaultTimeoutSeconds = 30;
    public const int MaximumTimeoutSeconds = 60;

    private static readonly ToolDefinition Tool = new(
        "computer_run",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"executable\":{\"type\":\"string\"},\"arguments\":{\"type\":\"array\",\"items\":{\"type\":\"string\"},\"maxItems\":32},\"directory\":{\"type\":\"string\"},\"timeoutSeconds\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":60}},\"required\":[\"executable\"],\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Run one program inside the configured VM. Requires active Guest Additions. The guest username and password file come from the process environment, not from the model.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        GuestCommand command;
        try
        {
            using var document = JsonDocument.Parse(call.Arguments);
            if (!TryRead(document.RootElement, out var parsed, out var error))
            {
                return Failure(error ?? "Tool arguments must be a JSON object.");
            }

            command = parsed;
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        try
        {
            var adapter = ComputerAdapters.Open();
            var result = await adapter.RunAsync(command, cancellationToken);
            return new ToolOutput(result.Format());
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static bool TryRead(
        JsonElement root,
        [NotNullWhen(true)] out GuestCommand? command,
        out string? error)
    {
        command = null;
        error = null;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("executable", out var executableValue)
            || executableValue.ValueKind != JsonValueKind.String)
        {
            error = "Executable must be a guest path.";
            return false;
        }

        var executable = executableValue.GetString() ?? "";
        if (!IsGuestText(executable, MaximumExecutableLength))
        {
            error = "Executable must be a guest path.";
            return false;
        }

        var arguments = new List<string>();
        if (root.TryGetProperty("arguments", out var argumentValues))
        {
            if (argumentValues.ValueKind != JsonValueKind.Array
                || argumentValues.GetArrayLength() > MaximumArgumentCount)
            {
                error = "Arguments must be an array of at most 32 strings.";
                return false;
            }

            foreach (var argument in argumentValues.EnumerateArray())
            {
                if (argument.ValueKind != JsonValueKind.String)
                {
                    error = "Arguments must be an array of at most 32 strings.";
                    return false;
                }

                var text = argument.GetString() ?? "";
                if (text.Length > MaximumArgumentLength || HasLineBreak(text))
                {
                    error = "Arguments must be an array of at most 32 strings.";
                    return false;
                }

                arguments.Add(text);
            }
        }

        string? directory = null;
        if (root.TryGetProperty("directory", out var directoryValue))
        {
            if (directoryValue.ValueKind != JsonValueKind.String
                || !IsGuestText(directoryValue.GetString() ?? "", MaximumExecutableLength))
            {
                error = "Directory must be a guest path.";
                return false;
            }

            directory = directoryValue.GetString();
        }

        var timeout = DefaultTimeoutSeconds;
        if (root.TryGetProperty("timeoutSeconds", out var timeoutValue)
            && (timeoutValue.ValueKind != JsonValueKind.Number
                || !timeoutValue.TryGetInt32(out timeout)
                || timeout is < 1 or > MaximumTimeoutSeconds))
        {
            error = "Timeout must be an integer from 1 through 60 seconds.";
            return false;
        }

        command = new GuestCommand(executable, arguments, directory, timeout);
        return true;
    }

    private static bool IsGuestText(string value, int maximumLength)
    {
        if (value.Length is 0 || value.Length > maximumLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is '\0' or '\r' or '\n')
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasLineBreak(string value)
    {
        foreach (var character in value)
        {
            if (character is '\0' or '\r' or '\n')
            {
                return true;
            }
        }

        return false;
    }

    private static ToolOutput Failure(string message) =>
        new(message, ToolResultStatus.Failure);
}
