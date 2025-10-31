using System.Collections.Concurrent;
using System.IO.Pipelines;

using GenHTTP.Adapters.WiredIO.Server;
using GenHTTP.Adapters.WiredIO.Types;
using GenHTTP.Adapters.WiredIO.Utils;

using GenHTTP.Api.Content;
using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;

using Wired.IO.App;
using Wired.IO.Http11Express.Context;

using WR = Wired.IO.Protocol.Response;

namespace GenHTTP.Adapters.WiredIO.Mapping;

public static class  Bridge
{
    private const int BufferSize = 8192;

    private static readonly ConcurrentDictionary<string, ImplicitServer> ServerCache = [];
    
    public static async Task MapAsync(WiredApp<Http11ExpressContext> app, Http11ExpressContext context, IHandler handler, IServerCompanion? companion = null, string? registeredPath = null)
    {
        var server =ServerCache.GetOrAdd(registeredPath ?? "/", key => new ImplicitServer(app, handler, companion));

        try
        {
            using var request = new Request(server, context.Request);

            if (!string.IsNullOrEmpty(registeredPath))
            {
                AdvanceTo(request, registeredPath);
            }
            
            using var response = await handler.HandleAsync(request);

            if (response != null)
            {
                MapResponse(response, context);

                server.Companion?.OnRequestHandled(request, response);
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

            target.Content(CreateHandler(context.Writer, response.Content), response.ContentLength);
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
        if (content.Length == null)
        {
            await using var stream = new ChunkedStream(writer.AsWiredStream());

            await content.WriteAsync(stream, BufferSize);

            await stream.FinishAsync();
        }
        else
        {
            await using var stream = writer.AsWiredStream();
            
            await content.WriteAsync(stream, BufferSize);
        }
    };
    
    private static Func<Task> CreateHandler(PipeWriter writer, IResponseContent content) => 
        () => StaticHandler(writer, content);
    
}
