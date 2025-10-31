using System.Net;

using GenHTTP.Api.Infrastructure;

using Wired.IO.Http11Express.Context;

namespace GenHTTP.Adapters.WiredIO.Server;

public class EndpointCollection : List<IEndPoint>, IEndPointCollection;

public class ImplicitEndpoint : IEndPoint
{

    #region Get-/Setters

    public IPAddress? Address { get; }

    public ushort Port { get; }

    public bool Secure { get; }

    #endregion
    
    #region Initialization

    public ImplicitEndpoint(Http11ExpressContext context)
    {
        // todo: can we get this more efficiently?
        Port = DeterminePort(context);

        Secure = false;
        Address = null;
    }
    
    #endregion
    
    #region Functionality

    private static ushort DeterminePort(Http11ExpressContext context)
    {
        if (context.Request.Headers.TryGetValue("Host", out var host))
        {
            ReadOnlySpan<char> span = host.AsSpan();
            
            var colonIndex = span.LastIndexOf(':');

            if (colonIndex >= 0 && colonIndex < span.Length - 1)
            {
                var portSpan = span[(colonIndex + 1)..];
                
                if (ushort.TryParse(portSpan, out var port))
                {
                    return port;
                }
            }
        }

        return 8080;
    }
    
    public void Dispose()
    {
        // nop
    }
    
    #endregion
    
}
