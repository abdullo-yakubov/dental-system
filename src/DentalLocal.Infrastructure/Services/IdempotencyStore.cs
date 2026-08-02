using System.Collections.Concurrent;
using DentalLocal.Application.Common;

namespace DentalLocal.Infrastructure.Services;

internal sealed class IdempotencyStore : IIdempotencyStore
{
    private sealed record Entry(string Json, DateTime ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public bool TryGet(string key, out string cachedJson)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
        {
            cachedJson = entry.Json;
            return true;
        }

        cachedJson = string.Empty;
        return false;
    }

    public void Set(string key, string json, TimeSpan ttl) =>
        _entries[key] = new Entry(json, DateTime.UtcNow.Add(ttl));
}
