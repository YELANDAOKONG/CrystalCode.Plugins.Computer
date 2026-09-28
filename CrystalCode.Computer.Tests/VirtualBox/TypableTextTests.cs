using CrystalCode.Computer.VirtualBox;

namespace CrystalCode.Computer.Tests.VirtualBox;

public sealed class TypableTextTests
{
    [Theory]
    [InlineData("Hello")]
    [InlineData("line\nnext")]
    [InlineData("col\tvalue")]
    [InlineData(" ~!@#$%^&*()_+")]
    public void IsSupported_AcceptsUsKeyboardText(string text)
    {
        Assert.True(TypableText.IsSupported(text));
    }

    [Theory]
    [InlineData("café")]
    [InlineData("你好")]
    [InlineData("line\r\n")]
    public void IsSupported_RejectsCharactersVirtualBoxSkips(string text)
    {
        Assert.False(TypableText.IsSupported(text));
    }
}
