namespace EosDashboards.Application.Abstractions;

public interface ISecretHasher
{
    string Hash(string value);

    bool Verify(string value, string expectedHash);
}
