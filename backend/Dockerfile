# ----------------------------
# BUILD STAGE
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

COPY API/API.csproj API/
COPY Business/Business.csproj Business/
COPY Core/Core.csproj Core/
COPY DataAccess/DataAccess.csproj DataAccess/
COPY Entities/Entities.csproj Entities/

# Restore only the API and its referenced projects for the runtime image.
RUN dotnet restore API/API.csproj

COPY . .

RUN dotnet publish API/API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ----------------------------
# RUNTIME STAGE
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "API.dll"]
