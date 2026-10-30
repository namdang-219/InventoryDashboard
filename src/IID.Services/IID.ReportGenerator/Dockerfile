# Multi-stage build for IID.ReportGenerator
# Build context MUST be the repo root (where docker-compose.yml lives).
#
# Local build:    cd /Users/namdang/Documents/IID && docker build -f src/IID.Services/IID.ReportGenerator/Dockerfile -t iid-report-generator .
# Compose uses:   build: { context: ., dockerfile: src/IID.Services/IID.ReportGenerator/Dockerfile }

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first for optimal layer caching
COPY NuGet.config global.json ./
COPY src/IID.Domain/IID.Domain.csproj                                      src/IID.Domain/
COPY src/IID.Application/IID.Application.csproj                            src/IID.Application/
COPY src/IID.Infrastructure/IID.Infrastructure.csproj                      src/IID.Infrastructure/
COPY src/IID.Services/IID.ReportGenerator/IID.ReportGenerator.csproj       src/IID.Services/IID.ReportGenerator/
RUN dotnet restore src/IID.Services/IID.ReportGenerator/IID.ReportGenerator.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/IID.Services/IID.ReportGenerator/IID.ReportGenerator.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "IID.ReportGenerator.dll"]

