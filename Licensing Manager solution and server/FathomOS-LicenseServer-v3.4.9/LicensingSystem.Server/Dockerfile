# Fathom OS License Server - Dockerfile
# Build: docker build -t fathomos-license-server .
# Run: docker run -p 5000:5000 -v ./data:/app/data fathomos-license-server

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY LicensingSystem.Server/*.csproj ./LicensingSystem.Server/
COPY LicensingSystem.Shared/*.csproj ./LicensingSystem.Shared/
RUN dotnet restore LicensingSystem.Server/LicensingSystem.Server.csproj

# Copy source code
COPY LicensingSystem.Server/ ./LicensingSystem.Server/
COPY LicensingSystem.Shared/ ./LicensingSystem.Shared/

# Build and publish
WORKDIR /src/LicensingSystem.Server
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create data directory for SQLite database
RUN mkdir -p /app/data && chmod 777 /app/data

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DB_PATH=/app/data/licenses.db

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "LicensingSystem.Server.dll"]
