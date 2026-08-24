using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace WindowTitleRenamer.Tests;

public sealed class RenamerPipeServerTests
{
    [Fact]
    public async Task QueryPersistentRule_ReturnsCurrentInMemoryRule()
    {
        using PersistentRenamer renamer = new();
        IntPtr handle = new(12345);
        renamer.AddOrUpdate(handle, "Smoke title");
        using RenamerPipeServer server = new(renamer);
        server.Start();

        await using NamedPipeClientStream pipe = new(
            ".",
            RenamerPipeServer.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));
        await pipe.ConnectAsync(timeout.Token);
        using StreamWriter writer = new(pipe, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
        };
        using StreamReader reader = new(pipe, Encoding.UTF8, leaveOpen: true);

        await writer.WriteLineAsync(
            "{\"operation\":\"query_persistent_rule\",\"hwnd\":12345,\"title\":null}".AsMemory(),
            timeout.Token);
        string responseJson = await reader.ReadLineAsync(timeout.Token) ?? string.Empty;
        using JsonDocument response = JsonDocument.Parse(responseJson);

        Assert.True(response.RootElement.GetProperty("ok").GetBoolean());
        Assert.True(response.RootElement.GetProperty("hasRule").GetBoolean());
        Assert.Equal("Smoke title", response.RootElement.GetProperty("title").GetString());
    }
}
