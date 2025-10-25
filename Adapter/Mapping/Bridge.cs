using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using GenHTTP.Adapters.WiredIO.Server;
using GenHTTP.Adapters.WiredIO.Types;
using GenHTTP.Adapters.WiredIO.Utils;

using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Engine.Shared.Types;
using Microsoft.Extensions.ObjectPool;
using Wired.IO.Http11Express.Context;
using StringContent = GenHTTP.Modules.IO.Strings.StringContent;
using WR = Wired.IO.Protocol.Response;

namespace GenHTTP.Adapters.WiredIO.Mapping;

public static class  Bridge
{
    private const int BufferSize = 8192;

    public static async Task MapAsync(Http11ExpressContext context, Func<Http11ExpressContext, Task> next, IHandler handler, IServerCompanion? companion = null, string? registeredPath = null)
    {
        /*
        if ((registeredPath == null))
        {
            await Task.Delay(1);
        }
        context
            .Respond()
            .Status((WR.ResponseStatus)ResponseStatus.Ok)
            .Content("Hello, World!"u8)
            .Type("text/html"u8);
        */
        
        if ((registeredPath != null) && !context.Request.Route.StartsWith(registeredPath))
        {
            await next(context);
            return;
        }
        
        // todo: can we cache this somewhere?
        var server = new ImplicitServer(handler, companion);

        try
        {
            using var request = new Request(server, context.Request);

            if (registeredPath != null)
            {
                AdvanceTo(request, registeredPath);
            }
            
            using var response = await handler.HandleAsync(request);

            if (response != null)
            {
                MapResponse(response, context);

                server.Companion?.OnRequestHandled(request, response);
            }
            else
            {
                await next(context);
            }
        }
        catch (Exception e)
        {
            // todo: cannot tell the IP of the client in wired
            server.Companion?.OnServerError(ServerErrorScope.ServerConnection, null, e);
            throw;
        }
    }

    private static void MapResponse(IResponse response, Http11ExpressContext context)
    {
        var target = context.Respond();
        
        target.Status((WR.ResponseStatus)response.Status.RawStatus);

        foreach (var header in response.Headers)
        {
            target.Header(header.Key, header.Value);
        }

        if (response.Modified != null)
        {
            target.Header("Last-Modified", response.Modified.Value.ToUniversalTime().ToString("r"));
        }

        if (response.Expires != null)
        {
            target.Header("Expires", response.Expires.Value.ToUniversalTime().ToString("r"));
        }

        if (response.HasCookies)
        {
            foreach (var cookie in response.Cookies)
            {
                target.Header("Set-Cookie", $"{cookie.Key}={cookie.Value.Value}");
            }
        }

        if (response.Content != null)
        {
            target.Header("Content-Type", (response.ContentType?.Charset != null ? $"{response.ContentType?.RawType}; charset={response.ContentType?.Charset}" : response.ContentType?.RawType) ?? "application/octet-stream");

            if (response.ContentEncoding != null)
            {
                target.Header("Content-Encoding", response.ContentEncoding);
            }

            if (response.Content.Length == null)
            {
                target.Header("Transfer-Encoding", "chunked");
            }

            target.Type("text/html"u8);
            target.Content(CreateHandler(context.Writer, response.Content), response.ContentLength);
            //target.Content("Hello, World!"u8)
            //      .Type("text/html"u8);
        }
    }

    private static void AdvanceTo(Request request, string registeredPath)
    {
        var parts = registeredPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var _ in parts)
        {
            request.Target.Advance();
        }
    }
    
    private static readonly Func<PipeWriter, IResponseContent, Task> StaticHandler =  async (writer, content) =>
    {
        //writer.Write("Hello, World!"u8);
        
        if (content.Length == null)
        {
            await using var stream = new ChunkedStream(writer.AsStream(leaveOpen: true));

            await content.WriteAsync(stream, BufferSize);

            await stream.FinishAsync();
        }
        else
        {
            //await using var stream = writer.AsStream(leaveOpen: true);
            //await content.WriteAsync(stream, BufferSize);
            
            await using var stream = writer.AsWiredStream();
            await content.WriteAsync(stream, BufferSize);
        }

        //await writer.FlushAsync();
    };
    
    private static Func<Task> CreateHandler(PipeWriter writer, IResponseContent content) => () => StaticHandler(writer, content);

}

public sealed class WiredStream : Stream
{
    private readonly PipeWriter _writer;
    private readonly bool _completeOnDispose;
    private bool _disposed;

    public WiredStream(PipeWriter writer, bool completeOnDispose = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _completeOnDispose = completeOnDispose; // default: leave writer open
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count)
        => _writer.Write(buffer.AsSpan(offset, count));

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _writer.Write(buffer.Span);          // no implicit flush
        return ValueTask.CompletedTask;
    }

    public override void Flush()
        => _writer.FlushAsync().AsTask().GetAwaiter().GetResult();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _writer.FlushAsync(cancellationToken).AsTask();

    protected override void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        if (_completeOnDispose) _writer.Complete();  // otherwise leave open, no flush
        _disposed = true;
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed && _completeOnDispose)
            await _writer.CompleteAsync().ConfigureAwait(false);
        _disposed = true;
        await base.DisposeAsync().ConfigureAwait(false);
    }

    // Unsupported
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}

public static class PipeWriterExtensions
{
    public static WiredStream AsWiredStream(this PipeWriter writer, bool completeOnDispose = false)
        => new WiredStream(writer, completeOnDispose);
}
