using GenHTTP.Adapters.WiredIO;

using GenHTTP.Modules.DirectoryBrowsing;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.IO;
using GenHTTP.Modules.Layouting;

using Wired.IO.App;

var api = Inline.Create()
                .Get("hello", (string a) => $"Hello {a}!");

var files = Listing.From(ResourceTree.FromDirectory("./"));

var services = Layout.Create()
                     .Add("api", api)
                     .Add("files", files)
                     .Defaults();

var builder = WiredApp.CreateExpressBuilder()
                      .Port(5000)
                      .Map("/services", services);

var app = builder.Build();

await app.RunAsync();
