# NOIR API Dockerfile
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files for restore
COPY src/NOIR.sln ./
COPY src/NOIR.Domain/NOIR.Domain.csproj NOIR.Domain/
COPY src/NOIR.Application/NOIR.Application.csproj NOIR.Application/
COPY src/NOIR.Infrastructure/NOIR.Infrastructure.csproj NOIR.Infrastructure/
COPY src/NOIR.Web/NOIR.Web.csproj NOIR.Web/

# Restore dependencies
RUN dotnet restore NOIR.Web/NOIR.Web.csproj

# Copy all source code
COPY src/NOIR.Domain/ NOIR.Domain/
COPY src/NOIR.Application/ NOIR.Application/
COPY src/NOIR.Infrastructure/ NOIR.Infrastructure/
COPY src/NOIR.Web/ NOIR.Web/

# Build and publish
WORKDIR /src/NOIR.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r noir && useradd -r -g noir noir

# Copy published app
COPY --from=build /app/publish .

# Create uploads directory for file storage
RUN mkdir -p /app/uploads && chown -R noir:noir /app

# Switch to non-root user
USER noir

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/api/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "NOIR.Web.dll"]
