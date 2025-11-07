using System;

namespace Application.Services;

public interface IIdEncryptionService
{
    Guid Encrypt(Guid id);
    Guid Decrypt(Guid encryptedId);
}
