using GenHTTP.Api.Infrastructure;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Routing;

using GenHTTP.Engine.Shared.Types;

using Wired.IO.Http11Express.Request;

namespace GenHTTP.Adapters.WiredIO.Types;

public sealed class Request : IRequest
{
    private bool _freshResponse = true;

    private ResponseBuilder _responseBuilder;

    private IServer? _server;

    private IClientConnection? _client;
    private IClientConnection? _localCLient;

    private FlexibleRequestMethod? _method;
    private RoutingTarget? _target;

    private readonly RequestProperties _properties = new();

    private readonly Query _query = new();

    private readonly CookieCollection _cookies = new();

    private readonly ForwardingCollection _forwardings = new();

    private readonly Headers _headers = new();

    #region Get-/Setters

    public IRequestProperties Properties => _properties;

    public IServer Server => _server ?? throw new InvalidOperationException("Request is not initialized yet");

    public IEndPoint EndPoint => Server.EndPoints[0];

    public IClientConnection Client => _client ?? throw new InvalidOperationException("Request is not initialized yet");

    public IClientConnection LocalClient => _localCLient ?? throw new InvalidOperationException("Request is not initialized yet");

    public HttpProtocol ProtocolType { get; private set; }

    public FlexibleRequestMethod Method => _method ?? throw new InvalidOperationException("Request is not initialized yet");

    public RoutingTarget Target => _target?? throw new InvalidOperationException("Request is not initialized yet");

    public string? UserAgent => this["User-Agent"];

    public string? Referer => this["Referer"];

    public string? Host => this["Host"];

    public string? this[string additionalHeader] => Headers.GetValueOrDefault(additionalHeader);

    public IRequestQuery Query => _query;

    public ICookieCollection Cookies => _cookies;

    public IForwardingCollection Forwardings => _forwardings;

    public IHeaderCollection Headers => _headers;

    // todo: this is quite inefficient
    public Stream Content => (InnerRequest?.Content != null) ? new MemoryStream(InnerRequest.Content) : Stream.Null;

    public FlexibleContentType? ContentType
    {
        get
        {
            if (Headers.TryGetValue("Content-Type", out var contentType))
            {
                return FlexibleContentType.Parse(contentType);
            }

            return null;
        }
    }

    private IExpressRequest? InnerRequest { get; set; }

    #endregion

    #region Initialization

    public Request(ResponseBuilder responseBuilder)
    {
        _responseBuilder = responseBuilder;
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

    public IResponseBuilder Respond()
    {
        if (!_freshResponse)
        {
            _responseBuilder.Reset();
        }
        else
        {
            _freshResponse = false;
        }

        return _responseBuilder;
    }

    public UpgradeInfo Upgrade() => throw new NotSupportedException("Upgrading is not supported by this adapter. Please use the native websocket capabilities of Wired.IO.");

    internal void SetRequest(IServer server, IExpressRequest request)
    {
        _server = server;

        InnerRequest = request;

        // todo: no API provided by wired
        ProtocolType = HttpProtocol.Http11;

        _method = FlexibleRequestMethod.Get(request.HttpMethod);
        _target = new RoutingTarget(WebPath.FromString(request.Route));

        _headers.SetRequest(request);
        _query.SetRequest(request);

        if (request.Headers.TryGetValue("forwarded", out var entry))
        {
            _forwardings.Add(entry);
        }
        else
        {
            _forwardings.TryAddLegacy(Headers);
        }

        _localCLient = new ClientConnection(request);

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
        _client = _forwardings.DetermineClient(null) ?? LocalClient;
    }

    internal void Reset()
    {
        _headers.SetRequest(null);
        _query.SetRequest(null);

        _server = null;
        _client = null;
        _localCLient = null;
        _method = null;
    }

    #endregion

    #region Lifecycle

    public void Dispose()
    {
        // nop
    }

    #endregion

}
