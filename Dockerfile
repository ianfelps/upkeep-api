# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY upkeep-api.sln ./
COPY src/UpkeepAPI/UpkeepAPI.csproj src/UpkeepAPI/
COPY tests/UpkeepAPI.Tests/UpkeepAPI.Tests.csproj tests/UpkeepAPI.Tests/
RUN dotnet restore src/UpkeepAPI/UpkeepAPI.csproj

COPY src/ src/
RUN dotnet publish src/UpkeepAPI/UpkeepAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UpkeepAPI.dll"]
