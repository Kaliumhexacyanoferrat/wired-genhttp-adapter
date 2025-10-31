using GenHTTP.Adapters.WiredIO;

using GenHTTP.Modules.ApiBrowsing;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Layouting;
using GenHTTP.Modules.OpenApi;

using Wired.IO.App;

// GET http://localhost:5000/api/redoc/

var api = Inline.Create()
                .Get("hello", (string a) => $"Hello {a}!");

var layout = Layout.Create()
                   .Add(api)
                   .AddOpenApi()
                   .AddRedoc()
                   .Defaults(); // adds compression, eTag handling, ...

var builder = WiredApp.CreateExpressBuilder()
                      .Port(5000)
                      .MapGenHttp("/api/*", layout);

var app = builder.Build();

await app.RunAsync();
