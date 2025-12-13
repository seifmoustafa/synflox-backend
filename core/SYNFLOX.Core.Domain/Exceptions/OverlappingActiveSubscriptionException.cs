using System;

namespace Domain.Exceptions;

public class OverlappingActiveSubscriptionException : Exception
{
    public OverlappingActiveSubscriptionException(string message) : base(message)
    {
    }
}
