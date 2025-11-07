namespace Domain.Enums
{
    /// <summary>
    /// Describes the usage of an OTP code.
    /// </summary>
    public enum OtpPurpose
    {
        EmailVerification = 1,
        PasswordReset = 2,
        PhoneVerification = 3
    }
}
