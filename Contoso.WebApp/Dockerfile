# Multi-stage Dockerfile for Contoso.WebApp (.NET 8 Razor Pages)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy csproj and restore (layer caching)
COPY Contoso.WebApp.csproj ./
RUN dotnet restore Contoso.WebApp.csproj
# Copy source
COPY . .
# Publish
RUN dotnet publish Contoso.WebApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
EXPOSE 8081
# Healthcheck aligned with configured port
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD wget -qO- http://localhost:8081/ || exit 1
# Create directory for persisted DataProtection keys (mounted as volume optionally)
VOLUME ["/keys"]
ENTRYPOINT ["dotnet","Contoso.WebApp.dll"]
