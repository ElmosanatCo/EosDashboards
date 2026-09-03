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

internal sealed class FakeCorrelationContext(string traceId) : ICorrelationContext
{
    public string TraceId { get; } = traceId;
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public List<CancellationToken> FindTokens { get; } = [];

    public List<CancellationToken> GetTokens { get; } = [];

    public Task<User?> FindByOrganizationalIdAsync(string stableId, CancellationToken cancellationToken)
    {
        FindTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Users.SingleOrDefault(user => user.OrganizationalId == stableId));
    }

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Users.SingleOrDefault(user => user.Username == username));
    }

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        GetTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
    }

    public void Add(User user) => Users.Add(user);
}

internal sealed class FakeRoleRepository : IRoleRepository
{
    public List<Role> Roles { get; } = [];

    public Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(Roles.SingleOrDefault(role => role.Code == code));

    public Task<IReadOnlyList<Role>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Role>>(Roles.Where(role => ids.Contains(role.Id)).ToArray());

    public void Add(Role role) => Roles.Add(role);
}

internal sealed class FakeDepartmentRepository : IDepartmentRepository
{
    public List<Department> Departments { get; } = [];

    public Task<Department?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Departments.SingleOrDefault(department => department.Name == name));

    public Task<Department?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Departments.SingleOrDefault(department => department.Id == id));
}

internal sealed class FakeOtpChallengeRepository : IOtpChallengeRepository
{
    public List<OtpChallenge> Challenges { get; } = [];

    public List<CancellationToken> FindTokens { get; } = [];

    public Task<OtpChallenge?> FindByPublicTokenAsync(string token, CancellationToken cancellationToken)
    {
        FindTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Challenges.SingleOrDefault(challenge => challenge.PublicToken == token));
    }

    public Task<OtpChallenge?> FindLatestActiveAsync(long userId, CancellationToken cancellationToken)
        => FindLatestActiveAsync(userId, OtpChallengePurpose.SignIn, cancellationToken);

    public Task<OtpChallenge?> FindLatestActiveAsync(
        long userId,
        OtpChallengePurpose purpose,
        CancellationToken cancellationToken)
    {
        FindTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var challenge = Challenges
            .Where(item => item.UserId == userId)
            .Where(item => item.Purpose == purpose)
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

    public Task<IReadOnlyCollection<UserSession>> GetActiveByUserIdAsync(
        long userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<UserSession>>(
            Sessions.Where(session => session.UserId == userId && session.IsActive(nowUtc)).ToArray());
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

internal sealed class FakeExternalIdentityLinkRepository : IExternalIdentityLinkRepository
{
    public List<ExternalIdentityLink> Links { get; } = [];

    public Task<ExternalIdentityLink?> FindByProviderSubjectAsync(
        ExternalIdentityProvider provider,
        string providerSubject,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Links.SingleOrDefault(link =>
            link.Provider == provider && link.ProviderSubject == providerSubject));
    }

    public Task<ExternalIdentityLink?> FindPendingByProviderEmailAsync(
        ExternalIdentityProvider provider,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Links.SingleOrDefault(link =>
            link.Provider == provider &&
            link.ProviderSubject is null &&
            link.NormalizedEmail == normalizedEmail));
    }

    public Task<ExternalIdentityLink?> FindByUserIdAndProviderAsync(
        long userId,
        ExternalIdentityProvider provider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Links.SingleOrDefault(link =>
            link.UserId == userId && link.Provider == provider));
    }

    public void Add(ExternalIdentityLink link) => Links.Add(link);
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

    public Action<string>? OnHash { get; set; }

    public string Hash(string value)
    {
        HashedValues.Add(value);
        OnHash?.Invoke(value);
        return Hashes[value];
    }

    public bool Verify(string value, string expectedHash)
    {
        return Hashes.TryGetValue(value, out var actualHash) &&
               string.Equals(actualHash, expectedHash, StringComparison.Ordinal);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public Dictionary<string, string> Hashes { get; } = [];

    public List<string> HashRequests { get; } = [];

    public string Hash(string password)
    {
        HashRequests.Add(password);
        return Hashes[password];
    }

    public PasswordVerificationResult Verify(string password, string passwordHash) =>
        Hashes.TryGetValue(password, out var actualHash) &&
        string.Equals(actualHash, passwordHash, StringComparison.Ordinal)
            ? PasswordVerificationResult.Succeeded
            : PasswordVerificationResult.Failed;
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
    public List<(User User, long SessionId, DateTimeOffset IssuedAtUtc, DateTimeOffset ExpiresAtUtc)> Requests { get; } = [];

    public IssuedAccessToken Issue(
        User user,
        long sessionId,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Requests.Add((user, sessionId, issuedAtUtc, expiresAtUtc));
        return new IssuedAccessToken("access-token", expiresAtUtc);
    }
}

internal sealed class FakeAuditWriter : IAuditWriter
{
    public List<AuditRecord> Records { get; } = [];

    public List<CancellationToken> CancellationTokens { get; } = [];

    public Exception? Exception { get; set; }

    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        CancellationTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (Exception is not null)
        {
            return Task.FromException(Exception);
        }

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

    public List<CancellationToken> CancellationTokens { get; } = [];

    public List<string> OperationKeys { get; } = [];

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        CancellationTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
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

    public async Task ExecuteSerializedTransactionAsync(
        string operationKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        OperationKeys.Add(operationKey);
        await operation(cancellationToken);
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

internal static class AuditRecordAssertions
{
    public static void AssertSingle(
        FakeAuditWriter writer,
        long? actorUserId,
        long? subjectUserId,
        string eventCode,
        bool succeeded)
    {
        var record = Assert.Single(writer.Records);
        Assert.Equal(actorUserId, record.ActorUserId);
        Assert.Equal(subjectUserId, record.SubjectUserId);
        Assert.Equal(eventCode, record.EventCode);
        Assert.Equal(succeeded, record.Succeeded);
        Assert.Equal("trace-test", record.TraceId);
        Assert.Null(record.SafeMetadata);
    }
}
