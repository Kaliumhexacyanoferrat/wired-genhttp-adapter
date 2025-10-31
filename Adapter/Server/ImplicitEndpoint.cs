using System.Net;

using GenHTTP.Api.Infrastructure;

using Wired.IO.App;
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

    public ImplicitEndpoint(WiredApp<Http11ExpressContext> app)
    {
        Address = app.IpAddress;
        Port = (ushort)app.Port;
        Secure = app.TlsEnabled;
    }
    
    #endregion
    
    #region Functionality
    
    public void Dispose()
    {
        // nop
    }
    
    #endregion
    
}
