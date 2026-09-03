using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Preferences;

public sealed record UpdateMyPreferencesCommand(
    string AppearanceMode,
    string Palette,
    bool SidebarCollapsed);

public sealed class UpdateMyPreferences(
    IClock clock,
    ICorrelationContext correlationContext,
    IUserPreferenceRepository preferences,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private static readonly HashSet<string> AppearanceModes =
        ["light", "dark", GetMyPreferences.DefaultAppearanceMode];

    public async Task<UserPreferenceDto> HandleAsync(
        long userId,
        UpdateMyPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentNullException.ThrowIfNull(command);
        if (!AppearanceModes.Contains(command.AppearanceMode) ||
            command.Palette != GetMyPreferences.DefaultPalette)
        {
            throw new ArgumentException("The preference value is not supported.", nameof(command));
        }

        var now = clock.UtcNow;
        var preference = await preferences.GetForUpdateAsync(userId, cancellationToken);
        var changed = false;
        if (preference is null)
        {
            preference = UserPreference.Create(
                userId,
                command.AppearanceMode,
                command.Palette,
                command.SidebarCollapsed,
                now);
            preferences.Add(preference);
            changed = true;
        }
        else
        {
            changed = preference.Update(
                command.AppearanceMode,
                command.Palette,
                command.SidebarCollapsed,
                now);
        }

        if (changed)
        {
            await auditWriter.WriteAsync(
                new AuditRecord(
                    userId,
                    userId,
                    "UserPreferenceChanged",
                    true,
                    correlationContext.TraceId,
                    null),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new UserPreferenceDto(
            preference.AppearanceMode,
            preference.Palette,
            preference.SidebarCollapsed);
    }
}
