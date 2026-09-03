# ============================================================
# Stage 1: Build & Publish
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + all project files first for layer caching
COPY uTPro.sln global.json ./
COPY Common/uTPro.Common/uTPro.Common.csproj Common/uTPro.Common/
COPY Foundation/uTPro.Foundation/uTPro.Foundation.csproj Foundation/uTPro.Foundation/
COPY Foundation/uTPro.Foundation.Sitemap/uTPro.Foundation.Sitemap.csproj Foundation/uTPro.Foundation.Sitemap/
COPY Foundation/uTPro.Foundation.Robots/uTPro.Foundation.Robots.csproj Foundation/uTPro.Foundation.Robots/
COPY Foundation/uTPro.Foundation.Favicon/uTPro.Foundation.Favicon.csproj Foundation/uTPro.Foundation.Favicon/
COPY Foundation/uTPro.Foundation.Middleware/uTPro.Foundation.Middleware.csproj Foundation/uTPro.Foundation.Middleware/
COPY Extension/uTPro.Extension/uTPro.Extension.csproj Extension/uTPro.Extension/
COPY Extension/uTPro.Extension.CurrentSite/uTPro.Extension.CurrentSite.csproj Extension/uTPro.Extension.CurrentSite/
COPY Extension/uTPro.Extension.Search/uTPro.Extension.Search.csproj Extension/uTPro.Extension.Search/
COPY Extension/uTPro.Extension.UrlRouting/uTPro.Extension.UrlRouting.csproj Extension/uTPro.Extension.UrlRouting/
COPY Feature/uTPro.Feature/uTPro.Feature.csproj Feature/uTPro.Feature/
COPY Feature/uTPro.Feature.Dashboard/uTPro.Feature.Dashboard.csproj Feature/uTPro.Feature.Dashboard/
COPY Feature/uTPro.Feature.BlockPreviewToggle/uTPro.Feature.BlockPreviewToggle.csproj Feature/uTPro.Feature.BlockPreviewToggle/
COPY Project/uTPro.Project.Web/uTPro.Project.Web.csproj Project/uTPro.Project.Web/
COPY Project/uTPro.Project.Web.Configure/uTPro.Project.Web.Configure.csproj Project/uTPro.Project.Web.Configure/

RUN dotnet restore

# Copy everything & publish
COPY . .
RUN dotnet publish Project/uTPro.Project.Web/uTPro.Project.Web.csproj \
    -c Release -o /app/publish --no-restore

# ============================================================
# Stage 2: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Kerberos lib needed by Npgsql on Linux + curl for container healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl && rm -rf /var/lib/apt/lists/*

# Low-memory tuning (512MB container)
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_GCHeapHardLimit=0x18000000
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Disable file watchers — Render Free limits inotify instances to 128.
# ASP.NET Core's reloadOnChange creates too many watchers and crashes.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
# Use polling instead of inotify for any remaining file watchers (avoids inotify exhaustion).
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Writable temp for Umbraco (NuCache, Examine indexes, locks)
RUN mkdir -p /tmp/umbraco
ENV TMPDIR=/tmp/umbraco

COPY --from=build /app/publish .

# Render assigns port via $PORT env var
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000

# Create a non-root user and grant it ownership of the app + writable temp dirs.
# Use --force so groupadd is a no-op if GID 1000 already exists in the base image,
# and use the -o (non-unique) flag on useradd to tolerate a pre-existing UID 1000.
RUN groupadd --system --gid 1000 --force appgroup \
    && useradd --system --uid 1000 -o --gid appgroup --no-create-home appuser \
    && chown -R appuser:appgroup /app /tmp/umbraco
USER appuser

EXPOSE 10000

# Verify the container is serving requests on the assigned port
HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fsS "http://localhost:${PORT}/" || exit 1

# Use shell form so $PORT is expanded at runtime
ENTRYPOINT dotnet uTPro.Project.Web.dll --urls "http://+:${PORT}"
