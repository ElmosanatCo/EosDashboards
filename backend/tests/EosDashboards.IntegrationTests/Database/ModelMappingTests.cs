using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;
using EosDashboards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EosDashboards.IntegrationTests.Database;

public sealed class ModelMappingTests
{
    private readonly IModel _model;

    public ModelMappingTests()
    {
        var options = new DbContextOptionsBuilder<EosDashboardDbContext>()
            .UseSqlServer()
            .Options;

        using var context = new EosDashboardDbContext(options);
        _model = context.Model;
    }

    [Fact]
    public void Model_MapsTheSevenApprovedTables()
    {
        var tableNames = _model.GetEntityTypes()
            .Select(entity => entity.GetTableName()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["AuditLogs", "OtpChallenges", "Roles", "UserPreferences", "UserRoles", "UserSessions", "Users"],
            tableNames);
    }

    [Fact]
    public void PrincipalKeys_AreBigintIdentityColumnsNamedId()
    {
        var principalTypes = new[]
        {
            typeof(User),
            typeof(Role),
            typeof(OtpChallenge),
            typeof(UserSession),
            typeof(UserPreference),
            typeof(AuditLog),
        };

        foreach (var principalType in principalTypes)
        {
            var entity = RequiredEntity(principalType);
            var key = Assert.Single(entity.FindPrimaryKey()!.Properties);

            Assert.Equal("Id", key.Name);
            Assert.Equal(typeof(long), key.ClrType);
            Assert.Equal("bigint", key.GetColumnType());
            Assert.Equal(ValueGenerated.OnAdd, key.ValueGenerated);
            Assert.Equal(SqlServerValueGenerationStrategy.IdentityColumn, key.GetValueGenerationStrategy());
        }
    }

    [Fact]
    public void UserRoles_UsesApprovedCompositeKey()
    {
        var keyNames = RequiredEntity(typeof(UserRole)).FindPrimaryKey()!.Properties
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["UserId", "RoleId"], keyNames);
    }

    [Fact]
    public void StableLookupValues_AreUniquelyIndexed()
    {
        AssertUniqueIndex<User>(nameof(User.OrganizationalId));
        AssertUniqueIndex<Role>(nameof(Role.Code));
        AssertUniqueIndex<OtpChallenge>(nameof(OtpChallenge.PublicToken));
        AssertUniqueIndex<UserSession>(nameof(UserSession.RefreshCredentialHash));
        AssertUniqueIndex<UserPreference>(nameof(UserPreference.UserId));
        AssertUniqueIndex<User>(nameof(User.Username));
    }

    [Fact]
    public void Local_credential_and_otp_purpose_columns_are_mapped()
    {
        // Break caught: accepting local credentials without enforcing unique lookup or OTP purpose persistence.
        var username = RequiredProperty<User>(nameof(User.Username));
        var passwordHash = RequiredProperty<User>(nameof(User.PasswordHash));
        var purpose = RequiredProperty<OtpChallenge>(nameof(OtpChallenge.Purpose));

        Assert.True(username.IsNullable);
        Assert.True(passwordHash.IsNullable);
        Assert.Equal(256, username.GetMaxLength());
        Assert.Equal(1024, passwordHash.GetMaxLength());
        Assert.False(purpose.IsNullable);
        Assert.Equal(32, purpose.GetMaxLength());
        Assert.Equal(typeof(OtpChallengePurpose), purpose.ClrType);
    }

    [Fact]
    public void SensitiveAndTextColumns_HaveExplicitSafeBounds()
    {
        Assert.InRange(RequiredProperty<User>(nameof(User.ProtectedMobileNumber)).GetMaxLength()!.Value, 1, 2048);
        Assert.InRange(RequiredProperty<User>(nameof(User.MaskedMobileNumber)).GetMaxLength()!.Value, 1, 64);
        Assert.InRange(RequiredProperty<OtpChallenge>(nameof(OtpChallenge.CodeHash)).GetMaxLength()!.Value, 1, 512);
        Assert.InRange(RequiredProperty<AuditLog>(nameof(AuditLog.SafeMetadata)).GetMaxLength()!.Value, 1, 4000);
    }

    [Theory]
    [InlineData(typeof(OtpChallenge))]
    [InlineData(typeof(UserSession))]
    public void ConcurrentAuthenticationState_UsesShadowRowVersion(Type entityType)
    {
        var rowVersion = RequiredEntity(entityType).FindProperty("RowVersion");

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsShadowProperty());
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(typeof(byte[]), rowVersion.ClrType);
        Assert.False(rowVersion.IsNullable);
        Assert.Equal("rowversion", rowVersion.GetColumnType());
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void Relationships_UseExplicitDeleteBehavior()
    {
        Assert.All(
            RequiredEntity(typeof(OtpChallenge)).GetForeignKeys(),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.All(
            RequiredEntity(typeof(UserSession)).GetForeignKeys(),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.All(
            RequiredEntity(typeof(AuditLog)).GetForeignKeys(),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private void AssertUniqueIndex<TEntity>(string propertyName)
    {
        var entity = RequiredEntity(typeof(TEntity));
        Assert.Contains(
            entity.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(property => property.Name).SequenceEqual([propertyName]));
    }

    private IProperty RequiredProperty<TEntity>(string propertyName) =>
        RequiredEntity(typeof(TEntity)).FindProperty(propertyName)!;

    private IEntityType RequiredEntity(Type clrType) => _model.FindEntityType(clrType)!;
}
