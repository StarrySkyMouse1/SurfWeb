FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Server/SurfWeb.slnx Server/
COPY Server/SurfWeb.Api/SurfWeb.Api.csproj Server/SurfWeb.Api/
COPY Server/Configurations/Configurations.csproj Server/Configurations/
COPY Server/Services/SurfWeb.Services.csproj Server/Services/
COPY Server/Repositories/Repositories.csproj Server/Repositories/
COPY Server/Utils/Utils.csproj Server/Utils/
COPY Server/SurfWeb.Core/SurfWeb.Core.csproj Server/SurfWeb.Core/
COPY Server/SurfWeb.Realtime/SurfWeb.Realtime.csproj Server/SurfWeb.Realtime/
COPY Server/SurfWeb.ServerStatus/SurfWeb.ServerStatus.csproj Server/SurfWeb.ServerStatus/

RUN dotnet restore Server/SurfWeb.Api/SurfWeb.Api.csproj

COPY Server/ Server/
RUN dotnet publish Server/SurfWeb.Api/SurfWeb.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5240

COPY --from=build /app/publish .

EXPOSE 5240

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -fsS http://127.0.0.1:5240/health || exit 1

ENTRYPOINT ["dotnet", "SurfWeb.Api.dll"]
