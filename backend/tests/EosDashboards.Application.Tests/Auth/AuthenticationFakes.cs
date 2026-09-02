using System.Reflection;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Application.Tests.Auth;

internal sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.SingleOrDefault(user => user.OrganizationalId == stableId));
    }

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
    }

    public void Add(User user) => Users.Add(user);
}

internal sealed class FakeOtpChallengeRepository : IOtpChallengeRepository
{
    public List<OtpChallenge> Challenges { get; } = [];

    public Task<OtpChallenge?> FindByPublicTokenAsync(string token, CancellationToken cancellationToken)
    {
        return Task.FromResult(Challenges.SingleOrDefault(challenge => challenge.PublicToken == token));
    }

    public Task<OtpChallenge?> FindLatestActiveAsync(long userId, CancellationToken cancellationToken)
    {
        var challenge = Challenges
            .Where(item => item.UserId == userId)
            .Where(item => item.Status is OtpChallengeStatus.Pending or OtpChallengeStatus.Sent)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefault();
        return Task.FromResult(challenge);
    }

    public void Add(OtpChallenge challenge) => Challenges.Add(challenge);
}

internal sealed class FakeUserSessionRepository : IUserSessionRepository
{
    public List<UserSession> Sessions { get; } = [];

    public Func<OtpChallengeStatus>? CurrentChallengeStatus { get; set; }

    public List<OtpChallengeStatus> ChallengeStatusesAtAdd { get; } = [];

    public Task<UserSession?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sessions.SingleOrDefault(session => session.Id == id));
    }

    public Task<UserSession?> FindByRefreshHashAsync(string refreshHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Sessions.SingleOrDefault(session => session.RefreshCredentialHash == refreshHash));
    }

    public void Add(UserSession session)
    {
        if (CurrentChallengeStatus is not null)
        {
            ChallengeStatusesAtAdd.Add(CurrentChallengeStatus());
        }

        Sessions.Add(session);
    }
}

internal sealed class FakeSmsSender : ISmsSender
{
    public List<SmsMessage> Messages { get; } = [];

    public SmsSendResult Result { get; set; } = new(true, null);

    public Exception? Exception { get; set; }

    public Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken)
    {
        Messages.Add(message);
        return Exception is null ? Task.FromResult(Result) : Task.FromException<SmsSendResult>(Exception);
    }
}

internal sealed class FakeSecretHasher : ISecretHasher
{
    public Dictionary<string, string> Hashes { get; } = [];

    public List<string> HashedValues { get; } = [];

    public string Hash(string value)
    {
        HashedValues.Add(value);
        return Hashes[value];
    }

    public bool Verify(string value, string expectedHash)
    {
        return Hashes.TryGetValue(value, out var actualHash) &&
               string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
    }
}

internal sealed class FakeSecureTokenGenerator : ISecureTokenGenerator
{
    private readonly Queue<string> _opaqueTokens = [];

    public string SixDigitCode { get; set; } = "246810";

    public List<int> RequestedByteCounts { get; } = [];

    public void AddOpaqueToken(string token) => _opaqueTokens.Enqueue(token);

    public string CreateSixDigitCode() => SixDigitCode;

    public string CreateOpaqueToken(int byteCount)
    {
        RequestedByteCounts.Add(byteCount);
        return _opaqueTokens.Dequeue();
    }
}

internal sealed class FakeMobileProtector : IMobileProtector
{
    public Dictionary<string, string> UnprotectedValues { get; } = [];

    public List<string> UnprotectCalls { get; } = [];

    public string Protect(string normalizedMobile) => throw new NotSupportedException();

    public string Unprotect(string protectedMobile)
    {
        UnprotectCalls.Add(protectedMobile);
        return UnprotectedValues[protectedMobile];
    }

    public string Mask(string normalizedMobile) => throw new NotSupportedException();
}

internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public List<(User User, long SessionId, DateTimeOffset IssuedAtUtc)> Requests { get; } = [];

    public IssuedAccessToken Issue(User user, long sessionId, DateTimeOffset issuedAtUtc)
    {
        Requests.Add((user, sessionId, issuedAtUtc));
        return new IssuedAccessToken("access-token", issuedAtUtc.AddMinutes(10));
    }
}

internal sealed class FakeAuditWriter : IAuditWriter
{
    public List<AuditRecord> Records { get; } = [];

    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork(
    FakeOtpChallengeRepository otpChallenges,
    FakeUserSessionRepository sessions) : IUnitOfWork
{
    private long _nextChallengeId = 101;
    private long _nextSessionId = 201;

    public int SaveCount { get; private set; }

    public List<SaveObservation> Observations { get; } = [];

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var challenge in otpChallenges.Challenges.Where(item => item.Id == 0))
        {
            EntityId.Set(challenge, _nextChallengeId++);
        }

        foreach (var session in sessions.Sessions.Where(item => item.Id == 0))
        {
            EntityId.Set(session, _nextSessionId++);
        }

        SaveCount++;
        Observations.Add(new SaveObservation(
            otpChallenges.Challenges.Select(challenge => challenge.Status).ToArray(),
            sessions.Sessions.Count));
        return Task.FromResult(1);
    }
}

internal sealed record SaveObservation(
    IReadOnlyCollection<OtpChallengeStatus> ChallengeStatuses,
    int SessionCount);

internal static class EntityId
{
    public static void Set<T>(T entity, long id)
    {
        typeof(T).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)!.SetValue(entity, id);
    }
}
