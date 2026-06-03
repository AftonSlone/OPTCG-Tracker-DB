# Multi-stage build: the .NET SDK lives only in the build image.
# The final runtime image is slim and contains no SDK/local tooling.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for layer-cached restore.
COPY src/OPTCG.Tracker.Data/OPTCG.Tracker.Data.csproj src/OPTCG.Tracker.Data/
COPY src/OPTCG.Tracker.Importer/OPTCG.Tracker.Importer.csproj src/OPTCG.Tracker.Importer/
RUN dotnet restore src/OPTCG.Tracker.Importer/OPTCG.Tracker.Importer.csproj

# Copy the rest of the source and publish.
COPY . .
RUN dotnet publish src/OPTCG.Tracker.Importer/OPTCG.Tracker.Importer.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OPTCG.Tracker.Importer.dll"]
