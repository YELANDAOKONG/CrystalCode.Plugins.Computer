using System.Text.Json;

using Crystal.Media;
using Crystal.Multimodal;
using Crystal.Multimodal.Tools;
using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerObserveTool : ITool, IMultimodalTool
{
    private const int MaximumDisplay = 7;

    private static readonly ToolDefinition Tool = new(
        "computer_observe",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"display\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":7}},\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Capture one display of the configured VirtualBox VM. Pointer coordinates are pixels from the top left of this image. Requires image input support.");

    public ToolDefinition Definition => Tool;

    public ValueTask<ToolOutput> InvokeAsync(
        ToolCall call,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryReadDisplay(call.Arguments, out _, out var error))
        {
            return ValueTask.FromResult(new ToolOutput(error!, ToolResultStatus.Failure));
        }

        return ValueTask.FromResult(
            new ToolOutput(
                "This model cannot accept tool images.",
                ToolResultStatus.Failure));
    }

    public async ValueTask<MultimodalToolOutput> InvokeAsync(
        MultimodalToolCall call,
        CancellationToken cancellationToken = default)
    {
        if (!TryReadDisplay(call.Arguments, out var display, out var error))
        {
            return Failure(error!);
        }

        try
        {
            var adapter = ComputerAdapters.Open();
            var capture = await adapter.CaptureAsync(display, cancellationToken);
            var image = new ImageMedia(
                new InlineMediaSource(capture.Data),
                new MediaMimeType("image/png"),
                new PixelSize(capture.Width, capture.Height));
            return new MultimodalToolOutput(
            [
                new TextContent($"VM display {display}: {capture.Width}x{capture.Height}."),
                new ImageContent(image)
            ]);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failure("The VM screenshot could not be read.");
        }
    }

    private static bool TryReadDisplay(string arguments, out int display, out string? error)
    {
        display = 0;
        error = null;
        try
        {
            using var document = JsonDocument.Parse(arguments);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "Tool arguments must be a JSON object.";
                return false;
            }

            if (root.TryGetProperty("display", out var value)
                && (value.ValueKind != JsonValueKind.Number
                    || !value.TryGetInt32(out display)))
            {
                error = "Display must be an integer from 0 through 7.";
                return false;
            }
        }
        catch (JsonException)
        {
            error = "Tool arguments are not valid JSON.";
            return false;
        }

        if (display is < 0 or > MaximumDisplay)
        {
            error = "Display must be an integer from 0 through 7.";
            return false;
        }

        return true;
    }

    private static MultimodalToolOutput Failure(string message) =>
        new([new TextContent(message)], MultimodalToolResultStatus.Failure);
}
