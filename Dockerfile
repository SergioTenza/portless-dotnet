# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY Portless.slnx .
COPY Directory.Build.props .
COPY Portless.Core/Portless.Core.csproj Portless.Core/
COPY Portless.Proxy/Portless.Proxy.csproj Portless.Proxy/

# Restore dependencies
RUN dotnet restore Portless.Proxy/Portless.Proxy.csproj

# Copy source code
COPY Portless.Core/ Portless.Core/
COPY Portless.Proxy/ Portless.Proxy/

# Publish the proxy
RUN dotnet publish Portless.Proxy/Portless.Proxy.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd --system --gid 2000 portless \
    && useradd --system --uid 2000 --gid portless --shell /bin/bash --home-dir /home/portless --create-home portless

COPY --from=build /app/publish .

# Create state directory and set ownership
RUN mkdir -p /home/portless/.portless \
    && chown -R portless:portless /home/portless/.portless \
    && chown -R portless:portless /app

# Environment defaults
ENV PORTLESS_PORT=1355 \
    PORTLESS_HTTPS_ENABLED=false \
    PORTLESS_STATE_DIR=/home/portless/.portless \
    DOTNET_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:1355

EXPOSE 1355
EXPOSE 1356

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:1355/health || exit 1

USER portless

ENTRYPOINT ["dotnet", "Portless.Proxy.dll"]
