# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["User.Api.csproj", "."]
COPY ["nuget.config", "."]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "User.Api.dll"]
