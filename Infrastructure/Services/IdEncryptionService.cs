using System;
using System.Security.Cryptography;
using System.Text;
using Application.Services;
using Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public class IdEncryptionService : IIdEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public IdEncryptionService(IOptions<EncryptionSettings> options)
        {
            var settings = options.Value;
            _key = Encoding.UTF8.GetBytes(settings.Key);
            _iv = Encoding.UTF8.GetBytes(settings.IV);
        }

        public Guid Encrypt(Guid id)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            var encryptor = aes.CreateEncryptor();
            var bytes = id.ToByteArray();
            var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return new Guid(encrypted);
        }

        public Guid Decrypt(Guid encryptedId)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;

            var decryptor = aes.CreateDecryptor();
            var bytes = encryptedId.ToByteArray();
            var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return new Guid(decrypted);
        }
    }
}
