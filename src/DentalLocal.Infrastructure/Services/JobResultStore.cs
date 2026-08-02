using System.Collections.Concurrent;
using DentalLocal.Application.Common;

namespace DentalLocal.Infrastructure.Services;

internal sealed class JobResultStore : IJobResultStore
{
    private sealed record Entry(byte[] Content, string ContentType, string FileName, DateTime ExpiresAtUtc);

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    public void Set(Guid jobId, byte[] content, string contentType, string fileName, TimeSpan ttl) =>
        _entries[jobId] = new Entry(content, contentType, fileName, DateTime.UtcNow.Add(ttl));

    public bool TryGet(Guid jobId, out byte[] content, out string contentType, out string fileName)
    {
        if (_entries.TryGetValue(jobId, out var entry) && entry.ExpiresAtUtc > DateTime.UtcNow)
        {
            content = entry.Content;
            contentType = entry.ContentType;
            fileName = entry.FileName;
            return true;
        }

        content = [];
        contentType = string.Empty;
        fileName = string.Empty;
        return false;
    }
}
