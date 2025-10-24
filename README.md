# Wired.IO - GenHTTP Adapter

[![nuget Package](https://img.shields.io/nuget/v/GenHTTP.Adapters.WiredIO.svg)](https://www.nuget.org/packages/GenHTTP.Adapters.WiredIO/)

Allows to use modules provided by [GenHTTP](https://genhttp.org) within a
[Wired.IO](https://github.com/MDA2AV/Wired.IO) application.

```csharp
using GenHTTP.Adapters.WiredIO;
using GenHTTP.Modules.Functional;
using Wired.IO.App;

// GET http://localhost:5000/api/hello?a=World

var api = Inline.Create()
                .Get("hello", (string a) => $"Hello {a}!");

var builder = WiredApp.CreateExpressBuilder()
                      .Port(5000)
                      .Map("/api", api);

var app = builder.Build();

await app.RunAsync();
```
