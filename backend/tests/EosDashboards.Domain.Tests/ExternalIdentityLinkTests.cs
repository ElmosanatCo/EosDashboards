using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Tests;

public sealed class ExternalIdentityLinkTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pending_google_link_normalizes_email_and_binds_subject_once()
    {
        var link = ExternalIdentityLink.CreatePending(
            11,
            ExternalIdentityProvider.Google,
            " Person@Example.Com ",
            Now);

        link.BindSubject("google-subject", Now.AddMinutes(1));
        link.BindSubject("google-subject", Now.AddMinutes(2));

        Assert.Equal("PERSON@EXAMPLE.COM", link.NormalizedEmail);
        Assert.Equal("google-subject", link.ProviderSubject);
        Assert.Equal(Now.AddMinutes(1), link.LinkedAtUtc);
        Assert.Throws<InvalidOperationException>(() =>
        {
            link.BindSubject("different-subject", Now.AddMinutes(3));
        });
    }

    [Theory]
    [InlineData(0, "person@example.com")]
    [InlineData(1, "")]
    [InlineData(1, "   ")]
    public void Pending_link_rejects_invalid_user_or_email(long userId, string email)
    {
        Assert.ThrowsAny<ArgumentException>(() => ExternalIdentityLink.CreatePending(
            userId,
            ExternalIdentityProvider.Google,
            email,
            Now));
    }

    [Fact]
    public void Updating_approved_email_preserves_the_bound_provider_subject()
    {
        var link = ExternalIdentityLink.CreatePending(
            91,
            ExternalIdentityProvider.Google,
            "first.synthetic@example.test",
            Now);
        link.BindSubject("google-subject-synthetic", Now.AddMinutes(2));

        link.UpdateApprovedEmail("  second.synthetic@example.test  ");

        Assert.Equal("SECOND.SYNTHETIC@EXAMPLE.TEST", link.NormalizedEmail);
        Assert.Equal("google-subject-synthetic", link.ProviderSubject);
        Assert.Equal(Now.AddMinutes(2), link.LinkedAtUtc);
    }
}
