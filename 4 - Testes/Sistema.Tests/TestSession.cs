using Microsoft.AspNetCore.Http;

namespace Sistema.Tests;

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _data = [];

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _data.Keys;

    public void Clear() => _data.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _data.Remove(key);

    public void Set(string key, byte[] value) => _data[key] = value;

    public bool TryGetValue(string key, out byte[] value)
    {
        if (_data.TryGetValue(key, out var data))
        {
            value = data;
            return true;
        }

        value = [];
        return false;
    }
}
