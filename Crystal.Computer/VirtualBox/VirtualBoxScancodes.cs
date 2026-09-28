namespace Crystal.Computer.VirtualBox;

internal static class VirtualBoxScancodes
{
    private static readonly IReadOnlyDictionary<string, byte[]> Codes =
        new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = [0x1E], ["b"] = [0x30], ["c"] = [0x2E],
            ["d"] = [0x20], ["e"] = [0x12], ["f"] = [0x21],
            ["g"] = [0x22], ["h"] = [0x23], ["i"] = [0x17],
            ["j"] = [0x24], ["k"] = [0x25], ["l"] = [0x26],
            ["m"] = [0x32], ["n"] = [0x31], ["o"] = [0x18],
            ["p"] = [0x19], ["q"] = [0x10], ["r"] = [0x13],
            ["s"] = [0x1F], ["t"] = [0x14], ["u"] = [0x16],
            ["v"] = [0x2F], ["w"] = [0x11], ["x"] = [0x2D],
            ["y"] = [0x15], ["z"] = [0x2C],
            ["0"] = [0x0B], ["1"] = [0x02], ["2"] = [0x03],
            ["3"] = [0x04], ["4"] = [0x05], ["5"] = [0x06],
            ["6"] = [0x07], ["7"] = [0x08], ["8"] = [0x09],
            ["9"] = [0x0A],
            ["enter"] = [0x1C], ["escape"] = [0x01],
            ["tab"] = [0x0F], ["backspace"] = [0x0E],
            ["space"] = [0x39], ["delete"] = [0xE0, 0x53],
            ["up"] = [0xE0, 0x48], ["down"] = [0xE0, 0x50],
            ["left"] = [0xE0, 0x4B], ["right"] = [0xE0, 0x4D],
            ["home"] = [0xE0, 0x47], ["end"] = [0xE0, 0x4F],
            ["ctrl"] = [0x1D], ["alt"] = [0x38],
            ["shift"] = [0x2A], ["meta"] = [0xE0, 0x5B]
        };

    public static bool TryBuild(IReadOnlyList<string> keys, out IReadOnlyList<string> bytes)
    {
        bytes = [];
        if (keys.Count is < 1 or > 4)
        {
            return false;
        }

        var sequence = new List<byte>();
        var pressed = new List<byte[]>();
        foreach (var key in keys)
        {
            if (!Codes.TryGetValue(key, out var code)
                || pressed.Any(existing => existing.AsSpan().SequenceEqual(code)))
            {
                return false;
            }

            pressed.Add(code);
            sequence.AddRange(code);
        }

        for (var index = pressed.Count - 1; index >= 0; index--)
        {
            var code = pressed[index];
            if (code.Length == 2)
            {
                sequence.Add(code[0]);
            }

            sequence.Add((byte)(code[^1] | 0x80));
        }

        bytes = sequence.Select(value => value.ToString("x2")).ToArray();
        return true;
    }
}
