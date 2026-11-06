FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY GastronomiaMarketplace.sln ./
COPY src/Domain/GastronomiaMarketplace.Domain.csproj src/Domain/
COPY src/Application/GastronomiaMarketplace.Application.csproj src/Application/
COPY src/Infrastructure/GastronomiaMarketplace.Infrastructure.csproj src/Infrastructure/
COPY src/WebAPI/GastronomiaMarketplace.WebAPI.csproj src/WebAPI/
RUN dotnet restore

COPY src/ src/
RUN dotnet publish src/WebAPI/GastronomiaMarketplace.WebAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/keys
VOLUME ["/app/keys"]
ENTRYPOINT ["dotnet", "GastronomiaMarketplace.WebAPI.dll"]
