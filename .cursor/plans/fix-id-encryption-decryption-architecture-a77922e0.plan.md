<!-- a77922e0-1c80-4dfc-954e-22de6609bd90 86705e68-3168-464d-b06f-64cf1c3b127f -->
# Fix ID Encryption/Decryption Architecture

## Problem Analysis

Current violations of the architecture:

1. **Controllers** manually decrypting route parameters (e.g., `CompanyController`, `ApiKeyController`, `CompanyGroupController`)
2. **Services** manually encrypting IDs in responses (e.g., `ApiKeyService`, `CompanyService` for webhooks)
3. **Services** manually decrypting IDs from DTOs (e.g., `MenuItemService`)
4. **Services** passing encrypted IDs to other services (e.g., `CompanyGroupService` → `LicensingService`)
5. **Missing mapper configurations** for DTOs that contain encrypted IDs (e.g., `CreateCompanyCustomFieldDto.CompanyId`)

## Solution Architecture

### Rule: Encryption/Decryption ONLY in Mappers

- **Entity → DTO**: Mapper encrypts IDs using `EncryptGuidConverter`
- **DTO → Entity**: Mapper decrypts IDs using `DecryptGuidConverter` (when DTO contains encrypted IDs)
- **Route Parameters**: Controllers receive encrypted GUIDs, decrypt them before calling services (this is acceptable as it's the boundary layer)
- **Services**: Work ONLY with decrypted GUIDs internally, never encrypt/decrypt manually
- **Service-to-Service**: Always pass decrypted GUIDs

## Implementation Plan

### Phase 1: Fix Mapping Profiles

1. **Update `LicensingMappingProfile.cs`**:

   - Add decryption for `CreateCompanyCustomFieldDto.CompanyId` → `CompanyCustomField.CompanyId`
   - Add decryption for `CreateProjectModuleDto.ProjectId` and `ModuleId`
   - Add decryption for `UpdatePlanProjectModulesDto.ProjectModuleIds` (list)
   - Add decryption for any other DTOs with encrypted IDs

2. **Update `ApiKeyMappingProfile.cs`**:

   - Remove manual encryption from `ApiKeyService` - mapper already handles it
   - Ensure `CreateApiKeyResponse` uses mapper (or create mapping if needed)

3. **Update `MenuItemMappingProfile.cs`**:

   - Add decryption for `UpdateMenuItemsDto.ParentMenuItemsId` → `MenuItems.ParentMenuItemsId`
   - Remove manual decryption from `MenuItemService`

4. **Update `AdminMappingProfile.cs`**:

   - Verify all ID fields are properly handled (already looks correct)

5. **Review all other mapping profiles**:

   - Check for missing encryption/decryption configurations

### Phase 2: Remove Manual Encryption from Services

1. **Fix `ApiKeyService.cs`**:

   - Remove `_idEncryption.Encrypt()` calls on lines 72, 152
   - Use mapper to create `CreateApiKeyResponse` DTOs
   - Remove `IIdEncryptionService` dependency if no longer needed

2. **Fix `CompanyService.cs`**:

   - Line 72: Webhook payload encryption is acceptable (external system), but document it
   - Remove `IIdEncryptionService` dependency if only used for webhooks (or keep it)

3. **Fix `CompanyGroupService.cs`**:

   - Lines 165, 174, 183, 192: Remove encryption before calling `LicensingService`
   - Pass decrypted GUIDs directly: `await _licensingService.ActivateCompanyAsync(company.Id, ...)`
   - Remove `IIdEncryptionService` dependency

4. **Fix `MenuItemService.cs`**:

   - Line 121: Remove manual decryption
   - Use mapper to handle `UpdateMenuItemsDto.ParentMenuItemsId` decryption
   - Remove `IIdEncryptionService` dependency

5. **Review all other services**:

   - Remove any remaining manual encryption/decryption
   - Remove `IIdEncryptionService` dependencies where not needed

### Phase 3: Standardize Controller Pattern

1. **Keep decryption in controllers** (acceptable as boundary layer):

   - Controllers receive encrypted GUIDs from route/query parameters
   - Controllers decrypt them before passing to services
   - This is the ONLY place decryption should happen outside mappers

2. **Update all controllers** to follow consistent pattern:
   ```csharp
   [HttpGet("{id}")]
   public async Task<IActionResult> GetEntity(Guid id)
   {
       var decryptedId = _idEncryption.Decrypt(id);
       var result = await _service.GetByIdAsync(decryptedId);
       return Ok(new ApiResponse<EntityDto>(200, string.Empty, result));
   }
   ```

3. **Fix controllers that decrypt incorrectly**:

   - Ensure all route parameters are decrypted consistently
   - Ensure query parameters with IDs are decrypted
   - Ensure request body DTOs with IDs use mapper (not manual decryption)

### Phase 4: Fix DTOs with Encrypted IDs

1. **Identify all DTOs that contain IDs**:

   - `CreateCompanyCustomFieldDto.CompanyId` - needs decryption
   - `CreateProjectModuleDto.ProjectId`, `ModuleId` - need decryption
   - `UpdatePlanProjectModulesDto.ProjectModuleIds` - needs decryption (list)
   - `BulkOperationRequest.CompanyIds` - needs decryption (list)
   - `BulkUpdateRequest.CompanyIds` - needs decryption (list)
   - Any other DTOs with Guid ID fields

2. **Add mapper configurations** for all DTO → Entity mappings that contain IDs

3. **Update controllers** to use mapper for DTOs with IDs (not manual decryption)

### Phase 5: Update Rules Documentation

1. **Update `.cursor/rules/synflox-development-rules.mdc`**:

   - Clarify that encryption/decryption happens ONLY in mappers
   - Document that controllers can decrypt route parameters (boundary layer)
   - Document that services work with decrypted GUIDs internally
   - Add examples of correct patterns

## Files to Modify

### Mapping Profiles

- `Application/Mapping/LicensingMappingProfile.cs`
- `Application/Mapping/ApiKeyMappingProfile.cs`
- `Application/Mapping/MenuItemMappingProfile.cs`
- Review all other mapping profiles

### Services

- `Infrastructure/Services/ApiKeyService.cs`
- `Infrastructure/Services/CompanyService.cs`
- `Infrastructure/Services/CompanyGroupService.cs`
- `Infrastructure/Services/MenuItemService.cs`
- Review all other services for manual encryption/decryption

### Controllers

- Review all controllers for consistency
- Ensure DTOs with IDs use mapper, not manual decryption

### Rules

- `.cursor/rules/synflox-development-rules.mdc`

## Testing Checklist

- [ ] All Entity → DTO mappings encrypt IDs automatically
- [ ] All DTO → Entity mappings decrypt IDs automatically (where applicable)
- [ ] No services manually encrypt/decrypt IDs
- [ ] Controllers only decrypt route/query parameters (boundary layer)
- [ ] Services pass decrypted GUIDs to other services
- [ ] All DTOs with IDs are properly handled in mappers
- [ ] Build succeeds with no errors
- [ ] All existing functionality still works