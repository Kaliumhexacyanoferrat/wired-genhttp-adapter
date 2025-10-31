using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;

using GenHTTP.Engine.Shared.Types;

using Wired.IO.Http11Express.Request;

namespace GenHTTP.Adapters.WiredIO.Types;

public sealed class Request : IRequest
{
    private RequestProperties? _properties;

    private Query? _query;

    private ICookieCollection? _cookies;

    private readonly ForwardingCollection _forwardings = new();

    private Headers? _headers;

    #region Get-/Setters

    public IRequestProperties Properties
    {
        get { return _properties ??= new RequestProperties(); }
    }

    public IServer Server { get; }

    public IEndPoint EndPoint => Server.EndPoints[0];

    public IClientConnection Client { get; }

    public IClientConnection LocalClient { get; }

    public HttpProtocol ProtocolType { get; }

    public FlexibleRequestMethod Method { get; }

    public RoutingTarget Target { get; }

    public string? UserAgent => this["User-Agent"];

    public string? Referer => this["Referer"];

    public string? Host => this["Host"];

    public string? this[string additionalHeader] => Headers.GetValueOrDefault(additionalHeader);

    public IRequestQuery Query
    {
        get { return _query ??= new Query(InnerRequest); }
    }

    public ICookieCollection Cookies
    {
        get { return _cookies ??= FetchCookies(InnerRequest); }
    }

    public IForwardingCollection Forwardings => _forwardings;

    public IHeaderCollection Headers
    {
        get { return _headers ??= new Headers(InnerRequest); }
    }

    // todo: this is quite inefficient
    public Stream Content => (InnerRequest.Content != null) ? new MemoryStream(InnerRequest.Content) : Stream.Null;

    public FlexibleContentType? ContentType
    {
        get
        {
            if (InnerRequest.Headers.TryGetValue("Content-Type", out var contentType))
            {
                return FlexibleContentType.Parse(contentType);
            }

            return null;
        }
    }

    private IExpressRequest InnerRequest { get; }

    #endregion

    #region Initialization

    public Request(IServer server, IExpressRequest request)
    {
        Server = server;
        InnerRequest = request;

        // todo: no API provided by wired
        ProtocolType = HttpProtocol.Http11;

        Method = FlexibleRequestMethod.Get(request.HttpMethod);
        Target = new RoutingTarget(WebPath.FromString(request.Route));

        if (request.Headers.TryGetValue("forwarded", out var entry))
        {
            _forwardings.Add(entry);
        }
        else
        {
            _forwardings.TryAddLegacy(Headers);
        }

        LocalClient = new ClientConnection(request);

        // todo: potential client certificate is not exposed by wired
        // Taveira: wired has a SslServerAuthenticationOptions property
        /*
         * public SslServerAuthenticationOptions SslServerAuthenticationOptions { get; set; } =
           new SslServerAuthenticationOptions
           {
               EnabledSslProtocols = SslProtocols.None
           };
         */
        // which contains the certificate plus SslApplicationProtocol.Http11 for http level
        Client = _forwardings.DetermineClient(null) ?? LocalClient;
    }

    private CookieCollection FetchCookies(IExpressRequest request)
    {
        var cookies = new CookieCollection();

        if (request.Headers.TryGetValue("Cookie", out var header))
        {
            cookies.Add(header);
        }

        return cookies;
    }

    #endregion

    #region Functionality

    public IResponseBuilder Respond() => new ResponseBuilder().Status(ResponseStatus.Ok);
    
    public UpgradeInfo Upgrade() => throw new NotSupportedException("Upgrading is not supported by this adapter. Please use the native websocket capabilities of Wired.IO.");

    #endregion

    #region Lifecycle

    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            _properties?.Dispose();

            _disposed = true;
        }
    }

    #endregion

}
