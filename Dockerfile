# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and csproj files
COPY *.sln ./
COPY ./WebAPI/WebAPI.csproj ./WebAPI/
COPY ./Application/Application.csproj ./Application/
COPY ./Domain/Domain.csproj ./Domain/
COPY ./Infrastructure/Infrastructure.csproj ./Infrastructure/

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy source code
COPY ./WebAPI ./WebAPI/
COPY ./Application ./Application/
COPY ./Domain ./Domain/
COPY ./Infrastructure ./Infrastructure/

# Build and publish
RUN dotnet publish "WebAPI/WebAPI.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 10000

# Set environment variables for email configuration
ENV EmailSettings__User=seif.moustafa516@gmail.com \
    EmailSettings__Pass=paagqvddbrdagikq

ENTRYPOINT ["sh", "-c", "dotnet WebAPI.dll --urls http://0.0.0.0:$PORT"]
