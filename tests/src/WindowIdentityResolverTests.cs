namespace WindowTitleRenamer.Tests;

public sealed class WindowIdentityResolverTests
{
    [Theory]
    [InlineData(
        "Tuanjie.exe -projectPath E:/Unity/Project",
        "E:\\Unity\\Project")]
    [InlineData(
        "Tuanjie.exe -projectPath \"E:/Unity/Project With Spaces\"",
        "E:\\Unity\\Project With Spaces")]
    public void TryParseProjectPath_ExtractsQuotedAndUnquotedValues(
        string commandLine,
        string expected)
    {
        Assert.True(WindowIdentityResolver.TryParseProjectPath(
            commandLine,
            out string? path));
        Assert.Equal(Path.GetFullPath(expected), path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Tuanjie.exe -batchmode")]
    public void TryParseProjectPath_RejectsMissingArgument(string? commandLine)
    {
        Assert.False(WindowIdentityResolver.TryParseProjectPath(commandLine, out _));
    }
}
