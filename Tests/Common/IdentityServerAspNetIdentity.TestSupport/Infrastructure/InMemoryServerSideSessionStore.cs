using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace IdentityServerAspNetIdentity.TestSupport.Infrastructure;

public class InMemoryServerSideSessionStore : IServerSideSessionStore
{
    private readonly List<ServerSideSession> _sessions = [];

    public Task CreateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s => s.Key == key);
        return Task.CompletedTask;
    }

    public Task DeleteSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s =>
            (filter.SubjectId == null || s.SubjectId == filter.SubjectId) &&
            (filter.SessionId == null || s.SessionId == filter.SessionId));
        return Task.CompletedTask;
    }

    public Task<ServerSideSession?> GetSessionAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.FirstOrDefault(s => s.Key == key));
    }

    public Task<IReadOnlyCollection<ServerSideSession>> GetSessionsAsync(SessionFilter filter, CancellationToken cancellationToken = default)
    {
        var results = _sessions.Where(s =>
                (filter.SubjectId == null || s.SubjectId == filter.SubjectId) &&
                (filter.SessionId == null || s.SessionId == filter.SessionId))
            .ToList();
        return Task.FromResult<IReadOnlyCollection<ServerSideSession>>(results);
    }

    public Task UpdateSessionAsync(ServerSideSession session, CancellationToken cancellationToken = default)
    {
        _sessions.RemoveAll(s => s.Key == session.Key);
        _sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<QueryResult<ServerSideSession>> QuerySessionsAsync(SessionQuery? filter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new QueryResult<ServerSideSession>
        {
            Results = _sessions,
            HasPrevResults = false,
            HasNextResults = false,
            TotalCount = _sessions.Count,
            TotalPages = 1,
            CurrentPage = 1
        });
    }

    public Task<IReadOnlyCollection<ServerSideSession>> GetAndRemoveExpiredSessionsAsync(int count, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<ServerSideSession>>([]);
    }
}
