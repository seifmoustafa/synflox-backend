using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for LoginAttempt entity operations.
/// </summary>
public interface ILoginAttemptRepository : IBaseRepository<Guid, LoginAttempt>
{
    /// <summary>
    /// Gets recent failed attempts for a username within the specified time window.
    /// </summary>
    Task<IEnumerable<LoginAttempt>> GetRecentFailedAttemptsAsync(
        string username,
        DateTime since,
        int limit = 10);

    /// <summary>
    /// Gets recent failed attempts by IP address.
    /// </summary>
    Task<IEnumerable<LoginAttempt>> GetRecentFailedAttemptsByIpAsync(
        string ipAddress,
        DateTime since,
        int limit = 10);

    /// <summary>
    /// Counts failed attempts for a username within the specified time window.
    /// </summary>
    Task<int> CountFailedAttemptsAsync(string username, DateTime since);

    /// <summary>
    /// Counts failed attempts by IP address within the specified time window.
    /// </summary>
    Task<int> CountFailedAttemptsByIpAsync(string ipAddress, DateTime since);
}

