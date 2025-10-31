using GenHTTP.Adapters.WiredIO;

using GenHTTP.Modules.ApiBrowsing;
using GenHTTP.Modules.DirectoryBrowsing;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Functional.Provider;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.Layouting.Provider;
using GenHTTP.Modules.OpenApi;

using Wired.IO.App;

// dotnet publish -c release -r linux-x64 --no-restore --self-contained

// http://localhost:8080/genhttp/api/plaintext
// http://localhost:8080/wiredio/api/plaintext

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WiredApp.CreateExpressBuilder()
            .Port(8080)
            .NoScopedEndpoints() // Do not use AsyncServiceScope for each pipeline call
            .MapGenHttp("/genhttp/*", CreateLayoutBuilder());
            
        _ = builder
            .MapGroup("/wiredio")
            .MapGroup("/api")
            .MapGet("/plaintext", ctx =>
            {
                ctx
                    .Respond()
                    .Status(Wired.IO.Protocol.Response.ResponseStatus.Ok)
                    .Type("plain/text"u8)
                    .Content("Hello, World!"u8);
            });
        
        await builder
            .Build()
            .RunAsync();
    }

    private static LayoutBuilder CreateLayoutBuilder() => 
        Layout
            .Create()
            .Add("plaintext", Content.From(Resource.FromString("Hello World!")))
            .Add("/api", CreateApi())
            .Add("files", Listing.From(ResourceTree.FromDirectory("./")))
            .AddOpenApi()
            .AddRedoc()
            .Defaults();
    
    private static InlineBuilder CreateApi() => 
        Inline
            .Create() 
            .Get("plaintext", () => "Hello, World!");
}