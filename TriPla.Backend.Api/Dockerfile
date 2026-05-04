# syntax=docker/dockerfile:1.7
# Multi-stage build dla TriPla.Backend.Api (.NET 10).
# Build context = root rozwiązania (TriPla-Backend/), nie folder Api.

# ---------- 1. RESTORE ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Najpierw kopiujemy tylko csproj-y i sln, żeby cachować restore.
COPY global.json TriPla-Backend.sln ./
COPY TriPla.Backend.Domain/TriPla.Backend.Domain.csproj         TriPla.Backend.Domain/
COPY TriPla.Backend.Application/TriPla.Backend.Application.csproj TriPla.Backend.Application/
COPY TriPla.Backend.Infrastructure/TriPla.Backend.Infrastructure.csproj TriPla.Backend.Infrastructure/
COPY TriPla.Backend.Api/TriPla.Backend.Api.csproj               TriPla.Backend.Api/
COPY TriPla.Backend.Tests/TriPla.Backend.Tests.csproj           TriPla.Backend.Tests/

RUN dotnet restore TriPla-Backend.sln

# ---------- 2. BUILD + PUBLISH ----------
FROM restore AS build
WORKDIR /src
COPY . .
RUN dotnet publish TriPla.Backend.Api/TriPla.Backend.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------- 3. RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Tworzymy non-root usera (image bazowy ma APP_UID=1654)
USER $APP_UID

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "TriPla.Backend.Api.dll"]
