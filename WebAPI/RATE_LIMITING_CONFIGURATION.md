# ✅ Rate Limiting Configuration - CONFIGURED IN RateLimiterConfiguration.cs

## Configuration Location
**File:** `WebAPI/Configurations/RateLimiterConfiguration.cs`

**Status:** ✅ **ALREADY CONFIGURED** - No manual setup needed!

---

## Rate Limiting Policies

### 1. Password Change with 2FA
- **Endpoint:** `PUT /api/admin/profile/me/password/change-with-2fa`
- **Policy:** `PasswordChange`
- **DEV Limit:** 1000 attempts per hour (unlimited for testing)
- **PROD Limit:** 5 attempts per hour per user
- **Reason:** Prevent brute force attacks on 2FA codes

### 2. Check 2FA Status
- **Endpoint:** `GET /api/admin/auth/check-2fa-status`
- **Policy:** `Check2FAStatus`
- **DEV Limit:** 1000 requests per minute (unlimited for testing)
- **PROD Limit:** 10 requests per minute per IP
- **Reason:** Prevent email enumeration attacks

### 3. Security Reports (Already Configured)
- **Endpoint:** `POST /api/admin/profile/me/security/report/export`
- **Policy:** `SecurityReports`
- **Limit:** DEV=1000/hour | PRODUCTION=10/hour

## Notes
- Rate limiting is per IP address by default
- Consider adding per-user rate limiting for authenticated endpoints
- Monitor rate limit violations in application logs
- Adjust limits based on production usage patterns
