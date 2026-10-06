using System.Text.Json;

using Crystal.Media;
using Crystal.Multimodal;
using Crystal.Multimodal.Tools;
using Crystal.Tools;

using CrystalCode.Computer.Configuration;

namespace CrystalCode.Computer.Tools;

public sealed class ComputerObserveTool : IMultimodalTool
{
    private static readonly ToolDefinition Tool = new(
        "computer_observe",
        JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"display\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":7}},\"additionalProperties\":false}")
            .RootElement.Clone(),
        "Capture one display of the configured VirtualBox VM. Pointer coordinates are pixels from the top left of this image. Requires image input support.");

    public ToolDefinition Definition => Tool;

    public async ValueTask<MultimodalToolOutput> InvokeAsync(
        MultimodalToolCall call,
        CancellationToken cancellationToken = default)
    {
        int display;
        try
        {
            using var document = JsonDocument.Parse(call.Arguments);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Failure("Tool arguments must be a JSON object.");
            }

            display = 0;
            if (root.TryGetProperty("display", out var value)
                && (value.ValueKind != JsonValueKind.Number
                    || !value.TryGetInt32(out display)))
            {
                return Failure("Display must be an integer from 0 through 7.");
            }
        }
        catch (JsonException)
        {
            return Failure("Tool arguments are not valid JSON.");
        }

        if (display is < 0 or > 7)
        {
            return Failure("Display must be an integer from 0 through 7.");
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

    private static MultimodalToolOutput Failure(string message) =>
        new([new TextContent(message)], MultimodalToolResultStatus.Failure);
}
