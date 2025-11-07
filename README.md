# .Net Login Project – Clean Architecture

This repository provides a starting point for building ASP.NET Core Web APIs using a layered **Clean Architecture**. It combines Entity Framework Core for data access, JWT bearer authentication and a variety of infrastructure concerns (caching, logging, localization, file storage and more) to create a secure login and user management template.

## Project structure

| Folder | Description |
| ------ | ----------- |
| `Domain` | Entity models, enums, interfaces and domain‑level exceptions. |
| `Application` | DTOs, mappings and service contracts that implement business rules. |
| `Infrastructure` | EF Core context, repositories, authentication helpers and other service implementations. |
| `WebAPI` | ASP.NET Core host project containing controllers, middleware and API configuration. |
| `Benchmarks` | BenchmarkDotNet project used for performance testing repository queries. |

## Features

- Registration and authentication with JWT access tokens and refresh tokens stored in cookies.
- Email and phone verification using one‑time passwords and configurable resend limits.
- Password reset flow and support for external login providers.
- Role based administration with **SuperAdmin** and **Admin** policies.
- CRUD operations for users and administrators with encrypted identifier support.
- File upload handling up to 10 GB and static serving of stored images, videos, PDFs and PPTX files.
- Localization for English and Arabic cultures.
- Request logging, exception handling, cache headers and ETag middleware.
- Authentication endpoint rate limiting and response caching.

## Configuration

All settings are read from *appsettings.json* (or environment variables) in the `WebAPI` project.

- **ConnectionStrings** – provide `SqlServerConnection` or `OracleConnection` for the database.
- **JwtSettings** – `Issuer`, `Audience`, `SecretKey`, `Lifetime` and `RefreshTokenExpiration` values.
- **ImageSettings**, **PdfSettings**, **PptxSettings**, **VideoSettings** and **AllFileSettings** – storage and request paths, allowed extensions and `MaxFileSizeMb`.
- **EmailSettings** – SMTP `Host`, `Port`, `FromName`/`FromEmail` and optional `Mode` value. Credentials are supplied via environment variables (`EmailSettings__User`, `EmailSettings__Pass`).
- **Verification** – configure OTP expiration and resend limits.
- **CacheHeaders** and **ETag** – options for the corresponding middlewares.

Update these values before running the application.

## Running the application

```bash
# Restore and build the solution
dotnet build

# Run the WebAPI project
dotnet run --project WebAPI
```

A Dockerfile is also provided. Build and run with:

```bash
docker build -t login-template .
docker run -p 8080:8080 login-template
```

## API endpoints

### Authentication

- `POST /api/authentication/register` – register a new user.
- `POST /api/authentication/verify-email` – verify e‑mail using OTP.
- `POST /api/authentication/verify-phone` – verify phone number using OTP.
- `POST /api/authentication/login` – login with e‑mail/phone/username and password.
- `POST /api/authentication/login/admin` – administrator login.
- `POST /api/authentication/login/{provider}` – external provider login.
- `POST /api/authentication/refresh-token` – refresh the access token.
- `POST /api/authentication/logout` – revoke the refresh token and clear cookies.

### Admin operations

- `POST /api/admins` – create a new administrator (**SuperAdmin** only).
- `POST /api/admins/users` – create a regular user (**Admin** or **SuperAdmin**).
- `PUT /api/admins/{id}/password` – change another admin's password (**Admin** or **SuperAdmin**).
- `PUT /api/admins/{id}/activate` / `deactivate` – enable or disable an admin (**SuperAdmin**).
- `DELETE /api/admins/me` – remove the current administrator.

### User operations

- `GET /api/users/me` – retrieve the current user's profile.
- `PUT /api/users/me` – update profile details.
- `PUT /api/users/me/password` – change the current user's password.
- `GET /api/users` – list all users (**SuperAdmin** only).
- `GET /api/users/{id}` – retrieve a user by ID (**SuperAdmin** only).
- `POST /api/users` – create a user (**SuperAdmin** only).
- `PUT /api/users/{id}` – update a user (**SuperAdmin** only).
- `DELETE /api/users/{id}` – delete a user (**SuperAdmin** only).
- `POST /api/users/{id}/reset-password` – reset a user's password (**SuperAdmin** only).

Request/exception logging, cache headers and ETag middleware are enabled by default for easier diagnostics and performance.

## Benchmarks

Run repository benchmarks in release mode:

```bash
dotnet run -c Release --project Benchmarks
```

This executes `RepositorySearchBenchmark` using BenchmarkDotNet.

---

This template is intended as a learning tool or a starting point for projects requiring a robust authentication and authorization system built on top of ASP.NET Core's clean architecture approach.

