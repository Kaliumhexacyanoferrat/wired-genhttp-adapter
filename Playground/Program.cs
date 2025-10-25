using GenHTTP.Adapters.WiredIO;
using GenHTTP.Api.Protocol;
using GenHTTP.Modules.DirectoryBrowsing;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;
using Microsoft.Extensions.DependencyInjection;
using Wired.IO.App;

internal class Program
{
    public class JsonMessage
    {
        public string message { get; set; } = null!;
    }
    
    public static async Task Main(string[] args)
    {
        var api = Inline.Create()
            .Get("plaintext", () => "Hello, World!")
            .Get("json", () => new JsonMessage{ message = "Hello, World!" });

        var files = Listing.From(ResourceTree.FromDirectory("./"));

        var services = Layout.Create()
            .Add("plaintext", Content.From(Resource.FromString("Hello World!")));
            //.Add("/", api);
            //.Add("files", files);
        //.Defaults();

        var builder = WiredApp.CreateExpressBuilder()
            .Port(8080)
            /*.MapGet("/plaintext", _ => ctx =>
            {
                ctx
                    .Respond()
                    .Status(Wired.IO.Protocol.Response.ResponseStatus.Ok)
                    .Type("text/plain"u8)
                    .Content("Hello World!"u8);
            })*/
            .Map("/", services);

        builder.Services.AddScoped<Service>();

        var app = builder.Build();

        IServiceProvider sp = app.Services;

        await app.RunAsync();
    }
}


public class Service
{
    public string Handle() => "Service Handled";
}
