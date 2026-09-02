namespace EosDashboards.Application.Abstractions;

public interface ISecureTokenGenerator
{
    string CreateSixDigitCode();

    string CreateOpaqueToken(int byteCount);
}
