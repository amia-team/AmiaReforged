# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files (only WorldSimulator and AmiaReforged.Core dependency)
COPY WorldSimulator/WorldSimulator.csproj WorldSimulator/
COPY AmiaReforged.Core/AmiaReforged.Core.csproj AmiaReforged.Core/
RUN dotnet restore WorldSimulator/WorldSimulator.csproj

# Copy source and publish
COPY WorldSimulator/ WorldSimulator/
COPY AmiaReforged.Core/ AmiaReforged.Core/
RUN dotnet publish WorldSimulator/WorldSimulator.csproj -c Release -o /app/publish --no-restore

# Runtime stage - using runtime image instead of aspnet since this is a Worker service
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published output
COPY --from=build /app/publish .

# Health check endpoint (if we add one later)
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#   CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WorldSimulator.dll"]
