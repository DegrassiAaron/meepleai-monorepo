using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Api.Services;

public class ConfigSecretProtectorFactory : ISecretProtectorFactory
{
    private readonly IConfiguration _configuration;

    public ConfigSecretProtectorFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ISecretProtector Create(string encryptionKeyConfigName, string encryptionKeyPlaceholder)
    {
        return new ConfigSecretProtector(_configuration, encryptionKeyConfigName, encryptionKeyPlaceholder);
    }

    private sealed class ConfigSecretProtector : ISecretProtector
    {
        private readonly IConfiguration _configuration;
        private readonly string _configName;
        private readonly string _placeholder;

        public ConfigSecretProtector(IConfiguration configuration, string configName, string placeholder)
        {
            _configuration = configuration;
            _configName = configName;
            _placeholder = placeholder;
        }

        public string Protect(string secret)
        {
            if (secret == null)
            {
                throw new ArgumentNullException(nameof(secret));
            }

            var encryptionKey = GetEncryptionKey();
            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(secret.Trim());
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Unprotect(string protectedSecret)
        {
            if (protectedSecret == null)
            {
                throw new ArgumentNullException(nameof(protectedSecret));
            }

            var encryptionKey = GetEncryptionKey();
            var fullCipher = Convert.FromBase64String(protectedSecret);

            using var aes = Aes.Create();
            aes.Key = encryptionKey;

            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        private byte[] GetEncryptionKey()
        {
            var key = _configuration[_configName]?.Trim();

            if (string.IsNullOrWhiteSpace(key) || string.Equals(key, _placeholder, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Missing or invalid encryption key. Set the {_configName} environment variable to a secure value.");
            }

            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}
