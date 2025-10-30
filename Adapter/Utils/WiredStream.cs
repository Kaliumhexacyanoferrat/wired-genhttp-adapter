using System.Buffers;
using System.IO.Pipelines;

namespace GenHTTP.Adapters.WiredIO.Utils;

public sealed class WiredStream : Stream
{
    private readonly PipeWriter _writer;
    private readonly bool _completeOnDispose;
    private bool _disposed;

    public WiredStream(PipeWriter writer, bool completeOnDispose = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _completeOnDispose = completeOnDispose; // default: leave writer open
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count)
        => _writer.Write(buffer.AsSpan(offset, count));

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _writer.Write(buffer.Span);          // no implicit flush
        return ValueTask.CompletedTask;
    }

    public override void Flush()
        => _writer.FlushAsync().AsTask().GetAwaiter().GetResult();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _writer.FlushAsync(cancellationToken).AsTask();

    protected override void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        if (_completeOnDispose) _writer.Complete();  // otherwise leave open, no flush
        _disposed = true;
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed && _completeOnDispose)
            await _writer.CompleteAsync().ConfigureAwait(false);
        _disposed = true;
        await base.DisposeAsync().ConfigureAwait(false);
    }

    // Unsupported
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}