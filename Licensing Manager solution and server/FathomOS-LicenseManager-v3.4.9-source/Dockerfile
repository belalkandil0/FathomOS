# Fathom OS License Server - Dockerfile for Render.com
# Root directory should be: Licensing Manager solution and server/FathomOS-LicenseManager-v3.4.9-source

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
# Note: Render uses port 10000 by default
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DB_PATH=/app/data/licenses.db

# Expose port (Render uses 10000)
EXPOSE 10000

# Run the app
ENTRYPOINT ["dotnet", "LicensingSystem.Server.dll"]
