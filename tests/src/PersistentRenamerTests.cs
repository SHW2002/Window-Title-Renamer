namespace WindowTitleRenamer.Tests;

public sealed class PersistentRenamerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "WindowTitleRenamer.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void StableProjectRule_SurvivesRenamerRestart()
    {
        string rulesPath = Path.Combine(_root, "rules.json");
        string stableKey = WindowIdentityResolver.BuildProjectKey(
            Path.Combine(_root, "Project"));
        WindowIdentityResolver resolver = new(_ => stableKey);
        IntPtr handle = new(12345);

        using (PersistentRenamer renamer = new(resolver, rulesPath))
        {
            renamer.AddOrUpdate(handle, "Unity 13 clone");
        }

        using PersistentRenamer reloaded = new(resolver, rulesPath);
        Assert.True(reloaded.TryGetRule(handle, out string? title));
        Assert.Equal("Unity 13 clone", title);
        Dictionary<string, string> stored =
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(rulesPath))!;
        Assert.Equal("Unity 13 clone", stored[stableKey]);
    }

    [Fact]
    public void RemovingStableProjectRule_RemovesPersistedRule()
    {
        string rulesPath = Path.Combine(_root, "rules.json");
        string stableKey = WindowIdentityResolver.BuildProjectKey(
            Path.Combine(_root, "Project"));
        WindowIdentityResolver resolver = new(_ => stableKey);
        IntPtr handle = new(12345);

        using PersistentRenamer renamer = new(resolver, rulesPath);
        renamer.AddOrUpdate(handle, "Temporary title");
        renamer.Remove(handle);

        using PersistentRenamer reloaded = new(resolver, rulesPath);
        Assert.False(reloaded.TryGetRule(handle, out _));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }
}
