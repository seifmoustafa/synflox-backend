using System;

namespace Domain.Enums
{
    [Flags]
    public enum AuthProvider
    {
        None = 0,
        Credentials = 1,
        Google = 2,
        Facebook = 4,
        Apple = 8
    }
}
