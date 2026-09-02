namespace EosDashboards.Application.Abstractions;

public interface IMobileProtector
{
    string Protect(string normalizedMobile);

    string Unprotect(string protectedMobile);

    string Mask(string normalizedMobile);
}
