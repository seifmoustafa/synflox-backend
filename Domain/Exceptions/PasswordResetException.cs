using System;

namespace Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when password reset operations fail
    /// </summary>
    public class PasswordResetException : Exception
    {
        public PasswordResetException(string message) : base(message) { }

        public PasswordResetException(string message, Exception innerException) 
            : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when OTP is invalid or expired
    /// </summary>
    public class InvalidOtpException : PasswordResetException
    {
        public InvalidOtpException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when rate limit is exceeded
    /// </summary>
    public class RateLimitExceededException : PasswordResetException
    {
        public int RetryAfterMinutes { get; }

        public RateLimitExceededException(string message, int retryAfterMinutes) : base(message)
        {
            RetryAfterMinutes = retryAfterMinutes;
        }
    }

    /// <summary>
    /// Exception thrown when too many OTP verification attempts
    /// </summary>
    public class TooManyAttemptsException : PasswordResetException
    {
        public TooManyAttemptsException(string message) : base(message) { }
    }
}
