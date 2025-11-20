using Microsoft.Extensions.ObjectPool;

namespace GenHTTP.Adapters.WiredIO.Types;

internal class ClientContextPolicy : PooledObjectPolicy<ClientContext>
{

    public override ClientContext Create() => new();

    public override bool Return(ClientContext obj)
    {
        obj.Reset();
        return true;
    }

}
