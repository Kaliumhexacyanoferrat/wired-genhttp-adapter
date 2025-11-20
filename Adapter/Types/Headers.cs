using System.Collections;
using GenHTTP.Api.Protocol;
using Wired.IO.Http11Express.Request;

namespace GenHTTP.Adapters.WiredIO.Types;

public sealed class Headers : IHeaderCollection
{

    #region Get-/Setters

    public int Count => Request?.Headers.Count ?? 0;

    public bool ContainsKey(string key) => Request?.Headers.ContainsKey(key) ?? false;

    public bool TryGetValue(string key, out string value)
    {
        if (Request?.Headers.TryGetValue(key, out var found) ?? false)
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public string this[string key] => Request?.Headers.GetValueOrDefault(key) ?? string.Empty;

    public IEnumerable<string> Keys => Request?.Headers.Keys ?? Enumerable.Empty<string>();

    public IEnumerable<string> Values
    {
        get
        {
            if (Request != null)
            {
                foreach (var entry in Request.Headers)
                {
                    yield return entry.Value;
                }
            }
        }
    }

    private IExpressRequest? Request { get; set; }

    #endregion

    #region Functionality

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        if (Request != null)
        {
            foreach (var entry in Request.Headers)
            {
                yield return new(entry.Key, entry.Value);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void SetRequest(IExpressRequest? request)
    {
        Request = request;
    }

    #endregion

}
