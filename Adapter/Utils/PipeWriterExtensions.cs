using System.IO.Pipelines;

namespace GenHTTP.Adapters.WiredIO.Utils;

public static class PipeWriterExtensions
{
    
    public static WiredStream AsWiredStream(this PipeWriter writer, bool completeOnDispose = false)
        => new(writer, completeOnDispose);
    
}