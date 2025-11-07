# Cursor Rules for SYNFLOX

This folder contains development rules and guidelines for the SYNFLOX project.

## Files

- `synflox-development-rules.md` - Complete development rules, architecture guide, and feature implementation checklist

## How to Use

Cursor will automatically read these rules files to understand the project structure and development patterns. When implementing new features or making changes, refer to these rules to ensure consistency with the existing codebase.

## Quick Reference

1. **Architecture**: Clean Architecture (4 layers)
2. **Feature Implementation**: Follow the checklist in `synflox-development-rules.md`
3. **ID Encryption**: Always encrypt in responses, decrypt in requests
4. **Authorization**: Use `SuperAdminOnly` for management endpoints
5. **Localization**: Always use `_localizer["Key"]` for messages
6. **Validation**: Check `ModelState.IsValid` on POST/PUT endpoints

For detailed rules, see `synflox-development-rules.md`.

