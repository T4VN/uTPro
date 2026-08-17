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

# Install Kerberos lib needed by Npgsql on Linux
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

# Low-memory tuning (512MB container)
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
ENV DOTNET_GCHeapHardLimit=0x18000000
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Disable file watchers — Render Free limits inotify instances to 128.
# ASP.NET Core's reloadOnChange creates too many watchers and crashes.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false

# Writable temp for Umbraco (NuCache, Examine indexes, locks)
RUN mkdir -p /tmp/umbraco
ENV TMPDIR=/tmp/umbraco

COPY --from=build /app/publish .

# Render assigns port via $PORT env var
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000

EXPOSE 10000

# Use shell form so $PORT is expanded at runtime
ENTRYPOINT dotnet uTPro.Project.Web.dll --urls "http://+:${PORT}"
