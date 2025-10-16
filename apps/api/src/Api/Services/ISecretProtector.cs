namespace Api.Services;

public interface ISecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}

public interface ISecretProtectorFactory
{
    ISecretProtector Create(string encryptionKeyConfigName, string encryptionKeyPlaceholder);
}
