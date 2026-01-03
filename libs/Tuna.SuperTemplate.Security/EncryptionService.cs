using System.Security.Cryptography;
using System.Text;
using Tuna.SuperTemplate.Security.Interfaces;

namespace Tuna.SuperTemplate.Security;

public class EncryptionService : IEncryptionService
{
    private readonly string _key;

    public EncryptionService(string key)
    {
        _key = key;
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_key[..32]);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);

        return Convert.ToBase64String(aes.IV.Concat(encrypted).ToArray());
    }

    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_key[..32]);

        var iv = fullCipher[..16];
        var cipher = fullCipher[16..];
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(decrypted);
    }

    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
