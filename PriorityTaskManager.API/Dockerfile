# Build from the repository root: docker build -f PriorityTaskManager.API/Dockerfile .
# (needs repo-root context so it can see the PriorityTaskManager project reference).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PriorityTaskManager/PriorityTaskManager.csproj PriorityTaskManager/
COPY PriorityTaskManager.API/PriorityTaskManager.API.csproj PriorityTaskManager.API/
RUN dotnet restore PriorityTaskManager.API/PriorityTaskManager.API.csproj

COPY PriorityTaskManager/ PriorityTaskManager/
COPY PriorityTaskManager.API/ PriorityTaskManager.API/
RUN dotnet publish PriorityTaskManager.API/PriorityTaskManager.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Production config (real connection string, Jwt:Key, etc.) is supplied via environment
# variables/secrets at deploy time, never baked into the image (see appsettings.json,
# which intentionally ships no Jwt:Key so a missing production secret fails fast).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PriorityTaskManager.API.dll"]
