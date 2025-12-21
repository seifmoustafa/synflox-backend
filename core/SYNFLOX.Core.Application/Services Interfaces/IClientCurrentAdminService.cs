using System;
using System.Collections.Generic;

namespace Application.Services
{
    /// <summary>
    /// Service interface for accessing the current client admin's information and permissions.
    /// Used by services to filter data based on the logged-in company admin.
    /// </summary>
    public interface IClientCurrentAdminService
    {
        /// <summary>
        /// Whether the current request has a valid session.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// The ID of the current company admin.
        /// </summary>
        Guid? AdminId { get; }

        /// <summary>
        /// The ID of the company the admin belongs to.
        /// </summary>
        Guid? CompanyId { get; }

        /// <summary>
        /// The username of the current admin.
        /// </summary>
        string? Username { get; }

        /// <summary>
        /// List of permissions the current admin has.
        /// </summary>
        List<string> Permissions { get; }

        /// <summary>
        /// Whether the admin can manage devices.
        /// </summary>
        bool CanManageDevices { get; }

        /// <summary>
        /// Whether the admin can view subscriptions.
        /// </summary>
        bool CanViewSubscriptions { get; }

        /// <summary>
        /// Whether the admin can approve device replacements.
        /// </summary>
        bool CanApproveReplacements { get; }

        /// <summary>
        /// Whether the admin can generate licenses.
        /// </summary>
        bool CanGenerateLicenses { get; }

        /// <summary>
        /// Whether the admin can view usage reports.
        /// </summary>
        bool CanViewUsageReports { get; }

        /// <summary>
        /// Whether the admin can modify session settings.
        /// </summary>
        bool CanModifySessionSettings { get; }
    }
}
