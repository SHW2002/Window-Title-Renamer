using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowTitleRenamer;

internal sealed class RenamerPipeServer : IDisposable
{
    internal const string PipeName = "WindowTitleRenamer.UnityRestart.v1";
    private const int MaximumTitleLength = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private readonly PersistentRenamer _persistentRenamer;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _serverTask;

    public RenamerPipeServer(PersistentRenamer persistentRenamer)
    {
        _persistentRenamer = persistentRenamer;
    }

    public void Start()
    {
        _serverTask ??= Task.Run(() => AcceptLoopAsync(_cancellation.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using NamedPipeServerStream pipe = new(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellationToken);
                await HandleConnectionAsync(pipe, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // A client can disconnect at any point; keep accepting later requests.
            }
            catch (JsonException)
            {
                // Invalid clients must not stop the title-maintenance worker.
            }
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream pipe,
        CancellationToken cancellationToken)
    {
        using StreamReader reader = new(pipe, Encoding.UTF8, leaveOpen: true);
        using StreamWriter writer = new(pipe, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
        };

        string? requestJson = await reader.ReadLineAsync(cancellationToken);
        PipeResponse response;
        if (string.IsNullOrWhiteSpace(requestJson))
        {
            response = PipeResponse.Failure("请求为空。");
        }
        else
        {
            PipeRequest? request = JsonSerializer.Deserialize<PipeRequest>(requestJson, JsonOptions);
            response = HandleRequest(request);
        }

        await writer.WriteLineAsync(
            JsonSerializer.Serialize(response, JsonOptions).AsMemory(),
            cancellationToken);
    }

    private PipeResponse HandleRequest(PipeRequest? request)
    {
        if (request is null || request.WindowHandle == 0)
        {
            return PipeResponse.Failure("窗口句柄无效。");
        }

        IntPtr windowHandle = new(request.WindowHandle);
        if (string.Equals(
                request.Operation,
                "query_persistent_rule",
                StringComparison.Ordinal))
        {
            bool found = _persistentRenamer.ListRules().TryGetValue(windowHandle, out string? title);
            return new PipeResponse(true, found, title, null);
        }

        if (string.Equals(
                request.Operation,
                "bind_persistent_rule",
                StringComparison.Ordinal))
        {
            if (!NativeMethods.IsWindow(windowHandle))
            {
                return PipeResponse.Failure("目标窗口不存在。");
            }
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > MaximumTitleLength)
            {
                return PipeResponse.Failure("标题为空或过长。");
            }

            RenameResult renameResult = WindowService.Rename(windowHandle, request.Title);
            if (!renameResult.Succeeded)
            {
                return PipeResponse.Failure(
                    renameResult.WindowNoLongerExists
                        ? "目标窗口已经关闭。"
                        : $"设置标题失败，Win32 错误码 {renameResult.ErrorCode}。");
            }

            _persistentRenamer.AddOrUpdate(windowHandle, request.Title);
            return new PipeResponse(true, true, request.Title, null);
        }

        return PipeResponse.Failure("未知操作。");
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        try
        {
            _serverTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
        }
        _cancellation.Dispose();
    }

    private sealed class PipeRequest
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("hwnd")]
        public long WindowHandle { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    private sealed record PipeResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("hasRule")] bool HasRule,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("error")] string? Error)
    {
        public static PipeResponse Failure(string error) => new(false, false, null, error);
    }
}
