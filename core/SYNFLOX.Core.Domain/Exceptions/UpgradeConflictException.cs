using System;

namespace Domain.Exceptions;

public class UpgradeConflictException : Exception
{
    public UpgradeConflictException(string message) : base(message)
    {
    }
}
