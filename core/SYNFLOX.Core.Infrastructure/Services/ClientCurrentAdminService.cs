using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Services;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service for accessing the current client admin's information and permissions.
    /// Reads from session via X-Session-Id header.
    /// </summary>
    public class ClientCurrentAdminService : IClientCurrentAdminService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICompanyAdminService _adminService;
        
        // Cached admin data to avoid multiple DB calls per request
        private bool _isInitialized = false;
        private bool _isAuthenticated = false;
        private Guid? _adminId;
        private Guid? _companyId;
        private string? _username;
        private List<string> _permissions = new List<string>();
        private bool _canManageDevices;
        private bool _canViewSubscriptions;
        private bool _canApproveReplacements;
        private bool _canGenerateLicenses;
        private bool _canViewUsageReports;
        private bool _canModifySessionSettings;

        public ClientCurrentAdminService(
            IHttpContextAccessor httpContextAccessor,
            ICompanyAdminService adminService)
        {
            _httpContextAccessor = httpContextAccessor;
            _adminService = adminService;
        }

        private void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;
            
            // Get session ID from header
            var sessionId = httpContext.Request.Headers["X-Session-Id"].ToString();
            if (string.IsNullOrEmpty(sessionId)) return;
            
            // Validate session and get admin info synchronously
            // Note: This is a blocking call, but necessary for property access
            var admin = _adminService.ValidateSessionAsync(sessionId).GetAwaiter().GetResult();
            if (admin == null) return;
            
            _isAuthenticated = true;
            _adminId = admin.Id;
            _companyId = admin.CompanyId;
            _username = admin.Username;
            
            // Build permissions list
            _canManageDevices = admin.CanManageDevices;
            _canViewSubscriptions = admin.CanViewSubscriptions;
            _canApproveReplacements = admin.CanApproveReplacements;
            _canGenerateLicenses = admin.CanGenerateLicenses;
            _canViewUsageReports = admin.CanViewUsageReports;
            _canModifySessionSettings = admin.CanModifySessionSettings;
            
            _permissions = new List<string>();
            if (_canManageDevices) _permissions.Add("CanManageDevices");
            if (_canViewSubscriptions) _permissions.Add("CanViewSubscriptions");
            if (_canApproveReplacements) _permissions.Add("CanApproveReplacements");
            if (_canGenerateLicenses) _permissions.Add("CanGenerateLicenses");
            if (_canViewUsageReports) _permissions.Add("CanViewUsageReports");
            if (_canModifySessionSettings) _permissions.Add("CanModifySessionSettings");
        }

        public bool IsAuthenticated { get { Initialize(); return _isAuthenticated; } }
        public Guid? AdminId { get { Initialize(); return _adminId; } }
        public Guid? CompanyId { get { Initialize(); return _companyId; } }
        public string? Username { get { Initialize(); return _username; } }
        public List<string> Permissions { get { Initialize(); return _permissions; } }
        public bool CanManageDevices { get { Initialize(); return _canManageDevices; } }
        public bool CanViewSubscriptions { get { Initialize(); return _canViewSubscriptions; } }
        public bool CanApproveReplacements { get { Initialize(); return _canApproveReplacements; } }
        public bool CanGenerateLicenses { get { Initialize(); return _canGenerateLicenses; } }
        public bool CanViewUsageReports { get { Initialize(); return _canViewUsageReports; } }
        public bool CanModifySessionSettings { get { Initialize(); return _canModifySessionSettings; } }
    }
}
