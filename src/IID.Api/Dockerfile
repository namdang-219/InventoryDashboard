# Multi-stage build for IID.Api
# Build context MUST be the repo root (where docker-compose.yml lives), so that
# paths like src/IID.Api/... resolve correctly.
#
# Local build:    cd /Users/namdang/Documents/IID && docker build -f src/IID.Api/Dockerfile -t iid-api .
# Compose uses:   build: { context: ., dockerfile: src/IID.Api/Dockerfile }

# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (better layer caching when only source changes).
COPY NuGet.config global.json ./
COPY src/IID.Domain/IID.Domain.csproj              src/IID.Domain/
COPY src/IID.Application/IID.Application.csproj    src/IID.Application/
COPY src/IID.Infrastructure/IID.Infrastructure.csproj src/IID.Infrastructure/
COPY src/IID.Api/IID.Api.csproj                    src/IID.Api/
RUN dotnet restore src/IID.Api/IID.Api.csproj

# Now copy the source.
COPY src/ src/

# Publish Release; framework-dependent so the runtime image stays small.
RUN dotnet publish src/IID.Api/IID.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Match the port exposed in docker-compose.yml.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as non-root (the aspnet image already ships a non-root user in 10.x; uncomment to force):
# USER $APP_UID

ENTRYPOINT ["dotnet", "IID.Api.dll"]
