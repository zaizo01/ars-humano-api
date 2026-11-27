# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaurar primero (capa cacheada mientras no cambien los .csproj)
COPY global.json Directory.Build.props HealthAuthorizations.sln ./
COPY src/HealthAuthorizations.Domain/*.csproj src/HealthAuthorizations.Domain/
COPY src/HealthAuthorizations.Application/*.csproj src/HealthAuthorizations.Application/
COPY src/HealthAuthorizations.Infrastructure/*.csproj src/HealthAuthorizations.Infrastructure/
COPY src/HealthAuthorizations.Api/*.csproj src/HealthAuthorizations.Api/
RUN dotnet restore src/HealthAuthorizations.Api/HealthAuthorizations.Api.csproj

COPY src/ src/
RUN dotnet publish src/HealthAuthorizations.Api/HealthAuthorizations.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
# Usuario sin privilegios incluido en la imagen base
USER $APP_UID
ENTRYPOINT ["dotnet", "HealthAuthorizations.Api.dll"]
