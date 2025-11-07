<!-- 44a82a0f-1887-495c-b472-e4d795f270fb bba10a39-c1a8-4bf4-a66e-09b94364796b -->
# SYNFLOX Licensing Module Implementation Plan

## Phase 1: Remove User Features

- Delete User entity, UserRepository, UserService, UserController
- Remove OTP entities (OtpCode) and services (OtpService, OtpRepository)
- Remove User authentication endpoints (keep admin auth only)
- Remove UserDto and related DTOs
- Update JWT token generation to only support Admin
- Remove user-related localization keys
- Clean up references in ApplicationDBContext

## Phase 2: Create Licensing Domain Layer

- **Domain/Entities/Licensing/Company.cs**
- Extends AuditEntity<Guid>
- Properties: Name (required), IsActive, ExpiryDate (nullable DateTime)
- Additional: ContactEmail, ContactPhone, Address, LicenseKey (encrypted storage)

- **Domain/Enums/LicenseStatus.cs**
- Active, Expired, Suspended

- **Domain/Interfaces/ICompanyRepository.cs**
- Extends IBaseRepository<Guid, Company>
- GetByNameAsync(string name)
- GetByLicenseKeyAsync(string licenseKey)

## Phase 3: Create Licensing Application Layer

- **Application/DTOs/Licensing/**
- CreateCompanyDto, UpdateCompanyDto, CompanyDto
- ActivateCompanyRequest, SuspendCompanyRequest, ExtendCompanyRequest
- CompanyStatusResponse (status, expiryDate, isActive)
- GenerateLicenseKeyRequest, ValidateLicenseKeyRequest, LicenseKeyValidationResponse

- **Application/Services Interfaces/ILicensingService.cs**
- CreateCompanyAsync(CreateCompanyDto)
- ActivateCompanyAsync(Guid id, DateTime expiryDate)
- SuspendCompanyAsync(Guid id)
- ResumeCompanyAsync(Guid id)
- ExtendCompanyAsync(Guid id, DateTime newExpiryDate)
- CheckCompanyStatusAsync(Guid id) → CompanyStatusResponse
- GenerateLicenseKeyAsync(Guid companyId) → string (encrypted key)
- ValidateLicenseKeyAsync(string licenseKey) → LicenseKeyValidationResponse
- RegenerateLicenseKeyAsync(Guid companyId) → string

- **Application/Mapping/LicensingMappingProfile.cs**
- AutoMapper profile for Company ↔ DTOs

## Phase 4: Create Licensing Infrastructure Layer

- **Infrastructure/Repositories/CompanyRepository.cs**
- Implements ICompanyRepository
- Custom queries for name and license key lookup

- **Infrastructure/Services/LicensingService.cs**
- Status calculation logic:
- Expired: ExpiryDate < DateTime.UtcNow (takes precedence)
- Suspended: IsActive = false AND ExpiryDate >= DateTime.UtcNow
- Active: IsActive = true AND ExpiryDate >= DateTime.UtcNow
- License key generation: Encrypted JWT-like token
- Payload: CompanyId, ExpiryDate, IssuedDate (UTC), Version
- Signed with HMAC SHA256
- Encrypted with AES-256
- Base64 encoded
- License key validation:
- Decrypt and verify signature
- Validate expiry date
- Check system clock tampering (current time must be >= issued time)
- Return validation result with status

- **Infrastructure/Configurations/CompanyConfiguration.cs**
- EF Core configuration
- Unique index on Name
- Index on LicenseKey
- Index on ExpiryDate, IsActive for status queries

- **Infrastructure/Settings/LicenseKeySettings.cs**
- Encryption key, IV, signing key configuration

## Phase 5: Create Licensing WebAPI Layer

- **WebAPI/Controllers/LicensingController.cs**
- POST /api/licensing/companies - Create company (SuperAdmin only)
- GET /api/licensing/companies - List companies (SuperAdmin only)
- GET /api/licensing/companies/{id} - Get company (SuperAdmin only)
- PUT /api/licensing/companies/{id} - Update company (SuperAdmin only)
- DELETE /api/licensing/companies/{id} - Delete company (SuperAdmin only)
- PUT /api/licensing/{id}/activate - Activate company (SuperAdmin only)
- PUT /api/licensing/{id}/suspend - Suspend company (SuperAdmin only)
- PUT /api/licensing/{id}/resume - Resume company (SuperAdmin only)
- PUT /api/licensing/{id}/extend - Extend subscription (SuperAdmin only)
- GET /api/licensing/{id}/status - Check status (Public with API key or JWT)
- POST /api/licensing/{id}/license-key/generate - Generate license key (SuperAdmin only)
- POST /api/licensing/{id}/license-key/regenerate - Regenerate license key (SuperAdmin only)
- POST /api/licensing/validate-key - Validate license key (Public, for offline systems)

## Phase 6: Add Localization

- **Infrastructure/Resources/SharedResource.resx** (English)
- Licensing.CompanyCreated
- Licensing.CompanyUpdated
- Licensing.CompanyDeleted
- Licensing.CompanyActivated
- Licensing.CompanySuspended
- Licensing.CompanyResumed
- Licensing.SubscriptionExtended
- Licensing.SubscriptionExpired
- Licensing.SubscriptionActive
- Licensing.SubscriptionSuspended
- Licensing.LicenseKeyGenerated
- Licensing.LicenseKeyRegenerated
- Licensing.LicenseKeyValid
- Licensing.LicenseKeyInvalid
- Licensing.LicenseKeyExpired
- Licensing.SystemClockTampered
- Licensing.CompanyNotFound
- Licensing.InvalidExpiryDate
- Licensing.CompanyAlreadyActive
- Licensing.CompanyAlreadySuspended

- **Infrastructure/Resources/SharedResource.ar.resx** (Arabic)
- Same keys with proper Arabic translations

## Phase 7: Update Infrastructure Registration

- Register CompanyRepository, ILicensingService, LicensingService
- Add Company DbSet to ApplicationDBContext
- Register license key settings from appsettings.json

## Phase 8: Security Implementation

- API Key authentication for public status endpoint
- License key encryption/decryption service
- Clock tampering detection algorithm
- Secure key storage (encrypted in database)

## Phase 9: Documentation

- **README.md** - Comprehensive documentation:
- What SYNFLOX is (Central Licensing System)
- Architecture explanation
- How external products call the API
- Online vs Offline system integration
- License key format and validation
- Examples for ERP/CRM/POS usage
- Arabic vs English response examples
- API endpoint documentation

## Phase 10: Testing & Validation

- Verify all endpoints work correctly
- Test status calculation logic
- Test license key generation and validation
- Test clock tampering detection
- Verify localization works (EN/AR)
- Test authorization policies

### To-dos

- [x] Remove all User-related features: User entity, UserRepository, UserService, UserController, OTP entities/services, User DTOs, and update authentication to Admin-only
- [x] Create Company entity in Domain/Entities/Licensing/ with Name, IsActive, ExpiryDate, ContactEmail, ContactPhone, Address, LicenseKey fields
- [x] Create LicenseStatus enum (Active, Expired, Suspended) in Domain/Enums/
- [x] Create ICompanyRepository interface and CompanyRepository implementation with custom queries
- [x] Create all Licensing DTOs in Application/DTOs/Licensing/ (CreateCompanyDto, UpdateCompanyDto, CompanyDto, status responses, license key DTOs)
- [x] Create ILicensingService interface with all methods (CreateCompany, ActivateCompany, SuspendCompany, ResumeCompany, ExtendCompany, CheckStatus, GenerateLicenseKey, ValidateLicenseKey)
- [x] Implement LicensingService with status calculation logic, license key generation (encrypted JWT-like token), validation with clock tampering detection
- [x] Create CompanyConfiguration for EF Core with indexes on Name, LicenseKey, ExpiryDate, IsActive
- [x] Create LicensingController with all endpoints (CRUD, activate, suspend, resume, extend, status, license key generation/validation)
- [x] Add all Licensing localization keys to SharedResource.resx (EN) and SharedResource.ar.resx (AR) with proper translations
- [x] Register CompanyRepository, ILicensingService, LicensingService, and license key settings in InfrastructureServiceRegistration
- [x] Add Company DbSet to ApplicationDBContext and ensure CompanyConfiguration is applied
- [x] Create LicensingMappingProfile for AutoMapper to map Company entity to/from DTOs with ID encryption
- [x] Implement secure license key generation (encrypted JWT-like token with HMAC signature + AES encryption) and validation with clock tampering detection
- [x] Create comprehensive README.md explaining SYNFLOX purpose, architecture, API usage, online/offline integration, examples for ERP/CRM/POS, Arabic vs English examples
- [x] Implement ID encryption/decryption: Encrypt IDs in GET responses (via AutoMapper), Decrypt IDs in POST/PUT/DELETE requests (in Controller)