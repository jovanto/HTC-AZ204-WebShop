# Multi-stage build for Contoso.Api (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy project file and restore first (layer caching)
COPY Contoso.Api.csproj ./
RUN dotnet restore Contoso.Api.csproj
# Copy remaining source
COPY . .
# Publish (no app host for smaller image)
RUN dotnet publish Contoso.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget -qO- http://localhost:8080/ || exit 1
ENTRYPOINT ["dotnet","Contoso.Api.dll"]
