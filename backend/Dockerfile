# Finans API — çok aşamalı, minimal, non-root imaj (02 §6, 11 §8).
# Bağlam: backend/ (yalnız .NET; web/mobil ayrı).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore katmanı: önce csproj'lar (katman cache verimi). Test projeleri gerekmez.
COPY src/Finans.Api/Finans.Api.csproj src/Finans.Api/
COPY src/Finans.Application/Finans.Application.csproj src/Finans.Application/
COPY src/Finans.Domain/Finans.Domain.csproj src/Finans.Domain/
COPY src/Finans.Infrastructure/Finans.Infrastructure.csproj src/Finans.Infrastructure/
RUN dotnet restore src/Finans.Api/Finans.Api.csproj

COPY src/ src/
RUN dotnet publish src/Finans.Api/Finans.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Düz HTTP 8080 (TLS reverse proxy'de sonlanır — 11 §5).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Liveness health (busybox wget — alpine'de mevcut).
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
  CMD wget -qO- http://localhost:8080/health || exit 1

# Non-root (imaj sırsız; en az ayrıcalık — 11 §8). APP_UID base imajda tanımlı.
USER $APP_UID

ENTRYPOINT ["dotnet", "Finans.Api.dll"]
